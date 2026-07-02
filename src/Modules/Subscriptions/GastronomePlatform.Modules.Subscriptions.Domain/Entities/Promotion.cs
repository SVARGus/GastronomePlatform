using GastronomePlatform.Common.Domain.Primitives;

namespace GastronomePlatform.Modules.Subscriptions.Domain.Entities
{
    /// <summary>
    /// Промоакция — оверлей грантов, действующий на срок акции.
    /// Forward-compat заготовка для Phase C: сама сущность, состав таргетов
    /// (<see cref="PromotionTarget"/>) и изменяемые гранты (<see cref="PromotionGrant"/>)
    /// материализуются в БД сразу, но реальное создание/редактирование
    /// (UC-SUB-008, UC-SUB-009, UC-SUB-010) и применение в резолвере грантов —
    /// в Phase C.
    /// </summary>
    /// <remarks>
    /// <para>
    /// В Phase A есть только приватный конструктор для EF Core — фабричные
    /// методы и Update-логика будут добавлены при реализации Phase C.
    /// Пустая таблица <c>promotions</c> нужна уже сейчас, чтобы FK-ссылки из
    /// <see cref="PromotionTarget"/> и <see cref="PromotionGrant"/> собирались
    /// в initial-миграции без дополнительной миграции при подключении Phase C.
    /// </para>
    /// </remarks>
    public sealed class Promotion : Entity<Guid>
    {
        #region Limits

        /// <summary>Максимальная длина <see cref="Name"/>.</summary>
        public const int MAX_NAME_LENGTH = 200;

        /// <summary>Максимальная длина <see cref="Description"/>.</summary>
        public const int MAX_DESCRIPTION_LENGTH = 2000;

        /// <summary>Максимальная длина <see cref="InternalNotes"/>.</summary>
        public const int MAX_INTERNAL_NOTES_LENGTH = 2000;

        #endregion

        #region Properties

        /// <summary>Название акции.</summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>Описание акции. Опционально.</summary>
        public string? Description { get; private set; }

        /// <summary>Начало действия акции.</summary>
        public DateTimeOffset From { get; private set; }

        /// <summary>Конец действия акции.</summary>
        public DateTimeOffset To { get; private set; }

        /// <summary>Активна ли акция (быстрое отключение независимо от даты).</summary>
        public bool IsActive { get; private set; }

        /// <summary>Служебные заметки. Не показывается клиенту.</summary>
        public string? InternalNotes { get; private set; }

        /// <summary>Дата создания. Иммутабельна.</summary>
        public DateTimeOffset CreatedAt { get; private set; }

        /// <summary>Дата последней правки.</summary>
        public DateTimeOffset UpdatedAt { get; private set; }

        #endregion

        /// <summary>Конструктор без параметров для EF Core.</summary>
        private Promotion() : base() { }
    }
}
