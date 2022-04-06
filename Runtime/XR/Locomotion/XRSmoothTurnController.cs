#if USE_XR_TOOLKIT

using Unity.XR.CoreUtils;

using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackTundra.World.XR.Locomotion {

    public class XRSmoothTurnController : XRTurnProvider {

        #region variable

        public float turnSpeed = 180.0f;

        private readonly XROrigin origin;

        private readonly InputAction turnAction;

        #endregion

        #region constructor

        public XRSmoothTurnController(in XRLocomotionController locomotion) : base(locomotion) {
            origin = locomotion.origin;
            turnAction = locomotion.inputTurnAction;
        }

        #endregion

        #region logic

        #region Update

        protected internal sealed override void Update(in float deltaTime) {
            Vector2 inputTurn = turnAction.ReadValue<Vector2>();
            float angularSpeed = inputTurn.x * turnSpeed * deltaTime;
            if (Mathf.Approximately(angularSpeed, 0f)) return; // no rotation, stop here
            origin.RotateAroundCameraUsingOriginUp(angularSpeed);
        }

        #endregion

        #endregion

    }

}

#endif