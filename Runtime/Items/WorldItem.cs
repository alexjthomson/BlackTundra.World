using BlackTundra.Foundation.Utility;
#if USE_XR_TOOLKIT
using BlackTundra.World.XR;
#endif

using UnityEngine;
using UnityEngine.Events;
#if USE_XR_TOOLKIT
using UnityEngine.XR.Interaction.Toolkit;
#endif

namespace BlackTundra.World.Items {

    /// <summary>
    /// Controls and manages an instance of an <see cref="Item"/> that exists in the world.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
#if USE_XR_TOOLKIT
    [RequireComponent(typeof(XRGrabInteractable))]
#endif
    public sealed class WorldItem : MonoBehaviour {

        #region constant

        /// <summary>
        /// Minimum square impact speed in order for an impact to be registered.
        /// </summary>
        public float ThresholdSqrImpactSpeed = 0.1f * 0.1f;

        /// <summary>
        /// Number of <see cref="FixedUpdate"/> calls to skip.
        /// </summary>
        private const int UpdateSkipCount = 100;

        /// <summary>
        /// Minimum square distance that a <see cref="WorldItem"/> must move for the <see cref="rigidbody"/> to remain enabled.
        /// </summary>
        private const float NotMovedDistanceSqrMagnitude = 0.05f * 0.05f;

        /// <summary>
        /// Minimum square velocity that a <see cref="WorldItem"/> must move for the <see cref="rigidbody"/> to remain enabled.
        /// </summary>
        private const float NotMovedVelocitySqrMagnitude = 0.01f * 0.01f;

        /// <summary>
        /// Minimum square angular velocity that a <see cref="WorldItem"/> must move for the <see cref="rigidbody"/> to remain enabled.
        /// </summary>
        private const float NotMovedAngularVelocitySqrMangitude = (Mathf.PI * 0.05f) * (Mathf.PI * 0.05f);

        /// <summary>
        /// Minimum Y level before an item is considered to have "fallen" out of the map.
        /// </summary>
        private const float MinYLevel = -100;

        #endregion

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

        #region snap point

#if USE_XR_TOOLKIT
        [SerializeField]
        private UnityEvent<XRItemSnapPoint, SelectEnterEventArgs> onXRItemSnapPointEnter = null;
#endif

#if USE_XR_TOOLKIT
        [SerializeField]
        private UnityEvent<XRItemSnapPoint, SelectExitEventArgs> onXRItemSnapPointExit = null;
#endif

        #endregion

        /// <summary>
        /// <see cref="AudioSource"/> used to play impact sounds when the <see cref="WorldItem"/> impacts a surface.
        /// </summary>
        [SerializeField]
        private AudioSource impactSource = null;

        /// <summary>
        /// Maximum volume that the <see cref="impactSource"/> can have.
        /// </summary>
#if UNITY_EDITOR
        [Range(0.0f, 1.0f)]
#endif
        [SerializeField]
        private float impactVolume = 1.0f;

        /// <summary>
        /// Invoked when the <see cref="WorldItem"/> impacts a surface.
        /// </summary>
        /// <remarks>
        /// Collision = collision, Vector3 = impact velocity, float = sqr impact speed.
        /// </remarks>
        [SerializeField]
        private UnityEvent<Collision, Vector3, float> onImpact = null;

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

#if USE_XR_TOOLKIT
        /// <summary>
        /// When <c>true</c>, the XR hands will be hidden while the item is grabbed.
        /// </summary>
        [SerializeField]
        internal bool hideXRHands = false;
#endif

        /// <summary>
        /// <see cref="IItemHolder"/> that is currently holding the <see cref="WorldItem"/>.
        /// </summary>
        private IItemHolder holder = null;

        /// <summary>
        /// <see cref="Item"/> associated with the <see cref="WorldItem"/>.
        /// </summary>
        private Item item = null;

#if USE_XR_TOOLKIT
        /// <summary>
        /// <see cref="XRGrabInteractable"/> used to allow XR to interact with the <see cref="WorldItem"/>.
        /// </summary>
        private XRGrabInteractable xrGrabInteractable = null;
#endif

        /// <summary>
        /// Number of updates to skip before checking if the <see cref="rigidbody"/> should be disabled due to not moving.
        /// </summary>
        private int updateSkipCounter = UpdateSkipCount;

        /// <summary>
        /// Last position the last time the <see cref="rigidbody"/> position was checked.
        /// </summary>
        private Vector3 lastPosition = Vector3.zero;

        /// <summary>
        /// Last position that the <see cref="WorldItem"/> was stable. If the item finds itself to be stuck inside of a collider or out of bounds
        /// it will teleport the item to this position.
        /// </summary>
        private Vector3 lastStablePosition = Vector3.zero;

        /// <summary>
        /// Similar to the <see cref="lastStablePosition"/> but this records the rotation of the <see cref="WorldItem"/> when it was in the
        /// <see cref="lastStablePosition"/>.
        /// </summary>
        private Quaternion lastStableRotation = Quaternion.identity;

        /// <summary>
        /// Tracks if the <see cref="WorldItem"/> has made contact with anything yet. This is used to cull the impact sound made when the item
        /// initially impacts the floor when the item first enters the scene.
        /// </summary>
        private bool initialContact = false;

        /// <summary>
        /// Cached <see cref="Collider"/> array.
        /// </summary>
        private Collider[] colliders = null;

        /// <summary>
        /// Cached states of each collider.
        /// </summary>
        private bool[] colliderStates = null;

        /// <summary>
        /// Cached layers of assigned to each of the <see cref="colliders"/>.
        /// </summary>
        private int[] colliderLayers = null;

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
        /// <c>true</c> when the <see cref="WorldItem"/> physics are active.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public bool isPhysicsActive { get; private set; } = true;
#pragma warning restore IDE1006 // naming styles

        /// <summary>
        /// Last time that the <see cref="WorldItem"/> was picked up (grabbed).
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public float lastPickupTime { get; private set; } = 0.0f;
#pragma warning restore IDE1006 // naming styles

        /// <summary>
        /// Last time that the <see cref="WorldItem"/> was released (dropped).
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public float lastReleaseTime { get; private set; } = 0.0f;
#pragma warning restore IDE1006 // naming styles

        /// <summary>
        /// <see cref="IItemHolder"/> instance currently holding this <see cref="WorldItem"/>.
        /// </summary>
        public IItemHolder ItemHolder => holder;

        public Item Item => item;

        public Vector3 LocalHoldPosition => holdPositionOffset;

        public Vector3 LocalHoldRotation => holdRotationOffset;

        #endregion

        #region constructor

        #endregion

        #region logic

        #region Awake

        private void Awake() {
            rigidbody = GetComponent<Rigidbody>();
#if USE_XR_TOOLKIT
            xrGrabInteractable = GetComponent<XRGrabInteractable>();
#endif
            if (item == null) {
                ItemData itemData = ItemData.GetItem(itemDescriptor.name);
                if (itemData != null) item = new Item(itemData.id);
            }
            lastPosition = rigidbody.position;
            lastStablePosition = lastPosition;
            lastStableRotation = rigidbody.rotation;
        }

        #endregion

        #region FixedUpdate

        private void FixedUpdate() {
            if (holder == null && --updateSkipCounter <= 0) { // physics disable check
                updateSkipCounter = UpdateSkipCount;
                Vector3 position = rigidbody.position;
                if (position.y < MinYLevel) {
                    ReturnToLastStablePosition();
                } else {
                    Vector3 deltaPosition = position - lastPosition;
                    float deltaPositionSqrMagnitude = deltaPosition.sqrMagnitude;
                    if (deltaPositionSqrMagnitude < NotMovedDistanceSqrMagnitude
                        && rigidbody.velocity.sqrMagnitude < NotMovedVelocitySqrMagnitude
                        && rigidbody.angularVelocity.sqrMagnitude < NotMovedAngularVelocitySqrMangitude
                    ) {
                        DisablePhysics();
                    } else {
                        lastPosition = position;
                    }
                }
            }
        }

        #endregion

        #region OnCollisionEnter

        private void OnCollisionEnter(Collision collision) {
            EnablePhysics();
            Vector3 velocity = collision.relativeVelocity;
            float sqrSpeed = velocity.sqrMagnitude;
            if (sqrSpeed < ThresholdSqrImpactSpeed) return;
            if (initialContact) {
                if (impactSource != null) {
                    float normalizedImpactIntensity = (1.0f - (1.0f / ((sqrSpeed * 0.1f) + 1)));
                    if (impactSource.isPlaying) {
                        impactSource.PlayOneShot(impactSource.clip, normalizedImpactIntensity);
                    } else {
                        impactSource.pitch = 0.95f + (0.1f * normalizedImpactIntensity) + Random.Range(-0.01f, 0.01f);
                        impactSource.volume = normalizedImpactIntensity * impactVolume;
                        impactSource.Play();
                    }
                }
            } else {
                initialContact = true;
            }
            if (onImpact != null) onImpact.Invoke(collision, velocity, sqrSpeed);
        }

        #endregion

        #region ReturnToLastStablePosition

        /// <summary>
        /// Returns the <see cref="WorldItem"/> to the last stable position (and rotation).
        /// </summary>
        private void ReturnToLastStablePosition() {
            rigidbody.isKinematic = true;
            rigidbody.position = lastStablePosition;
            rigidbody.rotation = lastStableRotation;
            DisablePhysics();
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

        public void PickupItem(in IItemHolder holder, in bool disableCollision) {
            enabled = false;
            if (this.holder != null) {
                if (!this.holder.CanTakeItem(this, holder)) return; // item cannot be taken
                ReleaseItem(this.holder);
            }
            if (holder != null) {
                this.holder = holder;
                this.holder.OnHoldItem(this);
            }
            if (disableCollision) {
                DisableCollision();
            } else {
                EnableCollision();
            }
            lastPickupTime = Time.time;
            onItemPickup?.Invoke();
        }

        #endregion

        #region XRPickupItem
#if USE_XR_TOOLKIT
        public void XRPickupItem() {
            IXRSelectInteractor interactor = xrGrabInteractable.GetOldestInteractorSelecting();
            if (interactor is Behaviour interactorBehaviour) {
                IItemHolder itemHolder = interactorBehaviour.GetComponent<IItemHolder>();
                if (itemHolder != null) {
                    PickupItem(itemHolder, false);
                }
            }
        }
#endif
        #endregion

        #region ReleaseItem

        public void ReleaseItem(in IItemHolder holder) {
            updateSkipCounter = UpdateSkipCount;
            enabled = true;
            if (this.holder == holder) {
                try {
                    this.holder.OnReleaseItem(this);
                } finally {
                    this.holder = null;
                }
            }
            EnableCollision();
            lastReleaseTime = Time.time;
            onItemDrop.Invoke();
        }

        #endregion

        #region XRReleaseItem
#if USE_XR_TOOLKIT
        public void XRReleaseItem() {
            if (holder != null) {
                Behaviour holderBehaviour = holder as Behaviour;
                if (holderBehaviour != null) {
                    XRBaseController controller = holderBehaviour.GetComponent<XRBaseController>();
                    if (controller != null) {
                        ReleaseItem(holder);
                    }
                }
            }
        }
#endif
        #endregion

        #region EnablePhysics

        public void EnablePhysics() {
            if (!enabled && holder == null && colliders == null) {
                isPhysicsActive = true;
                updateSkipCounter = UpdateSkipCount;
                enabled = true;
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rigidbody.isKinematic = false;
            }
        }

        #endregion

        #region DisablePhysics

        public void DisablePhysics() {
            isPhysicsActive = false;
            rigidbody.isKinematic = true;
            rigidbody.interpolation = RigidbodyInterpolation.None;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            lastStablePosition = rigidbody.position;
            lastStableRotation = rigidbody.rotation;
            enabled = false;
        }

        #endregion

        #region EnableCollision

        public void EnableCollision() {
            if (colliders == null || colliderStates == null) return;
            Collider collider;
            for (int i = colliders.Length - 1; i >= 0; i--) {
                collider = colliders[i];
                collider.enabled = collider.enabled || colliderStates[i];
            }
            colliderStates = null;
        }

        #endregion

        #region DisableCollision

        /// <summary>
        /// Disables collisions for the item.
        /// </summary>
        public void DisableCollision() {
            DisablePhysics();
            if (colliders == null) colliders = gameObject.GetColliders(false);
            int colliderCount = colliders.Length;
            colliderStates = new bool[colliderCount];
            Collider collider;
            for (int i = colliderCount - 1; i >= 0; i--) {
                collider = colliders[i];
                if (collider.enabled) {
                    colliderStates[i] = true;
                    collider.enabled = false;
                } else {
                    colliderStates[i] = false;
                }
            }
        }

        #endregion

        #region SetLayers

        public void SetLayers(in int layer) {
            if (colliders == null) colliders = gameObject.GetColliders(false);
            int colliderCount = colliders.Length;
            colliderLayers = new int[colliderCount];
            GameObject colliderGameObject;
            for (int i = colliderCount - 1; i >= 0; i--) {
                colliderGameObject = colliders[i].gameObject;
                colliderLayers[i] = colliderGameObject.layer;
                colliderGameObject.layer = layer;
            }
        }

        #endregion

        #region ResetLayers

        public void ResetLayers() {
            if (colliders == null || colliderLayers == null) return;
            GameObject colliderGameObject;
            for (int i = colliders.Length - 1; i >= 0; i--) {
                colliderGameObject = colliders[i].gameObject;
                colliderGameObject.layer = colliderLayers[i];
            }
        }

        #endregion

        #region OnEnterXRItemSnapPoint
#if USE_XR_TOOLKIT
        internal void OnEnterSnapPoint(in XRItemSnapPoint snapPoint, in SelectEnterEventArgs args) {
            onXRItemSnapPointEnter?.Invoke(snapPoint, args);
        }
#endif
        #endregion

        #region OnExitXRItemSnapPoint
#if USE_XR_TOOLKIT
        internal void OnExitSnapPoint(in XRItemSnapPoint snapPoint, in SelectExitEventArgs args) {
            onXRItemSnapPointExit?.Invoke(snapPoint, args);
        }
#endif
        #endregion

        #endregion

    }

}