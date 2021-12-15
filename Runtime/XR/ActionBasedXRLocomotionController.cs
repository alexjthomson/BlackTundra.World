#if ENABLE_VR

using BlackTundra.Foundation;
using BlackTundra.Foundation.Control;
using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Utility;
using BlackTundra.World.CameraSystem;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

namespace BlackTundra.World.XR {

    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRRig))]
    [RequireComponent(typeof(CharacterController))]
#if UNITY_EDITOR
    [AddComponentMenu("XR/XR Player Controller (Action-based)")]
#endif
    public sealed class ActionBasedXRLocomotionController : MonoBehaviour, IControllable {

        #region constant

        private const float NearClipDistance = 0.01f;

        /// <summary>
        /// Amount to increase the LOD bias by.
        /// </summary>
        private const float LODIncrease = 10.0f;

        private const float MoveBaseSpeed = 3.0f;
        private const float MoveSprintSpeedCoefficient = 2.0f;

        private const float SprintSmoothing = 10.0f;

        /// <summary>
        /// If the the <see cref="controller"/> heights drops below this threshold, the move speed will be reduced relative to how
        /// much it has fallen below the threshold by.
        /// </summary>
        private const float HeightMoveSpeedDamperThreshold = 1.4f;

        #endregion

        #region variable

        /// <summary>
        /// Sprint <see cref="InputActionProperty"/>.
        /// </summary>
        [SerializeField]
        private InputActionProperty inputSprint;
        private InputAction inputSprintAction = null;

        /// <summary>
        /// <see cref="CameraController"/> used for XR.
        /// </summary>
#if UNITY_EDITOR
        new
#endif
        private CameraController camera = null;

        /// <summary>
        /// <see cref="CharacterController"/> used for movement.
        /// </summary>
        private CharacterController controller = null;

        /// <summary>
        /// <see cref="CharacterControllerDriver"/> used to drive the XR <see cref="controller"/>.
        /// </summary>
        private CharacterControllerDriver driver = null;

        /// <summary>
        /// <see cref="ContinuousMoveProviderBase"/> used for moving.
        /// </summary>
        private ContinuousMoveProviderBase moveProvider = null;

        /// <summary>
        /// <see cref="ContinuousTurnProviderBase"/> used for turning.
        /// </summary>
        private ContinuousTurnProviderBase turnProvider = null;

        /// <summary>
        /// <see cref="XRRig"/> used for VR.
        /// </summary>
        private XRRig rig = null;

        private SmoothFloat sprintAmount = new SmoothFloat(0.0f);

        /// <summary>
        /// Height of the controller.
        /// </summary>
        private float height = 1.0f;

        /// <summary>
        /// <c>true</c> while being controlled.
        /// </summary>
        private bool controlled = false;

        /// <summary>
        /// Singleton reference to the <see cref="ActionBasedXRLocomotionController"/>.
        /// </summary>
        private static ActionBasedXRLocomotionController instance = null;

        #endregion

        #region property

        [ConfigurationEntry(XRManager.XRConfigName, "xr.locomotion.turn_speed", 120.0f)]
        private static float TurnBaseSpeed {
            get => _turnBaseSpeed;
            set {
                _turnBaseSpeed = Mathf.Clamp(value, 0.0f, 720.0f);
                if (instance != null && instance.turnProvider != null) instance.turnProvider.turnSpeed = _turnBaseSpeed;
            }
        }
        private static float _turnBaseSpeed = 120.0f;

        #endregion

        #region logic

        #region Awake

        private void Awake() {
            this.ManageObjectSingleton(ref instance);
            rig = GetComponent<XRRig>();
            controller = GetComponent<CharacterController>();
            driver = GetComponent<CharacterControllerDriver>();
            Transform locomotion = transform.Find("LocomotionSystem");
            moveProvider = locomotion.GetComponent<ContinuousMoveProviderBase>();
            turnProvider = locomotion.GetComponent<ContinuousTurnProviderBase>();
            ResetProviders();
        }

        #endregion

        #region OnEnable

        private void OnEnable() {
            this.GainControl(true);
        }

        #endregion

        #region OnDisable

        private void OnDisable() {
            this.RevokeControl(true);
        }

        #endregion

        #region ResetProviders

        private void ResetProviders() {
            moveProvider.moveSpeed = MoveBaseSpeed;
            turnProvider.turnSpeed = _turnBaseSpeed;
        }

        #endregion

        #region ConfigureCamera

        private void ConfigureCamera() {
            if (camera == null) {
                camera = CameraController.current;
                Console.AssertReference(camera);
            }
            camera.target = rig.cameraGameObject.transform;
            camera.TrackingFlags = CameraTrackingFlags.Parent;
            camera.nearClipPlane = NearClipDistance;
        }

        #endregion

        #region OnControlGained

        public ControlFlags OnControlGained() {
            controlled = true;
            ResetProviders();
            ConfigureCamera();
            UpdateInputActionReferences();
            QualitySettings.SetQualityLevel(QualitySettings.GetQualityLevel());
            QualitySettings.lodBias *= LODIncrease; // for some reason the LOD bias gets shrank while in VR
            return ControlFlags.HideCursor | ControlFlags.LockCursor;
        }

        #endregion

        #region OnControlRevoked

        public void OnControlRevoked() {
            controlled = false;
            if (camera != null) {
                QualitySettings.SetQualityLevel(QualitySettings.GetQualityLevel()); // reset the quality level
                camera.TrackingFlags = CameraTrackingFlags.None;
                camera.target = null;
            }
        }

        #endregion

        #region Update

        private void Update() {
            if (!controlled) return; // not currently being controlled
            float deltaTime = Time.deltaTime;
            UpdateMove(deltaTime);
        }

        #endregion

        #region FixedUpdate

        private void FixedUpdate() {
            if (!controlled) return; // not currently being controlled
            UpdateCharacterController();
        }

        #endregion

        #region UpdateMove

        private void UpdateMove(in float deltaTime) {
            sprintAmount.Apply(inputSprintAction.ReadValue<float>(), SprintSmoothing * deltaTime);
            float sprintCoefficient = Mathf.Lerp(1.0f, MoveSprintSpeedCoefficient, sprintAmount.value);
            float heightCoefficient = Mathf.Lerp(driver.minHeight, HeightMoveSpeedDamperThreshold, height) / HeightMoveSpeedDamperThreshold;
            heightCoefficient = Mathf.Clamp(heightCoefficient * heightCoefficient, 0.1f, 1.0f);
            float moveSpeed = MoveBaseSpeed * heightCoefficient * sprintCoefficient;
            moveProvider.moveSpeed = moveSpeed;
        }

        #endregion

        #region UpdateInputActionReference

        private void UpdateInputActionReferences() {
            inputSprintAction = inputSprint.action;
        }

        #endregion

        #region UpdateCharacterController

        private void UpdateCharacterController() {
            height = Mathf.Clamp(rig.cameraInRigSpaceHeight, driver.minHeight, driver.maxHeight); // calculate the height of the xr controller
            Vector3 position = transform.position; // get the position of the controller (in world-space)
            Vector3 cameraPosition = rig.cameraGameObject.transform.position; // get the position of the camera (in world-space)
            Vector3 deltaPosition = new Vector3( // calculate the difference in position in the XZ plane between the XR controller and the camera
                cameraPosition.x - position.x,
                0.0f,
                cameraPosition.z - position.z
            );
            float controllerHeight = (height * 0.5f) + controller.skinWidth;
            Vector3 controllerCenter = new Vector3(0.0f, controllerHeight, 0.0f);
            controller.height = height;
            controller.center = controllerCenter;
            controller.Move(deltaPosition); // move to catch up with the camera position
            position = transform.position;
            cameraPosition = new Vector3( // calculate remaining distance towards the camera that could not be moved, move the camera back to the center of the controller
                position.x,
                cameraPosition.y,
                position.z
            );
            rig.MoveCameraToWorldLocation(cameraPosition); // re-center the camera to the controller
        }

        #endregion

        #endregion

    }

}

#endif