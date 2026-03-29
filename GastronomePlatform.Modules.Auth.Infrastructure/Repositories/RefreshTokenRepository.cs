using GastronomePlatform.Modules.Auth.Domain.Entities;
using GastronomePlatform.Modules.Auth.Domain.Repositories;
using GastronomePlatform.Modules.Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GastronomePlatform.Modules.Auth.Infrastructure.Repositories
{
    /// <summary>
    /// Репозиторий для работы с refresh-токенами через EF Core.
    /// </summary>
    public sealed class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AuthDbContext _context;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="RefreshTokenRepository"/>.
        /// </summary>
        /// <param name="context">Контекст базы данных модуля Auth.</param>
        public RefreshTokenRepository(AuthDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <inheritdoc/>
        public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(refreshToken);

            await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task DeleteInactiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            List<RefreshToken> inactiveTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && (rt.RevokedAt != null || rt.ExpiresAt <= DateTimeOffset.UtcNow))
                .ToListAsync(cancellationToken);

            _context.RefreshTokens.RemoveRange(inactiveTokens);
        }

        /// <inheritdoc/>
        public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(token);

            return await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task SaveChangesAsync(CancellationToken cancellationToken = default) => await _context.SaveChangesAsync(cancellationToken);
    }
}
