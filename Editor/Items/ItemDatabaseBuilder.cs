using BlackTundra.Foundation;
using BlackTundra.Foundation.Editor.Utility;
using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Serialization;
using BlackTundra.World.Items;

using System;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;

using Console = BlackTundra.Foundation.Console;
using Object = UnityEngine.Object;

namespace BlackTundra.World.Editor.Items {

    /// <summary>
    /// Responsible for building the item database.
    /// </summary>
    public static class ItemDatabaseBuilder {

        #region logic

        #region BuildItemDatabase
#if UNITY_EDITOR

        /// <summary>
        /// Builds the item database.
        /// </summary>
        [MenuItem("Tools/Item/Rebuild Database")]
        internal static void BuildItemDatabase() {
            // find item descriptors:
            string[] guids = AssetDatabase.FindAssets(string.Concat("t:", typeof(ItemDescriptor)));
            ItemDescriptor descriptor;
            List<ItemDescriptor> descriptors = new List<ItemDescriptor>();
            for (int i = guids.Length - 1; i >= 0; i--) {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                descriptor = AssetDatabase.LoadAssetAtPath<ItemDescriptor>(assetPath);
                if (descriptor == null) {
                    Console.Fatal($"Failed to load item descriptor at \"{assetPath}\" (guid: \"{guids[i]}\").");
                    Console.Fatal($"Failed build item database.");
                    return;
                }
                descriptors.Add(descriptor);
            }
            // order descriptors:
            descriptors.Sort((d0, d1) => d0.order.CompareTo(d1.order));
            // bake orders and resources:
            SerializableDictionary<string, Object> resources = new SerializableDictionary<string, Object>();
            int descriptorCount = descriptors.Count;
            for (int i = descriptorCount - 1; i >= 0; i--) {
                descriptor = descriptors[i];
                if (descriptor.order != i) {
                    descriptor.order = i;
                    CustomInspector.MarkAsDirty(descriptor);
                }
                if (descriptor.resources != null) {
                    foreach (string guid in descriptor.resources.Values) { // iterate each resource referenced by guid
                        if (guid != null && !resources.ContainsKey(guid)) { // valid guid and not yet cached
                            string assetPath = AssetDatabase.GUIDToAssetPath(guid); // get path to referenced object
                            if (assetPath != null) { // path exists
                                Object asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object)); // load the asset
                                if (asset != null) { // asset exists
                                    resources.Add(guid, asset); // add the asset
                                }
                            }
                        }
                    }
                } else {
                    descriptor.resources = new SerializableDictionary<string, string>();
                    CustomInspector.MarkAsDirty(descriptor);
                }
            }
            // save resources:
            ResourceReference<ItemResources> itemResourcesReference = new ResourceReference<ItemResources>(ItemResources.ResourcePath);
            ItemResources itemResources = itemResourcesReference.Value;
            if (itemResources == null) {
                itemResources = ScriptableObject.CreateInstance<ItemResources>();
                itemResources.resources = resources;
                if (!AssetDatabase.IsValidFolder("Assets/Resources/Settings")) {
                    if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
                    AssetDatabase.CreateFolder("Assets/Resources", "Settings");
                }
                AssetDatabase.CreateAsset(itemResources, "Assets/Resources/Settings/ItemResources.asset");
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = itemResources;
                Console.Info($"Created \"{nameof(ItemResources)}\" at \"Resources/Settings/ItemResources.asset\".");
            } else {
                itemResources.resources = resources;
                CustomInspector.MarkAsDirty(itemResources);
            }
            // bake database:
            SerializedByteArrayBuilder database = new SerializedByteArrayBuilder();
            database.WriteNext(descriptorCount);
            for (int i = 0; i < descriptorCount; i++) {
                descriptor = descriptors[i];
                SerializableDictionary<string, string> dictionary = descriptor.resources;
                int dictionaryCount = dictionary.Count;
                if (dictionaryCount >= byte.MaxValue) throw new NotSupportedException();
                database.WriteNext(i)
                    .WriteNext(descriptor.name)
                    .WriteNext(descriptor.description)
                    .WriteNext((byte)descriptor.width)
                    .WriteNext((byte)descriptor.height)
                    .WriteNext((byte)dictionaryCount);
                foreach (KeyValuePair<string, string> kvp in dictionary) {
                    database.WriteNext(kvp.Key).WriteNext(kvp.Value);
                }
            }
            // write database to system:
            if (FileSystem.Write(ItemData.DatabaseFSR, database.ToBytes(), ItemData.DatabaseFormat, false)) {
                Console.Info($"Build item database to \"{ItemData.DatabaseFSR.AbsolutePath}\".");
            } else {
                Console.Fatal($"Failed to write item database to system at \"{ItemData.DatabaseFSR.AbsolutePath}\".");
            }
        }

#endif
        #endregion

        #endregion

    }

}