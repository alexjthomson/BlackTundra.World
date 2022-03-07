#if USE_XR_TOOLKIT

using UnityEngine;

namespace BlackTundra.World.XR {

    [DisallowMultipleComponent]
#if UNITY_EDITOR
    [AddComponentMenu("XR/XR Physics Hand")]
#endif
    public sealed class XRPhysicsHand : MonoBehaviour {

        #region variable

        [SerializeField]
        internal Transform gripUpperRayOrigin = null;

        [SerializeField]
        internal Transform gripLowerRayOrigin = null;

        #endregion

    }

}

#endif