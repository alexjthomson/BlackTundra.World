using System;

namespace BlackTundra.World.Interaction {

    /// <summary>
    /// Utility class extending <see cref="IInteractable"/>.
    /// </summary>
    public static class IInteractableUtility {

        /// <summary>
        /// Completes an interaction.
        /// </summary>
        /// <param name="interactable"><see cref="IInteractable"/> to interact with.</param>
        /// <param name="sender">Sender of the interaction.</param>
        /// <param name="parameters">Parameters sent with the interaction.</param>
        /// <returns>Returns <c>true</c> if the interaction was completed successfully.</returns>
        public static bool Interact(this IInteractable interactable, in object sender, params object[] parameters) {
            if (interactable == null) throw new ArgumentNullException("interactable");
            if (parameters == null) parameters = new object[0];
            bool success = interactable.InteractStart(sender, parameters);
            return success && interactable.InteractStop(sender, parameters);
        }

        /// <summary>
        /// Gets a hook to a <see cref="IInteractable"/> for an interaction.
        /// </summary>
        /// <param name="interactable"><see cref="IInteractable"/> to interact with.</param>
        /// <param name="sender">Sender of the interaction.</param>
        /// <param name="parameters">Parameters sent with the interaction.</param>
        /// <returns>Hook to the interaction.</returns>
        public static InteractionHook GetInteractionHook(this IInteractable interactable, in object sender, params object[] parameters) {
            if (interactable == null) throw new ArgumentNullException("interactable");
            return new InteractionHook(interactable, sender, parameters ?? new object[0]);
        }

    }

}