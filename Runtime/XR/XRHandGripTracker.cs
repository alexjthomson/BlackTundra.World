#if USE_XR_TOOLKIT

using UnityEngine;

namespace BlackTundra.World.XR {

    /// <summary>
    /// Tracks a <see cref="Collider"/> that an <see cref="XRHandController"/> is gripping.
    /// </summary>
    internal sealed class XRHandGripTracker {

        #region variable

        /// <summary>
        /// <see cref="Transform"/> component being gripped.
        /// </summary>
        internal readonly Transform transform;

        /// <summary>
        /// <see cref="IPhysicsObject"/> that has been gripped.
        /// </summary>
        internal readonly IPhysicsObject physicsObject;

        /// <summary>
        /// <see cref="Rigidbody"/> that has been gripped.
        /// </summary>
        internal readonly Rigidbody rigidbody;

        /// <summary>
        /// Grip point location relative to the <see cref="transform"/>.
        /// </summary>
        internal readonly Vector3 localGripPoint;

        #endregion

        #region constructor

        internal XRHandGripTracker(in Collider collider, in Vector3 gripPoint) {
            physicsObject = collider.GetComponentInParent<IPhysicsObject>();
            if (physicsObject != null) {
                transform = ((Component)physicsObject).transform;
            } else {
                rigidbody = collider.GetComponentInParent<Rigidbody>();
                if (rigidbody != null) {
                    transform = rigidbody.transform;
                } else {
                    transform = collider.transform;
                }
            }
            //localGripPoint = transform.InverseTransformPoint(hit.point);
            localGripPoint = transform.InverseTransformPoint(gripPoint);
        }

        #endregion

        #region logic

        internal Vector3 GetWorldGripPoint() => transform.TransformPoint(localGripPoint);

        internal void AddForce(in Vector3 force, in ForceMode forceMode) {
            if (physicsObject != null) physicsObject.AddForce(force, forceMode);
            if (rigidbody != null) rigidbody.AddForce(force, forceMode);
        }

        internal void AddForceAtPosition(in Vector3 force, in Vector3 position, in ForceMode forceMode) {
            if (physicsObject != null) physicsObject.AddForceAtPosition(force, position, forceMode);
            if (rigidbody != null) rigidbody.AddForceAtPosition(force, position, forceMode);
        }

        #endregion

    }

}

#endif