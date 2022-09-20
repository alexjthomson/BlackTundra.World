#if USE_XR_TOOLKIT

using UnityEngine;

namespace BlackTundra.World.XR.Experimental.Locomotion {

    [RequireComponent(typeof(XRLocomotionController))]
#if UNITY_EDITOR
    [AddComponentMenu("XR/Locomotion/XR Teleport Movement Provider", 2000)]
#endif
    public sealed class XRTeleportMovementProvider : MonoBehaviour, IXRMovementProvider {

        #region variable

        #endregion

        #region logic

        #region Update

        public void Update(in float deltaTime) {

        }

        #endregion

        #region FixedUpdate

        public void FixedUpdate(in float deltaTime) {

        }

        #endregion

        #endregion

    }

}

#endif