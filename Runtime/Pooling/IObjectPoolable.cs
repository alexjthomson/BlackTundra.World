namespace BlackTundra.World.Pooling {

    /// <summary>
    /// Describes an object that can be used by an <see cref="ObjectPool"/>.
    /// </summary>
    public interface IObjectPoolable {

        /// <returns>
        /// Returns <c>true</c> if the object is available to be used by an <see cref="ObjectPool"/>.
        /// </returns>
        bool IsAvailable(in ObjectPool objectPool);

        /// <summary>
        /// Invoked when the object is used by an <see cref="ObjectPool"/>.
        /// </summary>
        void OnPoolUse(in ObjectPool objectPool);

        /// <summary>
        /// Invoked when the object is released from the <see cref="ObjectPool"/>.
        /// </summary>
        void OnPoolRelease(in ObjectPool objectPool);

        /// <summary>
        /// Invoked when the <see cref="ObjectPool"/> managing this object is disposed.
        /// </summary>
        /// <remarks>
        /// If the object is being used by the pool, <see cref="OnPoolRelease(in ObjectPool)"/> will not first be invoked.
        /// </remarks>
        void OnPoolDispose(in ObjectPool objectPool);

    }

}