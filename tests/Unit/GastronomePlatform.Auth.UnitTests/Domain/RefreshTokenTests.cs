using FluentAssertions;
using GastronomePlatform.Modules.Auth.Domain.Entities;

namespace GastronomePlatform.Auth.UnitTests.Domain
{
    /// <summary>
    /// Тесты для <see cref="RefreshToken"/>.
    /// </summary>
    public sealed class RefreshTokenTests
    {
        #region Create

        [Fact]
        public void Create_WithValidData_SetsAllFields()
        {
            // Arrange
            const string TOKEN = "test-token-value";
            Guid userId = Guid.NewGuid();
            DateTimeOffset expiresAt = DateTimeOffset.UtcNow.AddDays(30);

            // Act
            RefreshToken refreshToken = RefreshToken.Create(TOKEN, userId, expiresAt);

            // Assert
            refreshToken.Token.Should().Be(TOKEN);
            refreshToken.UserId.Should().Be(userId);
            refreshToken.ExpiresAt.Should().Be(expiresAt);
            refreshToken.RevokedAt.Should().BeNull();
            refreshToken.IsActive.Should().BeTrue();
            refreshToken.Id.Should().NotBe(Guid.Empty);
        }

        #endregion

        #region IsActive

        [Fact]
        public void IsActive_ReturnsTrue_WhenNotExpiredAndNotRevoked()
        {
            // Arrange
            RefreshToken refreshToken = RefreshToken.Create("token", Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(30));

            // Assert
            refreshToken.IsActive.Should().BeTrue();
        }

        [Fact]
        public void IsActive_ReturnsFalse_WhenExpired()
        {
            // Arrange — ExpiresAt в прошлом
            RefreshToken refreshToken = RefreshToken.Create("token", Guid.NewGuid(), DateTimeOffset.UtcNow.AddSeconds(-1));

            // Assert
            refreshToken.IsActive.Should().BeFalse();
        }

        [Fact]
        public void IsActive_ReturnsFalse_WhenRevoked()
        {
            // Arrange
            RefreshToken refreshToken = RefreshToken.Create("token", Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(30));

            // Act
            refreshToken.Revoke(DateTimeOffset.UtcNow);

            // Assert
            refreshToken.IsActive.Should().BeFalse();
        }

        #endregion

        #region Revoke

        [Fact]
        public void Revoke_ActiveToken_SetsRevokedAt()
        {
            // Arrange
            RefreshToken refreshToken = RefreshToken.Create("token", Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(30));
            DateTimeOffset revokedAt = DateTimeOffset.UtcNow;

            // Act
            refreshToken.Revoke(revokedAt);

            // Assert
            refreshToken.RevokedAt.Should().Be(revokedAt);
            refreshToken.IsActive.Should().BeFalse();
        }

        [Fact]
        public void Revoke_AlreadyRevokedToken_DoesNotChangeRevokedAt()
        {
            // Arrange
            RefreshToken refreshToken = RefreshToken.Create("token", Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(30));
            DateTimeOffset firstRevoke = DateTimeOffset.UtcNow;
            refreshToken.Revoke(firstRevoke);

            // Act — повторный отзыв
            DateTimeOffset secondRevoke = DateTimeOffset.UtcNow.AddSeconds(10);
            refreshToken.Revoke(secondRevoke);

            // Assert — RevokedAt не изменился
            refreshToken.RevokedAt.Should().Be(firstRevoke);
        }

        [Fact]
        public void Revoke_ExpiredToken_DoesNotSetRevokedAt()
        {
            // Arrange — токен уже истёк
            RefreshToken refreshToken = RefreshToken.Create("token", Guid.NewGuid(), DateTimeOffset.UtcNow.AddSeconds(-1));

            // Act
            refreshToken.Revoke(DateTimeOffset.UtcNow);

            // Assert — Revoke игнорируется для неактивного токена
            refreshToken.RevokedAt.Should().BeNull();
        }

        #endregion
    }
}
