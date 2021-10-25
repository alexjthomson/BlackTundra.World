using BlackTundra.Foundation.Editor.Utility;
using BlackTundra.Foundation.Utility;

using System;

using UnityEditor;

using UnityEngine;

using Random = UnityEngine.Random;

namespace BlackTundra.World.Editor {

    [CustomEditor(typeof(Volume))]
    public sealed class VolumeEditor : CustomInspector {

        bool tagFoldout = true;

        protected sealed override void DrawInspector() {
            Volume volume = (Volume)target;
            bool global = volume.global;
            if (EditorLayout.BooleanField("Is Global", global) != global) {
                global = !global;
                volume.global = global;
                MarkAsDirty(volume);
            }
            if (!global) {
                float blendDistance = volume.blendDistance;
                float newBlendDistance = Mathf.Max(EditorLayout.FloatField("Blend Distance", blendDistance), 0.0f);
                if (blendDistance != newBlendDistance) {
                    volume.blendDistance = newBlendDistance;
                    MarkAsDirty(volume);
                }
                float weight = volume.weight;
                float newWeight = Mathf.Clamp01(EditorLayout.FloatField("Weight", weight));
                if (weight != newWeight) {
                    volume.weight = newWeight;
                    MarkAsDirty(volume);
                }
            }
            tagFoldout = EditorLayout.Foldout("Tags", tagFoldout);
            if (tagFoldout) {
                EditorLayout.StartVerticalBox();
                string[] tags = volume.tags;
                bool modified = false;
                string tag;
                for (int i = tags.Length - 1; i >= 0; i--) {
                    tag = tags[i];
                    EditorLayout.StartHorizontal();
                    string newTag = EditorLayout.TextField(tag);
                    if (tag != newTag) {
                        tags[i] = newTag;
                        modified = true;
                    }
                    if (EditorLayout.UpButton()) {
                        tags.Swap(i, i + 1);
                        modified = true;
                        break;
                    }
                    if (EditorLayout.DownButton()) {
                        tags.Swap(i, i - 1);
                        modified = true;
                        break;
                    }
                    if (EditorLayout.XButton()) {
                        tags = tags.RemoveAt(i);
                        modified = true;
                        continue;
                    }
                    EditorLayout.EndHorizontal();
                }
                if (EditorLayout.Button("Add Tag")) {
                    tags = tags.AddLast("New Tag " + Random.Range(int.MinValue, int.MaxValue).ToHex());
                    modified = true;
                }
                if (modified) {
                    volume.tags = tags;
                    MarkAsDirty(volume);
                }
                EditorLayout.EndVerticalBox();
            }
        }

    }

}