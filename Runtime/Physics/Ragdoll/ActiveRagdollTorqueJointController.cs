using BlackTundra.Foundation.Utility;

using UnityEngine;

namespace BlackTundra.World.Ragdoll {

    /// <summary>
    /// Controls an active ragdoll torque-based physics joint. The <see cref="ActiveRagdollTorqueJointController"/> aims to emulate a foreign
    /// <see cref="Transform"/> component local rotation on an identical skeletal structure to the ragdoll. This controller will not work
    /// on a skeleton that doesn't share the exact heirarchical structure of the ragdoll.
    /// </summary>
#if UNITY_EDITOR
    [AddComponentMenu(menuName: "Physics/Ragdoll/Active Ragdoll/Active Ragdoll Joint (Torque)", order: 0)]
#endif
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ActiveRagdollTorqueJointController : MonoBehaviour {

        #region constant

        #endregion

        #region variable

        /// <summary>
        /// Foreign <see cref="Transform"/> component to emulate the local rotation of.
        /// </summary>
#if UNITY_EDITOR
        [Tooltip("Transform component that the active ragdoll controller will aim to emulate. The controller will use the local rotation of the target transform; therefore, the target transform should be part of a skeletal structure that matches the active ragdoll skeletal structure.")]
#endif
        [SerializeField]
        private Transform targetTransform = null;

        /// <summary>
        /// Maximum torque that can be exerted upon the <see cref="rigidbody"/> specifically by the <see cref="ActiveRagdollTorqueJointController"/>.
        /// </summary>
#if UNITY_EDITOR
        [Tooltip("Maximum torque that can be exerted to emulate the target transform component.")]
        [Min(0.0f)]
#endif
        [SerializeField]
        private float maxTorque = 500.0f;

        /// <summary>
        /// Maximum angular velocity that can be applied to the <see cref="rigidbody"/> before the <see cref="ActiveRagdollTorqueJointController"/> will attempt to stabilize the <see cref="rigidbody"/>.
        /// </summary>
#if UNITY_EDITOR
        [Tooltip("Maximum angular velocity that the connected rigidbody can have before the active ragdoll joint controller will attempt to stabilize the joint.")]
        [Min(0.0f)]
#endif
        [SerializeField]
        private float maxAngularVelocity = 25.0f;

        /// <summary>
        /// Coefficient used to convert the difference between the ragdoll joint local rotation and the <see cref="targetTransform"/> local rotation into a target angular velocity.
        /// </summary>
#if UNITY_EDITOR
        [Tooltip("Coefficient used to scale the difference in rotation between the current ragdoll joint and the target joint when converting into a target angular velocity.")]
        [Min(0.0f)]
#endif
        [SerializeField]
        private float reactionCoefficient = 1.0f;

        /// <summary>
        /// <see cref="Rigidbody"/> component attached to the same <see cref="GameObject"/> as the <see cref="ActiveRagdollTorqueJointController"/>.
        /// </summary>
#if UNITY_EDITOR
        new
#endif
        private Rigidbody rigidbody = null;

        #endregion

        #region property

        #endregion

        #region logic

        #region Awake

        private void Awake() {
            // get references:
            rigidbody = GetComponent<Rigidbody>();
        }

        #endregion

        #region OnEnable

        private void OnEnable() {
            // sanitize variables:
            if (targetTransform == null || rigidbody == null) {
                enabled = false;
                return;
            }
            if (!float.IsNormal(maxTorque)) {
                maxTorque = 0.0f;
            }
        }

        #endregion

        #region FixedUpdate

        private void FixedUpdate() {
            // calculate timing:
            float deltaTime = Time.fixedDeltaTime;
            // get current angular velocity:
            Vector3 currentAngularVelocity = transform.InverseTransformDirection(rigidbody.angularVelocity); // convert current angular velocity into local space
            // stabilize (clamp) angular velocity:
            float sqrCurrentAngularVelocity = currentAngularVelocity.sqrMagnitude;
            if (sqrCurrentAngularVelocity > maxAngularVelocity * maxAngularVelocity) {
                currentAngularVelocity *= maxAngularVelocity / Mathf.Sqrt(sqrCurrentAngularVelocity);
                Vector3 newCurrentAngularVelocity = transform.TransformDirection(currentAngularVelocity);
                rigidbody.angularVelocity = newCurrentAngularVelocity;
            }
            // calculate delta rotation:
            Quaternion currentRotation = transform.localRotation;
            Quaternion targetRotation = targetTransform.localRotation;
            Quaternion deltaRotation = Quaternion.Inverse(currentRotation) * targetRotation; // current -> target
            // calculate target angular velocity:
            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis); // convert delta rotation to an axis and an angle (in degrees) about the axis that it is rotated about
            Vector3 angularDisplacement = axis * (angle * Mathf.Deg2Rad);
            Vector3 targetAngularVelocity = angularDisplacement * (reactionCoefficient / deltaTime);
            // calculate difference between current angular velocity and target angular velocity:
            Vector3 deltaAngularVelocity = targetAngularVelocity - currentAngularVelocity; // current -> target in rad/s
            // sanitize:
            if (deltaAngularVelocity.IsNaN()) return; // do not put the joint into an unstable state
            // clamp delta angular velocity:
            float sqrDeltaAngularVelocity = deltaAngularVelocity.sqrMagnitude;
            if (sqrDeltaAngularVelocity > maxTorque * maxTorque) {
                deltaAngularVelocity *= maxTorque / Mathf.Sqrt(sqrDeltaAngularVelocity);
            }
            // apply delta angular velocity:
            rigidbody.AddRelativeTorque(deltaAngularVelocity, ForceMode.Force);
        }

        #endregion

        #endregion

    }

}