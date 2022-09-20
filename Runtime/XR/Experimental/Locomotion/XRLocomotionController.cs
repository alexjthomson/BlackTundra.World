#if USE_XR_TOOLKIT

using BlackTundra.Foundation.Control;
using BlackTundra.World.Targetting;

using System;

using UnityEngine;

namespace BlackTundra.World.XR.Experimental.Locomotion {

    /// <summary>
    /// Locomotion controller built ontop of the <see cref="PhysicsCharacterController"/> class. This class adds a locomotion layer ontop of the
    /// <see cref="PhysicsCharacterController"/>.
    /// <para>
    /// Movement and turning are split into <see cref="MovementProvider"/> and <see cref="TurnProvider"/> respectfully.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
#if UNITY_EDITOR
    [AddComponentMenu("XR/Experimental/Locomotion Controller")]
#endif
    public sealed class XRLocomotionController : PhysicsCharacterController, IControllable, ITargetable {

        #region variable

        /// <inheritdoc cref="MovementProvider"/>
        private IXRMovementProvider _movementProvider;

        /// <inheritdoc cref="TurnProvider"/>
        private IXRTurnProvider _turnProvider;

        #endregion

        #region property

        public int TargetFlags { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <summary>
        /// <see cref="IXRMovementProvider"/> used by the <see cref="XRLocomotionController"/> for movement.
        /// </summary>
        /// <remarks>
        /// This can be <c>null</c>.
        /// </remarks>
        public IXRMovementProvider MovementProvider {
            get => _movementProvider;
            set => SetMovementProvider(value);
        }

        /// <summary>
        /// <see cref="IXRTurnProvider"/> used by the <see cref="XRLocomotionController"/> for turning.
        /// </summary>
        /// <remarks>
        /// This can be <c>null</c>.
        /// </remarks>
        public IXRTurnProvider TurnProvider {
            get => _turnProvider;
            set => SetTurnProvider(value);
        }

        #endregion

        #region logic

        #region RegisterMovementProvider

        public void RegisterMovementProvider(in IXRMovementProvider movementProvider) {
            _movementProvider = movementProvider;
        }

        #endregion

        #region RegisterTurnProvider

        public void RegisterTurnProvider(in IXRTurnProvider turnProvider) {
            _turnProvider = turnProvider;
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

        #region SetMovementProvider

        /// <summary>
        /// Sets the <see cref="MovementProvider"/> to the provided <paramref name="movementProvider"/>.
        /// </summary>
        /// <param name="movementProvider"><see cref="IXRMovementProvider"/> reference to use for movement. This can be <c>null</c>.</param>
        public void SetMovementProvider(in IXRMovementProvider movementProvider) {
            if (_movementProvider == movementProvider) return; // already set
            if (_movementProvider != null && _movementProvider is Behaviour _behaviour) {
                _behaviour.enabled = false;
            }
            _movementProvider = movementProvider;
            if (movementProvider != null && movementProvider is Behaviour behaviour) {
                behaviour.enabled = true;
            }
        }

        #endregion

        #region SetTurnProvider

        /// <summary>
        /// Sets the <see cref="TurnProvider"/> to the provided <paramref name="turnProvider"/>.
        /// </summary>
        /// <param name="turnProvider"><see cref="IXRTurnProvider"/> reference to use for turning. This can be <c>null</c>.</param>
        public void SetTurnProvider(in IXRTurnProvider turnProvider) {
            if (_turnProvider == turnProvider) return; // already set
            if (_turnProvider != null && _turnProvider is Behaviour _behaviour) {
                _behaviour.enabled = false;
            }
            _turnProvider = turnProvider;
            if (turnProvider != null && turnProvider is Behaviour behaviour) {
                behaviour.enabled = true;
            }
        }

        #endregion

        #endregion

    }

}

#endif