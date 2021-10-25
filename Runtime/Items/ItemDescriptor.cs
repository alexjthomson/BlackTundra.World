using BlackTundra.Foundation.Serialization;

using System.Collections.Generic;

using UnityEngine;

namespace BlackTundra.World.Items {

    /// <summary>
    /// Describes an <see cref="ItemData"/> entry.
    /// </summary>
#if UNITY_EDITOR
    [CreateAssetMenu(menuName = "Items/Item Descriptor")]
#endif
    public sealed class ItemDescriptor : ScriptableObject {

        #region variable

        /// <summary>
        /// Internal order used to order this item correctly when building the item database.
        /// </summary>
        [SerializeField]
        [HideInInspector]
        internal int order = -1;

        /// <inheritdoc cref="ItemData.name"/>
        [SerializeField]
#if UNITY_EDITOR
        new
#endif
        internal string name = string.Empty;

        /// <inheritdoc cref="ItemData.description"/>
        [SerializeField]
        internal string description = string.Empty;

        /// <inheritdoc cref="ItemData.width"/>
        [SerializeField]
        internal int width = 1;

        /// <inheritdoc cref="ItemData.height"/>
        [SerializeField]
        internal int height = 1;

        /// <summary>
        /// Set of asset GUIDs (value) associated with a resource name (key).
        /// </summary>
        [SerializeField]
        internal SerializableDictionary<string, string> resources;

        #endregion

    }

}