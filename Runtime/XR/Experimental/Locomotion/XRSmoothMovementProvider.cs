#if USE_XR_TOOLKIT

using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackTundra.World.XR.Experimental.Locomotion {

    [RequireComponent(typeof(XRLocomotionController))]
#if UNITY_EDITOR
    [AddComponentMenu("XR/Locomotion/XR Smooth Movement Provider", 1000)]
#endif
    public sealed class XRSmoothMovementProvider : XRBaseMovementProvider {

        #region variable

        [SerializeField]
        private InputActionProperty movementAction;
        internal InputAction _movementAction = null;

        [SerializeField]
        private Transform headTransform = null;

        #endregion

        #region logic

        #region OnEnable

        protected sealed override void OnEnable() {
            base.OnEnable();
            _movementAction = movementAction.action;
            if (_movementAction != null) enabled = false;
        }

        #endregion

        #region Update

        public sealed override void Update(in float deltaTime) {
            
        }

        #endregion

        #region FixedUpdate

        public sealed override void FixedUpdate(in float deltaTime) {
            
        }

        #endregion

        #endregion

    }

}

#endif