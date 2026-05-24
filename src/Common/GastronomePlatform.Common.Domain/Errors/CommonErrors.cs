using GastronomePlatform.Common.Domain.Results;

namespace GastronomePlatform.Common.Domain.Errors
{
    /// <summary>
    /// Общие инфраструктурные ошибки, доступные всем модулям без создания
    /// межмодульных зависимостей.
    /// </summary>
    /// <remarks>
    /// Содержит ошибки, не привязанные к конкретному предметному домену,
    /// но возникающие в типовых инфраструктурных сценариях (проблемы
    /// аутентификационного контекста, согласованности и т.п.).
    /// <para>
    /// Размещены в <c>Common.Domain</c> как часть Shared Kernel — все модули
    /// уже ссылаются на этот проект, дополнительные cross-module зависимости
    /// не создаются. Соответствует принципу дорожной карты: «межмодульные
    /// вызовы идут через абстракции и интеграционные события».
    /// </para>
    /// </remarks>
    public static class CommonErrors
    {
        /// <summary>
        /// Не удалось извлечь идентификатор пользователя из контекста запроса.
        /// Теоретический сценарий — токен прошёл валидацию подписи на уровне
        /// middleware, но <c>sub</c>-claim отсутствует или имеет невалидный формат.
        /// </summary>
        /// <remarks>
        /// Defense-in-depth для Handler-ов с <c>[Authorize]</c>. На целевой
        /// архитектуре эта проверка должна быть на уровне инфраструктуры
        /// (Filter / middleware / Policy), не в каждом Handler — см. TODO 4.6
        /// «Централизованная валидация UserId» в
        /// <c>docs/_private/private_TODO-будущие-этапы.md</c>.
        /// </remarks>
        public static readonly Error UnauthenticatedRequest =
            Error.Failure(
                "COMMON.UNAUTHENTICATED",
                "Не удалось определить пользователя из контекста запроса.");
    }
}
