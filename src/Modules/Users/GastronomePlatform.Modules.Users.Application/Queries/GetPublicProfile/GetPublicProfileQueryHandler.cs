using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Users.Application.DTOs;
using GastronomePlatform.Modules.Users.Domain.Entities;
using GastronomePlatform.Modules.Users.Domain.Errors;
using GastronomePlatform.Modules.Users.Domain.Repositories;

namespace GastronomePlatform.Modules.Users.Application.Queries.GetPublicProfile
{
    /// <summary>
    /// Обработчик запроса публичного профиля пользователя.
    /// </summary>
    /// <remarks>
    /// Гейт приватности применяется на маппинге: у скрытого профиля
    /// (<c>IsPublic = false</c>) поля <c>Bio</c> и местоположение обнуляются;
    /// имя, аватар и дата регистрации отдаются всегда — авторство
    /// опубликованных блюд публично по построению. Контактные данные
    /// (Email/Phone) и чувствительные (DateOfBirth/Gender/ФИО) в публичное
    /// DTO не попадают ни при каком значении флага.
    /// </remarks>
    public sealed class GetPublicProfileQueryHandler
        : IQueryHandler<GetPublicProfileQuery, PublicUserProfileDto>
    {
        private readonly IUserProfileRepository _repository;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetPublicProfileQueryHandler"/>.
        /// </summary>
        /// <param name="repository">Репозиторий профилей пользователей.</param>
        public GetPublicProfileQueryHandler(IUserProfileRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <inheritdoc/>
        public async Task<Result<PublicUserProfileDto>> Handle(
            GetPublicProfileQuery request,
            CancellationToken cancellationToken)
        {
            UserProfile? profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);

            if (profile is null)
            {
                return UsersErrors.ProfileNotFound;
            }

            bool isPublic = profile.IsPublic;

            PublicUserProfileDto dto = new(
                UserId:        profile.UserId,
                UserName:      profile.UserName,
                DisplayName:   profile.DisplayName,
                AvatarMediaId: profile.AvatarMediaId,
                IsPublic:      isPublic,
                Bio:           isPublic ? profile.Bio : null,
                Country:       isPublic ? profile.Country : null,
                Region:        isPublic ? profile.Region : null,
                City:          isPublic ? profile.City : null,
                CreatedAt:     profile.CreatedAt);

            return dto;
        }
    }
}
