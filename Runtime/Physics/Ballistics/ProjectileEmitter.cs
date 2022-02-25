using BlackTundra.World.Pooling;

using System;

using UnityEngine;

namespace BlackTundra.World.Ballistics {

    /// <summary>
    /// Manages projectile emission.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProjectileEmitter : MonoBehaviour {

        #region variable

        [SerializeField]
        private ProjectileProperties _properties = null;

        [SerializeField]
        private ProjectileSimulationFlags _flags
            = ProjectileSimulationFlags.Gravity
            | ProjectileSimulationFlags.EnvironmentalDrag
            | ProjectileSimulationFlags.EnvironmentalForce
            | ProjectileSimulationFlags.Penetrate
            | ProjectileSimulationFlags.Ricochet
            | ProjectileSimulationFlags.TransferMomentum;

        [SerializeField]
#if UNITY_EDITOR
        internal
#else
        private
#endif
        int _poolSize = 100;

        private ObjectPool projectilePool = null;

        #endregion

        #region property

        public ProjectileProperties properties {
            get => _properties;
            set {
                if (value == null) throw new ArgumentNullException(nameof(properties));
                _properties = value;
            }
        }

        public ProjectileSimulationFlags flags {
            get => _flags;
            set {
                if (_flags == value) return;
                _flags = value;
                RefreshFlags();
            }
        }

        #endregion

        #region logic

        #region RefreshFlags

        private void RefreshFlags() {
            if (projectilePool != null) {
                int objectCount = projectilePool.Length;
                if (objectCount > 0) {
                    // change flags on existing projectiles here
                }
            }
        }

        #endregion

        #endregion

    }

}