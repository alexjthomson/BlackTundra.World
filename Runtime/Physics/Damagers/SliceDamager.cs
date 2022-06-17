using BlackTundra.Foundation.Utility;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace BlackTundra.World.Damagers {

    /// <summary>
    /// Slice damanger.
    /// </summary>
#if UNITY_EDITOR
    [AddComponentMenu(menuName: "Physics/Damager/Slice Damager", order: 101)]
#endif
    [DisallowMultipleComponent]
    public sealed class SliceDamager : BaseDamager {

        #region nested

        /// <summary>
        /// Contains slice data for a <see cref="Collider"/>.
        /// </summary>
        private struct ColliderSliceData {

            #region variable

            /// <summary>
            /// <see cref="Collider"/> that was struck.
            /// </summary>
            internal readonly Collider collider;

            /// <summary>
            /// <see cref="MaterialDescriptor"/> that describes the <see cref="Collider"/> that was struck.
            /// </summary>
            internal readonly MaterialDescriptor materialDescriptor;

            /// <summary>
            /// <see cref="IDamageable"/> component (if any) found on the <see cref="Collider"/>.
            /// </summary>
            internal IDamageable damageReceiver;

            /// <summary>
            /// Depth that the slice surface initially penetrated the <see cref="collider"/>.
            /// </summary>
            internal readonly float sliceDepth;

            /// <summary>
            /// Local point relative to the <see cref="collider"/> that describes where the slice started.
            /// </summary>
            internal readonly Vector3 localSlicePoint;

            /// <summary>
            /// Local direction of the slice relative to the <see cref="collider"/>.
            /// </summary>
            internal readonly Vector3 localSliceDirection;

            #endregion

            #region constructor

            internal ColliderSliceData(in Collider collider, in MaterialDescriptor materialDescriptor, in float sliceDepth, in Vector3 localSlicePoint, in Vector3 localSliceDirection) {
                if (collider == null) throw new ArgumentNullException(nameof(collider));
                if (materialDescriptor == null) throw new ArgumentNullException(nameof(materialDescriptor));
                if (sliceDepth < 0.0f) throw new ArgumentOutOfRangeException(nameof(sliceDepth));
                this.collider = collider;
                this.materialDescriptor = materialDescriptor;
                this.sliceDepth = sliceDepth;
                this.localSlicePoint = localSlicePoint;
                this.localSliceDirection = localSliceDirection;
                damageReceiver = collider.GetComponentInParent<IDamageable>();
            }

            #endregion

        }

        #endregion

        #region variable

        /// <summary>
        /// Local position that marks the starting position of the slicing area.
        /// On a sword, this would be where the blade meets the guard.
        /// </summary>
#if UNITY_EDITOR
        [Tooltip("Local position that marks the starting position of the slicing area. On a sword, this would be where the blade meets the guard.")]
#endif
        [SerializeField]
        private Vector3 sliceSurfaceStart = Vector3.zero;

        /// <summary>
        /// Local position that marks the end/tip position of the slicing area.
        /// On a sword, this would be at the tip of the blade.
        /// </summary>
#if UNITY_EDITOR
        [Tooltip("Local position that marks the end/tip position of the slicing area. On a sword, this would be at the tip of the blade.")]
#endif
        [SerializeField]
        private Vector3 sliceSurfaceEnd = Vector3.up;

        /// <summary>
        /// Local direction that the slicing surface would penetrate/strike an object.
        /// On a sword, this would point from the centre of the blade to the edge of the blade. It would be the direction of the flat
        /// area of the blade.
        /// </summary>
#if UNITY_EDITOR
        [Tooltip("Local direction that the slicing surface would penetrate/strike an object. On a sword, this would point from the centre of the blade to the edge of the blade. It would be the direction of the flat area of the blade.")]
#endif
        [SerializeField]
        private Vector3 sliceSurfaceDirection = Vector3.forward;

        /// <summary>
        /// Indicates if the <see cref="SliceDamager"/> is one or two sided.
        /// A one sided <see cref="SliceDamager"/> will only slice in the <see cref="sliceSurfaceDirection"/>.
        /// A two sided <see cref="SliceDamager"/> will slice in the <see cref="sliceSurfaceDirection"/> and also the opposite direction.
        /// </summary>
#if UNITY_EDITOR
        [Tooltip("Indicates if the slice damager is one or two sided. A one sided slice damager will only slice in the specified slice surface direction. A two sided slice damager will slice in the specified slice surface direction and also the opposite direction.")]
#endif
        [SerializeField]
        private bool doubleSided = false;

        /// <summary>
        /// Depth that the <see cref="SliceDamager"/> can penetrate an object in the <see cref="sliceSurfaceDirection"/> (or opposite direction if the damager is <see cref="doubleSided"/>).
        /// </summary>
#if UNITY_EDITOR
        [Tooltip("Depth that the slice damager can penetrate an object in the direction of the slice surface.")]
        [Min(0.0f)]
#endif
        [SerializeField]
        private float penetrationDepth = 0.1f;

        /// <summary>
        /// Minimum amount of force required to penetrate a surface.
        /// </summary>
#if UNITY_EDITOR
        [Tooltip("Minimum amount of force required to penetrate a surface.")]
        [Min(0.0f)]
#endif
        [SerializeField]
        private float thresholdPenetrationForce = 5.0f;

        /// <summary>
        /// Maximum amount of excess force after subtracting the <see cref="thresholdPenetrationForce"/> required to fully penetrate an object.
        /// </summary>
#if UNITY_EDITOR
        [Tooltip("Amount of excess force required after subtracting the threshold penetration force that's required to penetrate an object at the maximum penetration depth.")]
        [Min(0.0f)]
#endif
        [SerializeField]
        private float maxDepthPenetrationForce = 5.0f;

        /// <summary>
        /// Translates metres of penetration into damage. For every 1 metre, <see cref="penetrationDistanceToDamage"/> will be delt to the <see cref="IDamageable"/> object
        /// when initially penetrated.
        /// </summary>
#if UNITY_EDITOR
        [Tooltip("Translates metres of penetration into damage. For every 1 metre of penetration, this amount of damage will be delt to the penetrated object upon initial penetration.")]
        [Min(0.0f)]
#endif
        [SerializeField]
        private float penetrationDistanceToDamage = 10.0f;

        /// <summary>
        /// Translates metres of slicing into damage. For every 1 metre, <see cref="distanceToDamage"/> will be delt to the <see cref="IDamageable"/> object
        /// being sliced.
        /// </summary>
#if UNITY_EDITOR
        [Tooltip("Translates metres of slicing into damage. For every 1 metre of slicing, this amount of damage will be delt.")]
        [Min(0.0f)]
#endif
        [SerializeField]
        private float distanceToDamage = 0.1f;

        /// <summary>
        /// Set of <see cref="Collider"/> componets that have been hit by the <see cref="SliceDamager"/>.
        /// </summary>
        private Dictionary<Collider, ColliderSliceData> hitColliders = new Dictionary<Collider, ColliderSliceData>();

        /// <summary>
        /// World-space <see cref="sliceSurfaceStart"/>.
        /// </summary>
        private Vector3 _sliceSurfaceStart = Vector3.zero;

        /// <summary>
        /// World-space <see cref="sliceSurfaceEnd"/>.
        /// </summary>
        private Vector3 _sliceSurfaceEnd = Vector3.up;

        /// <summary>
        /// Normalized direction from <see cref="_sliceSurfaceStart"/> to <see cref="_sliceSurfaceEnd"/>.
        /// </summary>
        private Vector3 _sliceSurfaceStartToEndDirection = Vector3.up;

        /// <summary>
        /// Length of the slice surface.
        /// </summary>
        private float _sliceSurfaceLength = 1.0f;

        /// <summary>
        /// World-space normalized direction that the slice surface can penetrate an object in.
        /// </summary>
        private Vector3 _sliceSurfaceDirection = Vector3.forward;

        /// <summary>
        /// <see cref="Collider"/> components that are children of the <see cref="SliceDamager"/>.
        /// </summary>
        private Collider[] colliders = new Collider[0];

        #endregion

        #region logic

        #region OnAwake

        protected override void Awake() {
            base.Awake();
            colliders = UnityUtility.GetColliders(gameObject, false);
        }

        #endregion

        #region OnDrawGizmosSelected
#if UNITY_EDITOR

        private void OnDrawGizmosSelected() {
            // draw surface start and end:
            Gizmos.color = Color.cyan;
            Vector3 start = transform.TransformPoint(sliceSurfaceStart);
            Vector3 end = transform.TransformPoint(sliceSurfaceEnd);
            Gizmos.DrawWireSphere(start, 0.025f);
            Gizmos.DrawWireSphere(end, 0.025f);
            Gizmos.DrawLine(start, end);
            // draw surface direction:
            Gizmos.color = Color.green;
            Vector3 sliceSurfaceVector = transform.TransformDirection(sliceSurfaceDirection).normalized * 0.25f;
            Gizmos.DrawLine(start, start + sliceSurfaceVector);
            if (doubleSided) {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(start, start - sliceSurfaceVector);
            }
        }

#endif
        #endregion

        #region OnDamagerCollisionEnter

        public override void OnDamagerCollisionEnter(in Collision collision) {
            if (collision == null) return;
            // recalculate slice surface data:
            RecalculateSliceSurfaceData();
            // process contact points:
            ContactPoint[] contactPoints = collision.contacts;
            for (int i = contactPoints.Length - 1; i >= 0; i--) {
                ProcessContactPointHit(contactPoints[i]);
            }
        }

        #endregion

        #region OnDamagerCollisionExit

        public override void OnDamagerCollisionExit(in Collision collision) {
            if (collision == null) return;
            // process contact points:
            ContactPoint[] contactPoints = collision.contacts;
            for (int i = contactPoints.Length - 1; i >= 0; i--) {
                RemoveContactPoint(contactPoints[i]);
            }
        }

        #endregion

        #region RecalculateSliceSurfaceData

        /// <summary>
        /// Recalculates the data required to calculate if an object was struck by the <see cref="SliceDamager"/>.
        /// </summary>
        private void RecalculateSliceSurfaceData() {
            _sliceSurfaceStart = transform.TransformPoint(sliceSurfaceStart);
            _sliceSurfaceEnd = transform.TransformPoint(sliceSurfaceEnd);
            _sliceSurfaceStartToEndDirection = _sliceSurfaceEnd - _sliceSurfaceStart; // calculate world-space direction from the start to the end of the slice surface
            _sliceSurfaceLength = _sliceSurfaceStartToEndDirection.magnitude;
            _sliceSurfaceStartToEndDirection *= 1.0f / _sliceSurfaceLength; // normalize slice surface start to end direction
            _sliceSurfaceDirection = transform.TransformDirection(sliceSurfaceDirection).normalized;
        }

        #endregion

        #region ProcessContactPointHit

        private void ProcessContactPointHit(in ContactPoint contactPoint) {
            // get collider:
            Collider collider = contactPoint.otherCollider;
            if (collider == null) return;
            // try calculate slice data:
            if (!TryCalculateSliceData(contactPoint, out ColliderSliceData sliceData)) {
                hitColliders.Remove(collider);
                return;
            }
            // register collider with slice data:
            hitColliders[collider] = sliceData;
            // disable collision:
            colliders.SetCollisionStates(collider, false);
            // calulcate parenting:
            if (DamageController.HasOriginalParent) { // re-parent
                Transform newParent = collider.transform;
            }
            // calculate penetration damage:
            IDamageable damageReceiver = sliceData.damageReceiver;
            if (damageReceiver != null) {
                damageReceiver.OnDamage(
                    DamageController.sender,
                    penetrationDistanceToDamage * sliceData.sliceDepth,
                    DamageType.Piercing,
                    contactPoint.point,
                    -contactPoint.impulse,
                    null
                );
            }
        }

        #endregion

        #region TryCalculateSliceData

        /// <summary>
        /// Calculates <see cref="ColliderSliceData"/> based on the initial <paramref name="contactPoint"/> collision.
        /// </summary>
        /// <returns>
        /// Returns <c>true</c> if the <paramref name="contactPoint"/> was sliced.
        /// </returns>
        private bool TryCalculateSliceData(in ContactPoint contactPoint, out ColliderSliceData sliceData) {
            // process contact point:
            Vector3 localContactPoint = contactPoint.point - _sliceSurfaceStart; // get the contact point relative to the starting position of the slice surface
            float localContactDistance = Vector3.Dot(localContactPoint, _sliceSurfaceStartToEndDirection); // calculate how much the local contact point projects onto the slice surface start to end direction
            if (localContactDistance < 0.0f || localContactDistance > _sliceSurfaceLength) { // calculate if the contact point is beyond the slice surface start and end points and ignore the point if true
                sliceData = default;
                return false;
            }
            // process impact force:
            Vector3 impactForce = -contactPoint.impulse; // calculate the impact force
            float relativeImpactForceMagnitude = Vector3.Dot(impactForce, _sliceSurfaceDirection); // magnitude of the impact force that went into striking the object in the direction of the slice surface
            if (relativeImpactForceMagnitude < thresholdPenetrationForce) { // the slice does not have the required threshold force to initiate a slice
                sliceData = default;
                return false;
            }
            // calculate the slice depth:
            float sliceDepth = penetrationDepth * Mathf.Clamp01((relativeImpactForceMagnitude - thresholdPenetrationForce) / maxDepthPenetrationForce);
            // process collider:
            Collider collider = contactPoint.otherCollider;
            Transform colliderTransform = collider.transform;
            Vector3 localSlicePoint = colliderTransform.InverseTransformPoint(contactPoint.point); // calculate the local point relative to the hit collider that the slice started at
            Vector3 sliceUpDirection = Vector3.Cross(_sliceSurfaceDirection, _sliceSurfaceStartToEndDirection); // calculate a world-space upwards direction relative to the slice surface
            Vector3 sliceDirection = Vector3.Cross(sliceUpDirection, contactPoint.normal); // calculate the world-space slice direction
            Vector3 localSliceDirection = colliderTransform.InverseTransformDirection(sliceDirection).normalized; // calculate the local direction relative to the hit collider that the slice should go in
            PhysicMaterial physicMaterial = collider.material;
            MaterialDescriptor materialDescriptor = MaterialDatabase.GetMaterialDescriptor(physicMaterial);
            // create slice data:
            sliceData = new ColliderSliceData(
                collider,
                materialDescriptor,
                sliceDepth,
                localSlicePoint,
                localSliceDirection
            );
            return true;
        }

        #endregion

        #region RemoveContactPoint

        /// <summary>
        /// Removes a <see cref="ContactPoint"/> from being tracked.
        /// </summary>
        private void RemoveContactPoint(in ContactPoint contactPoint) {
            Collider collider = contactPoint.otherCollider;
            if (collider != null && hitColliders.Remove(collider)) {
                // enable collisions:
                colliders.SetCollisionStates(collider, true);
            }
        }

        #endregion

        #endregion

    }

}
