#if ENABLE_VR

using BlackTundra.Foundation;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace BlackTundra.World.XR {

    /// <summary>
    /// Manages an XR hand.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ActionBasedController))]
    public sealed class XRPlayerHand : MonoBehaviour {

        #region constant

        public const string GripAnimatorPropertyName = "Grip";

        #endregion

        #region variable

        /// <summary>
        /// <see cref="ActionBasedController"/> used to control the hand.
        /// </summary>
        private ActionBasedController controller = null;

        /// <summary>
        /// <see cref="Animator"/> component on the <see cref="handModel"/>.
        /// </summary>
        private Animator handAnimator = null;

        /// <summary>
        /// <see cref="InputAction"/> bound to the primary action.
        /// </summary>
        private InputAction primaryAction = null;

        /// <summary>
        /// <see cref="InputAction"/> bound to gripping an object.
        /// </summary>
        private InputAction gripAction = null;

        #endregion

        #region logic

        #region Awake

        private void Awake() {
            controller = GetComponent<ActionBasedController>();
            primaryAction = controller.activateAction.action;
            gripAction = controller.selectAction.action;
            UpdateHands();
        }

        #endregion

        #region UpdateHands

        private void UpdateHands() {
            Transform model = controller.model;
            handAnimator = model != null ? model.GetComponent<Animator>() : null;
        }

        #endregion

        #region Update

        private void Update() {
            if (handAnimator != null) {
                handAnimator.SetFloat(GripAnimatorPropertyName, gripAction.ReadValue<float>());
            } else {
                UpdateHands();
            }
        }

        #endregion

        #endregion

    }

}

#endif