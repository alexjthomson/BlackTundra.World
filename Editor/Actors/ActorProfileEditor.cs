using BlackTundra.Foundation.Editor.Utility;
using BlackTundra.World.Actors;

using System.Text.RegularExpressions;

using UnityEditor;

using UnityEngine;
using UnityEngine.AI;

namespace BlackTundra.World.Editor.Actors {

    [CustomEditor(typeof(ActorProfile))]
    public sealed class ActorProfileEditor : CustomInspector {

        #region constant

        private const int GeneralToolbarIndex = 0;
        private const int PathFindingToolbarIndex = 1;
        private const int VisionToolbarIndex = 2;
        private const int HearingToolbarIndex = 3;

        private static readonly GUIContent[] ToolbarElements = new GUIContent[] {
            new GUIContent("General"),
            new GUIContent("Path Finding"),
            new GUIContent("Vision"),
            new GUIContent("Hearing"),
        };

        //private const string Locomotion_BaseSpeed_Description = "Base speed that the actor will default to move at.";
        //private const string Locomotion_BaseAcceleration_Description = "Base acceleration that the actor will accelerate at.";
        //private const string Locomotion_BaseAngularSpeed_Description = "Base angular speed in degrees that the actor will turn/rotate at about the global y axis.";

        private const string PathFinding_TargetCompensation_Description = "The target compensation coefficient is used to predict the position of the target once it has been lost. A higher number means the actor will predict the target further away from the last known position of the target by combining this value with the last known velocity of the target.";
        private static readonly GUIContent PathFinding_TargetCompensation_Content = new GUIContent("Target Compensation", PathFinding_TargetCompensation_Description);
        private const string PathFinding_StoppingDistance_Description = "Distance from the target position that the actor will aim to stop within.";
        private static readonly GUIContent PathFinding_StoppingDistance_Content = new GUIContent("Stopping Distance", PathFinding_StoppingDistance_Description);
        private const string PathFinding_BaseObstacleAvoidanceType_Description = "Base/default obstacle avoidance type that the actor should use for pathfinding.";
        private static readonly GUIContent PathFinding_BaseObstacleAvoidanceType_Content = new GUIContent("Obstacle Avoidance Type", PathFinding_BaseObstacleAvoidanceType_Description);

        private const string Vision_SensorPosition_Description = "The sensor position is the local position relative to the actor that the vision sensors are located. The center of vision is always pointing in the forwards direction.";
        private const string Vision_SensorRange_Description = "The sensor range (in meters) is the distance from the sensor position at which objects are still visible and identifyable to the actor.";
        private const string Vision_PeripheralVisionRadius_Description = "The vision sensor can utilize peripheral vision which acts as a sphere around the sensor position that makes up part of the area the actor can see in. This is the radius (in meters) of that sphere.";
        private const string Vision_FieldOfView_Description = "Field of view (in degrees) that the vision sensor can see in.";
        private const string Vision_LayerMask_Description = "Layer mask that indicates which layers are visible/invisible to the vision sensor/actor.";

        private const string Hearing_SensorPosition_Description = "The sensor position is the local position relative to the actor that the audio sensors are located.";
        private const string Hearing_SensorRange_Description = "The sensor range (in meters) is the distance from the sensor position at which sounds can be percieved by the actor.";
        private const string Hearing_ThresholdIntensity_Description = "The threshold intensity is the minimum relative intensity of a sound that can be percieved by the actor.";

        #endregion

        #region variable

        private ActorProfile profile;
        new private string name;

        private int toolbarIndex = 0;
        private float scrollHeight = 0.0f;

        private static bool initStatic = false;
        private static int agentCount;
        private static int[] agentTypeValues;
        private static string[] agentTypeNames;
        private static string[] areaMaskNames;

        #endregion

        #region logic

        private void OnEnable() {
            profile = (ActorProfile)target;
            name = Regex.Replace(profile.name.Replace('_', ' '), "([A-Z])([A-Z])([a-z])|([a-z])([A-Z])", "$1$4 $2$3$5");
            InitStatic();
        }

        protected sealed override void DrawInspector() {
            EditorLayout.Title(name);
            EditorLayout.Space();
            DrawProfile(profile, ref toolbarIndex, ref scrollHeight);
        }

        private static void InitStatic() {
            agentCount = NavMesh.GetSettingsCount();
            agentTypeValues = new int[agentCount];
            agentTypeNames = new string[agentCount];
            NavMeshBuildSettings agentSettings;
            int id;
            for (int i = agentCount - 1; i >= 0; i--) {
                agentSettings = NavMesh.GetSettingsByIndex(i);
                id = agentSettings.agentTypeID;
                agentTypeValues[i] = id;
                agentTypeNames[i] = NavMesh.GetSettingsNameFromID(id);
            }
            areaMaskNames = GameObjectUtility.GetNavMeshAreaNames();
            initStatic = true;
        }

        internal static bool DrawProfile(in ActorProfile profile, ref int toolbarIndex, ref float scrollHeight) {
            if (!initStatic) InitStatic();
            int lastToolbarIndex = toolbarIndex;
            toolbarIndex = EditorLayout.Toolbar(lastToolbarIndex, ToolbarElements);
            if (lastToolbarIndex != toolbarIndex) GUI.FocusControl(null);
            EditorLayout.StartVerticalBox();
            EditorLayout.StartScrollView(ref scrollHeight);
            bool dirty = false;
            switch (toolbarIndex) {
                case GeneralToolbarIndex: { // general
                    EditorLayout.Title("General");

                    int oi, ni;
                    // agent type:
                    oi = profile.agentType;
                    ni = Mathf.Clamp(EditorLayout.DropdownField("Agent Type", oi, agentTypeNames, agentTypeValues), 0, agentCount);
                    if (ni != oi) {
                        profile.agentType = ni;
                        dirty = true;
                    }

                    float of, nf;
                    // height:
                    of = profile.height;
                    nf = Mathf.Max(EditorLayout.FloatField("Height", of), 0.0f);
                    if (nf != of) {
                        profile.height = nf;
                        dirty = true;
                    }

                    // radius:
                    of = profile.radius;
                    nf = Mathf.Max(EditorLayout.FloatField("Radius", of), 0.0f);
                    if (nf != of) {
                        profile.radius = nf;
                        dirty = true;
                    }

                    break;
                }
                case PathFindingToolbarIndex: { // path finding
                    EditorLayout.Title("Path Finding");

                    int oi, ni;
                    float of, nf;
                    bool ob, nb;

                    // area mask:
                    oi = profile.areaMask;
                    ni = EditorLayout.MaskField("Area Mask", oi, areaMaskNames);
                    if (oi != ni) {
                        profile.areaMask = ni;
                        dirty = true;
                    }

                    // auto traverse off-mesh-link:
                    ob = profile.autoTraverseOffMeshLink;
                    nb = EditorLayout.BooleanField("Auto Traverse Off Mesh Link", ob);
                    if (ob != nb) {
                        profile.autoTraverseOffMeshLink = nb;
                        dirty = true;
                    }

                    // auto breaking:
                    ob = profile.autoRepath;
                    nb = EditorLayout.BooleanField("Auto Repath", ob);
                    if (ob != nb) {
                        profile.autoRepath = nb;
                        dirty = true;
                    }

                    EditorLayout.Space();
                    EditorLayout.Title("Locomotion");

                    // speed:
                    of = profile.baseSpeed;
                    nf = Mathf.Max(EditorLayout.FloatField("Base Speed", of), 0.0f);
                    //EditorLayout.Info(Locomotion_BaseSpeed_Description);
                    if (!Mathf.Approximately(nf, of)) {
                        profile.baseSpeed = nf;
                        dirty = true;
                    }
                    //EditorLayout.Space();

                    // acceleration:
                    of = profile.baseAngularSpeed;
                    nf = Mathf.Max(EditorLayout.FloatField("Base Acceleration", of), 0.0f);
                    //EditorLayout.Info(Locomotion_BaseAcceleration_Description);
                    if (!Mathf.Approximately(nf, of)) {
                        profile.baseAcceleration = nf;
                        dirty = true;
                    }
                    //EditorLayout.Space();

                    // angular speed:
                    of = profile.baseAngularSpeed;
                    nf = Mathf.Clamp(EditorLayout.FloatField("Base Angular Speed", of), 0.0f, 360f);
                    //EditorLayout.Info(Locomotion_BaseAngularSpeed_Description);
                    if (!Mathf.Approximately(nf, of)) {
                        profile.baseAngularSpeed = nf;
                        dirty = true;
                    }

                    // auto breaking:
                    ob = profile.autoBreaking;
                    nb = EditorLayout.BooleanField("Auto Breaking", ob);
                    if (ob != nb) {
                        profile.autoBreaking = nb;
                        dirty = true;
                    }

                    EditorLayout.Space();
                    EditorLayout.Title("Targetting");

                    // target compensation:
                    of = profile.suspectedTargetCompensation;
                    nf = Mathf.Max(EditorLayout.FloatField(PathFinding_TargetCompensation_Content, of), 0.0f);
                    if (nf != of) {
                        profile.suspectedTargetCompensation = nf;
                        dirty = true;
                    }

                    // stopping distance:
                    of = profile.stoppingDistance;
                    nf = Mathf.Max(EditorLayout.FloatField(PathFinding_StoppingDistance_Content, of), 0.0f);
                    if (nf != of) {
                        profile.stoppingDistance = nf;
                        dirty = true;
                    }

                    EditorLayout.Space();
                    EditorLayout.Title("Obstacle Avoidance");

                    // obstacle avoidance type:
                    oi = profile.baseObstacleAvoidanceType;
                    ni = (int)EditorLayout.EnumField(PathFinding_BaseObstacleAvoidanceType_Content, (ObstacleAvoidanceType)oi);
                    if (ni != oi) {
                        profile.baseObstacleAvoidanceType = ni;
                        dirty = true;
                    }

                    break;
                }
                case VisionToolbarIndex: { // vision
                    EditorLayout.Title("Visual Perception");

                    // visual sensor position:
                    Vector3 ov3 = profile.visualSensorPosition;
                    Vector3 nv3 = EditorLayout.Vector3Field("Sensor Position", ov3);
                    EditorLayout.Info(Vision_SensorPosition_Description);
                    if (nv3 != ov3) {
                        profile.visualSensorPosition = nv3;
                        dirty = true;
                    }
                    EditorLayout.Space();

                    float of, nf;
                    // visial perception/sensor distance:
                    of = profile.visualPerceptionDistance;
                    nf = Mathf.Max(EditorLayout.FloatField("Sensor Range", of), 0.0f);
                    EditorLayout.Info(Vision_SensorRange_Description);
                    if (nf != of) {
                        profile.visualPerceptionDistance = nf;
                        dirty = true;
                    }
                    EditorLayout.Space();

                    // peripheral vision radius:
                    of = profile.visualPerveptionPeripheralVisionRadius;
                    nf = Mathf.Clamp(EditorLayout.FloatField("Peripheral Vision Radius", of), 0.0f, profile.visualPerceptionDistance);
                    EditorLayout.Info(Vision_PeripheralVisionRadius_Description);
                    if (nf != of) {
                        profile.visualPerveptionPeripheralVisionRadius = nf;
                        dirty = true;
                    }
                    EditorLayout.Space();

                    // field of view:
                    of = profile.visualPerceptionFieldOfView;
                    nf = Mathf.Clamp(EditorLayout.FloatField("Field of View", of * Mathf.Rad2Deg), 0.0f, 360f) * Mathf.Deg2Rad;
                    EditorLayout.Info(Vision_FieldOfView_Description);
                    if (!Mathf.Approximately(nf, of)) {
                        profile.visualPerceptionFieldOfView = nf;
                        dirty = true;
                    }
                    EditorLayout.Space();

                    // layermask:
                    LayerMask olm = profile.visualPerceptionLayerMask;
                    LayerMask nlm = EditorLayout.LayerMaskField("Layermask", olm);
                    EditorLayout.Info(Vision_LayerMask_Description);
                    if (nlm != olm) {
                        profile.visualPerceptionLayerMask = nlm;
                        dirty = true;
                    }
                    EditorLayout.Space();
                    break;
                }
                case HearingToolbarIndex: { // hearing
                    EditorLayout.Title("Auditory Perception");

                    // auditory sensor position:
                    Vector3 ov3 = profile.auditorySensorPosition;
                    Vector3 nv3 = EditorLayout.Vector3Field("Sensor Position", ov3);
                    EditorLayout.Info(Hearing_SensorPosition_Description);
                    if (nv3 != ov3) {
                        profile.auditorySensorPosition = nv3;
                        dirty = true;
                    }
                    EditorLayout.Space();

                    float of, nf;
                    // auditory preception/sensor range:
                    of = profile.auditoryPerceptionRange;
                    nf = EditorLayout.FloatField("Sensor Range", of);
                    EditorLayout.Info(Hearing_SensorRange_Description);
                    if (nf != of) {
                        profile.auditoryPerceptionRange = nf;
                        dirty = true;
                    }
                    EditorLayout.Space();

                    // auditory perception/sensor threshold intensity:
                    of = profile.auditoryPerceptionThresholdIntensity;
                    nf = EditorLayout.FloatField("Threshold Intensity", of);
                    EditorLayout.Info(Hearing_ThresholdIntensity_Description);
                    if (nf != of) {
                        profile.auditoryPerceptionThresholdIntensity = nf;
                        dirty = true;
                    }
                    break;
                }
                default: {
                    toolbarIndex = 0;
                    break;
                }
            }
            EditorLayout.EndVerticalBox();
            EditorLayout.EndScrollView();
            if (dirty) MarkAsDirty(profile);
            return dirty;
        }

        #endregion

    }

}