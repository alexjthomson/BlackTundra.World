using UnityEngine;

namespace BlackTundra.World.Targetting {

    /// <summary>
    /// Defines an object that can be targetted.
    /// </summary>
    public interface ITargetable {

        #region property

        /// <summary>
        /// Current <see cref="velocity"/> of the <see cref="ITargetable"/>.
        /// </summary>
        public Vector3 velocity { get; }

        /// <summary>
        /// Current <see cref="position"/> of the <see cref="ITargetable"/>.
        /// </summary>
        public Vector3 position { get; }

        /// <summary>
        /// Returns <c>true</c> if the <see cref="ITargetable"/> is registered as an object that can be targetted.
        /// </summary>
        public sealed bool isRegistered => TargetManager.IsRegistered(this);

        /// <summary>
        /// Flags that define what kind of target this is. When attempting to target an <see cref="ITargetable"/>
        /// you can define a set of flags to target.
        /// </summary>
        public int TargetFlags { get; set; }

        #endregion

        #region logic

        /// <summary>
        /// Get the predicted position of the <see cref="ITargetable"/> <paramref name="time"/> seconds in the future.
        /// </summary>
        /// <param name="time">Number of seconds in the future to predict the position of the <see cref="ITargetable"/>.</param>
        public virtual Vector3 GetPredictedPosition(in float time) => position + (velocity * time);

        /// <summary>
        /// Registers the <see cref="ITargetable"/> as an object that can be targetted.
        /// </summary>
        /// <returns>
        /// Returns <c>true</c> if the <see cref="ITargetable"/> was successfully registered.
        /// This can fail if the <see cref="ITargetable"/> is already registered.
        /// </returns>
        /// <seealso cref="Deregister"/>
        public sealed bool Register() => TargetManager.Register(this);

        /// <summary>
        /// Deregisters the <see cref="ITargetable"/> as an object that can be targetted.
        /// </summary>
        /// <remarks>
        /// Once called, the <see cref="ITargetable"/> will no longer be targetable.
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if the <see cref="ITargetable"/> was successfully deregistered.
        /// This can fail if the <see cref="ITargetable"/> was never registered.
        /// </returns>
        /// <seealso cref="Register"/>
        public sealed bool Deregister() => TargetManager.Deregister(this);

        #endregion

    }

}