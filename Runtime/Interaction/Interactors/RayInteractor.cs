using UnityEngine;
using UnityEngine.InputSystem;

namespace BlackTundra.World.Interaction.Interactors {

    /// <summary>
    /// Manages a raycast based interaction system.
    /// </summary>
#if UNITY_EDITOR
    [AddComponentMenu("Interaction/RayInteractor")]
#endif
    [DisallowMultipleComponent]
    public sealed class RayInteractor : MonoBehaviour {

        #region variable

        /// <summary>
        /// <see cref="LayerMask"/> to use for physics calculations.
        /// </summary>
        [SerializeField]
        private LayerMask layerMask = -1;

        /// <summary>
        /// Maximum interaction range.
        /// </summary>
        [Min(0.0f)]
        [SerializeField]
        private float range = 4.0f;

        /// <summary>
        /// Current <see cref="IInteractable"/> that the <see cref="RayInteractor"/> is continuously hitting with a raycast.
        /// </summary>
        private IInteractable interactable = null;

        /// <summary>
        /// <see cref="Transform"/> component associated with the <see cref="interactable"/>.
        /// </summary>
        private Transform interactableTransform = null;

        /// <summary>
        /// Tracks if an interaction is currently occuring to the <see cref="interactable"/>.
        /// </summary>
        private bool interactionActive = false;

        /// <summary>
        /// Input action used to perform an interaction operation.
        /// </summary>
        [SerializeField]
        private InputActionProperty interactAction;

        #endregion

        #region logic

        #region Update

        private void Update() {
            if (interactionActive) { // there is currently an interaction active
                if (interactable == null) { // there is no current interactable, cancel the active interaction
                    interactionActive = false; // cancel
                } else { // there is a current interactable
                    // limit interaction range:
                    Vector3 position = interactableTransform.position;
                    float sqrDistance = (position - transform.position).sqrMagnitude;
                    if (sqrDistance > range * range) { // interactable further away than maximum interaction range, cancel the interaction
                        try {
                            interactable.InteractStop(this, null); // stop the interaction
                        } finally {
                            interactionActive = false; // cancel the current interaction
                            interactable = null; // remove reference to the interactable
                            interactableTransform = null; // remove reference to the interactable transform
                        }
                    } else { // interactable is within the maximum interaction range
                        // check for input interaction end:
                        InputAction action = interactAction.action;
                        if (action != null) { // there is an input action
                            float inputInteract = action.ReadValue<float>();
                            if (inputInteract < 0.5f) { // input interact does not have a high enough value to sustain the interaction
                                try {
                                    interactable.InteractStop(this, null); // stop the interaction
                                } finally {
                                    interactionActive = false; // stop the current interaction
                                    interactable = null; // remove reference to the interactable
                                    interactableTransform = null; // remove reference to the interactable transform
                                }
                            }
                        }
                    }
                }
            } else { // there is no interaction currently active
                if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, range, layerMask, QueryTriggerInteraction.Ignore)) { // cast interaction ray
                    IInteractable currentInteractable = hit.collider.GetComponent<IInteractable>(); // get any element that implements the IInteractable interface
                    if (currentInteractable == null) { // no interactable component was hit
                        interactable = null;
                    } else { // the hit object is an interactable component
                        if (currentInteractable != interactable) { // the current interactable is different from the last hit interactable
                            interactable = currentInteractable;
                        }
                        InputAction action = interactAction.action; // get the input action
                        if (action != null) { // there is an input action
                            float inputInteract = action.ReadValue<float>();
                            if (inputInteract > 0.5f) { // input interact is above the threshold value to invoke an interaction
                                interactableTransform = hit.collider.transform; // assign the interatable transform
                                interactionActive = true;
                                interactable.InteractStart(this, null);
                            }
                        }
                    }
                } else { // nothing was hit
                    interactable = null;
                }
            }
        }

        #endregion

        #endregion

    }

}