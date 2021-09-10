using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BlackTundra.WorldSystem.Paths {

    /// <summary>
    /// Stores path data and creates a path in the world.
    /// </summary>
    [DisallowMultipleComponent]
#if UNITY_EDITOR
    [AddComponentMenu("World/Path")]
#endif
    public sealed class Path : MonoBehaviour {

        #region nested
#if UNITY_EDITOR

        /// <summary>
        /// Stores data about a Path used by the Unity editor.
        /// </summary>
        [Serializable]
        public sealed class PathEditorData {

            #region variable

            public Path path;

            // bezier display settings:
            public bool showTransformTool = true;
            public bool showPathBounds = false;
            public bool showPerSegmentBounds = false;
            public bool displayAnchorPoints = true;
            public bool displayControlPoints = true;
            public float bezierHandleScale = 10.0f;
            public bool keepConstantHandleSize = false;

            // vertex display settings:
            public bool showNormalsInVertexMode = false;
            public bool showTangentsInVertexMode = false;
            public bool showBezierPathInVertexMode = false;

            // editor display states:
            public bool showDisplayOptions = false;
            public bool showPathOptions = true;
            public bool showVertexPathDisplayOptions = false;
            public bool showVertexPathOptions = true;
            public bool showNormals = false;
            public bool showNormalsHelpInfo = false;
            public int tabIndex = 0;

            #endregion

            #region event

            public event Action OnBezierOrVertexPathModified;
            public event Action OnBezierCreated;

            #endregion

            #region constructor

            internal PathEditorData(in Path path) {

                this.path = path;

            }

            #endregion

            #region destructor

            ~PathEditorData() {

                if (path.bezierPath != null) path.bezierPath.OnModified -= PathModified;

            }

            #endregion

            #region logic

            #region Initialize

            internal void Initialise() {

                if (path.bezierPath == null) CreateBezier(Vector3.zero, PathSpace.xyz);
                path.updateVertexPath = true;
                path.bezierPath.OnModified -= PathModified;
                path.bezierPath.OnModified += PathModified;

            }

            #endregion

            #region ResetBezierPath

            public void ResetBezierPath(in Vector3 centre, in PathSpace space = PathSpace.xyz) => CreateBezier(centre, space);

            #endregion

            #region CreateBezier

            private void CreateBezier(in Vector3 centre, in PathSpace space) {

                if (path.bezierPath != null) path.bezierPath.OnModified -= PathModified;
                path.bezierPath = new BezierPath(centre, false, space);
                path.bezierPath.OnModified += PathModified;
                path.updateVertexPath = true;
                OnBezierOrVertexPathModified?.Invoke();
                OnBezierCreated?.Invoke();

            }

            #endregion

            #region PathTransformed

            public void PathTransformed() => OnBezierOrVertexPathModified?.Invoke();

            #endregion

            #region PathModified

            public void PathModified() {
                path.updateVertexPath = true;
                OnBezierOrVertexPathModified?.Invoke();
            }

            #endregion

            #endregion

        }

#endif
        #endregion

        #region variable

        /// <summary>
        /// Stores a reference to the bezier path that describes the shape of the path.
        /// </summary>
        [SerializeField, HideInInspector] private BezierPath bezierPath = null;

        /// <summary>
        /// Reference to the vertex path used for path mathematics.
        /// </summary>
        [SerializeField, HideInInspector] private VertexPath vertexPath = null;

        /// <summary>
        /// Tracks if the vertex path needs to be updated or not.
        /// </summary>
        [SerializeField, HideInInspector] private bool updateVertexPath = false;

        /// <summary>
        /// Maximum angle difference before a new vertex is generated.
        /// </summary>
#if UNITY_EDITOR
        public
#else
        [SerializeField, HideInInspector] private
#endif
        float maxAngleError = 0.3f;

        /// <summary>
        /// Minimum distance between verticies.
        /// </summary>
#if UNITY_EDITOR
        public
#else
        [SerializeField, HideInInspector] private
#endif
        float minVertexDistance = 0.0f;

#if UNITY_EDITOR

        /// <summary>
        /// Data used by the Unity editor for this path.
        /// </summary>
        [SerializeField, HideInInspector] private PathEditorData editorData = null;

        /// <summary>
        /// Stores if the path editor data has been initialized or not.
        /// </summary>
        [SerializeField, HideInInspector] private bool initialised = false;

#endif

        #endregion

        #region event

        public event Action OnPathUpdated;

        #endregion

        #region property

        public VertexPath VertexPath {

            get {
#if UNITY_EDITOR
                InitialiseEditorData();
#endif
                if (vertexPath == null || updateVertexPath) UpdateVertexPath();
                return vertexPath;
            }

        }

        public BezierPath BezierPath {

            get {
#if UNITY_EDITOR
                InitialiseEditorData();
#endif
                return bezierPath;
            }
            set {
                if (bezierPath == value) return;
#if UNITY_EDITOR
                InitialiseEditorData();
                bezierPath.OnModified -= editorData.PathModified;
#endif
                bezierPath = value;
#if UNITY_EDITOR
                bezierPath.OnModified += editorData.PathModified;
#endif
                updateVertexPath = true;
            }

        }

#if UNITY_EDITOR
        /// <summary>
        /// Gets the data used by the Unity editor.
        /// This property should never be used outside of the Unity editor and will not be compiled with a release build of the game.
        /// </summary>
        public PathEditorData EditorData => editorData;
#endif

        #endregion

        #region logic

        #region OnEnable

        private void OnEnable() => CheckValid();

        #endregion

        #region UpdateVertexPath

        private void UpdateVertexPath() {

            vertexPath = new VertexPath(
                bezierPath,
                transform,
                maxAngleError,
                minVertexDistance
            );
            updateVertexPath = false;

        }

        #endregion

        #region CheckValid

        private void CheckValid() {

            if (bezierPath == null) {
                bezierPath = new BezierPath(Vector3.zero);
                UpdateVertexPath();
            }// else if (vertexPath == null || updateVertexPath) UpdateVertexPath();

        }

        #endregion

        #region InitialiseEditorData
#if UNITY_EDITOR

        /// <summary>
        /// Initialises data used in the unity editor.
        /// </summary>
        public void InitialiseEditorData() {

            if (initialised) return;
            if (editorData == null) editorData = new PathEditorData(this);
            editorData.OnBezierOrVertexPathModified -= OnPathUpdated;
            editorData.OnBezierOrVertexPathModified += OnPathUpdated;
            editorData.Initialise();
            initialised = true;

        }

#endif
        #endregion

        #region CreatePath
#if UNITY_EDITOR

        [MenuItem("GameObject/3D Object/Path")]
        private static void CreatePath(MenuCommand menuCommand) {
            GameObject gameObject = new GameObject("Path");
            Path path = gameObject.AddComponent<Path>();
            path.CheckValid();
            path.InitialiseEditorData();
            GameObjectUtility.SetParentAndAlign(gameObject, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(gameObject, "Created Path");
            Selection.activeObject = gameObject;
        }

#endif
        #endregion

        #region OnDrawGizmos
#if UNITY_EDITOR

        private void OnDrawGizmos() {
            CheckValid();
            GameObject selectedObject = Selection.activeGameObject;
            if (selectedObject != gameObject) { // only draw path gizmo if the path object is not selected
                if (VertexPath != null) {
                    vertexPath.UpdateTransform(transform);
                    Gizmos.color = Color.white;
                    for (int i = 0; i < vertexPath.points.Length; i++) {
                        int nextIndex = i + 1;
                        if (nextIndex >= vertexPath.points.Length) {
                            if (vertexPath.closed) nextIndex -= vertexPath.points.Length;
                            else break;
                        }
                        Gizmos.DrawLine(vertexPath.GetPoint(i), vertexPath.GetPoint(nextIndex));
                    }
                }
            }
        }

#endif
        #endregion

        #endregion

    }

}