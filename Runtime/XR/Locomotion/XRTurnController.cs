#if USE_XR_TOOLKIT

using System;

namespace BlackTundra.World.XR.Locomotion {

    public abstract class XRTurnController {

        #region variable

        /// <summary>
        /// <see cref="XRLocomotionController"/> that the <see cref="XRTurnController"/> should control.
        /// </summary>
        protected readonly XRLocomotionController locomotion;

        #endregion

        #region constructor

        protected XRTurnController(in XRLocomotionController locomotion) {
            if (locomotion == null) throw new ArgumentNullException(nameof(locomotion));
            this.locomotion = locomotion;
        }

        #endregion

        #region logic

        internal protected abstract void Update(in float deltaTime);

        #endregion

    }

}

#endif