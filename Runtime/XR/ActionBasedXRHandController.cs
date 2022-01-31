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
    public sealed class ActionBasedXRHandController : MonoBehaviour, IItemHolder {

        #region constant

        private static readonly ConsoleFormatter ConsoleFormatter = new ConsoleFormatter(nameof(ActionBasedXRHandController));

        /// <summary>
        /// Field on the <see cref="nonPhysicsAnimator"/> used for gripping.
        /// </summary>
        public const string GripAnimatorPropertyName = "Grip";

        /// <summary>
        /// Amount of smoothing to apply to the field on the <see cref="nonPhysicsAnimator"/> named <see cref="GripAnimatorPropertyName"/>.
        /// </summary>
        private const float GripAmountSmoothing = 10.0f;

        /// <summary>
        /// Minimum percentage of the velocity of the <see cref="rigidbody"/> to transform the <see cref="rigidbody"/> by while smoothing.
        /// </summary>
        private const float RigidbodyMinSmoothing = 0.1f;

        /// <summary>
        /// Maximum speed that the <see cref="rigidbody"/> can travel while still having smoothing applied.
        /// Smoothing will be applied relative to how fast the <see cref="rigidbody"/> is travelling.
        /// </summary>
        private const float RigidbodyThresholdSmoothingSpeed = 0.75f;

        private const float RigidbodyThresholdSmoothingSqrSpeed = RigidbodyThresholdSmoothingSpeed * RigidbodyThresholdSmoothingSpeed;

        private const float RigidbodySmoothingCoefficient = (1.0f - RigidbodyMinSmoothing) / RigidbodyThresholdSmoothingSpeed;

        /// <summary>
        /// Minimum percentage of the velocity to transform an item by while smoothing.
        /// </summary>
        private const float ItemMinSmoothing = 0.1f;

        /// <summary>
        /// Velocity that if an item is travelling at, it should have it's position smoothed.
        /// This makes aiming and holding items in still positions better since they jitter less.
        /// </summary>
        /// <remarks>
        /// When items exceed this value, the jitter is less noticable. Smoothing will be applied more
        /// or less depending how close to this value the velocity is. This should occur linearly.
        /// </remarks>
        private const float ItemSmoothingThresholdVelocity = 0.75f;

        /// <summary>
        /// <see cref="ItemSmoothingThresholdVelocity"/> squared. Constant required for optimization.
        /// </summary>
        private const float ItemThresholdSmoothingSqrSpeed = ItemSmoothingThresholdVelocity * ItemSmoothingThresholdVelocity;

        /// <summary>
        /// Coefficient to convert a velocity into a smoothing factor between <c>0.0f</c> and <c>1.0 - <see cref="ItemMinSmoothing"/></c>.
        /// </summary>
        private const float ItemSmoothingCoefficient = (1.0f - ItemMinSmoothing) / ItemSmoothingThresholdVelocity;

        /// <summary>
        /// Square distance between the physics hands and the non-physics hands that will result in the non-physics hands being rendered.
        /// </summary>
        private const float RenderNonPhysicsHandsSqrDistance = 0.1f * 0.1f;

        #endregion

        #region variable

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
        /// Layermask used to detect objects that an item can collide with.
        /// This is used to switch between smooth item modes.
        /// </summary>
        [SerializeField]
        private LayerMask itemCollisionLayerMask = -1;

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
        /// <see cref="Transform"/> component attached to the non-physics hand variant.
        /// </summary>
        private Transform nonPhysicsTransform = null;

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
        /// Tracks if the item should collide with things or use smooth movement instead.
        /// </summary>
        private bool useCollisions = false;

        /// <summary>
        /// Smooth grip amount.
        /// </summary>
        private SmoothFloat gripAmount = new SmoothFloat(0.0f);

        /// <summary>
        /// Position of the <see cref="transform"/> in the previous frame.
        /// This is used to predict where an object should be in the current frame.
        /// </summary>
        private Vector3 lastHandPosition = Vector3.zero;

        /// <summary>
        /// Position of the <see cref="item"/> before transforming it to look as if the item is where the player's hand is.
        /// </summary>
        private Vector3 lastItemPosition = Vector3.zero;

        /// <summary>
        /// Rotation of the <see cref="item"/> before transforming it to look as if the item is where the player's hand is.
        /// </summary>
        private Quaternion lastItemRotation = Quaternion.identity;

        /// <summary>
        /// Position of the <see cref="rigidbody"/> before transforming it to look as if the hand is where the player's hand should be.
        /// </summary>
        private Vector3 lastRigidbodyPosition = Vector3.zero;

        /// <summary>
        /// Rotation of the <see cref="rigidbody"/> before transforming it to look as if the hand is where the player's hand should be.
        /// </summary>
        private Quaternion lastRigidbodyRotation = Quaternion.identity;

        /// <summary>
        /// List of <see cref="XRHandCollisionTracker"/> instances.
        /// </summary>
        private List<XRHandCollisionTracker> collisionTrackers = new List<XRHandCollisionTracker>();

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
            _primaryAction = primaryAction.action;     // xrcontroller/activate
            _secondaryAction = secondaryAction.action; // xrcontroller/primary button
            _tertiaryAction = tertiaryAction.action;   // xrcontroller/secondary button
            _gripAction = gripAction.action;           // xrcontroller/select
            SetupPhysicsHands();
            SetupNonPhysicsHands();
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
            nonPhysicsTransform = model;
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
            gripAmount.Apply(_gripAction.ReadValue<float>(), GripAmountSmoothing * deltaTime);
            if (physicsAnimator != null) physicsAnimator.SetFloat(GripAnimatorPropertyName, gripAmount.value);
            if (nonPhysicsAnimator != null) nonPhysicsAnimator.SetFloat(GripAnimatorPropertyName, gripAmount.value);
            else SetupNonPhysicsHands();
            if (item != null) {
                item.SetPrimaryUseState(_primaryAction.ReadValue<float>() > 0.5f);
                item.SetSecondaryUseState(_secondaryAction.ReadValue<float>() > 0.5f);
                item.SetTertiaryUseState(_tertiaryAction.ReadValue<float>() > 0.5f);
            }
            if (rigidbody != null && nonPhysicsRenderer != null) {
                if (item == null) {
                    Vector3 deltaPosition = rigidbody.position - nonPhysicsTransform.position;
                    float sqrDistance = deltaPosition.sqrMagnitude;
                    nonPhysicsRenderer.enabled = sqrDistance > RenderNonPhysicsHandsSqrDistance;
                } else { // item in hand, never show the non-physics renderer
                    nonPhysicsRenderer.enabled = false;
                }
            }
        }

        #endregion

        #region FixedUpdate

        private void FixedUpdate() {
            float deltaTime = Time.fixedDeltaTime;
            // update physics hand:
            if (nonPhysicsTransform != null && rigidbody != null) {
                float inverseDeltaTime = 1.0f / deltaTime;
                Vector3 deltaPosition = nonPhysicsTransform.position - rigidbody.position;
                Vector3 velocity = deltaPosition * inverseDeltaTime;
                rigidbody.velocity = velocity;
                Quaternion deltaRotation = nonPhysicsTransform.rotation * Quaternion.Inverse(rigidbody.rotation);
                deltaRotation.ToAngleAxis(out float deltaRotationDegrees, out Vector3 deltaRotationAngleAxis);
                rigidbody.angularVelocity = deltaRotationAngleAxis * (deltaRotationDegrees * Mathf.Deg2Rad * inverseDeltaTime);
            }
            // update item:
            if (item != null) { // item is being held
                if (item.physicsCulling && itemColliders != null && itemColliders.Length > 0) { // update item physics culling
                    Bounds bounds = new Bounds(item.transform.position, Vector3.zero);
                    Collider itemCollider;
                    for (int i = itemColliders.Length - 1; i >= 0; i--) {
                        itemCollider = itemColliders[i];
                        bounds.Encapsulate(itemCollider.bounds);
                    }
                    bool collision = false; // track if collisions should be enabled or not
                    int collisionCount = Physics.OverlapBoxNonAlloc( // get the number of colliders near the item
                        bounds.center,
                        bounds.extents * 2.0f,
                        physicsColliderBuffer,
                        Quaternion.identity,
                        itemCollisionLayerMask,
                        QueryTriggerInteraction.Ignore
                    );
                    if (collisionCount > 0) { // there is at least one collider near the item
                        Collider currentCollider; // store reference to current collider being evaluated
                        for (int i = collisionCount - 1; i >= 0; i--) { // iterate colliders near the item
                            currentCollider = physicsColliderBuffer[i]; // get the current collider near the item
                            if (currentCollider.isTrigger) continue; // skip triggers
                            collision = true; // enable collisions
                            for (int j = itemColliders.Length - 1; j >= 0; j--) { // iterate each collider that is part of the item
                                itemCollider = itemColliders[j]; // get the item collider
                                if (currentCollider == itemCollider) { // the current near collider is part of the item
                                    collision = false; // disable collisions
                                    break; // move onto the next near collider
                                }
                            }
                            if (collision) break; // collisions have remained on since the current near collider is not part of the item
                        }
                    }
                    if (collision != useCollisions) { // collision mode has changed
                        useCollisions = collision;
                        Rigidbody rigidbody = item.rigidbody;
                        if (useCollisions) {
                            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                            rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                            rigidbody.detectCollisions = true;
                            rigidbody.isKinematic = false;
                        } else {
                            rigidbody.interpolation = RigidbodyInterpolation.None;
                            rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                            rigidbody.detectCollisions = false;
                            rigidbody.isKinematic = true;
                        }
                    }
                }
            }
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
            if (skipHandUpdate) { // should the update be skipped?
                skipHandUpdate = false;
                return;
            }
            // calculate timing variables:
            float deltaTime = Time.deltaTime;
            float inverseDeltaTime = 1.0f / deltaTime;
            float inverseSqrDeltaTime = inverseDeltaTime * inverseDeltaTime;
            // calculate hand position variables:
            Vector3 currentHandPosition = transform.position; // get the position of the non-physics hand
            Vector3 deltaHandPosition = currentHandPosition - lastHandPosition; // calculate the vector from the last hand position to the current hand position
            lastHandPosition = currentHandPosition; // update the last hand position
            // update physics hands:
            if (rigidbody != null) {
                Transform rigidbodyTransform = rigidbody.transform;
                Vector3 currentRigidbodyPosition = rigidbodyTransform.position;
                Vector3 deltaRigidbodyPosition = currentRigidbodyPosition - lastRigidbodyPosition;
                lastRigidbodyRotation = rigidbodyTransform.rotation;
                // calculate hand speed and smoothing:
                float sqrSpeed = deltaRigidbodyPosition.sqrMagnitude * inverseSqrDeltaTime;
                if (1==2 && sqrSpeed < RigidbodyThresholdSmoothingSqrSpeed) { // within speed that smoothing is applied at
                    float speed = Mathf.Sqrt(sqrSpeed);
                    float smoothingCoefficient = RigidbodyMinSmoothing + (speed * RigidbodySmoothingCoefficient);
                    rigidbodyTransform.position = lastRigidbodyPosition + (deltaRigidbodyPosition * smoothingCoefficient) + deltaHandPosition;
                } else { // exceeded speed that smoothing is applied
                    rigidbodyTransform.position = currentRigidbodyPosition + deltaHandPosition;
                }
                lastRigidbodyPosition = currentRigidbodyPosition;
            }
            // update item:
            if (item != null) {
                Transform itemTransform = item.transform; // get reference to item transform
                Vector3 currentItemPosition = itemTransform.position;
                lastItemRotation = itemTransform.rotation; // update last item rotation
                if (useCollisions) { // item uses collisions
                    // calculate item translation vector:
                    Vector3 deltaItemPosition = currentItemPosition - lastItemPosition; // calculate vector from last item position to current item position
                    // calculate item speed and smoothing:
                    float sqrSpeed = deltaItemPosition.sqrMagnitude * inverseSqrDeltaTime;
                    if (sqrSpeed < ItemThresholdSmoothingSqrSpeed) { // within speed that smoothing is applied at
                        float speed = Mathf.Sqrt(sqrSpeed); // calculate speed of item
                        float smoothingCoefficient = ItemMinSmoothing + (speed * ItemSmoothingCoefficient); // calculate amount of smoothing to apply (higher number is less smoothing)
                        itemTransform.position = lastItemPosition + (deltaItemPosition * smoothingCoefficient) + deltaHandPosition;
                    } else { // exceeded speed that smoothing is applied
                        itemTransform.position = currentItemPosition + deltaHandPosition;
                    }
                } else { // item does not use collisions, no special calculations are required
                    itemTransform.SetPositionAndRotation(
                        transform.position + (lastItemRotation * itemPositionalOffset),
                        transform.rotation * itemRotationalOffset
                    );
                }
                lastItemPosition = currentItemPosition; // finally, update last item position
            }
        }

        #endregion

        #region OnPostRender

        private void OnPostRender() {
            if (rigidbody != null) {
                rigidbody.transform.SetPositionAndRotation(
                    lastRigidbodyPosition,
                    lastRigidbodyRotation
                );
            }
            if (item != null) { // position of the current item needs to be reset
                item.transform.SetPositionAndRotation(
                    lastItemPosition,
                    lastItemRotation
                );
            }
        }

        #endregion

        #region ResetItemCollision

        private void ResetItemCollision() {
            useCollisions = true;
            Rigidbody itemRigidbody = item.rigidbody;
            itemRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            itemRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
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