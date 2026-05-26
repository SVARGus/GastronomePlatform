using GastronomePlatform.Common.Domain.Constants;
using GastronomePlatform.Modules.Dishes.Domain.Enums;

namespace GastronomePlatform.Modules.Dishes.Application.Helpers
{
    /// <summary>
    /// Резолвер денормализованного поля <see cref="OwnerType"/> блюда на основе
    /// ролей текущего пользователя.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Используется в Application Handler-ах, которые задают или обновляют
    /// <see cref="OwnerType"/> (UC-DSH-001, UC-DSH-002 и др.). Логика вынесена
    /// в один helper, чтобы при изменении правила (например, приоритета ролей
    /// или добавлении новой роли) поменять её в одном месте.
    /// </para>
    /// <para>
    /// Приоритет ролей: <see cref="OwnerType.Restaurant"/> &gt;
    /// <see cref="OwnerType.Chef"/> &gt; <see cref="OwnerType.User"/>.
    /// Если пользователь одновременно <c>Restaurant</c> и <c>Chef</c> —
    /// побеждает <c>Restaurant</c> как более «институциональный» владелец.
    /// </para>
    /// </remarks>
    internal static class OwnerTypeResolver
    {
        /// <summary>
        /// Определяет <see cref="OwnerType"/> по набору ролей пользователя.
        /// </summary>
        /// <param name="roles">Список ролей текущего пользователя (из JWT claims).</param>
        /// <returns>
        /// <see cref="OwnerType.Restaurant"/>, если есть роль
        /// <see cref="PlatformRoles.RESTAURANT"/>; иначе
        /// <see cref="OwnerType.Chef"/>, если есть <see cref="PlatformRoles.CHEF"/>;
        /// иначе <see cref="OwnerType.User"/> как fallback.
        /// </returns>
        public static OwnerType ResolveFromRoles(IReadOnlyCollection<string> roles)
        {
            if (roles.Contains(PlatformRoles.RESTAURANT))
            {
                return OwnerType.Restaurant;
            }

            if (roles.Contains(PlatformRoles.CHEF))
            {
                return OwnerType.Chef;
            }

            return OwnerType.User;
        }
    }
}
