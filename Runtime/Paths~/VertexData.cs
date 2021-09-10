using UnityEngine;

namespace BlackTundra.World.Paths {

    internal sealed class VertexData {

        #region variable

        internal int index;

        internal Vector3 tangent;

        internal Vector3 normal;

        internal float length;

        #endregion

        #region constructor

        internal VertexData(in int index, in Vector3 tangent, in Vector3 normal, in float length) {
            this.index = index;
            this.tangent = tangent;
            this.normal = normal;
            this.length = length;
        }

        #endregion

    }

}