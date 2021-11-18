#if ENABLE_VR

using BlackTundra.Foundation;
using BlackTundra.Foundation.Control;
using BlackTundra.Foundation.IO;
using BlackTundra.World.CameraSystem;

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace BlackTundra.World.XR {

    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRRig))]
    [RequireComponent(typeof(CharacterController))]
    public sealed class XRPlayerController : MonoBehaviour, IControllable {

        #region constant

        private const float NearClipDistance = 0.01f;

        #endregion

        #region variable

        /// <summary>
        /// <see cref="CameraController"/> used for XR.
        /// </summary>
#if UNITY_EDITOR
        new
#endif
        private CameraController camera = null;

        /// <summary>
        /// <see cref="CharacterController"/> used for movement.
        /// </summary>
        private CharacterController controller = null;

        /// <summary>
        /// <see cref="CharacterControllerDriver"/> used to drive the XR <see cref="controller"/>.
        /// </summary>
        private CharacterControllerDriver driver = null;

        /// <summary>
        /// <see cref="XRRig"/> used for VR.
        /// </summary>
        private XRRig rig = null;

        #endregion

        #region property

        [ConfigurationEntry(XRManager.XRConfigName, "xr.locomotion.turn_speed", 120.0f)]
        public static int TurnSpeed { get; set; }

        #endregion

        #region logic

        #region Awake

        private void Awake() {
            rig = GetComponent<XRRig>();
            controller = GetComponent<CharacterController>();
            driver = GetComponent<CharacterControllerDriver>();
            Transform locomotion = transform.Find("LocomotionSystem");
            locomotion.GetComponent<ActionBasedContinuousTurnProvider>().turnSpeed = TurnSpeed;
        }

        #endregion

        #region OnEnable

        private void OnEnable() {
            this.GainControl(true);
        }

        #endregion

        #region OnDisable

        private void OnDisable() {
            this.RevokeControl(true);
        }

        #endregion

        #region ConfigureCamera

        private void ConfigureCamera() {
            if (camera == null) {
                camera = CameraController.current;
                Console.AssertReference(camera);
            }
            camera.target = rig.cameraGameObject.transform;
            camera.TrackingFlags = CameraTrackingFlags.None;
            camera.nearClipPlane = NearClipDistance;
        }

        #endregion

        #region OnControlGained

        public ControlFlags OnControlGained() {
            ConfigureCamera();
            return ControlFlags.HideCursor | ControlFlags.LockCursor;
        }

        #endregion

        #region OnControlRevoked

        public void OnControlRevoked() {
            if (camera != null) {
                camera.target = null;
            }
        }

        #endregion

        #region FixedUpdate

        private void FixedUpdate() {
            UpdateCharacterController();
        }

        #endregion

        #region UpdateCharacterController

        private void UpdateCharacterController() {
            float height = Mathf.Clamp(rig.cameraInRigSpaceHeight, driver.minHeight, driver.maxHeight);
            Vector3 center = rig.cameraInRigSpacePos;
            center.y = (height * 0.5f) + controller.skinWidth;
            controller.height = height;
            controller.center = center;
        }

        #endregion

        #endregion

    }

}

#endif