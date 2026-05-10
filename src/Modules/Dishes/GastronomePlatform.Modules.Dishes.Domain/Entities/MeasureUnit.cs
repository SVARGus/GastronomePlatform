using GastronomePlatform.Common.Domain.Primitives;
using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Domain.Entities
{
    /// <summary>
    /// Справочник единиц измерения с коэффициентами конвертации к базовой единице каждого типа.
    /// Базовая единица для <see cref="MeasureUnitType.Mass"/> — грамм;
    /// для <see cref="MeasureUnitType.Volume"/> — миллилитр.
    /// </summary>
    /// <remarks>
    /// Кросс-сущностный инвариант «ровно одна запись с <see cref="IsBase"/> = true на каждый <see cref="Type"/>»
    /// проверяется на уровне Application или БД, не в Domain.
    /// На Этапе 2 запись создаётся seed-данными; runtime-кода для добавления единиц пока нет.
    /// </remarks>
    public sealed class MeasureUnit : Entity<Guid>
    {
        #region Properties

        /// <summary>
        /// Уникальное кодовое обозначение единицы (латиницей).
        /// Примеры: <c>g</c>, <c>kg</c>, <c>ml</c>, <c>tbsp</c>, <c>tsp</c>, <c>cup_250</c>, <c>pinch</c>, <c>pcs</c>.
        /// </summary>
        public string Code { get; private set; } = string.Empty;

        /// <summary>
        /// Название единицы на русском языке для отображения пользователю.
        /// Примеры: «грамм», «столовая ложка», «стакан 250 мл».
        /// </summary>
        public string NameRu { get; private set; } = string.Empty;

        /// <summary>
        /// Тип единицы измерения. Определяет правила конвертации.
        /// </summary>
        public MeasureUnitType Type { get; private set; }

        /// <summary>
        /// Коэффициент пересчёта к базовой единице того же <see cref="Type"/>.
        /// Для базовой единицы равен <c>1</c>. Примеры: <c>kg</c> → <c>1000</c> (в граммах),
        /// <c>tbsp</c> → <c>15</c> (в миллилитрах).
        /// </summary>
        public decimal ConversionToBase { get; private set; }

        /// <summary>
        /// <see langword="true"/>, если эта единица — базовая в своём <see cref="Type"/>
        /// (грамм для <see cref="MeasureUnitType.Mass"/>, миллилитр для <see cref="MeasureUnitType.Volume"/>).
        /// На каждый тип допустима ровно одна запись с этим флагом
        /// (за исключением <see cref="MeasureUnitType.Pinch"/> — он не конвертируется).
        /// </summary>
        public bool IsBase { get; private set; }

        #endregion

        #region Constructors
        /// <summary>
        /// Конструктор без параметров для EF Core.
        /// EF Core использует его при материализации объектов из БД.
        /// </summary>
        private MeasureUnit() : base() { }

        /// <summary>
        /// Создаёт новый экземпляр <see cref="MeasureUnit"/>.
        /// Используется только из фабричного метода <see cref="Create"/>.
        /// </summary>
        /// <param name="code">Уникальное кодовое обозначение единицы (латиницей).</param>
        /// <param name="nameRu">Русское название единицы для отображения пользователю.</param>
        /// <param name="type">Тип единицы измерения.</param>
        /// <param name="conversionToBase">Коэффициент пересчёта к базовой единице своего <paramref name="type"/>.</param>
        /// <param name="isBase">Является ли эта единица базовой в своём <paramref name="type"/>.</param>
        private MeasureUnit(
            string code,
            string nameRu,
            MeasureUnitType type,
            decimal conversionToBase,
            bool isBase)
            : base(Guid.NewGuid())
        {
            Code = code;
            NameRu = nameRu;
            Type = type;
            ConversionToBase = conversionToBase;
            IsBase = isBase;
        }
        #endregion

        #region Factory Methods
        /// <summary>
        /// Создаёт новую единицу измерения.
        /// Валидация входных данных ожидается на уровне команды (FluentValidation),
        /// в фабрике проверки не выполняются.
        /// </summary>
        /// <param name="code">Уникальное кодовое обозначение единицы.</param>
        /// <param name="nameRu">Русское название единицы.</param>
        /// <param name="type">Тип единицы измерения.</param>
        /// <param name="conversionToBase">Коэффициент пересчёта к базовой единице своего <paramref name="type"/>.</param>
        /// <param name="isBase">Является ли эта единица базовой в своём <paramref name="type"/>.</param>
        /// <returns>Новый экземпляр <see cref="MeasureUnit"/>.</returns>
        public static MeasureUnit Create(
            string code,
            string nameRu,
            MeasureUnitType type,
            decimal conversionToBase,
            bool isBase)
        {
            return new MeasureUnit(code, nameRu, type, conversionToBase, isBase);
        }
        #endregion
    }
}
