using FluentValidation;

namespace GastronomePlatform.Modules.Media.Application.Queries.GetFile
{
    /// <summary>
    /// Валидатор запроса <see cref="GetFileQuery"/> (UC-MED-002).
    /// </summary>
    public sealed class GetFileQueryValidator : AbstractValidator<GetFileQuery>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetFileQueryValidator"/>.
        /// </summary>
        public GetFileQueryValidator()
        {
            RuleFor(x => x.MediaId)
                .NotEqual(Guid.Empty).WithMessage("Идентификатор медиафайла обязателен.");
        }
    }
}
