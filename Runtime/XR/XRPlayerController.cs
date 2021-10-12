#if ENABLE_VR

using BlackTundra.Foundation;
using BlackTundra.Foundation.Collections;
using BlackTundra.Foundation.Control;
using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Utility;
using BlackTundra.World.CameraSystem;
using BlackTundra.World.Player;

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace BlackTundra.World.XR {

    [DisallowMultipleComponent]
    [RequireComponent(typeof(XRRig))]
    [RequireComponent(typeof(PlayerController))]
    public sealed class XRPlayerController : MonoBehaviour, IControllable {

        #region constant

        private const string ConfigurationName = "xr";

        private const float NearClipDistance = 0.01f;

        private const float DefaultLookSensitivity = 15.0f;

        private const float DefaultMoveSpeed = 15.0f;

        #endregion

        #region variable

        /// <summary>
        /// Right <see cref="XRPlayerHand"/> component.
        /// </summary>
        [SerializeField]
        private XRPlayerHand rightHand;

        /// <summary>
        /// Left <see cref="XRPlayerHand"/> component.
        /// </summary>
        [SerializeField]
        private XRPlayerHand leftHand;

        /// <summary>
        /// <see cref="CameraController"/> used for XR.
        /// </summary>
#if UNITY_EDITOR
        new
#endif
        private CameraController camera;

        /// <summary>
        /// <see cref="XRRig"/> used for VR.
        /// </summary>
        private XRRig rig;

        /// <summary>
        /// <see cref="PlayerController"/> used for movement and physics.
        /// </summary>
        private PlayerController controller;

        private Vector2 inputLook;

        private Vector2 inputMove;

        /// <summary>
        /// Look sensitivity used for looking around.
        /// </summary>
        private float lookSensitivity = DefaultLookSensitivity;

        /// <summary>
        /// Base move speed.
        /// </summary>
        private float moveSpeed = DefaultMoveSpeed;

        /// <summary>
        /// <see cref="Configuration"/> used to store XR settings.
        /// </summary>
        private Configuration configuration;

        #endregion

        #region property

        #endregion

        #region logic

        #region Awake

        private void Awake() {
            rig = GetComponent<XRRig>();
            controller = GetComponent<PlayerController>();
        }

        #endregion

        #region OnEnable

        private void OnEnable() {
            Console.AssertReference(rightHand);
            Console.AssertReference(leftHand);
            this.GainControl();
        }

        #endregion

        #region Update

        private void Update() {

            float deltaTime = Time.deltaTime;

            #region input
            InputDevice device = rightHand.device;
            if (device.isValid) { // right hand
                if (!device.TryGetFeatureValue(CommonUsages.primary2DAxis, out inputLook)) inputLook = Vector2.zero;
            }
            device = leftHand.device;
            if (device.isValid) { // left hand
                if (!device.TryGetFeatureValue(CommonUsages.primary2DAxis, out inputMove)) inputMove = Vector2.zero;
            }
            #endregion

            #region look

            Quaternion headYaw = Quaternion.Euler(0.0f, rig.cameraGameObject.transform.eulerAngles.y, 0.0f);
            float lookYaw = inputLook.x * lookSensitivity * deltaTime;
            transform.rotation = Quaternion.Euler(0.0f, transform.eulerAngles.y + lookYaw, 0.0f);

            #endregion

            #region movement

            controller.SetMotiveVelocity(headYaw * new Vector3(inputMove.x, 0.0f, inputMove.y) * moveSpeed);

            #endregion

        }

        #endregion

        #region RefreshConfiguration

        private void RefreshConfiguration() {
            configuration = FileSystem.LoadConfiguration(ConfigurationName, configuration);
            lookSensitivity = configuration.ForceGet("look.sensitivity.y", DefaultLookSensitivity);
            moveSpeed = configuration.ForceGet("move.speed", DefaultMoveSpeed);
            FileSystem.UpdateConfiguration(ConfigurationName, configuration);
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

        #region ConfigureHands

        private void ConfigureInput() {
            List<InputDevice> devices = new List<InputDevice>();
            InputDevice rightController = GetInputDevice(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right, devices);
            rightHand.device = rightController;
            devices.Clear();
            InputDevice leftController = GetInputDevice(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left, devices);
            leftHand.device = leftController;
        }

        #endregion

        #region GetInputDevice

        private InputDevice GetInputDevice(in InputDeviceCharacteristics characteristics, in List<InputDevice> devices) {
            InputDevices.GetDevicesWithCharacteristics(characteristics, devices);
            int deviceCount = devices.Count;
            Console.Info($"[{nameof(XRPlayerController)}] Discovered {deviceCount} XR device(s) for characteristics: {((uint)characteristics).ToHex()}.");
            return deviceCount > 0 ? devices[0] : default;
        }

        #endregion

        #region OnControlGained

        public ControlFlags OnControlGained(in ControlUser user) {
            RefreshConfiguration();
            ConfigureCamera();
            ConfigureInput();
            return ControlFlags.HideCursor | ControlFlags.LockCursor;
        }

        #endregion

        #region OnControlRevoked

        public ControlFlags OnControlRevoked(in ControlUser controlUser) {
            if (camera != null) {
                camera.target = null;
            }
            return ControlUser.ControlFlags;
        }

        #endregion

        #endregion

    }

}

#endif