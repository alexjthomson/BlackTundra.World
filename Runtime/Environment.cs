using BlackTundra.Foundation;

using System;

using UnityEngine;

using Object = UnityEngine.Object;

namespace BlackTundra.World {

    /// <summary>
    /// Manages the environment including resistance (rho), wind direction and force, gravity, environmental forces, and more.
    /// </summary>
    public static class Environment {

        #region constant

        /// <summary>
        /// Universal gas constant in J/(mol*K).
        /// </summary>
        public static float UniversalGasConstant = 8.314462618f;

        /// <summary>
        /// Molar mass of dry air in kg/mol.
        /// </summary>
        public static float MolarMassAir = 0.02896968f;

        /// <summary>
        /// Temperature lapse rate for dry air in K/m.
        /// </summary>
        public static float TemperatureLapseRateAir = 0.0065f;

        /// <summary>
        /// Height above sea level where rho of air will be modelled as a linear relationship between rho at this altitude (in meters) and
        /// rho of air at sea level.
        /// </summary>
        private const float RhoAirSeaLevelApproxHeight = 1000.0f;

        #endregion

        #region variable

        /// <summary>
        /// Power to raise part of the rho air equation to.
        /// </summary>
        private static float rhoAirPower = 0.0f;

        /// <summary>
        /// L/T where L = temperature lapse rate (<see cref="TemperatureLapseRateAir"/>), and T = sea level standard temperature
        /// (<see cref="SeaLevelStandardTemperature"/>).
        /// </summary>
        private static float rhoAirTemperatureLapseRateBySeaLevelTemperature = 1.0f;

        /// <summary>
        /// pM/RT where p = sea level standard atmospheric pressure, M = molar mass of dry air (<see cref="MolarMassAir"/>),
        /// R = ideal universal gas constant (<see cref="UniversalGasConstant"/>), T = sea level standard temperature
        /// (<see cref="SeaLevelStandardTemperature"/>).
        /// </summary>
        private static float rhoAirPressureToDensity = 0.0f;

        /// <summary>
        /// Rho air calculated at sea level.
        /// </summary>
        private static float rhoAirSeaLevel = 0.0f;

        private static float rhoAirAltitudeLerpCoefficient = 0.0f;

        /// <summary>
        /// Global <see cref="WindZone"/>.
        /// </summary>
        private static WindZone globalWindZone = null;

        #endregion

        #region property

        public static float gravity {
            get => _gravity;
            set {
                if (value < 0.0f) throw new ArgumentException($"{nameof(gravity)} cannot have a negative value.");
                _gravity = value;
                Physics.gravity = new Vector3(0.0f, -_gravity, 0.0f);
                RecalculateConstants();
            }
        }
        private static float _gravity = 9.80665f;

        /// <summary>
        /// Sea level in world y coordinate space.
        /// </summary>
        public static float SeaLevel {
            get => _seaHeight;
            set => _seaHeight = value;
        }
        private static float _seaHeight = 0.0f;

        /// <summary>
        /// Sea level atmospheric pressure in Pascal.
        /// </summary>
        public static float SeaLevelAtmosphericPressure {
            get => _seaLevelAtmosphericPressure;
            set {
                if (value < 0.0f) throw new ArgumentException($"{nameof(SeaLevelAtmosphericPressure)} cannot have a negative value.");
                _seaLevelAtmosphericPressure = value;
                RecalculateConstants();
            }
        }
        private static float _seaLevelAtmosphericPressure = 101325.0f;

        /// <summary>
        /// Sea level standard temperature in kelvin.
        /// </summary>
        public static float SeaLevelStandardTemperature {
            get => _seaLevelStandardTemperature;
            set {
                if (value < 0.0f) throw new ArgumentNullException($"{nameof(SeaLevelStandardTemperature)} cannot have a negative value.");
                _seaLevelStandardTemperature = value;
                RecalculateConstants();
            }
        }
        private static float _seaLevelStandardTemperature = 288.16f;

        /// <summary>
        /// Normalized direction that the global <see cref="WindZone"/> is facing.
        /// </summary>
        public static Vector3 WindDirection {
            get => _windDirection;
            set {
                _windDirection = new Vector3(value.x, 0.0f, value.z).normalized;
                _windForce = _windForceMagnitude * _windDirection;
                if (globalWindZone != null) {
                    globalWindZone.transform.rotation = Quaternion.LookRotation(_windDirection, Vector3.up);
                }
            }
        }
        private static Vector3 _windDirection = Vector3.forward;

        /// <summary>
        /// Wind force magnitude.
        /// </summary>
        public static float WindForceMagnitude {
            get => _windForceMagnitude;
            set {
                if (value > 0.0f) {
                    _windForceMagnitude = value;
                    _windForce = value * _windDirection;
                } else {
                    _windForceMagnitude = 0.0f;
                    _windForce = Vector3.zero;
                }
                if (globalWindZone != null) UpdateGlobalWindZone();
            }
        }
        private static float _windForceMagnitude = 0.0f;

        /// <summary>
        /// Wind force.
        /// </summary>
        public static Vector3 WindForce {
            get => _windForce;
            set {
                float magnitude = value.magnitude;
                WindDirection = value * (1.0f / magnitude);
                WindForceMagnitude = magnitude;
            }
        }
        internal static Vector3 _windForce = Vector3.zero;

        #endregion

        #region logic

        #region RecalculateConstants

        [CoreInitialise(-65000)]
        private static void RecalculateConstants() {
            // rho air:
            // calculate values required for calculating rho of air by elevation:
            // https://en.wikipedia.org/wiki/Atmospheric_pressure
            rhoAirPower = (_gravity * MolarMassAir) / (UniversalGasConstant * TemperatureLapseRateAir);
            rhoAirTemperatureLapseRateBySeaLevelTemperature = TemperatureLapseRateAir / _seaLevelStandardTemperature;
            rhoAirSeaLevel = RhoAir(0.0f);
            rhoAirPressureToDensity = (_seaLevelAtmosphericPressure * MolarMassAir) / (UniversalGasConstant * SeaLevelStandardTemperature);
            float rhoAirAltitude = RhoAir(RhoAirSeaLevelApproxHeight);
            rhoAirAltitudeLerpCoefficient = (rhoAirAltitude - rhoAirSeaLevel) / RhoAirSeaLevelApproxHeight;
        }

        #endregion

        #region Initialise

        [CoreInitialise(-60000)]
        private static void Initialise() {
            // create global wind zone:
            GameObject gameObject = new GameObject(
                "_GlobalWindZone",
                typeof(WindZone)
            ) {
                layer = 2,
                isStatic = false,
            };
            globalWindZone = gameObject.GetComponent<WindZone>();
            globalWindZone.mode = WindZoneMode.Directional;
            Object.DontDestroyOnLoad(gameObject);
            UpdateGlobalWindZone();
        }

        #endregion

        #region UpdateGlobalWindZone

        private static void UpdateGlobalWindZone() {
            globalWindZone.windMain = _windForceMagnitude;
            globalWindZone.windTurbulence = 0.25f + (_windForceMagnitude * 0.01f);
            globalWindZone.windPulseMagnitude = 0.5f + (_windForceMagnitude * 0.0073f);
            globalWindZone.windPulseFrequency = Mathf.Lerp(0.01f, 0.001f, _windForceMagnitude * 0.0001f);
        }

        #endregion

        #region EnvironmentalForceAt

        public static Vector3 EnvironmentalForceAt(in Vector3 worldPosition) {
            // TODO: add wind forces based on sea level height etc here
            return Vector3.zero;
        }

        #endregion

        #region RhoAt

        public static float RhoAt(in Vector3 worldPosition) {
            float heightAboveSeaLevel = worldPosition.y - _seaHeight;
            // TODO: add sea calculation here:
            return RhoAirApprox(Mathf.Max(heightAboveSeaLevel, 0.0f));
        }

        #endregion

        #region RhoAirApprox

        /// <summary>
        /// Approximates rho of air when <paramref name="heightAboveSeaLevel"/> is below <see cref="RhoAirSeaLevelApproxHeight"/>.
        /// Otherwise, rho is calculated accurately.
        /// </summary>
        public static float RhoAirApprox(in float heightAboveSeaLevel) {
            if (heightAboveSeaLevel < RhoAirSeaLevelApproxHeight) {
                if (heightAboveSeaLevel < Mathf.Epsilon) {
                    return rhoAirSeaLevel;
                } else {
                    return rhoAirSeaLevel + (rhoAirAltitudeLerpCoefficient * heightAboveSeaLevel);
                }
            } else {
                return RhoAir(heightAboveSeaLevel);
            }
        }

        #endregion

        #region RhoAir

        /// <summary>
        /// Calculates the value of rho of air at a certain <paramref name="heightAboveSeaLevel"/>.
        /// </summary>
        public static float RhoAir(in float heightAboveSeaLevel) {
            return rhoAirPressureToDensity * Mathf.Pow(
                1.0f - (rhoAirTemperatureLapseRateBySeaLevelTemperature * heightAboveSeaLevel),
                rhoAirPower
            );
        }

        #endregion

        #endregion

    }

}