using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GastronomePlatform.Modules.Auth.Application.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GastronomePlatform.Modules.Auth.Infrastructure.Identity
{
    /// <summary>
    /// Реализация сервиса генерации и валидации JWT-токенов.
    /// </summary>
    public sealed class JwtService : IJwtService
    {
        private readonly JwtSettings _jwtSettings;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="JwtService"/>.
        /// </summary>
        /// <param name="options">Настройки JWT из конфигурации.</param>
        public JwtService(IOptions<JwtSettings> options)
        {
            _jwtSettings = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc/>
        public int AccessTokenExpiryMinutes => _jwtSettings.AccessTokenExpiryMinutes;

        /// <inheritdoc/>
        public int RefreshTokenExpiryDays => _jwtSettings.RefreshTokenExpiryDays;

        /// <inheritdoc/>
        public string GenerateAccessToken(Guid userId, string email, IReadOnlyCollection<string> roles)
        {
            ArgumentNullException.ThrowIfNull(roles);

            // Ключ подписи — создаётся из секретной строки
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));

            // Алгоритм подписи
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Claims - данные внутри токена.
            // Все имена claim-ов — короткие, стандартные OIDC ("sub", "email", "role", "jti").
            // Раньше "role" клали через ClaimTypes.Role (long Microsoft URI) — это создавало
            // несогласованность с другими claim-ами и ломало чтение в CurrentUserService.
            var claims = new List<Claim>(roles.Count + 3)
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(JwtRegisteredClaimNames.Email, email),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Каждая роль — отдельный claim "role". Именно так их читают
            // CurrentUserService.Roles и [Authorize(Roles = ...)]:
            // в Program.cs задан RoleClaimType = "role", поэтому несколько
            // одноимённых claim-ов складываются в набор ролей принципала.
            foreach (string role in roles)
            {
                claims.Add(new Claim("role", role));
            }

            // Создание токена
            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <inheritdoc/>
        public string GenerateRefreshToken()
        {
            // 64 криптографически случайных байта → Base64 строка
            byte[] randomBytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(randomBytes);
        }

        /// <inheritdoc/>
        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string accessToken)
        {
            // Параметры валидации - все кроме строка жизни
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
                ValidateLifetime = false // ← специально false — токен может быть просрочен
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            // Отключаем inbound mapping (sub → ClaimTypes.NameIdentifier и т.п.),
            // чтобы читать claim-ы по тем же коротким именам, под которыми они кладутся
            // в GenerateAccessToken. Иначе principal.FindFirst("sub") вернёт null.
            tokenHandler.InboundClaimTypeMap.Clear();

            try
            {
                ClaimsPrincipal principal = tokenHandler.ValidateToken(
                    accessToken,
                    tokenValidationParameters,
                    out SecurityToken securityToken);

                // Дополнительна проверка - токен должен быть JWT с HMAC-SHA256
                if (securityToken is not JwtSecurityToken jwtToken ||
                    !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                // Токен невалиден (подделан, неверная подпись и т.д.)
                return null;
            }
        }
    }
}
