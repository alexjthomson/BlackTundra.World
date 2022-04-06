using System;

using UnityEngine;

namespace BlackTundra.World.Ballistics {

    /// <summary>
    /// Describes a projectile.
    /// </summary>
#if UNITY_EDITOR
    [CreateAssetMenu(menuName = "Configuration/Physics/Projectile Properties", fileName = "ProjectileProperties", order = 100)]
#endif
    public sealed class ProjectileProperties : ScriptableObject {

        #region variable

        /// <summary>
        /// Mass of the <see cref="Projectile"/> in kilograms.
        /// </summary>
        [SerializeField]
#if UNITY_EDITOR
        [Min(0.0001f)]
#endif
        internal float mass = 0.004f;

        /// <summary>
        /// <c>1.0f / <see cref="mass"/></c>.
        /// </summary>
        [NonSerialized]
        internal float _inverseMass;

        /// <summary>
        /// Radius of the <see cref="Projectile"/> in meters.
        /// </summary>
        [SerializeField]
#if UNITY_EDITOR
        [Min(0.0001f)]
#endif
        internal float radius = 0.00278f;
        /// <summary>
        /// Drag coefficient used in drag calculations for the <see cref="Projectile"/>.
        /// </summary>
        [SerializeField]
#if UNITY_EDITOR
        [Min(0.0f)]
#endif
        internal float dragCoefficient = 0.000001f;

        /// <summary>
        /// Combined drag coefficient value, this is used to reduce the number of calculations required to simulate this projectile.
        /// </summary>
        [NonSerialized]
        internal float _dragCoefficient;

        /// <summary>
        /// Penetration power that the <see cref="Projectile"/> has.
        /// </summary>
#if UNITY_EDITOR
        [Range(0.0f, 1.0f)]
#endif
        [SerializeField]
        internal float penetrationPower = 0.1f;

        /// <summary>
        /// Coefficient used to convert energy transferred into damage delt to objects hit by the <see cref="Projectile"/>.
        /// </summary>
#if UNITY_EDITOR
        [Min(0.0f)]
#endif
        [SerializeField]
        internal float damageCoefficient = 1.0f;

        /// <summary>
        /// <see cref="LayerMask"/> that determinds what objects the <see cref="Projectile"/> can strike.
        /// </summary>
        [SerializeField]
        internal LayerMask layerMask = -1;

        /// <summary>
        /// Threshold angle that the <see cref="Projectile"/> will ricochet at.
        /// </summary>
        [SerializeField]
#if UNITY_EDITOR
        [Range(0.0f, 90.0f)]
#endif
        internal float ricochetThresholdAngle = 80.0f;

        /// <summary>
        /// Maximum energy before the projectile will not ricochet.
        /// </summary>
        [SerializeField]
#if UNITY_EDITOR
        [Min(0.0f)]
#endif
        internal float ricochetMaxEnergy = 1000.0f;

        /// <summary>
        /// Maximum amount of seconds that the <see cref="Projectile"/> can be active.
        /// </summary>
        [SerializeField]
#if UNITY_EDITOR
        [Min(0.01f)]
#endif
        internal float lifetime = 6.0f;

#if !UNITY_EDITOR
        /// <summary>
        /// Tracks if the <see cref="ProjectileProperties"/> have been initialised.
        /// </summary>
        [NonSerialized]
        private bool _initialised = false;
#endif

        #endregion

        #region logic

        /// <summary>
        /// Pre-calculates useful values.
        /// </summary>
        internal void Initialise() {
#if !UNITY_EDITOR
            if (_initialised) return;
            _initialised = true;
#endif
            _inverseMass = 1.0f / mass;
            _dragCoefficient = -0.5f * Mathf.PI * radius * radius * dragCoefficient * _inverseMass;
        }

        #endregion

    }

}