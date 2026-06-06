using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Modules.Media.Application.Commands.DeleteAnyFile;
using GastronomePlatform.Modules.Media.Application.Commands.DeleteOwnFile;
using GastronomePlatform.Modules.Media.Application.Commands.UploadFile;
using GastronomePlatform.Modules.Media.Application.Commands.UploadSystemFile;
using GastronomePlatform.Modules.Media.Application.Queries.GetFile;
using GastronomePlatform.Modules.Media.Application.Queries.GetFileMetadata;
using GastronomePlatform.Modules.Media.Application.Queries.GetThumbnail;
using GastronomePlatform.Modules.Media.Application.Queries.GetUserFiles;
using GastronomePlatform.Modules.Media.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastronomePlatform.WebAPI.Controllers.Media
{
    /// <summary>
    /// Контроллер модуля Media — управление медиафайлами.
    /// </summary>
    /// <remarks>
    /// Реализует сценарии UC-MED-001–005 (пользователь),
    /// UC-MED-101–103 (администратор).
    /// Внутренние сценарии UC-MED-200–204 доступны через <c>IMediaService</c>.
    /// </remarks>
    [ApiController]
    [Route("api/media")]
    public sealed class MediaController : ApiController
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="MediaController"/>.
        /// </summary>
        /// <param name="sender">Отправитель команд MediatR.</param>
        public MediaController(ISender sender) : base(sender) { }

        #region Пользовательские эндпоинты (001–005)

        /// <summary>
        /// Загружает файл от имени текущего пользователя (UC-MED-001).
        /// Сохраняет JPEG или PNG до 10 МБ, синхронно создаёт Medium-миниатюру.
        /// Файл остаётся orphan до явной привязки через <c>IMediaService.AttachToEntityAsync</c>.
        /// </summary>
        /// <param name="file">Загружаемый файл (JPEG или PNG, до 10 МБ).</param>
        /// <param name="intendedEntityType">Тип сущности-цели (напр. <c>Dish</c>, <c>RecipeStep</c>).</param>
        /// <param name="ct">Токен отмены.</param>
        [HttpPost("upload")]
        [Authorize]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(11_534_336)]
        [RequestFormLimits(MultipartBodyLengthLimit = 11_534_336)]
        public async Task<IActionResult> UploadFileAsync(
            IFormFile file,
            [FromForm] string intendedEntityType,
            CancellationToken ct)
        {
            if (file is null || file.Length == 0)
            {
                return BadRequest("Файл обязателен.");
            }

            byte[] content;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms, ct);
                content = ms.ToArray();
            }

            var command = new UploadFileCommand(
                file.FileName,
                file.ContentType,
                content,
                intendedEntityType);

            var result = await Sender.Send(command, ct);

            if (result.IsFailure)
            {
                return MapError(result.Error);
            }

            return CreatedAtAction(
                "GetFileMetadata",
                new { id = result.Value.MediaId },
                result.Value);
        }

        /// <summary>
        /// Отдаёт файл по идентификатору (UC-MED-002).
        /// Доступ согласно POL-002: Public — всем, Personal — только авторизованным.
        /// </summary>
        /// <param name="id">Идентификатор медиафайла.</param>
        /// <param name="ct">Токен отмены.</param>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetFileAsync(Guid id, CancellationToken ct)
        {
            var result = await Sender.Send(new GetFileQuery(id), ct);

            if (result.IsFailure)
            {
                return MapError(result.Error);
            }

            return File(result.Value.Content, result.Value.ContentType);
        }

        /// <summary>
        /// Отдаёт миниатюру файла (UC-MED-003).
        /// Миниатюры доступны всем пользователям, включая гостей.
        /// На Этапе 2 поддерживается только <c>size=medium&amp;format=jpeg</c>.
        /// </summary>
        /// <param name="id">Идентификатор медиафайла.</param>
        /// <param name="size">Размер миниатюры.</param>
        /// <param name="format">Формат миниатюры.</param>
        /// <param name="ct">Токен отмены.</param>
        [HttpGet("{id:guid}/thumbnail")]
        public async Task<IActionResult> GetThumbnailAsync(
            Guid id,
            [FromQuery] ThumbnailSize size = ThumbnailSize.Medium,
            [FromQuery] ThumbnailFormat format = ThumbnailFormat.Jpeg,
            CancellationToken ct = default)
        {
            var result = await Sender.Send(new GetThumbnailQuery(id, size, format), ct);

            if (result.IsFailure)
            {
                return MapError(result.Error);
            }

            return File(result.Value.Content, result.Value.ContentType);
        }

        /// <summary>
        /// Возвращает метаданные файла без содержимого (UC-MED-004).
        /// Доступ согласно POL-002: Personal-метаданные — только авторизованным.
        /// </summary>
        /// <param name="id">Идентификатор медиафайла.</param>
        /// <param name="ct">Токен отмены.</param>
        [HttpGet("{id:guid}/metadata")]
        public async Task<IActionResult> GetFileMetadataAsync(Guid id, CancellationToken ct)
            => MapResult(await Sender.Send(new GetFileMetadataQuery(id), ct));

        /// <summary>
        /// Мягко удаляет файл владельца (UC-MED-005).
        /// Физическое удаление — фоновой задачей UC-MED-211 (Этап 8+).
        /// Нельзя удалить файл, привязанный к сущности (<c>MEDIA.STILL_ATTACHED</c>).
        /// </summary>
        /// <param name="id">Идентификатор медиафайла.</param>
        /// <param name="ct">Токен отмены.</param>
        [HttpDelete("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> DeleteOwnFileAsync(Guid id, CancellationToken ct)
            => MapResult(await Sender.Send(new DeleteOwnFileCommand(id), ct));

        #endregion

        #region Административные эндпоинты (101–103)

        /// <summary>
        /// Загружает системный файл (иконка, иллюстрация ингредиента) от имени администратора (UC-MED-101).
        /// Разрешены JPEG, PNG и SVG; SVG санируется для предотвращения XSS.
        /// </summary>
        /// <param name="file">Загружаемый файл (JPEG, PNG или SVG, до 3 МБ).</param>
        /// <param name="intendedEntityType">Тип целевой сущности (<c>CategoryIcon</c>, <c>IngredientImage</c>).</param>
        /// <param name="ct">Токен отмены.</param>
        [HttpPost("system/upload")]
        [Authorize(Roles = PlatformRoles.ADMIN)]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(3_145_728)]
        [RequestFormLimits(MultipartBodyLengthLimit = 3_145_728)]
        public async Task<IActionResult> UploadSystemFileAsync(
            IFormFile file,
            [FromForm] string intendedEntityType,
            CancellationToken ct)
        {
            if (file is null || file.Length == 0)
            {
                return BadRequest("Файл обязателен.");
            }

            byte[] content;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms, ct);
                content = ms.ToArray();
            }

            var command = new UploadSystemFileCommand(
                file.FileName,
                file.ContentType,
                content,
                intendedEntityType);

            var result = await Sender.Send(command, ct);

            if (result.IsFailure)
            {
                return MapError(result.Error);
            }

            return CreatedAtAction(
                "GetFileMetadata",
                new { id = result.Value.MediaId },
                result.Value);
        }

        /// <summary>
        /// Принудительно мягко удаляет любой файл (UC-MED-102).
        /// Если файл привязан к сущности — сначала автоматически отвязывается.
        /// </summary>
        /// <param name="id">Идентификатор медиафайла.</param>
        /// <param name="ct">Токен отмены.</param>
        [HttpDelete("admin/{id:guid}")]
        [Authorize(Roles = PlatformRoles.ADMIN)]
        public async Task<IActionResult> DeleteAnyFileAsync(Guid id, CancellationToken ct)
            => MapResult(await Sender.Send(new DeleteAnyFileCommand(id), ct));

        /// <summary>
        /// Возвращает постраничный список файлов пользователя (UC-MED-103).
        /// Используется для аудита и экспорта персональных данных (152-ФЗ).
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="status">Фильтр по статусу. <see langword="null"/> — все статусы.</param>
        /// <param name="entityType">Фильтр по типу сущности. <see langword="null"/> — все типы.</param>
        /// <param name="page">Номер страницы (от 1).</param>
        /// <param name="pageSize">Записей на странице (1–100).</param>
        /// <param name="ct">Токен отмены.</param>
        [HttpGet("admin/users/{userId:guid}/files")]
        [Authorize(Roles = PlatformRoles.ADMIN)]
        public async Task<IActionResult> GetUserFilesAsync(
            Guid userId,
            [FromQuery] MediaStatus? status,
            [FromQuery] string? entityType,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
            => MapResult(await Sender.Send(
                new GetUserFilesQuery(userId, status, entityType, page, pageSize), ct));

        #endregion
    }
}
