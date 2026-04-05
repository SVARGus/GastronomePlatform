using GastronomePlatform.Modules.Users.Domain.Entities;
using GastronomePlatform.Modules.Users.Domain.Repositories;
using GastronomePlatform.Modules.Users.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GastronomePlatform.Modules.Users.Infrastructure.Repositories
{
    /// <summary>
    /// Репозиторий для работы с профилями пользователей через EF Core.
    /// </summary>
    public sealed class UserProfileRepository : IUserProfileRepository
    {
        private readonly UsersDbContext _context;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="UserProfileRepository"/>.
        /// </summary>
        /// <param name="context">Контекст базы данных модуля Users.</param>
        public UserProfileRepository(UsersDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public async Task AddAsync(UserProfile userProfile, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(userProfile);

            await _context.UserProfiles.AddAsync(userProfile, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default)
            => await _context.UserProfiles.AnyAsync(x => x.UserId == userId, cancellationToken);

        /// <inheritdoc/>
        public async Task<UserProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
            => await _context.UserProfiles.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        /// <inheritdoc/>
        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken);
    }
}
