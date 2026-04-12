using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Users.Application.Commands.ChangeEmail;
using GastronomePlatform.Modules.Users.Application.Commands.ChangePhone;
using GastronomePlatform.Modules.Users.Application.Commands.ChangeUserName;
using GastronomePlatform.Modules.Users.Application.Commands.SetVisibility;
using GastronomePlatform.Modules.Users.Application.Commands.UpdateAvatar;
using GastronomePlatform.Modules.Users.Application.Commands.UpdateLocation;
using GastronomePlatform.Modules.Users.Application.Commands.UpdatePersonalInfo;
using GastronomePlatform.Modules.Users.Application.DTOs;
using GastronomePlatform.Modules.Users.Application.Queries.GetProfile;
using GastronomePlatform.Modules.Users.Application.Queries.GetUserRoles;
using GastronomePlatform.Modules.Users.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastronomePlatform.WebAPI.Controllers.Users
{
    /// <summary>
    /// Контроллер управления профилями пользователей.
    /// Предоставляет эндпоинты для получения и обновления профиля, а также изменения учётных данных.
    /// </summary>
    [ApiController]
    [Route("api/users")]
    [Authorize] // Один из Endpoints имеет [AllowAnonymous]!!!
    public sealed class UsersController : ApiController
    {
        private readonly ICurrentUserService _currentUserService;

        #region Request Models

        /// <summary>
        /// Данные для обновления персональных данных профиля.
        /// </summary>
        /// <param name="FirstName">Имя.</param>
        /// <param name="LastName">Фамилия.</param>
        /// <param name="MiddleName">Отчество.</param>
        /// <param name="DisplayName">Публичное отображаемое имя.</param>
        /// <param name="Bio">Описание профиля.</param>
        /// <param name="Gender">Пол пользователя.</param>
        /// <param name="DateOfBirth">Дата рождения.</param>
        public sealed record UpdatePersonalInfoRequest(
            string? FirstName,
            string? LastName,
            string? MiddleName,
            string? DisplayName,
            string? Bio,
            Gender? Gender,
            DateOnly? DateOfBirth);

        /// <summary>
        /// Данные для обновления местоположения пользователя.
        /// </summary>
        /// <param name="Country">Страна.</param>
        /// <param name="Region">Регион или область.</param>
        /// <param name="City">Город.</param>
        public sealed record UpdateLocationRequest(
            string? Country,
            string? Region,
            string? City);

        /// <summary>
        /// Данные для обновления аватара пользователя.
        /// </summary>
        /// <param name="AvatarMediaId">
        /// Идентификатор медиафайла из модуля Media.
        /// <see langword="null"/> — удалить аватар.
        /// </param>
        public sealed record UpdateAvatarRequest(Guid? AvatarMediaId);

        /// <summary>
        /// Данные для изменения видимости профиля.
        /// </summary>
        /// <param name="IsPublic">
        /// <see langword="true"/> — профиль публичный;
        /// <see langword="false"/> — профиль скрыт.
        /// </param>
        public sealed record SetVisibilityRequest(bool IsPublic);

        /// <summary>
        /// Данные для изменения email пользователя.
        /// </summary>
        /// <param name="NewEmail">Новый адрес электронной почты.</param>
        public sealed record ChangeEmailRequest(string NewEmail);

        /// <summary>
        /// Данные для изменения номера телефона пользователя.
        /// </summary>
        /// <param name="NewPhone">Новый номер телефона.</param>
        public sealed record ChangePhoneRequest(string NewPhone);

        /// <summary>
        /// Данные для изменения никнейма пользователя.
        /// </summary>
        /// <param name="NewUserName">Новый никнейм.</param>
        public sealed record ChangeUserNameRequest(string NewUserName);

        #endregion

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UsersController"/>.
        /// </summary>
        /// <param name="sender">Отправитель команд MediatR.</param>
        /// <param name="currentUserService">Сервис текущего пользователя.</param>
        public UsersController(ISender sender, ICurrentUserService currentUserService) : base(sender)
        {
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        #region GET Endpoints

        /// <summary>
        /// Возвращает профиль текущего авторизованного пользователя.
        /// </summary>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>200 OK</c> с <see cref="UserProfileDto"/>;
        /// <c>404 Not Found</c> если профиль не найден.
        /// </returns>
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfileAsync(CancellationToken ct)
        {
            Guid userId = _currentUserService.UserId!.Value;
            var query = new GetProfileQuery(userId);

            Result<UserProfileDto> result = await Sender.Send(query, ct);

            return MapResult(result);
        }

        /// <summary>
        /// Возвращает публичный профиль пользователя по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор пользователя.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>200 OK</c> с <see cref="UserProfileDto"/>;
        /// <c>404 Not Found</c> если профиль не найден.
        /// </returns>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProfileByIdAsync(Guid id, CancellationToken ct)
        {
            var query = new GetProfileQuery(id);

            Result<UserProfileDto> result = await Sender.Send(query, ct);

            return MapResult(result);
        }

        /// <summary>
        /// Возвращает роли текущего авторизованного пользователя.
        /// </summary>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>200 OK</c> со списком ролей;
        /// <c>404 Not Found</c> если профиль не найден.
        /// </returns>
        [HttpGet("me/roles")]
        public async Task<IActionResult> GetMyRolesAsync(CancellationToken ct)
        {
            Guid userId = _currentUserService.UserId!.Value;
            var query = new GetUserRolesQuery(userId);

            Result<IReadOnlyCollection<string>> result = await Sender.Send(query, ct);

            return MapResult(result);
        }

        #endregion

        #region PUT Endpoints

        /// <summary>
        /// Обновляет персональные данные профиля текущего пользователя.
        /// </summary>
        /// <param name="request">Данные для обновления.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успехе;
        /// <c>404 Not Found</c> если профиль не найден.
        /// </returns>
        [HttpPut("me/personal-info")]
        public async Task<IActionResult> UpdatePersonalInfoAsync([FromBody] UpdatePersonalInfoRequest request, CancellationToken ct)
        {
            Guid userId = _currentUserService.UserId!.Value;

            var command = new UpdatePersonalInfoCommand(
                userId,
                request.FirstName,
                request.LastName,
                request.MiddleName,
                request.DisplayName,
                request.Bio,
                request.Gender,
                request.DateOfBirth);

            Result result = await Sender.Send(command, ct);

            return MapResult(result);
        }

        /// <summary>
        /// Обновляет местоположение текущего пользователя.
        /// </summary>
        /// <param name="request">Данные местоположения.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успехе;
        /// <c>404 Not Found</c> если профиль не найден.
        /// </returns>
        [HttpPut("me/location")]
        public async Task<IActionResult> UpdateLocationAsync([FromBody] UpdateLocationRequest request, CancellationToken ct)
        {
            Guid userId = _currentUserService.UserId!.Value;

            var command = new UpdateLocationCommand(
                userId,
                request.Country,
                request.Region,
                request.City);

            Result result = await Sender.Send(command, ct);

            return MapResult(result);
        }

        /// <summary>
        /// Обновляет аватар текущего пользователя.
        /// </summary>
        /// <param name="request">Идентификатор медиафайла аватара.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успехе;
        /// <c>404 Not Found</c> если профиль не найден.
        /// </returns>
        [HttpPut("me/avatar")]
        public async Task<IActionResult> UpdateAvatarAsync([FromBody] UpdateAvatarRequest request, CancellationToken ct)
        {
            Guid userId = _currentUserService.UserId!.Value;

            var command = new UpdateAvatarCommand(
                userId,
                request.AvatarMediaId);

            Result result = await Sender.Send(command, ct);

            return MapResult(result);
        }

        /// <summary>
        /// Изменяет видимость профиля текущего пользователя.
        /// </summary>
        /// <param name="request">Признак публичности профиля.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успехе;
        /// <c>404 Not Found</c> если профиль не найден.
        /// </returns>
        [HttpPut("me/visibility")]
        public async Task<IActionResult> SetVisibilityAsync([FromBody] SetVisibilityRequest request, CancellationToken ct)
        {
            Guid userId = _currentUserService.UserId!.Value;

            var command = new SetVisibilityCommand(
                userId,
                request.IsPublic);

            Result result = await Sender.Send(command, ct);

            return MapResult(result);
        }

        /// <summary>
        /// Изменяет email текущего пользователя.
        /// Проверяет уникальность в модуле Auth перед изменением.
        /// </summary>
        /// <param name="request">Новый адрес электронной почты.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успехе;
        /// <c>404 Not Found</c> если профиль не найден;
        /// <c>409 Conflict</c> если email уже занят.
        /// </returns>
        [HttpPut("me/email")]
        public async Task<IActionResult> ChangeEmailAsync([FromBody] ChangeEmailRequest request, CancellationToken ct)
        {
            Guid userId = _currentUserService.UserId!.Value;

            var command = new ChangeEmailCommand(
                userId,
                request.NewEmail);

            Result result = await Sender.Send(command, ct);

            return MapResult(result);
        }

        /// <summary>
        /// Изменяет номер телефона текущего пользователя.
        /// Проверяет уникальность в модуле Auth перед изменением.
        /// </summary>
        /// <param name="request">Новый номер телефона.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успехе;
        /// <c>404 Not Found</c> если профиль не найден;
        /// <c>409 Conflict</c> если телефон уже занят.
        /// </returns>
        [HttpPut("me/phone")]
        public async Task<IActionResult> ChangePhoneAsync([FromBody] ChangePhoneRequest request, CancellationToken ct)
        {
            Guid userId = _currentUserService.UserId!.Value;

            var command = new ChangePhoneCommand(
                userId,
                request.NewPhone);

            Result result = await Sender.Send(command, ct);

            return MapResult(result);
        }

        /// <summary>
        /// Изменяет никнейм текущего пользователя.
        /// Проверяет уникальность в модуле Auth перед изменением.
        /// </summary>
        /// <param name="request">Новый никнейм.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успехе;
        /// <c>404 Not Found</c> если профиль не найден;
        /// <c>409 Conflict</c> если никнейм уже занят.
        /// </returns>
        [HttpPut("me/username")]
        public async Task<IActionResult> ChangeUserNameAsync([FromBody] ChangeUserNameRequest request, CancellationToken ct)
        {
            Guid userId = _currentUserService.UserId!.Value;

            var command = new ChangeUserNameCommand(
                userId,
                request.NewUserName);

            Result result = await Sender.Send(command, ct);

            return MapResult(result);
        }

        #endregion
    }
}
