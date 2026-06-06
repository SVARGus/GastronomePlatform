using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Media.Application.Commands.DeleteAnyFile
{
    /// <summary>
    /// Команда принудительного мягкого удаления любого файла администратором (UC-MED-102).
    /// Не проверяет владельца. Если файл привязан к сущности — сначала выполняется
    /// отвязка, затем soft delete. Физическое удаление — фоновой задачей UC-MED-211.
    /// </summary>
    /// <param name="MediaId">Идентификатор медиафайла для удаления.</param>
    public sealed record DeleteAnyFileCommand(Guid MediaId) : ICommand;
}
