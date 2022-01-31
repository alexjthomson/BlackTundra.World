using BlackTundra.Foundation;
using BlackTundra.Foundation.Control;
using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Utility;
using BlackTundra.World.CameraSystem;

using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackTundra.World.Player {

    [DisallowMultipleComponent]
    public sealed class ActionBasedLocomotionController : LocomotionBase, IControllable {

        #region constant

        public const string ConfigurationName = "locomotion";

        private const float MinSmoothing = 1.0f;
        private const float MaxSmoothing = 1000.0f;

        private const float DefaultMoveSmoothingX = 20.0f;
        private const float DefaultMoveSmoothingY = 20.0f;
        private const float MoveBaseForwardSpeed = 3.0f;
        private const float MoveBaseStrafeSpeed = 2.0f;
        private const float MoveSprintSpeedCoefficient = 2.0f;

        private const float DefaultLookSmoothingX = 50.0f;
        private const float DefaultLookSmoothingY = 50.0f;
        private const float DefaultLookSensitivityX = 0.175f;
        private const float DefaultLookSensitivityY = 0.100f;
        public const float MinLookSensitivity = 0.0f;
        public const float MaxLookSensitivity = 1.0f;

        private const float SprintSmoothing = 10.0f;

        private const float SkinWidth = 0.125f;
        private const float Radius = 0.25f;
        private const float StandHeight = 1.8f;

        private static readonly ConsoleFormatter ConsoleFormatter = new ConsoleFormatter(nameof(ActionBasedLocomotionController));

        #endregion

        #region variable

        #region input actions

        /// <summary>
        /// Move <see cref="InputActionProperty"/>.
        /// </summary>
        [SerializeField]
        private InputActionProperty inputMove;
        private InputAction inputMoveAction = null;

        /// <summary>
        /// Look <see cref="InputActionProperty"/>.
        /// </summary>
        [SerializeField]
        private InputActionProperty inputLook;
        private InputAction inputLookAction = null;

        /// <summary>
        /// Jump <see cref="InputActionProperty"/>.
        /// </summary>
        [SerializeField]
        private InputActionProperty inputJump;
        private InputAction inputJumpAction = null;

        /// <summary>
        /// Sprint <see cref="InputActionProperty"/>.
        /// </summary>
        [SerializeField]
        private InputActionProperty inputSprint;
        private InputAction inputSprintAction = null;

        /// <summary>
        /// Crouch <see cref="InputActionProperty"/>.
        /// </summary>
        [SerializeField]
        private InputActionProperty inputCrouch;
        private InputAction inputCrouchAction = null;

        /// <summary>
        /// Prone <see cref="InputActionProperty"/>.
        /// </summary>
        [SerializeField]
        private InputActionProperty inputProne;
        private InputAction inputProneAction = null;

        #endregion

        /// <summary>
        /// Target <see cref="Transform"/> that the <see cref="cameraController"/> will track.
        /// </summary>
        [SerializeField]
        private Transform cameraTarget = null;

        /// <summary>
        /// <see cref="CameraController"/> used to control the <see cref="ActionBasedLocomotionController"/>.
        /// </summary>
        private CameraController cameraController = null;

        /// <summary>
        /// Tracks if the <see cref="ActionBasedLocomotionController"/> is being controlled.
        /// </summary>
        private bool isControlled;

        private static SmoothVector2 moveVelocity = new SmoothVector2(0.0f, 0.0f, DefaultMoveSmoothingX, DefaultMoveSmoothingY);
        private static SmoothVector2 lookVelocity = new SmoothVector2(0.0f, 0.0f, DefaultLookSmoothingX, DefaultLookSmoothingY);
        private SmoothFloat sprintAmount = new SmoothFloat(0.0f);

        #endregion

        #region property

        #region move

        [ConfigurationEntry(ConfigurationName, "move.smoothing.x", DefaultMoveSmoothingX)]
        public static float MoveSmoothingX {
            get => moveSmoothing.x;
            set {
                moveSmoothing.x = Mathf.Clamp(value, MinSmoothing, MaxSmoothing);
                moveVelocity.xSmoothing = moveSmoothing.x;
            }
        }
        [ConfigurationEntry(ConfigurationName, "move.smoothing.y", DefaultMoveSmoothingY)]
        public static float MoveSmoothingY {
            get => moveSmoothing.y;
            set {
                moveSmoothing.y = Mathf.Clamp(value, MinSmoothing, MaxSmoothing);
                moveVelocity.ySmoothing = moveSmoothing.y;
            }
        }
        private static Vector2 moveSmoothing = new Vector2(DefaultMoveSmoothingX, DefaultMoveSmoothingY);

        #endregion

        #region look

        [ConfigurationEntry(ConfigurationName, "look.sensitivity.x", DefaultLookSensitivityX)]
        public static float LookSensitivityX {
            get => lookSensitivity.x;
            set => lookSensitivity.x = Mathf.Clamp(value, MinLookSensitivity, MaxLookSensitivity);
        }
        [ConfigurationEntry(ConfigurationName, "look.sensitivity.y", DefaultLookSensitivityY)]
        public static float LookSensitivityY {
            get => lookSensitivity.y;
            set => lookSensitivity.y = Mathf.Clamp(value, MinLookSensitivity, MaxLookSensitivity);
        }
        private static Vector2 lookSensitivity = new Vector2(DefaultLookSensitivityX, DefaultLookSensitivityY);

        [ConfigurationEntry(ConfigurationName, "look.smoothing.x", DefaultLookSmoothingX)]
        public static float LookSmoothingX {
            get => lookSmoothing.x;
            set {
                lookSmoothing.x = Mathf.Clamp(value, MinSmoothing, MaxSmoothing);
                lookVelocity.xSmoothing = lookSmoothing.x;
            }
        }
        [ConfigurationEntry(ConfigurationName, "look.smoothing.y", DefaultLookSmoothingY)]
        public static float LookSmoothingY {
            get => lookSmoothing.y;
            set {
                lookSmoothing.y = Mathf.Clamp(value, MinSmoothing, MaxSmoothing);
                lookVelocity.ySmoothing = lookSmoothing.y;
            }
        }
        private static Vector2 lookSmoothing = new Vector2(DefaultLookSmoothingX, DefaultLookSmoothingY);

        #endregion

        #endregion

        #region logic

        #region OnEnable

        private void OnEnable() {
            skinWidth = SkinWidth;
            radius = Radius;
            height = StandHeight;
            ControlManager.GainControl(this, true);
        }

        #endregion

        #region OnDisable

        private void OnDisable() {
            ControlManager.RevokeControl(this, true);
        }

        #endregion

        #region Update

        private void Update() {
            if (isControlled) {
                float deltaTime = Time.deltaTime;
                UpdateMove(deltaTime);
                UpdateLook(deltaTime);
            } else {
                SetMotiveVelocity(Vector3.zero);
            }
        }

        #endregion

        #region OnControlGained

        public ControlFlags OnControlGained() {
            isControlled = true;
            ResetSmoothInputs();
            SetMotiveVelocity(Vector3.zero);
            UpdateInputActionReferences();
            UpdateCameraController();
            return ControlFlags.HideCursor | ControlFlags.LockCursor;
        }

        #endregion

        #region OnControlRevoked

        public void OnControlRevoked() {
            isControlled = false;
            SetMotiveVelocity(Vector3.zero);
        }

        #endregion

        #region ResetSmoothInputs

        private static void ResetSmoothInputs() {
            moveVelocity.value = Vector2.zero;
            lookVelocity.value = Vector2.zero;
        }

        #endregion

        #region UpdateInputActionReferences

        private void UpdateInputActionReferences() {
            inputMoveAction = inputMove.action;
            inputLookAction = inputLook.action;
            inputJumpAction = inputJump.action;
            inputSprintAction = inputSprint.action;
            inputCrouchAction = inputCrouch.action;
            inputProneAction = inputProne.action;
        }

        #endregion

        #region UpdateCameraController

        private void UpdateCameraController() {
            cameraController = CameraController.current;
            if (cameraController == null) {
                ConsoleFormatter.Error($"Failed to assign {nameof(CameraController)} instance to {nameof(cameraTarget)}.");
                return;
            }
            cameraController.target = cameraTarget;
            cameraController.TrackingFlags = CameraTrackingFlags.Parent;
        }

        #endregion

        #region UpdateMove

        private void UpdateMove(in float deltaTime) {
            Vector2 moveVector = inputMoveAction.ReadValue<Vector2>();
            moveVelocity.Apply(moveVector, deltaTime);
            bool isMovingForward = moveVector.y > 0.0f;
            sprintAmount.Apply(
                isMovingForward
                    ? inputSprintAction.ReadValue<float>()
                    : 0.0f,
                SprintSmoothing * deltaTime
            );
            float sprintCoefficient = isMovingForward
                ? Mathf.Lerp(1.0f, MoveSprintSpeedCoefficient, sprintAmount.value)
                : 1.0f;
            Vector3 movementVector = new Vector3(
                Mathf.Clamp(moveVelocity.x, -1.0f, 1.0f) * sprintCoefficient * MoveBaseStrafeSpeed,
                0.0f,
                Mathf.Clamp(moveVelocity.y, -1.0f, 1.0f) * sprintCoefficient * MoveBaseForwardSpeed
            );
            SetMotiveVelocity(movementVector);
        }

        #endregion

        #region UpdateLook

        private void UpdateLook(in float deltaTime) {
            Vector2 lookVector = inputLookAction.ReadValue<Vector2>() * lookSensitivity;
            lookVelocity.Apply(lookVector, deltaTime);
            transform.rotation = Quaternion.Euler(0.0f, transform.eulerAngles.y + lookVelocity.x, 0.0f);
            cameraTarget.localRotation = Quaternion.Euler(cameraTarget.localEulerAngles.x - lookVelocity.y, 0.0f, 0.0f);
        }

        #endregion

        #endregion

    }

}