using GastronomePlatform.Common.Domain.Results;
using MediatR;

namespace GastronomePlatform.Common.Application.Messaging
{
    /// <summary>
    /// Обработчик команды без значения
    /// </summary>
    /// <typeparam name="TCommand">Тип команды</typeparam>
    public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Result> where TCommand : ICommand
    {

    }

    /// <summary>
    /// Обработчик команды с значением
    /// </summary>
    /// <typeparam name="TCommand">Тип команды</typeparam>
    /// <typeparam name="TResponse">Тип ответа</typeparam>
    public interface ICommandHandler<TCommand, TResponse> 
        : IRequestHandler<TCommand, Result<TResponse>> 
        where TCommand : ICommand<TResponse>
    {

    }
}
