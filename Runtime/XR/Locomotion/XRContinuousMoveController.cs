#if USE_XR_TOOLKIT

using BlackTundra.Foundation.Utility;

using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackTundra.World.XR.Locomotion {

    public class XRContinuousMoveController : XRMovementProvider {

        #region constant

        /// <summary>
        /// If the the <see cref="controller"/> heights drops below this threshold, the move speed will be reduced relative to how
        /// much it has fallen below the threshold by.
        /// </summary>
        private const float HeightMoveSpeedDamperThreshold = 1.4f;

        private const float SprintSmoothing = 10.0f;

        private const float StrafeSpeedCoefficient = 0.7f;

        private const float JumpCooldownTime = 0.1f;

        #endregion

        #region variable

        public float baseSpeed = 2.5f;

        public float sprintSpeedMultiplier = 2.0f;

        public float jumpSpeed = 3.5f;

        private readonly InputAction moveAction;
        private Vector2 inputMove;

        private readonly InputAction jumpAction;
        private float inputJump;

        private readonly InputAction sprintAction;
        private float inputSprint;

        private SmoothFloat sprintCoefficient;

        private float jumpCooldownTimer = 0.0f;
        private bool jumpPressed = false;

        #endregion

        #region constructor

        public XRContinuousMoveController(in XRLocomotionController locomotion) : base(locomotion) {
            moveAction = locomotion.inputMoveAction;
            jumpAction = locomotion.inputJumpAction;
            sprintAction = locomotion.inputSprintAction;
            sprintCoefficient = 0.0f;
            jumpCooldownTimer = 0.0f;
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
            // jump cooldown timer:
            if (jumpCooldownTimer != 0.0f) {
                if (jumpCooldownTimer > deltaTime) {
                    jumpCooldownTimer -= deltaTime;
                } else {
                    jumpCooldownTimer = 0.0f;
                }
            }
            // movement:
            // height:
            float heightCoefficient = Mathf.Lerp(locomotion.minHeight, HeightMoveSpeedDamperThreshold, locomotion.height) / HeightMoveSpeedDamperThreshold;
            heightCoefficient = Mathf.Clamp(heightCoefficient * heightCoefficient, 0.1f, 1.0f);
            // movement speed:
            float movementSpeed = heightCoefficient * baseSpeed;
            // sprint:
            sprintCoefficient.Apply(inputSprint, SprintSmoothing * deltaTime);
            float sprintValue = sprintCoefficient.value;
            if (sprintValue > 0.0f) {
                float sprintCoefficient = Mathf.Lerp(1.0f, sprintSpeedMultiplier, sprintValue);
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
            // jump:
            if (inputJump > 0.5f) {
                if (locomotion.grounded && !jumpPressed && jumpCooldownTimer == 0.0f) {
                    Vector3 jumpVelocity = new Vector3(0.0f, jumpSpeed, 0.0f);
                    locomotion.AddForce(jumpVelocity, ForceMode.VelocityChange);
                    jumpCooldownTimer = JumpCooldownTime;
                }
                jumpPressed = true;
            } else {
                jumpPressed = false;
            }
        }

        #endregion

        #endregion

    }

}

#endif