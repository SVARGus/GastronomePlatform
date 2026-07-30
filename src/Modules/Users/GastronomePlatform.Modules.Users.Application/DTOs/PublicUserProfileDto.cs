namespace GastronomePlatform.Modules.Users.Application.DTOs
{
    /// <summary>
    /// Публичное представление профиля пользователя — для анонимного просмотра
    /// (страница автора, подпись автора на карточке блюда).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Контактные и чувствительные данные (<c>Email</c>, <c>Phone</c>,
    /// <c>DateOfBirth</c>, <c>Gender</c>, ФИО) в публичное DTO не входят
    /// никогда — они доступны только владельцу через <c>GET /api/users/me</c>
    /// (<see cref="UserProfileDto"/>).
    /// </para>
    /// <para>
    /// Поля <see cref="Bio"/>, <see cref="Country"/>, <see cref="Region"/>,
    /// <see cref="City"/> заполняются только при публичном профиле
    /// (<see cref="IsPublic"/> = <see langword="true"/>); у скрытого профиля
    /// они равны <see langword="null"/>, но имя, аватар и дата регистрации
    /// отдаются всегда — авторство опубликованных блюд публично по построению.
    /// </para>
    /// </remarks>
    /// <param name="UserId">Идентификатор пользователя.</param>
    /// <param name="UserName">Уникальный никнейм.</param>
    /// <param name="DisplayName">Публичное отображаемое имя. Опционально.</param>
    /// <param name="AvatarMediaId">Идентификатор медиафайла аватара. Опционально.</param>
    /// <param name="IsPublic">Признак публичности профиля.</param>
    /// <param name="Bio">Описание профиля. <see langword="null"/> у скрытого профиля.</param>
    /// <param name="Country">Страна. <see langword="null"/> у скрытого профиля.</param>
    /// <param name="Region">Регион. <see langword="null"/> у скрытого профиля.</param>
    /// <param name="City">Город. <see langword="null"/> у скрытого профиля.</param>
    /// <param name="CreatedAt">Дата регистрации на платформе (UTC).</param>
    public sealed record PublicUserProfileDto(
        Guid UserId,
        string UserName,
        string? DisplayName,
        Guid? AvatarMediaId,
        bool IsPublic,
        string? Bio,
        string? Country,
        string? Region,
        string? City,
        DateTimeOffset CreatedAt);
}
