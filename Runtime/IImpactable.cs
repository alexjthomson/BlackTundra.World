using UnityEngine;

namespace BlackTundra.World {

    /// <summary>
    /// Defines an object that can be impacted.
    /// </summary>
    public interface IImpactable {

        /// <summary>
        /// Invoked when the object is impacted.
        /// </summary>
        /// <param name="impacterVelocity">Velocity of the object that impacted the impactable object.</param>
        /// <param name="impactPoint">Point of impact.</param>
        /// <param name="energyTransferred">Amount of energy transferred to the <see cref="IImpactable"/> from the impacter.</param>
        public void OnImpact(in Vector3 impacterVelocity, in Vector3 impactPoint, in float energyTransferred);

    }

}