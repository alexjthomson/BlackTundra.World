#if USE_XR_TOOLKIT

using BlackTundra.Foundation;
using BlackTundra.Foundation.Utility;
using BlackTundra.World.Items;

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

namespace BlackTundra.World.XR {

    /// <summary>
    /// Manages an XR hand.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRRayInteractor))]
    [RequireComponent(typeof(ActionBasedController))]
#if UNITY_EDITOR
    [AddComponentMenu("XR/XR Hand Controller (Action-based)")]
#endif
    public /*sealed*/ class XRHandController : MonoBehaviour, IItemHolder {

        #region constant

        protected static readonly ConsoleFormatter ConsoleFormatter = new ConsoleFormatter(nameof(XRHandController));

        /// <summary>
        /// Field on the <see cref="nonPhysicsAnimator"/> used for gripping.
        /// </summary>
        public const string GripAnimatorPropertyName = "Grip";

        /// <summary>
        /// Amount of smoothing to apply to the field on the <see cref="nonPhysicsAnimator"/> named <see cref="GripAnimatorPropertyName"/>.
        /// </summary>
        private const float GripAmountSmoothing = 10.0f;

        /// <summary>
        /// Minimum percentage of the velocity to transform a rigidbody by while smoothing.
        /// </summary>
        private const float RigidbodyMinSmoothing = 0.1f;

        /// <summary>
        /// Velocity that if an item is travelling at, it should have it's position smoothed.
        /// This makes aiming and holding items in still positions better since they jitter less.
        /// </summary>
        /// <remarks>
        /// When items exceed this value, the jitter is less noticable. Smoothing will be applied more
        /// or less depending how close to this value the velocity is. This should occur linearly.
        /// </remarks>
        private const float RigidbodySmoothingMaxSpeed = 0.75f;

        /// <summary>
        /// Coefficient to convert a velocity into a smoothing factor between <c>0.0f</c> and <c>1.0 - <see cref="RigidbodyMinSmoothing"/></c>.
        /// </summary>
        private const float RigidbodySmoothingCoefficient = (1.0f - RigidbodyMinSmoothing) / RigidbodySmoothingMaxSpeed;

        /// <summary>
        /// Square distance between the physics hands and the non-physics hands that will result in the non-physics hands being rendered.
        /// </summary>
        private const float RenderNonPhysicsHandsSqrDistance = 0.1f * 0.1f;

        /// <summary>
        /// Threshold distance between the physics hand and the target hand position for pushing to occur.
        /// </summary>
        private const float ThresholdPushDistance = 0.075f;

        /// <summary>
        /// <see cref="ThresholdPushDistance"/> squared.
        /// </summary>
        private const float ThresholdSqrPushDistance = ThresholdPushDistance * ThresholdPushDistance;

        /// <summary>
        /// Maximum distance between the <see cref="rigidbody"/> hand and the target hand position.
        /// </summary>
        private const float MaxHandDistance = 0.75f;

        /// <summary>
        /// <see cref="MaxHandDistance"/> squared.
        /// </summary>
        private const float MaxSqrHandDistance = MaxHandDistance * MaxHandDistance;

        /// <summary>
        /// Time it should take to move to a grip point.
        /// </summary>
        private const float GripMoveTime = 0.1f;

        /// <summary>
        /// <c>1.0 / <see cref="GripMoveTime"/></c>
        /// </summary>
        private const float GripInverseMoveTime = 1.0f / GripMoveTime;

        /// <summary>
        /// Size of the velocity buffer used for velocity smoothing.
        /// </summary>
        private const int VelocitySmoothingBufferSize = 3;

        /// <summary>
        /// Coefficient used to finalize the smooth velocity averaged value.
        /// </summary>
        private const float VelocitySmoothingCoefficient = 1.0f / VelocitySmoothingBufferSize;

        #endregion

        #region variable

        #region XR Rig Component References
#if UNITY_EDITOR
        [Header("XR Rig Component References"), Space]
#endif

        /// <summary>
        /// Reference to the <see cref="XRLocomotionController"/> responsible for moving the XR player.
        /// </summary>
        [SerializeField]
        private XRLocomotionController _locomotion = null;

        /// <summary>
        /// <see cref="XRRayInteractor"/> component attached to the <see cref="XRHandController"/> <see cref="gameObject"/>.
        /// </summary>
        private XRRayInteractor _interactor = null;

        /// <summary>
        /// <see cref="ActionBasedController"/> component attached to the <see cref="XRHandController"/> <see cref="gameObject"/>.
        /// </summary>
        private ActionBasedController _controller = null;
        #endregion

        #region Physics Hand
#if UNITY_EDITOR
        [Header("Physics Hand"), Space]
#endif

        /// <summary>
        /// Prefab to use for a physics model for the hand.
        /// </summary>
        /// <remarks>
        /// This model should have a <see cref="Rigidbody"/> component and colliders.
        /// </remarks>
        [SerializeField]
#if UNITY_EDITOR
        internal
#else
        private
#endif
        GameObject physicsModelPrefab = null;

        /// <summary>
        /// Scalar used for the velocity based physics-hand tracking.
        /// </summary>
#if UNITY_EDITOR
        [Min(0.0f)]
#endif
        [SerializeField]
        private float velocityScale = 1.0f;

        /// <summary>
        /// Scalar used for the angular velocity based physics-hand tracking.
        /// </summary>
#if UNITY_EDITOR
        [Min(0.0f)]
#endif
        [SerializeField]
        private float angularVelocityScale = 1.0f;
        #endregion

        #region Physics Hand Push Configuration
#if UNITY_EDITOR
        [Header("Physics Hand Push Configuration"), Space]
#endif

        /// <summary>
        /// Scalar that controls how much force the hand exerts on the player when colliding with a surface.
        /// </summary>
#if UNITY_EDITOR
        [Min(0.0f)]
#endif
        [SerializeField]
        private float pushStrength = 4.0f;

        /// <summary>
        /// Maximum distance between the physics hand and the actual hand position that will be converted into a pushing force.
        /// </summary>
#if UNITY_EDITOR
        [Min(ThresholdPushDistance)]
#endif
        [SerializeField]
        private float maxPushHandDistance = 0.25f;

        /// <summary>
        /// Layermask used to detect objects that an item can collide with.
        /// This is used to switch between smooth item modes.
        /// </summary>
        [SerializeField]
        private LayerMask itemCollisionLayerMask = -1;
        #endregion

        #region Physics Hand Grip Configuration
#if UNITY_EDITOR
        [Header("Physics Hand Grip Configuration"), Space]
#endif

        /// <summary>
        /// Maximum distance from the <see cref="gripRayOrigin"/> that an object may be gripped.
        /// </summary>
        [SerializeField]
        private float gripRange = 0.1f;

        /// <summary>
        /// <see cref="LayerMask"/> to use for grip physics operations.
        /// </summary>
        [SerializeField]
        private LayerMask gripLayerMask = 0;

        /// <summary>
        /// Minimum angle that can be gripped on the corner of a surface.
        /// </summary>
        [SerializeField]
        private float minGripAngle = 30.0f;

        /// <summary>
        /// Maximum velocity that can be exerted to the hand while gripping an object.
        /// </summary>
        [SerializeField]
        private float maxGripVelocity = 2.5f;

        #endregion

        #region Input
#if UNITY_EDITOR
        [Header("Input"), Space]
#endif

        [SerializeField]
        private InputActionProperty positionAction;
        private InputAction _positionAction = null;

        [SerializeField]
        private InputActionProperty rotationAction;
        private InputAction _rotationAction = null;

        [SerializeField]
        private InputActionProperty primaryAction;
        private InputAction _primaryAction = null;

        [SerializeField]
        private InputActionProperty secondaryAction;
        private InputAction _secondaryAction = null;

        [SerializeField]
        private InputActionProperty tertiaryAction;
        private InputAction _tertiaryAction = null;

        [SerializeField]
        private InputActionProperty gripAction;
        private InputAction _gripAction = null;
        #endregion

        /// <summary>
        /// <see cref="Animator"/> component on the <see cref="nonPhysicsTransform"/>.
        /// </summary>
        private Animator nonPhysicsAnimator = null;

        /// <summary>
        /// <see cref="Renderer"/> component on child of the <see cref="nonPhysicsTransform"/>.
        /// </summary>
        private Renderer nonPhysicsRenderer = null;

        /// <summary>
        /// <see cref="Renderer"/> component on child of the <see cref="rigidbody"/>.
        /// </summary>
        private Renderer physicsRenderer = null;

        /// <summary>
        /// <see cref="Rigidbody"/> component attached to the physics hand variant.
        /// </summary>
#if UNITY_EDITOR
        new
#endif
        private Rigidbody rigidbody = null;

        /// <summary>
        /// <see cref="Collider"/> array containing every collider that should be excluded from colliding with picked up items.
        /// </summary>
        private Collider[] physicsColliders = null;

        /// <summary>
        /// <see cref="Animator"/> component on the <see cref="rigidbody"/>.
        /// </summary>
        private Animator physicsAnimator = null;

        /// <summary>
        /// Current <see cref="WorldItem"/> that the <see cref="XRPlayerHand"/> is holding.
        /// </summary>
        private WorldItem item = null;

        /// <summary>
        /// Positional offset of the held object.
        /// </summary>
        private Vector3 heldPositionalOffset = Vector3.zero;

        /// <summary>
        /// Rotational offset of the held object.
        /// </summary>
        private Quaternion heldRotationalOffset = Quaternion.identity;

        /// <summary>
        /// Array containing every <see cref="Collider"/> component attached to the <see cref="item"/>.
        /// </summary>
        private Collider[] heldColliders = null;

        /// <summary>
        /// <see cref="Collider"/> buffer used for physics casting operations.
        /// </summary>
        private Collider[] physicsColliderBuffer = new Collider[0];

        /// <summary>
        /// Smooth grip amount.
        /// </summary>
        private SmoothFloat gripAmount = new SmoothFloat(0.0f);

        /// <summary>
        /// Position of the <see cref="transform"/> in the last physics update.
        /// </summary>
        private Vector3 lastPositionPhysics = Vector3.zero;

        /// <summary>
        /// Position of the <see cref="transform"/> in the previous frame.
        /// This is used to predict where an object should be in the current frame.
        /// </summary>
        private Vector3 lastPosition = Vector3.zero;

        /// <summary>
        /// Position of the held object before transforming it to look as if it is where the player's hand is.
        /// </summary>
        private Vector3 lastHeldPosition = Vector3.zero;

        /// <summary>
        /// Rotation of the held object before transforming it to look as if it is where the player's hand is.
        /// </summary>
        private Quaternion lastHeldRotation = Quaternion.identity;

        /// <summary>
        /// List of <see cref="XRHandCollisionTracker"/> instances.
        /// </summary>
        private readonly List<XRHandCollisionTracker> collisionTrackers = new List<XRHandCollisionTracker>();

        /// <summary>
        /// Tracks if the <see cref="XRHandController"/> has grabbed an object.
        /// </summary>
        private XRHandGripTracker gripTracker = null;

        /// <summary>
        /// Point that the <see cref="XRHandController"/> will cast a ray from to find the surface that the hands fingers (excluding thumb) will grip onto during the grab.
        /// </summary>
        private Transform gripUpperRayOrigin = null;

        /// <summary>
        /// Point that the <see cref="XRHandController"/> will cast a ray from to find where either the thumb or palm will grip onto during the grab.
        /// </summary>
        private Transform gripLowerRayOrigin = null;

        /// <summary>
        /// When <c>true</c>, the next hand update will be skipped.
        /// </summary>
        /// <remarks>
        /// Without this, a weird physics bug is introduced where the hand will teleport
        /// far from the position it should be in. This is caused by the deltaHandPosition
        /// vector being very large. It is unclear why this happens.
        /// </remarks>
        private bool skipHandUpdate = false;

        /// <summary>
        /// Last input position.
        /// </summary>
        private Vector3 lastInputPosition = Vector3.zero;

        /// <summary>
        /// Raw velocity of the <see cref="XRHandController"/>.
        /// </summary>
        private Vector3 _localVelocity = Vector3.zero;

        /// <summary>
        /// Calculated smooth velocity of the <see cref="XRHandController"/>.
        /// </summary>
        private Vector3 _smoothLocalVelocity = Vector3.zero;

        /// <summary>
        /// Velocity buffer containing the the previous velocities.
        /// This is so that a smooth velocity value can be obtained.
        /// </summary>
        private readonly Vector3[] velocityBuffer = new Vector3[VelocitySmoothingBufferSize];

        #endregion

        #region property

        /// <summary>
        /// Value between <c>0.0</c> (open hand) and <c>1.0</c> (fist) that describes how much the hand is gripping.
        /// </summary>
        public float GripAmount => gripAmount.value;

        /// <summary>
        /// Describes if the hand is gripping anything.
        /// </summary>
        public virtual bool IsEmpty => !_interactor.hasSelection && !IsGrippingObject() && !IsHoldingItem();

        /// <summary>
        /// <see cref="XRLocomotionController"/> belonging to the <see cref="XRHandController"/>.
        /// </summary>
        public XRLocomotionController locomotion => _locomotion;

        /// <summary>
        /// <see cref="XRRayInteractor"/> attached to the <see cref="XRHandController"/>.
        /// </summary>
        public XRRayInteractor interactor => _interactor;

        /// <summary>
        /// <see cref="ActionBasedController"/> attached to the <see cref="XRHandController"/>.
        /// </summary>
        public ActionBasedController controller => _controller;

        /// <summary>
        /// Actual position of the <see cref="XRHandController"/>.
        /// </summary>
        /// <seealso cref="actualTransform"/>
        public Vector3 position => rigidbody != null ? rigidbody.position : transform.position;

        /// <summary>
        /// Actual rotation of the <see cref="XRHandController"/>.
        /// </summary>
        /// <seealso cref="actualTransform"/>
        public Quaternion rotation => rigidbody != null ? rigidbody.rotation : transform.rotation;

        /// <summary>
        /// <see cref="Transform"/> component that contains the actual position and rotation of the <see cref="XRHandController"/> in the world.
        /// </summary>
        /// <seealso cref="position"/>
        /// <seealso cref="rotation"/>
        public Transform actualTransform => rigidbody != null ? rigidbody.transform : transform;

        /// <summary>
        /// Raw velocity of the <see cref="XRHandController"/>.
        /// </summary>
        public Vector3 velocity => _localVelocity + _locomotion._velocity;

        /// <summary>
        /// Smooth velocity of the <see cref="XRHandController"/>.
        /// </summary>
        public Vector3 smoothVelocity => _smoothLocalVelocity + _locomotion._velocity;

        #endregion

        #region logic

        #region Awake

        protected virtual void Awake() {
            _interactor = GetComponent<XRRayInteractor>();
            _controller = GetComponent<ActionBasedController>();
            SetupPhysicsHands();
            SetupNonPhysicsHands();
        }

        #endregion

        #region OnEnable

        protected virtual void OnEnable() {
            lastPosition = transform.position;
            lastPositionPhysics = lastPosition;
            EnableInput();
        }

        #endregion

        #region OnDisable

        protected virtual void OnDisable() {
            DisableInput();
        }

        #endregion

        #region EnableInput

        private void EnableInput() {
            // xrcontroller/position:
            _positionAction = positionAction.action;
            _positionAction?.Enable();
            // xrcontroller/rotation:
            _rotationAction = rotationAction.action;
            _rotationAction?.Enable();
            // xrcontroller/activate:
            _primaryAction = primaryAction.action;
            _primaryAction?.Enable();
            // xrcontroller/primary button:
            _secondaryAction = secondaryAction.action;
            _secondaryAction?.Enable();
            // xrcontroller/secondary button:
            _tertiaryAction = tertiaryAction.action;
            _tertiaryAction?.Enable();
            // xrcontroller/select:
            _gripAction = gripAction.action;
            _gripAction?.Enable();
        }

        #endregion

        #region DisableInput

        private void DisableInput() {
            _positionAction?.Enable();
            _rotationAction?.Enable();
            _primaryAction?.Enable();
            _secondaryAction?.Enable();
            _tertiaryAction?.Enable();
            _gripAction?.Enable();
        }

        #endregion

        #region SetupPhysicsHands

        private void SetupPhysicsHands() {
            if (physicsModelPrefab != null) {
                GameObject instance = Instantiate(
                    physicsModelPrefab,
                    transform.position,
                    transform.rotation,
                    null//parent
                );
                rigidbody = instance.GetComponent<Rigidbody>();
                if (rigidbody == null) {
                    ConsoleFormatter.Error("No Rigidbody component found on physics hand variant instance.");
                } else {
                    rigidbody.useGravity = false;
                }
                physicsAnimator = instance.GetComponent<Animator>();
                if (physicsAnimator == null) {
                    ConsoleFormatter.Error("No Animator component found on physics hand variant instance.");
                } else {
                    physicsAnimator.updateMode = AnimatorUpdateMode.AnimatePhysics;
                }
                physicsColliders = instance.GetColliders(false).AddFirst(_locomotion.controller);
                physicsRenderer = instance.GetComponentInChildren<Renderer>();
                XRPhysicsHand physicsHand = instance.GetComponent<XRPhysicsHand>();
                if (physicsHand == null) {
                    ConsoleFormatter.Error($"Referenced {nameof(physicsModelPrefab)} should have a {nameof(XRPhysicsHand)} component.");
                    gripUpperRayOrigin = null;
                    gripLowerRayOrigin = null;
                } else {
                    gripUpperRayOrigin = physicsHand.gripUpperRayOrigin;
                    gripLowerRayOrigin = physicsHand.gripLowerRayOrigin;
                }
            } else {
                physicsColliders = new Collider[0];
                physicsRenderer = null;
                gripUpperRayOrigin = null;
                gripLowerRayOrigin = null;
            }
        }

        #endregion

        #region SetupNonPhysicsHands

        private void SetupNonPhysicsHands() {
            Transform model = _controller.model;
            if (model != null) {
                nonPhysicsAnimator = model.GetComponent<Animator>();
                nonPhysicsRenderer = model.GetComponentInChildren<Renderer>();
            } else {
                nonPhysicsAnimator = null;
                nonPhysicsRenderer = null;
            }
        }

        #endregion

        #region Update

        protected virtual void Update() {
            float deltaTime = Time.deltaTime;
            // update velocity:
            Vector3 inputPosition = _positionAction.ReadValue<Vector3>();
            Vector3 localVelocity = (inputPosition - lastInputPosition) * (1.0f / deltaTime);
            RecordVelocity(transform.parent != null ? transform.parent.TransformDirection(localVelocity) : localVelocity);
            lastInputPosition = inputPosition;
            // update position and rotation:
            transform.localPosition = inputPosition;
            transform.localRotation = _rotationAction.ReadValue<Quaternion>();
            // apply grip:
            gripAmount.Apply(_gripAction.ReadValue<float>(), GripAmountSmoothing * deltaTime);
            _interactor.allowSelect = gripAmount > 0.0f; // toggle allowing select by how much grip is applied
            if (physicsAnimator != null) physicsAnimator.SetFloat(GripAnimatorPropertyName, gripAmount.value);
            if (nonPhysicsAnimator != null) nonPhysicsAnimator.SetFloat(GripAnimatorPropertyName, gripAmount.value);
            else SetupNonPhysicsHands();
            // update item input:
            if (item != null) {
                item.SetPrimaryUseState(_primaryAction.ReadValue<float>() > 0.5f);
                item.SetSecondaryUseState(_secondaryAction.ReadValue<float>() > 0.5f);
                item.SetTertiaryUseState(_tertiaryAction.ReadValue<float>() > 0.5f);
            }
            // update hand visuals:
            if (rigidbody != null && nonPhysicsRenderer != null) { // both a physics and non-physics hand exist
                if (item == null) { // hand is not holding an item
                    Vector3 deltaPosition = transform.position - rigidbody.position; // find the vector from the physics to the non-physics hand
                    float sqrDistance = deltaPosition.sqrMagnitude; // find the square distance between the physics and non-physics hands
                    nonPhysicsRenderer.enabled = sqrDistance > RenderNonPhysicsHandsSqrDistance; // if the distance between the two hands is greater than a threshold value, enable the non-physics hand renderer
                } else { // item in hand, never show the non-physics renderer
                    nonPhysicsRenderer.enabled = false;
                }
            }
        }

        #endregion

        #region LateUpdate

        protected virtual void LateUpdate() {
            if (skipHandUpdate) { // skip the current update
                skipHandUpdate = false;
                return;
            }
            // calculate timing variables:
            float deltaTime = Time.deltaTime; // delta time
            float inverseSqrDeltaTime = 1.0f / (deltaTime * deltaTime); // inverse delta time squared
            // calculate hand movement vector:
            Vector3 position = transform.position; // get the position of the non-physics hand
            Vector3 deltaPosition = position - lastPosition; // calculate the vector from the last hand position to the current hand position
            lastPosition = position; // update the last hand position
            // update held object:
            IXRSelectInteractable interactable = _interactor.GetOldestInteractableSelected();
            if (interactable != null && interactable is Component component && component != null) {
                UpdateHeldVisual(
                    component.transform,
                    ref lastHeldPosition,
                    ref lastHeldRotation,
                    deltaPosition,
                    inverseSqrDeltaTime
                );
            }
        }

        #endregion

        #region OnPostRender

        protected virtual void OnPostRender() {
            // reset held object position:
            IXRSelectInteractable interactable = _interactor.GetOldestInteractableSelected();
            if (interactable != null && interactable is Component component) {
                component.transform.SetPositionAndRotation(
                    lastHeldPosition,
                    lastHeldRotation
                );
            }
        }

        #endregion

        #region InternalPhysicsUpdate

        /// <summary>
        /// Invoked by the <see cref="_locomotion"/> when <see cref="XRLocomotionController.FixedUpdate"/> is invoked.
        /// </summary>
        /// <param name="deltaTime"><see cref="Time.fixedDeltaTime"/></param>
        protected internal virtual void InternalPhysicsUpdate(in float deltaTime) {
            UpdatePhysicsHand(deltaTime);
            UpdateCollisionTrackers(deltaTime);
            lastPositionPhysics = transform.position;
        }

        #endregion

        #region RecordVelocity

        private void RecordVelocity(in Vector3 velocity) {
            _localVelocity = velocity;
            _smoothLocalVelocity = velocity;
            Vector3 v;
            for (int i = VelocitySmoothingBufferSize - 1; i > 0; i--) {
                v = velocityBuffer[i - 1];
                velocityBuffer[i] = v;
                _smoothLocalVelocity += v;
            }
            velocityBuffer[0] = velocity;
            _smoothLocalVelocity *= VelocitySmoothingCoefficient;
        }

        #endregion

        #region UpdatePhysicsHand

        private void UpdatePhysicsHand(in float deltaTime) {
            if (rigidbody == null) return; // no physics hand exists
            if (gripTracker != null) { // hand is gripping an object
                if (gripAmount.value < 0.5f) { // lost grip
                    gripTracker = null;
                } else { // hand is still gripping current object
                    UpdateGripHand(deltaTime);
                }
            } else if (CanGripObject() && gripAmount.value > 0.5f && TryGrip()) { // hand wants to start gripping an object
                UpdateGripHand(deltaTime);
            } else { // hand is not gripping an object
                UpdateFreeHand(deltaTime);
            }
        }

        #endregion

        #region UpdateFreeHand

        /// <summary>
        /// Updates the hand as if it is not gripping an object. This is not the same thing as if the hand is not holding an object.
        /// It simply means the hand is not physically gripping a non item object.
        /// </summary>
        private void UpdateFreeHand(in float deltaTime) {
            // timing:
            float inverseDeltaTime = 1.0f / deltaTime;
            // calculate positional data:
            Vector3 targetPosition = transform.position;
            Vector3 actualPosition = rigidbody.transform.position;
            Vector3 deltaPosition = targetPosition - actualPosition;
            // distance check:
            float deltaPositionSqrDistance = deltaPosition.sqrMagnitude; // find the square distance between where the hand is and where it should be
            if (deltaPositionSqrDistance > MaxSqrHandDistance) { // hand has moved too far from the target position
                // teleport hand to the target position:
                rigidbody.position = targetPosition;
                rigidbody.rotation = transform.rotation;
                // remove velocity and angular velocity:
                rigidbody.velocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            } else { // hands are within an appropriate distance of the target hand position
                // velocity tracking:
                Vector3 velocity = deltaPosition * (velocityScale * inverseDeltaTime);
                if (!float.IsNaN(velocity.x)) {
                    rigidbody.velocity = velocity;
                }
                // angular velocity tracking:
                Quaternion targetRotation = transform.rotation;
                Quaternion actualRotation = rigidbody.transform.rotation;
                Quaternion deltaRotation = targetRotation * Quaternion.Inverse(actualRotation);
                deltaRotation.ToAngleAxis(out float angleDegrees, out Vector3 axis);
                if (angleDegrees > 180.0f) angleDegrees -= 360.0f;
                if (Mathf.Abs(angleDegrees) > Mathf.Epsilon) {
                    Vector3 angularVelocity = axis * (angleDegrees * Mathf.Deg2Rad * inverseDeltaTime);
                    if (!float.IsNaN(angularVelocity.x)) {
                        rigidbody.angularVelocity = angularVelocity * angularVelocityScale;
                    }
                }
                // push force:
                if (deltaPositionSqrDistance > ThresholdSqrPushDistance) { // this distance is greater than 5cm, apply a force
                    float deltaPositionDistance = Mathf.Sqrt(deltaPositionSqrDistance); // calculate the distance between the physics hand and the target hand position
                    float actualDistance = Mathf.Min(deltaPositionDistance, maxPushHandDistance) - ThresholdPushDistance; // remove the threshold push distance from the total distance
                    // calucalte the push force:
                    Vector3 pushForce = deltaPosition * (-pushStrength * actualDistance * actualDistance / deltaPositionDistance); // normalize the push direction and apply coefficients
                    _locomotion.AddForce(pushForce, ForceMode.VelocityChange); // apply force to the locomotion controller
                }
            }
        }

        #endregion

        #region UpdateGripHand

        /// <summary>
        /// Updates the hand as if it was gripping a non-item item.
        /// </summary>
        private void UpdateGripHand(in float deltaTime) {
            // calculate world vectors:
            Vector3 handPosition = transform.position;
            Vector3 gripPosition = gripTracker.GetWorldGripPoint();
            // move physics hand:
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.position = gripPosition;
            //rigidbody.rotation = ;
            // calculate force vector:
            Vector3 deltaPosition = gripPosition - handPosition; // calculate the difference in position between the hand and the target point
            float deltaPositionDistance = deltaPosition.magnitude; // calculate the distance between the hand and the grip point
            if (deltaPositionDistance > Mathf.Epsilon) { // the hand is far from the target point
                Vector3 velocity = (handPosition - lastPositionPhysics) * (1.0f / deltaTime); // estimate the velocity of the hand
                Vector3 predictedPosition = handPosition + (velocity * GripMoveTime); // predict the position of the hand in the future
                Vector3 moveVector = (gripPosition - predictedPosition) * GripInverseMoveTime; // calculate the velocity change required to reach the target position
                float sqrMoveVectorMagnitude = moveVector.sqrMagnitude; // get move vector magnitude
                if (sqrMoveVectorMagnitude > maxGripVelocity * maxGripVelocity) moveVector *= maxGripVelocity / Mathf.Sqrt(sqrMoveVectorMagnitude); // clamp move vector magnitude
                _locomotion.AddForce(moveVector * 0.5f, ForceMode.VelocityChange); // apply velocity change to the locomotion controller
                gripTracker.AddForceAtPosition(moveVector * -0.5f, gripPosition, ForceMode.Impulse); // apply opposing force to gripped object
            }
        }

        #endregion

        #region TryGrip

        /// <summary>
        /// Attempts to grip a surface.
        /// </summary>
        /// <returns>
        /// Returns <c>true</c> if the grip operation was a success.
        /// </returns>
        private bool TryGrip() {
            // cast upper ray:
            RaycastHit hit;
            if (!Physics.Raycast(
                gripUpperRayOrigin.position,
                gripUpperRayOrigin.forward,
                out hit,
                gripRange,
                gripLayerMask,
                QueryTriggerInteraction.Ignore
            )) return false;
            Collider collider = hit.collider;
            Vector3 upperNormal = hit.normal;
            // cast lower ray:
            if (!Physics.Raycast(
                gripLowerRayOrigin.position,
                gripLowerRayOrigin.forward,
                out hit,
                gripRange,
                gripLayerMask,
                QueryTriggerInteraction.Ignore
            ) || hit.collider != collider) return false;
            Vector3 lowerNormal = hit.normal;
            // compare normals:
            float angle = Vector3.Angle(lowerNormal, upperNormal);
            if (angle < minGripAngle) return false; // angle too small to be gripped
            // create grip tracker:
            gripTracker = new XRHandGripTracker(collider, rigidbody.position);
            return true;
        }

        #endregion

        #region UpdateCollisionTrackers

        /// <summary>
        /// Updates the <see cref="collisionTrackers"/>.
        /// </summary>
        private void UpdateCollisionTrackers(in float deltaTime) {
            int collisionTrackerCount = collisionTrackers.Count; // get the number of active collision being tracked
            if (collisionTrackerCount < 1) return; // no active collisions to track
            XRHandCollisionTracker tracker;
            for (int i = collisionTrackerCount - 1; i >= 0; i--) {
                tracker = collisionTrackers[i];
                if (!tracker.IsIntersecting()) {
                    float timer = tracker.timer;
                    if (timer > deltaTime) tracker.timer = timer - deltaTime;
                    else {
                        tracker.EnableCollisions();
                        collisionTrackers.RemoveAt(i);
                    }
                } else {
                    tracker.ResetTimer();
                }
            }
        }

        #endregion

        #region UpdateHeldVisual

        /// <summary>
        /// Updates a held transform visual to match up with where it should be. This is not the same as where
        /// the physics system thinks the object is, so it must be visually updated.
        /// </summary>
        /// <param name="transform"><see cref="Transform"/> component on the <see cref="Rigidbody"/> being updated.</param>
        /// <param name="lastPosition">Position of the <paramref name="transform"/> last time this method was invoked for the <paramref name="transform"/>.</param>
        /// <param name="lastRotation">Rotation of the <paramref name="transform"/> last time this method was invoked for the <paramref name="transform"/>.</param>
        /// <param name="deltaHandPosition">Vector that the hand has moved since the last frame.</param>
        /// <param name="inverseSqrDeltaTime">Inverse delta time squared.</param>
        private static void UpdateHeldVisual(
            in Transform transform,
            ref Vector3 lastPosition,
            ref Quaternion lastRotation,
            in Vector3 deltaHandPosition,
            in float inverseSqrDeltaTime
        ) {
            Vector3 position = transform.position;
            Quaternion rotation = transform.rotation;
            Vector3 deltaPosition = position - lastPosition;
            float sqrSpeed = deltaPosition.sqrMagnitude * inverseSqrDeltaTime;
            if (sqrSpeed < RigidbodySmoothingMaxSpeed) {
                float speed = Mathf.Sqrt(sqrSpeed);
                float smoothing = RigidbodyMinSmoothing + (speed * RigidbodySmoothingCoefficient);
                transform.position = lastPosition + (deltaPosition * smoothing) + deltaHandPosition;
            } else {
                transform.position = position + deltaHandPosition;
            }
            lastPosition = position;
            lastRotation = rotation;
        }

        #endregion

        #region ResetItemCollision

        private void ResetItemCollision() {
            Rigidbody itemRigidbody = item.rigidbody;
            itemRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            itemRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            itemRigidbody.detectCollisions = true;
            itemRigidbody.isKinematic = false;
            Transform itemTransform = item.transform;
            lastHeldPosition = itemTransform.position;
            lastHeldRotation = itemTransform.rotation;
        }

        #endregion

        #region IgnoreCollisionWithCollider

        private void IgnoreCollisionWithCollider(in Collider collider, in bool ignore) {
            Collider physicsCollider;
            for (int i = physicsColliders.Length - 1; i >= 0; i--) {
                physicsCollider = physicsColliders[i];
                Physics.IgnoreCollision(collider, physicsCollider, ignore);
            }
        }

        #endregion

        #region IgnoreCollisionWithColliders

        private void IgnoreCollisionWithColliders(in Collider[] colliders, in bool ignore) {
            int colliderCount = colliders.Length;
            Collider handCollider;
            Collider currentCollider;
            for (int i = physicsColliders.Length - 1; i >= 0; i--) {
                handCollider = physicsColliders[i];
                for (int j = 0; j < colliderCount; j++) {
                    currentCollider = colliders[j];
                    Physics.IgnoreCollision(handCollider, currentCollider, ignore);
                }
            }
        }

        #endregion

        // holding (physical rigidbody object held in hand):

        #region IsHoldingItem

        public bool IsHoldingItem() => item != null;

        public bool IsHoldingItem(in WorldItem item) => item != null && this.item == item;

        #endregion

        #region CanTakeItem

        /// <returns>
        /// Returns <c>true</c> if the <see cref="XRHandController"/> can pick up an item.
        /// </returns>
        public virtual bool CanTakeItem() => IsEmpty;

        /// <inheritdoc cref="IItemHolder.CanTakeItem(in WorldItem, in IItemHolder)"/>
        public bool CanTakeItem(in WorldItem item, in IItemHolder holder) => item != null && CanTakeItem();

        #endregion

        #region TryHold

        /// <summary>
        /// Tells the <see cref="XRHandController"/> to try and hold an <see cref="XRGrabInteractable"/>.
        /// </summary>
        /// <returns>
        /// Returns <c>true</c> if the hold operation was successful.
        /// </returns>
        public bool TryHold(in XRGrabInteractable interactable) {
            if (interactable == null || !IsEmpty) return false; // hands are not empty, a new object cannot be held
            IXRSelectInteractable interactableInterface = interactable; // get the interactable interface
            //_interactor.useForceGrab = true;
            _interactor.StartManualInteraction(interactableInterface); // manually start an interaction with the target
            InitialiseHeldObjectData(interactable);
            return true; // success
        }

        #endregion

        #region TryRelease

        /// <summary>
        /// Tells the <see cref="XRHandController"/> to try to release the currently held object.
        /// </summary>
        /// <returns>
        /// Returns <c>true</c> if the release operation was successful.
        /// </returns>
        public bool TryRelease() {
            if (!_interactor.isPerformingManualInteraction) return false;
            IXRSelectInteractable interactableInterface = _interactor.GetOldestInteractableSelected();
            if (interactableInterface != null && interactableInterface is XRGrabInteractable interactable) {
                ReleaseHeldItem(interactable);
                Rigidbody rigidbody = interactable.GetComponent<Rigidbody>();
                if (rigidbody != null) {
                    rigidbody.velocity = (_smoothLocalVelocity + _locomotion._velocity) * interactable.throwVelocityScale;
                    rigidbody.angularVelocity = Vector3.zero;
                }
            }
            _interactor.EndManualInteraction();
            return true;
        }

        #endregion

        #region ForceDrop

        /// <summary>
        /// Forces the <see cref="XRHandController"/> to drop any held items.
        /// </summary>
        public void ForceDrop() {
            if (_interactor.isPerformingManualInteraction) {
                _interactor.EndManualInteraction();
            }
            _interactor.allowSelect = false;
        }

        #endregion

        #region OnHoldItem

        public virtual void OnHoldItem(in WorldItem item) {
            if (item == null) throw new ArgumentNullException(nameof(item));
            // assign item:
            this.item = item;
            // hide physics hands:
            if (item.hideXRHands && physicsRenderer != null) physicsRenderer.enabled = false;
            // reset item collision:
            ResetItemCollision();
            // check for grab interactable:
            XRGrabInteractable interactable = item.GetComponent<XRGrabInteractable>();
            InitialiseHeldObjectData(interactable);
        }

        #endregion

        #region OnReleaseItem

        public virtual void OnReleaseItem(in WorldItem item) {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (this.item != item) return; // item released is not the held item
            physicsRenderer.enabled = true; // re-enable physics renderer (if disabled)
            // reset item collision:
            ResetItemCollision();
            // process item colliders:
            XRGrabInteractable interactable = item.GetComponent<XRGrabInteractable>();
            ReleaseHeldItem(interactable);
            // reset item state:
            item.SetPrimaryUseState(false);
            item.SetSecondaryUseState(false);
            item.SetTertiaryUseState(false);
            // remove item reference:
            this.item = null;
            // skip hand update:
            skipHandUpdate = true;
        }

        #endregion

        #region InitialiseHeldObjectData

        /// <summary>
        /// Initialises data used for tracking the currently held <paramref name="interactable"/>.
        /// </summary>
        private void InitialiseHeldObjectData(in XRGrabInteractable interactable) {
            if (interactable != null) { // the item has a valid XR interactable
                // get references:
                Transform interactableTransform = interactable.transform;
                Transform attachTransform = interactable.attachTransform;
                if (attachTransform != null) {
                    // calculate offsets:
                    heldPositionalOffset = -interactableTransform.InverseTransformPoint(attachTransform.position);
                    heldRotationalOffset = Quaternion.Inverse(attachTransform.rotation) * interactableTransform.rotation;
                } else {
                    heldPositionalOffset = Vector3.zero;
                    heldRotationalOffset = Quaternion.identity;
                }
                // check if interactable is already tracked:
                bool disableCollision = true;
                int trackerCount = collisionTrackers.Count; // get the number of collision trackers
                if (trackerCount > 0) { // there are collision trackers
                    XRHandCollisionTracker tracker; // current tracker
                    for (int i = 0; i < trackerCount; i++) { // iterate each tracker
                        tracker = collisionTrackers[i]; // get the current tracker
                        if (tracker.interactable == interactable) { // item already tracked by the current tracker
                            disableCollision = false; // no need to disable collision again
                            break;
                        }
                    }
                }
                // process colliders:
                heldColliders = interactable.gameObject.GetColliders(false);
                if (disableCollision) {
                    IgnoreCollisionWithColliders(heldColliders, true);
                }
                physicsColliderBuffer = new Collider[heldColliders.Length + 1];
            } else {
                heldPositionalOffset = Vector3.zero;
                heldRotationalOffset = Quaternion.identity;
                heldColliders = null;
            }
        }

        #endregion

        #region ReleaseHeldItem

        /// <summary>
        /// Releases the <paramref name="interactable"/> and begins tracking collisions with the <paramref name="interactable"/>.
        /// </summary>
        private void ReleaseHeldItem(in XRGrabInteractable interactable) {
            if (heldColliders != null) {
                collisionTrackers.Add(
                    new XRHandCollisionTracker(
                        interactable,
                        heldColliders,
                        physicsColliders
                    )
                );
                heldColliders = null;
            }
        }

        #endregion

        // gripping (an object in the scene that is being gripped by the hand):

        #region IsGrippingObject

        /// <returns>
        /// Returns <c>true</c> if the <see cref="XRHandController"/> is gripping an object.
        /// </returns>
        public bool IsGrippingObject() => gripTracker != null;

        /// <returns>
        /// Returns <c>true</c> if the <see cref="XRHandController"/> is the <paramref name="transform"/> specifically.
        /// </returns>
        public bool IsGrippingObject(in Transform transform) => gripTracker != null && gripTracker.transform == transform;

        #endregion

        #region CanGripObject

        /// <returns>
        /// Returns <c>true</c> when the <see cref="XRHandController"/> is in a state that it could grip an object if it wanted to.
        /// </returns>
        public virtual bool CanGripObject() => IsEmpty;

        #endregion

        #endregion

    }

}

#endif