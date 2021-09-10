namespace BlackTundra.World.Interaction {

    /// <summary>
    /// References and tracks an interaction.
    /// </summary>
    public sealed class InteractionHook {

        #region variable

        /// <summary>
        /// <see cref="IInteractable"/> to interact with.
        /// </summary>
        public readonly IInteractable interactable;

        /// <summary>
        /// Sender of the interaction.
        /// </summary>
        public readonly object sender;

        /// <summary>
        /// Parameters the interaction was created with.
        /// </summary>
        private readonly object[] parameters;

        /// <inheritdoc cref="InteractionState"/>
        private InteractionState state;

        #endregion

        #region property

        /// <inheritdoc cref="InteractionState"/>
        public InteractionState State => state;

        #endregion

        #region constructor

        internal InteractionHook(in IInteractable interactable, in object sender, in object[] parameters) {
            this.interactable = interactable;
            this.sender = sender;
            this.parameters = parameters;
            state = InteractionState.Pending;
        }

        #endregion

        #region logic

        /// <summary>
        /// Starts an interaction.
        /// </summary>
        /// <returns>Returns <c>true</c> if the interaction was started correctly.</returns>
        public bool Start() {
            if (state >= InteractionState.Started) return false;
            try {
                return interactable.InteractStart(sender, parameters);
            } finally {
                state = InteractionState.Started;
            }
        }

        /// <summary>
        /// Stops/completes an interaction.
        /// </summary>
        /// <returns>Returns <c>true</c> if the interaction was stopped correctly.</returns>
        public bool Stop() {
            if (state >= InteractionState.Completed) return false;
            try {
                return interactable.InteractStop(sender, parameters);
            } finally {
                state = InteractionState.Completed;
            }
        }

        #endregion

    }

}