using UnityEngine;

namespace BlackTundra.World.XR.Experimental.Tracking {

    /// <summary>
    /// Manages a set of <see cref="IXRTracker"/> components.
    /// </summary>
#if UNITY_EDITOR
    [AddComponentMenu(menuName: "XR/Tracking/Tracking Controller", order: 0)]
#endif
    public sealed class TrackingController : MonoBehaviour {

        #region variable

        [SerializeField]
#if UNITY_EDITOR
        internal
#else
        private
#endif
        TrackerDriver[] trackerDrivers = new TrackerDriver[0];

        #endregion

        #region logic

        #region Awake

        private void Awake() {
            TrackerDriver trackerDriver;
            for (int i = trackerDrivers.Length - 1; i >= 0; i--) {
                trackerDriver = trackerDrivers[i];
                trackerDriver.Initialise();
            }
        }

        #endregion

        #region Update

        private void Update() {
            TrackerDriver trackerDriver;
            for (int i = trackerDrivers.Length - 1; i >= 0; i--) {
                trackerDriver = trackerDrivers[i];
                trackerDriver.UpdateTracker();
            }
        }

        #endregion

        #endregion

    }

}