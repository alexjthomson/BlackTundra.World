using System;

using UnityEngine;

namespace BlackTundra.World {

    [Serializable]
    public sealed class MaterialDescriptor {

        #region variable

        [SerializeField]
        internal string name = "Material";

        [SerializeField]
        internal PhysicMaterial material = null;

        /// <summary>
        /// Describes how easily the material can be penetrated as a value between <c>0.0</c> and <c>1.0</c>.
        /// </summary>
        [SerializeField]
#if UNITY_EDITOR
        [Tooltip("Describes how easily the material can be penetrated.")]
        [Range(0.0f, 1.0f)]
#endif
        internal float hardness = 0.5f;

        /// <summary>
        /// Density of the material (kg/m^3).
        /// </summary>
        [SerializeField]
#if UNITY_EDITOR
        [Tooltip("Density of the material (kg/m^3).")]
        [Min(0.0f)]
#endif
        internal float density = 1.0f;

        #endregion

    }

}