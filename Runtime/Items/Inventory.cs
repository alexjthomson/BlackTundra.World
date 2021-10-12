using System;

namespace BlackTundra.World.Items {

    /// <summary>
    /// Contains and manages a 2D space containing <see cref="Item"/> instances.
    /// </summary>
    public sealed class Inventory {

        #region constant

        /// <summary>
        /// Maximum length of a side of the inventory (inclusive).
        /// </summary>
        public const int MaxLength = 46340;
        
        /// <summary>
        /// Maximum width (inclusive).
        /// </summary>
        public const int MaxWidth = MaxLength;

        /// <summary>
        /// Maximum height (inclusive).
        /// </summary>
        public const int MaxHeight = MaxLength;

        #endregion

        #region variable

        /// <summary>
        /// Number of cells wide that the <see cref="Inventory"/> is.
        /// </summary>
        public readonly int width;

        /// <summary>
        /// Number of cells tall that the <see cref="Inventory"/> is.
        /// </summary>
        public readonly int height;

        /// <summary>
        /// Area of the inventory (<see cref="width"/> * <see cref="height"/>).
        /// </summary>
        public readonly int area;

        /// <summary>
        /// <see cref="Item"/> buffer used to store items in the <see cref="Inventory"/>.
        /// </summary>
        private readonly Item[] itemBuffer;

        /// <summary>
        /// Grid that references indicies to items in the <see cref="itemBuffer"/>.
        /// </summary>
        private readonly int[,] grid;

        #endregion

        #region property

        /// <summary>
        /// Number of <see cref="Item">items</see> contained within the <see cref="Inventory"/>.
        /// </summary>
        public int ItemCount {
            get {
                for (int i = 0; i < area; i++) if (itemBuffer[i] == null) return i;
                return area;
            }
        }

        #endregion

        #region constructor

        /// <summary>
        /// Constructs a new <see cref="Inventory"/> with a fixed <paramref name="width"/> and <paramref name="height"/>.
        /// </summary>
        public Inventory(in int width, in int height) {
            if (width < 0 || width > MaxWidth) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 0 || height > MaxHeight) throw new ArgumentOutOfRangeException(nameof(height));
            this.width = width;
            this.height = height;
            area = width * height;
            itemBuffer = new Item[area];
            grid = new int[width, height];
            ClearGrid();
        }

        #endregion

        #region logic

        #region Clear

        /// <summary>
        /// Clears the entire <see cref="Inventory"/>.
        /// </summary>
        /// <returns>Array containing each <see cref="Item"/> removed from the <see cref="Inventory"/>.</returns>
        public Item[] Clear() {
            int itemCount = ItemCount;
            Item[] items = new Item[itemCount];
            Array.Copy(itemBuffer, 0, items, 0, itemCount);
            for (int i = itemCount - 1; i >= 0; i--) itemBuffer[i] = null;
            ClearGrid();
            return items;
        }

        #endregion

        #region ClearGrid

        /// <summary>
        /// Clears the item grid.
        /// </summary>
        private void ClearGrid() {
            for (int x = width - 1; x >= 0; x--) { // iterate width
                for (int y = height - 1; y >= 0; y--) { // iterate height
                    grid[x, y] = -1; // set empty cell
                }
            }
        }

        #endregion

        #region SetArea

        /// <summary>
        /// Sets an area of the grid to a value.
        /// </summary>
        /// <param name="x">Start x position.</param>
        /// <param name="y">Start y position.</param>
        /// <param name="width">Width of the area to set.</param>
        /// <param name="height">Height of the area to set.</param>
        /// <param name="value">Value to set the area to.</param>
        private void SetArea(in int x, in int y, in int width, in int height, in int value) {
            for (int px = x + width - 1; px >= x; px--) { // iterate width
                for (int py = y + height - 1; py >= x; py--) { // iterate height
                    grid[x, y] = value; // set value
                }
            }
        }

        #endregion

        #region TryLocateArea

        /// <summary>
        /// Locates an area of a specified <see cref="width"/> and <see cref="height"/>.
        /// </summary>
        /// <param name="width">Width of the area to find.</param>
        /// <param name="height">Height of the area to find.</param>
        /// <param name="x">x coordinate of the found area.</param>
        /// <param name="y">y coordinate of the found area.</param>
        /// <returns>Returns <c>true</c> if an area was located; otherwise <c>false</c> is returned.</returns>
        public bool TryLocateArea(in int width, in int height, out int x, out int y) {
            if (width < 1 || width > this.width) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1 || height > this.height) throw new ArgumentOutOfRangeException(nameof(height));
            for (int py = 0; py < this.height; py++) { // iterate each row one at a time
                for (int px = 0; px < this.width; px++) { // iterate each column in order
                    if (grid[px, py] == -1) { // top right cell is free, start searching for space
                        bool empty = true; // store if the current search has an empty area.
                        if (height > 1) { // height is more than 1
                            for (int spy = py + 1; spy < py + height; spy++) { // check left side is empty
                                if (grid[px, spy] != -1) { // not empty
                                    empty = false;
                                    break;
                                }
                            }
                            if (!empty) continue; // this area is not empty
                        }
                        if (width > 1) { // width is more than 1
                            int yLimit = py + height;
                            for (int spx = px + 1; spx < px + width; spx++) { // iterate each remaining column
                                for (int spy = py; spy < yLimit; spy++) { // iterate each row in the current column
                                    if (grid[spx, spy] != -1) { // not empty
                                        empty = false;
                                        break;
                                    }
                                }
                                if (!empty) break;
                            }
                        }
                        if (empty) { // all checks passed, an empty area has been found
                            x = px; y = py; // assign x and y positions
                            return true; // successfully stop here
                        }
                    }
                }
            }
            x = -1; y = -1; // assign x and y positions to -1
            return false; // return false since no area was found
        }

        #endregion

        #region IsAreaEmpty

        /// <returns>
        /// Returns <c>true</c> if the area (<paramref name="width"/>, <paramref name="height"/>) at [<paramref name="x"/>, <paramref name="y"/>]
        /// is empty; otherwise, <c>false</c> is returned.
        /// </returns>
        public bool IsAreaEmpty(in int x, in int y, in int width, in int height) {
            if (width < 1 || width > this.width) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1 || height > this.height) throw new ArgumentOutOfRangeException(nameof(height));
            if (x < 0 || x > this.width - width) throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y > this.height - height) throw new ArgumentOutOfRangeException(nameof(y));
            for (int py = y + height - 1; py >= y; py--) { // iterate y
                for (int px = x + width - 1; px >= x; px--) { // iterate x
                    if (grid[px, py] != -1) return false; // found occupied cell
                }
            }
            return true; // all checks passed
        }

        #endregion

        #region ReplaceGridIndex

        /// <summary>
        /// Replaces any occurance of the <paramref name="oldIndex"/> in the <see cref="grid"/> with the <paramref name="newIndex"/> value.
        /// </summary>
        private void ReplaceGridIndex(in int oldIndex, in int newIndex) {
            for (int py = height - 1; py >= 0; py--) { // iterate rows
                for (int px = width - 1; px >= 0; px--) { // iterate columns
                    if (grid[px, py] == oldIndex) grid[px, py] = newIndex; // replace index
                }
            }
        }

        #endregion

        #region TryLocateItem

        /// <returns>
        /// Returns <c>true</c> if the specified <paramref name="item"/> is found.
        /// </returns>
        public bool TryLocateItem(in Item item, out int x, out int y) {
            if (item == null) throw new ArgumentNullException(nameof(item));
            int index = IndexOf(item);
            if (index == -1) { // item not found
                x = -1; y = -1;
                return false;
            }
            return TryLocateItem(index, out x, out y); // try locate item by index
        }

        /// <returns>
        /// Returns <c>true</c> if the specified <paramref name="itemIndex"/> is found.
        /// </returns>
        public bool TryLocateItem(in int itemIndex, out int x, out int y) {
            if (itemIndex < 0 || itemIndex >= area) throw new ArgumentOutOfRangeException(nameof(itemIndex));
            if (itemBuffer[itemIndex] == null) { // item index not assigned in item buffer
                x = -1; y = -1;
                return false;
            }
            for (int py = 0; py < height; py++) {
                for (int px = 0; px < width; px++) {
                    if (grid[px, py] == itemIndex) { // found item position
                        x = px;
                        y = py;
                        return true;
                    }
                }
            }
            // failed to find item:
            x = -1; y = -1;
            return false;
        }

        #endregion

        #region IndexOf

        /// <returns>
        /// Returns the index of the <paramref name="item"/> or <c>-1</c> if no item was found.
        /// </returns>
        public int IndexOf(in Item item) {
            if (item == null) throw new ArgumentNullException(nameof(item));
            Item temp; // temporary reference to the current item
            for (int i = 0; i < area; i++) { // iterate item buffer
                temp = itemBuffer[i]; // get reference to current item
                if (temp == null) return -1; // reached end of item buffer
                if (temp.Equals(item)) return i; // found item
            }
            return -1; // no match found
        }

        #endregion

        #region TryInjectItemAt

        /// <summary>
        /// Attempts to inject an <see cref="Item"/> into the <see cref="Inventory"/>. This method should only be used
        /// when it is known that the item can be inserted at a point with a specified width, height, and rotation.
        /// </summary>
        /// <returns>Returns <c>true</c> if the injection was successful; otherwise, <c>false</c> is returned.</returns>
        private bool TryInjectItemAt(in Item item, in int x, in int y, in int width, in int height, in bool rotated) {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (width < 1 || width > this.width) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1 || height > this.height) throw new ArgumentOutOfRangeException(nameof(height));
            if (x < 0 || x > this.width - width) throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y > this.height - height) throw new ArgumentOutOfRangeException(nameof(y));
            int injectIndex = ItemCount; // get the index to inject the item at
            if (injectIndex >= area) return false; // maximum number of items reached
            itemBuffer[injectIndex] = item; // insert into item buffer
            if (rotated) SetArea(x, y, height, width, injectIndex); else SetArea(x, y, width, height, injectIndex); // set item area
            return true;
        }

        #endregion

        #region TryAdd

        public bool TryAdd(in Item item) {
            if (item == null) throw new ArgumentNullException(nameof(item));
            int width = item.width, height = item.height; bool rotated = false;
            if (!TryLocateArea(width, height, out int px, out int py)) { // try find non-rotated area
                if (!TryLocateArea(height, width, out px, out py)) { // try find rotated area
                    return false; // failed to find area for any orientation
                }
                rotated = true; // set rotated flag to true
            }
            return TryInjectItemAt(item, px, py, width, height, rotated);
        }

        #endregion

        #region TryAddAt

        public bool TryAddAt(in Item item, in int x, in int y) {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (x < 0 || x >= this.width) throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y >= this.height) throw new ArgumentOutOfRangeException(nameof(y));
            int width = item.width, height = item.height; bool rotated = false;
            if (!IsAreaEmpty(x, y, width, height)) { // try find non-rotated area
                if (!IsAreaEmpty(x, y, height, width)) { // try find rotated area
                    return false; // failed to find area for any orientation
                }
                rotated = true;
            }
            return TryInjectItemAt(item, x, y, width, height, rotated);
        }

        #endregion

        #region TryRemove

        public bool TryRemove(in Item item) {
            if (item == null) throw new ArgumentNullException(nameof(item));
            int itemIndex = IndexOf(item);
            if (itemIndex == -1) return false; // item not found
            return TryRemove(itemIndex);
        }

        public bool TryRemove(in int itemIndex) {
            if (itemIndex < 0 || itemIndex >= area) throw new ArgumentOutOfRangeException(nameof(itemIndex));
            if (itemBuffer[itemIndex] == null) return false; // item not found
            RemoveFromItemBufferAt(itemIndex); // remove item from item buffer
            return true; // successfully removed
        }

        #endregion

        #region TryRemoveAt

        public bool TryRemoveAt(in int x, in int y, out Item item) {
            if (x < 0 || x >= width) throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y >= height) throw new ArgumentOutOfRangeException(nameof(y));
            int itemIndex = grid[x, y];
            if (itemIndex == -1) { // coordinates have no item reference
                item = null;
                return false;
            }
            item = itemBuffer[itemIndex]; // get a reference to the item to remove
            RemoveFromItemBufferAt(itemIndex); // remove item from item buffer
            return true; // success
        }

        #endregion

        #region RemoveFromItemBufferAt

        /// <summary>
        /// Removes an <see cref="Item"/> by <paramref name="index"/> from the <see cref="itemBuffer"/> and <see cref="grid"/>.
        /// </summary>
        private void RemoveFromItemBufferAt(in int index) {
            if (index < 0 || index >= area) throw new ArgumentOutOfRangeException(nameof(index));
            if (index == area - 1 || itemBuffer[index + 1] == null) {
                itemBuffer[index] = null; // at end of item buffer
                ReplaceGridIndex(index, -1); // replace grid index with empty index
            } else {
                Array.Copy(itemBuffer, index + 1, itemBuffer, index, itemBuffer.Length - index - 1); // shift left
                int temp;
                for (int py = height - 1; py >= 0; py--) { // iterate rows
                    for (int px = width - 1; px >= 0; px--) { // iterate columns
                        temp = grid[px, py]; // get item index reference in current grid position
                        if (temp > index) grid[px, py] = temp - 1; // shift left by 1 if more than removed index
                        else if (temp == index) grid[px, py] = -1; // remove reference to removed item
                    }
                }
            }
        }

        #endregion

        #region IndexAt

        /// <returns>
        /// Returns the item index at [<paramref name="x"/>, <paramref name="y"/>].
        /// </returns>
        public int IndexAt(in int x, in int y) {
            if (x < 0 || x >= width) throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y >= height) throw new ArgumentOutOfRangeException(nameof(y));
            return grid[x, y];
        }

        #endregion

        #region ItemAt

        /// <returns>
        /// Returns the <see cref="Item"/> at [<paramref name="x"/>, <paramref name="y"/>].
        /// </returns>
        public Item ItemAt(in int x, in int y) {
            if (x < 0 || x >= width) throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y >= height) throw new ArgumentOutOfRangeException(nameof(y));
            int itemIndex = grid[x, y];
            return itemIndex == -1 ? null : itemBuffer[itemIndex]; // return item at grid index
        }

        #endregion

        #region Contains

        /// <returns>
        /// Returns <c>true</c> if the <see cref="Inventory"/> contains the specified <paramref name="item"/>; otherwise, <c>false</c> is returned.
        /// </returns>
        public bool Contains(in Item item) {
            if (item == null) throw new ArgumentNullException(nameof(item));
            Item temp;
            for (int i = 0; i < area; i++) {
                temp = itemBuffer[i];
                if (temp == null) return false;
                if (temp.Equals(item)) return true;
            }
            return false;
        }

        #endregion

        #endregion

    }

}