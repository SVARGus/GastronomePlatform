using GastronomePlatform.Common.Domain.Primitives;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;
using GastronomePlatform.Modules.Subscriptions.Domain.Errors;

namespace GastronomePlatform.Modules.Subscriptions.Domain.Entities
{
    /// <summary>
    /// Тарифный план каталога подписок — продукт, который платформа продаёт
    /// (Premium, Business и т. п.). Хранит атрибуты продукта: имена, описание,
    /// покупочный роль-гейт (для <c>Base</c>-планов), окно доступности,
    /// а также композитную коллекцию грантов <see cref="PlanGrant"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// План — <b>не</b> корень агрегата (единственный <c>AggregateRoot</c> модуля
    /// — <see cref="UserSubscription"/>). Однако <see cref="SubscriptionPlan"/>
    /// управляет составом грантов через <see cref="SetGrants"/> — по аналогии
    /// с тем, как <c>Dish.SetTags</c> управляет тегами.
    /// </para>
    /// <para>
    /// Длительность покупки (30/90/365 дней) — атрибут офферов
    /// <see cref="PlanPrice"/>, не плана. Здесь только окно доступности продукта.
    /// </para>
    /// </remarks>
    public sealed class SubscriptionPlan : Entity<Guid>
    {
        #region Limits

        /// <summary>Минимальная длина <see cref="PublicName"/> после trim.</summary>
        public const int MIN_PUBLIC_NAME_LENGTH = 2;

        /// <summary>Максимальная длина <see cref="PublicName"/>.</summary>
        public const int MAX_PUBLIC_NAME_LENGTH = 200;

        /// <summary>Максимальная длина <see cref="TechnicalName"/> (системное имя).</summary>
        public const int MAX_TECHNICAL_NAME_LENGTH = 100;

        /// <summary>Максимальная длина <see cref="Description"/>.</summary>
        public const int MAX_DESCRIPTION_LENGTH = 2000;

        /// <summary>Максимальная длина <see cref="InternalNotes"/>.</summary>
        public const int MAX_INTERNAL_NOTES_LENGTH = 2000;

        #endregion

        // Backing field для коллекции грантов. Настраивается в SubscriptionPlanConfiguration
        // через HasField("_grants") + PropertyAccessMode.Field.
        private readonly List<PlanGrant> _grants = new();

        #region Properties

        /// <summary>
        /// Род плана: <see cref="PlanKind.Base"/> (тарифный уровень, ≤1 активной,
        /// несёт роль) или <see cref="PlanKind.AddOn"/> (докупаемая услуга параллельно
        /// Base, роль-агностична). Устанавливается в фабрике и не меняется.
        /// </summary>
        public PlanKind PlanKind { get; private set; }

        /// <summary>Публичное название для витрины («Премиум», «Бизнес»).</summary>
        public string PublicName { get; private set; } = string.Empty;

        /// <summary>
        /// Системное имя для кода/конфигов («premium»). Уникально в рамках справочника.
        /// <see langword="null"/>, если не задано.
        /// </summary>
        public string? TechnicalName { get; private set; }

        /// <summary>Публичное описание тарифа. Опционально.</summary>
        public string? Description { get; private set; }

        /// <summary>
        /// Покупочный роль-гейт (порог «не ниже роли», значение
        /// <see cref="Common.Domain.Constants.PlatformRoles"/>).
        /// <see langword="null"/> = доступен всем. Только у <see cref="PlanKind.Base"/>-планов
        /// (см. domain-model §4.7). Инвариант проверяется в <see cref="Create"/>
        /// и <see cref="UpdateCatalog"/>.
        /// </summary>
        public string? RequiredRole { get; private set; }

        /// <summary>
        /// Продукт активен в принципе. Мягкое удаление вместо физического
        /// (действующие подписки продолжают работать).
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>Продукт не предлагается до этой даты. <see langword="null"/> = сразу доступен.</summary>
        public DateTimeOffset? AvailableFrom { get; private set; }

        /// <summary>Дата снятия продукта с продажи. <see langword="null"/> = бессрочно.</summary>
        public DateTimeOffset? AvailableUntil { get; private set; }

        /// <summary>Служебные заметки маркетолога/админа. Не показывается клиенту.</summary>
        public string? InternalNotes { get; private set; }

        /// <summary>Дата создания. Иммутабельна.</summary>
        public DateTimeOffset CreatedAt { get; private set; }

        /// <summary>Дата последней правки. Обновляется при каждом Update-методе.</summary>
        public DateTimeOffset UpdatedAt { get; private set; }

        /// <summary>
        /// Состав услуг плана. Read-only коллекция; полная замена — через <see cref="SetGrants"/>.
        /// </summary>
        public IReadOnlyList<PlanGrant> Grants => _grants;

        #endregion

        #region Constructors

        /// <summary>Конструктор без параметров для EF Core.</summary>
        private SubscriptionPlan() : base() { }

        /// <summary>Приватный конструктор — используется только из <see cref="Create"/>.</summary>
        private SubscriptionPlan(
            PlanKind planKind,
            string publicName,
            string? technicalName,
            string? description,
            string? requiredRole,
            DateTimeOffset? availableFrom,
            DateTimeOffset? availableUntil,
            string? internalNotes,
            DateTimeOffset utcNow)
            : base(Guid.NewGuid())
        {
            PlanKind = planKind;
            PublicName = publicName;
            TechnicalName = technicalName;
            Description = description;
            RequiredRole = requiredRole;
            IsActive = true;
            AvailableFrom = availableFrom;
            AvailableUntil = availableUntil;
            InternalNotes = internalNotes;
            CreatedAt = utcNow;
            UpdatedAt = utcNow;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Создаёт новый тарифный план. Уникальность <paramref name="technicalName"/>
        /// и валидацию длин строк ожидается на уровне команды через FluentValidation.
        /// </summary>
        /// <param name="planKind">Род плана (Base / AddOn).</param>
        /// <param name="publicName">Публичное название для витрины.</param>
        /// <param name="technicalName">Системное имя. Опционально.</param>
        /// <param name="description">Публичное описание. Опционально.</param>
        /// <param name="requiredRole">
        /// Покупочный роль-гейт. Только для <see cref="PlanKind.Base"/> —
        /// для <see cref="PlanKind.AddOn"/> должен быть <see langword="null"/>.
        /// </param>
        /// <param name="availableFrom">Дата начала доступности. Опционально.</param>
        /// <param name="availableUntil">Дата конца доступности. Опционально.</param>
        /// <param name="internalNotes">Служебные заметки. Опционально.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result{TValue}.Success(TValue)"/> с новым планом или
        /// <see cref="SubscriptionsErrors.AddOnCannotHaveRequiredRole"/>, если
        /// у AddOn-плана задан <paramref name="requiredRole"/>.
        /// </returns>
        public static Result<SubscriptionPlan> Create(
            PlanKind planKind,
            string publicName,
            string? technicalName,
            string? description,
            string? requiredRole,
            DateTimeOffset? availableFrom,
            DateTimeOffset? availableUntil,
            string? internalNotes,
            DateTimeOffset utcNow)
        {
            if (planKind == PlanKind.AddOn && !string.IsNullOrEmpty(requiredRole))
            {
                return SubscriptionsErrors.AddOnCannotHaveRequiredRole;
            }

            return new SubscriptionPlan(
                planKind,
                publicName,
                technicalName,
                description,
                requiredRole,
                availableFrom,
                availableUntil,
                internalNotes,
                utcNow);
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// Обновляет атрибуты плана. <see cref="PlanKind"/> и состав грантов
        /// через этот метод не меняются (для грантов — <see cref="SetGrants"/>;
        /// смена <see cref="PlanKind"/> не предусмотрена).
        /// </summary>
        /// <param name="publicName">Новое публичное название.</param>
        /// <param name="technicalName">Новое системное имя. Опционально.</param>
        /// <param name="description">Новое описание. Опционально.</param>
        /// <param name="requiredRole">Новый покупочный роль-гейт. Должен быть <see langword="null"/> для AddOn.</param>
        /// <param name="availableFrom">Новая дата начала доступности.</param>
        /// <param name="availableUntil">Новая дата конца доступности.</param>
        /// <param name="internalNotes">Новые служебные заметки.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или <see cref="SubscriptionsErrors.AddOnCannotHaveRequiredRole"/>.
        /// </returns>
        public Result UpdateCatalog(
            string publicName,
            string? technicalName,
            string? description,
            string? requiredRole,
            DateTimeOffset? availableFrom,
            DateTimeOffset? availableUntil,
            string? internalNotes,
            DateTimeOffset utcNow)
        {
            if (PlanKind == PlanKind.AddOn && !string.IsNullOrEmpty(requiredRole))
            {
                return SubscriptionsErrors.AddOnCannotHaveRequiredRole;
            }

            PublicName = publicName;
            TechnicalName = technicalName;
            Description = description;
            RequiredRole = requiredRole;
            AvailableFrom = availableFrom;
            AvailableUntil = availableUntil;
            InternalNotes = internalNotes;
            UpdatedAt = utcNow;

            return Result.Success();
        }

        /// <summary>
        /// Полностью заменяет состав грантов плана (replace-семантика). Уникальность
        /// значения <see cref="FeatureGrant"/> в наборе гарантируется словарём —
        /// ключом является само значение enum.
        /// </summary>
        /// <param name="grantsWithQuota">
        /// Словарь <c>FeatureGrant → Quantity?</c>. Значение <see langword="null"/>
        /// = безлимит (или неприменимо для не-квотовых грантов).
        /// </param>
        /// <param name="utcNow">Текущее время UTC.</param>
        public void SetGrants(IReadOnlyDictionary<FeatureGrant, int?> grantsWithQuota, DateTimeOffset utcNow)
        {
            _grants.Clear();
            foreach (var kv in grantsWithQuota)
            {
                _grants.Add(new PlanGrant(Id, kv.Key, kv.Value));
            }

            UpdatedAt = utcNow;
        }

        /// <summary>Активирует план (мягкое включение).</summary>
        /// <param name="utcNow">Текущее время UTC.</param>
        public void Activate(DateTimeOffset utcNow)
        {
            IsActive = true;
            UpdatedAt = utcNow;
        }

        /// <summary>Деактивирует план (мягкое отключение). Действующие подписки продолжают работать.</summary>
        /// <param name="utcNow">Текущее время UTC.</param>
        public void Deactivate(DateTimeOffset utcNow)
        {
            IsActive = false;
            UpdatedAt = utcNow;
        }

        #endregion
    }
}
