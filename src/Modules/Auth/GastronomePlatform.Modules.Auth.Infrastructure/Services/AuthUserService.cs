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
                return AuthErrors.PhonelAlreadyTaken;
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
    }
}
