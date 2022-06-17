using System.Collections.Generic;

using UnityEngine;

using Object = UnityEngine.Object;

namespace BlackTundra.World.Damagers {

    /// <summary>
    /// Controls damagers that are part of a <see cref="Rigidbody"/>.
    /// </summary>
#if UNITY_EDITOR
    [AddComponentMenu(menuName: "Physics/Damager/Rigidbody Damage Controller", order: 0)]
#endif
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class RigidbodyDamageController : MonoBehaviour {

        #region variable

        /// <summary>
        /// Sender override when applying damage.
        /// </summary>
#if UNITY_EDITOR
        [Tooltip("Sender override when applying damage.")]
#endif
        public Object sender = null;

        /// <summary>
        /// <see cref="Rigidbody"/> that contains <see cref="BaseDamager"/> components.
        /// </summary>
#if UNITY_EDITOR
        new
#endif
        internal Rigidbody rigidbody = null;

        /// <summary>
        /// <see cref="BaseDamager"/> components that are children of the <see cref="RigidbodyDamageController"/>.
        /// </summary>
        internal readonly List<BaseDamager> damagers = new List<BaseDamager>();

        /// <summary>
        /// Original <see cref="Transform"/> that the <see cref="RigidbodyDamageController"/> was parented to.
        /// </summary>
        private Transform originalParent = null;

        #endregion

        #region property

        /// <inheritdoc cref="originalParent"/>
        public Transform OriginalParent => originalParent;

        /// <summary>
        /// Current parent of the <see cref="RigidbodyDamageController"/>.
        /// </summary>
        public Transform CurrentParent => transform.parent;

        /// <summary>
        /// <c>true</c> if the <see cref="RigidbodyDamageController"/> is parented to the original parent.
        /// This may also return <c>true</c> if the original parent was destroyed and the <see cref="RigidbodyDamageController"/>
        /// also does not have a parent.
        /// </summary>
        public bool HasOriginalParent => transform.parent == originalParent;

        #endregion

        #region logic

        #region Awake

        private void Awake() {
            originalParent = transform.parent;
            rigidbody = GetComponent<Rigidbody>();
        }

        #endregion

        #region OnCollisionEnter

        private void OnCollisionEnter(Collision collision) {
            if (collision == null) return;
            BaseDamager damager;
            for (int i = damagers.Count - 1; i >= 0; i--) {
                damager = damagers[i];
                if (damager == null) continue;
                damager.OnDamagerCollisionEnter(collision);
            }
        }

        #endregion

        #region OnCollisionExit

        private void OnCollisionExit(Collision collision) {
            if (collision == null) return;
            BaseDamager damager;
            for (int i = damagers.Count - 1; i >= 0; i--) {
                damager = damagers[i];
                if (damager == null) continue;
                damager.OnDamagerCollisionExit(collision);
            }
        }

        #endregion

        #endregion

    }

}