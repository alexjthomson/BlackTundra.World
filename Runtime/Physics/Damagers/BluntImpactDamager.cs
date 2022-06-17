using UnityEngine;

using Object = UnityEngine.Object;

namespace BlackTundra.World.Damagers {

    /// <summary>
    /// Deals blunt damage when this object impacts another.
    /// </summary>
#if UNITY_EDITOR
    [AddComponentMenu(menuName: "Physics/Damager/Blunt Impact Damager", order: 100)]
#endif
    [DisallowMultipleComponent]
    public sealed class BluntImpactDamager : BaseDamager {

        #region variable

        /// <summary>
        /// Minimum amount of raw blunt impact damage that can be delt by this <see cref="BluntImpactDamager"/>.
        /// </summary>
#if UNITY_EDITOR
        [Tooltip("Minimum amount of raw blunt impact damage that can be delt.")]
        [Min(0.0f)]
#endif
        [SerializeField]
        private float minDamage = 0.05f;

        /// <summary>
        /// Maximum amount of raw blunt impact damage that can be delt by this <see cref="BluntImpactDamager"/>.
        /// </summary>
#if UNITY_EDITOR
        [Tooltip("Maximum amount of raw blunt impact damage that can be delt.")]
        [Min(0.0f)]
#endif
        [SerializeField]
        private float maxDamage = 1.0f;

        /// <summary>
        /// Minimum impact force required to cause damage.
        /// </summary>
#if UNITY_EDITOR
        [Tooltip("Minimum impact force required to cause damage.")]
        [Min(0.0f)]
#endif
        [SerializeField]
        private float minImpactForce = 1.0f;

        /// <summary>
        /// Maximum impact force before the amount of damage delt stops increasing.
        /// </summary>
#if UNITY_EDITOR
        [Tooltip("Maximum impact force before the amount of damage delt stops increasing.")]
        [Min(0.0f)]
#endif
        [SerializeField]
        private float maxImpactForce = 10.0f;

        #endregion

        #region logic

        #region OnCollisionEnter

        private void OnCollisionEnter(Collision collision) {
            // validate collision:
            if (collision == null) return;
            // validate damage range:
            float damageRange = maxDamage - minDamage;
            if (damageRange < 0.0f) return;
            // validate impact force range:
            float impactForceRange = maxImpactForce - minImpactForce;
            if (impactForceRange < 0.0f) return;
            // calculate average contact point and magnitude of the total impulse forces to resolve the collision:
            ContactPoint[] contactPoints = collision.contacts;
            ContactPoint contactPoint;
            for (int i = contactPoints.Length - 1; i >= 0; i--) {
                contactPoint = contactPoints[i];
                // process contact point:
                Collider impactCollider = contactPoint.otherCollider;
                if (impactCollider == null) continue;
                IDamageable damageable = impactCollider.GetComponentInParent<IDamageable>();
                if (damageable == null) continue;
                // calculate impact force:
                float sqrImpulseForceAbsorbed = contactPoint.impulse.sqrMagnitude;
                if (sqrImpulseForceAbsorbed < minImpactForce * minImpactForce) continue; // less than the minimum impact force
                float impulseForceAbsorbAmount = sqrImpulseForceAbsorbed < maxImpactForce * maxImpactForce
                    ? (Mathf.Sqrt(sqrImpulseForceAbsorbed) - minImpactForce) / impactForceRange
                    : 1.0f;
                // calculate impact damage:
                float impactDamage = impulseForceAbsorbAmount * damageRange;
                // calculate impact point and velocity:
                Vector3 impactPoint = contactPoint.point;
                Vector3 impactVelocity = DamageController.rigidbody.GetPointVelocity(impactPoint);
                // invoke on impact:
                OnImpact(damageable, DamageController.sender, impactDamage, impactPoint, impactVelocity);
            }
        }

        #endregion

        #region OnImpact

        private void OnImpact(in IDamageable damageable, in Object sender, in float impactDamage, in Vector3 point, in Vector3 velocity) {
            damageable.OnDamage(sender, impactDamage, DamageType.BluntImpact, point, velocity, null);
        }

        #endregion

        #endregion

    }

}