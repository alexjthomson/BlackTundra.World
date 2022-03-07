using BlackTundra.Foundation.Utility;

using System;

using UnityEngine;
using UnityEngine.Events;

using Console = BlackTundra.Foundation.Console;

namespace BlackTundra.World.Interaction.Interactables {

    /// <summary>
    /// Handles interactions with a <see cref="Rigidbody"/> component.
    /// </summary>
#if UNITY_EDITOR
    [AddComponentMenu("Interaction/Rigidbody Grab Interactable")]
#endif
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class RigidbodyGrabInteractable : MonoBehaviour, IInteractable {

        #region variable

        /// <summary>
        /// Amount of power to apply to the interaction. The higher this value is, the snappier the interaction will be.
        /// If this is too high, it may cause the object to overshoot the target position.
        /// </summary>
        [SerializeField]
        [Min(0.01f)]
        private float power = 1.0f;

        /// <summary>
        /// Invoked when the interaction state changes.
        /// </summary>
        /// <remarks>
        /// <see cref="bool"/>: <c>true</c> if the interaction was started, <c>false</c> if the interaction was ended.
        /// <see cref="Behaviour"/>: <see cref="Behaviour"/> component that made the interaction.
        /// <see cref="object">object[]</see>: Parameters passed into the interaction.
        /// </remarks>
        [SerializeField]
        private UnityEvent<bool, Behaviour, object[]> onInteract = null;

        /// <summary>
        /// Invoked when an interaction starts.
        /// </summary>
        [SerializeField]
        private UnityEvent onInteractStart = null;

        /// <summary>
        /// Invoked when an interaction stops.
        /// </summary>
        [SerializeField]
        private UnityEvent onInteractStop = null;

        /// <summary>
        /// Interaction sender.
        /// </summary>
        private Transform senderTransform = null;

        /// <summary>
        /// Target distance from the <see cref="senderTransform"/> in the forward direction.
        /// </summary>
        private float targetDistance = 0.0f;

        /// <summary>
        /// <see cref="Rigidbody"/> component attached to the <see cref="RigidbodyGrabInteractable"/>.
        /// </summary>
#if UNITY_EDITOR
        new
#endif
        private Rigidbody rigidbody = null;

        #endregion

        #region logic

        #region Awake

        private void Awake() {
            rigidbody = GetComponent<Rigidbody>();
            Console.AssertReference(rigidbody);
        }

        #endregion

        #region OnEnable

        private void OnEnable() {
            if (senderTransform == null) enabled = false; // only allow the component to be enabled while there is an interaction sender
        }

        #endregion

        #region FixedUpdate

        private void FixedUpdate() {
            if (senderTransform == null) {
                enabled = false;
                return;
            }
            Vector3 targetPosition = senderTransform.position + (senderTransform.forward * targetDistance);
            Vector3 deltaPosition = targetPosition - rigidbody.position;
            rigidbody.velocity = deltaPosition * power;
        }

        #endregion

        #region InteractStart

        /// <summary>
        /// Invoked when an interaction starts.
        /// </summary>
        public bool InteractStart(in object sender, in object[] parameters) {
            if (sender is Behaviour behaviour) {
                senderTransform = behaviour.transform;
                Vector3 forward = senderTransform.forward;
                targetDistance = Vector3.Distance(
                    MathsUtility.ClosestPointOnInfiniteLine(
                        senderTransform.position,
                        forward,
                        rigidbody.position
                    ),
                    senderTransform.position
                );
                enabled = true;
                if (onInteract != null) onInteract.Invoke(true, behaviour, parameters);
                if (onInteractStart != null) onInteractStart.Invoke();
                return true;
            }
            return false;
        }

        #endregion

        #region InteractStop

        /// <summary>
        /// Invoked when an interaction ends.
        /// </summary>
        public bool InteractStop(in object sender, in object[] parameters) {
            if (senderTransform != null && sender is Behaviour behaviour && senderTransform == behaviour.transform) {
                enabled = false;
                senderTransform = null;
                if (onInteract != null) onInteract.Invoke(true, behaviour, parameters);
                if (onInteractStop != null) onInteractStop.Invoke();
                return true;
            }
            return false;
        }

        #endregion

        #region XRInteractStart

        public void XRInteractStart() => throw new NotSupportedException();

        #endregion

        #region XRInteractEnd

        public void XRInteractEnd() => throw new NotSupportedException();

        #endregion

        #endregion

    }

}