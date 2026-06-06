using FluentValidation;

namespace GastronomePlatform.Modules.Media.Application.Queries.GetFileMetadata
{
    /// <summary>
    /// Валидатор запроса <see cref="GetFileMetadataQuery"/> (UC-MED-004).
    /// </summary>
    public sealed class GetFileMetadataQueryValidator : AbstractValidator<GetFileMetadataQuery>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetFileMetadataQueryValidator"/>.
        /// </summary>
        public GetFileMetadataQueryValidator()
        {
            RuleFor(x => x.MediaId)
                .NotEqual(Guid.Empty).WithMessage("Идентификатор медиафайла обязателен.");
        }
    }
}
