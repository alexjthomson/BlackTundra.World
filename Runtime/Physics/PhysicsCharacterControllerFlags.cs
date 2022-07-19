using System;

namespace BlackTundra.World {

    /// <summary>
    /// A set of flags that toggles features of the <see cref="PhysicsCharacterController"/> simulation.
    /// </summary>
    [Flags]
    [Serializable]
    public enum PhysicsCharacterControllerFlags : int {
        
        /// <summary>
        /// When set, the <see cref="PhysicsCharacterController"/> will use gravity in it's simulation.
        /// </summary>
        SimulateGravity = 1,

        /// <summary>
        /// When set, the <see cref="PhysicsCharacterController"/> will apply a downwards force when grounded to accellerate the controller into the ground.
        /// This helps stick the controller to the ground when moving down a slope.
        /// </summary>
        UseStickForce = 2,

        /// <summary>
        /// When set, the resistance of the fluid that the <see cref="PhysicsCharacterController"/> is moving within is taken into account and a resistive
        /// drag force is applied.
        /// </summary>
        SimulateDrag = 4,

        /// <summary>
        /// When set, environmental wind forces are applied to the <see cref="PhysicsCharacterController"/>.
        /// </summary>
        SimulateWind = 8,

    }

}