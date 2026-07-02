using GastronomePlatform.Common.Domain.Primitives;
using GastronomePlatform.Modules.Subscriptions.Domain.Enums;

namespace GastronomePlatform.Modules.Subscriptions.Domain.Entities
{
    /// <summary>
    /// Иммутабельный снепшот условий оферты (договора) подписки. Хранит
    /// зафиксированный слепок того, «на что именно соглашался клиент»
    /// на момент версии.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Таблица <b>append-only</b>: строки только добавляются, никогда не правятся
    /// и не удаляются. Каждая версия — уникальная пара <c>(SubscriptionId, Version)</c>.
    /// Новая версия создаётся только при <b>материальном изменении условий</b>:
    /// <see cref="AgreementChangeType.PriceChange"/>, <see cref="AgreementChangeType.PlanChange"/>,
    /// <see cref="AgreementChangeType.Downgrade"/>. Обычное автопродление с неизменной
    /// ценой версию не плодит.
    /// </para>
    /// <para>
    /// Согласие (сценарий B): здесь фиксируется «что принято»
    /// (<see cref="TermsSnapshot"/>, <see cref="ContentHash"/>) + якорь акта
    /// (<see cref="AcceptedAt"/>, <see cref="Version"/>). Полный контекст акта
    /// (IP, User-Agent, метод, 3-D Secure) — событием в бизнес-лог.
    /// </para>
    /// <para>
    /// Создание — <c>internal</c>: агремент рождается только из lifecycle-методов
    /// <see cref="UserSubscription"/>.
    /// </para>
    /// </remarks>
    public sealed class SubscriptionAgreement : Entity<Guid>
    {
        #region Limits

        /// <summary>Максимальная длина <see cref="DocumentNumber"/>.</summary>
        public const int MAX_DOCUMENT_NUMBER_LENGTH = 200;

        /// <summary>Длина <see cref="ContentHash"/> (SHA-256 hex — 64 символа).</summary>
        public const int CONTENT_HASH_LENGTH = 64;

        #endregion

        #region Properties

        /// <summary>FK на <see cref="UserSubscription"/>. <c>OnDelete: Cascade</c>.</summary>
        public Guid SubscriptionId { get; private set; }

        /// <summary>
        /// Порядковый номер версии оферты (1 = первичная). UNIQUE с <see cref="SubscriptionId"/>.
        /// </summary>
        public int Version { get; private set; }

        /// <summary>Причина создания версии (Initial / PriceChange / PlanChange / Downgrade).</summary>
        public AgreementChangeType ChangeType { get; private set; }

        /// <summary>
        /// Полный слепок условий в формате jsonb: план, гранты, длительность,
        /// сумма, валюта, скидка, период. Задаётся Application-слоем при сборке снепшота.
        /// </summary>
        public string TermsSnapshot { get; private set; } = string.Empty;

        /// <summary>Человекочитаемый номер договора/оферты. Опционально.</summary>
        public string? DocumentNumber { get; private set; }

        /// <summary>Хеш снепшота (SHA-256 hex) для проверки целостности. Опционально.</summary>
        public string? ContentHash { get; private set; }

        /// <summary>
        /// Якорь акта согласия — когда пользователь принял эту версию условий.
        /// <see langword="null"/> = версия применена системно без явного акта
        /// (заранее раскрытое продление, автопонижение).
        /// </summary>
        public DateTimeOffset? AcceptedAt { get; private set; }

        /// <summary>С какого момента действует эта версия условий.</summary>
        public DateTimeOffset EffectiveAt { get; private set; }

        /// <summary>Момент фиксации записи. Иммутабелен.</summary>
        public DateTimeOffset CreatedAt { get; private set; }

        #endregion

        #region Constructors

        /// <summary>Конструктор без параметров для EF Core.</summary>
        private SubscriptionAgreement() : base() { }

        /// <summary>Приватный конструктор — используется только из <see cref="Create"/>.</summary>
        private SubscriptionAgreement(
            Guid subscriptionId,
            int version,
            AgreementChangeType changeType,
            string termsSnapshot,
            string? documentNumber,
            string? contentHash,
            DateTimeOffset? acceptedAt,
            DateTimeOffset effectiveAt,
            DateTimeOffset utcNow)
            : base(Guid.NewGuid())
        {
            SubscriptionId = subscriptionId;
            Version = version;
            ChangeType = changeType;
            TermsSnapshot = termsSnapshot;
            DocumentNumber = documentNumber;
            ContentHash = contentHash;
            AcceptedAt = acceptedAt;
            EffectiveAt = effectiveAt;
            CreatedAt = utcNow;
        }

        #endregion

        /// <summary>
        /// Создаёт иммутабельный снепшот оферты. Вызывается только из <see cref="UserSubscription"/>.
        /// </summary>
        /// <param name="subscriptionId">Идентификатор подписки-владельца.</param>
        /// <param name="version">Порядковый номер версии (1 для <see cref="AgreementChangeType.Initial"/>).</param>
        /// <param name="changeType">Причина создания версии.</param>
        /// <param name="termsSnapshot">Полный слепок условий (jsonb) от Application-слоя.</param>
        /// <param name="documentNumber">Человекочитаемый номер договора. Опционально.</param>
        /// <param name="contentHash">Хеш снепшота для проверки целостности. Опционально.</param>
        /// <param name="acceptedAt">Момент явного акта согласия. Опционально.</param>
        /// <param name="effectiveAt">С какого момента действует версия.</param>
        /// <param name="utcNow">Текущее время UTC.</param>
        /// <returns>Новая иммутабельная запись <see cref="SubscriptionAgreement"/>.</returns>
        internal static SubscriptionAgreement Create(
            Guid subscriptionId,
            int version,
            AgreementChangeType changeType,
            string termsSnapshot,
            string? documentNumber,
            string? contentHash,
            DateTimeOffset? acceptedAt,
            DateTimeOffset effectiveAt,
            DateTimeOffset utcNow)
        {
            return new SubscriptionAgreement(
                subscriptionId,
                version,
                changeType,
                termsSnapshot,
                documentNumber,
                contentHash,
                acceptedAt,
                effectiveAt,
                utcNow);
        }
    }
}
