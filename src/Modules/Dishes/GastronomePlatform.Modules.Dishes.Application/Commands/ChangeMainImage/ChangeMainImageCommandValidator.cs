using FluentValidation;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.ChangeMainImage
{
    /// <summary>
    /// Валидатор команды <see cref="ChangeMainImageCommand"/>.
    /// </summary>
    /// <remarks>
    /// Только структурные проверки. Существование медиафайла, его владение и
    /// статус (Ready) валидируются на уровне <c>IMediaService.AttachToEntityAsync</c>.
    /// </remarks>
    public sealed class ChangeMainImageCommandValidator : AbstractValidator<ChangeMainImageCommand>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="ChangeMainImageCommandValidator"/>.
        /// </summary>
        public ChangeMainImageCommandValidator()
        {
            RuleFor(x => x.DishId)
                .NotEmpty().WithMessage("Идентификатор блюда обязателен.");

            RuleFor(x => x.MainImageId)
                .NotEqual(Guid.Empty)
                    .WithMessage("Идентификатор главного фото не может быть пустым GUID.")
                .When(x => x.MainImageId.HasValue);
        }
    }
}
