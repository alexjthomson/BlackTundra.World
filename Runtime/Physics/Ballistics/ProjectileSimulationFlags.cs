using System;

namespace BlackTundra.World.Ballistics {

    /// <summary>
    /// Flags that can be set to toggle features of <see cref="Projectile"/> simulation on or off.
    /// </summary>
    [Flags]
    public enum ProjectileSimulationFlags : int {

        /// <summary>
        /// No flags, this will simulate a projectile as a ray that travels in a straight line.
        /// </summary>
        None = 0,

        /// <summary>
        /// The projectile is affected by gravity.
        /// </summary>
        Gravity = 1,

        /// <summary>
        /// The projectile will use a sphere-cast instead of a raycast.
        /// </summary>
        /// <remarks>
        /// This makes simulating the <see cref="Projectile"/> much more expensive.
        /// </remarks>
        SphereCast = 2,

        /// <summary>
        /// The projectile is affected by environmental drag.
        /// </summary>
        EnvironmentalDrag = 4,

        /// <summary>
        /// The projectile is affected by environmental forces.
        /// </summary>
        EnvironmentalForce = 8,

        /// <summary>
        /// The projectile can penetrate objects.
        /// </summary>
        Penetrate = 16,

        /// <summary>
        /// The projectile can ricochet off objects.
        /// </summary>
        Ricochet = 32,

        /// <summary>
        /// The projectile can transfer momentum to <see cref="UnityEngine.Rigidbody"/> objects that are struck.
        /// </summary>
        TransferMomentum = 64

    }

}