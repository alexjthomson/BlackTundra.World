namespace BlackTundra.World.Ballistics {

    /// <summary>
    /// Describes how a projectile hit an object.
    /// </summary>
    public enum ProjectileHitType : int {

        /// <summary>
        /// The projectile fully penetrated the object.
        /// </summary>
        PenetrateFull = 1,

        /// <summary>
        /// The projectile partially penetrated the object but remains lodged into the object.
        /// </summary>
        PenetratePartial = 2,

        /// <summary>
        /// The projectile struck the object and disintegrated upon impact.
        /// </summary>
        Disintegrate = 4,

        /// <summary>
        /// The projectile struck the object and richoet off of the surface of the object.
        /// </summary>
        Ricochet = 8

    }

}