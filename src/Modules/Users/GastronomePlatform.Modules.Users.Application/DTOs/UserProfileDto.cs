namespace GastronomePlatform.Modules.Users.Application.DTOs
{
    /// <summary>
    /// Данные профиля пользователя для отображения в интерфейсе.
    /// </summary>
    /// <param name="UserId">Идентификатор пользователя.</param>
    /// <param name="Email">Адрес электронной почты.</param>
    /// <param name="UserName">Уникальный никнейм.</param>
    /// <param name="IsPublic">Признак публичности профиля.</param>
    /// <param name="Phone">Номер телефона (опционально).</param>
    /// <param name="FirstName">Имя (опционально).</param>
    /// <param name="LastName">Фамилия (опционально).</param>
    /// <param name="MiddleName">Отчество (опционально).</param>
    /// <param name="DisplayName">Публичное отображаемое имя (опционально).</param>
    /// <param name="Bio">Описание профиля (опционально).</param>
    /// <param name="Gender">Пол пользователя в виде строки (опционально).</param>
    /// <param name="DateOfBirth">Дата рождения (опционально).</param>
    /// <param name="AvatarMediaId">Идентификатор медиафайла аватара (опционально).</param>
    /// <param name="Country">Страна проживания (опционально).</param>
    /// <param name="Region">Регион проживания (опционально).</param>
    /// <param name="City">Город проживания (опционально).</param>
    /// <param name="CreatedAt">Дата регистрации на платформе (UTC).</param>
    public sealed record UserProfileDto(
        Guid UserId,
        string Email,
        string UserName,
        bool IsPublic,
        string? Phone,
        string? FirstName,
        string? LastName,
        string? MiddleName,
        string? DisplayName,
        string? Bio,
        string? Gender,
        DateOnly? DateOfBirth,
        Guid? AvatarMediaId,
        string? Country,
        string? Region,
        string? City,
        DateTimeOffset CreatedAt);
}
