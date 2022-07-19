#if USE_XR_TOOLKIT

using BlackTundra.Foundation.Control;
using BlackTundra.Foundation.Utility;

using BlackTundra.World.CameraSystem;
using BlackTundra.World.Targetting;

using System;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace BlackTundra.World.XR.Experimental.Locomotion {

    [DisallowMultipleComponent]
#if UNITY_EDITOR
    [AddComponentMenu("XR/Experimental/Locomotion Controller")]
#endif
    public sealed class XRLocomotionController : PhysicsCharacterController, IControllable, ITargetable {

        #region variable

        #endregion

        #region property

        public int TargetFlags { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        #endregion

        #region logic

        public ControlFlags OnControlGained() {
            throw new NotImplementedException();
        }

        public void OnControlRevoked() {
            throw new NotImplementedException();
        }

        #endregion

    }

}

#endif