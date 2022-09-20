#if USE_XR_TOOLKIT

using BlackTundra.Foundation;

using UnityEngine;

namespace BlackTundra.World.XR.Experimental.Locomotion {

    /// <summary>
    /// <see cref="MonoBehaviour"/> base implementation of <see cref="IXRMovementProvider"/>.
    /// </summary>
    public abstract class XRBaseMovementProvider : MonoBehaviour, IXRMovementProvider {

        #region constant

        protected static readonly ConsoleFormatter ConsoleFormatter = new ConsoleFormatter(nameof(XRBaseMovementProvider));

        #endregion

        #region variable

        [SerializeField]
        protected XRLocomotionController locomotionController = null;

        #endregion

        #region logic

        #region OnEnable

        protected virtual void OnEnable() {
            // get locomotion controller:
            if (locomotionController == null) {
                ConsoleFormatter.Error($"{nameof(XRBaseMovementProvider)} requires a {nameof(XRLocomotionController)} component.");
                return;
            }
            // register as movement provider:
            locomotionController.RegisterMovementProvider(this);
        }

        #endregion

        #region OnDisable

        protected virtual void OnDisable() {
            if (locomotionController != null) {
                locomotionController.RegisterMovementProvider(null);
            }
        }

        #endregion

        #region Update

        /// <inheritdoc cref="IXRMovementProvider.Update(in float)"/>
        public abstract void Update(in float deltaTime);

        #endregion

        #region FixedUpdate

        /// <inheritdoc cref="IXRMovementProvider.FixedUpdate(in float)"/>
        public abstract void FixedUpdate(in float deltaTime);

        #endregion

        #endregion

    }

}

#endif