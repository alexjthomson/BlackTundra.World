using UnityEngine;
using UnityEngine.AI;

namespace BlackTundra.World.Actors {

#if UNITY_EDITOR
    [CreateAssetMenu(fileName = "ActorProfile", menuName = "Settings/Actors/Actor Profile", order = -1)]
#endif
    public sealed class ActorProfile : ScriptableObject {

#if UNITY_EDITOR
        [Header("General")]
#endif

        /// <summary>
        /// Agent type to use for path finding.
        /// </summary>
        [SerializeField] internal int agentType = 0;

        /// <summary>
        /// Height of the <see cref="Actor"/>.
        /// </summary>
        [SerializeField] internal float height = 2.0f;

        /// <summary>
        /// Radius of the <see cref="Actor"/>.
        /// </summary>
        [SerializeField] internal float radius = 0.5f;

#if UNITY_EDITOR
        [Header("Path Finding")]
#endif

        /// <summary>
        /// Base speed (m/s) that the <see cref="Actor"/> will move at.
        /// </summary>
        [SerializeField] internal float baseSpeed = 3.5f;

        /// <summary>
        /// Base angular speed (deg/s) that the <see cref="Actor"/> can turn at.
        /// </summary>
        [SerializeField] internal float baseAngularSpeed = 120.0f;

        /// <summary>
        /// Base acceleration (m/s^2) that the <see cref="Actor"/> can change their speed at.
        /// </summary>
        [SerializeField] internal float baseAcceleration = 8.0f;

        /// <summary>
        /// Compensation coefficient used to predict the position of the target once its lost.
        /// A higher number means the actor will predict the target further from the last position
        /// they were seen in.
        /// </summary>
        [SerializeField] internal float suspectedTargetCompensation = 0.5f;

        /// <summary>
        /// Distance that the <see cref="Actor"/> will aim to stop within of the target position.
        /// </summary>
        [SerializeField] internal float stoppingDistance = 0.0f;

        /// <summary>
        /// Base/default obstacle avoidance type that the <see cref="Actor"/> will use.
        /// </summary>
        [SerializeField] internal int baseObstacleAvoidanceType = (int)ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        /// <inheritdoc cref="NavMeshAgent.autoBraking"/>
        [SerializeField] internal bool autoBreaking = true;

        /// <inheritdoc cref="NavMeshAgent.autoTraverseOffMeshLink"/>
        [SerializeField] internal bool autoTraverseOffMeshLink = true;

        /// <inheritdoc cref="NavMeshAgent.autoRepath"/>
        [SerializeField] internal bool autoRepath = true;

        /// <inheritdoc cref="NavMeshAgent.areaMask"/>
        [SerializeField] internal int areaMask = 0;

#if UNITY_EDITOR
        [Header("Visual Perception")]
#endif

        /// <summary>
        /// Local position of the <see cref="Actor"/> vision sensor.
        /// </summary>
        [SerializeField] internal Vector3 visualSensorPosition = new Vector3(0.0f, 1.8f, 0.0f);

        /// <summary>
        /// Maximum distance that the <see cref="Actor"/> can see.
        /// </summary>
        [SerializeField] internal float visualPerceptionDistance = 50.0f;

        /// <summary>
        /// LayerMask describing what layers will block line of sight for the <see cref="Actor"/>.
        /// This includes all layers that are visible.
        /// Transparent layers shouldn't be included in this layer.
        /// </summary>
        [SerializeField] internal LayerMask visualPerceptionLayerMask = -1;

        /// <summary>
        /// Field of view in radians that the <see cref="Actor"/> can see in.
        /// </summary>
        [SerializeField] internal float visualPerceptionFieldOfView = 120.0f * Mathf.Deg2Rad;

        /// <summary>
        /// Radius of peripheral vision around the <see cref="Actor"/> visual sensor.
        /// </summary>
        [SerializeField] internal float visualPerveptionPeripheralVisionRadius = 1.5f;

#if UNITY_EDITOR
        [Header("Auditory Perception")]
#endif

        /// <summary>
        /// Local position of the actor's auditory sensor.
        /// </summary>
        [SerializeField] internal Vector3 auditorySensorPosition = new Vector3(0.0f, 1.8f, 0.0f);

        /// <summary>
        /// Minimum relative intensity of a sound that the actor can perceive.
        /// </summary>
        [SerializeField] internal float auditoryPerceptionThresholdIntensity = 0.1f;

        /// <summary>
        /// Distance from the actor's auditory sensor that the actor can still hear audio.
        /// </summary>
        [SerializeField] internal float auditoryPerceptionRange = 32.0f;

    }

}