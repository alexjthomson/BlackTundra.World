using UnityEngine;

namespace BlackTundra.World {

    public interface IPhysicsObject {

        Vector3 velocity { get; }

        Vector3 position { get; }

        Quaternion rotation { get; }

        Vector3 centreOfMass { get; }

        float mass { get; }

        void AddForce(in Vector3 force, in ForceMode forceMode);

        void AddForceAtPosition(in Vector3 force, in Vector3 position, in ForceMode forceMode);

        void AddExplosionForce(in float force, in Vector3 point, in float radius, float upwardsModifier, in ForceMode forceMode);

    }

}