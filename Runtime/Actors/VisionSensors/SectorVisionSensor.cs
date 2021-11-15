#if UNITY_EDITOR
#define SENSOR_DEBUG
#endif

using System;
using System.Collections.Generic;

using UnityEngine;

namespace BlackTundra.World.Actors {

#if UNITY_EDITOR
    [CreateAssetMenu(menuName = "Configuration/Actor/Sector Vision Sensor", fileName = "SectorVisionSensor", order = 1001)]
#endif
    [Serializable]
    public sealed class SectorVisionSensor : ScriptableObject, IVisionSensor {

        #region variable

        /// <summary>
        /// Field of view of the <see cref="SectorVisionSensor"/> vision sector.
        /// </summary>
#if UNITY_EDITOR
        [Range(0.1f, 179.9f)]
#endif
        [SerializeField]
        public float theta = 120.0f * Mathf.Deg2Rad;

        /// <summary>
        /// Maximum range that the <see cref="SectorVisionSensor"/> can detect objects at.
        /// </summary>
#if UNITY_EDITOR
        [Min(0.0f)]
#endif
        [SerializeField]
        public float range = 50.0f;

        /// <summary>
        /// Radius of the peripheral vision sphere around the <see cref="SectorVisionSensor"/>.
        /// </summary>
#if UNITY_EDITOR
        [Min(0.0f)]
#endif
        [SerializeField]
        public float preripheralRange = 1.0f;

        /// <summary>
        /// <see cref="LayerMask"/> used for vision.
        /// </summary>
        [SerializeField]
        public LayerMask layerMask = -1;

        #endregion

        #region logic

        #region IsDetectable

        public bool IsDetectable(in Collider collider) {
            if (collider == null) throw new ArgumentNullException(nameof(collider));
            return (collider.gameObject.layer & layerMask) != 0;
        }

        #endregion

        #region QueryVisualSensorFrom

        /// <summary>
        /// An expensive operation to query what colliders the <see cref="SectorVisionSensor"/> can see from <paramref name="point"/>.
        /// Queries every <see cref="Collider"/> near the <see cref="SectorVisionSensor"/> and test if any of those colliders can be seen.
        /// This is an expensive operation.
        /// </summary>
        /// <param name="point">Point in world-space to start the query from.</param>
        /// <param name="direction">Direction to look in.</param>
        /// <returns>Every <see cref="Collider"/> that the <see cref="SectorVisionSensor"/> can see.</returns>
        public IEnumerator<Collider> QueryVisualSensorFrom(Vector3 point, Vector3 direction) {
            Collider[] colliders = Physics.OverlapSphere(point, range, layerMask); // get all colliders in range of the actors visison
            int colliderCount = colliders.Length;
            if (colliderCount == 0) yield break;
            Collider collider;
            for (int i = colliderCount - 1; i >= 0; i--) { // iterate each collider in range
                collider = colliders[i];
                if (IsVisibleFrom(point, direction, collider)) {
                    yield return collider;
                }
            }
        }

        #endregion

        #region IsVisibleFrom

        /// <returns>
        /// Returns <c>true</c> if the <paramref name="collider"/> is visible to the <see cref="SectorVisionSensor"/> when looking from the <paramref name="point"/>
        /// in the specified <paramref name="direction"/>.
        /// </returns>
        public bool IsVisibleFrom(in Vector3 point, Vector3 direction, in Collider collider) {
            if (collider == null) throw new ArgumentNullException(nameof(collider));
            if ((collider.gameObject.layer & layerMask) == 0) return false; // not in visible layer mask
            
            Bounds colliderBounds = collider.bounds;
            Vector3 colliderWorldPosition = colliderBounds.center;
            Vector3 colliderLocalPosition = colliderWorldPosition - point; // convert collider position into local space

            #region sphere check

            /*
             * This check makes sure that the collider is within the sensors view distance.
             * It does this by calculating the square distance in the x-z plane (which is
             * used later) and checking that the x-z square distance is not a very small
             * as this will later cause divide by zero errors.
             * The x-z square distance is then turned into x-y-z squared distance and that
             * value is compared against the square of the view distance.
             * If the x-y-z square distance is more than the square view distance then the
             * collider is outside of the sensor view distance.
             */

            float sqrRange = range * range;
            float sqrXZDistanceToTarget = (colliderLocalPosition.x * colliderLocalPosition.x) + (colliderLocalPosition.z * colliderLocalPosition.z); // get the distance to the target in the xz plane
            if (sqrXZDistanceToTarget > sqrRange) return false; // outside of range

            float sqrDistanceToTarget = sqrXZDistanceToTarget + (colliderLocalPosition.y * colliderLocalPosition.y);
            if (sqrDistanceToTarget > sqrRange) return false; // outside of range
            if (sqrDistanceToTarget < 0.001f) return true; // close enough that the target is almost 0m away, therefore just say it is visible to prevent divide by zero errors later

            #endregion

            #region field-of-view check

            /*
             * This check ensures that the target collider is within the sensors field of view.
             * This is done by finding the forward direction of the sensor in the x, z plane
             * and normalising the vector. This vector is then used as a direction vector in
             * a line equation. Because the sensor position is treated as [0, 0, 0], the line
             * equation is simply "xF", where F is the normalised x-z forward vector and x
             * is a real number. x also equals the distance to the closest point on the line
             * to the collider. This is used with the previously calculated distance to the
             * target collider to form a right angled triangle, where theta can be calculated.
             * Theta is the angle FAT, where F is the forward direction, A is [0, 0, 0] (the
             * sensor position), and T is the local collider position. If the collider is within
             * the sensors field of view, then theta will be less than or equal to the sensors
             * field of view. Otherwise the collider is not within the sensors field of view
             * and is therefore not visible.
             */

            direction = new Vector3(direction.x, 0.0f, direction.z).normalized;

            float distanceToClosestPointOnForwardLine = ((direction.x * colliderLocalPosition.x) + (direction.z * colliderLocalPosition.z)) / ((direction.x * direction.x) + (direction.z * direction.z)); // find the distance from the closest point on the line [A -> F] to the point T
            float xzDistanceToTarget = Mathf.Sqrt(sqrXZDistanceToTarget); // find the x-z distance to the target entity
            //float inverseXZDistanceToTarget = 1.0f / xzDistanceToTarget;

            float theta = Mathf.Abs(Mathf.Acos(Mathf.Abs(distanceToClosestPointOnForwardLine) / xzDistanceToTarget)); // find theta inside right angled FAT triangle
            if (distanceToClosestPointOnForwardLine < 0.0f) theta = Mathf.PI - theta; // correct the angle if it exceeds 90 degrees

            if (Mathf.Abs(theta) > this.theta * 0.5f && sqrDistanceToTarget > preripheralRange * preripheralRange) return false; // outside field of view and peripheral vision

            #endregion

            #region line of sight check

            Transform colliderTransform = collider.transform;

            #region cast to center of bounds

            if (QueryLineOfSight(point, colliderLocalPosition, colliderTransform)) return true; // cast a ray directly into the center of the target (fast check)

            #endregion

            #region find tangent direction

            /*
             * This is the 2D tangent to the line [Sensor -> Target].
             * This is calculated using existing variables to speed up trigonometric calculations.
             * 
             * Explanation:
             * Let O = Origin (0, 0) (Sensor Position)
             *     T = Target Entity Position
             *     f = Sensor forward line (forward direction but in 2D space)
             *     C = Closest Point on line f to point T
             *     theta = Angle TOC
             * Imagine a right angled triangle TOC, then extend line f infinitely. Another right angled
             * triangle can be drawn on the line TC where at point C is the 90deg angle and point T is
             * angle theta, the hypotenuse extends until it intersects line f, let the intersection be
             * point I.
             * The distance [T -> C] can be calculated as the distances [O -> C] and [O -> T] are both
             * known so Pythagoras' Theorem can be used to find [T -> C].
             * This intersection point I is important as the direction [I -> T] is equal to the tangent
             * direction for the line [A -> T]. This is what is calculated below:
             */

            // use Pythagoras' Theorem to find the distance required to be added to the distanceToClosestPointOnForwardLine to get the distance to the tangent intersection
            float tangentIntersectionDistance = distanceToClosestPointOnForwardLine + (Mathf.Sqrt(sqrXZDistanceToTarget - (distanceToClosestPointOnForwardLine * distanceToClosestPointOnForwardLine)) * Mathf.Tan(theta));

            float tangentDirectionX = colliderLocalPosition.x - (direction.x * tangentIntersectionDistance);
            float tangentDirectionZ = colliderLocalPosition.z - (direction.z * tangentIntersectionDistance);

            // normalise:

            float tangentNormaliseCoefficient = 1.0f / Mathf.Sqrt((tangentDirectionX * tangentDirectionX) + (tangentDirectionZ * tangentDirectionZ));
            tangentDirectionX *= tangentNormaliseCoefficient;
            tangentDirectionZ *= tangentNormaliseCoefficient;

            #endregion

            #region cast around center

            /*
            * Imagine the following box:
            * 
            * 100% #######tx#######
            *      #              #
            *      #  tl  tm  tr  #
            *      #              #
            *      #  cl  cm  cr  #
            *      #              #
            *      #  bl  bm  br  #
            *      #              #
            *      ################
            *    0%               100%
            *    
            * where left (l) is 25% horizontally
            *       middle (m) is 50% horizontally
            *       right (r) is 75% horizontally
            *       tx = top eXtended (used top of the collider
            *       
            * this repeats for top (t), center (c) and bottom (b) but vertically
            * 
            * Point cm has already been tested with bounds.center,
            * so the remaining points need to be tested.
            * 
            * This is done to check if any other parts of the sensor is showing.
            * 25% around the edges is ignored.
            * 
            * The order the points are checked are:
            * cm, tx, tm,
            * cr, cl,
            * tr, tl,
            * bm, br, bl
            * 
            * This order is because the most likely place to be able to see the
            * target is the top if them (as their head will likely be showing).
            * The middle center is the next most likely, and the bottom is least
            * likely as the bottom portion may be obstructed by small objects.
            * 
            * The actual box to cast rays is is created as a 2D box tangent to
            * the line [Sensor -> Target] (always vertical, but rotated about y axis).
            * 
            * This is calculated using the tangent calculated in the prior stage
            * to this stage.
            * See above for calculation.
            */

            Vector3 extents = colliderBounds.extents;
            float maxExtentSize = (extents.x > extents.z ? extents.x : extents.z) * 0.5f;

            float dx = tangentDirectionX * maxExtentSize;
            float dy = extents.y * 0.5f;
            float dz = tangentDirectionZ * maxExtentSize;

            return
                QueryLineOfSight(
                    point,
                    new Vector3( // top extended
                        colliderLocalPosition.x,
                        colliderLocalPosition.y + (dy * 1.9f), // 1.9 = almost the top (2.0 would be the top)
                        colliderLocalPosition.z
                    ),
                    colliderTransform
                ) || QueryLineOfSight(
                    point,
                    new Vector3( // top middle
                        colliderLocalPosition.x,
                        colliderLocalPosition.y + dy,
                        colliderLocalPosition.z
                    ),
                    colliderTransform
                ) || QueryLineOfSight(
                    point,
                    new Vector3( // middle right
                        colliderLocalPosition.x + dx,
                        colliderLocalPosition.y,
                        colliderLocalPosition.z + dz
                    ),
                    colliderTransform
                ) || QueryLineOfSight(
                    point,
                    new Vector3( // middle left
                        colliderLocalPosition.x - dx,
                        colliderLocalPosition.y,
                        colliderLocalPosition.z - dz
                    ),
                    colliderTransform
                ) || QueryLineOfSight(
                    point,
                    new Vector3( // top right
                        colliderLocalPosition.x + dx,
                        colliderLocalPosition.y + dy,
                        colliderLocalPosition.z + dz
                    ),
                    colliderTransform
                ) || QueryLineOfSight( // top left
                    point,
                    new Vector3(
                        colliderLocalPosition.x - dx,
                        colliderLocalPosition.y + dy,
                        colliderLocalPosition.z - dz
                    ),
                    colliderTransform
                ) || QueryLineOfSight(
                    point,
                    new Vector3( // bottom middle
                        colliderLocalPosition.x,
                        colliderLocalPosition.y - dy,
                        colliderLocalPosition.z
                    ),
                    colliderTransform
                ) || QueryLineOfSight(
                    point,
                    new Vector3( // bottom right
                        colliderLocalPosition.x + dx,
                        colliderLocalPosition.y - dy,
                        colliderLocalPosition.z + dz
                    ),
                    colliderTransform
                ) || QueryLineOfSight(
                    point,
                    new Vector3( // bottom left
                        colliderLocalPosition.x - dx,
                        colliderLocalPosition.y - dy,
                        colliderLocalPosition.z - dz
                    ),
                    colliderTransform
                );

            #endregion

            #endregion

        }

        #endregion

        #region QueryLineOfSight

        private bool QueryLineOfSight(in Vector3 origin, in Vector3 direction, in Transform transform) {
#if SENSOR_DEBUG
            Debug.DrawLine(origin, origin + direction, Color.cyan); // draw the line of sight
#endif
            return Physics.Raycast(origin, direction, out RaycastHit hit, range, layerMask) && hit.collider.transform == transform;

        }

        #endregion

        #region OnDrawGizmos
#if UNITY_EDITOR
        public void OnDrawGizmos() {
            // add gizmo here
        }
#endif
        #endregion

        #endregion

    }

}