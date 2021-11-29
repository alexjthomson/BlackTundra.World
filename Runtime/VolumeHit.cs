using UnityEngine;

namespace BlackTundra.World {

    public struct VolumeHit {

        #region variable

        /// <summary>
        /// <see cref="Volume"/> that was intersected.
        /// </summary>
        public readonly Volume volume;

        /// <summary>
        /// Point in world-space that the volume was intersected at.
        /// </summary>
        public readonly Vector3 point;

        /// <summary>
        /// Square distance from the query point to the closest <see cref="point"/> on the <see cref="volume"/> to the query point.
        /// </summary>
        public readonly float sqrDistance;

        /// <summary>
        /// Amount of an effect that this volume has.
        /// </summary>
        public readonly float weight;

        #endregion

        #region constructor

        internal VolumeHit(in Volume volume, in Vector3 point, in float sqrDistance) {
            this.volume = volume;
            this.point = point;
            this.sqrDistance = sqrDistance;
            if (sqrDistance <= volume.sqrBlendDistance) { // the square distance is within the blend distance
                if (volume.sqrBlendDistance > 0.0f) { // there is a blend distance
                    weight = volume._weight * (1.0f - (sqrDistance * volume.inverseSqrBlendDistance)); // calculate the volume based on the blend distance
                } else { // there is no blend distance, just use the volume
                    weight = volume._weight;
                }
            } else { // the square distance is too far for the volume to have any effect
                weight = 0.0f;
            }
        }

        #endregion

    }

}