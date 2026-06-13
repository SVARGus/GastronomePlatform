using System.Text;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Media.Application.Abstractions;
using GastronomePlatform.Modules.Media.Application.Configuration;
using GastronomePlatform.Modules.Media.Domain.Entities;
using GastronomePlatform.Modules.Media.Domain.Enums;
using GastronomePlatform.Modules.Media.Domain.Errors;
using GastronomePlatform.Modules.Media.Domain.Repositories;
using GastronomePlatform.Common.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace GastronomePlatform.Modules.Media.Application.Commands.UploadSystemFile
{
    /// <summary>
    /// Обработчик команды <see cref="UploadSystemFileCommand"/> (UC-MED-101).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Проверка роли Admin (defense-in-depth).</item>
    ///   <item>Проверка размера файла против <c>Media:SystemUpload:MaxSizeBytes</c>.</item>
    ///   <item>Ветвление по MIME-типу:</item>
    ///   <item>
    ///     SVG — санирование через <see cref="ISvgSanitizer"/>, сохранение в хранилище,
    ///     создание <see cref="MediaFile"/> без миниатюры.
    ///   </item>
    ///   <item>
    ///     JPEG/PNG — проверка magic bytes, чтение размеров через <see cref="IImageProcessor"/>,
    ///     проверка максимального размера (1024 px), сохранение оригинала,
    ///     генерация Medium-миниатюры, создание <see cref="MediaFile"/> с миниатюрой.
    ///   </item>
    ///   <item>Сохранение в БД. Публикация доменных событий.</item>
    /// </list>
    /// Системные файлы создаются без владельца (<c>OwnerUserId = NULL</c>)
    /// и всегда в категории <see cref="MediaDataCategory.Public"/>.
    /// </remarks>
    public sealed class UploadSystemFileCommandHandler
        : ICommandHandler<UploadSystemFileCommand, UploadSystemFileResult>
    {
        private readonly IMediaFileRepository _repository;
        private readonly IFileStorage _fileStorage;
        private readonly IStorageKeyGenerator _keyGenerator;
        private readonly IImageProcessor _imageProcessor;
        private readonly ISvgSanitizer _svgSanitizer;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IOptions<MediaOptions> _options;
        private readonly IDomainEventDispatcher _eventDispatcher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UploadSystemFileCommandHandler"/>.
        /// </summary>
        /// <param name="repository">Репозиторий медиафайлов.</param>
        /// <param name="fileStorage">Хранилище файлов.</param>
        /// <param name="keyGenerator">Генератор ключей хранилища.</param>
        /// <param name="imageProcessor">Обработчик изображений (чтение размеров, генерация миниатюр).</param>
        /// <param name="svgSanitizer">Санитайзер SVG-разметки для предотвращения XSS.</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="options">Типизированные настройки модуля Media.</param>
        /// <param name="eventDispatcher">Диспетчер доменных событий.</param>
        public UploadSystemFileCommandHandler(
            IMediaFileRepository repository,
            IFileStorage fileStorage,
            IStorageKeyGenerator keyGenerator,
            IImageProcessor imageProcessor,
            ISvgSanitizer svgSanitizer,
            ICurrentUserService currentUser,
            IDateTimeProvider clock,
            IOptions<MediaOptions> options,
            IDomainEventDispatcher eventDispatcher)
        {
            _repository      = repository      ?? throw new ArgumentNullException(nameof(repository));
            _fileStorage     = fileStorage     ?? throw new ArgumentNullException(nameof(fileStorage));
            _keyGenerator    = keyGenerator    ?? throw new ArgumentNullException(nameof(keyGenerator));
            _imageProcessor  = imageProcessor  ?? throw new ArgumentNullException(nameof(imageProcessor));
            _svgSanitizer    = svgSanitizer    ?? throw new ArgumentNullException(nameof(svgSanitizer));
            _currentUser     = currentUser     ?? throw new ArgumentNullException(nameof(currentUser));
            _clock           = clock           ?? throw new ArgumentNullException(nameof(clock));
            _options         = options         ?? throw new ArgumentNullException(nameof(options));
            _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
        }

        /// <inheritdoc/>
        public async Task<Result<UploadSystemFileResult>> Handle(
            UploadSystemFileCommand request,
            CancellationToken cancellationToken)
        {
            // Defense-in-depth: контроллер защищён [Authorize(Roles="Admin")],
            // но проверяем роль и здесь на случай прямого вызова через MediatR.
            if (!_currentUser.IsInRole(PlatformRoles.ADMIN))
            {
                return MediaErrors.ForbiddenNotOwner;
            }

            var utcNow = _clock.UtcNow;
            var opts = _options.Value;

            if (request.FileContent.LongLength > opts.SystemUpload.MaxSizeBytes)
            {
                return MediaErrors.FileTooLarge;
            }

            var mediaId = Guid.NewGuid();
            var orphanTimeout = TimeSpan.FromHours(opts.Orphan.ExpirationHours);

            return request.ContentType == "image/svg+xml"
                ? await HandleSvgAsync(request, mediaId, orphanTimeout, utcNow, cancellationToken)
                : await HandleRasterAsync(request, mediaId, orphanTimeout, utcNow, opts, cancellationToken);
        }

        private async Task<Result<UploadSystemFileResult>> HandleSvgAsync(
            UploadSystemFileCommand request,
            Guid mediaId,
            TimeSpan orphanTimeout,
            DateTimeOffset utcNow,
            CancellationToken cancellationToken)
        {
            var svgString = Encoding.UTF8.GetString(request.FileContent);
            var sanitized = _svgSanitizer.Sanitize(svgString);
            var sanitizedBytes = Encoding.UTF8.GetBytes(sanitized);

            var storageKey = _keyGenerator.Generate(
                MediaDataCategory.Public, request.IntendedEntityType, mediaId, "svg");

            using var svgStream = new MemoryStream(sanitizedBytes);
            var saveResult = await _fileStorage.SaveAsync(
                svgStream, storageKey, "image/svg+xml", cancellationToken);

            if (saveResult.IsFailure)
            {
                return MediaErrors.UploadFailed;
            }

            // TODO: tech debt — при сбое после этой точки SVG-файл остаётся в хранилище
            // без записи в БД. Cleanup через фоновую задачу UC-MED-210 (Этап 8+).

            var uploadResult = MediaFile.Upload(
                id: mediaId,
                ownerUserId: null,
                mediaType: MediaType.Image,
                contentType: "image/svg+xml",
                originalFileName: request.FileName,
                storageProvider: _fileStorage.ProviderName,
                storageKey: storageKey,
                sizeBytes: sanitizedBytes.LongLength,
                width: null,
                height: null,
                durationSeconds: null,
                dataCategory: MediaDataCategory.Public,
                orphanTimeout: orphanTimeout,
                utcNow: utcNow);

            if (uploadResult.IsFailure)
            {
                return uploadResult.Error;
            }

            var mediaFile = uploadResult.Value;

            var readyResult = mediaFile.MarkAsReady(utcNow);
            if (readyResult.IsFailure)
            {
                return readyResult.Error;
            }

            await _repository.AddAsync(mediaFile, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            await _eventDispatcher.DispatchAsync(mediaFile, cancellationToken);

            return new UploadSystemFileResult(mediaFile.Id, null, null, mediaFile.SizeBytes);
        }

        private async Task<Result<UploadSystemFileResult>> HandleRasterAsync(
            UploadSystemFileCommand request,
            Guid mediaId,
            TimeSpan orphanTimeout,
            DateTimeOffset utcNow,
            MediaOptions opts,
            CancellationToken cancellationToken)
        {
            if (!HasValidContentSignature(request.FileContent, request.ContentType))
            {
                return MediaErrors.InvalidFileType;
            }

            if (HasForbiddenSignature(request.FileContent))
            {
                return MediaErrors.InvalidFileType;
            }

            var infoResult = _imageProcessor.GetImageInfo(request.FileContent, request.ContentType);
            if (infoResult.IsFailure)
            {
                return MediaErrors.InvalidFileType;
            }

            var imageInfo = infoResult.Value;
            if (imageInfo.Width > opts.SystemUpload.MaxImageDimension ||
                imageInfo.Height > opts.SystemUpload.MaxImageDimension)
            {
                return MediaErrors.InvalidImageDimensions;
            }

            var extension = GetExtension(request.ContentType);
            var storageKey = _keyGenerator.Generate(
                MediaDataCategory.Public, request.IntendedEntityType, mediaId, extension);

            using var originalStream = new MemoryStream(request.FileContent);
            var saveResult = await _fileStorage.SaveAsync(
                originalStream, storageKey, request.ContentType, cancellationToken);

            if (saveResult.IsFailure)
            {
                return MediaErrors.UploadFailed;
            }

            // TODO: tech debt — при сбое после этой точки файл оригинала остаётся в хранилище
            // без записи в БД. Cleanup через фоновую задачу UC-MED-210 (Этап 8+).

            var thumbResult = await _imageProcessor.GenerateMediumThumbnailAsync(
                request.FileContent,
                request.ContentType,
                opts.Thumbnails.MediumSize,
                opts.Thumbnails.JpegQuality,
                cancellationToken);

            if (thumbResult.IsFailure)
            {
                return MediaErrors.UploadFailed;
            }

            var thumbnail = thumbResult.Value;
            var thumbKey = _keyGenerator.GenerateThumbnail(
                MediaDataCategory.Public, request.IntendedEntityType, mediaId,
                ThumbnailSize.Medium, ThumbnailFormat.Jpeg);

            using var thumbStream = new MemoryStream(thumbnail.Content);
            var thumbSaveResult = await _fileStorage.SaveAsync(
                thumbStream, thumbKey, "image/jpeg", cancellationToken);

            if (thumbSaveResult.IsFailure)
            {
                return MediaErrors.UploadFailed;
            }

            var uploadResult = MediaFile.Upload(
                id: mediaId,
                ownerUserId: null,
                mediaType: MediaType.Image,
                contentType: request.ContentType,
                originalFileName: request.FileName,
                storageProvider: _fileStorage.ProviderName,
                storageKey: storageKey,
                sizeBytes: request.FileContent.LongLength,
                width: imageInfo.Width,
                height: imageInfo.Height,
                durationSeconds: null,
                dataCategory: MediaDataCategory.Public,
                orphanTimeout: orphanTimeout,
                utcNow: utcNow);

            if (uploadResult.IsFailure)
            {
                return uploadResult.Error;
            }

            var mediaFile = uploadResult.Value;

            var addThumbResult = mediaFile.AddThumbnail(
                ThumbnailSize.Medium,
                ThumbnailFormat.Jpeg,
                thumbKey,
                thumbnail.Width,
                thumbnail.Height,
                thumbnail.Content.LongLength,
                utcNow);

            if (addThumbResult.IsFailure)
            {
                return addThumbResult.Error;
            }

            var readyResult = mediaFile.MarkAsReady(utcNow);
            if (readyResult.IsFailure)
            {
                return readyResult.Error;
            }

            await _repository.AddAsync(mediaFile, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            await _eventDispatcher.DispatchAsync(mediaFile, cancellationToken);

            return new UploadSystemFileResult(
                mediaFile.Id, mediaFile.Width, mediaFile.Height, mediaFile.SizeBytes);
        }

        // Верифицирует magic bytes: первые байты файла должны совпадать с заявленным ContentType.
        private static bool HasValidContentSignature(byte[] content, string contentType)
        {
            if (content.Length < 4)
            {
                return false;
            }

            return contentType switch
            {
                "image/jpeg" => content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF,
                "image/png"  => content[0] == 0x89 && content[1] == 0x50 &&
                                content[2] == 0x4E && content[3] == 0x47,
                _            => false
            };
        }

        // MZ (0x4D 0x5A) — Windows EXE/DLL; PK\x03\x04 (0x50 0x4B 0x03 0x04) — ZIP/DOCX/JAR.
        private static bool HasForbiddenSignature(byte[] content)
        {
            if (content.Length < 4)
            {
                return false;
            }

            if (content[0] == 0x4D && content[1] == 0x5A)
            {
                return true;
            }
            if (content[0] == 0x50 && content[1] == 0x4B && content[2] == 0x03 && content[3] == 0x04)
            {
                return true;
            }

            return false;
        }

        private static string GetExtension(string contentType) => contentType switch
        {
            "image/jpeg" => "jpg",
            "image/png"  => "png",
            _            => "bin"
        };
    }
}
