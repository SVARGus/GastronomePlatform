using FluentValidation;

namespace GastronomePlatform.Modules.Media.Application.Queries.GetUserFiles
{
    /// <summary>
    /// Валидатор запроса <see cref="GetUserFilesQuery"/> (UC-MED-103).
    /// </summary>
    public sealed class GetUserFilesQueryValidator : AbstractValidator<GetUserFilesQuery>
    {
        /// <summary>
        /// Инициализирует новый экземпляр <see cref="GetUserFilesQueryValidator"/>.
        /// </summary>
        public GetUserFilesQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEqual(Guid.Empty).WithMessage("Идентификатор пользователя обязателен.");

            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("Номер страницы должен быть не менее 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("Размер страницы должен быть от 1 до 100.");
        }
    }
}
