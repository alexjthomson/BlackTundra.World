#if ENABLE_VR

using UnityEngine;
using UnityEngine.XR;

namespace BlackTundra.World.XR {

    public sealed class XRPlayerHand : MonoBehaviour {

        #region variable

        internal InputDevice device;

        #endregion

        #region logic

        #region OnEnable

        private void OnEnable() {

        }

        #endregion

        #region Update

        private void Update() {
            if (device.isValid) {
                if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primary) && primary) { // primary button pressed

                }
                if (device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondary) && secondary) { // secondary button pressed

                }
            }
        }

        #endregion

        #endregion

    }

}

#endif