using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Users.Domain.Enums;

namespace GastronomePlatform.Modules.Users.Application.Commands.UpdatePersonalInfo
{
    /// <summary>
    /// Команда обновления персональных данных профиля пользователя.
    /// </summary>
    /// <param name="UserId">Идентификатор пользователя.</param>
    /// <param name="FirstName">Имя.</param>
    /// <param name="LastName">Фамилия.</param>
    /// <param name="MiddleName">Отчество.</param>
    /// <param name="DisplayName">Публичное отображаемое имя.</param>
    /// <param name="Bio">Описание профиля.</param>
    /// <param name="Gender">Пол пользователя.</param>
    /// <param name="DateOfBirth">Дата рождения.</param>
    public sealed record UpdatePersonalInfoCommand(
        Guid UserId,
        string? FirstName,
        string? LastName,
        string? MiddleName,
        string? DisplayName,
        string? Bio,
        Gender? Gender,
        DateOnly? DateOfBirth) : ICommand;
}
