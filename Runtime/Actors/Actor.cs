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

        /// <summary>
        /// Instance of <see cref="NavMeshPath"/> that is cached and used for nav mesh calculations where a <see cref="NavMeshPath"/> instance is required.
        /// </summary>
        private static NavMeshPath calculationPathInstance = null;

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

        /// <inheritdoc cref="NavMeshAgent.velocity"/>
#pragma warning disable IDE1006 // naming styles
        public Vector3 velocity {
#pragma warning restore IDE1006 // naming styles
            get => agent.velocity;
        }

        /// <inheritdoc cref="NavMeshAgent.desiredVelocity"/>
#pragma warning disable IDE1006 // naming styles
        public Vector3 desiredVelocity {
#pragma warning restore IDE1006 // naming styles
            get => agent.desiredVelocity;
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
        public bool HasReachedTargetPosition => !agent.pathPending && (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance);

        /// <summary>
        /// Checks if the <see cref="Actor"/> is moving.
        /// </summary>
        public bool IsMoving => agent.velocity.sqrMagnitude > Mathf.Epsilon;

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

        /// <summary>
        /// Returns <c>true</c> if the <see cref="Actor"/> is paused.
        /// </summary>
        public bool IsPaused => agent.isStopped;

        /// <summary>
        /// Number of seconds since the <see cref="Actor"/> was last updated.
        /// </summary>
        public float TimeSinceUpdate => Time.time - currentTime;

        /// <summary>
        /// Number of seconds between the last time the <see cref="Actor"/> was updated and the time before the last time the <see cref="Actor"/> was updated.
        /// </summary>
        public float LastDeltaTime => currentTime - lastTime;

        /// <summary>
        /// Estimated time between updates of actors.
        /// </summary>
        public static float EstimatedUpdateInterval => ((ActorBuffer.Count / ActorBufferBatchUpdateSize) + 1) * Time.deltaTime;

        #endregion

        #region logic

        #region Awake

        private void Awake() {
            Console.AssertReference(profile);
            profile.Initialise();
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
            if (collider == null) {
                collider = GetComponent<CapsuleCollider>();
                if (collider == null) return;
            }
            if (agent == null) SetupAgent();
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

        #region RemoveTarget

        /// <summary>
        /// Removes the <see cref="Actor"/> <see cref="TargetCollider"/> and <see cref="TargetPosition"/>.
        /// </summary>
        public void RemoveTarget() {
            targetPosition = transform.position;
            targetCollider = null;
            updateTarget = true;
        }

        #endregion

        #region Resume

        /// <summary>
        /// Resumes travelling to the <see cref="TargetPosition"/>.
        /// </summary>
        public void Resume() => agent.isStopped = false;

        #endregion

        #region Pause

        /// <summary>
        /// Pauses travelling to the <see cref="TargetPosition"/>.
        /// </summary>
        public void Pause() => agent.isStopped = true;

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
            agent.isStopped = false;
            agent.velocity = Vector3.zero;
            updateTarget = true;
        }

        #endregion

        #region UpdateTarget

        /// <summary>
        /// Updates all targetting calculations.
        /// </summary>
        private void UpdateTarget() {
            if (targetCollider != null) { // the actor has a collider to track
                //updateTarget = false;
                Vector3 currentTargetPosition = targetCollider.transform.position;
                if (!currentTargetPosition.IsInsideCube(targetPosition, agent.stoppingDistance) // the current position of the target collider is significantly far from the current targetPosition
                    && SetDestination(currentTargetPosition) // try set the destination for the actor to the new position of the target
                ) { // a new destination was assigned
                    targetSpeed = (currentTargetPosition - targetPosition) * (1.0f / (currentTime - lastTime)); // calculate the speed of the target
                    targetPosition = currentTargetPosition; // update the target position to the current position
                    if (behaviour != null) behaviour.OnActorTargetUpdated(targetCollider, targetPosition); // update the actor behaviour
                }
            } else if (updateTarget) { // no collider to track, just track destination
                updateTarget = false;
                if (SetDestination(targetPosition) && behaviour != null)
                    behaviour.OnActorTargetUpdated(null, targetPosition); // update the actor behaviour
            }
        }

        #endregion

        #region SetTargetCollider

        /// <summary>
        /// Sets the <see cref="TargetCollider"/>.
        /// </summary>
        public void SetTargetCollider(Collider target) {
            if (target != null && !profile.visionSensor.IsDetectable(target)) target = null;
            if (target != targetCollider) { // the target collider is different to the current collider
                targetCollider = target;
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
                nextBehaviour.agent = agent;
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

        #region IsWithinRangeOfTarget

        /// <summary>
        /// Tests if the <see cref="Actor"/> is within a specific <paramref name="range"/> of the <see cref="TargetPosition"/>.
        /// </summary>
        /// <returns>Returns <c>true</c> if the <see cref="Actor"/> is within the specified <paramref name="range"/> of the <see cref="TargetPosition"/>.</returns>
        public bool IsWithinRangeOfTarget(in float range) {
            if (range < 0.0f) throw new ArgumentOutOfRangeException(nameof(range));
            Vector3 actorPosition = transform.position;
            Vector3 targetPosition = targetCollider != null ? targetCollider.transform.position : this.targetPosition;
            float x = targetPosition.x - actorPosition.x;
            float z = targetPosition.z - actorPosition.z;
            float sqrXZDistance = (x * x) + (z * z);
            float sqrRange = range * range;
            if (sqrXZDistance > sqrRange) return false; // outside of range
            float y = targetPosition.y - actorPosition.y;
            float sqrDistance = sqrXZDistance + (y * y);
            if (sqrDistance > sqrRange) return false; // outside of range
            NavMeshPath agentPath = agent.path;
            if (agentPath == null) return true; // within distance of target
            Vector3[] points = agentPath.corners;
            if (points.Length > 1) {
                float distance = 0.0f;
                for (int i = points.Length - 1; i >= 1;) {
                    distance += (points[i] - points[--i]).magnitude;
                    if (distance > range) return false; // path length greater than range range
                }
            }
            return true; // all checks passed
        }

        #endregion

        #region IsVisible

        /// <returns>
        /// Returns <c>true</c> if the <paramref name="collider"/> is visible to the <see cref="Actor"/>.
        /// </returns>
        public bool IsVisible(in Collider collider) {
            IVisionSensor sensor = profile.visionSensor;
            if (sensor == null) return false;
            return sensor.IsVisibleFrom(
                transform.position + (transform.rotation * profile.visionSensorOffset),
                transform.forward,
                collider
            );
        }

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
            IVisionSensor sensor = profile.visionSensor;
            if (sensor == null) return false;
            return sensor.IsVisibleFrom(point, direction, collider);
        }

        #endregion

        #region QueryVisionSensor

        /// <summary>
        /// An expensive operation to query what colliders the <see cref="Actor"/> can see.
        /// </summary>
        public IEnumerator<Collider> QueryVisionSensor() {
            IVisionSensor sensor = profile.visionSensor;
            if (sensor == null) return null;
            return sensor.QueryVisualSensorFrom(
                transform.position + (transform.rotation * profile.visionSensorOffset),
                transform.forward
            );
        }

        #endregion

        #region QueryVisionSensorFrom

        /// <summary>
        /// An expensive operation to query what colliders the <see cref="Actor"/> can see from <paramref name="point"/>.
        /// Queries every <see cref="Collider"/> near the <see cref="Actor"/> and test if any of those colliders can be seen.
        /// This is an expensive operation.
        /// </summary>
        /// <param name="point">Point in world-space to start the query from.</param>
        /// <param name="direction">Direction to look in.</param>
        /// <returns>Every <see cref="Collider"/> that the <see cref="Actor"/> can see.</returns>
        public IEnumerator<Collider> QueryVisionSensorFrom(Vector3 point, Vector3 direction) {
            IVisionSensor sensor = profile.visionSensor;
            if (sensor == null) return null;
            return sensor.QueryVisualSensorFrom(point, direction);
        }

        #endregion

        #region QuerySoundSensor

        public IEnumerator<SoundSample> QuerySoundSensor() {
            ISoundSensor sensor = profile.soundSensor;
            if (sensor == null) return null;
            return profile.soundSensor.QuerySoundSensorFrom(
                transform.position + (transform.rotation * profile.soundSensorOffset),
                transform.forward
            );
        }

        #endregion

        #region QuerySoundSensorFrom

        public IEnumerator<SoundSample> QuerySoundSensorFrom(in Vector3 point, in Vector3 direction) {
            ISoundSensor sensor = profile.soundSensor;
            if (sensor == null) return null;
            return profile.soundSensor.QuerySoundSensorFrom(point, direction);
        }

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

        #region CalculateRandomPointInRange

        /// <summary>
        /// Calculates a random point within a specified <paramref name="range"/> from a <paramref name="point"/>.
        /// </summary>
        /// <param name="point">Centre point to generate the random point around.</param>
        /// <param name="range">Maximum path distance from the <paramref name="point"/> that the random point can be generated at.</param>
        /// <returns>
        /// Returns a random point that is no further than <paramref name="range"/> from <paramref name="point"/>.
        /// </returns>
        public Vector3 CalculateRandomPointInRange(in Vector3 point, in float range) {
            Vector2 circularPoint = MathsUtility.RandomPoint(
                new Vector2(point.x, point.z),
                range
            );
            Vector3 targetDestination = new Vector3(
                point.x + circularPoint.x,
                point.y,
                point.z + circularPoint.y
            );
            if (calculationPathInstance == null) calculationPathInstance = new NavMeshPath();
            else calculationPathInstance.ClearCorners(); // clear corners on calculation path instance
            if (NavMesh.CalculatePath(point, targetDestination, agent.areaMask, calculationPathInstance)) { // a path was calculated successfully, limit the range
                Vector3[] points = calculationPathInstance.corners; // get each point on the path
                int pointCount = points.Length; // get the number of points on the path
                if (pointCount == 1) return points[0]; // there is only one point on the path
                float totalDistance = 0.0f; // cumulative total distance over the following iteration
                float distance = 0.0f; // track the current distance
                Vector3 deltaPosition; // track the vector from the last point to the current point
                for (int i = 1; i < pointCount; i++) { // iterate each point
                    deltaPosition = points[i] - points[i - 1]; // calculate the vector from the last point to the current point
                    distance = deltaPosition.magnitude; // get the distance from the last point to the current point
                    totalDistance += distance; // add the distance to the cumulative total distance
                    if (totalDistance > range) { // the total distance is greater than the range
                        float overshootAmount = totalDistance - range; // calculate by how many meters the range was overshot
                        Vector3 direction = deltaPosition * (1.0f / distance); // calculate the direction from the last point to the overshot point
                        Vector3 finalPoint = points[i - 1] + (direction * overshootAmount); // calculate the new final point
                        return finalPoint; // return the new final point
                    }
                }
                return points[pointCount - 1];
            } else { // no path was calculated
                return targetDestination; // return the target point
            }
        }

        #endregion

        #region ActorCommand

        [Command(
            name: "actor",
            description: "Displays actor information.",
            usage:
            "actor list" +
            "\n\tDisplays a table with information about each actor." +
            "\nactor destroy {instanceId}" +
            "\n\tDestroys an actor by the actors instance ID." +
            "\nactor enable" +
            "\n\tEnables an actor." +
            "\nactor disable" +
            "\n\tDisables an actor." +
            "\nactor clone {instanceId}" +
            "\n\tClones an actor by the actors instance ID.",
            hidden: false
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
                            ConsoleWindow.PrintTable(table, true, true);
                            return true;
                        } else {
                            ConsoleWindow.Print(ConsoleUtility.UnknownArgumentMessage(info.args, 1));
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
                                        ConsoleWindow.Print($"Destroying actor \"{instanceId.ToHex()}\".");
                                        Destroy(actor.gameObject);
                                        return true;
                                    }
                                }
                                ConsoleWindow.Print($"No actor \"{instanceId.ToHex()}\" exists.");
                                return false;
                            } else {
                                ConsoleWindow.Print($"Failed to parse \"{instanceId.ToHex()}\" to actor instance ID.");
                                return false;
                            }
                        } else {
                            ConsoleWindow.Print(ConsoleUtility.UnknownArgumentMessage(info.args, 1));
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
                                            ConsoleWindow.Print($"Actor \"{instanceId.ToHex()}\" is already enabled.");
                                        } else {
                                            ConsoleWindow.Print($"Enabling actor \"{instanceId.ToHex()}\".");
                                            actor.enabled = true;
                                        }
                                        return true;
                                    }
                                }
                                ConsoleWindow.Print($"No actor \"{instanceId.ToHex()}\" exists.");
                                return false;
                            } else {
                                ConsoleWindow.Print($"Failed to parse \"{instanceId.ToHex()}\" to actor instance ID.");
                                return false;
                            }
                        } else {
                            ConsoleWindow.Print(ConsoleUtility.UnknownArgumentMessage(info.args, 1));
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
                                            ConsoleWindow.Print($"Actor \"{instanceId.ToHex()}\" is already disabled.");
                                        } else {
                                            ConsoleWindow.Print($"Disabling actor \"{instanceId.ToHex()}\".");
                                            actor.enabled = false;
                                        }
                                        return true;
                                    }
                                }
                                ConsoleWindow.Print($"No actor \"{instanceId.ToHex()}\" exists.");
                                return false;
                            } else {
                                ConsoleWindow.Print($"Failed to parse \"{instanceId.ToHex()}\" to actor instance ID.");
                                return false;
                            }
                        } else {
                            ConsoleWindow.Print(ConsoleUtility.UnknownArgumentMessage(info.args, 1));
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
                                        ConsoleWindow.Print($"Cloning actor \"{instanceId.ToHex()}\".");
                                        Actor clone = Instantiate(actor, actor.transform.position, actor.transform.rotation, actor.transform.parent);
                                        clone.gameObject.name = string.Concat(actor.name, "_Clone_", actor.GetInstanceID().ToHex());
                                        return true;
                                    }
                                }
                                ConsoleWindow.Print($"No actor \"{instanceId.ToHex()}\" exists.");
                                return false;
                            } else {
                                ConsoleWindow.Print($"Failed to parse \"{instanceId.ToHex()}\" to actor instance ID.");
                                return false;
                            }
                        } else {
                            ConsoleWindow.Print(ConsoleUtility.UnknownArgumentMessage(info.args, 1));
                            return false;
                        }
                    }
                    default: {
                        ConsoleWindow.Print(ConsoleUtility.UnknownArgumentMessage(arg0, 0));
                        return false;
                    }
                }
            } else {
                ConsoleWindow.Print("Expected at least one argument.");
                return false;
            }
        }

        #endregion

        #endregion

    }

}