using BlackTundra.Foundation.Editor.Utility;
using BlackTundra.Foundation.Serialization;
using BlackTundra.Foundation.Utility;
using BlackTundra.World.Items;

using System.Collections.Generic;

using UnityEditor;

using UnityEngine;

namespace BlackTundra.World.Editor.Items {

    [CustomEditor(typeof(ItemDescriptor))]
    public sealed class ItemDescriptorEditor : CustomInspector {

        #region variable

        private Dictionary<string, Object> resources = null;

        #endregion

        #region logic

        #region DrawInspector

        protected override void DrawInspector() {

            ItemDescriptor item = (ItemDescriptor)target;
            string os, ns;

            // order:
            EditorLayout.Info($"Order: {item.order}");

            // name:
            os = item.name;
            ns = EditorLayout.TextField("Name", os);
            if (ns != os) {
                item.name = ns;
                MarkAsDirty(item);
            }

            // description:
            os = item.description;
            ns = EditorLayout.TextAreaField("Description", os);
            if (ns != os) {
                item.description = ns;
                MarkAsDirty(item);
            }

            // scale:
            Vector2Int scale = EditorLayout.Vector2IntField("Scale", item.width, item.height);
            int width = Mathf.Clamp(scale.x, 0, ItemData.MaxLength), height = Mathf.Clamp(scale.y, 0, ItemData.MaxLength);
            if (item.width != width) {
                item.width = width;
                MarkAsDirty(item);
            }
            if (item.height != height) {
                item.height = height;
                MarkAsDirty(item);
            }

            // resources:
            if (item.resources == null) {
                item.resources = new SerializableDictionary<string, string>();
                MarkAsDirty(item);
            }
            if (resources == null || resources.Count != item.resources.Count) {
                resources = new Dictionary<string, Object>();
                foreach (KeyValuePair<string, string> kvp in item.resources) {
                    string guid = kvp.Value;
                    Object resource = null;
                    if (guid != null) {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        if (assetPath != null) {
                            resource = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));
                        }
                    }
                    resources.Add(kvp.Key, resource);
                }
            }
            EditorLayout.Title("Resources");
            EditorLayout.StartVerticalBox();
            bool resourcesModified = false;
            foreach (KeyValuePair<string, Object> resource in resources) {
                EditorLayout.StartHorizontal();
                string key = EditorLayout.TextField(resource.Key);
                if (key != resource.Key) resourcesModified = true;
                Object reference = EditorLayout.ReferenceField(resource.Value);
                if (reference != resource.Value) resourcesModified = true;
                if (EditorLayout.XButton()) {
                    resourcesModified = true;
                    item.resources.Remove(resource.Key);
                    EditorLayout.EndHorizontal();
                    break;
                }
                EditorLayout.EndHorizontal();
                if (resourcesModified) {
                    if (reference == null) {
                        item.resources.Remove(resource.Key);
                        item.resources.Add(key, null);
                    } else if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(reference.GetInstanceID(), out string guid, out long localId)) {
                        item.resources.Remove(resource.Key);
                        item.resources.Add(key, guid);
                    } else {
                        Debug.LogError("Failed to generate resource references.");
                    }
                    break;
                }
            }
            if (resourcesModified) {
                resources = null; // rebuild
                MarkAsDirty(item);
            }
            if (EditorLayout.Button("Add Resource")) {
                item.resources.Add("New Resource", null);
                MarkAsDirty(item);
            }
            EditorLayout.EndVerticalBox();

            // tags:
            EditorLayout.Title("Tags");
            EditorLayout.StartVerticalBox();
            string[] tags = item.tags;
            bool tagsModified = false;
            if (tags == null) {
                tags = new string[0];
                tagsModified = true;
            }
            for (int i = 0; i < tags.Length; i++) {
                EditorLayout.StartHorizontal();
                string currentTag = tags[i];
                string newTag = EditorLayout.TextField(currentTag);
                if (newTag != currentTag) {
                    tagsModified = true;
                    tags[i] = newTag;
                }
                if (EditorLayout.UpButton()) {
                    tags.Swap(i, i - 1);
                    tagsModified = true;
                }
                if (EditorLayout.DownButton()) {
                    tags.Swap(i, i + 1);
                    tagsModified = true;
                }
                if (EditorLayout.XButton()) {
                    tags = tags.RemoveAt(i);
                    tagsModified = true;
                    break;
                }
                EditorLayout.EndHorizontal();
            }
            if (EditorLayout.Button("Add Tag")) {
                tags = tags.AddLast("New Tag");
                tagsModified = true;
            }
            if (tagsModified) {
                item.tags = tags;
                MarkAsDirty(item);
            }
            EditorLayout.EndVerticalBox();

            // finalize:
            if (EditorLayout.Button("Build Item Database")) ItemDatabaseBuilder.BuildItemDatabase();

        }

        #endregion

        #endregion

    }

}