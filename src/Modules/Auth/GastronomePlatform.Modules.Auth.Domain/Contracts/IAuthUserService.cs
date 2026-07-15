using GastronomePlatform.Common.Domain.Results;

namespace GastronomePlatform.Modules.Auth.Domain.Contracts
{
    /// <summary>
    /// Публичный контракт модуля Auth для межмодульного взаимодействия.
    /// Предоставляет операции над учётными данными пользователя которые
    /// хранятся в модуле Auth (<c>auth.AspNetUsers</c>).
    /// </summary>
    /// <remarks>
    /// Используется модулем Users для:
    /// <list type="bullet">
    /// <item>Проверки уникальности при изменении email, телефона и никнейма</item>
    /// <item>Фактического изменения учётных данных в Auth</item>
    /// </list>
    /// При переходе к микросервисам — реализация заменяется на HTTP-клиент
    /// без изменений в коде модуля Users.
    /// </remarks>
    public interface IAuthUserService
    {
        #region Existence Checks

        /// <summary>
        /// Проверяет существование пользователя с указанным email.
        /// </summary>
        /// <param name="email">Адрес электронной почты.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see langword="true"/> если пользователь с таким email существует;
        /// иначе <see langword="false"/>.
        /// </returns>
        Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверяет существование пользователя с указанным номером телефона.
        /// </summary>
        /// <param name="phone">Номер телефона.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see langword="true"/> если пользователь с таким телефоном существует;
        /// иначе <see langword="false"/>.
        /// </returns>
        Task<bool> ExistsByPhoneAsync(string phone, CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверяет существование пользователя с указанным никнеймом.
        /// </summary>
        /// <param name="userName">Никнейм пользователя.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see langword="true"/> если пользователь с таким никнеймом существует;
        /// иначе <see langword="false"/>.
        /// </returns>
        Task<bool> ExistsByUserNameAsync(string userName, CancellationToken cancellationToken = default);

        #endregion

        #region Credential Changes

        /// <summary>
        /// Изменяет email пользователя в модуле Auth.
        /// Проверяет уникальность нового email перед изменением.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="newEmail">Новый адрес электронной почты.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Result.Success"/> если изменение выполнено;
        /// <c>Result.Failure</c> с ошибкой если email уже занят.
        /// </returns>
        Task<Result> ChangeEmailAsync(Guid userId, string newEmail, CancellationToken cancellationToken = default);

        /// <summary>
        /// Изменяет номер телефона пользователя в модуле Auth.
        /// Проверяет уникальность нового телефона перед изменением.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="newPhone">Новый номер телефона.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Result.Success"/> если изменение выполнено;
        /// <c>Result.Failure</c> с ошибкой если телефон уже занят.
        /// </returns>
        Task<Result> ChangePhoneAsync(Guid userId, string newPhone, CancellationToken cancellationToken = default);

        /// <summary>
        /// Изменяет никнейм пользователя в модуле Auth.
        /// Проверяет уникальность нового никнейма перед изменением.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="newUserName">Новый никнейм.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Result.Success"/> если изменение выполнено;
        /// <c>Result.Failure</c> с ошибкой если никнейм уже занят.
        /// </returns>
        Task<Result> ChangeUserNameAsync(Guid userId, string newUserName, CancellationToken cancellationToken = default);

        #endregion

        #region User Info

        /// <summary>
        /// Возвращает список ролей пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// Список ролей пользователя; пустой список если роли не назначены.
        /// </returns>
        Task<IReadOnlyCollection<string>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);

        #endregion

        #region Role Management

        /// <summary>
        /// Добавляет пользователю указанную роль. Операция идемпотентна:
        /// если роль уже назначена — возвращает <see cref="Result.Success()"/>
        /// без обращения к Identity.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="roleName">Имя роли. Должно совпадать с одной из констант
        /// <c>PlatformRoles</c>. Регистр значим.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> если роль назначена (либо уже была назначена);
        /// <c>Result.Failure</c> с <c>AuthErrors.UserNotFound</c> если пользователь не существует;
        /// <c>Result.Failure</c> с <c>AuthErrors.RoleNotFound</c> если роль отсутствует в системе;
        /// <c>Result.Failure</c> с <c>AuthErrors.RoleAssignmentFailed</c> при иной ошибке Identity.
        /// </returns>
        Task<Result> AddUserToRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Снимает с пользователя указанную роль. Операция идемпотентна:
        /// если роли у пользователя нет — возвращает <see cref="Result.Success()"/>
        /// без обращения к Identity.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="roleName">Имя роли. Должно совпадать с одной из констант
        /// <c>PlatformRoles</c>. Регистр значим.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Result.Success()"/> если роль снята (либо её не было);
        /// <c>Result.Failure</c> с <c>AuthErrors.UserNotFound</c> если пользователь не существует;
        /// <c>Result.Failure</c> с <c>AuthErrors.RoleNotFound</c> если роль отсутствует в системе;
        /// <c>Result.Failure</c> с <c>AuthErrors.RoleAssignmentFailed</c> при иной ошибке Identity.
        /// </returns>
        Task<Result> RemoveUserFromRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default);

        #endregion
    }
}
