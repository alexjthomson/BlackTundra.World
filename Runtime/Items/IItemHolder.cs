namespace BlackTundra.World.Items {

    /// <summary>
    /// Holds a <see cref="WorldItem"/>.
    /// </summary>
    public interface IItemHolder {

        bool IsHoldingItem();
        bool IsHoldingItem(in WorldItem item);

        /// <returns>
        /// Returns <c>true</c> if the <paramref name="taker"/> can take the <paramref name="item"/> from the <see cref="IItemHolder"/>.
        /// </returns>
        bool CanTakeItem(in WorldItem item, in IItemHolder taker);

        void OnHoldItem(in WorldItem item);
        void OnReleaseItem(in WorldItem item);

    }

}