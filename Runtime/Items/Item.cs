using System;
using System.Collections.Generic;

namespace BlackTundra.World.Items {

    /// <summary>
    /// Class responsible for storing information about an item.
    /// </summary>
    public sealed class Item {

        #region variable

        /// <summary>
        /// ID of the <see cref="Item"/> in the item database.
        /// </summary>
        public readonly int id;

        /// <summary>
        /// <see cref="ItemAttribute"/> instances responsible for controlling attributes, properties, and behaviours related to this item.
        /// </summary>
        private readonly List<ItemAttribute> attributes;

        #endregion

        #region property

        /*
        /// <summary>
        /// <see cref="ItemData"/> associated with the <see cref="Item"/>.
        /// </summary>
        public ItemData data => ItemData.GetItem(id);
        */

        /// <summary>
        /// Width of the <see cref="Item"/>.
        /// </summary>
        public int width => ItemData.GetItem(id).width;

        /// <summary>
        /// Height of the <see cref="Item"/>.
        /// </summary>
        public int height => ItemData.GetItem(id).height;

        /// <summary>
        /// Tracks if the <see cref="Item"/> is rotated by 90 degrees or not.
        /// </summary>
        public bool rotated { get; internal set; } = false;

        /// <summary>
        /// Number of tags that the <see cref="Item"/> has.
        /// </summary>
        public int TagCount => ItemData.GetItem(id).tags.Length;

        #endregion

        #region constructor

        public Item(in int id) {
            if (id < 0 || id >= ItemData.ItemCount) throw new ArgumentOutOfRangeException(nameof(id));
            this.id = id;
            attributes = new List<ItemAttribute>();
        }

        public Item(in int id, in IEnumerable<ItemAttribute> attributes) {
            if (id < 0 || id >= ItemData.ItemCount) throw new ArgumentOutOfRangeException(nameof(id));
            if (attributes == null) throw new ArgumentNullException(nameof(attributes));
            this.id = id;
            this.attributes = new List<ItemAttribute>(attributes);
        }

        #endregion

        #region logic

        #region SendMessage

        /// <summary>
        /// Sends a <paramref name="message"/> of type <paramref name="T"/> to each <see cref="ItemAttribute"/> associated with the <see cref="Item"/>.
        /// </summary>
        /// <param name="message">Non-null message to send to each <see cref="ItemAttribute"/> associated with the <see cref="Item"/>.</param>
        public void SendMessage<T>(in T message) {
            if (message == null) throw new ArgumentNullException(nameof(message));
            ItemAttribute attribute;
            for (int i = attributes.Count - 1; i >= 0; i--) {
                attribute = attributes[i];
                attribute.ProcessMessage(message);
            }
        }

        #endregion

        #region IndexOf
        /*
        /// <returns>
        /// Returns the index of the <paramref name="attribute"/> in the <see cref="attributes"/> list.
        /// </returns>
        private int IndexOf(in ItemAttribute attribute) {
            ItemAttribute temp; // temporary reference to current attribute
            for (int i = attributes.Count; i >= 0; i--) { // iterate each attribute
                temp = attributes[i]; // get the current attribute
                if (temp.Equals(attribute)) return i; //  match found, return index
            }
            return -1; // no match found
        }
        */
        #endregion

        #region AddAttribute

        public T AddAttribute<T>() where T : ItemAttribute, new() {
            T attribute = new T(); // construct a new attribute
            attributes.Add(attribute);
            return attribute;
        }

        #endregion

        #region GetAttribute

        /// <returns>
        /// Returns a reference to an <see cref="ItemAttribute"/> of type <typeparamref name="T"/> or <c>null</c> if none was found.
        /// </returns>
        public T GetAttribute<T>() where T : ItemAttribute, new() {
            ItemAttribute attribute;
            for (int i = attributes.Count - 1; i >= 0; i--) {
                attribute = attributes[i];
                if (attribute is T t) return t;
            }
            return null;
        }

        #endregion

        #region GetAttributes

        /// <returns>
        /// Returns an <see cref="IEnumerable{T}"/> of each reference to an <see cref="ItemAttribute"/> of type <typeparamref name="T"/> or
        /// <c>null</c> if none was found.
        /// </returns>
        public IEnumerable<T> GetAttributes<T>() where T : ItemAttribute, new() {
            ItemAttribute attribute;
            for (int i = attributes.Count - 1; i >= 0; i--) {
                attribute = attributes[i];
                if (attribute is T t) yield return t;
            }
        }

        #endregion

        #region RemoveAttribute

        /// <returns>
        /// Returns an attribute of type <see cref="T"/> that was removed from the <see cref="Item"/>. If no attribute was removed, <c>null</c>
        /// is returned.
        /// </returns>
        public T RemoveAttribute<T>() where T : ItemAttribute, new() {
            ItemAttribute attribute;
            for (int i = attributes.Count - 1; i >= 0; i--) {
                attribute = attributes[i];
                if (attribute is T t) {
                    attributes.RemoveAt(i);
                    return t;
                }
            }
            return null; // nothing removed
        }

        /// <returns>
        /// Returns a reference to the attribute removed from the <see cref="Item"/>. If the <paramref name="attribute"/> was not found in the <see cref="Item"/>,
        /// <c>null</c> is returned; otherwise a reference to the <paramref name="attribute"/> is returned.
        /// </returns>
        public T RemoveAttribute<T>(in T attribute) where T : ItemAttribute, new() {
            if (attribute == null) throw new ArgumentNullException(nameof(attribute));
            ItemAttribute temp;
            for (int i = attributes.Count - 1; i >= 0; i--) {
                temp = attributes[i];
                if (temp == attribute) {
                    attributes.RemoveAt(i);
                    return attribute;
                }
            }
            return null;
        }

        #endregion

        #region RemoveAttributes

        /// <summary>
        /// Removes every <see cref="ItemAttribute"/> of type <typeparamref name="T"/>.
        /// </summary>
        public IEnumerator<T> RemoveAttributes<T>() where T : ItemAttribute, new() {
            ItemAttribute attribute;
            for (int i = attributes.Count - 1; i >= 0; i--) {
                attribute = attributes[i];
                if (attribute is T t) {
                    attributes.RemoveAt(i);
                    yield return t;
                }
            }
        }

        #endregion

        #region GetTag

        /// <summary>
        /// Gets a tag by <paramref name="index"/>.
        /// </summary>
        /// <seealso cref="TagCount"/>
        /// <seealso cref="HasTag(in string)"/>
        /// <seealso cref="IndexOfTag(in string)"/>
        public string GetTag(in int index) {
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            ItemData data = ItemData.GetItem(id);
            string[] tags = data.tags;
            int tagCount = tags.Length;
            if (index >= tagCount) throw new ArgumentOutOfRangeException(nameof(index));
            return tags[index];
        }

        #endregion

        #region HasTag

        /// <returns>
        /// Returns <c>true</c> if the <see cref="Item"/> contains the <paramref name="tag"/>.
        /// </returns>
        /// <seealso cref="HasAnyTag(in string[])"/>
        /// <seealso cref="GetTag(in int)"/>
        /// <seealso cref="IndexOfTag(in string)"/>
        public bool HasTag(in string tag) {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            ItemData data = ItemData.GetItem(id);
            string[] tags = data.tags;
            int tagCount = tags.Length;
            for (int i = tagCount - 1; i >= 0; i--) {
                if (tag.Equals(tags[i])) return true;
            }
            return false;
        }

        #endregion

        #region HasAnyTag

        /// <returns>
        /// Returns <c>true</c> if the <see cref="Item"/> has any of the specified <paramref name="tags"/>.
        /// </returns>
        /// <seealso cref="HasTag(in string)"/>
        public bool HasAnyTag(in string[] tags) {
            if (tags == null) throw new ArgumentNullException(nameof(tags));
            int tagCount = tags.Length;
            if (tagCount == 0) return false;
            ItemData data = ItemData.GetItem(id);
            string[] itemTags = data.tags;
            int itemTagCount = itemTags.Length;
            string currentTag;
            for (int i = tagCount - 1; i >= 0; i--) {
                currentTag = tags[i];
                if (currentTag == null) continue;
                for (int j = itemTagCount - 1; j >= 0; j--) {
                    if (currentTag.Equals(itemTags[j])) return true;
                }
            }
            return false;
        }

        #endregion

        #region IndexOfTag

        /// <returns>
        /// Returns the index of the <paramref name="tag"/>; if the <paramref name="tag"/> was not found, <c>-1</c> is returned.
        /// </returns>
        /// <seealso cref="GetTag(in int)"/>
        /// <seealso cref="HasTag(in string)"/>
        public int IndexOfTag(in string tag) {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            ItemData data = ItemData.GetItem(id);
            string[] tags = data.tags;
            int tagCount = tags.Length;
            for (int i = tagCount - 1; i >= 0; i--) {
                if (tag.Equals(tags[i])) return i;
            }
            return -1;
        }

        #endregion

        #endregion

    }

}