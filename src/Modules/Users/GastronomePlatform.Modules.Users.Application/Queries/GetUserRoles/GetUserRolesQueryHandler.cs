using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Auth.Domain.Contracts;
using GastronomePlatform.Modules.Users.Domain.Entities;
using GastronomePlatform.Modules.Users.Domain.Errors;
using GastronomePlatform.Modules.Users.Domain.Repositories;

namespace GastronomePlatform.Modules.Users.Application.Queries.GetUserRoles
{
    /// <summary>
    /// Обработчик запроса роли пользователя.
    /// </summary>
    public sealed class GetUserRolesQueryHandler : IQueryHandler<GetUserRolesQuery, IReadOnlyCollection<string>>
    {
        private readonly IAuthUserService _authUserService;
        private readonly IUserProfileRepository _repository;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetUserRolesQueryHandler"/>.
        /// </summary>
        /// <param name="authUserService">Сервис пользователя авторизации.</param>
        /// <param name="repository">Репозиторий профиля пользователя.</param>
        public GetUserRolesQueryHandler(IAuthUserService authUserService, IUserProfileRepository repository)
        {
            _authUserService = authUserService ?? throw new ArgumentNullException(nameof(authUserService));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <inheritdoc/>
        public async Task<Result<IReadOnlyCollection<string>>> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
        {
            UserProfile? profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);

            if (profile is null)
            {
                return UsersErrors.ProfileNotFound;
            }

            IReadOnlyCollection<string> roles = await _authUserService.GetUserRolesAsync(profile.UserId, cancellationToken);

            return Result<IReadOnlyCollection<string>>.Success(roles);
        }
    }
}
