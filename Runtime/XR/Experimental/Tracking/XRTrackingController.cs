#if USE_XR_TOOLKIT

using UnityEngine;

namespace BlackTundra.World.XR.Experimental.Tracking {

    /// <summary>
    /// Manages a set of <see cref="IXRTracker"/> components.
    /// </summary>
#if UNITY_EDITOR
    [AddComponentMenu(menuName: "XR/Tracking/XR Tracking Controller", order: 0)]
#endif
    public sealed class XRTrackingController : MonoBehaviour {

        #region variable

        [SerializeField]
#if UNITY_EDITOR
        internal
#else
        private
#endif
        XRTrackerDriver[] trackerDrivers = new XRTrackerDriver[0];

        #endregion

        #region logic

        #region Awake

        private void Awake() {
            XRTrackerDriver trackerDriver;
            for (int i = trackerDrivers.Length - 1; i >= 0; i--) {
                trackerDriver = trackerDrivers[i];
                trackerDriver.Initialise();
            }
        }

        #endregion

        #region Update

        private void Update() {
            XRTrackerDriver trackerDriver;
            for (int i = trackerDrivers.Length - 1; i >= 0; i--) {
                trackerDriver = trackerDrivers[i];
                trackerDriver.UpdateTracker();
            }
        }

        #endregion

        #endregion

    }

}

#endif