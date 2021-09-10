using System;
using System.Linq;
using System.Collections.Generic;

using UnityEngine;

using BlackTundra.Foundation.Utility;

namespace BlackTundra.WorldSystem.Paths {

    /// <summary>
    /// <para>
    /// Describes a path that is made of many cubic bezier curves.
    /// A single cubic bezier curve is defined by 4 points:
    /// <list type="bullet">
    /// <item>Anchor 1</item>
    /// <item>Control Point 1</item>
    /// <item>Control Point 2</item>
    /// <item>Anchor 2</item>
    /// </list>
    /// </para>
    /// <seealso cref="Paths.PathSpace"/>
    /// <seealso cref="Paths.ControlPointConstraints"/>
    /// </summary>
    [Serializable]
    public sealed class BezierPath {

        #region variable

        /// <summary>
        /// This list is a buffer of points.
        /// Each four points form a segment of a line:
        /// The first point is the start anchor point on the line.
        /// The second point is the smoothing point used by the start anchor point.
        /// The third point is the smoothing point used by the end anchor point of the segment.
        /// The fourth (and final) point is the end anchor point of the segment.
        /// These four points can then be used to form a cubic curve which is used to construct the actual segment.
        /// </summary>
        [SerializeField] private /*readonly*/ List<Vector3> points;

        /// <summary>
        /// Stores if the path is closed or not.
        /// </summary>
        [SerializeField] private bool closed;

        /// <summary>
        /// Space that the path exists in.
        /// </summary>
        [SerializeField] private PathSpace space;

        /// <summary>
        /// Constraints that control points follow.
        /// </summary>
        [SerializeField] private ControlPointConstraints controlPointConstraints;

        /// <summary>
        /// When using automatic control point placement, this value scales how far apart controls are placed from eachother.
        /// </summary>
        [SerializeField] private float autoControlLength = 0.3f;

        /// <summary>
        /// Stores if the bounds of the path need to be updated.
        /// </summary>
        private bool updateBounds;

        /// <summary>
        /// Bounds of the bezier path.
        /// </summary>
        private Bounds _bounds;

        /// <summary>
        /// Normal angle per anchor point.
        /// </summary>
        [SerializeField] private /*readonly*/ List<float> anchorNormalAngles;

        /// <summary>
        /// Global angle that all normal vectors are rotated by.
        /// This is only relevant for paths in 3D space.
        /// </summary>
        [SerializeField] private float globalNormalAngle;

        /// <summary>
        /// When true, the normal directions are flipped.
        /// </summary>
        [SerializeField] private bool flipNormals;

        #endregion

        #region property

        /// <summary>
        /// Allows access to a point in the path.
        /// </summary>
        /// <param name="index">Index of the point in the path.</param>
        /// <returns>Point in the path corresponding to the provided index.</returns>
        public Vector3 this[in int index] {

            get => points[index];
            set { // this setter should not be used internally when looping points since NotifyPathModifed will be called every time
                points[index] = value;
                NotifyPathModified();
            }

        }

        /// <summary>
        /// Total number of points in the path.
        /// </summary>
        public int Length => points.Count;

        /// <summary>
        /// Total number of anchor points in the path.
        /// </summary>
        public int AnchorPointCount => closed ? (points.Count / 3) : ((points.Count + 2) / 3);

        /// <summary>
        /// Total number of segments in the path.
        /// </summary>
        public int SegmentCount => points.Count / 3;

        /// <summary>
        /// Bounds of the path.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public Bounds bounds {

            get {

                if (updateBounds) {
                    _bounds = CalculateBounds();
                    updateBounds = false;
                }
                return _bounds;

            }

        }
#pragma warning restore IDE1006 // naming styles

        public PathSpace PathSpace {

            get => space;
            set {

                if (value == space) return; // already in desired space
                PathSpace previousPathSpace = space;
                space = value;
                RecalculatePathSpace(previousPathSpace);

            }

        }

        public bool IsClosed {

            get => closed;
            set {

                if (value == closed) return; // already in desired state
                closed = value;
                UpdateClosedState();

            }

        }

        /// <summary>
        /// Constraints that each control point obeys.
        /// </summary>
        public ControlPointConstraints ControlPointConstraints {

            get => controlPointConstraints;
            set {

                if (value == ControlPointConstraints) return; // already in desired state

                controlPointConstraints = value;
                if (controlPointConstraints == ControlPointConstraints.Automatic) {
                    AutoSetAllControlPoints();
                    NotifyPathModified();
                }

            }

        }

        /// <summary>
        /// When using automatic control point placement, this value scales how far apart anchor points are placed.
        /// </summary>
        public float AutoControlLength {

            get => autoControlLength;
            set {

                value = Mathf.Max(value, 0.01f); // clamp value
                if (autoControlLength == value) return;
                autoControlLength = value;
                AutoSetAllControlPoints();
                NotifyPathModified();

            }

        }

        /// <summary>
        /// Flips the normal vectors 180 degrees.
        /// </summary>
        public bool FlipNormals {

            get => flipNormals;
            set {
                if (value == flipNormals) return;
                flipNormals = value;
                NotifyPathModified();
            }

        }

        /// <summary>
        /// Global angle that all normal vectors are rotated by.
        /// This is only relevant in 3D space.
        /// </summary>
        public float GlobalNormalsAngle {

            get => globalNormalAngle;
            set {
                if (value == globalNormalAngle) return;
                globalNormalAngle = value;
                NotifyPathModified();
            }

        }

        #endregion

        #region event

        /// <summary>
        /// Called when the bezier path is modified.
        /// </summary>
        public event Action OnModified;

        #endregion

        #region constructor

        /// <summary>
        /// Creates a bezier path from a list of 2D points.
        /// </summary>
        /// <param name="points">2D points to create the bezier path from.</param>
        /// <param name="closed">When true, the end of the path will link back with the first point.</param>
        /// <param name="space">Determines if the path is in 3D space or clamped to the XY/XZ plane.</param>
        public BezierPath(in IEnumerable<Vector2> points, in bool closed = false, in PathSpace space = PathSpace.xy) : this(points.Select(v => new Vector3(v.x, v.y)), closed, space) { }

        /// <summary>
        /// Creates a bezier path from a list of Transform components.
        /// </summary>
        /// <param name="transforms">Transform components to create the bezier path from.</param>
        /// <param name="closed">When true, the end of the path will link back with the first point.</param>
        /// <param name="space">Determines if the path is in 3D space or clamped to the XY/XZ plane.</param>
        public BezierPath(in IEnumerable<Transform> transforms, in bool closed = false, in PathSpace space = PathSpace.xyz) : this(transforms.Select(t => t.position), closed, space) { }

        /// <summary>
        /// Creates a bezier path from a single center point.
        /// </summary>
        /// <param name="center">Center of the bezier path.</param>
        /// <param name="closed">When true, the end of the path will link back with the first point.</param>
        /// <param name="space">Determines if the path is in 3D space or clamped to the XY/XZ plane.</param>
        public BezierPath(in Vector3 center, in bool closed = false, in PathSpace space = PathSpace.xyz) {

            Vector3 direction = space == PathSpace.xz ? Vector3.forward : Vector3.up;
            points = new List<Vector3> {
                new Vector3(
                    center.x - 2.0f,
                    center.y,
                    center.z
                ),
                new Vector3(
                    center.x - 1.0f,
                    center.y + (direction.y * 0.5f),
                    center.z + (direction.z * 0.5f)
                ),
                new Vector3(
                    center.x + 1.0f,
                    center.y - (direction.y * 0.5f),
                    center.z - (direction.z * 0.5f)
                ),
                new Vector3(
                    center.x + 2.0f,
                    center.y,
                    center.z
                )
            };
            anchorNormalAngles = new List<float>() { 0.0f, 0.0f };
            controlPointConstraints = ControlPointConstraints.Automatic;

            this.closed = closed;
            this.space = space;

        }

        /// <summary>
        /// Creates a bezier path from a list of 3D points.
        /// </summary>
        /// <param name="points">3D points to create the bezier path from.</param>
        /// <param name="closed">When true, the end of the path will link back with the first point.</param>
        /// <param name="space">Determines if the path is in 3D space or clamped to the XY/XZ plane.</param>
        public BezierPath(in IEnumerable<Vector3> points, in bool closed = false, in PathSpace space = PathSpace.xyz) {

            Vector3[] pointBuffer = points.ToArray();
            if (pointBuffer.Length < 2) throw new ArgumentException("BezierPath requires at least 2 anchor points.");

            controlPointConstraints = ControlPointConstraints.Automatic;

            this.points = new List<Vector3>() { pointBuffer[0], Vector3.zero, Vector3.zero, pointBuffer[1] };
            anchorNormalAngles = new List<float>(new float[] { 0.0f, 0.0f });

            for (int i = 2; i < pointBuffer.Length; i++) {
                AddLast(pointBuffer[i]);
                anchorNormalAngles.Add(0.0f);
            }

            this.space = space;
            this.closed = closed; // this must be set after using AddLast since the method won't allow you to add to the end if the path is closed since it has no end

        }

        #endregion

        #region logic

        #region NotifyPathModified

#if UNITY_EDITOR
        public
#else
        private
#endif
        void NotifyPathModified() {

            updateBounds = true;
            OnModified?.Invoke();

        }

        #endregion

        #region RecalculatePathSpace

        private void RecalculatePathSpace(in PathSpace previousPathSpace) {

            if (previousPathSpace == PathSpace.xyz) { // was in 3D space

                Vector3 boundsSize = bounds.size;
                float minBoundsSize = Mathf.Min(boundsSize.x, boundsSize.y, boundsSize.z);

                if (space == PathSpace.xy) {

                    for (int i = 0; i < points.Count; i++)
                        points[i] = new Vector3(
                            minBoundsSize == boundsSize.x ? points[i].z : points[i].x,
                            minBoundsSize == boundsSize.y ? points[i].z : points[i].y
                        );

                } else if (space == PathSpace.xz) {

                    for (int i = 0; i < points.Count; i++)
                        points[i] = new Vector3(
                            minBoundsSize == boundsSize.x ? points[i].y : points[i].x,
                            0.0f,
                            minBoundsSize == boundsSize.z ? points[i].y : points[i].z
                        );

                }

            } else { // wasn't in 3D space

                if (space == PathSpace.xy) {

                    for (int i = 0; i < points.Count; i++)
                        points[i] = new Vector3(points[i].x, points[i].z);

                } else if (space == PathSpace.xz) {

                    for (int i = 0; i < points.Count; i++)
                        points[i] = new Vector3(points[i].x, 0.0f, points[i].y);

                }

            }

            NotifyPathModified();

        }

        #endregion

        #region UpdateClosedState

        private void UpdateClosedState() {

            if (closed) {

                int lastIndex = points.Count - 1;
                Vector3 lastAnchorSecondControlPoint;
                Vector3 firstAnchorSecondControl;
                if (controlPointConstraints != ControlPointConstraints.Mirrored && controlPointConstraints != ControlPointConstraints.Automatic) {

                    float halfDistanceBetweenAnchors = Vector3.Distance(points[lastIndex], points[0]) * 0.5f;
                    lastAnchorSecondControlPoint = points[lastIndex] + ((points[lastIndex] - points[lastIndex - 1]).normalized * halfDistanceBetweenAnchors * 0.5f);
                    firstAnchorSecondControl = points[0] + ((points[0] - points[1]).normalized * halfDistanceBetweenAnchors);

                } else {

                    lastAnchorSecondControlPoint = (points[lastIndex] * 2.0f) - points[lastIndex - 1];
                    firstAnchorSecondControl = (points[0] * 2.0f) - points[1];

                }

                points.Add(lastAnchorSecondControlPoint);
                points.Add(firstAnchorSecondControl);

            } else points.RemoveRange(points.Count - 2, 2);

            if (controlPointConstraints == ControlPointConstraints.Automatic)
                AutoSetStartEndControlPoints();

            NotifyPathModified();

        }

        #endregion

        #region AddFirst

        public void AddFirst(in Vector3 anchorPosition) {

            if (closed) return; // if the path is closed, a point cannot be added to the end

            Vector3 anchorOffset = points[0] - points[1];
            if (controlPointConstraints != ControlPointConstraints.Mirrored && controlPointConstraints != ControlPointConstraints.Automatic) {

                float distanceToFirstAnchor = Vector3.Distance(points[0], anchorPosition);
                anchorOffset = anchorOffset.normalized * distanceToFirstAnchor * 0.5f;

            }

            Vector3 previousControlPoint = points[0] + anchorOffset;
            Vector3 nextControlPoint = (anchorPosition + previousControlPoint) * 0.5f;

            points.Insert(0, anchorPosition);
            points.Insert(1, nextControlPoint);
            points.Insert(2, previousControlPoint);

            if (controlPointConstraints == ControlPointConstraints.Automatic)
                AutoSetAffectedControlPoints(0);

            NotifyPathModified();

        }

        #endregion

        #region AddLast

        public void AddLast(in Vector3 anchorPosition) {

            if (closed) return; // if the path is closed, a point cannot be added to the end

            int lastAnchorIndex = points.Count - 1; // get the last anchor point index
            Vector3 anchorOffset = points[lastAnchorIndex] - points[lastAnchorIndex - 1]; // get offset from last anchor point to the previous control point
            if (controlPointConstraints != ControlPointConstraints.Mirrored && controlPointConstraints != ControlPointConstraints.Automatic) { // set position for new control to be aligned with its counterpart

                float distanceToLastAnchor = Vector3.Distance(anchorPosition, points[lastAnchorIndex]); // get the distance to the last anchor point
                anchorOffset = anchorOffset.normalized * distanceToLastAnchor * 0.5f; // set the length to half the distance from the previous anchor to the new one

            }

            Vector3 previousControlPoint = points[lastAnchorIndex] + anchorOffset; // calculate the position of the control point before the new anchor point
            Vector3 nextControlPoint = (anchorPosition + previousControlPoint) * 0.5f; // use the last control point position to find the new control point position

            points.Add(previousControlPoint);
            points.Add(nextControlPoint);
            points.Add(anchorPosition);
            anchorNormalAngles.Add(anchorNormalAngles[anchorNormalAngles.Count - 1]);

            if (controlPointConstraints == ControlPointConstraints.Automatic)
                AutoSetAffectedControlPoints(points.Count - 1);

            NotifyPathModified();

        }

        #endregion

        #region NormalizeIndex

        /// <summary>
        /// Ensures an index is in range. This accepts an index in the range -points.Count to int.MaxValue.
        /// </summary>
        /// <param name="index">Index to ensure is in range.</param>
        /// <returns>Index that is in range.</returns>
        private int NormalizeIndex(in int index) => (index + points.Count) % points.Count;

        #endregion

        #region GetSegment

        /// <summary>
        /// Gets the points in a section of the path.
        /// </summary>
        /// <param name="index">Index of the segment.</param>
        /// <returns>Array of 4 Vector3 points in the segment.</returns>
        public Vector3[] GetSegment(int index) {

            index = Mathf.Clamp(index, 0, SegmentCount - 1) * 3;
            return new Vector3[] {
                points[index],
                points[index + 1],
                points[index + 2],
                points[NormalizeIndex(index + 3)]
            };

        }

        #endregion

        #region RemoveSegment

        /// <summary>
        /// Removes a segment of the path by the index of the anchor associated with the segment.
        /// </summary>
        /// <param name="index">Index of the anchor associated with the segment.</param>
        public void RemoveSegment(in int index) {

            int segmentCount = SegmentCount;
            if (segmentCount <= 2 && (closed || segmentCount <= 1)) return; // don't allow a segment to be deleted if its the last one remaining (or if only two segments in a closed path)

            if (index == 0) {

                if (closed) points[points.Count - 1] = points[2];
                points.RemoveRange(0, 3);

            } else if (index == points.Count - 1 && !closed)
                points.RemoveRange(index - 2, 3);
            else
                points.RemoveRange(index - 1, 3);

            anchorNormalAngles.RemoveAt(index / 3);

            if (controlPointConstraints == ControlPointConstraints.Automatic)
                AutoSetAllControlPoints();

            NotifyPathModified();

        }

        #endregion

        #region SplitSegment

        public void SplitSegment(in Vector3 anchorPosition, in int segmentIndex, float splitPosition) {

            int pointIndex = segmentIndex * 3;
            splitPosition = Mathf.Clamp01(splitPosition);
            if (controlPointConstraints == ControlPointConstraints.Automatic) {
                points.InsertRange(
                    pointIndex + 2,
                    new Vector3[] {
                        Vector3.zero,
                        anchorPosition,
                        Vector3.zero
                    }
                );
                AutoSetAffectedControlPoints(pointIndex + 3);
            } else { // split curve to find where the control points can be inserted to affect the curve the least

                Vector3[] segment = GetSegment(segmentIndex);
                Vector3[][] splitSegment = MathsUtility.SplitBezierCurve(segment[0], segment[1], segment[2], segment[3], splitPosition);
                int newAnchorIndex = pointIndex + 3;
                MovePoint(newAnchorIndex - 2, splitSegment[0][1], true);
                MovePoint(newAnchorIndex + 2, splitSegment[1][2], true);
                MovePoint(newAnchorIndex, anchorPosition, true);

                if (controlPointConstraints == ControlPointConstraints.Mirrored) {
                    Vector3 deltaPosition = splitSegment[1][1] - anchorPosition;
                    float averageDistance = 0.5f * (Vector3.Distance(splitSegment[0][2], anchorPosition) + deltaPosition.magnitude);
                    MovePoint(newAnchorIndex + 1, anchorPosition + (deltaPosition.normalized * averageDistance), true);
                }

            }

            int angleCount = anchorNormalAngles.Count;
            int newAnchorAngleIndex = (segmentIndex + 1) % angleCount;
            float previousAngle = anchorNormalAngles[segmentIndex];
            float nextAngle = anchorNormalAngles[newAnchorAngleIndex];
            float splitAngle = Mathf.LerpAngle(previousAngle, nextAngle, splitPosition);
            anchorNormalAngles.Insert(newAnchorAngleIndex, splitAngle);

            NotifyPathModified();

        }

        #endregion

        #region MovePoint

        public void MovePoint(in int index, Vector3 position, in bool suppressEvents = false) {

            if (space == PathSpace.xy) position.z = 0.0f;
            else if (space == PathSpace.xz) position.y = 0.0f;

            bool isAnchorPoint = index % 3 == 0;
            if (!isAnchorPoint && controlPointConstraints == ControlPointConstraints.Automatic) return; // don't allow modification of control points if control points are automatically positioned

            Vector3 deltaPosition = position - points[index];
            points[index] = position;

            if (controlPointConstraints == ControlPointConstraints.Automatic) // automatically set points
                AutoSetAffectedControlPoints(index);
            else {

                if (isAnchorPoint) { // move control points with anchor point

                    if (closed) {

                        points[NormalizeIndex(index + 1)] += deltaPosition;
                        points[NormalizeIndex(index - 1)] += deltaPosition;

                    } else {

                        if (index + 1 < points.Count) points[index + 1] += deltaPosition;
                        if (index > 1) points[index - 1] += deltaPosition;

                    }

                } else if (controlPointConstraints != ControlPointConstraints.None) {

                    bool nextPointIsAnchor = (index + 1) % 3 == 0;
                    int controlPointIndex = nextPointIsAnchor ? (index + 2) : (index - 2);

                    if (closed || (controlPointIndex >= 0 && controlPointIndex < points.Count)) {

                        int anchorIndex = nextPointIsAnchor ? (index + 1) : (index - 1);
                        if (closed) { // loop is closed, loop indexes
                            controlPointIndex = NormalizeIndex(controlPointIndex);
                            anchorIndex = NormalizeIndex(anchorIndex);
                        }

                        float distanceFromAnchor;
                        if (controlPointConstraints == ControlPointConstraints.Aligned)
                            distanceFromAnchor = Vector3.Distance(points[anchorIndex], points[controlPointIndex]);
                        else if (controlPointConstraints == ControlPointConstraints.Mirrored)
                            distanceFromAnchor = Vector3.Distance(points[anchorIndex], points[index]);
                        else
                            distanceFromAnchor = 0.0f;

                        Vector3 direction = (points[anchorIndex] - position).normalized;
                        points[controlPointIndex] = points[anchorIndex] + (direction * distanceFromAnchor);

                    }

                }

            }

            if (!suppressEvents) NotifyPathModified();

        }

        #endregion

        #region GetAnchorNormalAngle

        /// <summary>
        /// Get the desired angle of the normal vector at a particular anchor.
        /// </summary>
        public float GetAnchorNormalAngle(in int anchorIndex) => anchorNormalAngles[anchorIndex];

        #endregion

        #region SetAnchorNormalAngle

        public void SetAnchorNormalAngle(in int anchorIndex, float angle) {

            angle = (angle + 360.0f) % 360.0f; // wrap the angle
            if (anchorNormalAngles[anchorIndex] != angle) {
                anchorNormalAngles[anchorIndex] = angle;
                NotifyPathModified();
            }

        }

        #endregion

        #region CalculateBounds

        public Bounds CalculateBounds(in Transform transform = null) {

            //if (transform == null) throw new ArgumentNullException("transform");

            MinMaxVector3 minMax = new MinMaxVector3();

            int segmentCount = SegmentCount;
            for (int i = 0; i < segmentCount; i++) {

                Vector3[] points = GetSegment(i);
                if (transform != null) for (int j = 0; j < points.Length; j++) points[j] = transform.TransformPoint(points[j], space);

                minMax.Evaluate(points[0]);
                minMax.Evaluate(points[3]);

                IEnumerable<float> turningPoints = MathsUtility.FindBezierTurningPoints(
                    points[0], points[1], points[2], points[3]
                );
                foreach (float turningPoint in turningPoints) minMax.Evaluate(MathsUtility.EvaluateBezierCurve(points[0], points[1], points[2], points[3], turningPoint));

            }

            return minMax.ToBounds();

        }

        #endregion

        #region AutoSetAllControlPoints

        /// <summary>
        /// Calculates positions for all control points for each anchor.
        /// </summary>
        private void AutoSetAllControlPoints() {

            if (AnchorPointCount > 2) for (int i = 0; i < points.Count; i += 3) AutoSetAnchorControlPoints(i);
            AutoSetStartEndControlPoints();

        }

        #endregion

        #region AutoSetAffectedControlPoints

        private void AutoSetAffectedControlPoints(in int anchorIndex) {

            for (int i = anchorIndex - 3; i <= anchorIndex + 3; i += 3)
                if (closed || (i >= 0 && i < points.Count)) AutoSetAnchorControlPoints(NormalizeIndex(i));
            AutoSetStartEndControlPoints();

        }

        #endregion

        #region AutoSetAnchorControlPoints

        private void AutoSetAnchorControlPoints(in int anchorIndex) {

            Vector3 anchorPosition = points[anchorIndex];
            Vector3 direction = Vector3.zero;
            float[] neighbourDistances = new float[2];

            if (closed || anchorIndex - 3 >= 0) {

                Vector3 offset = points[NormalizeIndex(anchorIndex - 3)] - anchorPosition;
                direction += offset.normalized;
                neighbourDistances[0] = offset.magnitude;

            }

            if (closed || anchorIndex + 3 >= 0) {

                Vector3 offset = points[NormalizeIndex(anchorIndex + 3)] - anchorPosition;
                direction -= offset.normalized;
                neighbourDistances[1] = -offset.magnitude;

            }

            direction.Normalize(); // normalize the direction before using

            for (int i = 0; i < 2; i++) {

                int controlIndex = anchorIndex + i * 2 - 1;
                if (closed || (controlIndex >= 0 && controlIndex < points.Count))
                    points[NormalizeIndex(controlIndex)] = anchorPosition + (direction * neighbourDistances[i] * 0.5f);

            }

        }

        #endregion

        #region AutoSetStartEndControlPoints

        private void AutoSetStartEndControlPoints() {

            if (closed) return;

            points[1] = (points[0] + points[2]) * 0.5f;
            int pointCount = points.Count;
            points[pointCount - 2] = (points[pointCount - 1] + points[pointCount - 3]) * 0.5f;

        }
        #endregion

        #region SplitByAngleError

        public VertexPathData SplitByAngleError(float thresholdAngleError, float minimumVertexDistance, float accuracy) {

            if (thresholdAngleError < 0.025f) thresholdAngleError = 0.025f;//throw new ArgumentException("thresholdAngleError too small.");
            if (thresholdAngleError > 180.0f) thresholdAngleError = 180.0f; //throw new ArgumentException("thresholdAngleError too large.");
            if (minimumVertexDistance < 0.01f) minimumVertexDistance = 0.01f; //throw new ArgumentException("minimumVertexDistance too small.");
            if (accuracy < 0.1f) accuracy = 0.1f; //throw new ArgumentException("Accuracy too small.");

            Vector3 previousPoint = points[0];
            Vector3 lastAddedPoint = previousPoint;

            int segmentCount = SegmentCount;

            List<VertexData> vertexData = new List<VertexData> {
                new VertexData(
                    previousPoint,
                    MathsUtility.EvaluateBezierCurveDerivative(
                        GetSegment(0),
                        0.0f
                    ).normalized,
                    0.0f
                )
            };
            int[] anchorVertexMap = new int[segmentCount];

            float totalPathLength = 0.0f;
            float distanceSinceLastVertex = 0.0f;

            for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++) {

                Vector3[] segment = GetSegment(segmentIndex);
                float estimatedSegmentLength = MathsUtility.EstimateBezierCurveLength(segment);
                int divisions = Mathf.CeilToInt(estimatedSegmentLength * accuracy);
                float increment = 1.0f / divisions;

                for (float t = increment; t <= 1.0f; t += increment) {

                    bool isLastPointOnPath = (segmentIndex == segmentCount - 1 && t + increment > 1.0f);
                    if (isLastPointOnPath) t = 1.0f;

                    Vector3 point = MathsUtility.EvaluateBezierCurve(segment, t);
                    Vector3 nextPoint = MathsUtility.EvaluateBezierCurve(segment, t + increment);

                    float localAngle = 180.0f - MathsUtility.MinAngle(previousPoint, point, nextPoint);
                    float angleFromPreviousVertex = 180.0f - MathsUtility.MinAngle(lastAddedPoint, point, nextPoint);
                    float angleError = Mathf.Max(localAngle, angleFromPreviousVertex);

                    if ((angleError > thresholdAngleError && distanceSinceLastVertex >= minimumVertexDistance) || isLastPointOnPath) {

                        totalPathLength += Vector3.Distance(lastAddedPoint, point);
                        vertexData.Add(
                            new VertexData(
                                point,
                                MathsUtility.EvaluateBezierCurveDerivative(segment, t).normalized,
                                totalPathLength
                            )
                        );
                        distanceSinceLastVertex = 0;
                        lastAddedPoint = point;

                    } else {

                        distanceSinceLastVertex += Vector3.Distance(point, previousPoint);

                    }

                    previousPoint = point;

                }

                anchorVertexMap[segmentIndex] = vertexData.Count - 1;

            }

            return new VertexPathData(
                vertexData,
                anchorVertexMap,
                totalPathLength
            );

        }

        #endregion

        #region SplitByDistance

        public VertexPathData SplitByDistance(float spacing, float accuracy) {

            if (spacing < 0.01f) spacing = 0.01f; //throw new ArgumentException("spacing too small.");
            if (accuracy < 0.1f) accuracy = 0.1f; //throw new ArgumentException("accuracy too small.");

            Vector3 previousPoint = points[0];
            Vector3 lastAddedPoint = previousPoint;

            int segmentCount = SegmentCount;

            List<VertexData> vertexData = new List<VertexData>() {
                new VertexData(
                    previousPoint,
                    MathsUtility.EvaluateBezierCurveDerivative(
                        GetSegment(0),
                        0.0f
                    ),
                    0.0f
                )
            };
            int[] anchorVertexMap = new int[segmentCount];

            float totalPathLength = 0.0f;
            float distanceSinceLastVertex = 0.0f;

            for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++) {

                Vector3[] segment = GetSegment(segmentIndex);
                float estimatedSegmentLength = MathsUtility.EstimateBezierCurveLength(segment);
                int divisions = Mathf.CeilToInt(estimatedSegmentLength * accuracy);
                float increment = 1.0f / divisions;
                for (float t = increment; t <= 1.0f; t += increment) {

                    bool isLastPointOnPath = (segmentIndex == segmentCount - 1 && t + increment > 1.0f);
                    if (isLastPointOnPath) t = 1.0f;

                    Vector3 point = MathsUtility.EvaluateBezierCurve(segment, t);
                    distanceSinceLastVertex += Vector3.Distance(point, previousPoint);

                    if (distanceSinceLastVertex > spacing) {

                        float overshootDistance = distanceSinceLastVertex - spacing;
                        point += (previousPoint - point).normalized * overshootDistance; // if vertices are too far apart, go back by the amount overshot by
                        t -= increment;

                    }

                    if (distanceSinceLastVertex >= spacing || isLastPointOnPath) {

                        totalPathLength += Vector3.Distance(point, lastAddedPoint);
                        vertexData.Add(
                            new VertexData(
                                point,
                                MathsUtility.EvaluateBezierCurveDerivative(segment, t).normalized,
                                totalPathLength
                            )
                        );
                        distanceSinceLastVertex = 0.0f;
                        lastAddedPoint = point;

                    }

                    previousPoint = point;

                }

                anchorVertexMap[segmentIndex] = vertexData.Count - 1;

            }

            return new VertexPathData(
                vertexData,
                anchorVertexMap,
                totalPathLength
            );

        }

        #endregion

        #region ResetNormalAngles

        /// <summary>
        /// Resets global and anchor normal angles to zero.
        /// </summary>
        public void ResetNormalAngles() {

            for (int i = 0; i < anchorNormalAngles.Count; i++) anchorNormalAngles[i] = 0.0f;
            globalNormalAngle = 0.0f;
            NotifyPathModified();

        }

        #endregion

        #endregion

    }

}