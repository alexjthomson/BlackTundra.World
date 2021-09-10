using System.Collections.Generic;

using UnityEngine;

namespace BlackTundra.World.Interaction {
    
    /// <summary>
    /// Raycaster specifically used for casting interaction rays and handling interactions automatically.
    /// </summary>
    public sealed class InteractionRaycaster {

        #region variable

        /// <summary>
        /// Current <see cref="Collider"/> being interacted with.
        /// </summary>
        private Collider currentInteractable;

        /// <summary>
        /// <see cref="List{T}"/> of <see cref="InteractionHook"/> instances used for tracking interactions with the current interactable.
        /// </summary>
        private readonly List<InteractionHook> currentHooks;

        #endregion

        #region constructor

        /// <summary>
        /// Constructs a new <see cref="InteractionRaycaster"/>.
        /// </summary>
        public InteractionRaycaster() {
            currentInteractable = null;
            currentHooks = new List<InteractionHook>();
        }

        #endregion

        #region logic

        #region Raycast

        /// <summary>
        /// Casts an interaction ray.
        /// </summary>
        /// <param name="sender">Sender of the ray.</param>
        /// <param name="origin">Origin of the ray.</param>
        /// <param name="direction">Direction of the ray.</param>
        /// <param name="hit"><see cref="RaycastHit"/> to use for hit output information.</param>
        /// <param name="distance">Maximum distance the ray should travel.</param>
        /// <param name="layerMask">Layermask for the ray.</param>
        /// <param name="parameters">Additional data to supply to the interaction.</param>
        /// <returns>Returns <c>true</c> if the raycast resulted in a successful interaction.</returns>
        public bool Raycast(in object sender, in Vector3 origin, in Vector3 direction, out RaycastHit hit, in float distance, in LayerMask layerMask, params object[] parameters) {
            if (parameters == null) parameters = new object[0];
            if (Physics.Raycast(origin, direction, out hit, distance, layerMask)) {
                if (hit.collider != currentInteractable) {
                    bool success = false;
                    Reset();
                    currentInteractable = hit.collider;
                    Component[] components = currentInteractable.GetComponents<Component>();
                    foreach (Component component in components) {
                        if (component is IInteractable interactable) {
                            InteractionHook hook = new InteractionHook(interactable, sender, parameters);
                            hook.Start();
                            currentHooks.Add(hook);
                            success = true;
                        }
                    }
                    if (success) return true; // there are interactables, therefore success
                } else return true; // collider hasnt changed
            }
            Reset();
            return false; // the current interaction ended
        }

        #endregion

        #region Reset

        /// <summary>
        /// Resets the state of the <see cref="InteractionRaycaster"/> and completes the current
        /// interaction if there is one.
        /// </summary>
        public void Reset() {
            if (currentInteractable == null) return;
            currentInteractable = null;
            if (currentHooks.Count > 0) {
                foreach (InteractionHook hook in currentHooks) hook.Stop();
                currentHooks.Clear();
            }
        }

        #endregion

        #endregion

    }

}