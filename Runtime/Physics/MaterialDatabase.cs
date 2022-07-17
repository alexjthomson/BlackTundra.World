using BlackTundra.Foundation;

using System.Collections.Generic;

using UnityEngine;

namespace BlackTundra.World {

    /// <summary>
    /// Extends <see cref="PhysicMaterial"/> materials with additional properties.
    /// </summary>
#if UNITY_EDITOR
    [CreateAssetMenu(menuName = "Configuration/Physics/Material Database", fileName = "MaterialDatabase", order = 0)]
#endif
    public sealed class MaterialDatabase : ScriptableObject {

        #region constant

        private const string ResourcePath = "Settings/MaterialDatabase";

        private static readonly ResourceReference<MaterialDatabase> ResourceReference = new ResourceReference<MaterialDatabase>(ResourcePath);

        private static readonly Dictionary<PhysicMaterial, MaterialDescriptor> MaterialDictionary = new Dictionary<PhysicMaterial, MaterialDescriptor>();

        #endregion

        #region variable

#if UNITY_EDITOR
        [Header("Materials")]
#endif

        [SerializeField]
        private MaterialDescriptor[] materials = new MaterialDescriptor[0];

#if UNITY_EDITOR
        [Space]
        [Header("Fallback Material")]
#endif

        [SerializeField]
        private MaterialDescriptor fallbackMaterialDescriptor = null;

        private static MaterialDescriptor _fallbackMaterialDescriptor = null;

        #endregion

        #region logic

        #region Initialise

        [CoreInitialise(-80000)]
        private static void Initialise() {
            MaterialDatabase database = ResourceReference.Value;
            if (database == null) {
                ConsoleFormatter consoleFormatter = new ConsoleFormatter(nameof(MaterialDatabase));
                consoleFormatter.Error($"`{ResourcePath}` {nameof(MaterialDatabase)} resource not found.");
                return;
            }
            MaterialDescriptor[] materials = database.materials;
            MaterialDictionary.Clear();
            MaterialDescriptor material;
            PhysicMaterial physicMaterial;
            for (int i = materials.Length - 1; i >= 0; i--) {
                material = materials[i];
                if (material != null) {
                    physicMaterial = material.material;
                    if (physicMaterial != null) {
                        MaterialDictionary[physicMaterial] = material;
                    }
                }
            }
            _fallbackMaterialDescriptor = database.fallbackMaterialDescriptor;
        }

        #endregion

        #region GetMaterialDescriptor

        /// <summary>
        /// Gets the <see cref="MaterialDescriptor"/> associated with the <paramref name="physicMaterial"/>.
        /// </summary>
        public static MaterialDescriptor GetMaterialDescriptor(in PhysicMaterial physicMaterial)
            => physicMaterial != null && MaterialDictionary.TryGetValue(physicMaterial, out MaterialDescriptor descriptor)
                ? descriptor
                : GetFallbackMaterialDescriptor();

        #endregion

        #region GetFallbackMaterialDescriptor

        public static MaterialDescriptor GetFallbackMaterialDescriptor() => _fallbackMaterialDescriptor;

        #endregion

        #endregion

    }

}