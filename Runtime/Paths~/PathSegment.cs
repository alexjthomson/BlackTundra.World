using System;

using BlackTundra.Foundation.Utility;

using UnityEngine;

namespace BlackTundra.World.Paths {

    public sealed class PathSegment {

        #region variable

        /// <summary>
        /// Every <see cref="VertexData"/> that the <see cref="PathSegment"/> uses.
        /// </summary>
        private VertexData[] vertexData;

        /// <summary>
        /// Cumulative length of each <see cref="VertexData"/> in the <see cref="vertexData"/> array.
        /// </summary>
        private float[] cumulativeLengths;

        /// <summary>
        /// Total length of the <see cref="PathSegment"/>.
        /// </summary>
        private float length;

        /// <summary>
        /// <see cref="Path"/> that the <see cref="PathSegment"/> belongs to.
        /// </summary>
        public readonly Path parent;

        /// <summary>
        /// Bounds of the <see cref="PathSegment"/>.
        /// </summary>
        public Bounds bounds;

        /// <summary>
        /// When <c>true</c>, the normals directions are flipped.
        /// </summary>
        private bool flipNormals;

        /// <summary>
        /// When <c>true</c>, the <see cref="PathSegment"/> will only exist in the X,Z plane.
        /// </summary>
        private bool flat;

        #endregion

        #region property

        /// <summary>
        /// When <c>true</c>, the normals directions are flipped.
        /// </summary>
        public bool FlipNormals {
            get => flipNormals;
            set {
                if (value == flipNormals) return;
                flipNormals = value;
                Recalculate();
            }
        }

        public float PathLength => length;

        #endregion

        #region constructor

        internal PathSegment(in Path parent, in bool flat) {
            this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
            this.flat = flat;
            vertexData = new VertexData[0];
            cumulativeLengths = new float[0];
            length = 0.0f;
            bounds = new Bounds();
            flipNormals = false;
        }

        #endregion

        #region logic

        #region RemoveVertex

        /// <summary>
        /// Removes a vertex from the <see cref="PathSegment"/>.
        /// </summary>
        /// <param name="vertexIndex">Vertex index of the vertex in the parent <see cref="Path"/> object.</param>
        internal void RemoveVertex(in int vertexIndex) {
            VertexData vertex;
            for (int i = vertexData.Length - 1; i >= 0; i--) {
                vertex = vertexData[i];
                if (vertex.index == vertexIndex) {
                    vertexData = vertexData.RemoveAt(i);
                    cumulativeLengths = cumulativeLengths.RemoveAt(i);
                } else if (vertex.index > vertexIndex) vertex.index -= 1;
            }
            Recalculate();
        }

        #endregion

        #region Recalculate

        /// <summary>
        /// Recalculates all of the <see cref="PathSegment"/> data.
        /// </summary>
        private void Recalculate() {
            VertexData vertex;
            VertexData lastVertex = null;
            Vector3 lastRotationAxis = Vector3.up;
            for (int i = 0; i < vertexData.Length; i++) {
                vertex = vertexData[i];
                if (flat || i == 0) {
                    vertex.normal = (flipNormals ? -Vector3.Cross(vertex.tangent, Vector3.up) : Vector3.Cross(vertex.tangent, Vector3.up)).normalized;
                } else {
                    Vector3 offset = parent.GetVertex(vertex.index) - parent.GetVertex(lastVertex.index);
                    // first reflection:
                    float coefficient = 2.0f / offset.sqrMagnitude;
                    Vector3 r = lastRotationAxis - offset * coefficient * Vector3.Dot(offset, lastRotationAxis);
                    Vector3 t = lastVertex.tangent - offset * coefficient * Vector3.Dot(offset, lastVertex.tangent);
                    // second reflection:
                    Vector3 v2 = vertex.tangent - t;
                    float v2Sqr = v2.sqrMagnitude;

                    Vector3 finalRotation = (r - v2) * (2.0f / v2Sqr) * Vector3.Dot(v2, r);
                    vertex.normal = (flipNormals ? -Vector3.Cross(finalRotation, vertex.tangent) : Vector3.Cross(finalRotation, vertex.tangent)).normalized;
                    lastRotationAxis = finalRotation;
                }
                lastVertex = vertex;
            }
        }

        #endregion

        #endregion

    }

}