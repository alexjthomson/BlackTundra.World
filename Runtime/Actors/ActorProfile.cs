using UnityEngine;
using UnityEngine.AI;

namespace BlackTundra.World.Actors {

#if UNITY_EDITOR
    [CreateAssetMenu(menuName = "Configuration/Actor/Profile", fileName = "ActorProfile", order = 0)]
#endif
    public sealed class ActorProfile : ScriptableObject {

        #region variable

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
        [Header("Vision Sensor")]
#endif

        /// <summary>
        /// Local position of the <see cref="Actor"/> vision sensor.
        /// </summary>
        [SerializeField] internal Vector3 visionSensorOffset = new Vector3(0.0f, 1.8f, 0.0f);

        /// <summary>
        /// <see cref="IVisionSensor"/> used for <see cref="Actor"/> vision.
        /// </summary>
        [SerializeField] internal ScriptableObject visionSensorAsset = null;


        /// <summary>
        /// Cached <see cref="IVisionSensor"/> from <see cref="visionSensorAsset"/>.
        /// </summary>
        internal IVisionSensor visionSensor = null;

#if UNITY_EDITOR
        [Header("Sound Sensor")]
#endif

        /// <summary>
        /// Local position of the actor's auditory sensor.
        /// </summary>
        [SerializeField] internal Vector3 soundSensorOffset = new Vector3(0.0f, 1.8f, 0.0f);

        /// <summary>
        /// <see cref="ISoundSensor"/> used for <see cref="Actor"/> sound detection.
        /// </summary>
        [SerializeField] internal ScriptableObject soundSensorAsset = null;

        /// <summary>
        /// Cached <see cref="ISoundSensor"/> from <see cref="soundSensorAsset"/>.
        /// </summary>
        internal ISoundSensor soundSensor = null;

        #endregion

        #region logic

        #region Initialise

        /// <summary>
        /// Invoke this before the <see cref="ActorProfile"/> is used. This ensures it is setup correctly.
        /// </summary>
        internal void Initialise() {
            if (visionSensor == null) visionSensor = (IVisionSensor)visionSensorAsset;
            if (soundSensor == null) soundSensor = (ISoundSensor)soundSensorAsset;
        }

        #endregion

        #endregion

    }

}