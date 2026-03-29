using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Auth.Domain.Entities;
using GastronomePlatform.Modules.Auth.Domain.Errors;
using GastronomePlatform.Modules.Auth.Domain.Repositories;
using GastronomePlatform.Modules.Auth.Infrastructure.Identity;
using GastronomePlatform.Modules.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GastronomePlatform.Modules.Auth.Infrastructure.Repositories
{
    /// <summary>
    /// Репозиторий для работы с User через EF Core.
    /// </summary>
    public sealed class UserRepository : IUserRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuthDbContext _authDbContext;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UserRepository"/>.
        /// </summary>
        /// <param name="userManager">Менеджер пользователей ASP.NET Core Identity.</param>
        /// <param name="authDbContext">Контекст базы данных модуля Auth.</param>
        public UserRepository(UserManager<ApplicationUser> userManager, AuthDbContext authDbContext)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _authDbContext = authDbContext ?? throw new ArgumentNullException(nameof(authDbContext));
        }

        /// <inheritdoc/>
        public async Task<Result<Guid>> CreateAsync(string email, string userName,
            string password, string? phone, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(email);
            ArgumentException.ThrowIfNullOrEmpty(userName);
            ArgumentException.ThrowIfNullOrEmpty(password);

            var user = new ApplicationUser
            {
                Email = email,
                UserName = userName,
                PhoneNumber = phone,
                CreatedAt = DateTimeOffset.UtcNow,
                EmailConfirmed = false,
                IsDeactivated = false
            };

            IdentityResult result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                return AuthErrors.RegistrationFailed;
            }

            // Назначаем роль User по умолчанию
            await _userManager.AddToRoleAsync(user, PlatformRoles.USER);

            return user.Id;
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
        public async Task<AuthUserInfo?> FindByLoginAsync(string login, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(login);

            // Поиск по email
            AuthUserInfo? info = await _authDbContext.Users
                .Where(u => u.Email == login)
                .Select(u => new AuthUserInfo(u.Id, u.Email!))
                .FirstOrDefaultAsync(cancellationToken);

            if (info is not null)
                return info;

            // Поиск по никнейму
            info = await _authDbContext.Users
                .Where(u => u.UserName == login)
                .Select(u => new AuthUserInfo(u.Id, u.Email!))
                .FirstOrDefaultAsync(cancellationToken);

            if (info is not null)
                return info;

            // Поиск по телефону
            return await _authDbContext.Users
                .Where(u => u.PhoneNumber == login)
                .Select(u => new AuthUserInfo(u.Id, u.Email!))
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(userId.ToString());

            if (user is null)
            {
                return false;
            }

            return await _userManager.CheckPasswordAsync(user, password);
        }

        /// <inheritdoc/>
        public async Task<string?> GetUserRoleAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            ApplicationUser? user = await _userManager.FindByIdAsync(userId.ToString());

            if (user is null)
            {
                return null;
            }

            IList<string> roles = await _userManager.GetRolesAsync(user);

            // Возвращаем первую роль — у пользователя одна роль в нашей системе
            return roles.FirstOrDefault();
        }

        /// <inheritdoc/>
        public async Task<AuthUserInfo?> GetAuthUserInfoByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _authDbContext.Users
                .Where(u => u.Id == userId)
                .Select(u => new AuthUserInfo(u.Id, u.Email!))
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
