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
    public sealed class XRLocomotionController : CharacterController, IControllable, IPhysicsObject, ITargetable {

        #region constant

        #endregion

        #region variable

        #endregion

        #region property

        public Vector3 position => throw new NotImplementedException();

        public Quaternion rotation => throw new NotImplementedException();

        public Vector3 centreOfMass => throw new NotImplementedException();

        public float mass => throw new NotImplementedException();

        public int TargetFlags { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        #endregion

        #region logic

        #region AddForce

        public void AddForce(in Vector3 force, in ForceMode forceMode) {
            throw new NotImplementedException();
        }

        #endregion

        #region AddForceAtPosition

        public void AddForceAtPosition(in Vector3 force, in Vector3 position, in ForceMode forceMode) {
            throw new NotImplementedException();
        }

        #endregion

        #region AddExplosionForce

        public void AddExplosionForce(in float force, in Vector3 point, in float radius, float upwardsModifier, in ForceMode forceMode) {
            throw new NotImplementedException();
        }

        #endregion

        #region OnControlGained

        public ControlFlags OnControlGained() {
            throw new NotImplementedException();
        }

        #endregion

        #region OnControlRevoked

        public void OnControlRevoked() {
            throw new NotImplementedException();
        }

        #endregion

        #endregion

    }

}

#endif