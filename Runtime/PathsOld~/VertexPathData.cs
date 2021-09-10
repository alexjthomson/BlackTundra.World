using System.Collections.Generic;

using UnityEngine;

using BlackTundra.Foundation.Utility;

namespace BlackTundra.WorldSystem.Paths {

    public sealed class VertexPathData {

        #region variable

        /// <summary>
        /// Vertex data for the path.
        /// </summary>
        public readonly List<VertexData> vertexData;

        /// <summary>
        /// Maps anchor points to VertexData.
        /// </summary>
        public readonly int[] anchorVertexMap;

        /// <summary>
        /// Total length of the path.
        /// </summary>
        public readonly float length;

        /// <summary>
        /// Minmax Vector3 used for calculating bounds.
        /// </summary>
        internal readonly MinMaxVector3 minMax;

        #endregion

        #region property

        public Bounds Bounds => minMax.ToBounds();

        #endregion

        #region constructor

        internal VertexPathData(in List<VertexData> vertexData, in int[] anchorVertexMap, in float length) {

            this.vertexData = vertexData;
            this.anchorVertexMap = anchorVertexMap;
            this.length = length;
            minMax = new MinMaxVector3();

        }

        #endregion

    }

}