using BlackTundra.Foundation;

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AI;

namespace BlackTundra.World.Audio {

    /// <summary>
    /// Describes and manages the acoustic environment as perceived by a listener within it. Only sounds marked
    /// as <see cref="SoundTag.ENV"/> can be queried in the <see cref="Soundscape"/>.
    /// </summary>
    public static class Soundscape {

        #region constant

        internal static readonly HashSet<SoundInstance> TrackedSoundInstances = new HashSet<SoundInstance>();

        #endregion

        #region property

        #endregion

        #region logic

        #region Validate

        /// <summary>
        /// Validates that the current state of the application is appropriate for the <see cref="Soundscape"/> to function.
        /// </summary>
        [CoreValidate]
        private static void Validate() {
            if (!NavMesh.FindClosestEdge(Vector3.zero, out _, -1)) Console.Error("Soundscape cannot calculate accurate volumes without a baked NavMesh; none was found.");
        }

        #endregion

        #region QueryAt

        /// <summary>
        /// Queries a <paramref name="point"/> in world-space for information about each audible sound.
        /// </summary>
        /// <param name="point">Point in world-space to query from.</param>
        /// <param name="range">Maximum range from the query <paramref name="point"/> to search.</param>
        /// <param name="thresholdIntensity">Minimum intensity of a sound for it to be included in the results.</param>
        public static IEnumerator<SoundSample> QueryAt(Vector3 point, float range, float thresholdIntensity) {
            // calculate the maximum distance a sound can be away from the query position:
            float sqrRange = range * range; // square range
            float maximumSqrDistance = 1.0f / (thresholdIntensity * thresholdIntensity); // convert threshold intensity into square distance from observer where a sound at maximum volume will have a relative intensity equal to the threshold intensity
            if (sqrRange < maximumSqrDistance) maximumSqrDistance = sqrRange; // choose the smallest of the two calculated square distances
            // query tracked sound instances:
            NavMeshPath path = new NavMeshPath();
            foreach (SoundInstance instance in TrackedSoundInstances) { // iterate each sound instance that is tracked by the soundscape
                Vector3 position = instance.position; // position of current sound source/instance
                Vector3 direction = position - point; // direction from the query position to the positon of the sound source/instance
                float sqrDistance = direction.sqrMagnitude; // square distance between the query position and sound source/instance
                float penetration = instance.sound.penetration; // penetration factor
                if (penetration < 0.99f && NavMesh.CalculatePath(instance.position, point, -1, path)) { // penetration has a significant effect on volume
                    Vector3[] points = path.corners;
                    float sqrPathLenth = 0.0f;
                    for (int i = points.Length - 1; i >= 1; i--) {
                        sqrPathLenth += (points[i] - points[i]).sqrMagnitude;
                    }
                    sqrDistance = (sqrDistance * penetration) + (sqrPathLenth * (1.0f - penetration)); // combine with penetration calculation
                }
                if (sqrDistance < maximumSqrDistance) { // if the square distance is within the maximum square distance, yield return a new SoundSample
                    float normalizationCoefficient = 1.0f / Mathf.Sqrt(sqrDistance); // calculate the factor required to normalize the direction vector
                    float relativeIntensity = instance.clipVolume * normalizationCoefficient; // calculate the relative intensity of the sound at the query position
                    if (relativeIntensity > thresholdIntensity) { // the sound is audible
                        yield return new SoundSample( // return the information calculated so far as a SoundSample
                            position, // position of the sound
                            direction * normalizationCoefficient, // normalized direction of the source of the sound relative to the query position
                            relativeIntensity // relative intensity of the sound to the query position
                        );
                    }
                }
            }
        }

        #endregion

        #region QueryVolumeAt

        /// <summary>
        /// Queries the relative intensity of all tracked sources of sound at a <paramref name="point"/>.
        /// </summary>
        /// <param name="point">Point in world-space to query the relative intensity of all tracked sounds from.</param>
        /// <param name="range">Maximum range from the query <paramref name="point"/> to search for sounds.</param>
        /// <param name="thresholdIntensity">Minimum relative intensity for a sound to be included in the total relative intensity.</param>
        /// <returns>Returns the relative intensity of all tracked sources of sound at the query <paramref name="point"/>.</returns>
        public static float QueryVolumeAt(in Vector3 point, float range, float thresholdIntensity) {
            float volume = 0.0f;
            // calculate the maximum distance a sound can be away from the query position:
            float sqrRange = range * range; // square range
            float maximumSqrDistance = 1.0f / (thresholdIntensity * thresholdIntensity); // convert threshold intensity into square distance from observer where a sound at maximum volume will have a relative intensity equal to the threshold intensity
            if (sqrRange < maximumSqrDistance) maximumSqrDistance = sqrRange; // choose the smallest of the two calculated square distances
            // query tracked sound instances:
            NavMeshPath path = new NavMeshPath();
            foreach (SoundInstance instance in TrackedSoundInstances) { // iterate each sound instance that is tracked by the soundscape
                Vector3 direction = instance.position - point; // direction from the query position to the positon of the sound source/instance
                float sqrDistance = direction.sqrMagnitude; // square distance between the query position and sound source/instance
                float penetration = instance.sound.penetration; // penetration factor
                if (penetration < 0.99f && NavMesh.CalculatePath(instance.position, point, -1, path)) { // penetration has a significant effect on volume
                    Vector3[] points = path.corners;
                    float sqrPathLenth = 0.0f;
                    for (int i = points.Length - 1; i >= 1; i--) {
                        sqrPathLenth += (points[i] - points[i]).sqrMagnitude;
                    }
                    sqrDistance = (sqrDistance * penetration) + (sqrPathLenth * (1.0f - penetration)); // combine with penetration calculation
                }
                if (sqrDistance < maximumSqrDistance) { // if the square distance is within the maximum square distance, yield return a new SoundSample
                    float relativeIntensity = instance.clipVolume / Mathf.Sqrt(sqrDistance); // calculate the relative intensity of the sound to a listener at the query position
                    volume += relativeIntensity; // add to the cumulative volume at the query position
                }
            }
            return volume; // return the cumulative volume at the query position
        }

        #endregion

        #endregion

    }

}