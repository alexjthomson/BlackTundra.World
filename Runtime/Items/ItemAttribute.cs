namespace BlackTundra.World.Items {

    /// <summary>
    /// Manages a single attribute related to an <see cref="Item"/> instance.
    /// </summary>
    /// <remarks>
    /// The <see cref="ItemAttribute"/> class is created without a constructor much like a Unity component. If they were
    /// loaded from bytes, <see cref="FromBytes(in byte[])"/> is invoked. This method should load the attribute state. If
    /// the attribute needs to be serialized/saved, <see cref="ToBytes"/> is invoked. This data is stored along-side the
    /// rest of the data related to the <see cref="item"/>.
    /// </remarks>
    public abstract class ItemAttribute {

        #region property

        /// <summary>
        /// <see cref="Item"/> that this <see cref="ItemAttribute"/> belongs to.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public Item item { internal set; get; } = null;
#pragma warning restore IDE1006 // naming styles

        #endregion

        #region logic

        #region OnRecieveMessage

        /// <summary>
        /// Responsible for processing a message sent from the <see cref="item"/>.
        /// </summary>
        /// <typeparam name="T">Type of message that is being recieved.</typeparam>
        /// <param name="message">Content of the message.</param>
        protected internal abstract void ProcessMessage<T>(in T message);

        #endregion

        #region ToBytes

        /// <summary>
        /// Converts the important data associated with the <see cref="ItemAttribute"/> to a <see cref="byte"/> array.
        /// </summary>
        protected internal abstract byte[] ToBytes();

        #endregion

        #region FromBytes

        /// <summary>
        /// Reads data from a <see cref="byte"/> array and overrides the state of the <see cref="ItemAttribute"/>.
        /// </summary>
        protected internal abstract void FromBytes(in byte[] bytes);

        #endregion

        #endregion

    }

}