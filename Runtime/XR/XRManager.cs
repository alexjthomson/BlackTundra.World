#if ENABLE_VR

using BlackTundra.Foundation;
using BlackTundra.Foundation.IO;
using BlackTundra.Foundation.Utility;
using BlackTundra.Foundation.Collections;

using System;

using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

using Object = UnityEngine.Object;
using Console = BlackTundra.Foundation.Console;
using Colour = BlackTundra.Foundation.ConsoleColour;

namespace BlackTundra.World.XR {

    /// <summary>
    /// Manages XR for the entire application.
    /// </summary>
    public static class XRManager {

        #region constant

        /// <summary>
        /// <see cref="FileSystemReference"/> to where <see cref="configuration"/> is stored on the system.
        /// </summary>
        private static readonly FileSystemReference ConfigurationFSR = new FileSystemReference(
            FileSystem.LocalConfigDirectory + "xr.config",
            true,
            false
        );

        internal const string XRConfKey_Enabled = "xr.startup.enabled";
        internal const string XRConfKey_TurnSpeed = "xr.locomotion.turn_speed";

#if UNITY_EDITOR
        private const string XRDeviceSimulatorResourcePath = "XR/XRDeviceSimulator";
        private static readonly ResourceReference<GameObject> XRDeviceSimulator = new ResourceReference<GameObject>(XRDeviceSimulatorResourcePath);
        private static readonly string[] MockHMDAliases = new string[] {
            "MockHMD Display",
            "Mock HMD"
        };
#endif

        #endregion

        #region variable

#if UNITY_EDITOR
        /// <summary>
        /// Reference to the created XR device simulator (Mock HMD).
        /// </summary>
        private static GameObject deviceSimulator = null;
#endif

        /// <summary>
        /// <see cref="Configuration"/> for XR.
        /// </summary>
        internal static Configuration configuration = null;

        #endregion

        #region property

        /// <inheritdoc cref="XRSettings.isDeviceActive"/>
        public static bool IsActive => XRSettings.isDeviceActive;

        /// <inheritdoc cref="XRSettings.enabled"/>
        public static bool IsEnabled {
            get => XRGeneralSettings.Instance.Manager.isInitializationComplete;
            set {
                if (value) XRGeneralSettings.Instance.Manager.InitializeLoader();
                else XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            }
        }

        #endregion

        #region logic

        #region Initialise

        [CoreInitialise(int.MinValue)]
        private static void Initialise() {
            UpdateConfiguration();
            IsEnabled = configuration.GetBool(XRConfKey_Enabled);
            UpdateState();
        }

        #endregion

        #region UpdateConfiguration

        private static void UpdateConfiguration() {
            if (configuration == null) configuration = new Configuration();
            configuration = FileSystem.LoadConfiguration(ConfigurationFSR, configuration);
            configuration.ForceGet(XRConfKey_Enabled, false);
            configuration.ForceGet(XRConfKey_TurnSpeed, 120.0f);
            if (configuration.IsDirty) FileSystem.UpdateConfiguration(ConfigurationFSR, configuration);
        }

        #endregion

        #region UpdateState

        /// <summary>
        /// Updates the state of XR in the scene.
        /// </summary>
        private static void UpdateState() {
            
            #region Mock HMD
#if UNITY_EDITOR

            /*
             * Mock HMD is used for mocking XR input when there is no XR controller attached to the application.
             * This is strictly only used in the Unity Editor and is not compiled when the game is built.
             */
            
            string deviceName = XRSettings.loadedDeviceName; // get the loaded device name
            if (MockHMDAliases.Contains(deviceName)) { // check if the loaded device should be a mock device
                if (deviceSimulator == null) { // there is currently no loaded device simulator, create one
                    try {
                        deviceSimulator = Object.Instantiate(XRDeviceSimulator.Value);
                        deviceSimulator.name = deviceName;
                        Object.DontDestroyOnLoad(deviceSimulator);
                        Console.Info("Instantiated XR device simulator (Mock HMD).");
                    } catch (Exception exception) {
                        Console.Error("Failed to instantiate XR device simulator (Mock HMD).", exception);
                        if (deviceSimulator != null) {
                            Object.Destroy(deviceSimulator);
                            deviceSimulator = null;
                        }
                    }
                }
            } else if (deviceSimulator != null) { // there is a device simulator active that should be destroyed since the loaded device is not a mock device
                Console.Info("Destroying device simulator.");
                Object.Destroy(deviceSimulator);
            }
#endif
            #endregion

        }

        #endregion

        #region XRCommand

        [Command(
            "xr"
        )]
        private static bool XRCommand(CommandInfo info) {
            int argumentCount = info.args.Count;
            if (argumentCount == 0) {
                info.context.Print("Expected arguments.");
                return false;
            }
            string arg = info.args[0].ToLower();
            switch (arg) {
                case "info": { // display vr information
                    if (argumentCount > 1) {
                        info.context.Print(ConsoleUtility.UnknownArgumentMessage(info.args, 1));
                        return false;
                    }
                    info.context.PrintTable(
                        new string[,] {
                            { "<b>XR Device</b>", string.Empty },
                            { $"<color=#{Colour.Gray.hex}>Device Active</color>", XRSettings.isDeviceActive ? "true" : "false" },
                            { $"<color=#{Colour.Gray.hex}>Primary Device</color>", XRSettings.loadedDeviceName ?? "None" },
#if UNITY_EDITOR
                            { $"<color=#{Colour.Gray.hex}>Device Simulator</color>", deviceSimulator != null ? deviceSimulator.name : "None" },
#endif
                            { string.Empty, string.Empty },
                            { "<b>Rendering</b>", string.Empty },
                            { $"<color=#{Colour.Gray.hex}>Texture Size</color>", $"{XRSettings.eyeTextureWidth}x{XRSettings.eyeTextureHeight}" },
                            { $"<color=#{Colour.Gray.hex}>Resolution Scale</color>", XRSettings.eyeTextureResolutionScale.ToString() },
                            { $"<color=#{Colour.Gray.hex}>Texture Dimension</color>", XRSettings.deviceEyeTextureDimension.ToString() },
                            { string.Empty, string.Empty },
                            { $"<color=#{Colour.Gray.hex}>Game View Rendering Mode</color>", XRSettings.gameViewRenderMode.ToString() },
                            { $"<color=#{Colour.Gray.hex}>Stereo Rendering Mode</color>", XRSettings.stereoRenderingMode.ToString() },
                            { $"<color=#{Colour.Gray.hex}>Viewport Scale</color>", XRSettings.renderViewportScale.ToString() },
                            { string.Empty, string.Empty },
                            { $"<color=#{Colour.Gray.hex}>Occlusion Mesh</color>", XRSettings.useOcclusionMesh ? "enabled" : "disabled" },
                            { $"<color=#{Colour.Gray.hex}>Occlusion Mask Scale</color>", XRSettings.occlusionMaskScale.ToString() },
                        }, false, true
                    );
                    return true;
                }
                case "update": {
                    if (argumentCount > 1) {
                        info.context.Print(ConsoleUtility.UnknownArgumentMessage(info.args, 1));
                        return false;
                    }
                    UpdateState();
                    return true;
                }
                default: {
                    info.context.Print(ConsoleUtility.UnknownArgumentMessage(info.args));
                    return false;
                }
            }
        }

        #endregion

        #endregion

    }

}

#endif