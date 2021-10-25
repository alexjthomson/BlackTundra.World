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
        /// Amount of an effect that this volume has.
        /// </summary>
        public readonly float weight;

        #endregion

        #region constructor

        internal VolumeHit(in Volume volume, in Vector3 point, in float weight) {
            this.volume = volume;
            this.point = point;
            this.weight = weight;
        }

        #endregion

    }

}