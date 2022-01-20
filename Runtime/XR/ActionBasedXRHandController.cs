#if USE_XR_TOOLKIT

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

        public const string GripAnimatorPropertyName = "Grip";

        private const float GripAmountSmoothing = 10.0f;

        #endregion

        #region variable

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
        /// <see cref="Animator"/> component on the <see cref="handModel"/>.
        /// </summary>
        private Animator handAnimator = null;

        /// <summary>
        /// Current <see cref="WorldItem"/> that the <see cref="XRPlayerHand"/> is holding.
        /// </summary>
        private WorldItem item = null;

        private Vector3 itemPositionalOffset = Vector3.zero;
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
        /// Position of the hand in the previous frame.
        /// </summary>
        private Vector3 lastHandPosition = Vector3.zero;

        /// <summary>
        /// Position of the <see cref="item"/> before transforming it to look as if the item is where the players hands are.
        /// </summary>
        private Vector3 lastItemPosition = Vector3.zero;

        /// <summary>
        /// Rotation of the <see cref="item"/> before transforming it to look as if the item is where the players hands are.
        /// </summary>
        private Quaternion lastItemRotation = Quaternion.identity;

        #endregion

        #region logic

        #region Awake

        private void Awake() {
            controller = GetComponent<ActionBasedController>();
            _primaryAction = primaryAction.action;     // xrcontroller/activate
            _secondaryAction = secondaryAction.action; // xrcontroller/primary button
            _tertiaryAction = tertiaryAction.action;   // xrcontroller/secondary button
            _gripAction = gripAction.action;           // xrcontroller/select
            UpdateHands();
        }

        #endregion

        #region UpdateHands

        private void UpdateHands() {
            Transform model = controller.model;
            handAnimator = model != null ? model.GetComponent<Animator>() : null;
        }

        #endregion

        #region Update

        private void Update() {
            if (handAnimator != null) {
                float deltaTime = Time.deltaTime;
                gripAmount.Apply(_gripAction.ReadValue<float>(), GripAmountSmoothing * deltaTime);
                handAnimator.SetFloat(GripAnimatorPropertyName, gripAmount.value);
            } else {
                UpdateHands();
            }
            if (item != null) {
                item.SetPrimaryUseState(_primaryAction.ReadValue<float>() > 0.5f);
                item.SetSecondaryUseState(_secondaryAction.ReadValue<float>() > 0.5f);
                item.SetTertiaryUseState(_tertiaryAction.ReadValue<float>() > 0.5f);
            }
        }

        #endregion

        #region FixedUpdate

        private void FixedUpdate() {
            if (item != null) {
                if (itemColliders != null && itemColliders.Length > 0 && item.physicsCulling) {
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
                            lastHandPosition = transform.position;
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
            if (item != null) {
                Transform itemTransform = item.transform;
                lastItemPosition = itemTransform.position;
                lastItemRotation = itemTransform.rotation;
                if (useCollisions) {
                    Vector3 currentPosition = transform.position;
                    Vector3 deltaPosition = currentPosition - lastHandPosition;
                    itemTransform.position = lastItemPosition + deltaPosition;
                    lastHandPosition = currentPosition;
                } else {
                    itemTransform.SetPositionAndRotation(
                        transform.position + (lastItemRotation * itemPositionalOffset),
                        transform.rotation * itemRotationalOffset
                    );
                }
            }
        }

        #endregion

        #region OnPostRender

        private void OnPostRender() {
            if (item != null) {
                item.transform.SetPositionAndRotation(lastItemPosition, lastItemRotation); // reset transform
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
            lastHandPosition = transform.position;
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