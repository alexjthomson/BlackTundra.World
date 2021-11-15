using BlackTundra.World.Audio;

using System.Collections.Generic;

using UnityEngine;

namespace BlackTundra.World.Actors {

    public interface ISoundSensor {

        public IEnumerator<SoundSample> QuerySoundSensorFrom(in Vector3 point, in Vector3 direction);

    }

}