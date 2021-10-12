namespace BlackTundra.World.Items {

    /// <summary>
    /// Describes an <see cref="Item"/>.
    /// </summary>
    public sealed class Item {

        #region variable
        
        /// <summary>
        /// <see cref="Item"/> ID in the item database.
        /// </summary>
        public readonly int id = 0;

        #endregion

        #region property

        /// <summary>
        /// Number of units/cells wide that this <see cref="Item"/> is.
        /// </summary>
        public int width => 1;

        /// <summary>
        /// Number of units/cells high that this <see cref="Item"/> is.
        /// </summary>
        public int height => 1;

        /// <summary>
        /// Area of the <see cref="Item"/> in item units/cells.
        /// </summary>
        public int area => 1;

        #endregion

        #region constructor

        #endregion

        #region logic

        #endregion

    }

}