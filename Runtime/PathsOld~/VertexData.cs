using UnityEngine;

namespace BlackTundra.WorldSystem.Paths {

    public sealed class VertexData {

        #region variable

        public readonly Vector3 position;

        public readonly Vector3 tangent;

        public readonly float cumulativeLength;

        #endregion

        #region constructor

        internal VertexData(in Vector3 position, in Vector3 tangent, in float cumulativeLength) {

            this.position = position;
            this.tangent = tangent;
            this.cumulativeLength = cumulativeLength;

        }

        #endregion

    }

}