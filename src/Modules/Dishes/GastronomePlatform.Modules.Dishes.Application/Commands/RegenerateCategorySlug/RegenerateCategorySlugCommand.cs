using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.RegenerateCategorySlug
{
    /// <summary>
    /// Команда перегенерации slug категории (UC-DSH-105). Опасная операция —
    /// ломает существующие публичные ссылки. Отдельный эндпоинт с явным
    /// подтверждением на UI.
    /// </summary>
    /// <param name="CategoryId">Идентификатор категории.</param>
    public sealed record RegenerateCategorySlugCommand(Guid CategoryId)
        : ICommand<RegenerateCategorySlugResult>;
}
