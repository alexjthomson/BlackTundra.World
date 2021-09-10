namespace BlackTundra.WorldSystem.Paths {

    /// <summary>
    /// Describes the constaints control points in a path follow.
    /// </summary>
    public enum ControlPointConstraints {

        /// <summary>
        /// Control points have no constraints.
        /// </summary>
        None,

        /// <summary>
        /// Control points stay in a straight line around their anchor point.
        /// </summary>
        Aligned,

        /// <summary>
        /// Control points stay in a straight line, equidistant from their anchor point.
        /// </summary>
        Mirrored,

        /// <summary>
        /// Control points are placed automatically.
        /// </summary>
        Automatic

    }

}