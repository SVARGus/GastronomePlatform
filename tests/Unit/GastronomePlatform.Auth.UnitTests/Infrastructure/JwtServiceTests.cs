using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using FluentAssertions.Execution;
using GastronomePlatform.Modules.Auth.Infrastructure.Identity;
using Microsoft.Extensions.Options;

namespace GastronomePlatform.Auth.UnitTests.Infrastructure
{
    /// <summary>
    /// Тесты для <see cref="JwtService"/> — состав claim-ов выпускаемого access token.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Единственный класс слоя Infrastructure модуля Auth, покрытый unit-тестами:
    /// он не обращается ни к <c>UserManager</c>, ни к базе, а зависит только от
    /// <see cref="JwtSettings"/>. Остальная Infrastructure покрывается
    /// integration-тестами.
    /// </para>
    /// <para>
    /// Проверяется именно превращение коллекции ролей в claim-ы. Тесты хендлеров
    /// подтверждают, что в сервис ушла правильная коллекция, но не то, что она
    /// доехала до токена — между этими двумя фактами и находился дефект,
    /// из-за которого пользователь с двумя ролями получал права по одной.
    /// </para>
    /// </remarks>
    public sealed class JwtServiceTests
    {
        private const string ROLE_CLAIM = "role";

        private static readonly Guid _userId = Guid.NewGuid();
        private const string EMAIL = "user@example.com";

        private readonly JwtService _sut;

        public JwtServiceTests()
        {
            var settings = new JwtSettings
            {
                // Секрет для HmacSha256 должен быть не короче 256 бит.
                Secret = "unit-test-secret-key-at-least-32-characters-long",
                Issuer = "GastronomePlatform.Tests",
                Audience = "GastronomePlatform.Tests.Client",
                AccessTokenExpiryMinutes = 15,
                RefreshTokenExpiryDays = 30
            };

            _sut = new JwtService(Options.Create(settings));
        }

        /// <summary>
        /// Декодирует выпущенный токен без валидации подписи — здесь проверяется
        /// состав claim-ов, а не корректность подписи.
        /// </summary>
        private static IReadOnlyList<Claim> ReadClaims(string token)
            => new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.ToList();

        [Fact]
        public void GenerateAccessToken_WithSeveralRoles_PutsEachRoleIntoSeparateClaim()
        {
            // Act
            string token = _sut.GenerateAccessToken(_userId, EMAIL, new[] { "User", "Admin" });

            // Assert
            IReadOnlyList<Claim> claims = ReadClaims(token);
            List<string> roles = claims.Where(c => c.Type == ROLE_CLAIM).Select(c => c.Value).ToList();

            using (new AssertionScope())
            {
                roles.Should().HaveCount(2);
                roles.Should().BeEquivalentTo(new[] { "User", "Admin" });
            }
        }

        [Fact]
        public void GenerateAccessToken_WithSingleRole_PutsExactlyOneRoleClaim()
        {
            // Act
            string token = _sut.GenerateAccessToken(_userId, EMAIL, new[] { "User" });

            // Assert
            IReadOnlyList<Claim> claims = ReadClaims(token);

            claims.Where(c => c.Type == ROLE_CLAIM).Select(c => c.Value)
                .Should().ContainSingle().Which.Should().Be("User");
        }

        [Fact]
        public void GenerateAccessToken_WithEmptyRoles_IssuesTokenWithoutRoleClaims()
        {
            // Act — пустая коллекция допустима: отказ по «нет ролей» принимает
            // вызывающий хендлер, сервис лишь выпускает токен без claim-ов роли.
            string token = _sut.GenerateAccessToken(_userId, EMAIL, Array.Empty<string>());

            // Assert
            IReadOnlyList<Claim> claims = ReadClaims(token);

            using (new AssertionScope())
            {
                claims.Should().NotContain(c => c.Type == ROLE_CLAIM);
                claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub);
                claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email);
            }
        }

        [Fact]
        public void GenerateAccessToken_AlwaysPutsIdentityClaimsUnderShortNames()
        {
            // Короткие имена claim-ов согласованы с NameClaimType/RoleClaimType
            // в конфигурации JwtBearer и с чтением в CurrentUserService.
            string token = _sut.GenerateAccessToken(_userId, EMAIL, new[] { "User" });

            IReadOnlyList<Claim> claims = ReadClaims(token);

            using (new AssertionScope())
            {
                claims.Should().ContainSingle(c => c.Type == JwtRegisteredClaimNames.Sub)
                    .Which.Value.Should().Be(_userId.ToString());
                claims.Should().ContainSingle(c => c.Type == JwtRegisteredClaimNames.Email)
                    .Which.Value.Should().Be(EMAIL);
                claims.Should().ContainSingle(c => c.Type == JwtRegisteredClaimNames.Jti);
            }
        }

        [Fact]
        public void GenerateAccessToken_WithNullRoles_ThrowsArgumentNullException()
        {
            Action action = () => _sut.GenerateAccessToken(_userId, EMAIL, null!);

            action.Should().Throw<ArgumentNullException>().WithParameterName("roles");
        }

        [Fact]
        public void GenerateAccessToken_IssuesDistinctJtiPerCall()
        {
            // jti должен быть уникальным: он идентифицирует конкретный выпуск токена.
            string first = _sut.GenerateAccessToken(_userId, EMAIL, new[] { "User" });
            string second = _sut.GenerateAccessToken(_userId, EMAIL, new[] { "User" });

            string firstJti = ReadClaims(first).Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
            string secondJti = ReadClaims(second).Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

            firstJti.Should().NotBe(secondJti);
        }
    }
}
