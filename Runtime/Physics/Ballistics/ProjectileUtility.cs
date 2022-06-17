using System;

namespace BlackTundra.World.Ballistics {

    /// <summary>
    /// Implements utility methods related to projectiles.
    /// </summary>
    public static class ProjectileUtility {

        #region logic

        #region ToDamageType

        /// <summary>
        /// Converts a <see cref="ProjectileHitType"/> to a <see cref="DamageType"/>.
        /// </summary>
        public static DamageType ToDamageType(this ProjectileHitType hitType) {
            return hitType switch {
                ProjectileHitType.PenetrateFull => DamageType.Piercing,
                ProjectileHitType.PenetratePartial => DamageType.Piercing,
                ProjectileHitType.Disintegrate => DamageType.BluntImpact,
                ProjectileHitType.Ricochet => DamageType.Slashing,
                _ => throw new NotSupportedException($"Projectile type `{hitType}` has no conversion into a type of damage.")
            };
        }

        #endregion

        #endregion

    }

}