using GastronomePlatform.Common.Domain.Results;
using MediatR;

namespace GastronomePlatform.Common.Application.Messaging
{
    /// <summary>
    /// Команда без возвращаемого значения (DeleteDish, CancelOrder)
    /// </summary>
    public interface ICommand : IRequest<Result>
    {

    }

    /// <summary>
    /// Команда с возвращаемым значением (CreateDish → возвращает Guid нового Id)
    /// </summary>
    /// <typeparam name="TResponse">Тип возвращаемого значения при успехе</typeparam>
    public interface ICommand<TResponse> : IRequest<Result<TResponse>>
    {

    }
}
