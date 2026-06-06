using GastronomePlatform.Common.Application.Abstractions;
using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Media.Application.Abstractions;
using GastronomePlatform.Modules.Media.Application.Configuration;
using GastronomePlatform.Modules.Media.Application.Helpers;
using GastronomePlatform.Modules.Media.Domain.Entities;
using GastronomePlatform.Modules.Media.Domain.Enums;
using GastronomePlatform.Modules.Media.Domain.Errors;
using GastronomePlatform.Modules.Media.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Options;

namespace GastronomePlatform.Modules.Media.Application.Commands.UploadFile
{
    /// <summary>
    /// Обработчик команды <see cref="UploadFileCommand"/> (UC-MED-001).
    /// </summary>
    /// <remarks>
    /// Поток выполнения:
    /// <list type="number">
    ///   <item>Проверка magic bytes и запрет исполняемых форматов (EXE, ZIP).</item>
    ///   <item>Чтение размеров изображения через <see cref="IImageProcessor"/>.</item>
    ///   <item>Проверка ограничений размеров (100–4096 px по умолчанию).</item>
    ///   <item>Сохранение оригинала в хранилище через <see cref="IFileStorage"/>.</item>
    ///   <item>Синхронная генерация Medium-миниатюры (400×400 JPEG).</item>
    ///   <item>Сохранение миниатюры в хранилище.</item>
    ///   <item>Создание агрегата <see cref="MediaFile"/> через <see cref="MediaFile.Upload"/>.</item>
    ///   <item>Добавление миниатюры в агрегат и перевод в статус <c>Ready</c>.</item>
    ///   <item>Сохранение в БД. Публикация доменных событий.</item>
    /// </list>
    /// </remarks>
    public sealed class UploadFileCommandHandler
        : ICommandHandler<UploadFileCommand, UploadFileResult>
    {
        private readonly IMediaFileRepository _repository;
        private readonly IFileStorage _fileStorage;
        private readonly IStorageKeyGenerator _keyGenerator;
        private readonly IImageProcessor _imageProcessor;
        private readonly ICurrentUserService _currentUser;
        private readonly IDateTimeProvider _clock;
        private readonly IOptions<MediaOptions> _options;
        private readonly IPublisher _publisher;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UploadFileCommandHandler"/>.
        /// </summary>
        /// <param name="repository">Репозиторий медиафайлов.</param>
        /// <param name="fileStorage">Хранилище файлов.</param>
        /// <param name="keyGenerator">Генератор ключей хранилища.</param>
        /// <param name="imageProcessor">Обработчик изображений (чтение размеров, генерация миниатюр).</param>
        /// <param name="currentUser">Сервис текущего пользователя.</param>
        /// <param name="clock">Поставщик системного времени.</param>
        /// <param name="options">Типизированные настройки модуля Media.</param>
        /// <param name="publisher">Издатель доменных событий MediatR.</param>
        public UploadFileCommandHandler(
            IMediaFileRepository repository,
            IFileStorage fileStorage,
            IStorageKeyGenerator keyGenerator,
            IImageProcessor imageProcessor,
            ICurrentUserService currentUser,
            IDateTimeProvider clock,
            IOptions<MediaOptions> options,
            IPublisher publisher)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
            _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
            _imageProcessor = imageProcessor ?? throw new ArgumentNullException(nameof(imageProcessor));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        }

        /// <inheritdoc/>
        public async Task<Result<UploadFileResult>> Handle(
            UploadFileCommand request,
            CancellationToken cancellationToken)
        {
            var actorUserId = _currentUser.UserId!.Value;
            var utcNow = _clock.UtcNow;
            var opts = _options.Value;

            // Проверка magic bytes — ContentType должен совпадать с фактической сигнатурой файла.
            if (!HasValidContentSignature(request.FileContent, request.ContentType))
            {
                return MediaErrors.InvalidFileType;
            }

            // Защита от замаскированных исполняемых файлов с расширением .jpg/.png.
            if (HasForbiddenSignature(request.FileContent))
            {
                return MediaErrors.InvalidFileType;
            }

            // Чтение размеров изображения требует ImageSharp — делается в Handler, не в валидаторе.
            var infoResult = _imageProcessor.GetImageInfo(request.FileContent, request.ContentType);
            if (infoResult.IsFailure)
            {
                return MediaErrors.InvalidFileType;
            }

            var imageInfo = infoResult.Value;
            var userUpload = opts.UserUpload;

            if (imageInfo.Width < userUpload.MinImageDimension || imageInfo.Height < userUpload.MinImageDimension ||
                imageInfo.Width > userUpload.MaxImageDimension || imageInfo.Height > userUpload.MaxImageDimension)
            {
                return MediaErrors.InvalidImageDimensions;
            }

            // mediaId генерируется до сохранения в storage — используется в пути файла.
            var dataCategory = DataCategoryResolver.Resolve(request.IntendedEntityType);
            var mediaId = Guid.NewGuid();
            var extension = GetExtension(request.ContentType);
            var storageKey = _keyGenerator.Generate(dataCategory, request.IntendedEntityType, mediaId, extension);

            using var originalStream = new MemoryStream(request.FileContent);
            var saveResult = await _fileStorage.SaveAsync(originalStream, storageKey, request.ContentType, cancellationToken);
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
                dataCategory, request.IntendedEntityType, mediaId,
                ThumbnailSize.Medium, ThumbnailFormat.Jpeg);

            using var thumbStream = new MemoryStream(thumbnail.Content);
            var thumbSaveResult = await _fileStorage.SaveAsync(thumbStream, thumbKey, "image/jpeg", cancellationToken);
            if (thumbSaveResult.IsFailure)
            {
                return MediaErrors.UploadFailed;
            }

            var orphanTimeout = TimeSpan.FromHours(opts.Orphan.ExpirationHours);
            var uploadResult = MediaFile.Upload(
                id: mediaId,
                ownerUserId: actorUserId,
                mediaType: MediaType.Image,
                contentType: request.ContentType,
                originalFileName: request.FileName,
                storageProvider: _fileStorage.ProviderName,
                storageKey: storageKey,
                sizeBytes: request.FileContent.LongLength,
                width: imageInfo.Width,
                height: imageInfo.Height,
                durationSeconds: null,
                dataCategory: dataCategory,
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

            await PublishDomainEventsAsync(mediaFile, cancellationToken);

            return new UploadFileResult(mediaFile.Id, mediaFile.Width, mediaFile.Height, mediaFile.SizeBytes);
        }

        private async Task PublishDomainEventsAsync(MediaFile mediaFile, CancellationToken ct)
        {
            var events = mediaFile.DomainEvents.ToList();
            mediaFile.ClearDomainEvents();

            foreach (var domainEvent in events)
            {
                await _publisher.Publish(domainEvent, ct);
            }
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
