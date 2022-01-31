#if USE_XR_TOOLKIT

using BlackTundra.Foundation.Editor.Utility;
using BlackTundra.World.XR;

using UnityEditor;

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace BlackTundra.World.Editor.XR {

    [CustomEditor(typeof(ActionBasedXRHandController))]
    public sealed class ActionBasedXRHandControllerEditor : CustomInspector {

        protected sealed override void DrawInspector() {
            ActionBasedXRHandController handController = (ActionBasedXRHandController)target;
            // component check:
            ActionBasedController actionBasedController = handController.GetComponent<ActionBasedController>();
            if (actionBasedController == null) {
                EditorLayout.Error($"Expected `{nameof(ActionBasedController)}` component.");
            } else {
                // reference checks:
                if (CheckHandVariant(actionBasedController.modelPrefab.gameObject, false, "Non-Physics Model Prefab")) {
                    CheckHandVariant(handController.physicsModelPrefab, true, "Physics Model Prefab");
                }
            }
            // draw default inspector:
            DrawDefaultInspector();
        }

        private bool CheckHandVariant(in GameObject prefab, in bool rigidbody, in string objName) {
            // check prefab exists:
            if (prefab == null) {
                EditorLayout.Error($"{objName} reference missing.");
                return false;
            }
            // check animator:
            Animator animator = prefab.GetComponent<Animator>();
            if (animator == null) {
                EditorLayout.Error($"{objName} Animator component missing.");
                return false;
            }
            // check rigidbody:
            if (rigidbody) {
                Rigidbody rb = prefab.GetComponent<Rigidbody>();
                if (rb == null) {
                    EditorLayout.Error($"{objName} has no Rigidbody component.");
                    return false;
                }
            }
            return true;
        }

    }

}

#endif