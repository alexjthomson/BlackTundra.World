using UnityEngine;

namespace BlackTundra.World.Drawing {

    /// <summary>
    /// Casts a ray towards <see cref="DrawSurface"/> components and allows them to be drawn on.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DrawRayCaster : MonoBehaviour {

        #region variable

        [SerializeField]
        private Gradient gradient = new Gradient();

        [SerializeField]
        private Transform caster = null;

        [SerializeField]
        private float range = 0.1f;

        [SerializeField]
        private LayerMask layerMask = -1;

        [SerializeField]
        private float radius = 1.5f;

        #endregion

        #region logic

        #region OnEnable

        private void OnEnable() {
            if (caster == null) caster = transform;
        }

        #endregion

        #region Update

        private void Update() {
            if (Physics.Raycast(caster.position, caster.forward, out RaycastHit hit, range, layerMask, QueryTriggerInteraction.Ignore)) {
                Collider collider = hit.collider;
                DrawSurface drawSurface = collider.GetComponent<DrawSurface>();
                if (drawSurface != null) {
                    Vector2 textureCoordinate = hit.textureCoord;
                    drawSurface.DrawAt(textureCoordinate, radius, gradient);
                }
            }
        }

        #endregion

        #endregion

    }

}