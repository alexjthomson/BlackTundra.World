using System;

using UnityEngine;

using Object = UnityEngine.Object;

namespace BlackTundra.World.Damagers {

    /// <summary>
    /// Deals blunt damage when this object impacts another.
    /// </summary>
    public abstract class BaseDamager : MonoBehaviour {

        #region variable

        /// <summary>
        /// <see cref="RigidbodyDamageController"/> that manages the <see cref="BaseDamager"/>.
        /// </summary>
#if UNITY_EDITOR
        [Tooltip("Damage controller that manages this damager.")]
#endif
        [SerializeField]
        private RigidbodyDamageController damageController = null;

        #endregion

        #region property

        /// <inheritdoc cref="damageController"/>
        public RigidbodyDamageController DamageController => damageController;

        #endregion

        #region logic

        #region Awake

        protected virtual void Awake() {
            if (damageController == null) throw new NullReferenceException(nameof(damageController));
            damageController.damagers.Add(this);
        }

        #endregion

        #region OnDestroy

        protected virtual void OnDestroy() {
            if (damageController == null) throw new NullReferenceException(nameof(damageController));
            damageController.damagers.Remove(this);
        }

        #endregion

        #endregion

    }

}