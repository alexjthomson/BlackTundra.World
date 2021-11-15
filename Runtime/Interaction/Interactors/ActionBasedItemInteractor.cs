using BlackTundra.World.Items;

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
    public sealed class ActionBasedItemInteractor : MonoBehaviour {

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

        #endregion

        #region logic

        #region OnEnable

        private void OnEnable() {
            if (itemTarget == null) itemTarget = transform;
        }

        #endregion

        #region Update

        private void Update() {
            if (item != null) { // there is currently an item being held
                InputAction action = throwAction.action;
                if (action != null) {
                    float inputThrow = action.ReadValue<float>();
                    if (inputThrow > 0.5f) { // throw above threshold value
                        try {
                            item.ItemDropped();
                        } finally {
                            item = null;
                            itemRigidbody.isKinematic = false;
                            itemRigidbody.AddForce(transform.rotation * new Vector3(0.0f, 0.0f, itemThrowForce), ForceMode.Impulse);
                            itemRigidbody = null;
                        }
                        return;
                    }
                }
                action = primaryUseAction.action;
                item.SetPrimaryUseState(action != null && action.ReadValue<float>() > 0.5f);
                action = secondaryUseAction.action;
                item.SetSecondaryUseState(action != null && action.ReadValue<float>() > 0.5f);
                action = tertiaryUseAction.action;
                item.SetTertiaryUseState(action != null && action.ReadValue<float>() > 0.5f);
            } else { // there is not currently an item being held
                if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, range, layerMask, QueryTriggerInteraction.Ignore)) { // cast pickup ray
                    WorldItem currentWorldItem = hit.collider.GetComponent<WorldItem>();
                    if (currentWorldItem != null) { // a world item was hit
                        InputAction action = pickupAction.action;
                        if (action != null) {
                            float inputPickup = action.ReadValue<float>();
                            if (inputPickup > 0.5f) { // pickup input activated
                                item = currentWorldItem;
                                itemPositionOffset = item.holdPositionOffset;
                                itemRotationOffset = Quaternion.Euler(item.holdRotationOffset);
                                itemRigidbody = item.rigidbody;
                                itemRigidbody.isKinematic = true;
                                itemRigidbody.velocity = Vector3.zero;
                                itemRigidbody.angularVelocity = Vector3.zero;
                                itemRigidbody.position = itemTarget.position;
                                itemRigidbody.rotation = CalculateItemRotation();
                                item.ItemPickedUp(); // trigger item picked up events
                            }
                        }
                    }
                }
            }
        }

        #endregion

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

        #endregion

    }

}