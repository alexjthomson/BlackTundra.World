#if USE_XR_TOOLKIT

using System;

namespace BlackTundra.World.XR.Locomotion {

    public abstract class XRTurnProvider {

        #region variable

        /// <summary>
        /// <see cref="XRLocomotionController"/> that the <see cref="XRTurnProvider"/> should control.
        /// </summary>
        protected readonly XRLocomotionController locomotion;

        #endregion

        #region constructor

        protected XRTurnProvider(in XRLocomotionController locomotion) {
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