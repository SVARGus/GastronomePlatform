using GastronomePlatform.Common.Domain.Results;
using MediatR;

namespace GastronomePlatform.Common.Application.Messaging
{
    /// <summary>
    /// Запрос с возвращаемым значением.
    /// </summary>
    /// <typeparam name="TResponse">Тип возвращаемого значения</typeparam>
    public interface IQuery<TResponse> : IRequest<Result<TResponse>>
    {

    }
}
