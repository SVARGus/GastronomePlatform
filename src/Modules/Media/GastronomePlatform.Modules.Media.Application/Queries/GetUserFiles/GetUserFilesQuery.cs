using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Modules.Media.Domain.Enums;

namespace GastronomePlatform.Modules.Media.Application.Queries.GetUserFiles
{
    /// <summary>
    /// Запрос на получение страницы файлов конкретного пользователя (UC-MED-103).
    /// Используется администратором для аудита и экспорта персональных данных.
    /// </summary>
    /// <param name="UserId">Идентификатор пользователя, чьи файлы запрашиваются.</param>
    /// <param name="Status">Фильтр по статусу файла. <see langword="null"/> — все статусы.</param>
    /// <param name="EntityType">Фильтр по типу сущности-владельца. <see langword="null"/> — все типы.</param>
    /// <param name="Page">Номер страницы (начиная с 1).</param>
    /// <param name="PageSize">Количество записей на странице (1–100).</param>
    public sealed record GetUserFilesQuery(
        Guid UserId,
        MediaStatus? Status,
        string? EntityType,
        int Page,
        int PageSize) : IQuery<GetUserFilesResult>;
}
