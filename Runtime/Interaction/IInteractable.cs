namespace BlackTundra.World.Interaction {

    /// <summary>
    /// Interface for objects that can be interacted with.
    /// </summary>
    public interface IInteractable {

        /// <summary>
        /// Causes an interaction to occur between a sender and the recieving object.
        /// </summary>
        /// <param name="sender">Sender of the interaction.</param>
        /// <param name="parameters">Data sent in the interaction, this data should be serializable.</param>
        /// <returns>Returns <c>true</c> if the interaction occurred successfully.</returns>
        /// <remarks>Don't call this method directly to cause interactions.</remarks>
        bool InteractStart(in object sender, in object[] parameters);

        /// <summary>
        /// Called when an interaction stops.
        /// </summary>
        /// <param name="sender">Sender of the interaction.</param>
        /// <param name="parameters">Data sent in the interaction, this data should be serializable.</param>
        /// <returns>Returns <c>true</c> if the interaction occurred successfully.</returns>
        /// <remarks>Don't call this method directly to cause interactions.</remarks>
        bool InteractStop(in object sender, in object[] parameters);

#if ENABLE_VR

        /// <summary>
        /// Method that can be invoked via XR when the <see cref="IInteractable"/> is selected.
        /// </summary>
        void XRInteractStart();

        /// <summary>
        /// Method that can be invoked via XR when the <see cref="IInteractable"/> selection is exited or finished.
        /// </summary>
        void XRInteractEnd();

#endif

    }

}