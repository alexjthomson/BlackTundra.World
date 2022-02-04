#if USE_XR_TOOLKIT

using System;

namespace BlackTundra.World.XR.Locomotion {

    public abstract class XRMoveController {

        #region variable

        /// <summary>
        /// <see cref="XRLocomotionController"/> that the <see cref="XRMoveController"/> should control.
        /// </summary>
        protected readonly XRLocomotionController locomotion;

        #endregion

        #region constructor

        protected XRMoveController(in XRLocomotionController locomotion) {
            if (locomotion == null) throw new ArgumentNullException(nameof(locomotion));
            this.locomotion = locomotion;
        }

        #endregion

        #region logic

        internal protected abstract void Update(in float deltaTime);

        internal protected abstract void FixedUpdate(in float deltaTime);

        #endregion

    }

}

#endif