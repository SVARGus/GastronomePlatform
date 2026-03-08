using GastronomePlatform.Common.Domain.Results;
using MediatR;

namespace GastronomePlatform.Common.Application.Messaging
{
    /// <summary>
    /// Обработчик запроса
    /// </summary>
    /// <typeparam name="TQuery">Тип запроса</typeparam>
    /// <typeparam name="TResponse">Тип возвращаемого значения</typeparam>
    public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>> where TQuery : IQuery<TResponse>
    {

    }
}
