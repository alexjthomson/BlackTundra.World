using BlackTundra.Foundation;
using BlackTundra.Foundation.Serialization;

using System.Collections.Generic;

using UnityEngine;

using Object = UnityEngine.Object;

namespace BlackTundra.World.Items {

    /// <summary>
    /// Responsible for storing references to resources that items may use.
    /// </summary>
    public sealed class ItemResources : ScriptableObject {

        #region constant

        /// <summary>
        /// Resource location of the <see cref="ItemResources"/>.
        /// </summary>
        internal const string ResourcePath = "Settings/ItemResources";

        #endregion

        #region variable

        /// <summary>
        /// <see cref="Dictionary{TKey, TValue}"/> linking a resource path to a reference to the resource.
        /// </summary>
        [SerializeField]
        internal SerializableDictionary<string, Object> resources = new SerializableDictionary<string, Object>();

        /// <summary>
        /// Singleton instance of <see cref="ItemResources"/>.
        /// </summary>
        private static ItemResources instance = null;

        #endregion

        #region logic

        #region Initialise

        [CoreInitialise(int.MinValue)]
        private static void Initialise() {
            if (instance == null) {
                ResourceReference<ItemResources> resource = new ResourceReference<ItemResources>(ResourcePath);
                instance = resource.Value;
                if (instance == null) instance = new ItemResources();
            }
        }

        #endregion

        #region GetResource

        internal static Object GetResource(in string guid) {
            if (guid == null) return null;
            if (instance.resources.TryGetValue(guid, out Object resource)) return resource;
            else throw new KeyNotFoundException(guid);
        }

        #endregion

        #endregion

    }

}