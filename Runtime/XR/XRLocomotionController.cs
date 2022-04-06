#if USE_XR_TOOLKIT

using BlackTundra.Foundation.Control;
using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Utility;
using BlackTundra.World.CameraSystem;
using BlackTundra.World.Targetting;
using BlackTundra.World.XR.Locomotion;

using System;

using Unity.XR.CoreUtils;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

using Console = BlackTundra.Foundation.Console;

namespace BlackTundra.World.XR {

    [DisallowMultipleComponent]
    [RequireComponent(typeof(XROrigin))]
    [RequireComponent(typeof(CharacterController))]
#if UNITY_EDITOR
    [DefaultExecutionOrder(-1)]
    [AddComponentMenu("XR/Locomotion Controller")]
#endif
    public sealed class XRLocomotionController : MonoBehaviour, IControllable, IPhysicsObject, ITargetable {

        #region constant

        private const float NearClipDistance = 0.01f;

        /// <summary>
        /// Amount to increase the LOD bias by.
        /// </summary>
        private const float LODIncrease = 10.0f;

        /// <summary>
        /// Distance from the top of the <see cref="controller"/> that the eyes exist at.
        /// </summary>
        private const float EyeHeightOffset = 0.12f;

        /// <summary>
        /// Maximum distance that the head is allowed to be from the center of the <see cref="controller"/>. Head offset allows for the player
        /// to lean over surfaces, this simply limits how far they can lean.
        /// </summary>
        private const float MaxHeadOffset = 1.0f;

        /// <summary>
        /// Maximum number of meters per second that the <see cref="XRLocomotionController"/> can walk downwards without becoming "unstuck"
        /// from the ground.
        /// </summary>
        private const float MaxGroundedDownwardDeltaHeightRate = -10.0f;

        /// <summary>
        /// Mass of the <see cref="XRLocomotionController"/>.
        /// </summary>
        private const float Mass = 80.0f;

        /// <summary>
        /// Inverse mass of the <see cref="XRLocomotionController"/>.
        /// </summary>
        private const float InverseMass = 1.0f / Mass;

        /// <summary>
        /// Half the negative drag coefficient of a human.
        /// </summary>
        private const float HalfNegativeDragCoefficient = -1.2f * 0.5f;

        /// <summary>
        /// Amount to dampen the physics velocity while grounded.
        /// </summary>
        private const float GroundedVelocityDampenFactor = 25.0f;

        #endregion

        #region variable

        /// <summary>
        /// <see cref="XRMovementProvider"/> responsible for moving the <see cref="XRLocomotionController"/>.
        /// </summary>
        private XRMovementProvider movementProvider = null;

        /// <summary>
        /// <see cref="XRTurnProvider"/> responsible for turning the <see cref="XRLocomotionController"/>.
        /// </summary>
        private XRTurnProvider turnProvider = null;

        /// <summary>
        /// Toggles if the <see cref="XRLocomotionController"/> should use gravity.
        /// </summary>
        [SerializeField]
        private bool useGravity = true;

        /// <summary>
        /// Move <see cref="InputActionProperty"/>.
        /// </summary>
        [SerializeField]
        private InputActionProperty inputMove;
        internal InputAction inputMoveAction = null;

        /// <summary>
        /// Turn <see cref="InputActionProperty"/>.
        /// </summary>
        [SerializeField]
        private InputActionProperty inputTurn;
        internal InputAction inputTurnAction = null;

        /// <summary>
        /// Jump<see cref="InputActionProperty"/>.
        /// </summary>
        [SerializeField]
        private InputActionProperty inputJump;
        internal InputAction inputJumpAction = null;

        /// <summary>
        /// Sprint <see cref="InputActionProperty"/>.
        /// </summary>
        [SerializeField]
        private InputActionProperty inputSprint;
        internal InputAction inputSprintAction = null;

        /// <summary>
        /// <see cref="Transform"/> used to find the forwards direction.
        /// </summary>
        internal Transform forwardTransform = null;

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

        [SerializeField]
        private XRHandController leftHand = null;

        [SerializeField]
        private XRHandController rightHand = null;

        /// <summary>
        /// Minimum height of the <see cref="controller"/>.
        /// </summary>
#if UNITY_EDITOR
        [Min(0.0f)]
#endif
        [SerializeField]
        internal float minHeight = 0.35f;

        /// <summary>
        /// Maximum height of the <see cref="controller"/>.
        /// </summary>
#if UNITY_EDITOR
        [Min(0.01f)]
#endif
        [SerializeField]
        internal float maxHeight = 3.00f;

        [SerializeField]
        private UnityAction<Vector3> onImpactGround = null;

        [SerializeField]
        private UnityAction onLeaveGround = null;

        [SerializeField]
        private UnityAction<bool> onGroundedStateChanged = null;

        /// <summary>
        /// <see cref="CameraController"/> used for XR.
        /// </summary>
#if UNITY_EDITOR
        new
#endif
        private CameraController camera = null;

        /// <summary>
        /// <see cref="XROrigin"/> component attached to the <see cref="XRLocomotionController"/> <see cref="GameObject"/>.
        /// </summary>
        internal XROrigin origin = null;

        /// <summary>
        /// <see cref="CharacterController"/> used for movement.
        /// </summary>
        internal CharacterController controller = null;

        /// <summary>
        /// <c>true</c> while being controlled.
        /// </summary>
        private bool controlled = false;

        /// <summary>
        /// Velocity that the <see cref="XRLocomotionController"/> will move with.
        /// </summary>
        private Vector3 moveVelocity = Vector3.zero;

        /// <summary>
        /// Component of the <see cref="XRLocomotionController"/> velocity that is controlled by the custom physics system.
        /// </summary>
        private Vector3 physicsVelocity = Vector3.zero;

        /// <summary>
        /// Component of <see cref="physicsVelocity"/> that will be added every time <see cref="FixedUpdate"/> is invoked.
        /// This will be multiplied by <see cref="Time.fixedDeltaTime"/> and added to <see cref="physicsVelocity"/>. It will
        /// then be reset to <see cref="Vector3.zero"/>.
        /// </summary>
        private Vector3 physicsTimeDependentDeltaVelocity = Vector3.zero;

        /// <summary>
        /// Component of the <see cref="XRLocomotionController"/> velocity that is reset each frame.
        /// </summary>
        private Vector3 physicsInstantVelocity = Vector3.zero;

        /// <summary>
        /// Singleton reference to the <see cref="XRLocomotionController"/>.
        /// </summary>
        private static XRLocomotionController instance = null;

        #endregion

        #region property

        public Vector3 position { get; private set; } = Vector3.zero;
        public Quaternion rotation { get; private set; } = Quaternion.identity;
        public Vector3 velocity => controller.velocity;
        public Vector3 centreOfMass { get; private set; }
        public Vector3 HeadPosition { get; private set; } = Vector3.zero;
        public Quaternion HeadRotation { get; private set; } = Quaternion.identity;
        public float height => _height;
        private float _height = 1.0f;
        public float radius => _radius;
        private float _radius = 1.0f;
        public bool grounded => _grounded;
        private bool _grounded = false;
        public float mass => Mass;

        /// <summary>
        /// Custom targetting flags for the <see cref="XRLocomotionController"/>.
        /// </summary>
        public int TargetFlags {
            get => _targetFlags;
            set => _targetFlags = value;
        }
        private int _targetFlags;

        private XRForwardDirection ForwardDirection {
            get => _forwardDirection;
            set {
                if (_forwardDirection != value) {
                    _forwardDirection = value;
                    UpdateForwardDirection();
                }
            }
        }
        private static XRForwardDirection _forwardDirection = XRForwardDirection.Head;

        public XRMovementProvider MovementProvider {
            get => movementProvider;
            set => movementProvider = value;
        }

        public XRTurnProvider TurnProvider {
            get => turnProvider;
            set => turnProvider = value;
        }

        #endregion

        #region logic

        #region Awake

        private void Awake() {
            this.ManageObjectSingleton(ref instance);
            origin = GetComponent<XROrigin>();
            controller = GetComponent<CharacterController>();
            UpdateForwardDirection();
            UpdatePositionRotation();
        }

        #endregion

        #region OnEnable

        private void OnEnable() {
            EnableInputActions();
            this.GainControl(true);
            ((ITargetable)this).Register();
        }

        #endregion

        #region OnDisable

        private void OnDisable() {
            ((ITargetable)this).Deregister();
            DisableInputActions();
            this.RevokeControl(true);
        }

        #endregion

        #region EnableInputActions

        private void EnableInputActions() {
            inputMove.EnableDirectAction();
            inputTurn.EnableDirectAction();
            inputJump.EnableDirectAction();
            inputSprint.EnableDirectAction();
        }

        #endregion

        #region DisableInputActions

        private void DisableInputActions() {
            inputMove.DisableDirectAction();
            inputTurn.DisableDirectAction();
            inputJump.DisableDirectAction();
            inputSprint.DisableDirectAction();
        }

        #endregion

        #region UpdateInputActionReference

        private void UpdateInputActionReferences() {
            inputMoveAction = inputMove.action;
            inputTurnAction = inputTurn.action;
            inputJumpAction = inputJump.action;
            inputSprintAction = inputSprint.action;
        }

        #endregion

        #region UpdateForwardDirection

        /// <summary>
        /// Updates the <see cref="forwardTransform"/> based on the value of <see cref="_forwardDirection"/>.
        /// </summary>
        private void UpdateForwardDirection() {
            forwardTransform = _forwardDirection switch {
                XRForwardDirection.Head => headTargetTransform,
                XRForwardDirection.Body => transform,
                XRForwardDirection.LeftHand => leftHand.transform,
                XRForwardDirection.RightHand => rightHand.transform,
                _ => null
            };
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
            UpdateInputActionReferences();
            ConfigureCamera();
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
            float deltaTime = Time.deltaTime;
            if (controlled) {
                if (turnProvider != null) turnProvider.Update(deltaTime);
                if (movementProvider != null) movementProvider.Update(deltaTime);
            }
            UpdatePositionRotation();
        }

        #endregion

        #region FixedUpdate

        private void FixedUpdate() {
            float deltaTime = Time.fixedDeltaTime;
            if (controlled) {
                if (movementProvider != null) movementProvider.FixedUpdate(deltaTime);
            }
            UpdatePhysics(deltaTime);
            UpdateCharacterController(deltaTime);
            UpdatePositionRotation();
            UpdateHandPhysics(deltaTime);
        }

        #endregion

        #region UpdateHandPhysics

        private void UpdateHandPhysics(in float deltaTime) {
            if (leftHand != null) leftHand.InternalPhysicsUpdate(deltaTime);
            if (rightHand != null) rightHand.InternalPhysicsUpdate(deltaTime);
        }

        #endregion

        #region UpdatePhysics

        private void UpdatePhysics(in float deltaTime) {
            // grounded state:
            bool grounded = controller.isGrounded;
            if (grounded != _grounded) {
                _grounded = grounded;
                if (onGroundedStateChanged != null) {
                    onGroundedStateChanged.Invoke(grounded);
                }
                if (_grounded) { // stuck to ground
                    if (useGravity) {
                        physicsInstantVelocity = new Vector3(
                            0.0f,
                            MaxGroundedDownwardDeltaHeightRate,
                            0.0f
                        );
                    }
                    // invoke impact ground callback:
                    if (onImpactGround != null) {
                        onImpactGround.Invoke(controller.velocity);
                    }
                } else { // left ground
                    physicsInstantVelocity = Vector3.zero;
                    if (onLeaveGround != null) {
                        onLeaveGround.Invoke();
                    }
                }
            }
            // time based velocity:
            physicsVelocity += physicsTimeDependentDeltaVelocity * deltaTime; // apply time dependent delta velocity
            physicsTimeDependentDeltaVelocity = Vector3.zero; // reset time based velocity
            // environmental physics:
            if (_grounded) {
                if (physicsVelocity.y > 0.0f && physicsInstantVelocity.y < 0.0f) physicsInstantVelocity.y = 0.0f; // remove instant negative y velocity
                float dragCoefficient = 1.0f - (GroundedVelocityDampenFactor * deltaTime);
                physicsVelocity *= dragCoefficient;
                if (physicsVelocity.y < 0.0f) physicsVelocity.y = 0.0f; // do not allow downwards velocity while grounded
            } else {
                // find rho:
                float rho = Environment.RhoAt(transform.position);
                // calculate the vertical (top-down) area:
                float verticalArea = _radius * _radius * Mathf.PI;
                // calculate the horizontal (front) area:
                float horizontalArea = verticalArea + (_radius * Mathf.Max(_height - _radius - _radius, 0.0f));
                // combine the drag coefficient with rho and delta time:
                float combinedDragCoefficient = HalfNegativeDragCoefficient * rho * deltaTime;
                // calculate the vertical (Y) drag coefficient:
                float verticalDragCoefficient = combinedDragCoefficient * verticalArea;
                // calculate the horizontal (XZ) drag coefficient:
                float horizontalDragCoefficient = combinedDragCoefficient * horizontalArea;
                if (useGravity) {
                    physicsVelocity += new Vector3(
                        Mathf.Sign(physicsVelocity.x) * physicsVelocity.x * physicsVelocity.x * horizontalDragCoefficient,
                        (Mathf.Sign(physicsVelocity.y) * physicsVelocity.y * physicsVelocity.y * verticalDragCoefficient) - (Environment.gravity * deltaTime),
                        Mathf.Sign(physicsVelocity.z) * physicsVelocity.z * physicsVelocity.z * horizontalDragCoefficient
                    );
                } else {
                    physicsVelocity += new Vector3(
                        physicsVelocity.x * physicsVelocity.x * horizontalDragCoefficient,
                        physicsVelocity.y * physicsVelocity.y * verticalDragCoefficient,
                        physicsVelocity.z * physicsVelocity.z * horizontalDragCoefficient
                    );
                }
            }
        }

        #endregion

        #region ApplyVelocity

        private void ApplyVelocity(in float deltaTime) {
            Vector3 forward = forwardTransform.forward;
            float degrees = Vector2.SignedAngle(Vector2.up, new Vector2(forward.x, forward.z));
            Quaternion forwardRotation = Quaternion.Euler(0.0f, -degrees, 0.0f);
            Vector3 velocity = (forwardRotation * moveVelocity) + physicsVelocity + physicsInstantVelocity;
            controller.Move(velocity * deltaTime);
        }

        #endregion

        #region UpdateCharacterController

        /// <summary>
        /// Updates the <see cref="controller"/> to match with the position of the XR eyes (camera).
        /// </summary>
        private void UpdateCharacterController(in float deltaTime) {
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
            _height = targetHeight;
            _radius = actualRadius;
            // apply velocity:
            ApplyVelocity(deltaTime);
        }

        #endregion

        #region UpdatePositionRotation

        private void UpdatePositionRotation() {
            Vector3 centre = controller.center;
            position = transform.TransformPoint(centre.x, 0.0f, centre.z);
            centreOfMass = centre;
            Quaternion headRotation = headOffsetTransform.rotation;
            HeadRotation = headRotation;
            HeadPosition = headOffsetTransform.position;
            Vector3 cameraEulerRotation = headRotation.eulerAngles;
            rotation = Quaternion.Euler(0.0f, cameraEulerRotation.y, 0.0f);
        }

        #endregion

        #region SetMoveVelocity

        internal void SetMoveVelocity(in Vector3 velocity) {
            moveVelocity = velocity;
        }

        #endregion

        #region AddForce

        /// <summary>
        /// Adds a <paramref name="force"/> to the <see cref="XRLocomotionController"/>.
        /// </summary>
        public void AddForce(in Vector3 force, in ForceMode forceMode) {
            switch (forceMode) {
                case ForceMode.Force: {
                    physicsTimeDependentDeltaVelocity += force * InverseMass;
                    break;
                }
                case ForceMode.Impulse: {
                    physicsVelocity += force * InverseMass;
                    break;
                }
                case ForceMode.Acceleration: {
                    physicsTimeDependentDeltaVelocity += force;
                    break;
                }
                case ForceMode.VelocityChange: {
                    physicsVelocity += force;
                    break;
                }
            }
        }

        #endregion

        #region AddForceAtPosition

        public void AddForceAtPosition(in Vector3 force, in Vector3 position, in ForceMode forceMode) => AddForce(force, forceMode);

        #endregion

        #region AddExplosionForce

        public void AddExplosionForce(in float force, in Vector3 point, in float radius, float upwardsModifier = 0.0f, in ForceMode forceMode = ForceMode.Force) {
            if (radius < 0.0f) throw new ArgumentException(nameof(radius) + " cannot be negative.");
            Vector3 explosionVector = centreOfMass - point;
            float sqrDistance = explosionVector.sqrMagnitude;
            if (sqrDistance > radius * radius) return;
            explosionVector.y += upwardsModifier;
            AddForce(force * (1.0f - (1.0f / radius)) * explosionVector, forceMode);
        }

        #endregion

        #endregion

    }

}

#endif