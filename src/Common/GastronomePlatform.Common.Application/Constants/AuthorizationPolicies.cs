namespace GastronomePlatform.Common.Application.Constants
{
    /// <summary>
    /// Константы имён политик авторизации (Authorization Policies),
    /// используемых в атрибутах <c>[Authorize(Policy = AuthorizationPolicies.X)]</c>
    /// на эндпоинтах WebAPI и при программных проверках через
    /// <c>IAuthorizationService.AuthorizeAsync</c>.
    /// </summary>
    /// <remarks>
    /// Размещены в <c>Common.Application</c>, потому что одно и то же имя политики
    /// используется в WebAPI (на атрибутах контроллеров) и в Infrastructure
    /// (при регистрации политики через <c>AddAuthorization</c>). Хранение имени
    /// в одной константе исключает рассинхронизацию строк «политика, объявленная
    /// в DI» vs «политика, запрашиваемая на эндпоинте».
    /// <para>
    /// При переходе к микросервисам каждый сервис определяет нужные политики
    /// локально — аналогично подходу для
    /// <see cref="GastronomePlatform.Common.Domain.Constants.PlatformRoles"/>.
    /// </para>
    /// </remarks>
    public static class AuthorizationPolicies
    {
        /// <summary>
        /// Политика «валидный актор» — требует, чтобы в JWT присутствовал claim
        /// <c>sub</c>, успешно парсящийся как <see cref="System.Guid"/>.
        /// Применяется ко всем эндпоинтам, требующим аутентифицированного
        /// пользователя с корректным идентификатором, и заменяет голый
        /// <c>[Authorize]</c> + defense-in-depth проверку в Handler-ах.
        /// </summary>
        public const string VALID_ACTOR = "ValidActor";
    }
}
