using System;

namespace BlackTundra.World.Ballistics {

    /// <summary>
    /// Describes how a projectile hit an object.
    /// </summary>
    [Serializable]
    public enum ProjectileHitType : byte {

        /// <summary>
        /// The projectile fully penetrated the object.
        /// </summary>
        PenetrateFull = 0x01,

        /// <summary>
        /// The projectile partially penetrated the object but remains lodged into the object.
        /// </summary>
        PenetratePartial = 0x02,

        /// <summary>
        /// The projectile struck the object and disintegrated upon impact.
        /// </summary>
        Disintegrate = 0x04,

        /// <summary>
        /// The projectile struck the object and richoet off of the surface of the object.
        /// </summary>
        Ricochet = 0x08

    }

}