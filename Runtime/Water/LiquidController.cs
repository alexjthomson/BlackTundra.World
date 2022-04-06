using UnityEngine;

namespace BlackTundra.World.Fluids {

    /// <summary>
    /// Simulates a body of water.
    /// </summary>
#if UNITY_EDITOR
    [AddComponentMenu("Physics/Liquid")]
#endif
    [DisallowMultipleComponent]
    public sealed class LiquidController : MonoBehaviour {

        #region constant

        /// <summary>
        /// Water desnity in kg/m^3 (kilograms per meter cubed).
        /// </summary>
        private const float WaterDensity = 997.0f;

        /// <summary>
        /// Viscosity of water in PaS (Pascal seconds).
        /// </summary>
        private const float WaterViscosity = 0.0089f;

        /// <summary>
        /// Coefficient to convert the viscosity of a liquid into a scalar that controls how much the liquid is not effected by wind.
        /// </summary>
        private const float ViscosityToWindScalar = 1.0f / WaterViscosity;

        /// <summary>
        /// Converts density to a wind drag coefficient.
        /// </summary>
        private const float DensityToWindDrag = 1.0f / WaterDensity;

        #endregion

        #region variable

        /// <summary>
        /// Density of the liquid in kg/m^3 (kilograms per meter cubed).
        /// </summary>
        [SerializeField]
        private float density = WaterDensity;

        /// <summary>
        /// Viscosity of the liquid in PaS (Pascal seconds).
        /// </summary>
        [SerializeField]
        private float viscosity = WaterViscosity;

        /// <summary>
        /// Simulation flags for the <see cref="LiquidController"/>.
        /// </summary>
        [SerializeField]
        private LiquidSimulationFlags simulationFlags = 0;

        /// <summary>
        /// Smoothed wind velocity.
        /// </summary>
        private Vector3 windVelocity = Vector3.zero;

        /// <summary>
        /// Scalar that describes how much this liquid is effected by the wind per second.
        /// </summary>
        private float windVelocityScalar = 0.0f;

        /// <summary>
        /// Drag coefficient used to apply drag to the <see cref="windVelocity"/>.
        /// </summary>
        private float windDragCoefficient = 0.0f;

        #endregion

        #region property

        #endregion

        #region logic

        #region OnEnable

        private void OnEnable() {
            if ((simulationFlags & LiquidSimulationFlags.VertexDisplacement) == 0) return;
            RecalculateConstants();
        }

        #endregion

        #region RecalculateConstants

        private void RecalculateConstants() {
            windVelocityScalar = 1.0f / ((viscosity * ViscosityToWindScalar) + 1.0f);
            windDragCoefficient = density * DensityToWindDrag;
        }

        #endregion

        #region FixedUpdate

        private void FixedUpdate() {
            float deltaTime = Time.fixedDeltaTime;
            if ((simulationFlags & LiquidSimulationFlags.WindForce) != 0) {
                windVelocity = new Vector3(
                    windVelocity.x + (((Environment._windForce.x * windVelocityScalar) - (windVelocity.x * windVelocity.x * windDragCoefficient)) * deltaTime),
                    0.0f,
                    windVelocity.z + (((Environment._windForce.z * windVelocityScalar) - (windVelocity.x * windVelocity.x * windDragCoefficient)) * deltaTime)
                );
            }
        }

        #endregion

        #region GetHeightOffsetAt

        public float GetHeightOffsetAt(in Vector2 position) => GetHeightOffsetAt(position.x, position.y);
        public float GetHeightOffsetAt(in Vector3 position) => GetHeightOffsetAt(position.x, position.z);
        
        /// <summary>
        /// Get the vertical offset of the surface of the liquid above the surface.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public float GetHeightOffsetAt(in float x, in float z) {
            return 0.0f;
        }

        #endregion

        #endregion

    }

}