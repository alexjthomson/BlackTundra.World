#if USE_XR_TOOLKIT

using BlackTundra.Foundation.Utility;

using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackTundra.World.XR.Locomotion {

    public sealed class XRContinuousMoveController : XRMoveController {

        #region constant

        /// <summary>
        /// If the the <see cref="controller"/> heights drops below this threshold, the move speed will be reduced relative to how
        /// much it has fallen below the threshold by.
        /// </summary>
        private const float HeightMoveSpeedDamperThreshold = 1.4f;

        private const float SprintSmoothing = 10.0f;

        private const float StrafeSpeedCoefficient = 0.7f;

        #endregion

        #region variable

        private readonly InputAction moveAction;
        private Vector2 inputMove;

        private readonly InputAction jumpAction;
        private float inputJump;

        private readonly InputAction sprintAction;
        private float inputSprint;

        private SmoothFloat sprintCoefficient;

        #endregion

        #region constructor

        internal XRContinuousMoveController(in XRLocomotionController locomotion) : base(locomotion) {
            moveAction = locomotion.inputMoveAction;
            jumpAction = locomotion.inputJumpAction;
            sprintAction = locomotion.inputSprintAction;
            sprintCoefficient = 0.0f;
        }

        #endregion

        #region logic

        #region Update

        protected internal sealed override void Update(in float deltaTime) {
            inputMove = moveAction.ReadValue<Vector2>();
            inputJump = jumpAction.ReadValue<float>();
            inputSprint = sprintAction.ReadValue<float>();
        }

        #endregion

        #region FixedUpdate

        protected internal sealed override void FixedUpdate(in float deltaTime) {
            // height:
            float heightCoefficient = Mathf.Lerp(locomotion.minHeight, HeightMoveSpeedDamperThreshold, locomotion.height) / HeightMoveSpeedDamperThreshold;
            heightCoefficient = Mathf.Clamp(heightCoefficient * heightCoefficient, 0.1f, 1.0f);
            // movement speed:
            float movementSpeed = heightCoefficient * XRLocomotionController._continuousMoveBaseSpeed;
            // sprint:
            sprintCoefficient.Apply(inputSprint, SprintSmoothing * deltaTime);
            float sprintValue = sprintCoefficient.value;
            if (sprintValue > 0.0f) {
                float sprintCoefficient = Mathf.Lerp(1.0f, XRLocomotionController._continuousMoveSprintCoefficient, sprintValue);
                movementSpeed *= sprintCoefficient;
            }
            // base movement:
            Vector3 moveVelocity = new Vector3(
                inputMove.x * movementSpeed * StrafeSpeedCoefficient,
                0.0f,
                inputMove.y * movementSpeed
            );
            // set move velocity:
            locomotion.SetMoveVelocity(moveVelocity);
        }

        #endregion

        #endregion

    }

}

#endif