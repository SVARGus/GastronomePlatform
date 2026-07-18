using FluentValidation;

namespace GastronomePlatform.Modules.Subscriptions.Application.Commands.ExpireDueSubscriptions
{
    /// <summary>
    /// Валидатор команды <see cref="ExpireDueSubscriptionsCommand"/>.
    /// </summary>
    /// <remarks>
    /// Команда системная и приходит не от пользователя, а из конфигурации фонового
    /// сборщика — валидация защищает от опечатки в <c>appsettings</c> (ноль,
    /// отрицательное значение, неадекватно большой батч), а не от вредоносного ввода.
    /// Верхняя граница нужна, чтобы одна итерация не поднимала в память
    /// неограниченное число агрегатов.
    /// </remarks>
    public sealed class ExpireDueSubscriptionsCommandValidator : AbstractValidator<ExpireDueSubscriptionsCommand>
    {
        /// <summary>Максимально допустимый размер батча за одну итерацию.</summary>
        private const int MAX_BATCH_SIZE = 1000;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="ExpireDueSubscriptionsCommandValidator"/>.
        /// </summary>
        public ExpireDueSubscriptionsCommandValidator()
        {
            RuleFor(x => x.BatchSize)
                .InclusiveBetween(1, MAX_BATCH_SIZE)
                .WithMessage($"Размер батча должен быть в диапазоне от 1 до {MAX_BATCH_SIZE}.");
        }
    }
}
