using FluentAssertions;
using GastronomePlatform.Modules.Subscriptions.Application.Authorization;

namespace GastronomePlatform.Subscriptions.UnitTests.Application.Authorization
{
    /// <summary>
    /// Тесты для <see cref="RoleEligibilityService"/> (Phase A stub, POL-004 §5.1).
    /// </summary>
    /// <remarks>
    /// В Phase A сервис — заглушка, всегда возвращает <see langword="true"/>.
    /// Реальная реализация появится на Этапе 6 вместе с KYC-флоу в модуле Users
    /// (UC-SUB-072). При появлении реальной логики этот файл дополняется тестами
    /// проверки KYC-статуса.
    /// </remarks>
    public sealed class RoleEligibilityServiceTests
    {
        private readonly RoleEligibilityService _service = new();

        [Fact]
        public async Task IsEligibleForRoleAsync_ForAnyInput_ReturnsTrueAsync()
        {
            // Act
            bool eligible = await _service.IsEligibleForRoleAsync(
                Guid.NewGuid(),
                "Chef",
                CancellationToken.None);

            // Assert
            eligible.Should().BeTrue();
        }
    }
}
