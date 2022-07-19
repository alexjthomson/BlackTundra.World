using BlackTundra.Foundation.Editor.Utility;

using UnityEditor;

using UnityEngine;

namespace BlackTundra.World.Editor {

    [CustomEditor(typeof(PhysicsCharacterController))]
    public class PhysicsCharacterControllerEditor : CustomInspector {

        PhysicsCharacterController controller = null;
        GameObject gameObject = null;

        private void OnDisable() {
            CharacterController characterController = gameObject.GetComponent<CharacterController>();
            if (target == null && characterController != null) {
                DestroyImmediate(characterController);
            }
        }

        protected override void DrawInspector() {
            controller = (PhysicsCharacterController)target;
            gameObject = controller.gameObject;
            if (controller._characterController == null) {
                controller.OnEnable();
            }
            EditorLayout.Title("Physics");
            // mass:
            float mass = controller.mass;
            float newMass = EditorLayout.FloatField("Mass", mass);
            if (mass != newMass) {
                controller.mass = newMass;
                MarkAsDirty();
            }
            // centre of mass:
            Vector3 centreOfMass = controller.centreOfMass;
            Vector3 newCentreOfMass = EditorLayout.Vector3Field("Centre of Mass", centreOfMass);
            if (centreOfMass != newCentreOfMass) {
                controller.centreOfMass = newCentreOfMass;
                MarkAsDirty();
            }
            // drag:
            float dragCoefficient = controller.dragCoefficient;
            float newDragCoefficient = EditorLayout.FloatField("Drag Coefficient", dragCoefficient);
            if (dragCoefficient != newDragCoefficient) {
                controller.dragCoefficient = newDragCoefficient;
                MarkAsDirty();
            }
            // solid layer mask:
            LayerMask solidLayerMask = controller.solidLayerMask;
            LayerMask newSolidLayerMask = EditorLayout.LayerMaskField("Layer Mask", solidLayerMask);
            if (solidLayerMask != newSolidLayerMask) {
                controller.solidLayerMask = newSolidLayerMask;
                MarkAsDirty();
            }
            // simulation flags:
            PhysicsCharacterControllerFlags flags = controller.flags;
            PhysicsCharacterControllerFlags newFlags = EditorLayout.EnumFlagsField("Simulation Flags", flags);
            if (flags != newFlags) {
                controller.flags = newFlags;
                MarkAsDirty();
            }
            // dimensions:
            EditorLayout.Space(); 
            EditorLayout.Title("Dimensions");
            // height:
            float height = controller.height;
            float newHeight = EditorLayout.FloatField("Height", height);
            if (height != newHeight) {
                controller.height = newHeight;
                MarkAsDirty();
            }
            // radius:
            float radius = controller.radius;
            float newRadius = EditorLayout.FloatField("Radius", radius);
            if (radius != newRadius) {
                controller.radius = radius;
                MarkAsDirty();
            }
            // events:
            EditorLayout.Space();
            EditorLayout.Title("Events");
            DrawDefaultInspector();
        }

        public sealed override void MarkAsDirty() {
            base.MarkAsDirty();
            CharacterController characterController = gameObject.GetComponent<CharacterController>();
            if (characterController != null) {
                MarkAsDirty(characterController);
            }
        }

    }

}