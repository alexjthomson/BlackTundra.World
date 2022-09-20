#if USE_XR_TOOLKIT

using System;

using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackTundra.World.XR.Experimental.Tracking {

    /// <summary>
    /// Drives an <see cref="IXRTracker"/> based off of tracker inputs.
    /// </summary>
    [Serializable]
    public sealed class XRTrackerDriver {

        #region variable

        /// <summary>
        /// <see cref="IXRTracker"/> being driven.
        /// </summary>
        [SerializeField]
        private GameObject trackerGameObject = null;
        private IXRTracker tracker = null;

        /// <summary>
        /// Positional input property.
        /// </summary>
        [SerializeField]
        private InputActionProperty positionAction;
        internal InputAction _positionAction = null;

        /// <summary>
        /// Rotational input property.
        /// </summary>
        [SerializeField]
        private InputActionProperty rotationAction;
        internal InputAction _rotationAction = null;

        /// <summary>
        /// <see cref="Transform"/> component that the <see cref="positionAction"/> and <see cref="rotationAction"/> are relative to.
        /// </summary>
        [SerializeField]
        private Transform relativeTo = null;

        /// <summary>
        /// Fallback <see cref="Transform"/> component to use for position and rotation if no inputs are defined.
        /// </summary>
        [SerializeField]
        private Transform fallbackTransform = null;

        #endregion

        #region property

        public GameObject TrackerGameObject {
            get => trackerGameObject;
            set {
                trackerGameObject = value;
                tracker = trackerGameObject.GetComponent<IXRTracker>();
            }
        }

        public Transform FallbackTransform {
            get => fallbackTransform;
            set => fallbackTransform = value;
        }

        #endregion

        #region logic

        #region Initialise

        internal void Initialise() {
            tracker = trackerGameObject.GetComponent<IXRTracker>();
            _positionAction = positionAction.action;
            _rotationAction = rotationAction.action;
        }

        #endregion

        #region UpdateTracker

        internal void UpdateTracker() {
            if (tracker == null) return;
            // position:
            if (_positionAction != null && _positionAction.enabled) {
                if (relativeTo != null) {
                    tracker.TrackerPosition = relativeTo.TransformPoint(_positionAction.ReadValue<Vector3>());
                } else {
                    tracker.TrackerPosition = _positionAction.ReadValue<Vector3>();
                }
            } else if (fallbackTransform != null) {
                tracker.TrackerPosition = fallbackTransform.position;
            }
            // rotation:
            if (_rotationAction != null && _rotationAction.enabled) {
                if (relativeTo != null) {
                    tracker.TrackerRotation = relativeTo.rotation * _rotationAction.ReadValue<Quaternion>();
                } else {
                    tracker.TrackerRotation = _rotationAction.ReadValue<Quaternion>();
                }
            } else if (fallbackTransform != null) {
                tracker.TrackerRotation = fallbackTransform.rotation;
            }
        }

        #endregion

        #endregion

    }

}

#endif