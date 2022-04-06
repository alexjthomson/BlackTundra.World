namespace BlackTundra.World {

    public interface IDamageable {

        /// <summary>
        /// Damages an object.
        /// </summary>
        /// <param name="sender">Sender of the damage.</param>
        /// <param name="damage">Damage to deal to the target object.</param>
        /// <param name="data">Data associated with the damage.</param>
        /// <returns>Total amount of damage that was actually delt.</returns>
        float OnDamage(in object sender, float damage, in object data = null);

    }

}