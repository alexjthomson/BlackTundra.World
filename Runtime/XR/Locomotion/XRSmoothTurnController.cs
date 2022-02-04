#if USE_XR_TOOLKIT

using Unity.XR.CoreUtils;

using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackTundra.World.XR.Locomotion {

    public sealed class XRSmoothTurnController : XRTurnController {

        #region variable

        private readonly XROrigin origin;

        private readonly InputAction turnAction;

        #endregion

        #region constructor

        internal XRSmoothTurnController(in XRLocomotionController locomotion) : base(locomotion) {
            origin = locomotion.origin;
            turnAction = locomotion.inputTurnAction;
        }

        #endregion

        #region logic

        #region Update

        protected internal sealed override void Update(in float deltaTime) {
            Vector2 inputTurn = turnAction.ReadValue<Vector2>();
            float angularSpeed = inputTurn.x * XRLocomotionController._turnBaseSpeed * deltaTime;
            if (Mathf.Approximately(angularSpeed, 0f)) return; // no rotation, stop here
            origin.RotateAroundCameraUsingOriginUp(angularSpeed);
        }

        #endregion

        #endregion

    }

}

#endif