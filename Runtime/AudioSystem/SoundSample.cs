using System;

using UnityEngine;

namespace BlackTundra.World.Audio {

    /// <summary>
    /// Describes a <see cref="Sound"/> that was observed (heard) in the world at a point.
    /// </summary>
    public struct SoundSample {

        #region constant

        /// <summary>
        /// An empty <see cref="SoundSample"/> with no intensity.
        /// </summary>
        public static readonly SoundSample Empty = new SoundSample(
            Vector3.zero,
            Vector3.forward,
            0.0f
        );

        #endregion

        #region variable

        /// <summary>
        /// Point that the sound was heard at.
        /// </summary>
        public readonly Vector3 point;

        /// <summary>
        /// Direction relative to the observer/query position that the sound came from.
        /// </summary>
        public readonly Vector3 direction;

        /// <summary>
        /// Intensity of the sound relative to the observer/query position.
        /// </summary>
        public readonly float intensity;

        #endregion

        #region constructor

        internal SoundSample(in Vector3 point, in Vector3 direction, in float intensity) {
            this.point = point;
            this.direction = direction;
            this.intensity = intensity;
        }

        #endregion

    }

    public static class SoundSampleUtility {

        #region logic

        #region GetLoudestSoundSample

        public static SoundSample GetLoudestSoundSample(this SoundSample[] samples) {

            if (samples == null) throw new ArgumentNullException(nameof(samples));

            float highestIntensity = 0.0f;
            int highestIntensityIndex = -1;

            float intensity;
            for (int i = 0; i < samples.Length; i++) {

                intensity = samples[i].intensity;
                if (intensity > highestIntensity) {
                    highestIntensity = intensity;
                    highestIntensityIndex = i;
                }

            }

            return highestIntensityIndex == -1 ? SoundSample.Empty : samples[highestIntensityIndex];

        }

        #endregion

        #endregion

    }

}