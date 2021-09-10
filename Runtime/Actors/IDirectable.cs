using UnityEngine;

namespace BlackTundra.World.Actors {

    /// <summary>
    /// Marks an object as directable. This means the object can be directed to a position in
    /// the world.
    /// </summary>
    public interface IDirectable {

        /// <summary>
        /// Target position for the object to navigate to in world space.
        /// </summary>
        Vector3 TargetPosition { get; set; }

    }

}