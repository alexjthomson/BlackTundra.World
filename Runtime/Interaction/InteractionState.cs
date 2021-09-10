namespace BlackTundra.World.Interaction {

    /// <summary>
    /// Describes the state of an interaction.
    /// </summary>
    public enum InteractionState : int {

        /// <summary>
        /// The interaction hasn't occurred yet.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// the interaction has started.
        /// </summary>
        Started = 1,

        /// <summary>
        /// The interaction has been completed.
        /// </summary>
        Completed = 2

    }

}