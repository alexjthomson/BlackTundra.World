#if USE_XR_TOOLKIT

using BlackTundra.Foundation.Utility;

using UnityEngine;

namespace BlackTundra.World.XR.Experimental.Tracking {

    /// <summary>
    /// XR tracker that directly tracks an XR object.
    /// </summary>
#if UNITY_EDITOR
    [AddComponentMenu(menuName: "XR/Tracking/XR Tracker (Rigidbody)", order: 101)]
#endif
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class XRRigidbodyTracker : MonoBehaviour, IXRTracker, IPhysicsObject {

        #region variable

        #region configuration

#if UNITY_EDITOR
        [Header("Position Tracking")]
#endif

        /// <summary>
        /// Maximum distance between the current position and the target position before the <see cref="XRRigidbodyTracker"/> will be teleported to the target location.
        /// </summary>
#if UNITY_EDITOR
        [Tooltip("Maximum distance between the current position and the target position before the tracker will be teleported to the target location.")]
        [Min(0.0f)]
#endif
        [SerializeField]
        protected float teleportDistance = 0.75f;

        /// <summary>
        /// Scalar used for velocity-based tracking.
        /// </summary>
#if UNITY_EDITOR
        [Tooltip("Scalar used for velocity-based tracking.")]
        [Min(0.0f)]
#endif
        [SerializeField]
        protected float velocityScale = 1.0f;

#if UNITY_EDITOR
        [Header("Rotational Tracking")]
#endif

        /// <summary>
        /// Scalar used for angular-velocity-based tracking.
        /// </summary>
#if UNITY_EDITOR
        [Tooltip("Scalar used for angular-velocity-based tracking.")]
        [Min(0.0f)]
#endif
        [SerializeField]
        protected float angularVelocityScale = 1.0f;

        #endregion

        #region tracking

        /// <inheritdoc cref="TrackerPosition"/>
        protected Vector3 _trackerPosition = Vector3.zero;

        /// <inheritdoc cref="TrackerRotation"/>
        protected Quaternion _trackerRotation = Quaternion.identity;

        #endregion

        #region visual interpolation

        /// <summary>
        /// Actual position of the <see cref="XRRigidbodyTracker"/>.
        /// </summary>
        private Vector3 actualPosition = Vector3.zero;

        /// <summary>
        /// Actual rotation of the <see cref="XRRigidbodyTracker"/>.
        /// </summary>
        private Quaternion actualRotation = Quaternion.identity;

        private Vector3 lastPosition = Vector3.zero, nextPosition = Vector3.zero;
        private Quaternion lastRotation = Quaternion.identity, nextRotation = Quaternion.identity;
        private float lerpProgress = 0.0f;
        private float lerpCoefficient = 0.0f;

        #endregion

        #region references

        /// <summary>
        /// <see cref="Rigidbody"/> component attached to the same object as the <see cref="XRRigidbodyTracker"/>.
        /// </summary>
#if UNITY_EDITOR
        new
#endif
        protected Rigidbody rigidbody = null;

        /// <summary>
        /// Parent <see cref="IPhysicsObject"/>.
        /// </summary>
        protected IPhysicsObject parentPhysicsObject = null;

        #endregion

        #endregion

        #region property

        /// <summary>
        /// World-space tracker position.
        /// </summary>
        public virtual Vector3 TrackerPosition {
            get => _trackerPosition;
            set => _trackerPosition = value;
        }

        /// <summary>
        /// World-space tracker rotation.
        /// </summary>
        public virtual Quaternion TrackerRotation {
            get => _trackerRotation;
            set => _trackerRotation = value;
        }

        public Vector3 velocity => rigidbody.velocity;

        public Vector3 position => rigidbody.position;

        public Quaternion rotation => rigidbody.rotation;

        public Vector3 centreOfMass => rigidbody.centerOfMass;

        public float mass => rigidbody.mass;

        #endregion

        #region logic

        #region Awake

        protected virtual void Awake() {
            rigidbody = GetComponent<Rigidbody>();
            parentPhysicsObject = GetComponentInParent<IPhysicsObject>();
        }

        #endregion

        #region OnEnable

        protected virtual void OnEnable() {
            // rigidbody check:
            if (rigidbody == null) {
                enabled = false;
                return;
            }
            // teleport to tracker:
            TeleportToTracker();
        }

        #endregion

        #region TeleportToTracker

        /// <summary>
        /// Teleports the <see cref="XRRigidbodyTracker"/> to the tracker position and rotation.
        /// </summary>
        protected virtual void TeleportToTracker() {
            rigidbody.position = _trackerPosition;
            rigidbody.rotation = _trackerRotation;
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            actualPosition = _trackerPosition;
            actualRotation = _trackerRotation;
            ResetVisuals();
        }

        #endregion

        #region ResetVisuals

        /// <summary>
        /// Resets any visual variables.
        /// </summary>
        protected virtual void ResetVisuals() {
            Transform parent = transform.parent;
            nextPosition = parent.ToLocalPoint(actualPosition);
            nextRotation = parent.ToLocalRotation(actualRotation);
            lastPosition = nextPosition;
            lastRotation = nextRotation;
            lerpProgress = 0.0f;
            lerpCoefficient = 0.0f;
        }

        #endregion

        #region UpdateVisualTargets

        protected virtual void UpdateVisualTargets() {
            Transform parent = transform.parent;
            lastPosition = nextPosition;
            lastRotation = nextRotation;
            nextPosition = parent.ToLocalPoint(actualPosition);
            nextRotation = parent.ToLocalRotation(actualRotation);
            lerpProgress = 0.0f;
            lerpCoefficient = 1.0f / Time.fixedDeltaTime;
        }

        #endregion

        #region LateUpdate

        protected virtual void LateUpdate() {
            // interpolate:
            if (1 == 1) return;
            if (lerpProgress != 1.0f) {
                // increase lerp value:
                float deltaTime = Time.deltaTime;
                lerpProgress += deltaTime * lerpCoefficient;
                if (lerpProgress > 1.0f) {
                    lerpProgress = 1.0f;
                }
                // apply lerp:
                Vector3 currentPosition = Vector3.LerpUnclamped(lastPosition, nextPosition, lerpProgress);
                Quaternion currentRotation = Quaternion.LerpUnclamped(lastRotation, nextRotation, lerpProgress);
                // calculate world-space positions:
                Transform parent = transform.parent;
                transform.SetPositionAndRotation(parent.ToWorldPoint(currentPosition), parent.ToWorldRotation(currentRotation));
            }
        }

        #endregion

        #region OnPostRender

        protected virtual void OnPostRender() {
            // reset the transform position to the actual position and rotation after each frame:
            transform.SetPositionAndRotation(
                actualPosition,
                actualRotation
            );
        }

        #endregion

        #region FixedUpdate

        protected virtual void FixedUpdate() {
            // get time information:
            float deltaTime = Time.fixedDeltaTime;
            float inverseDeltaTime = 1.0f / deltaTime;
            // calculate positional data:
            actualPosition = rigidbody.position;
            Vector3 deltaPosition = _trackerPosition - actualPosition;
            // distance check:
            float deltaPositionSqrLength = deltaPosition.sqrMagnitude;
            if (deltaPositionSqrLength > teleportDistance * teleportDistance) { // greater than teleport distance
                TeleportToTracker();
            } else { // tracker is within the teleport distance:
                // velocity tracking:
                Vector3 velocity = deltaPosition * (velocityScale * inverseDeltaTime);
                if (!float.IsNaN(velocity.x)) {
                    rigidbody.velocity = velocity;
                }
                // angular velocity tracking:
                Quaternion actualRotation = rigidbody.rotation;
                Quaternion deltaRotation = _trackerRotation * Quaternion.Inverse(actualRotation);
                deltaRotation.ToAngleAxis(out float angleDegrees, out Vector3 axis);

                //Mathf.SmoothDampAngle();
                if (angleDegrees > 180.0f) angleDegrees -= 360.0f;
                if (Mathf.Abs(angleDegrees) > Mathf.Epsilon) {
                    Vector3 angularVelocity = axis * (angleDegrees * Mathf.Deg2Rad * inverseDeltaTime);
                    if (!float.IsNaN(angularVelocity.x)) {
                        rigidbody.angularVelocity = angularVelocity * angularVelocityScale;
                    }
                }
                //rigidbody.MoveRotation(_trackerRotation);
                // TODO: add reaction forces here (forces applied to the parent IPhysicsObject when pushing against an object)
            }
            // record positional and rotation restore points:
            actualPosition = rigidbody.position;
            actualRotation = rigidbody.rotation;
            // update visuals:
            UpdateVisualTargets();
        }

        #endregion

        #region AddForce

        public virtual void AddForce(in Vector3 force, in ForceMode forceMode) {
            if (rigidbody == null) return;
            rigidbody.AddForce(force, forceMode);
        }

        #endregion

        #region AddForceAtPosition

        public virtual void AddForceAtPosition(in Vector3 force, in Vector3 position, in ForceMode forceMode) {
            if (rigidbody == null) return;
            rigidbody.AddForceAtPosition(force, position, forceMode);
        }

        #endregion

        #region AddExplosionForce

        public virtual void AddExplosionForce(in float force, in Vector3 point, in float radius, float upwardsModifier, in ForceMode forceMode) {
            if (rigidbody == null) return;
            rigidbody.AddExplosionForce(force, point, radius, upwardsModifier, forceMode);
        }

        #endregion

        #endregion

    }

}

#endif