using System;
using System.Collections.Generic;
using System.Globalization;

using BlackTundra.Foundation;
using BlackTundra.Foundation.Collections.Generic;
using BlackTundra.Foundation.Utility;
using BlackTundra.World.Audio;

using UnityEngine;
using UnityEngine.AI;

using Console = BlackTundra.Foundation.Console;
using Object = UnityEngine.Object;

namespace BlackTundra.World.Actors {

    /// <summary>
    /// Class that controls a <see cref="UnityEngine.AI.NavMeshAgent"/>, which can be told to go to target positions or colliders.
    /// </summary>
    [DisallowMultipleComponent] // multiple actors cannot inhibit the same object
    [RequireComponent(typeof(NavMeshAgent))] // required for navigation, pathfinding, and some properties
#if UNITY_EDITOR
    [AddComponentMenu("AI/Actor")]
#endif
    public sealed class Actor : MonoBehaviour, IDamageable, IDirectable {

        #region constant

        /// <summary>
        /// Expand and shrink size of the <see cref="ActorBuffer"/>.
        /// </summary>
        private const int ActorBufferExpandSize = 16;

        /// <summary>
        /// Maximum number of <see cref="Actor"/> instances that will be updated in a single batch.
        /// </summary>
        private const int ActorBufferBatchUpdateSize = 16;

        /// <summary>
        /// <see cref="PackedBuffer{T}"/> containing all enabled (active) <see cref="Actor"/> instances.
        /// </summary>
        private static readonly PackedBuffer<Actor> ActorBuffer = new PackedBuffer<Actor>(ActorBufferExpandSize);

        #endregion

        #region variable

        /// <inheritdoc cref="Profile"/>
        [SerializeField] internal ActorProfile profile = null;

        /// <inheritdoc cref="NavMeshAgent"/>
        private NavMeshAgent agent = null;

        /// <summary>
        /// <see cref="CapsuleCollider"/> used for collisions.
        /// </summary>
#if UNITY_EDITOR
        new
#endif
        private CapsuleCollider collider = null;

        /// <inheritdoc cref="TargetCollider"/>
        private Collider targetCollider = null;

        /// <inheritdoc cref="TargetPosition"/>
        private Vector3 targetPosition = Vector3.zero;

        /// <inheritdoc cref="TargetSpeed"/>
        private Vector3 targetSpeed = Vector3.forward;

        /// <summary>
        /// Used to track if the target has changed or not.
        /// </summary>
        private bool updateTarget = false;

        /// <summary>
        /// Value of <see cref="Time.time"/> last time <see cref="InternalUpdate"/> was invoked.
        /// </summary>
        private float currentTime = 0.0f;

        /// <summary>
        /// Value of <see cref="currentTime"/> before it was last updated.
        /// </summary>
        /// <seealso cref="currentTime"/>
        private float lastTime = 0.0f;

        /// <inheritdoc cref="Behaviour"/>
        private ActorBehaviour behaviour = null;

        /// <summary>
        /// Stores a reference to the next <see cref="ActorBehaviour"/> to set. This is only used if a new <see cref="ActorBehaviour"/>
        /// is assigned while the current <see cref="behaviour"/> is not yet initialised (<see cref="behaviourLock"/>) is <c>true</c>.
        /// </summary>
        private ActorBehaviour queuedBehaviour = null;

        /// <summary>
        /// When <c>true</c>, the <see cref="behaviour"/> cannot be changed.
        /// </summary>
        private bool behaviourLock = false;

        /// <summary>
        /// Index that the next batch should start at.
        /// </summary>
        private static int batchStartIndex = ActorBufferBatchUpdateSize;

        #endregion

        #region property

        /// <inheritdoc cref="NavMeshAgent.speed"/>
#pragma warning disable IDE1006 // naming styles
        public float speed {
#pragma warning restore IDE1006 // naming styles
            get => agent.speed;
            set => agent.speed = value;
        }

        /// <summary>
        /// Coefficient used to control the maximum speed relative to the base speed of the <see cref="Actor"/>.
        /// </summary>
        public float SpeedCoefficient {
            get => agent.speed / profile.baseSpeed;
            set => agent.speed = profile.baseSpeed * value;
        }

        /// <inheritdoc cref="NavMeshAgent.acceleration"/>
#pragma warning disable IDE1006 // naming styles
        public float acceleration {
#pragma warning restore IDE1006 // naming styles
            get => agent.acceleration;
            set => agent.acceleration = value;
        }

        /// <summary>
        /// Coefficient used to control the acceleration relative to the base speed of the <see cref="Actor"/>.
        /// </summary>
        public float AccelerationCoefficient {
            get => agent.acceleration / profile.baseAcceleration;
            set => agent.acceleration = profile.baseAcceleration * value;
        }

        /// <inheritdoc cref="NavMeshAgent.angularSpeed"/>
#pragma warning disable IDE1006 // naming styles
        public float angularSpeed {
#pragma warning restore IDE1006 // naming styles
            get => agent.angularSpeed;
            set => agent.angularSpeed = value;
        }

        /// <summary>
        /// Height of the <see cref="Actor."/>
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public float height {
#pragma warning restore IDE1006 // naming styles
            get => agent.height;
            set {
                agent.height = value;
                if (collider != null) {
                    float height = agent.height;
                    collider.height = height;
                    collider.center = new Vector3(0.0f, height * 0.5f, 0.0f);
                }
            }
        }

        /// <summary>
        /// Radius of the <see cref="Actor."/>
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public float radius {
#pragma warning restore IDE1006 // naming styles
            get => agent.radius;
            set {
                agent.radius = value;
                if (collider != null) collider.radius = agent.radius;
            }
        }

        /// <summary>
        /// Coefficient used to control the maximum angular speed relative to the base angular speed of the <see cref="Actor"/>.
        /// </summary>
        public float AngularSpeedCoefficient {
            get => agent.angularSpeed / profile.baseAngularSpeed;
            set => agent.angularSpeed = profile.baseAngularSpeed * value;
        }

        /// <summary>
        /// <see cref="ActorProfile"/> that the <see cref="Actor"/> uses for its basic properties and behaviour.
        /// </summary>
        /// <seealso cref="NavMeshAgent"/>
        public ActorProfile Profile => profile;

        /// <summary>
        /// <see cref="UnityEngine.AI.NavMeshAgent"/> component used by the <see cref="Actor"/> for navigation and pathfinding.
        /// </summary>
        /// <seealso cref="UnityEngine.AI.NavMeshAgent"/>
        /// <seealso cref="Profile"/>
        public NavMeshAgent NavMeshAgent => agent;

        /// <summary>
        /// <see cref="ActorBehaviour"/> that implements and thus determinds how the <see cref="Actor"/> will behave when it
        /// receives different inputs.
        /// </summary>
        /// <seealso cref="SetBehaviour(in ActorBehaviour)"/>
        public ActorBehaviour Behaviour {
            get => behaviour;
            set => SetBehaviour(value);
        }

        /// <summary>
        /// Target <see cref="Collider"/> that the <see cref="Actor"/> will target.
        /// </summary>
        /// <remarks>
        /// When <see cref="TargetCollider"/> is not <c>null</c>, the <see cref="TargetPosition"/> will be updated with the last
        /// known position of the <see cref="TargetCollider"/>. To test if the <see cref="Actor"/> has a target collider, either
        /// check if this property is not <c>null</c> or query <see cref="HasTargetCollider"/>.
        /// </remarks>
        /// <seealso cref="TargetPosition"/>
        /// <seealso cref="HasTargetCollider"/>
        /// <seealso cref="SetTargetCollider(in Collider)"/>
        public Collider TargetCollider {
            get => targetCollider;
            set => SetTargetCollider(value);
        }

        /// <summary>
        /// <see cref="TargetTransform"/> is a reference to the <see cref="Transform"/> component on the
        /// <see cref="TargetCollider"/>. If <see cref="TargetCollider"/> is <c>null</c> then <c>null</c> is also returned from
        /// this property.
        /// </summary>
        /// <remarks>
        /// To test if the <see cref="Actor"/> has a <see cref="TargetCollider"/>, query <see cref="HasTargetCollider"/> or test
        /// if <see cref="TargetCollider"/> has a <c>null</c> value.
        /// </remarks>
        /// <seealso cref="TargetCollider"/>
        /// <seealso cref="HasTargetCollider"/>
        public Transform TargetTransform => targetCollider != null ? targetCollider.transform : null; // targetCollider?.transform cannot be used since targetCollider is a Unity object

        /// <summary>
        /// Target position that the <see cref="Actor"/> will target. Setting this will remove the <see cref="TargetCollider"/>
        /// if the <see cref="Actor"/> has one. This can be tested by querying the <see cref="HasTargetCollider"/> property.
        /// </summary>
        /// <remarks>
        /// When the <see cref="Actor"/> has a <see cref="TargetCollider"/> that it is tracking, it will update the
        /// <see cref="TargetPosition"/> according to the last known position of the <see cref="TargetCollider"/>. This can be
        /// tested by querying <see cref="HasTargetCollider"/>.
        /// </remarks>
        /// <seealso cref="TargetCollider"/>
        /// <seealso cref="HasTargetCollider"/>
        /// <seealso cref="SetTargetPosition"/>
        public Vector3 TargetPosition {
            get => targetPosition;
            set => SetTargetPosition(value);
        }

        /// <summary>
        /// Speed that the target the <see cref="Actor"/> is tracking is moving in world-space. This is calculated from the
        /// last known sighting of the target.
        /// </summary>
        /// <seealso cref="TargetPosition"/>
        public Vector3 TargetSpeed => targetSpeed;

        /// <summary>
        /// <see cref="HasTarget"/> is <c>true</c> when <see cref="TargetCollider"/> is not <c>null</c> or
        /// if <see cref="HasReachedTargetPosition"/> is <c>true</c>.
        /// </summary>
        public bool HasTarget => targetCollider != null || HasReachedTargetPosition;

        /// <summary>
        /// <see cref="HasTargetCollider"/> is <c>true</c> when <see cref="TargetCollider"/> is not <c>null</c>.
        /// </summary>
        public bool HasTargetCollider => targetCollider != null;

        /// <summary>
        /// <see cref="HasReachedTargetPosition"/> is <c>true</c> when the distance between the <see cref="Actor"/> instance and the
        /// <see cref="TargetPosition"/> is less than or equal to the <c>stoppingDistance</c> on the <see cref="NavMeshAgent"/>
        /// component.
        /// </summary>
        public bool HasReachedTargetPosition => (targetPosition - transform.position).sqrMagnitude <= agent.stoppingDistance * agent.stoppingDistance;

        /// <summary>
        /// Suspected position of the target that the <see cref="Actor"/> is tracking.
        /// </summary>
        /// <seealso cref="TargetPosition"/>
        /// <seealso cref="HasTarget"/>
        public Vector3 SuspectedTargetPosition => targetPosition + (profile.suspectedTargetCompensation * targetSpeed);

        /// <summary>
        /// Suspected direction of the target that the <see cref="Actor"/> is tracking.
        /// </summary>
        /// <seealso cref="TargetSpeed"/>
        /// <seealso cref="TargetPosition"/>
        /// <seealso cref="HasTarget"/>
        public Vector3 SuspectedTargetDirection => SuspectedTargetPosition - transform.position;

        /// <inheritdoc cref="NavMeshAgent.stoppingDistance"/>
        public float StoppingDistance {
            get => agent.stoppingDistance;
            set => agent.stoppingDistance = value;
        }

        public bool UseCollider {
            get => collider != null && collider.enabled;
            set {
                if (value) {
                    if (collider == null) {
                        collider = gameObject.ForceGetComponent<CapsuleCollider>();
#if UNITY_EDITOR
                        collider.hideFlags = HideFlags.HideInInspector;
#endif
                    } else {
                        collider.enabled = true;
                    }
                    SetupCollider();
                } else if (collider != null) {
                    collider.enabled = false;
                    //DestroyImmediate(collider);
                }
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the <see cref="Actor"/> is on a <see cref="NavMesh"/>.
        /// </summary>
        public bool IsDirectable => agent.isOnNavMesh;

        #endregion

        #region logic

        #region Awake

        private void Awake() {
            // Awake is primarily used to get and test references.
            Console.AssertReference(profile);
            SetupAgent();
        }

        #endregion

        #region OnEnable

        private void OnEnable() {
            ResetAll(); // reset targetting
            if (ActorBuffer.IsFull) ActorBuffer.Expand(ActorBufferExpandSize); // expand actor buffer if full
            ActorBuffer.AddLast(this, true); // add the end of actor buffer and ensure only one instance exists in the actor buffer
        }

        #endregion

        #region OnDisable

        private void OnDisable() {
            if (ActorBuffer.Remove(this) > 0) { // remove the current actor instance from the actor buffer
                if (ActorBuffer.TryShrink(ActorBufferExpandSize)) { // remove from actor buffer
                    if (ActorBuffer.Count <= ActorBufferBatchUpdateSize) // there are not enough actors to construct more than one batch
                        batchStartIndex = ActorBufferBatchUpdateSize; // set the next batch start index from the end of the current batch (ensures the next actors added are updated asap)
                }
                if (batchStartIndex >= ActorBuffer.Count) // the batch start index is out of range, move it back to the start of the actor buffer
                    batchStartIndex = 0;
            }
        }

        #endregion

        #region SetupAgent

#if UNITY_EDITOR
        internal
#else
        private
#endif
        void SetupAgent() {
            agent = GetComponent<NavMeshAgent>();
            Console.AssertReference(agent);
            agent.agentTypeID = profile.agentType;
            agent.height = profile.height;
            agent.radius = profile.radius;
            agent.stoppingDistance = profile.stoppingDistance;
            agent.speed = profile.baseSpeed;
            agent.acceleration = profile.baseAcceleration;
            agent.angularSpeed = profile.baseAngularSpeed;
            agent.autoBraking = profile.autoBreaking;
            agent.autoTraverseOffMeshLink = profile.autoTraverseOffMeshLink;
            agent.autoRepath = profile.autoRepath;
            agent.areaMask = profile.areaMask;
            agent.baseOffset = 0.0f;
        }

        #endregion

        #region SetupCollider

#if UNITY_EDITOR
        internal
#else
        private
#endif
        void SetupCollider() {
            float height = agent.height;
            collider.height = height;
            collider.radius = agent.radius;
            collider.center = new Vector3(0.0f, height * 0.5f, 0.0f);
        }

        #endregion

        #region StaticUpdate

        /// <summary>
        /// Invoked every frame to update enabled/active <see cref="Actor"/> instances in batches.
        /// </summary>
        [CoreUpdate]
        private static void StaticUpdate() {
            int actorCount = ActorBuffer.Count;
            if (actorCount > 0) {
                if (actorCount <= ActorBufferBatchUpdateSize) { // there are not enough actors to construct a batch
                    for (int i = actorCount - 1; i >= 0; i--) { // iterate every actor in the actor buffer
                        ActorBuffer[i].InternalUpdate(); // update each actor
                    }
                } else { // there are enough actors to construct a batch
                    int remainingActors = actorCount - batchStartIndex; // calculate the number of actors between the current batch start index and the end of the actors buffer
                    if (remainingActors < ActorBufferBatchUpdateSize) { // two loops required since the batch wraps from the end to the start of the actor buffer
                        for (int i = actorCount - 1; i >= batchStartIndex; i--) { // update the last actors in the actor buffer
                            ActorBuffer[i].InternalUpdate();
                        }
                        remainingActors = ActorBufferBatchUpdateSize - remainingActors; // calculate the number of actors at the start of the actor buffer to update
                        for (int i = remainingActors - 1; i >= 0; i--) { // update the actors at the start of the actor buffer that are part of the current batch
                            ActorBuffer[i].InternalUpdate();
                        }
                        batchStartIndex = remainingActors; // set the next batch start index to the number of remaining actors at the start of the actor buffer
                    } else { // only one loop is required to update the remaining actors in the current batch
                        int nextStartIndex = batchStartIndex + ActorBufferBatchUpdateSize; // calculate where the next batch should start
                        for (int i = nextStartIndex - 1; i >= batchStartIndex; i--) { // update each actor in the current batch
                            ActorBuffer[i].InternalUpdate();
                        }
                        batchStartIndex = nextStartIndex; // set the next batch start index to the pre-calculated start index from before
                    }
                }
            }
        }

        #endregion

        #region InternalUpdate

        /// <summary>
        /// Invoked by <see cref="StaticUpdate"/> when this <see cref="Actor"/> instance is updated in a batch update.
        /// </summary>
        private void InternalUpdate() {
            lastTime = currentTime;
            currentTime = Time.time;
            if (updateTarget) UpdateTarget();
            if (behaviour != null) behaviour.OnActorUpdated(currentTime - lastTime);
        }

        #endregion

        #region ResetAll

        private void ResetAll() {
            ResetTarget();
            ResetClock();
            behaviourLock = false;
        }

        #endregion

        #region ResetClock

        private void ResetClock() {
            currentTime = Time.time;
            lastTime = currentTime;
        }

        #endregion

        #region ResetTarget

        private void ResetTarget() {
            targetPosition = transform.position;
            targetCollider = null;
            updateTarget = true;
        }

        #endregion

        #region UpdateTarget

        /// <summary>
        /// Updates all targetting calculations.
        /// </summary>
        private void UpdateTarget() {
            if (targetCollider != null) { // the actor has a collider to track
                updateTarget = false;
                Vector3 currentTargetPosition = targetCollider.transform.position;
                if (!currentTargetPosition.IsInsideCube(targetPosition, agent.stoppingDistance) // the current position of the target collider is significantly far from the current targetPosition
                    && SetDestination(currentTargetPosition) // try set the destination for the actor to the new position of the target
                ) { // a new destination was assigned
                    targetSpeed = (currentTargetPosition - targetPosition) * (1.0f / (currentTime - lastTime)); // calculate the speed of the target
                    targetPosition = currentTargetPosition; // update the target position to the current position
                    if (behaviour != null) behaviour.OnActorTargetUpdated(targetCollider, targetPosition); // update the actor behaviour
                }
            } else if (updateTarget && SetDestination(targetPosition)) { // there is no collider to track, just update the destination
                updateTarget = false;
                if (behaviour != null) behaviour.OnActorTargetUpdated(null, targetPosition); // update the actor behaviour
            }
        }

        #endregion

        #region SetTargetCollider

        /// <summary>
        /// Sets the <see cref="TargetCollider"/>.
        /// </summary>
        public void SetTargetCollider(in Collider target) {
            if (target == null) throw new ArgumentNullException(nameof(target)); // the target is null
            Collider nextTarget = profile.visualPerceptionLayerMask.ContainsLayer(target.gameObject.layer) ? target : null; // the target is visible
            if (nextTarget != targetCollider) { // the next target collider is different to the current collider
                targetCollider = nextTarget;
                updateTarget = true;
            }
        }

        #endregion

        #region SetTargetPosition

        /// <summary>
        /// Sets the <see cref="TargetPosition"/>.
        /// </summary>
        public void SetTargetPosition(in Vector3 target) {
            targetCollider = null;
            targetPosition = target;
            targetSpeed = Vector3.zero;
            updateTarget = true;
        }

        #endregion

        #region SetBehaviour

        /// <summary>
        /// Sets the <see cref="ActorBehaviour"/> that implements and thus controls how the <see cref="Actor"/>
        /// behaves when it receives different inputs.
        /// </summary>
        /// <param name="nextBehaviour">
        /// New <see cref="ActorBehaviour"/> that the <see cref="Actor"/> should use.
        /// </param>
        /// <returns>
        /// Returns the previous <see cref="ActorBehaviour"/>.
        /// </returns>
        /// <seealso cref="Behaviour"/>
        private ActorBehaviour SetBehaviour(in ActorBehaviour nextBehaviour) {
            if (nextBehaviour == null) throw new ArgumentNullException(nameof(nextBehaviour));
            if (behaviourLock) { // behaviour lock active, a new behaviour cannot be assigned
                queuedBehaviour = nextBehaviour; // queue the current behaviour
                return behaviour;
            }
            behaviourLock = true;
            ActorBehaviour previousBehaviour = behaviour;
            try {
                behaviour?.OnBehaviourChanged(nextBehaviour);
                behaviour = nextBehaviour;
                nextBehaviour.actor = this;
                nextBehaviour.OnBehaviourStarted(previousBehaviour);
            } finally {
                behaviourLock = false;
                if (queuedBehaviour != null) { // there is a behaviour queued
                    ActorBehaviour temp = queuedBehaviour; // store a reference to the queued behaviour
                    queuedBehaviour = null; // remove the queued behaviour as a reference
                    SetBehaviour(temp); // set the next queued behaviour
                }
            }
            return previousBehaviour;
        }

        #endregion

        #region SetDestination

        /// <summary>
        /// Sets a destination for the <see cref="agent"/> to navigate to.
        /// </summary>
        /// <returns>Returns <c>true</c> if the destination was set successfully.</returns>
        private bool SetDestination(in Vector3 target) => agent.SetDestination(target);

        #endregion

        #region IsVisible

        /// <returns>
        /// Returns <c>true</c> if the <paramref name="collider"/> is visible to the <see cref="Actor"/>.
        /// </returns>
        public bool IsVisible(in Collider collider) => IsVisibleFrom(transform.position + (transform.rotation * profile.visualSensorPosition), transform.forward, collider);

        #endregion

        #region IsVisibleFrom

        /// <param name="point"></param>
        /// <param name="direction"></param>
        /// <param name="collider"></param>
        /// <returns>
        /// Returns <c>true</c> if the <paramref name="collider"/> is visible to the <see cref="Actor"/> when looking from the <paramref name="point"/>
        /// in the specified <paramref name="direction"/>.
        /// </returns>
        public bool IsVisibleFrom(in Vector3 point, Vector3 direction, in Collider collider) {
            if (collider == null) throw new ArgumentNullException(nameof(collider));
            if (!profile.visualPerceptionLayerMask.ContainsLayer(collider.gameObject.layer)) return false; // not in visible layer mask

            Vector3 colliderPosition = collider.bounds.center;
            Vector3 localPosition = colliderPosition - point; // convert collider position into local space

            #region sphere check

            /*
             * This check makes sure that the collider is within the actors view distance.
             * It does this by calculating the square distance in the x-z plane (which is
             * used later) and checking that the x-z square distance is not a very small
             * as this will later cause divide by zero errors.
             * The x-z square distance is then turned into x-y-z squared distance and that
             * value is compared against the square of the view distance.
             * If the x-y-z square distance is more than the square view distance then the
             * collider is outside of the actor view distance.
             */

            float sqrXZDistanceToTarget = (localPosition.x * localPosition.x) + (localPosition.z * localPosition.z); // get the distance to the target in the xz plane
            if (sqrXZDistanceToTarget < 0.0001f) return true; // close enough that the entity is almost 0m away, therefore say the entity is visible as this will cause divide by zero errors later on

            float sqrDistanceToTarget = sqrXZDistanceToTarget + (localPosition.y * localPosition.y);
            if (sqrXZDistanceToTarget > profile.visualPerceptionDistance * profile.visualPerceptionDistance) return false; // out of range of view sphere

            #endregion

            #region field-of-view check

            /*
             * This check ensures that the target collider is within the actors field of view.
             * This is done by finding the forward direction of the actor in the x, z plane
             * and normalising the vector. This vector is then used as a direction vector in
             * a line equation. Because the actor position is treated as [0, 0, 0], the line
             * equation is simply "xF", where F is the normalised x-z forward vector and x
             * is a real number. x also equals the distance to the closest point on the line
             * to the collider. This is used with the previously calculated distance to the
             * target collider to form a right angled triangle, where theta can be calculated.
             * Theta is the angle FAT, where F is the forward direction, A is [0, 0, 0] (the
             * actor position), and T is the local collider position. If the collider is within
             * the actors field of view, then theta will be less than or equal to the actors
             * field of view. Otherwise the collider is not within the actors field of view
             * and is therefore not visible.
             */

            direction = new Vector3(direction.x, 0.0f, direction.z).normalized;

            float distanceToClosestPointOnForwardLine = ((direction.x * localPosition.x) + (direction.z * localPosition.z)) / ((direction.x * direction.x) + (direction.z * direction.z)); // find the distance from the closest point on the line [A -> F] to the point T
            float xzDistanceToTarget = Mathf.Sqrt(sqrXZDistanceToTarget); // find the x-z distance to the target entity
            //float inverseXZDistanceToTarget = 1.0f / xzDistanceToTarget;

            float theta = Mathf.Abs(Mathf.Acos(Mathf.Abs(distanceToClosestPointOnForwardLine) / xzDistanceToTarget)); // find theta inside right angled FAT triangle
            if (distanceToClosestPointOnForwardLine < 0.0f) theta = Mathf.PI - theta; // correct the angle if it exceeds 90 degrees

            if (Mathf.Abs(theta) > profile.visualPerceptionFieldOfView * 0.5f && sqrDistanceToTarget > profile.visualPerveptionPeripheralVisionRadius * profile.visualPerveptionPeripheralVisionRadius) return false; // outside field of view and peripheral vision

            #endregion

            #region line of sight check

            Bounds bounds = collider.bounds; // find the bounds of the collider
            Transform colliderTransform = collider.transform;

            #region cast to center of bounds

            if (QueryLineOfSight(point, bounds.center, colliderTransform)) return true; // cast a ray directly into the center of the target (fast check)

            #endregion

            #region find tangent direction

            /*
             * This is the 2D tangent to the line [Actor -> Target].
             * This is calculated using existing variables to speed up trigonometric calculations.
             * 
             * Explanation:
             * Let O = Origin (0, 0) (Actor Position)
             *     T = Target Entity Position
             *     f = Actor forward line (forward direction but in 2D space)
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

            float tangentDirectionX = localPosition.x - (direction.x * tangentIntersectionDistance);
            float tangentDirectionZ = localPosition.z - (direction.z * tangentIntersectionDistance);

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
            * This is done to check if any other parts of the actor is showing.
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
            * the line [Actor -> Target] (always vertical, but rotated about y axis).
            * 
            * This is calculated using the tangent calculated in the prior stage
            * to this stage.
            * See above for calculation.
            */

            Vector3 extents = bounds.extents;
            float maxExtentSize = (extents.x > extents.z ? extents.x : extents.z) * 0.5f;

            float dx = tangentDirectionX * maxExtentSize;
            float dy = extents.y * 0.5f;
            float dz = tangentDirectionZ * maxExtentSize;

            return
                QueryLineOfSight(
                    point,
                    new Vector3( // top extended
                        colliderPosition.x,
                        colliderPosition.y + (dy * 1.9f), // 1.9 = almost the top (2.0 would be the top)
                        colliderPosition.z
                    ),
                    colliderTransform
                ) || QueryLineOfSight(
                    point,
                    new Vector3( // top middle
                        colliderPosition.x,
                        colliderPosition.y + dy,
                        colliderPosition.z
                    ),
                    colliderTransform
                ) || QueryLineOfSight(
                    point,
                    new Vector3( // middle right
                        colliderPosition.x + dx,
                        colliderPosition.y,
                        colliderPosition.z + dz
                    ),
                    colliderTransform
                ) || QueryLineOfSight(
                    point,
                    new Vector3( // middle left
                        colliderPosition.x - dx,
                        colliderPosition.y,
                        colliderPosition.z - dz
                    ),
                    colliderTransform
                ) || QueryLineOfSight(
                    point,
                    new Vector3( // top right
                        colliderPosition.x + dx,
                        colliderPosition.y + dy,
                        colliderPosition.z + dz
                    ),
                    colliderTransform
                ) || QueryLineOfSight( // top left
                    point,
                    new Vector3(
                        colliderPosition.x - dx,
                        colliderPosition.y + dy,
                        colliderPosition.z - dz
                    ),
                    colliderTransform
                ) || QueryLineOfSight(
                    point,
                    new Vector3( // bottom middle
                        colliderPosition.x,
                        colliderPosition.y - dy,
                        colliderPosition.z
                    ),
                    colliderTransform
                ) || QueryLineOfSight(
                    point,
                    new Vector3( // bottom right
                        colliderPosition.x + dx,
                        colliderPosition.y - dy,
                        colliderPosition.z + dz
                    ),
                    colliderTransform
                ) || QueryLineOfSight(
                    point,
                    new Vector3( // bottom left
                        colliderPosition.x - dx,
                        colliderPosition.y - dy,
                        colliderPosition.z - dz
                    ),
                    colliderTransform
                );

            #endregion

            #endregion

        }

        #endregion

        #region QueryLineOfSight

        /// <summary>
        /// Queries if the <see cref="Actor"/> can see a <paramref name="transform"/> when casting a line from the <paramref name="origin"/>
        /// to the <paramref name="target"/> position. Both are positions in world-space.
        /// </summary>
        /// <param name="origin">Origin position of the ray in world-space.</param>
        /// <param name="target">Target position of the ray in world-space. This is used to calculate a direction.</param>
        /// <param name="transform"><see cref="Transform"/> component to test if is visible.</param>
        /// <returns>Returns <c>true</c> if the <paramref name="transform"/> component is visible.</returns>
        private bool QueryLineOfSight(in Vector3 origin, in Vector3 target, in Transform transform) {
#if UNITY_EDITOR && ACTOR_DEBUG
            Debug.DrawLine(origin, target, Color.cyan); // draw the line of sight
#endif
            return Physics.Raycast(
                origin,
                target - origin,
                out RaycastHit hit,
                profile.visualPerceptionDistance,
                profile.visualPerceptionLayerMask
            ) && hit.collider.transform == transform;

        }

        #endregion

        #region QueryVisualPerception

        /// <summary>
        /// An expensive operation to query what colliders the <see cref="Actor"/> can see.
        /// </summary>
        public IEnumerator<Collider> QueryVisualPerception() => QueryVisualPerceptionFrom(transform.position + (transform.rotation * profile.visualSensorPosition), transform.forward);

        #endregion

        #region QueryVisualPerceptionFrom

        /// <summary>
        /// An expensive operation to query what colliders the <see cref="Actor"/> can see from <paramref name="point"/>.
        /// Queries every <see cref="Collider"/> near the <see cref="Actor"/> and test if any of those colliders can be seen.
        /// This is an expensive operation.
        /// </summary>
        /// <param name="point">Point in world-space to start the query from.</param>
        /// <param name="direction">Direction to look in.</param>
        /// <returns>Every <see cref="Collider"/> that the <see cref="Actor"/> can see.</returns>
        public IEnumerator<Collider> QueryVisualPerceptionFrom(Vector3 point, Vector3 direction) {
            Collider[] colliders = Physics.OverlapSphere(point, profile.visualPerceptionDistance, profile.visualPerceptionLayerMask); // get all colliders in range of the actors visison
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

        #region QueryAuditoryPerception

        public IEnumerator<SoundSample> QueryAuditoryPerception() => Soundscape.QueryAt(
            transform.position + (transform.rotation * profile.auditorySensorPosition),
            profile.auditoryPerceptionRange,
            profile.auditoryPerceptionThresholdIntensity
        );

        #endregion

        #region QueryAuditoryPerceptionFrom

        public IEnumerator<SoundSample> QueryAuditoryPerceptionFrom(in Vector3 point) => Soundscape.QueryAt(
            point, profile.auditoryPerceptionRange, profile.auditoryPerceptionThresholdIntensity
        );

        #endregion

        #region Damage

        /// <summary>
        /// Damages the <see cref="Actor"/> instance by a specified amount of <paramref name="damage"/>.
        /// </summary>
        /// <param name="sender">Sender/invoker of the damage.</param>
        /// <param name="damage">Numerical value for the amount of damage to deal.</param>
        /// <param name="data">Any data sent along with the damage, this can be <c>null</c> if there is no data.</param>
        /// <returns>Returns the actual amount of damage received by the <see cref="Actor"/>.</returns>
        public float Damage(in object sender, float damage, in object data = null) {
            if (behaviour == null) return 0.0f; // no damage was delt
            return behaviour.OnActorDamaged(sender, damage, data);
        }

        #endregion

        #region ActorCommand

        [Command(
            name: "actor",
            description: "Displays actor information.",
            usage: ""
        )]
        private static bool ActorCommand(CommandInfo info) {
            int argumentCount = info.args.Count;
            if (argumentCount > 0) {
                string arg0 = info.args[0].ToLower();
                switch (arg0) {
                    case "list": {
                        if (argumentCount == 1) {
                            int actorCount = ActorBuffer.Count;
                            string[,] table = new string[actorCount + 1, 7];
                            table[0, 0] = "Index";
                            table[0, 1] = "Instance ID";
                            table[0, 2] = "Name";
                            table[0, 3] = "Behaviour Type";
                            table[0, 4] = "Target Object";
                            table[0, 5] = "Target Position";
                            table[0, 6] = "Enabled";
                            Actor actor;
                            for (int i = actorCount; i > 0; i--) {
                                actor = ActorBuffer[i - 1];
                                table[i, 0] = i.ToString();
                                table[i, 1] = actor.GetInstanceID().ToHex();
                                table[i, 2] = actor.name;
                                table[i, 3] = actor.behaviour?.GetType().Name ?? "None";
                                table[i, 4] = actor.targetCollider?.name ?? "None";
                                table[i, 5] = actor.targetPosition.ToString();
                                table[i, 6] = actor.enabled ? "true" : "false";
                            }
                            info.context.PrintTable(table, true, true);
                            return true;
                        } else {
                            info.context.Print(ConsoleUtility.UnknownArgumentMessage(info.args, 1));
                            return false;
                        }
                    }
                    case "destroy": {
                        if (argumentCount == 2) {
                            string args1 = info.args[1];
                            if (int.TryParse(args1, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int instanceId)) {
                                Actor actor;
                                for (int i = ActorBuffer.Count - 1; i >= 0; i--) {
                                    actor = ActorBuffer[i];
                                    if (instanceId == actor.GetInstanceID()) {
                                        info.context.Print($"Destroying actor \"{instanceId.ToHex()}\".");
                                        Destroy(actor.gameObject);
                                        return true;
                                    }
                                }
                                info.context.Print($"No actor \"{instanceId.ToHex()}\" exists.");
                                return false;
                            } else {
                                info.context.Print($"Failed to parse \"{instanceId.ToHex()}\" to actor instance ID.");
                                return false;
                            }
                        } else {
                            info.context.Print(ConsoleUtility.UnknownArgumentMessage(info.args, 1));
                            return false;
                        }
                    }
                    case "enable": {
                        if (argumentCount == 2) {
                            string args1 = info.args[1];
                            if (int.TryParse(args1, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int instanceId)) {
                                Actor actor;
                                for (int i = ActorBuffer.Count - 1; i >= 0; i--) {
                                    actor = ActorBuffer[i];
                                    if (instanceId == actor.GetInstanceID()) {
                                        if (actor.enabled) {
                                            info.context.Print($"Actor \"{instanceId.ToHex()}\" is already enabled.");
                                        } else {
                                            info.context.Print($"Enabling actor \"{instanceId.ToHex()}\".");
                                            actor.enabled = true;
                                        }
                                        return true;
                                    }
                                }
                                info.context.Print($"No actor \"{instanceId.ToHex()}\" exists.");
                                return false;
                            } else {
                                info.context.Print($"Failed to parse \"{instanceId.ToHex()}\" to actor instance ID.");
                                return false;
                            }
                        } else {
                            info.context.Print(ConsoleUtility.UnknownArgumentMessage(info.args, 1));
                            return false;
                        }
                    }
                    case "disable": {
                        if (argumentCount == 2) {
                            string args1 = info.args[1];
                            if (int.TryParse(args1, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int instanceId)) {
                                Actor actor;
                                for (int i = ActorBuffer.Count - 1; i >= 0; i--) {
                                    actor = ActorBuffer[i];
                                    if (instanceId == actor.GetInstanceID()) {
                                        if (!actor.enabled) {
                                            info.context.Print($"Actor \"{instanceId.ToHex()}\" is already disabled.");
                                        } else {
                                            info.context.Print($"Disabling actor \"{instanceId.ToHex()}\".");
                                            actor.enabled = false;
                                        }
                                        return true;
                                    }
                                }
                                info.context.Print($"No actor \"{instanceId.ToHex()}\" exists.");
                                return false;
                            } else {
                                info.context.Print($"Failed to parse \"{instanceId.ToHex()}\" to actor instance ID.");
                                return false;
                            }
                        } else {
                            info.context.Print(ConsoleUtility.UnknownArgumentMessage(info.args, 1));
                            return false;
                        }
                    }
                    case "clone": {
                        if (argumentCount == 2) {
                            string args1 = info.args[1];
                            if (int.TryParse(args1, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int instanceId)) {
                                Actor actor;
                                for (int i = ActorBuffer.Count - 1; i >= 0; i--) {
                                    actor = ActorBuffer[i];
                                    if (instanceId == actor.GetInstanceID()) {
                                        info.context.Print($"Cloning actor \"{instanceId.ToHex()}\".");
                                        Instantiate(actor, actor.transform.position, actor.transform.rotation, actor.transform.parent);
                                        return true;
                                    }
                                }
                                info.context.Print($"No actor \"{instanceId.ToHex()}\" exists.");
                                return false;
                            } else {
                                info.context.Print($"Failed to parse \"{instanceId.ToHex()}\" to actor instance ID.");
                                return false;
                            }
                        } else {
                            info.context.Print(ConsoleUtility.UnknownArgumentMessage(info.args, 1));
                            return false;
                        }
                    }
                    default: {
                        info.context.Print(ConsoleUtility.UnknownArgumentMessage(arg0, 0));
                        return false;
                    }
                }
            } else {
                info.context.Print("Expected at least one argument.");
                return false;
            }
        }

        #endregion

        #endregion

    }

}