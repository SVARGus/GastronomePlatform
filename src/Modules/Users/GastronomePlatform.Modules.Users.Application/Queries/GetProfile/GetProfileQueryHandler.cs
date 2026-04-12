using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Users.Application.DTOs;
using GastronomePlatform.Modules.Users.Domain.Entities;
using GastronomePlatform.Modules.Users.Domain.Errors;
using GastronomePlatform.Modules.Users.Domain.Repositories;

namespace GastronomePlatform.Modules.Users.Application.Queries.GetProfile
{
    /// <summary>
    /// Обработчик запроса профиля пользователя.
    /// </summary>
    public sealed class GetProfileQueryHandler : IQueryHandler<GetProfileQuery, UserProfileDto>
    {
        private readonly IUserProfileRepository _repository;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetProfileQueryHandler"/>
        /// </summary>
        /// <param name="repository">Репозиторий профиля пользователя</param>
        public GetProfileQueryHandler(IUserProfileRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <inheritdoc/>
        public async Task<Result<UserProfileDto>> Handle(GetProfileQuery request, CancellationToken cancellationToken)
        {
            UserProfile? profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);

            if (profile is null)
            {
                return UsersErrors.ProfileNotFound;
            }

            UserProfileDto dto = new(
                UserId: profile.UserId,
                Email: profile.Email,
                UserName: profile.UserName,
                IsPublic: profile.IsPublic,
                Phone: profile.Phone,
                FirstName: profile.FirstName,
                LastName: profile.LastName,
                MiddleName: profile.MiddleName,
                DisplayName: profile.DisplayName,
                Bio: profile.Bio,
                Gender: profile.Gender?.ToString(), // enum → string
                DateOfBirth: profile.DateOfBirth,
                AvatarMediaId: profile.AvatarMediaId,
                Country: profile.Country,
                Region: profile.Region,
                City: profile.City,
                CreatedAt: profile.CreatedAt);

            return dto;
        }
    }
}
