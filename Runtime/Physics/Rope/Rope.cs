using UnityEngine;

namespace BlackTundra.World {

#if UNITY_EDITOR
    [AddComponentMenu("Physics/Rope")]
#endif
    [RequireComponent(typeof(LineRenderer))]
    [DisallowMultipleComponent]
    public sealed class Rope : MonoBehaviour {

        #region variable

        [SerializeField]
        private Transform point1;
        private IPhysicsObject po1;
        private Rigidbody rb1;

        [SerializeField]
        private Transform point2;
        private IPhysicsObject po2;
        private Rigidbody rb2;

        /// <summary>
        /// Additional length to apply to the rope.
        /// </summary>
#if UNITY_EDITOR
        [Min(0.0f)]
#endif
        [SerializeField]
        private float slack = 0.0f;

        /// <summary>
        /// Number of segments on the rope.
        /// </summary>
#if UNITY_EDITOR
        [Min(3)]
#endif
        [SerializeField]
        private int pointCount = 32;

        /// <summary>
        /// Numbers of iterations to calculate per physics update.
        /// </summary>
#if UNITY_EDITOR
        [Min(1)]
#endif
        [SerializeField]
        private int iterationCount = 5;

        /// <summary>
        /// Drag to apply to the rope points.
        /// </summary>
        [SerializeField]
        private float drag = 0.1f;

        /// <summary>
        /// Scalar that scales the amount of force due to tension of the rope to apply to the objects <see cref="point1"/> and <see cref="point2"/>.
        /// </summary>
#if UNITY_EDITOR
        [Min(0.0f)]
#endif
        [SerializeField]
        private float forceScale = 50.0f;

        /// <summary>
        /// Max calulcated length of the rope before tension is applied.
        /// </summary>
        private float maxLength = 0.0f;

        private RopePoint[] points;
        private RopeJoint[] joints;
        private Vector3[] linePoints;
        private LineRenderer lineRenderer;

        #endregion

        #region logic

        #region OnEnable

        private void OnEnable() {
            po1 = point1.GetComponentInParent<IPhysicsObject>();
            if (po1 == null) rb1 = point1.GetComponentInParent<Rigidbody>();
            po2 = point2.GetComponentInParent<IPhysicsObject>();
            if (po2 == null) rb2 = point2.GetComponentInParent<Rigidbody>();
            ConfigureRope();
        }

        #endregion

        #region ConfigureRope

        private void ConfigureRope() {
            // get line renderer:
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();
            // find point 1 and point 2 position:
            Vector3 p1 = point1.position;
            Vector3 p2 = point2.position;
            // create points:
            linePoints = new Vector3[pointCount];
            // initialise first and last point:
            int finalIndex = pointCount - 1;
            linePoints[0] = p1;
            linePoints[finalIndex] = p2;
            // calculate points between the first and last point:
            Vector3 deltaPosition = point2.position - point1.position;
            float indexCoefficient = 1.0f / pointCount;
            for (int i = 1; i < finalIndex; i++) {
                linePoints[i] = p1 + (i * indexCoefficient * deltaPosition); // lerp between p1 and p2
            }
            // create rope points:
            points = new RopePoint[pointCount];
            points[0] = new RopePoint(linePoints[0], false);
            points[finalIndex] = new RopePoint(linePoints[finalIndex], false);
            for (int i = 1; i < finalIndex; i++) {
                points[i] = new RopePoint(linePoints[i], true);
            }
            // create rope joints:
            float pointDistance = deltaPosition.magnitude;
            maxLength = pointDistance + slack;
            float jointLength = indexCoefficient * maxLength;
            joints = new RopeJoint[finalIndex];
            for (int i = 0; i < finalIndex; i++) {
                joints[i] = new RopeJoint(
                    points[i],
                    points[i + 1],
                    jointLength
                );
            }
            // set line renderer points:
            lineRenderer.positionCount = pointCount;
            lineRenderer.SetPositions(linePoints);
        }

        #endregion

        #region FixedUpdate

        private void FixedUpdate() {
            float deltaTime = Time.fixedDeltaTime;
            // update first and last points:
            RopePoint point;
            int finalIndex = points.Length - 1;
            points[0].position = point1.position;
            points[finalIndex].position = point2.position;
            // update points between first and last points:
            float gravity = Environment.gravity * deltaTime * deltaTime;
            Vector3 deltaPosition;
            float dragCoefficient = 1.0f - (drag * deltaTime);
            for (int i = 1; i < finalIndex; i++) {
                point = points[i];
                deltaPosition = point.position - point.lastPosition;
                deltaPosition.y -= gravity;
                point.lastPosition = point.position;
                point.position += deltaPosition * dragCoefficient;
            }
            // update joints:
            RopeJoint joint;
            for (int i = 0; i < iterationCount; i++) {
                for (int j = 0; j < finalIndex; j++) {
                    joint = joints[j];
                    Vector3 jointCentre = joint.centre;
                    Vector3 jointVector = joint.direction * (joint.length * 0.5f);
                    if (joint.p1.simulate) joint.p1.position = jointCentre - jointVector;
                    if (joint.p2.simulate) joint.p2.position = jointCentre + jointVector;
                }
            }
            // find rope length:
            float length = 0.0f;
            for (int i = 0; i < finalIndex; i++) {
                joint = joints[i];
                length += joint.actualLength;
            }
            // update rigidbodies:
            if (length > maxLength) {
                float stretchLength = length - maxLength;
                UpdateRopePhysics(po1, rb1, 1, stretchLength, point1);
                UpdateRopePhysics(po2, rb2, finalIndex - 1, stretchLength, point2);
            }
            // update line renderer:
            for (int i = finalIndex; i >= 0; i--) {
                linePoints[i] = points[i].position;
            }
            lineRenderer.SetPositions(linePoints);
        }

        #endregion

        #region UpdateRigidbody

        private void UpdateRopePhysics(in IPhysicsObject physicsObject, in Rigidbody rigidbody, in int pointIndex, in float stretchLength, in Transform transform) {
            if (physicsObject == null && rigidbody == null) return;
            Vector3 connectionPoint = transform.position;
            Vector3 ropePoint = points[pointIndex].position;
            Vector3 force = forceScale * stretchLength * stretchLength * (ropePoint - connectionPoint).normalized;
            if (physicsObject != null) {
                physicsObject.AddForceAtPosition(force, connectionPoint, ForceMode.Force);
            } else {
                rigidbody.AddForceAtPosition(force, connectionPoint, ForceMode.Force);
            }
        }

        #endregion

        #endregion

    }

}