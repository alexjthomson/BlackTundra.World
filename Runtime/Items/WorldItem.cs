using BlackTundra.World.Interaction;

using UnityEngine;
using UnityEngine.Events;

namespace BlackTundra.World.Items {

    /// <summary>
    /// Controls and manages an instance of an <see cref="Item"/> that exists in the world.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class WorldItem : MonoBehaviour {

        #region variable

        /// <summary>
        /// <see cref="ItemDescriptor"/> that describes this <see cref="WorldItem"/>.
        /// </summary>
        [SerializeField]
        private ItemDescriptor itemDescriptor = null;

        #region primary use

        /// <summary>
        /// Tracks the primary use state.
        /// </summary>
        private bool primaryUse = false;

        [SerializeField]
        private UnityEvent onPrimaryUse = null;

        [SerializeField]
        private UnityEvent<bool> onPrimaryUseChanged = null;

        #endregion

        #region secondary use

        /// <summary>
        /// Tracks the secondary use state.
        /// </summary>
        private bool secondaryUse = false;

        [SerializeField]
        private UnityEvent onSecondaryUse = null;

        [SerializeField]
        private UnityEvent<bool> onSecondaryUseChanged = null;

        #endregion

        #region teriary use

        /// <summary>
        /// Tracks the teriary use state.
        /// </summary>
        private bool tertiaryUse = false;

        [SerializeField]
        private UnityEvent onTertiaryUse = null;

        [SerializeField]
        private UnityEvent<bool> onTertiaryUseChanged = null;

        #endregion

        #region item held / dropped

        [SerializeField]
        private UnityEvent onItemPickup = null;

        [SerializeField]
        private UnityEvent onItemDrop = null;

        #endregion

        /// <summary>
        /// Positional offset when held.
        /// </summary>
        [SerializeField]
        internal Vector3 holdPositionOffset = Vector3.zero;

        /// <summary>
        /// Rotational offset when the item is held. This is not used for XR.
        /// </summary>
        [SerializeField]
        internal Vector3 holdRotationOffset = Vector3.zero;

        /// <summary>
        /// <see cref="IItemHolder"/> that is currently holding the <see cref="WorldItem"/>.
        /// </summary>
        private IItemHolder holder = null;

        /// <summary>
        /// <see cref="Item"/> associated with the <see cref="WorldItem"/>.
        /// </summary>
        private Item item = null;

        #endregion

        #region property

        /// <summary>
        /// <see cref="Rigidbody"/> component attached to the <see cref="WorldItem"/> <see cref="GameObject"/>.
        /// </summary>
#if UNITY_EDITOR
        new
#endif
#pragma warning disable IDE1006 // naming styles
        public Rigidbody rigidbody { get; private set; } = null;
#pragma warning restore IDE1006 // naming styles

        /// <summary>
        /// <see cref="IItemHolder"/> instance currently holding this <see cref="WorldItem"/>.
        /// </summary>
        public IItemHolder ItemHolder => holder;

        public Item Item {
            get => item;
        }

        public Vector3 LocalHoldPosition => holdPositionOffset;

        public Vector3 LocalHoldRotation => holdRotationOffset;

        #endregion

        #region constructor

        #endregion

        #region logic

        #region Awake

        private void Awake() {
            rigidbody = GetComponent<Rigidbody>();
            if (item == null) {
                ItemData itemData = ItemData.GetItem(itemDescriptor.name);
                if (itemData != null) item = new Item(itemData.id);
            }
        }

        #endregion

        #region PrimaryUse

        /// <summary>
        /// Oneshot primary use.
        /// </summary>
        public void PrimaryUse() {
            if (primaryUse) return;
            onPrimaryUse?.Invoke();
        }

        #endregion

        #region SetPrimaryUseState

        /// <summary>
        /// Sets the state of the <see cref="primaryUse"/>.
        /// </summary>
        internal void SetPrimaryUseState(in bool state) {
            if (state != primaryUse) {
                primaryUse = state;
                onPrimaryUseChanged?.Invoke(primaryUse);
                if (primaryUse) onPrimaryUse?.Invoke();
            }
        }

        #endregion

        #region SecondaryUse

        /// <summary>
        /// Oneshot secondary use.
        /// </summary>
        public void SecondaryUse() {
            if (secondaryUse) return;
            onSecondaryUse?.Invoke();
        }

        #endregion

        #region SetSecondaryUseState

        /// <summary>
        /// Sets the state of the <see cref="secondaryUse"/>.
        /// </summary>
        internal void SetSecondaryUseState(in bool state) {
            if (state != secondaryUse) {
                secondaryUse = state;
                onSecondaryUseChanged?.Invoke(secondaryUse);
                if (secondaryUse) onSecondaryUse?.Invoke();
            }
        }

        #endregion

        #region TertiaryUse

        /// <summary>
        /// Oneshot tertiary use.
        /// </summary>
        public void TertiaryUse() {
            if (tertiaryUse) return;
            onTertiaryUse?.Invoke();
        }

        #endregion

        #region SetTertiaryUseState

        /// <summary>
        /// Sets the state of the <see cref="secondaryUse"/>.
        /// </summary>
        internal void SetTertiaryUseState(in bool state) {
            if (state != tertiaryUse) {
                tertiaryUse = state;
                onTertiaryUseChanged?.Invoke(tertiaryUse);
                if (tertiaryUse) onTertiaryUse?.Invoke();
            }
        }

        #endregion

        #region PickupItem

        public void PickupItem(in IItemHolder holder) {
            if (this.holder != null) {
                ReleaseItem(this.holder);
            }
            if (holder != null) {
                this.holder = holder;
                this.holder.OnHoldItem(this);
            }
            onItemPickup?.Invoke();
        }

        #endregion

        #region XRPickupItem
#if ENABLE_VR
        public void XRPickupItem() {
            throw new System.NotImplementedException();
        }
#endif
        #endregion

        #region ReleaseItem

        public void ReleaseItem(in IItemHolder holder) {
            if (this.holder == holder) {
                try {
                    this.holder.OnReleaseItem(this);
                } finally {
                    this.holder = null;
                }
            }
            onItemDrop.Invoke();
        }

        #endregion

        #region XRReleaseItem
#if ENABLE_VR
        public void XRReleaseItem() {
            throw new System.NotImplementedException();
        }
#endif
        #endregion

        #endregion

    }

}