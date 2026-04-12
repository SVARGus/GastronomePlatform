using GastronomePlatform.Common.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace GastronomePlatform.Common.Infrastructure.Services
{

    /// <summary>
    /// Предоставляет информацию о текущем пользователе на основе JWT claims из HTTP-контекста.
    /// Возвращает null/пустые значения, если HTTP-контекст отсутствует (background jobs, тесты).
    /// </summary>
    public sealed class CurrentUserService : ICurrentUserService
    {
        /// <summary>
        /// Стандартные типы claims из JWT-токена.
        /// </summary>
        private static class JwtClaims
        {
            public const string USER_ID = "sub";
            public const string EMAIL = "email";
            public const string ROLE = "role";
        }

        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Конструктор сервиса текущего пользователя.
        /// </summary>
        /// <param name="httpContextAccessor">Доступ к HTTP-контексту</param>
        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public Guid? UserId
        {
            get
            {
                var userIdClaim = User?.FindFirst(JwtClaims.USER_ID)?.Value;
                return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
            }
        }

        public string? UserEmail => User?.FindFirst(JwtClaims.EMAIL)?.Value;

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

        public IReadOnlyCollection<string> Roles
        {
            get
            {
                var user = _httpContextAccessor?.HttpContext?.User;

                if (user == null)
                    return Array.Empty<string>();

                return user.Claims
                    .Where(c => c.Type == JwtClaims.ROLE)
                    .Select(c => c.Value)
                    .Distinct()
                    .ToArray();
            }
        }

        public bool IsInRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role)) 
                return false;

            var user = _httpContextAccessor.HttpContext?.User;

            return user?.IsInRole(role) ?? false;
        }
    }
}
