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
        /// Minimum percentage of the velocity to transform an item by while smoothing.
        /// </summary>
        private const float MinSmoothing = 0.1f;

        /// <summary>
        /// Velocity that if an item is travelling at, it should have it's position smoothed.
        /// This makes aiming and holding items in still positions better since they jitter less.
        /// </summary>
        /// <remarks>
        /// When items exceed this value, the jitter is less noticable. Smoothing will be applied more
        /// or less depending how close to this value the velocity is. This should occur linearly.
        /// </remarks>
        private const float SmoothingThresholdVelocity = 0.75f;

        /// <summary>
        /// <see cref="SmoothingThresholdVelocity"/> squared. Constant required for optimization.
        /// </summary>
        private const float SmoothingThresholdSqrVelocity = SmoothingThresholdVelocity * SmoothingThresholdVelocity;

        /// <summary>
        /// Coefficient to convert a velocity into a smoothing factor between <c>0.0f</c> and <c>1.0 - <see cref="MinSmoothing"/></c>.
        /// </summary>
        private const float SmoothingCoefficient = (1.0f - MinSmoothing) / SmoothingThresholdVelocity;

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
        /// <see cref="Rigidbody"/> component attached to the physics hand variant.
        /// </summary>
        private Rigidbody physicsRigidbody = null;

        /// <summary>
        /// <see cref="Animator"/> component on the <see cref="physicsRigidbody"/>.
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
        /// Position of the <see cref="physicsRigidbody"/> before transforming it to look as if the hand is where the player's hand should be.
        /// </summary>
        private Vector3 lastPhysicsHandPosition = Vector3.zero;

        /// <summary>
        /// Rotation of the <see cref="physicsRigidbody"/> before transforming it to look as if the hand is where the player's hand should be.
        /// </summary>
        private Quaternion lastPhysicsHandRotation = Quaternion.identity;

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
                physicsRigidbody = instance.GetComponent<Rigidbody>();
                if (physicsRigidbody == null) {
                    ConsoleFormatter.Error("No Rigidbody component found on physics hand variant instance.");
                } else {
                    physicsRigidbody.useGravity = false;
                }
                physicsAnimator = instance.GetComponent<Animator>();
                if (physicsAnimator == null) {
                    ConsoleFormatter.Error("No Animator component found on physics hand variant instance.");
                } else {
                    physicsAnimator.updateMode = AnimatorUpdateMode.AnimatePhysics;
                }
            }
        }

        #endregion

        #region SetupNonPhysicsHands

        private void SetupNonPhysicsHands() {
            Transform model = controller.model;
            nonPhysicsTransform = model;
            nonPhysicsAnimator = model != null ? model.GetComponent<Animator>() : null;
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
        }

        #endregion

        #region FixedUpdate

        private void FixedUpdate() {
            float deltaTime = Time.fixedDeltaTime;
            // update physics hand:
            if (nonPhysicsTransform != null && physicsRigidbody != null) {
                float inverseDeltaTime = 1.0f / deltaTime;
                Vector3 deltaPosition = nonPhysicsTransform.position - physicsRigidbody.position;
                Vector3 velocity = deltaPosition * inverseDeltaTime;
                physicsRigidbody.velocity = velocity;
                Quaternion deltaRotation = nonPhysicsTransform.rotation * Quaternion.Inverse(physicsRigidbody.rotation);
                deltaRotation.ToAngleAxis(out float deltaRotationDegrees, out Vector3 deltaRotationAngleAxis);
                physicsRigidbody.angularVelocity = deltaRotationAngleAxis * (deltaRotationDegrees * Mathf.Deg2Rad * inverseDeltaTime);

                //physicsRigidbody.angularVelocity = deltaRotation.eulerAngles * (Mathf.Deg2Rad * inverseDeltaTime);
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
                            lastPosition = transform.position;
                        } else {
                            rigidbody.interpolation = RigidbodyInterpolation.None;
                            rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
                            rigidbody.detectCollisions = false;
                            rigidbody.isKinematic = true;
                        }
                    }
                }
            }
        }

        #endregion

        #region LateUpdate

        private void LateUpdate() {
            if (item != null) { // item being held
                Transform itemTransform = item.transform; // get a reference to the transform component on the item being held
                Vector3 currentItemPosition = itemTransform.position; // find the current position of the item
                lastItemRotation = itemTransform.rotation; // update the last item rotation now since it can be used to cache the current rotation of the item
                if (useCollisions) { // item uses collisions, special position calculations are required to make sure the item appears to follow the hand correctly
                    // calculate hand position and translation vectors:
                    Vector3 currentHandPosition = transform.position; // get the current position of the hand
                    Vector3 deltaHandPosition = currentHandPosition - lastPosition; // calculate the vector from the last hand position to the current hand position
                    Vector3 deltaItemPosition = currentItemPosition - lastItemPosition; // calculate the vector from the last item position to the current item position
                    // calculate item velocity and smoothing:
                    float deltaTime = Time.deltaTime; // find delta time
                    float sqrVelocity = deltaItemPosition.sqrMagnitude / (deltaTime * deltaTime); // calculate the square velocity of the hand
                    if (sqrVelocity > SmoothingThresholdSqrVelocity) { // velocity of the item has exceeded the smoothing velocity, do not calculate any smoothing
                        itemTransform.position = currentItemPosition + deltaHandPosition;
                    } else { // the velocity of the item is less than the threshold velocity that smoothing will start to be applied at
                        float velocity = Mathf.Sqrt(sqrVelocity); // calculate the velocity of the hand
                        float smoothingFactor = MinSmoothing + (velocity * SmoothingCoefficient); // calculate the amount of smoothing to apply (higher number is less smoothing)
                        itemTransform.position = lastItemPosition + (deltaHandPosition * smoothingFactor) + deltaHandPosition;
                    }
                    lastPosition = currentHandPosition; // update the last position that the hand was in to the current position of the hand
                } else { // item does not use collisions, no special calculations are required
                    itemTransform.SetPositionAndRotation(
                        transform.position + (lastItemRotation * itemPositionalOffset),
                        transform.rotation * itemRotationalOffset
                    );
                }
                lastItemPosition = currentItemPosition; // finally update the last item position
            }
        }

        #endregion

        #region OnPostRender

        private void OnPostRender() {
            if (item != null) { // position of the current item needs to be reset
                item.transform.SetPositionAndRotation(lastItemPosition, lastItemRotation); // reset item transform
            }
        }

        #endregion

        #region ResetItemCollision

        private void ResetItemCollision() {
            useCollisions = true;
            Rigidbody rigidbody = item.rigidbody;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rigidbody.detectCollisions = true;
            rigidbody.isKinematic = false;
            lastItemPosition = item.transform.position;
            lastItemRotation = item.transform.rotation;
            lastPosition = transform.position;
        }

        #endregion

        #region IsHoldingItem

        public bool IsHoldingItem() => item != null;

        public bool IsHoldingItem(in WorldItem item) => item != null && this.item == item;

        #endregion

        #region CanTakeItem

        public bool CanTakeItem(in WorldItem item, in IItemHolder holder) => item != null && item != this.item;

        #endregion

        #region OnHoldItem

        public void OnHoldItem(in WorldItem item) {
            if (item == null) throw new ArgumentNullException(nameof(item));
            this.item = item;
            Transform model = controller.model;
            if (model != null) model.gameObject.SetActive(!item.hideXRHands);
            ResetItemCollision();
            XRGrabInteractable xrGrabInteractable = item.GetComponent<XRGrabInteractable>();
            if (xrGrabInteractable != null && xrGrabInteractable.attachTransform != null) {
                itemPositionalOffset = -item.transform.InverseTransformPoint(xrGrabInteractable.attachTransform.position);
                itemRotationalOffset = Quaternion.Inverse(xrGrabInteractable.attachTransform.rotation) * item.transform.rotation;
                List<Collider> colliderList = new List<Collider>(item.GetComponentsInChildren<Collider>());
                Collider currentCollider;
                for (int i = colliderList.Count - 1; i >= 0; i--) {
                    currentCollider = colliderList[i];
                    if (currentCollider.isTrigger) colliderList.RemoveAt(i);
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
            if (this.item != item) return;
            Transform model = controller.model;
            if (model != null) model.gameObject.SetActive(true);
            ResetItemCollision();
            itemColliders = null;
            item.SetPrimaryUseState(false);
            item.SetSecondaryUseState(false);
            item.SetTertiaryUseState(false);
            this.item = null;
        }

        #endregion

        #endregion

    }

}

#endif