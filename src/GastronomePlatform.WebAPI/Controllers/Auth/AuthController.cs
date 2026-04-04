using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Auth.Application.Commands.Login;
using GastronomePlatform.Modules.Auth.Application.Commands.Logout;
using GastronomePlatform.Modules.Auth.Application.Commands.RefreshAccessToken;
using GastronomePlatform.Modules.Auth.Application.Commands.Register;
using GastronomePlatform.Modules.Auth.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastronomePlatform.WebAPI.Controllers.Auth
{
    /// <summary>
    /// Контроллер аутентификации и управления сессиями.
    /// Обрабатывает регистрацию, вход, обновление и отзыв токенов.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    public sealed class AuthController : ApiController
    {
        // Request-объекты — входные данные от клиента
        /// <summary>
        /// Данные для регистрации нового пользователя.
        /// </summary>
        /// <param name="Email">Адрес электронной почты.</param>
        /// <param name="UserName">Никнейм пользователя (уникальный).</param>
        /// <param name="Password">Пароль в открытом виде.</param>
        /// <param name="Phone">Номер телефона (опционально).</param>
        public sealed record RegisterRequest(string Email, string UserName, string Password, string? Phone);

        /// <summary>
        /// Данные для входа в систему.
        /// </summary>
        /// <param name="Login">Email, никнейм или номер телефона.</param>
        /// <param name="Password">Пароль в открытом виде.</param>
        public sealed record LoginRequest(string Login, string Password);

        /// <summary>
        /// Данные для обновления пары токенов.
        /// </summary>
        /// <param name="RefreshToken">Строковое значение действующего refresh token.</param>
        public sealed record RefreshRequest(string RefreshToken);

        /// <summary>
        /// Данные для завершения сессии.
        /// </summary>
        /// <param name="RefreshToken">Строковое значение refresh token текущей сессии.</param>
        public sealed record LogoutRequest(string RefreshToken);

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="AuthController"/>.
        /// </summary>
        /// <param name="sender">Отправитель команд MediatR.</param>
        public AuthController(ISender sender) : base(sender) { }

        /// <summary>
        /// Регистрирует нового пользователя в системе.
        /// </summary>
        /// <param name="request">Данные для регистрации.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успехе;
        /// <c>409 Conflict</c> если email, никнейм или телефон уже заняты;
        /// <c>400 Bad Request</c> при ошибке валидации.
        /// </returns>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest request, CancellationToken ct)
        {
            var command = new RegisterCommand(request.Email, request.UserName, request.Password, request.Phone);

            Result result = await Sender.Send(command, ct);

            return MapResult(result);
        }

        /// <summary>
        /// Аутентифицирует пользователя и возвращает пару токенов.
        /// </summary>
        /// <param name="request">Данные для входа.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>200 OK</c> с <see cref="LoginResponse"/> при успехе;
        /// <c>400 Bad Request</c> при неверных учётных данных.
        /// </returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request, CancellationToken ct)
        {
            var command = new LoginCommand(request.Login, request.Password);

            Result<LoginResponse> result = await Sender.Send(command, ct);

            return MapResult<LoginResponse>(result);
        }

        /// <summary>
        /// Обновляет пару токенов по действующему refresh token.
        /// Старый refresh token отзывается (Token Rotation).
        /// </summary>
        /// <param name="request">Данные с текущим refresh token.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>200 OK</c> с новым <see cref="LoginResponse"/> при успехе;
        /// <c>400 Bad Request</c> если токен недействителен или истёк.
        /// </returns>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshAsync([FromBody] RefreshRequest request, CancellationToken ct)
        {
            var command = new RefreshAccessTokenCommand(request.RefreshToken);

            Result<LoginResponse> result = await Sender.Send(command, ct);

            return MapResult<LoginResponse>(result);
        }

        /// <summary>
        /// Завершает сессию пользователя, отзывая refresh token.
        /// </summary>
        /// <param name="request">Данные с refresh token текущей сессии.</param>
        /// <param name="ct">Токен отмены операции.</param>
        /// <returns>
        /// <c>204 No Content</c> при успехе;
        /// <c>400 Bad Request</c> если токен не найден или уже отозван.
        /// </returns>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> LogoutAsync([FromBody] LogoutRequest request, CancellationToken ct)
        {
            var command = new LogoutCommand(request.RefreshToken);

            Result result = await Sender.Send(command,ct);

            return MapResult(result);
        }
    }
}
