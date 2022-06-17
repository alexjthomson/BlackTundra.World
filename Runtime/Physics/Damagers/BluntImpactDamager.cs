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

        #region logic

        #region OnImpact

        protected override void OnImpact(in IDamageable damageable, in Object sender, in float impactDamage, in Vector3 point, in Vector3 velocity) {
            damageable.OnDamage(sender, impactDamage, DamageType.BluntImpact, point, velocity, null);
        }

        #endregion

        #endregion

    }

}