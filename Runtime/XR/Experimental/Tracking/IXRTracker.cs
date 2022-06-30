using UnityEngine;

namespace BlackTundra.World.XR.Experimental.Tracking {

    public interface IXRTracker {

        #region property

        /// <summary>
        /// World-space position of the tracker being emulated.
        /// </summary>
        public Vector3 TrackerPosition { get; set; }

        /// <summary>
        /// World-space rotation of the tracker being emulated.
        /// </summary>
        public Quaternion TrackerRotation { get; set; }

        #endregion

    }

}