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
    [RequireComponent(typeof(ActionBasedController))]
#if UNITY_EDITOR
    [AddComponentMenu("XR/XR Hand Controller (Action-based)")]
#endif
    public sealed class XRHandController : MonoBehaviour, IItemHolder {

        #region constant

        private static readonly ConsoleFormatter ConsoleFormatter = new ConsoleFormatter(nameof(XRHandController));

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
        private const float ThresholdPushDistance = 0.005f;

        /// <summary>
        /// <see cref="ThresholdPushDistance"/> squared.
        /// </summary>
        private const float ThresholdSqrPushDistance = ThresholdPushDistance * ThresholdPushDistance;

        #endregion

        #region variable

        /// <summary>
        /// Reference to the <see cref="XRLocomotionController"/> responsible for moving the XR player.
        /// </summary>
        [SerializeField]
        private XRLocomotionController locomotionController = null;

        /// <summary>
        /// Scalar that controls how much force the hand exerts on the player when colliding with a surface.
        /// </summary>
#if UNITY_EDITOR
        [Min(0.0f)]
#endif
        [SerializeField]
        private float pushForceCoefficient = 5000.0f;

        /// <summary>
        /// Maximum distance between the physics hand and the actual hand position that will be converted into a pushing force.
        /// </summary>
#if UNITY_EDITOR
        [Min(ThresholdPushDistance)]
#endif
        [SerializeField]
        private float maxPushHandDistance = 0.2f;

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

        /// <summary>
        /// Layermask used to detect objects that an item can collide with.
        /// This is used to switch between smooth item modes.
        /// </summary>
        [SerializeField]
        private LayerMask itemCollisionLayerMask = -1;

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

        private ActionBasedController controller = null;

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
        /// <see cref="Collider"/> array containing every collider childed to the <see cref="rigidbody"/>.
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
        /// Item positional offset (defined on the <see cref="item"/>).
        /// </summary>
        private Vector3 itemPositionalOffset = Vector3.zero;

        /// <summary>
        /// Item rotational offset (defined on the <see cref="item"/>).
        /// </summary>
        private Quaternion itemRotationalOffset = Quaternion.identity;

        /// <summary>
        /// Array containing every <see cref="Collider"/> component attached to the <see cref="item"/>.
        /// </summary>
        private Collider[] itemColliders = null;

        /// <summary>
        /// <see cref="Collider"/> buffer used for physics casting operations.
        /// </summary>
        private Collider[] physicsColliderBuffer = new Collider[0];

        /// <summary>
        /// Smooth grip amount.
        /// </summary>
        private SmoothFloat gripAmount = new SmoothFloat(0.0f);

        /// <summary>
        /// Position of the <see cref="transform"/> in the previous frame.
        /// This is used to predict where an object should be in the current frame.
        /// </summary>
        private Vector3 lastPosition = Vector3.zero;

        /// <summary>
        /// Position of the <see cref="item"/> before transforming it to look as if the item is where the player's hand is.
        /// </summary>
        private Vector3 lastItemPosition = Vector3.zero;

        /// <summary>
        /// Rotation of the <see cref="item"/> before transforming it to look as if the item is where the player's hand is.
        /// </summary>
        private Quaternion lastItemRotation = Quaternion.identity;

        /// <summary>
        /// List of <see cref="XRHandCollisionTracker"/> instances.
        /// </summary>
        private readonly List<XRHandCollisionTracker> collisionTrackers = new List<XRHandCollisionTracker>();

        /// <summary>
        /// When <c>true</c>, the next hand update will be skipped.
        /// </summary>
        /// <remarks>
        /// Without this, a weird physics bug is introduced where the hand will teleport
        /// far from the position it should be in. This is caused by the deltaHandPosition
        /// vector being very large. It is unclear why this happens.
        /// </remarks>
        private bool skipHandUpdate = false;

        #endregion

        #region logic

        #region Awake

        private void Awake() {
            controller = GetComponent<ActionBasedController>();
            SetupPhysicsHands();
            SetupNonPhysicsHands();
        }

        #endregion

        #region OnEnable

        private void OnEnable() {
            EnableInput();
        }

        #endregion

        #region OnDisable

        private void OnDisable() {
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
                Transform parent = controller.modelParent;
                GameObject instance = Instantiate(
                    physicsModelPrefab,
                    parent.position,
                    parent.rotation,
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
                physicsColliders = instance.GetColliders(false);
                physicsRenderer = instance.GetComponentInChildren<Renderer>();
            } else {
                physicsColliders = new Collider[0];
                physicsRenderer = null;
            }
        }

        #endregion

        #region SetupNonPhysicsHands

        private void SetupNonPhysicsHands() {
            Transform model = controller.model;
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

        private void Update() {
            float deltaTime = Time.deltaTime;
            // apply position and rotation:
            transform.localPosition = _positionAction.ReadValue<Vector3>();
            transform.localRotation = _rotationAction.ReadValue<Quaternion>();
            // apply grip:
            gripAmount.Apply(_gripAction.ReadValue<float>(), GripAmountSmoothing * deltaTime);
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

        #region InternalPhysicsUpdate

        internal void InternalPhysicsUpdate(in float deltaTime) {
            // update physics hand:
            if (rigidbody != null) {
                // timing:
                float inverseDeltaTime = 1.0f / deltaTime;
                // velocity tracking:
                Vector3 targetPosition = transform.position;
                Vector3 actualPosition = rigidbody.transform.position;
                Vector3 deltaPosition = targetPosition - actualPosition;
                Vector3 velocity = deltaPosition * inverseDeltaTime;
                if (!float.IsNaN(velocity.x)) {
                    rigidbody.velocity = velocity * velocityScale;
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
                float deltaPositionSqrDistance = deltaPosition.sqrMagnitude; // find the square distance between where the hand is and where it should be
                if (deltaPositionSqrDistance > ThresholdSqrPushDistance) { // this distance is greater than 5cm, apply a force
                    float deltaPositionDistance = Mathf.Sqrt(deltaPositionSqrDistance); // calculate the distance between the physics hand and the target hand position
                    float actualDistance = Mathf.Min(deltaPositionDistance, maxPushHandDistance) - ThresholdPushDistance; // remove the threshold push distance from the total distance
                    // calucalte the push force:
                    Vector3 pushForce = deltaPosition * (-pushForceCoefficient * actualDistance / deltaPositionDistance); // normalize the push direction and apply coefficients
                    locomotionController.AddForce(pushForce, ForceMode.Force); // apply force to the locomotion controller
                }
            }
            // update collision trackers:
            int collisionTrackerCount = collisionTrackers.Count;
            if (collisionTrackerCount > 0) {
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
        }

        #endregion

        #region LateUpdate

        private void LateUpdate() {
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
            // update item:
            if (item != null) {
                UpdateHeldVisual(
                    item.transform,
                    ref lastItemPosition,
                    ref lastItemRotation,
                    deltaPosition,
                    inverseSqrDeltaTime
                );
            }
        }

        #endregion

        #region OnPostRender

        private void OnPostRender() {
            // reset item position:
            if (item != null) {
                item.transform.SetPositionAndRotation(
                    lastItemPosition,
                    lastItemRotation
                );
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
            lastItemPosition = itemTransform.position;
            lastItemRotation = itemTransform.rotation;
        }

        #endregion

        #region IsHoldingItem

        public bool IsHoldingItem() => item != null;

        public bool IsHoldingItem(in WorldItem item) => item != null && this.item == item;

        #endregion

        #region CanTakeItem

        public bool CanTakeItem(in WorldItem item, in IItemHolder holder) => item != null && item != this.item;

        #endregion

        #region IgnoreCollisionWithItemCollider

        private void IgnoreCollisionWithItemCollider(in Collider collider, in bool ignore) {
            Collider physicsCollider;
            for (int i = physicsColliders.Length - 1; i >= 0; i--) {
                physicsCollider = physicsColliders[i];
                Physics.IgnoreCollision(collider, physicsCollider, ignore);
            }
        }

        #endregion

        #region OnHoldItem

        public void OnHoldItem(in WorldItem item) {
            if (item == null) throw new ArgumentNullException(nameof(item));
            // assign item:
            this.item = item;
            // hide physics hands:
            if (item.hideXRHands && physicsRenderer != null) physicsRenderer.enabled = false;
            // reset item collision:
            ResetItemCollision();
            // check for grab interactable:
            XRGrabInteractable xrGrabInteractable = item.GetComponent<XRGrabInteractable>();
            if (xrGrabInteractable != null && xrGrabInteractable.attachTransform != null) { // the item has a valid XR interactable
                itemPositionalOffset = -item.transform.InverseTransformPoint(xrGrabInteractable.attachTransform.position);
                itemRotationalOffset = Quaternion.Inverse(xrGrabInteractable.attachTransform.rotation) * item.transform.rotation;
                List<Collider> colliderList = new List<Collider>(item.GetComponentsInChildren<Collider>());
                Collider currentCollider;
                bool disableCollision = true;
                // check if item is already tracked:
                int trackerCount = collisionTrackers.Count;
                if (trackerCount > 0) {
                    XRHandCollisionTracker tracker;
                    for (int i = 0; i < trackerCount; i++) {
                        tracker = collisionTrackers[i];
                        if (tracker.item == item) { // item already tracked
                            disableCollision = false;
                            break;
                        }
                    }
                }
                // process colliders:
                for (int i = colliderList.Count - 1; i >= 0; i--) {
                    currentCollider = colliderList[i];
                    if (currentCollider.isTrigger) colliderList.RemoveAt(i);
                    else if (disableCollision) IgnoreCollisionWithItemCollider(currentCollider, true);
                }
                itemColliders = colliderList.ToArray();
                physicsColliderBuffer = new Collider[itemColliders.Length + 1];
            } else {
                itemPositionalOffset = Vector3.zero;
                itemRotationalOffset = Quaternion.identity;
                itemColliders = null;
            }
        }

        #endregion

        #region OnReleaseItem

        public void OnReleaseItem(in WorldItem item) {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (this.item != item) return; // item released is not the held item
            physicsRenderer.enabled = true; // re-enable physics renderer (if disabled)
            // reset item collision:
            ResetItemCollision();
            // process item colliders:
            if (itemColliders != null) {
                collisionTrackers.Add(
                    new XRHandCollisionTracker(
                        item,
                        itemColliders,
                        physicsColliders
                    )
                );
                itemColliders = null;
            }
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

        #endregion

    }

}

#endif