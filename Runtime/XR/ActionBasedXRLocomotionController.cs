#if USE_XR_TOOLKIT

using BlackTundra.Foundation;
using BlackTundra.Foundation.Control;
using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Utility;
using BlackTundra.World.CameraSystem;

using Unity.XR.CoreUtils;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

namespace BlackTundra.World.XR {

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
#if UNITY_EDITOR
    [DefaultExecutionOrder(-1)]
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

        /// <summary>
        /// Distance from the top of the <see cref="controller"/> that the eyes exist at.
        /// </summary>
        private const float EyeHeightOffset = 0.12f;

        /// <summary>
        /// Maximum distance that the head is allowed to be from the center of the <see cref="controller"/>. Head offset allows for the player
        /// to lean over surfaces, this simply limits how far they can lean.
        /// </summary>
        private const float MaxHeadOffset = 1.0f;

        #endregion

        #region variable

        /// <summary>
        /// <see cref="ContinuousMoveProviderBase"/> used by the <see cref="ActionBasedXRLocomotionController"/>.
        /// </summary>
        [SerializeField]
        private ContinuousMoveProviderBase continuousMoveProvider = null;

        /// <summary>
        /// <see cref="ContinuousTurnProviderBase"/> used by the <see cref="ActionBasedXRLocomotionController"/>.
        /// </summary>
        [SerializeField]
        private ContinuousTurnProviderBase continuousTurnProvider = null;

        /// <summary>
        /// Sprint <see cref="InputActionProperty"/>.
        /// </summary>
        [SerializeField]
        private InputActionProperty inputSprint;
        private InputAction inputSprintAction = null;

        /// <summary>
        /// <see cref="LayerMask"/> used for calculating collisions with solid objects. This <see cref="LayerMask"/>
        /// should contain layers that contain solid geometry.
        /// </summary>
        [SerializeField]
        private LayerMask layerMask = -1;

        /// <summary>
        /// <see cref="Transform"/> component that describes the position of the <see cref="camera"/>.
        /// </summary>
        [SerializeField]
        private Transform headTargetTransform = null;

        /// <summary>
        /// <see cref="Transform"/> to parent the <see cref="camera"/> to.
        /// </summary>
        /// <remarks>
        /// This should be a child of <see cref="headTargetTransform"/>.
        /// </remarks>
        [SerializeField]
        private Transform headOffsetTransform = null;

        /// <summary>
        /// <see cref="Transform"/> to parent the hands to.
        /// </summary>
        /// <remarks>
        /// This offsets the hands vertically if the eye height has to be forcibly changed to ensure the camera stays within bounds.
        /// </remarks>
        [SerializeField]
        private Transform handOffsetTransform = null;

        /// <summary>
        /// Minimum height of the <see cref="controller"/>.
        /// </summary>
#if UNITY_EDITOR
        [Min(0.0f)]
#endif
        [SerializeField]
        private float minHeight = 0.35f;

        /// <summary>
        /// Maximum height of the <see cref="controller"/>.
        /// </summary>
#if UNITY_EDITOR
        [Min(0.01f)]
#endif
        [SerializeField]
        private float maxHeight = 3.00f;

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
        /// Sprint value.
        /// </summary>
        private SmoothFloat sprintAmount = 0.0f;

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

        public Vector3 position { get; private set; } = Vector3.zero;
        public Quaternion rotation { get; private set; } = Quaternion.identity;
        public Vector3 cameraPosition { get; private set; } = Vector3.zero;
        public Quaternion cameraRotation { get; private set; } = Quaternion.identity;
        public float height { get; set; } = 1.0f;
        public float radius { get; set; } = 1.0f;

        [ConfigurationEntry(XRManager.XRConfigName, "xr.locomotion.turn_speed", 120.0f)]
        private static float TurnBaseSpeed {
            get => _turnBaseSpeed;
            set {
                _turnBaseSpeed = Mathf.Clamp(value, 0.0f, 720.0f);
                if (instance != null && instance.continuousTurnProvider != null) instance.continuousTurnProvider.turnSpeed = _turnBaseSpeed;
            }
        }
        private static float _turnBaseSpeed = 120.0f;

        #endregion

        #region logic

        #region Awake

        private void Awake() {
            this.ManageObjectSingleton(ref instance);
            controller = GetComponent<CharacterController>();
            Transform locomotion = transform.Find("LocomotionSystem");
            ResetProviders();
            UpdatePositionRotation();
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
            continuousMoveProvider.moveSpeed = MoveBaseSpeed;
            continuousTurnProvider.turnSpeed = _turnBaseSpeed;
        }

        #endregion

        #region ConfigureCamera

        private void ConfigureCamera() {
            if (camera == null) {
                camera = CameraController.current;
                Console.AssertReference(camera);
            }
            //camera.target = rig.cameraGameObject.transform;
            camera.target = headOffsetTransform;
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
            return ControlFlags.None;
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
            UpdatePositionRotation();
        }

        #endregion

        #region FixedUpdate

        private void FixedUpdate() {
            if (!controlled) return; // not currently being controlled
            UpdateCharacterController();
            UpdatePositionRotation();
        }

        #endregion

        #region UpdateMove

        private void UpdateMove(in float deltaTime) {
            sprintAmount.Apply(inputSprintAction.ReadValue<float>(), SprintSmoothing * deltaTime);
            float sprintCoefficient = Mathf.Lerp(1.0f, MoveSprintSpeedCoefficient, sprintAmount.value);
            float heightCoefficient = Mathf.Lerp(minHeight, HeightMoveSpeedDamperThreshold, controller.height) / HeightMoveSpeedDamperThreshold;
            heightCoefficient = Mathf.Clamp(heightCoefficient * heightCoefficient, 0.1f, 1.0f);
            float moveSpeed = MoveBaseSpeed * heightCoefficient * sprintCoefficient;
            continuousMoveProvider.moveSpeed = moveSpeed;
        }

        #endregion

        #region UpdateInputActionReference

        private void UpdateInputActionReferences() {
            inputSprintAction = inputSprint.action;
        }

        #endregion

        #region UpdateCharacterController

        /// <summary>
        /// Updates the <see cref="controller"/> to match with the position of the XR eyes (camera).
        /// </summary>
        private void UpdateCharacterController() {
            // get controller properties:
            float height = controller.height;
            float radius = controller.radius;
            float skinWidth = controller.skinWidth;
            float skinWidth2 = 2.0f * skinWidth; // double the skinWidth, this is used in later calculations
            float actualRadius = radius + skinWidth; // the actual radius is made up of the radius of the controller and the skinWidth of the controller
            float actualHeight = height + skinWidth2; // the actual height is made up of the height of the controller and 2 * skinWidth of the controller
            Vector3 center = controller.center;
            Vector3 position = transform.position;
            // calculate the actual position of the controller (taking into account center offset):
            Vector3 actualPosition = transform.TransformPoint(center.x, 0.0f, center.z); // transform the local center of the controller into a world position, this is effectively the actual position of the controller
            // get camera properties:
            Vector3 cameraWorldPosition = headTargetTransform.position; // camera position in world space
            Vector3 cameraRigPosition = transform.InverseTransformPoint(cameraWorldPosition); // the camera position in rig space is synonymous with the camera position in local space relative to the rig transform component
            // calculate the vector translation that would transform the controller to the camera position:
            Vector3 controllerToCamera = new Vector3(
                cameraWorldPosition.x - actualPosition.x,
                0.0f, // do not calculate the y value since this should be ignored in the translation
                cameraWorldPosition.z - actualPosition.z
            );
            /*
             * In order to move the center of the controller towards the camera position while not allowing the controller to pass
             * through solid geometry, the controller needs to physically move towards the new target head position. After the translation,
             * the difference in position can be calculated, and from there the movement can be undone by translating the transform component
             * by the negative difference in position. The new center of the controller can now safely be set by converting the difference
             * in position into a local directional vector and applying it to the center position.
             */
            // move the controller towards the camera:
            controller.Move(controllerToCamera);
            // find the new position of the controller after the movement:
            Vector3 newPosition = transform.position;
            // calculate the difference in position in world space before and after the movement:
            Vector3 deltaPosition = new Vector3(
                newPosition.x - position.x,
                0.0f,
                newPosition.z - position.z
            );
            // undo the movement by translating the transform component back by the difference in position:
            transform.position -= deltaPosition;
            // safely recalculate the center of the controller using the difference in position (and translating it from a world space direction to a local space direction):
            center += transform.InverseTransformDirection(deltaPosition);
            // re-apply the center of the controller:
            controller.center = center;
            // recalculate actual position:
            actualPosition = transform.TransformPoint(center.x, 0.0f, center.z); // transform the local center of the controller into a world position, this is effectively the actual position of the controller
            // calculate target height of the controller:
            float targetHeight = Mathf.Clamp( // clamping is required to prevent the controller from being extremely tall or too short
                cameraRigPosition.y + EyeHeightOffset, // the height of the controller should be the head height plus eye height offset
                minHeight, maxHeight // get lower and upper clamps from the driver
            );
            // detect if there is solid geometry above the controller:
            Vector3 sphereCastPoint = new Vector3(
                actualPosition.x,
                actualPosition.y + actualHeight - actualRadius, // start the cast from the center of the head of the controller
                actualPosition.z
            );
            if (Physics.SphereCast( // perform a sphere-cast operation to check if there are any colliders above the controller that would prevent the change in height
                sphereCastPoint, // use pre-calculated cast point
                radius, // use the radius rather than the actual radius since the collider may be slightly inside a collider, this is what the skin width is for, it acts as a buffer to prevent this
                Vector3.up, // cast upwards
                out RaycastHit hit,
                cameraWorldPosition.y - sphereCastPoint.y + (Mathf.Epsilon * 2.0f), // find the distance between the camera and cast point and use that as the distance for the cast
                layerMask, QueryTriggerInteraction.Ignore // configure layermask and trigger ignore for cast
            )) { // cast hit something
                Vector3 hitPoint = hit.point;
                targetHeight = hitPoint.y - actualPosition.y - (Mathf.Epsilon * 2.0f); // recalculate the target height to make contact with the surface that was hit
            }
            // re-calculate the center of the controller with the new target height:
            center = new Vector3(center.x, targetHeight * 0.5f, center.z);
            controller.center = center;
            controller.height = targetHeight - skinWidth2;
            // update head position:
            Vector3 headStartPosition = new Vector3( // find the position of the head on the controller if the head was perfectly centered on the center of the controller
                actualPosition.x,
                actualPosition.y + targetHeight - actualRadius,
                actualPosition.z
            );
            Vector3 headTargetPositon = new Vector3( // find the position that the head wants to be in
                cameraWorldPosition.x,
                headStartPosition.y,
                cameraWorldPosition.z
            );
            Vector3 deltaHeadPosition = headTargetPositon - headStartPosition;
            float deltaHeadDistance = deltaHeadPosition.magnitude;
            if (deltaHeadDistance > MaxHeadOffset) { // the difference in head positions is greater than the max allowed distance, clamp
                deltaHeadPosition = deltaHeadPosition * (MaxHeadOffset / deltaHeadDistance); // recalculate the vector between the head start position and target position with clamping
                deltaHeadDistance = MaxHeadOffset; // recalculate the magnitude of the deltaHeadPosition vector
                headTargetPositon = headStartPosition + deltaHeadPosition; // recalculate the target head position
            }
            // check if head can be in target position:
            if (Physics.SphereCast( // use a sphere-cast to check if there is any solid geometry between the head start position and head target position, this allows the player to look over objects but not put their head inside walls
                headStartPosition, // use pre-calculated head start position
                radius, // use the radius rather than the actual radius since the collider may be slightly inside a collider, this is what the skin width is for, it acts as a buffer to prevent this
                deltaHeadPosition, // calculate the direction that the sphere-cast should be made in
                out hit,
                deltaHeadDistance, // use pre-calculated cast distance
                layerMask, QueryTriggerInteraction.Ignore
            )) { // solid geometry exists between the current head position and the target head position
                headTargetPositon = headStartPosition + (deltaHeadPosition * (hit.distance / deltaHeadDistance)); // re-calculate the target position of the head
            }
            // calculate the eye position:
            Vector3 eyePosition = new Vector3(
                headTargetPositon.x,
                headTargetPositon.y + actualRadius - EyeHeightOffset,
                headTargetPositon.z
            );
            // set eye position:
            headOffsetTransform.position = eyePosition;
            // find the local eye position:
            Vector3 localEyePosition = headOffsetTransform.localPosition;
            // ensure the hand parent vertical offset matches the eye offset:
            handOffsetTransform.localPosition = new Vector3(0.0f, localEyePosition.y, 0.0f);
            // update properties:
            this.height = targetHeight;
            this.radius = actualRadius;
        }

        #endregion

        #region UpdatePositionRotation

        private void UpdatePositionRotation() {
            Vector3 center = controller.center;
            position = transform.TransformPoint(center.x, 0.0f, center.z);
            Quaternion cameraRotation = headOffsetTransform.rotation;
            this.cameraRotation = cameraRotation;
            cameraPosition = headOffsetTransform.position;
            Vector3 cameraEulerRotation = cameraRotation.eulerAngles;
            rotation = Quaternion.Euler(0.0f, cameraEulerRotation.y, 0.0f);
        }

        #endregion

        #endregion

    }

}

#endif