using GastronomePlatform.Common.Application.Messaging;
using GastronomePlatform.Common.Domain.Results;
using GastronomePlatform.Modules.Dishes.Domain.Entities;
using GastronomePlatform.Modules.Dishes.Domain.Errors;
using GastronomePlatform.Modules.Dishes.Domain.Repositories;

namespace GastronomePlatform.Modules.Dishes.Application.Commands.VerifyTag
{
    /// <summary>
    /// Обработчик команды <see cref="VerifyTagCommand"/> (UC-DSH-130).
    /// </summary>
    public sealed class VerifyTagCommandHandler : ICommandHandler<VerifyTagCommand>
    {
        private readonly ITagRepository _tagRepository;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="VerifyTagCommandHandler"/>.
        /// </summary>
        /// <param name="tagRepository">Репозиторий тегов.</param>
        public VerifyTagCommandHandler(ITagRepository tagRepository)
        {
            _tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        }

        /// <inheritdoc/>
        public async Task<Result> Handle(VerifyTagCommand request, CancellationToken cancellationToken)
        {
            Tag? tag = await _tagRepository.GetByIdAsync(request.TagId, cancellationToken);
            if (tag is null)
            {
                return DishesErrors.TagNotFound;
            }

            tag.Verify();

            await _tagRepository.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
