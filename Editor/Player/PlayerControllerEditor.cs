using BlackTundra.Foundation.Editor.Utility;
using BlackTundra.World.Player;

using UnityEditor;

using UnityEngine;

namespace BlackTundra.World.Editor.Player {

    [CustomEditor(typeof(PlayerController))]
    public sealed class PlayerControllerEditor : CustomInspector {

        #region variable

        private PlayerController controller = null;

        private CapsuleCollider collider = null;

        private CharacterController character = null;

        #endregion

        #region logic

        #region OnEnable

        private void OnEnable() {
            bool dirty = false;
            controller = (PlayerController)target;
            collider = controller.GetComponent<CapsuleCollider>();
            if (collider == null) {
                dirty = true;
                collider = controller.gameObject.AddComponent<CapsuleCollider>();
            }
            collider.hideFlags = HideFlags.HideInInspector;
            character = controller.GetComponent<CharacterController>();
            if (character == null) {
                dirty = true;
                character = controller.gameObject.AddComponent<CharacterController>();
            }
            character.hideFlags = HideFlags.HideInInspector;
            float height = collider.height;
            float radius = collider.radius;
            float skinWidth = character.skinWidth;
            Vector3 center = new Vector3(0.0f, height * 0.5f, 0.0f);
            collider.center = center;
            character.height = height - (skinWidth * 2.0f);
            character.center = center;
            character.radius = radius - skinWidth;
            if (dirty) MarkAllAsDirty();
        }

        #endregion

        #region DrawInspector

        protected override void DrawInspector() {

            float nf;

            float mass = controller._mass;
            float groundedVelocity = -controller.groundedVelocity.y;
            float gravity = controller._gravity;
            float drag = controller._drag;

            nf = Mathf.Max(EditorLayout.FloatField("Mass", mass), 0.1f);
            if (nf != mass) {
                mass = nf;
                controller._mass = mass;
                MarkAsDirty(controller);
            }

            nf = EditorLayout.FloatField("Gravity", gravity);
            if (nf != gravity) {
                gravity = nf;
                controller._gravity = gravity;
                MarkAsDirty(controller);
            }

            nf = Mathf.Clamp01(EditorLayout.FloatField("Drag", drag));
            if (nf != drag) {
                drag = nf;
                controller._drag = drag;
                MarkAsDirty(controller);
            }

            nf = Mathf.Max(EditorLayout.FloatField("Ground Stick Velocity", groundedVelocity), 0.0f);
            if (nf != groundedVelocity) {
                groundedVelocity = nf;
                controller.groundedVelocity = new Vector3(0.0f, -groundedVelocity, 0.0f);
                MarkAsDirty(controller);
            }

            EditorLayout.Space();

            float height = collider.height;
            float radius = collider.radius;
            float skinWidth = character.skinWidth;

            nf = Mathf.Max(EditorLayout.FloatField("Height", height), skinWidth * 2.0f, radius * 2.0f);
            if (nf != height) {
                height = nf;
                Vector3 center = new Vector3(0.0f, height * 0.5f, 0.0f);
                collider.height = height;
                collider.center = center;
                float characterHeight = height - (skinWidth * 2.0f);
                character.height = characterHeight;
                character.center = center;
                MarkAllAsDirty();
            }

            nf = Mathf.Max(EditorLayout.FloatField("Radius", radius), skinWidth);
            if (nf != radius) {
                radius = nf;
                collider.radius = radius;
                character.radius = radius - skinWidth;
                MarkAllAsDirty();
            }

            nf = Mathf.Max(EditorLayout.FloatField("Skin Width", skinWidth), 0.0f);
            if (nf != skinWidth) {
                skinWidth = nf;
                character.skinWidth = skinWidth;
                character.height = height - (skinWidth * 2.0f);
                //character.center = new Vector3(0.0f, height * 0.5f, 0.0f);
                character.radius = radius - skinWidth;
                MarkAllAsDirty();
            }

            EditorLayout.Space();

            float stepHeight = character.stepOffset;
            float slopeLimit = character.slopeLimit;
            float frictionCoefficient = controller.frictionCoefficient;
            float slideCoefficient = controller.slideCoefficient;

            nf = Mathf.Max(EditorLayout.FloatField("Max Step Height", stepHeight), 0.0f);
            if (nf != stepHeight) {
                stepHeight = nf;
                character.stepOffset = stepHeight;
                MarkAsDirty(character);
            }

            nf = Mathf.Clamp(EditorLayout.FloatField("Slope Limit (deg)", slopeLimit), 0.0f, 90.0f);
            if (nf != slopeLimit) {
                slopeLimit = nf;
                character.slopeLimit = slopeLimit;
                MarkAsDirty(character);
            }

            nf = Mathf.Clamp01(EditorLayout.FloatField("Friction Coefficient", frictionCoefficient));
            if (nf != frictionCoefficient) {
                frictionCoefficient = nf;
                controller.frictionCoefficient = frictionCoefficient;
                MarkAsDirty(controller);
            }

            nf = Mathf.Max(EditorLayout.FloatField("Slide Coefficient", slideCoefficient));
            if (nf != slideCoefficient) {
                slideCoefficient = nf;
                controller.slideCoefficient = slideCoefficient;
                MarkAsDirty(controller);
            }

        }

        #endregion

        #region MarkAllAsDirty

        private void MarkAllAsDirty() {
            MarkAsDirty(controller);
            MarkAsDirty(character);
            MarkAsDirty(collider);
        }

        #endregion

        #endregion

    }

}