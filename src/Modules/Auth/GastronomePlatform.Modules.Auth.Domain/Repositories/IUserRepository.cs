using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Auth.Domain.Entities;

namespace GastronomePlatform.Modules.Auth.Domain.Repositories
{
    /// <summary>
    /// Репозиторий для работы с пользователями.
    /// Абстракция над ASP.NET Core Identity — Domain не знает о деталях реализации.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Проверяет существование пользователя с указанным email.
        /// Используется при регистрации для проверки уникальности.
        /// </summary>
        /// <param name="email">Адрес электронной почты.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see langword="true"/> если пользователь с таким email уже существует;
        /// иначе <see langword="false"/>.
        /// </returns>
        Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверяет существование пользователя с указанным номером телефона.
        /// Используется при регистрации для проверки уникальности.
        /// </summary>
        /// <param name="phone">Номер телефона.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see langword="true"/> если пользователь с таким телефоном уже существует;
        /// иначе <see langword="false"/>.
        /// </returns>
        Task<bool> ExistsByPhoneAsync(string phone, CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверяет существование пользователя с указанным никнеймом.
        /// Используется при регистрации для проверки уникальности.
        /// </summary>
        /// <param name="userName">Никнейм пользователя.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see langword="true"/> если пользователь с таким никнеймом уже существует;
        /// иначе <see langword="false"/>.
        /// </returns>
        Task<bool> ExistsByUserNameAsync(string userName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Находит идентификатор пользователя по логину.
        /// Логином может быть email или номер телефона.
        /// </summary>
        /// <param name="login">UserName или Email или номер телефона пользователя.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="AuthUserInfo"/> если пользователь найден; иначе <see langword="null"/>.
        /// </returns>
        Task<AuthUserInfo?> FindByLoginAsync(string login, CancellationToken cancellationToken = default);

        /// <summary>
        /// Создаёт нового пользователя в системе.
        /// </summary>
        /// <param name="email">Адрес электронной почты.</param>
        /// <param name="userName">Никнейм пользователя.</param>
        /// <param name="password">Пароль в открытом виде (хэширование выполняет Identity).</param>
        /// <param name="phone">Номер телефона (опционально).</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="Result{Guid}"/> с идентификатором созданного пользователя
        /// или ошибкой если создание не удалось.
        /// </returns>
        Task<Result<Guid>> CreateAsync(string email, string userName, string password, string? phone, CancellationToken cancellationToken = default);

        /// <summary>
        /// Проверяет пароль пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="password">Пароль в открытом виде.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see langword="true"/> если пароль верный;
        /// иначе <see langword="false"/>.
        /// </returns>
        Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default);

        /// <summary>
        /// Возвращает первую роль пользователя.
        /// Возвращает null если роли не назначены.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// Название роли если найдена; иначе <see langword="null"/>.
        /// </returns>
        Task<string?> GetUserRoleAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Находит данные пользователя по идентификатору.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>
        /// <see cref="AuthUserInfo"/> если пользователь найден; иначе <see langword="null"/>.
        /// </returns>
        Task<AuthUserInfo?> GetAuthUserInfoByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
