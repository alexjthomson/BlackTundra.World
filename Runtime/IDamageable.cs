using UnityEngine;

namespace BlackTundra.World {

    /// <summary>
    /// Interface used to indicate that an object is capable of receiving damage.
    /// </summary>
    public interface IDamageable {

        /// <summary>
        /// Damages an object.
        /// </summary>
        /// <param name="sender">Sender of the damage.</param>
        /// <param name="damage">Damage to deal to the target object.</param>
        /// <param name="damageType">Type of damage being applied.</param>
        /// <param name="point">World-space point that the damage was delt to.</param>
        /// <param name="direction">World-space direction that the damage was delt in. This direction does not need to be normalized.</param>
        /// <param name="data">Data associated with the damage.</param>
        /// <returns>Total amount of damage that was actually delt.</returns>
        float OnDamage(in object sender, float damage, DamageType damageType, in Vector3 point, in Vector3 direction, in object data = null);

    }

}