using BlackTundra.World.Items;

using System;

using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackTundra.World.Interaction.Interactors {

    /// <summary>
    /// Manages a raycast based interaction system.
    /// </summary>
#if UNITY_EDITOR
    [AddComponentMenu("Interaction/Item Interactor")]
#endif
    [DisallowMultipleComponent]
    public sealed class ActionBasedItemInteractor : MonoBehaviour, IItemHolder {

        #region variable

        /// <summary>
        /// <see cref="LayerMask"/> to use for physics calculations.
        /// </summary>
        [SerializeField]
        private LayerMask layerMask = -1;

        /// <summary>
        /// Maximum interaction range.
        /// </summary>
        [Min(0.0f)]
        [SerializeField]
        private float range = 4.0f;

        /// <summary>
        /// Target <see cref="Transform"/> that the <see cref="ActionBasedItemInteractor"/> should use to find the target orientation for the <see cref="item"/>.
        /// If <c>null</c>, the parent will default to the <see cref="Transform"/> attached to the <see cref="ActionBasedItemInteractor"/>.
        /// </summary>
        [SerializeField]
        private Transform itemTarget = null;

        /// <summary>
        /// Amount of force to apply to an item when it is thrown.
        /// </summary>
        [SerializeField]
        private float itemThrowForce = 10.0f;

        /// <summary>
        /// Input action used to pick up an item.
        /// </summary>
        [SerializeField]
        private InputActionProperty pickupAction;

        /// <summary>
        /// Input action used to throw an item.
        /// </summary>
        [SerializeField]
        private InputActionProperty throwAction;

        /// <summary>
        /// Primary use action used when an <see cref="item"/> is being held to invoke it's primary use.
        /// </summary>
        [SerializeField]
        private InputActionProperty primaryUseAction;

        /// <summary>
        /// Secondary use action used when an <see cref="item"/> is being held to invoke it's secondary use.
        /// </summary>
        [SerializeField]
        private InputActionProperty secondaryUseAction;

        /// <summary>
        /// Tertiary use action used when an <see cref="item"/> is being held to invoke it's tertiary use.
        /// </summary>
        [SerializeField]
        private InputActionProperty tertiaryUseAction;

        /// <summary>
        /// Current <see cref="WorldItem"/> that the <see cref="ActionBasedItemInteractor"/> is currently holding.
        /// </summary>
        private WorldItem item = null;

        /// <summary>
        /// <see cref="Rigidbody"/> component associated with the <see cref="item"/>.
        /// </summary>
        private Rigidbody itemRigidbody = null;

        /// <summary>
        /// Positional offset for holding the <see cref="item"/>.
        /// </summary>
        private Vector3 itemPositionOffset = Vector3.zero;

        /// <summary>
        /// Rotational offset for holding the <see cref="item"/>.
        /// </summary>
        private Quaternion itemRotationOffset = Quaternion.identity;

        private InputAction _pickupAction = null;
        private InputAction _throwAction = null;
        private InputAction _primaryUseAction = null;
        private InputAction _secondaryUseAction = null;
        private InputAction _tertiaryUseAction = null;

        /// <summary>
        /// Tracks the last <see cref="WorldItem"/> that was hit with an interaction ray.
        /// </summary>
        private WorldItem lastHit = null;

        #endregion

        #region logic

        #region OnEnable

        private void OnEnable() {
            if (itemTarget == null) itemTarget = transform;
            _pickupAction = pickupAction.action;
            _throwAction = throwAction.action;
            _primaryUseAction = primaryUseAction.action;
            _secondaryUseAction = secondaryUseAction.action;
            _tertiaryUseAction = tertiaryUseAction.action;
        }

        #endregion

        #region Update

        private void Update() {
            if (item != null) { // there is currently an item being held
                if (lastHit != null) {
                    if (lastHit != item) ResetLastHit();
                    lastHit = null;
                }
                if (InputThrow()) { // throw above threshold value
                    item.ReleaseItem(this);
                    return;
                } else { // item not thrown
                    item.SetPrimaryUseState(InputPrimaryUse());
                    item.SetSecondaryUseState(InputSecondaryUse());
                    item.SetTertiaryUseState(InputTertiaryUse());
                }
            } else { // there is not currently an item being held
                if (CastInteractionRay(out WorldItem item)) {
                    if (item != lastHit) {
                        if (lastHit != null) ResetLastHit();
                        lastHit = item;
                    }
                    if (InputPickUp()) item.PickupItem(this, true);
                    else {
                        item.SetPrimaryUseState(InputPrimaryUse());
                        item.SetSecondaryUseState(InputSecondaryUse());
                        item.SetTertiaryUseState(InputTertiaryUse());
                    }
                }
            }
        }

        #endregion

        #region ResetLastHit

        private void ResetLastHit() {
            lastHit.SetPrimaryUseState(false);
            lastHit.SetSecondaryUseState(false);
            lastHit.SetTertiaryUseState(false);
        }

        #endregion

        #region CastInteractionRay

        /// <summary>
        /// Casts an interaction ray.
        /// </summary>
        private bool CastInteractionRay(out WorldItem item) {
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, range, layerMask, QueryTriggerInteraction.Ignore)) { // cast pickup ray
                item = hit.collider.GetComponentInParent<WorldItem>();
                return item != null && item.ItemHolder == null;
            } else {
                item = null;
                lastHit = null;
                return false;
            }
        }

        #endregion

        private bool InputPickUp() => _pickupAction.ReadValue<float>() > 0.5f;
        private bool InputThrow() => _throwAction.ReadValue<float>() > 0.5f;
        private bool InputPrimaryUse() => _primaryUseAction.ReadValue<float>() > 0.5f;
        private bool InputSecondaryUse() => _secondaryUseAction.ReadValue<float>() > 0.5f;
        private bool InputTertiaryUse() => _tertiaryUseAction.ReadValue<float>() > 0.5f;

        #region FixedUpdate

        private void FixedUpdate() {
            if (item != null) {
                itemRigidbody.position = CalculateItemPosition();
                itemRigidbody.rotation = CalculateItemRotation();
            }
        }

        #endregion

        #region LateUpdate

        private void LateUpdate() {
            if (item != null) {
                Transform itemTransform = item.transform;
                itemTransform.position = CalculateItemPosition();
                itemTransform.rotation = CalculateItemRotation();
            }
        }

        #endregion

        #region CalculateItemPosition

        private Vector3 CalculateItemPosition() => itemTarget.position + (itemTarget.rotation * itemPositionOffset);

        #endregion

        #region CalculateItemRotation

        private Quaternion CalculateItemRotation() => itemTarget.rotation * itemRotationOffset;

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
            itemPositionOffset = item.holdPositionOffset;
            itemRotationOffset = Quaternion.Euler(item.holdRotationOffset);
            itemRigidbody = item.rigidbody;
            itemRigidbody.isKinematic = true;
            itemRigidbody.velocity = Vector3.zero;
            itemRigidbody.angularVelocity = Vector3.zero;
            itemRigidbody.position = itemTarget.position;
            itemRigidbody.rotation = CalculateItemRotation();
        }

        #endregion

        #region OnReleaseItem

        public void OnReleaseItem(in WorldItem item) {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (item != this.item) return;
            this.item = null;
            itemRigidbody.isKinematic = false;
            itemRigidbody.AddForce(transform.rotation * new Vector3(0.0f, 0.0f, itemThrowForce), ForceMode.Impulse);
            itemRigidbody = null;
        }

        #endregion

        #endregion

    }

}