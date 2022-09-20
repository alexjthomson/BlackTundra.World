#if USE_XR_TOOLKIT

using UnityEngine;

namespace BlackTundra.World.XR.Experimental.Tracking {

    /// <summary>
    /// XR tracker that directly tracks an XR object.
    /// </summary>
#if UNITY_EDITOR
    [AddComponentMenu(menuName: "XR/Tracking/XR Tracker (Transform)", order: 100)]
#endif
    [RequireComponent(typeof(Transform))]
    [DisallowMultipleComponent]
    public sealed class XRTransformTracker : MonoBehaviour, IXRTracker {

        #region variable

        /// <inheritdoc cref="TrackerPosition"/>
        private Vector3 _trackerPosition = Vector3.zero;

        /// <inheritdoc cref="TrackerRotation"/>
        private Quaternion _trackerRotation = Quaternion.identity;

        #endregion

        #region property

        /// <summary>
        /// World-space tracker position.
        /// </summary>
        public Vector3 TrackerPosition {
            get => _trackerPosition;
            set => _trackerPosition = value;
        }

        /// <summary>
        /// World-space tracker rotation.
        /// </summary>
        public Quaternion TrackerRotation {
            get => _trackerRotation;
            set => _trackerRotation = value;
        }

        #endregion

        #region logic

        private void FixedUpdate() {
            transform.SetPositionAndRotation(
                _trackerPosition,
                _trackerRotation
            );
        }

        #endregion

    }

}

#endif