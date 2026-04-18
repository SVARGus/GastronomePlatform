using FluentValidation;
using FluentValidation.Results;
using GastronomePlatform.Common.Domain.Results;
using MediatR;

namespace GastronomePlatform.Common.Application.Behaviors
{
    /// <summary>
    /// MediatR Pipeline Behavior для автоматической валидации команд и запросов.
    /// Запускается до вызова Handler'а. При наличии ошибок валидации —
    /// Handler не вызывается, возвращается <see cref="Result"/> с ошибкой.
    /// </summary>
    /// <typeparam name="TRequest">Тип запроса (команда или запрос).</typeparam>
    /// <typeparam name="TResponse">Тип ответа (Result или Result&lt;T&gt;).</typeparam>
    public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="ValidationBehavior{TRequest, TResponse}"/>.
        /// </summary>
        /// <param name="validators">
        /// Коллекция валидаторов для типа <typeparamref name="TRequest"/>.
        /// Внедряется через DI — пустая коллекция если валидатор не зарегистрирован.
        /// </param>
        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        /// <inheritdoc/>
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            // Если валидаторов нет — пропускаем, вызываем следующий шаг
            if (!_validators.Any())
            {
                return await next();
            }

            // Запускаем все валидаторы параллельно
            ValidationContext<TRequest> context = new(request);

            ValidationResult[] results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            // Собираем все ошибки
            List<string> failures = results
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .Select(f => f.ErrorMessage)
                .Distinct()
                .ToList();

            // Если ошибок нет — передаём дальше по pipeline
            if (failures.Count == 0)
            {
                return await next();
            }

            // Формируем доменную ошибку валидации
            Error error = Error.Validation("VALIDATION.ERROR", string.Join("; ", failures));

            // Возвращаем failure нужного типа через рефлексию
            // TResponse может быть Result или Result<T>
            Type responseType = typeof(TResponse);

            if (responseType == typeof(Result))
            {
                return (TResponse)(object)Result.Failure(error);
            }

            // Для Result<T> — вызываем Result<T>.Failure(error) через рефлексию
            if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                object? failureResult = responseType.GetMethod(nameof(Result.Failure), [typeof(Error)])?.Invoke(null, [error]);

                if (failureResult is TResponse typedResult)
                {
                    return typedResult;
                }
            }

            // Throw вместо тихого bypass, чтобы ошибки валидации не терялись молча.
            throw new InvalidOperationException(
                $"ValidationBehavior поддерживает только Result и Result<T>, получен '{responseType.Name}'.");
        }
    }
}
