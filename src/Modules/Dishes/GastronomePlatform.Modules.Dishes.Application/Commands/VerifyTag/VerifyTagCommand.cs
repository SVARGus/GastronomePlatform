using GastronomePlatform.Common.Application.Messaging;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.VerifyTag
{
    /// <summary>
    /// Команда верификации тега администратором (UC-DSH-130). Помечает тег как
    /// <c>IsVerified = true</c> — после этого тег появляется в облаке популярных
    /// (UC-DSH-061) и в общем автокомплите без оглядки на <c>UsageCount</c>.
    /// </summary>
    /// <remarks>
    /// Авторизация — роль <c>Admin</c>. Проверка на эндпоинте через
    /// <c>[Authorize(Roles = PlatformRoles.ADMIN)]</c>.
    /// Идемпотентна: повторный вызов на уже верифицированном теге также возвращает
    /// <c>204 No Content</c>.
    /// </remarks>
    /// <param name="TagId">Идентификатор верифицируемого тега.</param>
    public sealed record VerifyTagCommand(Guid TagId) : ICommand;
}
