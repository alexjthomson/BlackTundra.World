#if USE_XR_TOOLKIT

using UnityEngine;

namespace BlackTundra.World.XR.Experimental.Locomotion {

    [RequireComponent(typeof(XRLocomotionController))]
#if UNITY_EDITOR
    [AddComponentMenu("XR/Locomotion/XR Snap Turn Provider", 2001)]
#endif
    public sealed class XRSnapTurnProvider : MonoBehaviour, IXRTurnProvider {

        #region variable

        #endregion

        #region logic

        #region Update

        public void Update(in float deltaTime) {

        }

        #endregion

        #endregion

    }

}

#endif