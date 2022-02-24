using BlackTundra.Foundation;

using System;

using UnityEngine;

namespace BlackTundra.World {

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
        public static float TemperatureLapseRateAir = 0.00976f;

        /// <summary>
        /// Height above sea level where rho of air will be modelled as a linear relationship between rho at this altitude (in meters) and
        /// rho of air at sea level.
        /// </summary>
        private const float RhoAirSeaLevelApproxHeight = 1000.0f;

        #endregion

        #region variable

        private static float rhoAirPower = 0.0f;

        private static float rhoAirCoefficient = 1.0f;

        private static float rhoAirSeaLevel = 0.0f;

        private static float rhoAirAltitudeLerpCoefficient = 0.0f;

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

        #endregion

        #region logic

        #region RecalculateConstants

        [CoreInitialise]
        private static void RecalculateConstants() {

            // rho air:
            // calculate values required for calculating rho of air by elevation:
            // https://en.wikipedia.org/wiki/Atmospheric_pressure
            rhoAirPower = (_gravity * MolarMassAir) / (UniversalGasConstant * TemperatureLapseRateAir);
            rhoAirCoefficient = TemperatureLapseRateAir / _seaLevelStandardTemperature;
            rhoAirSeaLevel = RhoAir(0.0f);
            float rhoAirAltitude = RhoAir(RhoAirSeaLevelApproxHeight);
            rhoAirAltitudeLerpCoefficient = (rhoAirAltitude - rhoAirSeaLevel) / RhoAirSeaLevelApproxHeight;
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

        #region RhoAir

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
        public static float RhoAir(in float heightAboveSeaLevel) => _seaLevelAtmosphericPressure * Mathf.Pow(1.0f - (rhoAirCoefficient * heightAboveSeaLevel), rhoAirPower);

        #endregion

        #endregion

    }

}