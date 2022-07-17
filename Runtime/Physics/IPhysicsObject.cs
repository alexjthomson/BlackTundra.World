using UnityEngine;

namespace BlackTundra.World {

    /// <summary>
    /// Implements basic physics properties and methods on an object. This allows the object to be treated as a physics-based object.
    /// This is useful for custom objects that do not require / should not use a <see cref="Rigidbody"/> component.
    /// </summary>
    public interface IPhysicsObject {

        /// <summary>
        /// World-space velocity of the object.
        /// </summary>
        Vector3 velocity { get; }

        /// <summary>
        /// World-space position of the object.
        /// </summary>
        Vector3 position { get; }

        /// <summary>
        /// World-space rotation of the object.
        /// </summary>
        Quaternion rotation { get; }

        /// <summary>
        /// Local-space centre of mass of the object relative to the object.
        /// </summary>
        Vector3 centreOfMass { get; }

        /// <summary>
        /// Mass of the object in kilograms.
        /// </summary>
        float mass { get; }

        /// <summary>
        /// Adds a <paramref name="force"/> to the object.
        /// </summary>
        /// <param name="force">World-space force vector.</param>
        /// <param name="forceMode">Type of force to apply.</param>
        void AddForce(
            in Vector3 force,
            in ForceMode forceMode
        );

        /// <summary>
        /// Applies a <paramref name="force"/> at a specific <paramref name="position"/> on an object.
        /// </summary>
        /// <param name="force">World-space force vector.</param>
        /// <param name="position">World-space position to apply the force at.</param>
        /// <param name="forceMode">Type of force to apply.</param>
        void AddForceAtPosition(
            in Vector3 force,
            in Vector3 position,
            in ForceMode forceMode
        );

        /// <summary>
        /// Applies a force to the object that simulates an explosion.
        /// </summary>
        /// <param name="explosionForce">The maximum magnitude of the force that can be applied by the explosion.</param>
        /// <param name="explosionPosition">World-space position of the centre of the explosion.</param>
        /// <param name="explosionRadius">World-space radius of the sphere within which the explosion has its effect.</param>
        /// <param name="upwardsModifier">Adjustment to the apparent position of the explosion to make it seem to lift objects.</param>
        /// <param name="forceMode">The method used to apply the force to the object.</param>
        void AddExplosionForce(
            in float explosionForce,
            in Vector3 explosionPosition,
            in float explosionRadius,
            float upwardsModifier,
            in ForceMode forceMode
        );

    }

}