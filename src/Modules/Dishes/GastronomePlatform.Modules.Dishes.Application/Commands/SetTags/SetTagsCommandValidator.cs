using FluentValidation;
using GastronomePlatform.Modules.Dishes.Application.Helpers;
using GastronomePlatform.Modules.Dishes.Domain.Entities;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.SetTags
{
    /// <summary>
    /// Валидатор команды <see cref="SetTagsCommand"/>.
    /// </summary>
    /// <remarks>
    /// Структурные проверки: список не <see langword="null"/>; общее число имён
    /// в пределах <c>MAX_INPUT_NAMES</c> (защита от больших payload-ов); каждое имя
    /// после нормализации непустое и в пределах <see cref="Tag.MIN_NAME_LENGTH"/>..<see cref="Tag.MAX_NAME_LENGTH"/>.
    /// Проверка лимита 20 итоговых тегов (после дедупликации) — задача Domain
    /// (<c>Dish.SetTags</c>): пользователь может прислать 25 имён, из которых 5 — дубликаты
    /// с разным регистром; после нормализации останется 20, и Domain пропустит.
    /// </remarks>
    public sealed class SetTagsCommandValidator : AbstractValidator<SetTagsCommand>
    {
        /// <summary>
        /// Жёсткий потолок размера входного списка имён — защита от payload-bomb.
        /// Должен быть больше Domain-лимита <c>Dish.MAX_TAGS</c> (20), чтобы оставлять
        /// запас на дубликаты, которые схлопнутся при нормализации.
        /// </summary>
        private const int MAX_INPUT_NAMES = 100;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="SetTagsCommandValidator"/>.
        /// </summary>
        public SetTagsCommandValidator()
        {
            RuleFor(x => x.DishId)
                .NotEmpty().WithMessage("Идентификатор блюда обязателен.");

            RuleFor(x => x.TagNames)
                .NotNull().WithMessage("Список тегов обязателен (для очистки передайте пустой массив).");

            RuleFor(x => x.TagNames)
                .Must(names => names is null || names.Count <= MAX_INPUT_NAMES)
                    .WithMessage($"Количество элементов в списке тегов не должно превышать {MAX_INPUT_NAMES}.")
                .When(x => x.TagNames is not null);

            RuleForEach(x => x.TagNames)
                .Must(name => !string.IsNullOrEmpty(TagNameNormalizer.Normalize(name)))
                    .WithMessage("Имя тега не может быть пустым.")
                .Must(name => TagNameNormalizer.Normalize(name).Length >= Tag.MIN_NAME_LENGTH)
                    .WithMessage($"Имя тега должно содержать не менее {Tag.MIN_NAME_LENGTH} символа.")
                .Must(name => name is null || name.Trim().Length <= Tag.MAX_NAME_LENGTH)
                    .WithMessage($"Имя тега не должно превышать {Tag.MAX_NAME_LENGTH} символов.")
                .When(x => x.TagNames is not null);
        }
    }
}
