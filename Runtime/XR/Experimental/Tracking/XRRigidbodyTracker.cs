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
        /// Actual position of the <see cref="transform"/> before a visual update (to make the tracking look smoother).
        /// </summary>
        protected Vector3 actualPositionRestore = Vector3.zero;

        /// <summary>
        /// Actual rotation of the <see cref="transform"/> before a visual update (to make the tracking look smoother).
        /// </summary>
        protected Quaternion actualRotationRestore = Quaternion.identity;

        /// <summary>
        /// Visual position of the tracker.
        /// </summary>
        protected Vector3 visualPosition = Vector3.zero;

        /// <summary>
        /// Visual rotation of the tracker.
        /// </summary>
        protected Quaternion visualRotation = Quaternion.identity;

        /// <summary>
        /// Last visual position of the tracker.
        /// </summary>
        protected Vector3 lastVisualPositionSnapshot = Vector3.zero;

        /// <summary>
        /// Last visual rotation of the tracker.
        /// </summary>
        protected Quaternion lastVisualRotationSnapshot = Quaternion.identity;

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
            actualPositionRestore = _trackerPosition;
            actualRotationRestore = _trackerRotation;
            ResetVisuals();
        }

        #endregion

        #region ResetVisuals

        /// <summary>
        /// Resets any visual variables.
        /// </summary>
        protected virtual void ResetVisuals() {
            visualPosition = actualPositionRestore;
            visualRotation = ToLocalRotation(actualRotationRestore);
            lastVisualPositionSnapshot = _trackerPosition;
            lastVisualRotationSnapshot = ToLocalRotation(_trackerRotation);
        }

        #endregion

        #region LateUpdate

        protected virtual void LateUpdate() {
            UpdateVisualPositionalOffset();
            UpdateVisualRotationalOffset();
            transform.position = visualPosition;
            //transform.localRotation = visualRotation;
        }

        #endregion

        #region UpdateVisualPositionalOffset

        protected virtual void UpdateVisualPositionalOffset() {
            // calculate delta position:
            Vector3 deltaTrackerPosition = _trackerPosition - lastVisualPositionSnapshot;
            lastVisualPositionSnapshot = _trackerPosition;
            // apply delta position to visual offset:
            visualPosition += deltaTrackerPosition;
        }

        #endregion

        #region ToLocalRotation

        protected Quaternion ToLocalRotation(in Quaternion rotation) {
            Transform parentTransform = transform.parent;
            if (parentTransform != null) {
                return Quaternion.Inverse(parentTransform.rotation) * rotation;
            }
            return rotation;
        }

        #endregion

        #region UpdateVisualRotationalOffset

        protected virtual void UpdateVisualRotationalOffset() {
            // calculate delta rotation:
            Quaternion localTrackerRotation = ToLocalRotation(_trackerRotation);
            Quaternion deltaTrackerRotation = localTrackerRotation * Quaternion.Inverse(lastVisualRotationSnapshot);
            lastVisualRotationSnapshot = localTrackerRotation;
            // apply delta rotation to visual offset:
            visualRotation *= deltaTrackerRotation;
        }

        #endregion

        #region OnPostRender

        protected virtual void OnPostRender() {
            // reset the transform position to the actual position and rotation after each frame:
            transform.SetPositionAndRotation(
                actualPositionRestore,
                actualRotationRestore
            );
        }

        #endregion

        #region FixedUpdate

        protected virtual void FixedUpdate() {
            // get time information:
            float deltaTime = Time.fixedDeltaTime;
            float inverseDeltaTime = 1.0f / deltaTime;
            // calculate positional data:
            Vector3 actualPosition = rigidbody.position;
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
                if (angleDegrees > 180.0f) angleDegrees -= 360.0f;
                if (Mathf.Abs(angleDegrees) > Mathf.Epsilon) {
                    Vector3 angularVelocity = axis * (angleDegrees * Mathf.Deg2Rad * inverseDeltaTime);
                    if (!float.IsNaN(angularVelocity.x)) {
                        rigidbody.angularVelocity = angularVelocity * angularVelocityScale;
                    }
                }
                // TODO: add reaction forces here (forces applied to the parent IPhysicsObject when pushing against an object)
            }
            // record positional and rotation restore points:
            actualPositionRestore = rigidbody.position;
            actualRotationRestore = rigidbody.rotation;
            // reset visuals:
            ResetVisuals();
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