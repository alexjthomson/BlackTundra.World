#if USE_XR_TOOLKIT

using BlackTundra.World.XR;
using BlackTundra.World.XR.Experimental.Tracking;

using UnityEditor;

using UnityEngine;

namespace BlackTundra.World.Editor.XR {

    public static class XRObjectCreator {

        [MenuItem("GameObject/Black Tundra/World/XR/XR Rig (Player)")]
        private static void CreatePlayer() {
            GameObject rigGameObject = new GameObject(
                "XRRig",
                /*
                 * The XRLocomotionController is an extension of the CharacterController which is used to control the XR rig.
                 */
                typeof(XRLocomotionController),
                /*
                 * The XRTrackingController controls each trackable XR object. By default, the players head and hands are the only things added
                 * to this tracker.
                 */
                typeof(XRTrackingController)
            );
        }

        [MenuItem("GameObject/Black Tundra/World/XR/Snap Point")]
        private static void CreateItemSnapPoint() {
            GameObject activeGameObject = Selection.activeGameObject;
            GameObject snapPointGameObject = new GameObject(nameof(XRItemSnapPoint), typeof(SphereCollider), typeof(XRItemSnapPoint)) {
                isStatic = false
            };
            if (activeGameObject != null) {
                Transform parent = activeGameObject.transform;
                Transform snapPointTransform = snapPointGameObject.transform;
                snapPointTransform.parent = parent;
                snapPointTransform.SetPositionAndRotation(
                    parent.position,
                    parent.rotation
                );
            }
            XRItemSnapPoint snapPoint = snapPointGameObject.GetComponent<XRItemSnapPoint>();
            snapPoint.attachTransform = snapPointGameObject.transform;
            SphereCollider sphereCollider = snapPointGameObject.GetComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            sphereCollider.radius = 0.1f;
            Selection.activeGameObject = snapPointGameObject;
            Undo.RegisterCreatedObjectUndo(snapPointGameObject, $"{nameof(XRItemSnapPoint)} created");
        }

    }

}

#endif