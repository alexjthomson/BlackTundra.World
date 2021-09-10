using System;

using UnityEngine;
using UnityEngine.AI;

namespace BlackTundra.World.Actors {

    public static class ActorUtility {

        #region constant

        private const float OnMeshThreshold = 3.0f;

        #endregion

        #region logic

        #region IsOnNavMesh

        public static bool IsOnNavMesh(this Transform transform) {
            if (transform == null) throw new ArgumentNullException("transform");
            return IsOnNavMesh(transform.position);
        }

        public static bool IsOnNavMesh(this GameObject gameObject) {
            if (gameObject == null) throw new ArgumentNullException("gameObject");
            return IsOnNavMesh(gameObject.transform.position);
        }

        public static bool IsOnNavMesh(in Vector3 position) => NavMesh.SamplePosition(position, out NavMeshHit hit, OnMeshThreshold, NavMesh.AllAreas)
            && Mathf.Approximately(position.x, hit.position.x)
            && Mathf.Approximately(position.z, hit.position.z)
            && position.y >= hit.position.y - Mathf.Epsilon;

        #endregion

        #endregion

    }

}