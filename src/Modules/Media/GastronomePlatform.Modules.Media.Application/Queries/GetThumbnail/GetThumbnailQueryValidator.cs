using FluentValidation;

namespace GastronomePlatform.Modules.Media.Application.Queries.GetThumbnail
{
    /// <summary>
    /// Валидатор запроса <see cref="GetThumbnailQuery"/> (UC-MED-003).
    /// </summary>
    public sealed class GetThumbnailQueryValidator : AbstractValidator<GetThumbnailQuery>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetThumbnailQueryValidator"/>.
        /// </summary>
        public GetThumbnailQueryValidator()
        {
            RuleFor(x => x.MediaId)
                .NotEqual(Guid.Empty).WithMessage("Идентификатор медиафайла обязателен.");

            RuleFor(x => x.Size)
                .IsInEnum().WithMessage("Указан недопустимый размер миниатюры.");

            RuleFor(x => x.Format)
                .IsInEnum().WithMessage("Указан недопустимый формат миниатюры.");
        }
    }
}
