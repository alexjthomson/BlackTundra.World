using BlackTundra.Foundation.Utility;

using System;

using UnityEngine;

namespace BlackTundra.World.Paths {

    /// <summary>
    /// Stores data about a path made up of multiple <see cref="PathSegment"/> instances.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The path works by storing every vertex used in the entire <see cref="Path"/> in
    /// a <see cref="verticies"/> array. These verticies are then referenced by indivitaul
    /// <see cref="PathSegment"/> instances, which use them to build a path.
    /// </para>
    /// <para>
    /// Each <see cref="PathSegment"/> can intersect with each other. These intersections
    /// are tracked by <see cref="PathIntersection"/> instances. These store the index of
    /// the vertex the intersection occurs at and the index of each
    /// <see cref="PathSegment"/> instance that intersects at that vertex.
    /// </para>
    /// <para>
    /// The combination of this data can then be used to construct a path.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
#if UNITY_EDITOR
    [AddComponentMenu("World/Path")]
#endif
    public sealed class Path : MonoBehaviour {

        #region variable

        /// <summary>
        /// An array of each vertex used in the <see cref="Path"/>.
        /// </summary>
        private Vector3[] verticies;

        /// <summary>
        /// An array of each <see cref="PathSegment"/> used in the <see cref="Path"/>.
        /// </summary>
        private PathSegment[] segments;

        /// <summary>
        /// An array of each <see cref="PathIntersection"/> found in the <see cref="Path"/>.
        /// </summary>
        private PathIntersection[] intersections;

        #endregion

        #region property

        /// <summary>
        /// Number of verticies in the <see cref="Path"/>.
        /// </summary>
        public int VertexCount => verticies.Length;

        /// <summary>
        /// Number of <see cref="PathSegment"/> instances associated with the <see cref="Path"/>.
        /// </summary>
        public int SegmentCount => segments.Length;

        /// <summary>
        /// Number of <see cref="PathIntersection"/> instances associated with the <see cref="Path"/>.
        /// </summary>
        public int IntersectionCount => intersections.Length;

        #endregion

        #region logic

        #region GetVertex

        public Vector3 GetVertex(in int index) => verticies[index];

        #endregion

        #region AddVertex

        /// <summary>
        /// Adds a vertex to the <see cref="Path"/>.
        /// </summary>
        /// <param name="vertex">Vertex data.</param>
        /// <returns>Returns the vertex index of the new <paramref name="vertex"/> added to the <see cref="Path"/>.</returns>
        public int AddVertex(in Vector3 vertex) {
            int index = verticies.Length;
            verticies = verticies.AddLast(vertex);
            return index;
        }

        #endregion

        #region RemoveVertex

        /// <summary>
        /// Removes a vertex from the <see cref="Path"/> by the vertex <paramref name="index"/>.
        /// </summary>
        /// <param name="index">Vertex index.</param>
        /// <returns>Returns the vertex that was removed from the <see cref="Path"/>.</returns>
        public Vector3 RemoveVertex(in int index) {
            if (index < 0 || index >= verticies.Length) throw new ArgumentOutOfRangeException(nameof(index));
            verticies = verticies.RemoveAt(index, out Vector3 vertex);
            for (int i = 0; i < segments.Length; i++) segments[i].RemoveVertex(index);
            for (int i = 0; i < intersections.Length; i++) intersections[i].RemoveVertex(index);
            return vertex;
        }

        #endregion

        #region GetSegment

        public PathSegment GetSegment(in int index) => segments[index];

        #endregion

        #region GetIntersection

        public PathIntersection GetIntersection(in int index) => intersections[index];

        #endregion

        #endregion

    }

}