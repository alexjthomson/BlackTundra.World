using BlackTundra.World.Audio;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace BlackTundra.World.Actors {

#if UNITY_EDITOR
    [CreateAssetMenu(menuName = "Configuration/Actor/Sphere Sound Sensor", fileName = "SphereSoundSensor", order = 2000)]
#endif
    [Serializable]
    public sealed class SphereSoundSensor : ScriptableObject, ISoundSensor {

        #region variable

        /// <summary>
        /// Maximum range that sounds can be detected from.
        /// </summary>
#if UNITY_EDITOR
        [Min(0.0f)]
#endif
        [SerializeField]
        public float range = 50.0f;

        /// <summary>
        /// Threshold intensity of a sound before it can be picked up / heard.
        /// </summary>
#if UNITY_EDITOR
        [Min(0.0f)]
#endif
        [SerializeField]
        public float thresholdIntensity = 0.1f;

        #endregion

        #region logic

        public IEnumerator<SoundSample> QuerySoundSensorFrom(in Vector3 point, in Vector3 direction) => Soundscape.QueryAt(point, range, thresholdIntensity);

        #endregion

    }

}