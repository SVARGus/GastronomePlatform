using GastronomePlatform.Common.Domain.Primitives;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;
using GastronomePlatform.Modules.Subscriptions.Domain.Errors;

namespace GastronomePlatform.Modules.Subscriptions.Domain.Entities
{
    /// <summary>
    /// Оффер каталога (SKU) — конкретное тарифное предложение внутри плана:
    /// сумма, длительность, флаги, ссылки на офферы продления и понижения.
    /// Каждая ступень градации (триал, интро, базовый, retention, удешевлённый)
    /// — отдельная строка <see cref="PlanPrice"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Инварианты, проверяемые Domain:
    /// </para>
    /// <list type="bullet">
    ///   <item><see cref="Amount"/> ≥ 0.</item>
    ///   <item><see cref="Kind"/> = <see cref="OfferKind.Trial"/> ⇒ <see cref="Amount"/> = 0 и <see cref="TrialDays"/>.HasValue.</item>
    ///   <item>
    ///     <see cref="IsRecurring"/> = <see langword="false"/> ⇒
    ///     <see cref="RenewsAsPriceId"/> и <see cref="FallbackPriceId"/> равны <see langword="null"/>
    ///     (переходить некуда).
    ///   </item>
    /// </list>
    /// <para>
    /// Инварианты цепочек офферов (<c>RenewsAs</c>/<c>Fallback</c> на оффер того же плана,
    /// отсутствие циклов) проверяются на уровне Application — FK-сравнение и обход цепочки
    /// CHECK-ом и Domain-инвариантом не выразить (см. domain-model §9).
    /// </para>
    /// </remarks>
    public sealed class PlanPrice : Entity<Guid>
    {
        #region Limits

        /// <summary>Максимальная длина <see cref="PublicName"/>.</summary>
        public const int MAX_PUBLIC_NAME_LENGTH = 200;

        /// <summary>Максимальная длина <see cref="InternalNotes"/>.</summary>
        public const int MAX_INTERNAL_NOTES_LENGTH = 2000;

        /// <summary>Длина кода валюты (ISO 4217 — 3 буквы).</summary>
        public const int CURRENCY_LENGTH = 3;

        #endregion

        #region Properties

        /// <summary>FK на <see cref="SubscriptionPlan"/>. <c>OnDelete: Restrict</c>.</summary>
        public Guid PlanId { get; private set; }

        /// <summary>Природа оффера (Trial / Intro / Standard / Retention / DunningFallback).</summary>
        public OfferKind Kind { get; private set; }

        /// <summary>Витринное имя оффера («Год со скидкой 25%»). Опционально.</summary>
        public string? PublicName { get; private set; }

        /// <summary>
        /// Длительность периода в днях. <see langword="null"/> = бессрочный
        /// («навсегда», для будущих lifetime-офферов).
        /// </summary>
        public int? DurationDays { get; private set; }

        /// <summary>Код валюты (ISO 4217), например <c>"RUB"</c>.</summary>
        public string Currency { get; private set; } = string.Empty;

        /// <summary>Фактически списываемая сумма за период — источник правды для биллинга.</summary>
        public decimal Amount { get; private set; }

        /// <summary>«Старая цена» для отображения зачёркнутой на витрине. Опционально.</summary>
        public decimal? CompareAtAmount { get; private set; }

        /// <summary>Метаданные скидки (%) для отчётов/витрины; биллинг считает по <see cref="Amount"/>.</summary>
        public int? DiscountPercent { get; private set; }

        /// <summary>Дней бесплатного периода. Обязателен для <see cref="OfferKind.Trial"/>.</summary>
        public int? TrialDays { get; private set; }

        /// <summary>
        /// Поддерживает ли оффер автопродление. <see langword="false"/> = разовая покупка
        /// без переходов <see cref="RenewsAsPriceId"/>/<see cref="FallbackPriceId"/>.
        /// </summary>
        public bool IsRecurring { get; private set; }

        /// <summary>
        /// Можно ли купить напрямую. <see langword="false"/> для
        /// <see cref="OfferKind.Retention"/> и <see cref="OfferKind.DunningFallback"/> —
        /// они назначаются системой, а не выбираются пользователем.
        /// </summary>
        public bool IsPurchasable { get; private set; }

        /// <summary>
        /// На успешном продлении перейти на этот оффер. <see langword="null"/> = продлевается собой.
        /// Self-FK, <c>Restrict</c>. Инварианты цепочки — Application.
        /// </summary>
        public Guid? RenewsAsPriceId { get; private set; }

        /// <summary>
        /// При провале списаний понизить на этот оффер (dunning-лестница).
        /// <see langword="null"/> = терминал, отключить рекуррент. Self-FK, <c>Restrict</c>.
        /// </summary>
        public Guid? FallbackPriceId { get; private set; }

        /// <summary>Оффер не покупается до этой даты. Опционально.</summary>
        public DateTimeOffset? AvailableFrom { get; private set; }

        /// <summary>Оффер не покупается после этой даты. <see langword="null"/> = бессрочно.</summary>
        public DateTimeOffset? AvailableUntil { get; private set; }

        /// <summary>Мягкое отключение оффера. Активные подписки, купленные раньше, продолжают действовать.</summary>
        public bool IsActive { get; private set; }

        /// <summary>Служебные заметки. Не показывается клиенту.</summary>
        public string? InternalNotes { get; private set; }

        /// <summary>Дата создания. Иммутабельна.</summary>
        public DateTimeOffset CreatedAt { get; private set; }

        /// <summary>Дата последней правки.</summary>
        public DateTimeOffset UpdatedAt { get; private set; }

        #endregion

        #region Constructors

        /// <summary>Конструктор без параметров для EF Core.</summary>
        private PlanPrice() : base() { }

        /// <summary>Приватный конструктор — используется только из <see cref="Create"/>.</summary>
        private PlanPrice(
            Guid planId,
            OfferKind kind,
            string? publicName,
            int? durationDays,
            string currency,
            decimal amount,
            decimal? compareAtAmount,
            int? discountPercent,
            int? trialDays,
            bool isRecurring,
            bool isPurchasable,
            Guid? renewsAsPriceId,
            Guid? fallbackPriceId,
            DateTimeOffset? availableFrom,
            DateTimeOffset? availableUntil,
            string? internalNotes,
            DateTimeOffset utcNow)
            : base(Guid.NewGuid())
        {
            PlanId = planId;
            Kind = kind;
            PublicName = publicName;
            DurationDays = durationDays;
            Currency = currency;
            Amount = amount;
            CompareAtAmount = compareAtAmount;
            DiscountPercent = discountPercent;
            TrialDays = trialDays;
            IsRecurring = isRecurring;
            IsPurchasable = isPurchasable;
            RenewsAsPriceId = renewsAsPriceId;
            FallbackPriceId = fallbackPriceId;
            AvailableFrom = availableFrom;
            AvailableUntil = availableUntil;
            IsActive = true;
            InternalNotes = internalNotes;
            CreatedAt = utcNow;
            UpdatedAt = utcNow;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Создаёт новый оффер. Проверяет внутриполевые инварианты
        /// (см. remarks класса). Инварианты цепочек (той же <see cref="PlanId"/>,
        /// отсутствия циклов) остаются на Application.
        /// </summary>
        /// <param name="planId">Идентификатор плана-владельца оффера.</param>
        /// <param name="kind">Природа оффера.</param>
        /// <param name="publicName">Витринное имя. Опционально.</param>
        /// <param name="durationDays">Длительность периода в днях. <see langword="null"/> = бессрочный.</param>
        /// <param name="currency">Код валюты (ISO 4217).</param>
        /// <param name="amount">Сумма списания за период.</param>
        /// <param name="compareAtAmount">«Старая цена» для витрины. Опционально.</param>
        /// <param name="discountPercent">Скидка в процентах для витрины. Опционально.</param>
        /// <param name="trialDays">Дней триала. Обязателен для <see cref="OfferKind.Trial"/>.</param>
        /// <param name="isRecurring">Поддерживает ли автопродление.</param>
        /// <param name="isPurchasable">Можно ли купить напрямую.</param>
        /// <param name="renewsAsPriceId">Оффер продления. Нельзя задать при <paramref name="isRecurring"/>=false.</param>
        /// <param name="fallbackPriceId">Оффер понижения. Нельзя задать при <paramref name="isRecurring"/>=false.</param>
        /// <param name="availableFrom">Начало окна доступности. Опционально.</param>
        /// <param name="availableUntil">Конец окна доступности. Опционально.</param>
        /// <param name="internalNotes">Служебные заметки. Опционально.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result{TValue}.Success(TValue)"/> с новым оффером или одна из ошибок:
        /// <see cref="SubscriptionsErrors.PriceNegativeAmount"/>,
        /// <see cref="SubscriptionsErrors.PriceTrialRequiresFreeWithDays"/>,
        /// <see cref="SubscriptionsErrors.PriceNonRecurringCannotTransition"/>.
        /// </returns>
        public static Result<PlanPrice> Create(
            Guid planId,
            OfferKind kind,
            string? publicName,
            int? durationDays,
            string currency,
            decimal amount,
            decimal? compareAtAmount,
            int? discountPercent,
            int? trialDays,
            bool isRecurring,
            bool isPurchasable,
            Guid? renewsAsPriceId,
            Guid? fallbackPriceId,
            DateTimeOffset? availableFrom,
            DateTimeOffset? availableUntil,
            string? internalNotes,
            DateTimeOffset utcNow)
        {
            var invariants = CheckInvariants(kind, amount, trialDays, isRecurring, renewsAsPriceId, fallbackPriceId);
            if (invariants.IsFailure)
            {
                return Result<PlanPrice>.Failure(invariants.Error);
            }

            return new PlanPrice(
                planId,
                kind,
                publicName,
                durationDays,
                currency,
                amount,
                compareAtAmount,
                discountPercent,
                trialDays,
                isRecurring,
                isPurchasable,
                renewsAsPriceId,
                fallbackPriceId,
                availableFrom,
                availableUntil,
                internalNotes,
                utcNow);
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// Обновляет все изменяемые поля оффера. <see cref="PlanId"/> и <see cref="Kind"/>
        /// не меняются (миграция между планами/типами = новый оффер).
        /// </summary>
        /// <param name="publicName">Новое витринное имя.</param>
        /// <param name="durationDays">Новая длительность периода.</param>
        /// <param name="amount">Новая сумма списания.</param>
        /// <param name="compareAtAmount">Новая «старая цена» для витрины.</param>
        /// <param name="discountPercent">Новая скидка (%).</param>
        /// <param name="trialDays">Новое количество дней триала.</param>
        /// <param name="isRecurring">Новое значение флага автопродления.</param>
        /// <param name="isPurchasable">Новое значение флага прямой покупки.</param>
        /// <param name="renewsAsPriceId">Новый оффер продления.</param>
        /// <param name="fallbackPriceId">Новый оффер понижения.</param>
        /// <param name="availableFrom">Новое начало окна доступности.</param>
        /// <param name="availableUntil">Новое конец окна доступности.</param>
        /// <param name="internalNotes">Новые служебные заметки.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> или одна из ошибок инвариантов
        /// (см. <see cref="Create"/>).
        /// </returns>
        public Result UpdateOffer(
            string? publicName,
            int? durationDays,
            decimal amount,
            decimal? compareAtAmount,
            int? discountPercent,
            int? trialDays,
            bool isRecurring,
            bool isPurchasable,
            Guid? renewsAsPriceId,
            Guid? fallbackPriceId,
            DateTimeOffset? availableFrom,
            DateTimeOffset? availableUntil,
            string? internalNotes,
            DateTimeOffset utcNow)
        {
            var invariants = CheckInvariants(Kind, amount, trialDays, isRecurring, renewsAsPriceId, fallbackPriceId);
            if (invariants.IsFailure)
            {
                return invariants;
            }

            PublicName = publicName;
            DurationDays = durationDays;
            Amount = amount;
            CompareAtAmount = compareAtAmount;
            DiscountPercent = discountPercent;
            TrialDays = trialDays;
            IsRecurring = isRecurring;
            IsPurchasable = isPurchasable;
            RenewsAsPriceId = renewsAsPriceId;
            FallbackPriceId = fallbackPriceId;
            AvailableFrom = availableFrom;
            AvailableUntil = availableUntil;
            InternalNotes = internalNotes;
            UpdatedAt = utcNow;

            return Result.Success();
        }

        /// <summary>Активирует оффер (мягкое включение).</summary>
        /// <param name="utcNow">Текущее время UTC.</param>
        public void Activate(DateTimeOffset utcNow)
        {
            IsActive = true;
            UpdatedAt = utcNow;
        }

        /// <summary>Деактивирует оффер. Действующие подписки, купленные раньше, продолжают работать.</summary>
        /// <param name="utcNow">Текущее время UTC.</param>
        public void Deactivate(DateTimeOffset utcNow)
        {
            IsActive = false;
            UpdatedAt = utcNow;
        }

        #endregion

        /// <summary>
        /// Проверяет внутренние инварианты оффера. Общий источник для
        /// <see cref="Create"/> и <see cref="UpdateOffer"/>.
        /// </summary>
        private static Result CheckInvariants(
            OfferKind kind,
            decimal amount,
            int? trialDays,
            bool isRecurring,
            Guid? renewsAsPriceId,
            Guid? fallbackPriceId)
        {
            if (amount < 0)
            {
                return SubscriptionsErrors.PriceNegativeAmount;
            }

            if (kind == OfferKind.Trial && (amount != 0m || !trialDays.HasValue))
            {
                return SubscriptionsErrors.PriceTrialRequiresFreeWithDays;
            }

            if (!isRecurring && (renewsAsPriceId.HasValue || fallbackPriceId.HasValue))
            {
                return SubscriptionsErrors.PriceNonRecurringCannotTransition;
            }

            return Result.Success();
        }
    }
}
