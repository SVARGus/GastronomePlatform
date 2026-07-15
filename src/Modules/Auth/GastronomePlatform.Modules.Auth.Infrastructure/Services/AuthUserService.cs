using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Auth.Domain.Contracts;
using GastronomePlatform.Modules.Auth.Domain.Errors;
using GastronomePlatform.Modules.Auth.Infrastructure.Identity;
using GastronomePlatform.Modules.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GastronomePlatform.Modules.Auth.Infrastructure.Services
{
    /// <summary>
    /// Реализация <see cref="IAuthUserService"/> через ASP.NET Core Identity.
    /// Обеспечивает межмодульное взаимодействие — используется модулем Users
    /// для изменения учётных данных пользователя.
    /// </summary>
    public sealed class AuthUserService : IAuthUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuthDbContext _authDbContext;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="AuthUserService"/>.
        /// </summary>
        /// <param name="userManager">Менеджер пользователей ASP.NET Core Identity.</param>
        /// <param name="authDbContext">Контекст базы данных модуля Auth.</param>
        public AuthUserService(UserManager<ApplicationUser> userManager, AuthDbContext authDbContext)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _authDbContext = authDbContext ?? throw new ArgumentNullException(nameof(authDbContext));
        }

        /// <inheritdoc/>
        public async Task<Result> ChangeEmailAsync(Guid userId, string newEmail, CancellationToken cancellationToken = default)
        {
            // Проверяем наличие переданного поля newEmail
            ArgumentException.ThrowIfNullOrEmpty(newEmail);

            // Проверяем наличие пользователя по userId
            ApplicationUser? user = await _userManager.FindByIdAsync(userId.ToString());

            if (user is  null)
            {
                return AuthErrors.UserNotFound;
            }

            // Если email не изменился — это не ошибка, просто ничего не делаем
            if (user.Email == newEmail)
            {
                return Result.Success();
            }

            // Проверка уникальности среди других пользователей
            bool emailTaken = await _authDbContext.Users
                .AnyAsync(u => u.Email == newEmail && u.Id != userId, cancellationToken);

            if (emailTaken)
            {
                return AuthErrors.EmailAlreadyTaken;
            }

            await _userManager.SetEmailAsync(user, newEmail);

            return Result.Success();
        }

        /// <inheritdoc/>
        public async Task<Result> ChangePhoneAsync(Guid userId, string newPhone, CancellationToken cancellationToken = default)
        {
            // Проверяем наличие переданного поля newPhone
            ArgumentException.ThrowIfNullOrEmpty(newPhone);

            // Проверяем наличие пользователя по userId
            ApplicationUser? user = await _userManager.FindByIdAsync(userId.ToString());

            if (user is null)
            {
                return AuthErrors.UserNotFound;
            }

            // Если телефон не изменился — это не ошибка, просто ничего не делаем
            if (user.PhoneNumber == newPhone)
            {
                return Result.Success();
            }

            // Проверка уникальности среди других пользователей
            bool phoneTaken = await _authDbContext.Users
                .AnyAsync(u => u.PhoneNumber == newPhone && u.Id != userId, cancellationToken);

            if (phoneTaken)
            {
                return AuthErrors.PhoneAlreadyTaken;
            }

            await _userManager.SetPhoneNumberAsync(user, newPhone);

            return Result.Success();
        }

        /// <inheritdoc/>
        public async Task<Result> ChangeUserNameAsync(Guid userId, string newUserName, CancellationToken cancellationToken = default)
        {
            // Проверяем наличие переданного поля newUserName
            ArgumentException.ThrowIfNullOrEmpty(newUserName);

            // Проверяем наличие пользователя по userId
            ApplicationUser? user = await _userManager.FindByIdAsync(userId.ToString());

            if (user is null)
            {
                return AuthErrors.UserNotFound;
            }

            // Если никнейм не изменился — это не ошибка, просто ничего не делаем
            if (user.UserName == newUserName)
            {
                return Result.Success();
            }

            // Проверка уникальности среди других пользователей
            bool userNameTaken = await _authDbContext.Users
                .AnyAsync(u => u.UserName == newUserName && u.Id != userId, cancellationToken);

            if (userNameTaken)
            {
                return AuthErrors.UserNameAlreadyTaken;
            }

            await _userManager.SetUserNameAsync(user, newUserName);

            return Result.Success();
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
            => await _authDbContext.Users.AnyAsync(u => u.Email == email, cancellationToken);

        /// <inheritdoc/>
        public async Task<bool> ExistsByPhoneAsync(string phone, CancellationToken cancellationToken = default)
            => await _authDbContext.Users.AnyAsync(ph => ph.PhoneNumber == phone, cancellationToken);

        /// <inheritdoc/>
        public async Task<bool> ExistsByUserNameAsync(string userName, CancellationToken cancellationToken = default)
            => await _authDbContext.Users.AnyAsync(u => u.UserName == userName, cancellationToken);

        /// <inheritdoc/>
        public async Task<IReadOnlyCollection<string>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(userId.ToString());

            if (user is null)
            {
                return Array.Empty<string>();
            }

            IList<string> roles = await _userManager.GetRolesAsync(user);

            return roles.AsReadOnly();
        }

        /// <inheritdoc/>
        public async Task<Result> AddUserToRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(roleName);

            ApplicationUser? user = await _userManager.FindByIdAsync(userId.ToString());

            if (user is null)
            {
                return AuthErrors.UserNotFound;
            }

            // Проверяем существование роли до вызова AddToRoleAsync —
            // при отсутствии роли UserStore бросает InvalidOperationException.
            string normalizedRoleName = roleName.ToUpperInvariant();
            bool roleExists = await _authDbContext.Roles
                .AnyAsync(r => r.NormalizedName == normalizedRoleName, cancellationToken);

            if (!roleExists)
            {
                return AuthErrors.RoleNotFound;
            }

            // Идемпотентность: если роль уже назначена — тихий Success.
            bool alreadyInRole = await _userManager.IsInRoleAsync(user, roleName);

            if (alreadyInRole)
            {
                return Result.Success();
            }

            IdentityResult identityResult = await _userManager.AddToRoleAsync(user, roleName);

            return identityResult.Succeeded ? Result.Success() : AuthErrors.RoleAssignmentFailed;
        }

        /// <inheritdoc/>
        public async Task<Result> RemoveUserFromRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(roleName);

            ApplicationUser? user = await _userManager.FindByIdAsync(userId.ToString());

            if (user is null)
            {
                return AuthErrors.UserNotFound;
            }

            // Идемпотентность: если роли у пользователя нет — тихий Success.
            // Побочно защищает от несуществующей роли: IsInRoleAsync для отсутствующей
            // роли всегда вернёт false, и мы вернём Success без обращения к RemoveFromRoleAsync.
            bool isInRole = await _userManager.IsInRoleAsync(user, roleName);

            if (!isInRole)
            {
                return Result.Success();
            }

            IdentityResult identityResult = await _userManager.RemoveFromRoleAsync(user, roleName);

            return identityResult.Succeeded ? Result.Success() : AuthErrors.RoleAssignmentFailed;
        }
    }
}
