using System;

using UnityEngine;

using Random = UnityEngine.Random;

namespace BlackTundra.World.Drawing {

    /// <summary>
    /// Manages a surface that can be drawn on.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DrawSurface : MonoBehaviour {

        #region variable

        /// <summary>
        /// <see cref="Renderer"/> component that the drawing should be applied to.
        /// </summary>
        [SerializeField]
#if UNITY_EDITOR
        new
#endif
        private Renderer renderer = null;

        /// <summary>
        /// Material IDs to apply the drawing to.
        /// </summary>
        [SerializeField]
        private int[] materialIds = new int[] { 0 };

        /// <summary>
        /// Size of the drawing surface texture.
        /// </summary>
        [SerializeField]
        private Vector2Int textureSize = new Vector2Int(1024, 1024);

        /// <summary>
        /// Texture to apply to the drawing surface.
        /// </summary>
        private Texture2D texture = null;

        #endregion

        #region property

        #endregion

        #region logic

        #region Awake

        private void Awake() {
            ResetTexture();
            AssignMaterials();
        }

        #endregion

        #region ResetTexture

        /// <summary>
        /// Resets the texture that is drawn to.
        /// </summary>
        public void ResetTexture() {
            if (texture != null) {
                Destroy(texture);
                texture = null;
            }
            texture = new Texture2D(textureSize.x, textureSize.y, TextureFormat.ARGB32, false, true) {
                name = $"{gameObject.name}_{nameof(DrawSurface)}Instance"
            };
        }

        #endregion

        #region AssignMaterials

        /// <summary>
        /// Overrides the current materials on the <see cref="renderer"/> with instanced clones of the original materials with the main texture overridden.
        /// </summary>
        private void AssignMaterials() {
            Material[] materials = renderer.materials;
            Material material;
            int materialId;
            for (int i = materialIds.Length - 1; i >= 0; i--) {
                materialId = materialIds[i];
                material = Instantiate(materials[materialId]); // clone material
                material.mainTexture = texture;
                //material.SetTexture("_MainTex", texture);
                materials[materialId] = material;
            }
            renderer.materials = materials;
        }

        #endregion

        #region DrawAt

        public void DrawAt(in Vector2 textureCoordinate, in float radius, in Gradient gradient) {
            if (radius < 0.0f) throw new ArgumentOutOfRangeException(nameof(radius));
            // texture size:
            int textureSizeX = textureSize.x;
            int textureSizeY = textureSize.y;
            // calculate uv coordinates:
            float uvx = Mathf.Clamp01(textureCoordinate.x);
            float uvy = Mathf.Clamp01(textureCoordinate.y);
            // calculate centre of circle on texture:
            Vector2 centre = new Vector2(
                uvx * textureSizeX,
                uvy * textureSizeY
            );
            // calculate lower left circle coordinate:
            Vector2 lowerLeft = new Vector2(
                centre.x - radius,
                centre.y - radius
            );
            // calculate diameter:
            float diameter = radius * 2.0f;
            // calculate circle bounds:
            int lowerX = Mathf.Max(Mathf.FloorToInt(lowerLeft.x), 0);
            int lowerY = Mathf.Max(Mathf.FloorToInt(lowerLeft.y), 0);
            int upperX = Mathf.Min(Mathf.CeilToInt(lowerLeft.x + diameter), textureSizeX);
            int upperY = Mathf.Min(Mathf.CeilToInt(lowerLeft.y + diameter), textureSizeY);
            // draw circle:
            float antiAliasRadius = Mathf.Max(radius - 0.5f, 0.0f);
            float sqrAntiAliasRadius = antiAliasRadius * antiAliasRadius;
            float dx, dy; // distance from centre of circle in each axis
            float distance; // distance from centre of circle
            float alpha; // relative alpha coefficient to apply to the colour at that point
            Color pixelColor; // pixel color
            Color texPixelColor; // pixel color of the texture at the current point
            for (int y = lowerY; y < upperY; y++) { // iterate circle y bounds
                for (int x = lowerX; x < upperX; x++) { // iterate circle x bounds
                    dx = x - centre.x;
                    dy = y - centre.y;
                    distance = dx * dx + dy * dy;
                    if (distance < sqrAntiAliasRadius) { // within non-antialiased circle
                        pixelColor = gradient.Evaluate(Random.value);
                    } else {
                        distance = Mathf.Sqrt(distance);
                        alpha = 1.0f - (distance - antiAliasRadius);
                        if (alpha < 0.0f) continue;
                        pixelColor = gradient.Evaluate(Random.value);
                        pixelColor.a *= alpha;
                    }
                    if (Mathf.Approximately(pixelColor.a, 1.0f)) { // the pixel being drawn has an alpha value of 1.0, no blending is required
                        pixelColor.a = 1.0f;
                        texture.SetPixel(x, y, pixelColor);
                    } else { // the pixel being drawn has an alpha value less than 1.0, therefore it must be blended with what has already been drawn on the texture
                        // get the texture pixel color:
                        texPixelColor = texture.GetPixel(x, y);
                        // calculate draw pixel alpha values:
                        alpha = pixelColor.a;
                        float inverseAlpha = 1.0f - alpha;
                        // calculate texture pixel alpha values:
                        float texAlpha = texPixelColor.a;
                        float inverseTexAlpha = 1.0f / texAlpha;
                        float relativeTexAlpha = inverseAlpha * texAlpha;
                        // composit two colors together:
                        Color finalPixelColor = new Color(
                            ((relativeTexAlpha * texPixelColor.r) + (alpha * pixelColor.r)) * inverseTexAlpha,
                            ((relativeTexAlpha * texPixelColor.g) + (alpha * pixelColor.g)) * inverseTexAlpha,
                            ((relativeTexAlpha * texPixelColor.b) + (alpha * pixelColor.b)) * inverseTexAlpha,
                            relativeTexAlpha + alpha
                        );
                        // draw color to pixel
                        texture.SetPixel(x, y, finalPixelColor);
                    }
                }
            }
            texture.Apply(); // apply modifications to the texture
        }

        #endregion

        #endregion

    }

}