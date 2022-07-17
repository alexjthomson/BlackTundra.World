using UnityEditor;

using UnityEngine;

namespace BlackTundra.World.Editor {

    static class VolumeMenuItems {

        [MenuItem("GameObject/Black Tundra/World/Volume/Global Volume")]
        private static void CreateGlobalVolume(MenuCommand command) {
            GameObject gameObject = new GameObject("GlobalVolume", typeof(Volume));
            GameObjectUtility.SetParentAndAlign(gameObject, command.context as GameObject);
            Volume volume = gameObject.GetComponent<Volume>();
            volume.global = true;
            Selection.activeObject = gameObject;
        }

        [MenuItem("GameObject/Black Tundra/World/Volume/Box Volume")]
        private static void CreateBoxVolume(MenuCommand command) {
            GameObject gameObject = new GameObject("GlobalVolume", typeof(Volume), typeof(BoxCollider));
            GameObjectUtility.SetParentAndAlign(gameObject, command.context as GameObject);
            BoxCollider collider = gameObject.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            Volume volume = gameObject.GetComponent<Volume>();
            volume.global = false;
            Selection.activeObject = gameObject;
        }

        [MenuItem("GameObject/Black Tundra/World/Volume/Sphere Volume")]
        private static void CreateSphereVolume(MenuCommand command) {
            GameObject gameObject = new GameObject("GlobalVolume", typeof(Volume), typeof(SphereCollider));
            GameObjectUtility.SetParentAndAlign(gameObject, command.context as GameObject);
            SphereCollider collider = gameObject.GetComponent<SphereCollider>();
            collider.isTrigger = true;
            Volume volume = gameObject.GetComponent<Volume>();
            volume.global = false;
            Selection.activeObject = gameObject;
        }

    }

}