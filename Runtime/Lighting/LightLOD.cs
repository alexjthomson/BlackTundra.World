#define SET_SHADOW_RESOLUTION

using BlackTundra.Foundation;
using BlackTundra.World.CameraSystem;

using System.Collections.Generic;

using UnityEngine;

namespace BlackTundra.World.Lighting {

    [DisallowMultipleComponent]
    [AddComponentMenu("Lighting/Light LOD")]
    public sealed class LightLOD : MonoBehaviour {

        #region constant

        /// <summary>
        /// Contains a reference to every active <see cref="LightLOD"/>.
        /// </summary>
        private static readonly List<LightLOD> LightLODList = new List<LightLOD>();

        private static int UpdateSkipCount = 32;

        #endregion

        #region variable

        /// <summary>
        /// Distance from the camera before this light is at minimum quality.
        /// </summary>
#if UNITY_EDITOR
        [Min(0.1f)]
#endif
        [SerializeField]
        private float distanceScale = 100.0f;

        /// <summary>
        /// Quality slider for the light.
        /// </summary>
#if UNITY_EDITOR
        [Range(0.0f, 2.0f)]
#endif
        [SerializeField]
        private float quality = 2.0f;

        /// <summary>
        /// When <c>true</c>, this light can be culled.
        /// </summary>
        [SerializeField]
        private bool useCulling = false;

        [SerializeField]
#if UNITY_EDITOR
        new
#endif
        private Light light = null;

        private int qualityIndex = -1;

        private float lodCoefficient = 1.0f;

        private static int updateSkipCounter = UpdateSkipCount;

        #endregion

        #region property

        public float DistanceScale {
            get => distanceScale;
            set {
                distanceScale = value;
                lodCoefficient = 1.0f / (distanceScale * distanceScale);
            }
        }

        #endregion

        #region logic

        #region OnEnable

        private void OnEnable() {
            if (light == null) enabled = false;
            LightLODList.Add(this);
            DistanceScale = distanceScale;
        }

        #endregion

        #region OnDisable

        private void OnDisable() {
            LightLODList.Remove(this);
        }

        #endregion

        #region InternalUpdate

        [CoreUpdate]
        private static void InternalUpdate() {
            if (--updateSkipCounter == 0) {
                updateSkipCounter = UpdateSkipCount;
                LightLOD light;
                Vector3 cameraPosition = CameraController.MainCameraPosition;
                for (int i = LightLODList.Count - 1; i >= 0; i--) {
                    light = LightLODList[i];
                    light.UpdateQuality(cameraPosition);
                }
            }
        }

        #endregion

        #region UpdateQuality

        public void UpdateQuality(in Vector3 cameraPosition) {
            if (light == null) return;
            float sqrDistance = (transform.position - cameraPosition).sqrMagnitude;
            float lod = sqrDistance * lodCoefficient;
            int nextQualityIndex;
            if (lod > 1.0f) { // at least at min quality
                nextQualityIndex = useCulling && lod > 1.5f ? - 1 : 2; // decide to cull or render at min settings
            } else {
                nextQualityIndex = Mathf.FloorToInt(quality * Mathf.Sqrt(lod));
            }
            if (nextQualityIndex != qualityIndex) SetQualityIndex(nextQualityIndex);
        }

        #endregion

        #region SetQualityIndex

        private void SetQualityIndex(in int qualityIndex) {
            this.qualityIndex = qualityIndex;
            switch (qualityIndex) {
                case 0: {
#if SET_SHADOW_RESOLUTION
                    light.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High;
#endif
                    light.shadows = LightShadows.Soft;
                    break;
                }
                case 1: {
#if SET_SHADOW_RESOLUTION
                    light.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Medium;
#endif
                    light.shadows = LightShadows.Soft;
                    break;
                }
                case 2: {
#if SET_SHADOW_RESOLUTION
                    light.shadowResolution = UnityEngine.Rendering.LightShadowResolution.Low;
#endif
                    light.shadows = LightShadows.Hard;
                    break;
                }
                default: {
                    light.enabled = false;
                    break;
                }
            }
        }

        #endregion

        #endregion

    }

}