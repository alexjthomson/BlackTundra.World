namespace BlackTundra.World.Items {

    /// <summary>
    /// Holds a <see cref="WorldItem"/>.
    /// </summary>
    public interface IItemHolder {

        bool IsHoldingItem();
        bool IsHoldingItem(in WorldItem item);

        void OnHoldItem(in WorldItem item);
        void OnReleaseItem(in WorldItem item);

    }

}