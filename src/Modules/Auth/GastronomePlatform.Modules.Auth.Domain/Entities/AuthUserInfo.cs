namespace GastronomePlatform.Modules.Auth.Domain.Entities
{
    /// <summary>
    /// Минимальные данные пользователя для процесса аутентификации.
    /// </summary>
    /// <param name="Id">Идентификатор пользователя.</param>
    /// <param name="Email">Адрес электронной почты.</param>
    public sealed record AuthUserInfo(Guid Id, string Email);
}
