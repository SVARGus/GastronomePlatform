using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Media.Application.Commands.DeleteOwnFile
{
    /// <summary>
    /// Команда мягкого удаления файла владельцем (UC-MED-005).
    /// Файл переходит в статус <c>Deleted</c>; физическое удаление — фоновой задачей UC-MED-211.
    /// </summary>
    /// <param name="MediaId">Идентификатор медиафайла.</param>
    public sealed record DeleteOwnFileCommand(Guid MediaId) : ICommand;
}
