using System.Collections.Generic;

using UnityEngine;

namespace BlackTundra.World.Actors {

    /// <summary>
    /// Describes a visual sensor that an <see cref="Actor"/> can use for vision.
    /// </summary>
    public interface IVisionSensor {

        /// <returns>
        /// Returns <c>true</c> if the <see cref="IVisionSensor"/> can detect the <paramref name="collider"/>. This is not the same as the
        /// <paramref name="collider"/> being visible. This simply means it can be detected by the sensor.
        /// </returns>
        /// <remarks>
        /// Usually this should just return <c>true</c> if the <paramref name="collider"/> is in the <see cref="LayerMask"/> used by the
        /// <see cref="IVisionSensor"/> for <see cref="Collider"/> detection.
        /// </remarks>
        public bool IsDetectable(in Collider collider);

        public IEnumerator<Collider> QueryVisualSensorFrom(Vector3 point, Vector3 direction);

        public bool IsVisibleFrom(in Vector3 point, Vector3 direction, in Collider collider);

#if UNITY_EDITOR
        public void OnDrawGizmos();
#endif

    }

}