using BlackTundra.Foundation.Utility;
using System;
using UnityEngine;

namespace BlackTundra.WorldSystem.Paths {

    [Serializable]
    public sealed class VertexPath {

        #region constant

        /// <summary>
        /// Scalar for how many times bezier paths are divided when determining vertex positions.
        /// </summary>
        private const int PathAccuracy = 10;

        #endregion

        #region nested

        private sealed class PathPositionData {

            #region variable

            public readonly int lastIndex;
            public readonly int nextIndex;
            public readonly float position;

            #endregion

            #region constructor

            internal PathPositionData(in int lastIndex, in int nextIndex, in float position) {

                this.lastIndex = lastIndex;
                this.nextIndex = nextIndex;
                this.position = position;

            }

            #endregion

        }

        #endregion

        #region variable

        /// <summary>
        /// Space that the path exists in.
        /// </summary>
        public /*readonly*/ PathSpace space;

        /// <summary>
        /// True if the path is closed.
        /// </summary>
        public /*readonly*/ bool closed;

        /// <summary>
        /// Points on the vertex path.
        /// </summary>
        public /*readonly*/ Vector3[] points;

        /// <summary>
        /// Corresponding tangents to each point in the vertex path.
        /// </summary>
        public /*readonly*/ Vector3[] tangents;

        /// <summary>
        /// Corresponding normal vectors to each point in the vertex path.
        /// </summary>
        public /*readonly*/ Vector3[] normals;

        /// <summary>
        /// Each position along the path at each vertex.
        /// This will always be a value between <c>0.0</c> and <c>1.0</c>.
        /// </summary>
        public /*readonly*/ float[] positions;

        /// <summary>
        /// Total length of the path.
        /// </summary>
        public /*readonly*/ float length;

        /// <summary>
        /// Total distance from the first vertex up to each vertex in the polyline.
        /// </summary>
        public /*readonly*/ float[] cumulativeLengthPerVertex;

        /// <summary>
        /// Bounding box of the path.
        /// </summary>
        public /*readonly*/ Bounds bounds;

        /// <summary>
        /// Up direction for the path.
        /// </summary>
        public Vector3 up;

        /// <summary>
        /// Transform component that the VertexPath is associated with.
        /// </summary>
        [SerializeField] private Transform transform;

        #endregion

        #region constructor

        public VertexPath(in BezierPath bezierPath, in Transform transform, in float maxAngleError = 0.3f, in float minVertexDistance = 0.0f) :
            this(bezierPath, bezierPath.SplitByAngleError(maxAngleError, minVertexDistance, PathAccuracy), transform) { }

        public VertexPath(in BezierPath bezierPath, in Transform transform, float vertexSpacing) :
            this(bezierPath, bezierPath.SplitByDistance(vertexSpacing, PathAccuracy), transform) { }

        private VertexPath(in BezierPath bezierPath, in VertexPathData vertexPathData, in Transform transform) {
            
            if (bezierPath == null) throw new ArgumentNullException("bezierPath");
            if (vertexPathData == null) throw new ArgumentNullException("pathData");
            if (transform == null) throw new ArgumentNullException("transform");
            
            this.transform = transform;
            space = bezierPath.PathSpace;
            closed = bezierPath.IsClosed;
            int vertexCount = vertexPathData.vertexData.Count;
            length = vertexPathData.length;
            float inverseLength = 1.0f / length;

            points = new Vector3[vertexCount];
            normals = new Vector3[vertexCount];
            tangents = new Vector3[vertexCount];
            cumulativeLengthPerVertex = new float[vertexCount];
            positions = new float[vertexCount];
            bounds = vertexPathData.Bounds;

            up = bounds.size.z > bounds.size.y ? Vector3.up : Vector3.back;
            Vector3 lastRotationAxis = up;

            VertexData vertexData;
            for (int i = 0; i < vertexCount; i++) { // iterate each vertex

                vertexData = vertexPathData.vertexData[i];
                points[i] = vertexData.position;
                tangents[i] = vertexData.tangent;
                cumulativeLengthPerVertex[i] = vertexData.cumulativeLength;
                positions[i] = vertexData.cumulativeLength * inverseLength;

                // calculate normals:
                if (space == PathSpace.xyz) {

                    if (i == 0) {
                        normals[0] = Vector3.Cross(lastRotationAxis, vertexData.tangent).normalized;
                    } else {

                        // first reflection:
                        Vector3 offset = points[i] - points[i - 1];
                        float coefficient = 2.0f / offset.sqrMagnitude;
                        Vector3 r = lastRotationAxis - offset * coefficient * Vector3.Dot(offset, lastRotationAxis);
                        Vector3 t = tangents[i - 1] - offset * coefficient * Vector3.Dot(offset, tangents[i - 1]);

                        // second reflection:
                        Vector3 v2 = vertexData.tangent - t;
                        float v2sqr = Vector3.Dot(v2, v2);

                        Vector3 finalRotation = r - v2 * 2.0f / v2sqr * Vector3.Dot(v2, r);
                        Vector3 n = Vector3.Cross(finalRotation, vertexData.tangent).normalized;
                        normals[i] = n;
                        lastRotationAxis = finalRotation;

                    }

                } else {

                    normals[i] = bezierPath.FlipNormals ? -Vector3.Cross(vertexData.tangent, up) : Vector3.Cross(vertexData.tangent, up);

                }

            }

            #region correct end normals

            if (closed && space == PathSpace.xyz) { // apply correction for 3D normals along a closed path

                float normalsAngleErrorAcrossJoin = Vector3.SignedAngle(normals[vertexCount - 1], normals[0], tangents[0]); // get the angle between the first and last normal
                if (Mathf.Abs(normalsAngleErrorAcrossJoin) > 0.1f) { // the angle between the start and end normals is above the correction threshold, this means they're not correct

                    float stepAmount = 1.0f / (vertexCount - 1);
                    for (int i = 1; i < vertexCount; i++) {

                        float t = i * stepAmount;
                        float angle = normalsAngleErrorAcrossJoin * t;
                        Quaternion rotation = Quaternion.AngleAxis(angle, tangents[i]);
                        normals[i] = rotation * (bezierPath.FlipNormals ? -normals[i] : normals[i]);

                    }

                }

            }

            #endregion

            #region rotate normals to match up with user-defined anchor angles

            if (space == PathSpace.xyz) {

                int anchorCount = vertexPathData.anchorVertexMap.Length;
                int segmentCount = bezierPath.SegmentCount;
                for (int anchorIndex = 0; anchorIndex < anchorCount - 1; anchorIndex++) {

                    int nextAnchorIndex = closed ? anchorIndex + 1 % segmentCount : anchorIndex + 1;

                    float startAngle = bezierPath.GetAnchorNormalAngle(anchorIndex) + bezierPath.GlobalNormalsAngle;
                    float endAngle = bezierPath.GetAnchorNormalAngle(nextAnchorIndex) + bezierPath.GlobalNormalsAngle;
                    float deltaAngle = Mathf.DeltaAngle(startAngle, endAngle);

                    int startVertexIndex = vertexPathData.anchorVertexMap[anchorIndex];
                    int endVertexIndex = vertexPathData.anchorVertexMap[anchorIndex + 1];

                    int num = endVertexIndex - startVertexIndex; // get number of verticies
                    if (anchorIndex == anchorCount - 2) num++;

                    for (int i = 0; i < num; i++) {

                        int vertexIndex = startVertexIndex + i;
                        float t = num == 1 ? 1.0f : (i / (num - 1.0f));
                        float angle = startAngle + (deltaAngle * t);
                        Quaternion rotation = Quaternion.AngleAxis(angle, tangents[vertexIndex]);
                        normals[vertexIndex] = rotation * (bezierPath.FlipNormals ? -normals[vertexIndex] : normals[vertexIndex]);

                    }

                }

            }

            #endregion

        }

        #endregion

        #region logic

        #region UpdateTransform

        public void UpdateTransform(in Transform transform) {

            if (transform == null) throw new ArgumentNullException("transform");
            this.transform = transform;

        }

        #endregion

        #region GetPoint

        public Vector3 GetPoint(in int index) => transform.TransformPoint(points[index], space);

        #endregion

        #region GetNormal

        public Vector3 GetNormal(in int index) => transform.TransformDirection(normals[index], space);

        #endregion

        #region GetTangent

        public Vector3 GetTangent(in int index) => transform.TransformDirection(tangents[index], space);

        #endregion

        #region GetPointAtDistance

        /// <summary>
        /// Gets a point on the path based on the distance travelled.
        /// </summary>
        public Vector3 GetPointAtDistance(in float distance, in PathLoopBehaviour loopBehaviour = PathLoopBehaviour.Loop) => GetPointAtPosition(distance / length, loopBehaviour);

        #endregion

        #region GetPointAtPosition

        public Vector3 GetPointAtPosition(in float position, in PathLoopBehaviour loopBehaviour = PathLoopBehaviour.Loop) {

            PathPositionData data = CalculatePathPositionData(position, loopBehaviour);
            return Vector3.Lerp(
                GetPoint(data.lastIndex),
                GetPoint(data.nextIndex),
                data.position
            );

        }

        #endregion

        #region GetDirectionAtDistance

        /// <summary>
        /// Gets the forward direction on the path based on the distance travelled.
        /// </summary>
        public Vector3 GetDirectionAtDistance(in float distance, in PathLoopBehaviour loopBehaviour = PathLoopBehaviour.Loop) => GetDirectionAtPosition(distance / length, loopBehaviour);

        #endregion

        #region GetDirectionAtPosition

        public Vector3 GetDirectionAtPosition(in float position, in PathLoopBehaviour loopBehaviour = PathLoopBehaviour.Loop) {

            PathPositionData data = CalculatePathPositionData(position, loopBehaviour);
            return transform.TransformDirection(
                Vector3.Lerp(
                    tangents[data.lastIndex],
                    tangents[data.nextIndex],
                    data.position
                ),
                space
            );

        }

        #endregion

        #region GetNormalAtDistance

        /// <summary>
        /// Gets the normal vector on the path based on the distance travelled.
        /// </summary>
        public Vector3 GetNormalAtDistance(in float distance, in PathLoopBehaviour loopBehaviour = PathLoopBehaviour.Loop) => GetNormalAtPosition(distance / length, loopBehaviour);

        #endregion

        #region GetNormalAtPosition

        public Vector3 GetNormalAtPosition(in float position, in PathLoopBehaviour loopBehaviour = PathLoopBehaviour.Loop) {

            PathPositionData data = CalculatePathPositionData(position, loopBehaviour);
            return transform.TransformDirection(
                Vector3.Lerp(
                    normals[data.lastIndex],
                    normals[data.nextIndex],
                    data.position
                ),
                space
            );

        }

        #endregion

        #region GetRotationAtDistance

        public Quaternion GetRotationAtDistance(in float distance, in PathLoopBehaviour loopBehaviour = PathLoopBehaviour.Loop) => GetRotationAtPosition(distance / length, loopBehaviour);

        #endregion

        #region GetRotationAtPosition

        public Quaternion GetRotationAtPosition(in float position, in PathLoopBehaviour loopBehaviour = PathLoopBehaviour.Loop) {

            PathPositionData data = CalculatePathPositionData(position, loopBehaviour);
            return Quaternion.LookRotation(
                transform.TransformDirection(
                    Vector3.Lerp(
                        tangents[data.lastIndex],
                        tangents[data.nextIndex],
                        data.position
                    ),
                    space
                ),
                transform.TransformDirection(
                    Vector3.Lerp(
                        normals[data.lastIndex],
                        normals[data.nextIndex],
                        data.position
                    ),
                    space
                )
            );

        }

        #endregion

        #region GetClosestPointOnPath

        public Vector3 GetClosestPointOnPath(in Vector3 position) {

            Vector3 localPosition = transform.InverseTransformPoint(position, space);
            PathPositionData data = CalculatePathPositionData(localPosition);
            return transform.TransformPoint(
                Vector3.Lerp(
                    points[data.lastIndex],
                    points[data.nextIndex],
                    data.position
                ),
                space
            );

        }

        #endregion

        #region GetClosestPositionOnPath

        public float GetClosestPositionOnPath(in Vector3 position) {

            Vector3 localPosition = transform.InverseTransformPoint(position, space);
            PathPositionData data = CalculatePathPositionData(localPosition);
            return Mathf.Lerp(
                positions[data.lastIndex],
                positions[data.nextIndex],
                data.position
            );

        }

        #endregion

        #region GetClosestDistanceAlongPath

        public float GetClosestDistanceAlongPath(in Vector3 position) {

            Vector3 localPosition = transform.InverseTransformPoint(position, space);
            PathPositionData data = CalculatePathPositionData(localPosition);
            return Mathf.Lerp(
                cumulativeLengthPerVertex[data.lastIndex],
                cumulativeLengthPerVertex[data.nextIndex],
                data.position
            );

        }

        #endregion

        #region CalculatePathPositionData

        /// <summary>
        /// For a given position, this method calculates the indicies of the two verticies before and after the position.
        /// </summary>
        /// <param name="position">Value between <c>0.0</c> and <c>1.0</c> that describes the position on the path from start to end.</param>
        private PathPositionData CalculatePathPositionData(float position, in PathLoopBehaviour loopBehaviour) {

            switch (loopBehaviour) {

                case PathLoopBehaviour.Loop: {

                    if (position < 0.0f) position += Mathf.Ceil(-position);
                    position %= 1.0f;
                    break;

                }

                case PathLoopBehaviour.Reverse: {

                    position = Mathf.PingPong(position, 1.0f);
                    break;

                }

                case PathLoopBehaviour.Stop: {

                    position = Mathf.Clamp01(position);
                    break;

                }

            }

            int lastIndex = 0;
            int nextIndex = points.Length - 1;
            int i = Mathf.RoundToInt(position * nextIndex); // first guess

            while (true) {

                if (position <= positions[i]) nextIndex = i; // t lies to the left
                else lastIndex = i; // t lies to the right
                i = Mathf.FloorToInt(0.5f * (nextIndex + lastIndex));
                if (nextIndex - lastIndex <= 1) break;

            }

            return new PathPositionData(
                lastIndex,
                nextIndex,
                Mathf.InverseLerp(positions[lastIndex], positions[nextIndex], position)
            );

        }

        /// <summary>
        /// Calculates path position data for the closest point on the path to the position.
        /// </summary>
        /// <param name="localPosition">Position to find the path position data for.</param>
        private PathPositionData CalculatePathPositionData(in Vector3 localPosition) {

            float minSqrDistance = float.MaxValue;
            Vector3 closestPoint = Vector3.zero;

            int closestSegmentIndexA = 0;
            int closestSegmentIndexB = 0;

            for (int i = 0; i < points.Length; i++) {

                int nextIndex = i + 1;
                if (nextIndex >= points.Length) {

                    if (closed) nextIndex -= points.Length;
                    else break;

                }

                Vector3 closestPointOnSegment = MathsUtility.ClosestPointOnLine(localPosition, points[i], points[nextIndex]);
                float sqrDistance = (localPosition - closestPointOnSegment).sqrMagnitude;
                if (sqrDistance < minSqrDistance) {
                    minSqrDistance = sqrDistance;
                    closestPoint = closestPointOnSegment;
                    closestSegmentIndexA = i;
                    closestSegmentIndexB = nextIndex;
                }

            }

            return new PathPositionData(
                closestSegmentIndexA,
                closestSegmentIndexB,
                Mathf.Sqrt((closestPoint - points[closestSegmentIndexA]).sqrMagnitude / (points[closestSegmentIndexA] - points[closestSegmentIndexB]).sqrMagnitude)
            );

        }

        #endregion

        #endregion

    }

}