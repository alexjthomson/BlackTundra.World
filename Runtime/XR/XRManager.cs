#if ENABLE_VR

using BlackTundra.Foundation;
using BlackTundra.Foundation.IO;

using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

using Colour = BlackTundra.Foundation.ConsoleColour;

namespace BlackTundra.World.XR {

    /// <summary>
    /// Manages XR for the entire application.
    /// </summary>
    public static class XRManager {

        #region constant

        public const string XRConfigName = "xr";

        private static readonly ConsoleFormatter ConsoleFormatter = new ConsoleFormatter(nameof(XRManager));

        #endregion

        #region variable

#if UNITY_EDITOR
        /// <summary>
        /// Reference to the created XR device simulator (Mock HMD).
        /// </summary>
        private static GameObject deviceSimulator = null;
#endif

        #endregion

        #region property

        /// <inheritdoc cref="XRSettings.isDeviceActive"/>
        public static bool IsActive => XRSettings.isDeviceActive;

        /// <summary>
        /// Returns <c>true</c> when the application is ready to start using XR.
        /// </summary>
        public static bool IsReady => isEnabled && HasDevice;

        /// <summary>
        /// Returns <c>true</c> if an XR device has been registered with the XR system.
        /// </summary>
        public static bool HasDevice {
            get {
                string loadedDeviceName = XRSettings.loadedDeviceName;
                return loadedDeviceName != null && loadedDeviceName.Length > 0;
            }
        }

        /// <inheritdoc cref="XRSettings.enabled"/>
        [ConfigurationEntry(XRConfigName, "xr.enabled", true)]
        public static bool IsEnabled {
            get => isEnabled;
            set {
                isEnabled = value;
                if (isEnabled) {
                    ConsoleFormatter.Info($"XR enabled.");
                    XRGeneralSettings.Instance.Manager.InitializeLoader();
                } else {
                    ConsoleFormatter.Info($"XR disabled.");
                    XRGeneralSettings.Instance.Manager.DeinitializeLoader();
                }
            }
        }
        private static bool isEnabled = false;

        #endregion

        #region logic

        #region Initialise

        [CoreInitialise(int.MinValue + 1)]
        private static void Initialise() {
            UpdateState();
        }

        #endregion

        #region UpdateState

        public static void UpdateState() {
            if (IsActive) {
                QualitySettings.lodBias = 1.0f; // ensure the LOD bias is set correctly
            }
        }

        #endregion

        #region XRCommand

        [Command(
            name: "xr",
            description: "Manages XR systems within the application.",
            usage:
            "xr info" +
            "\n\tDisplays a table of XR information." +
            "\nxr update" +
            "\n\tForces updates all XR systems.",
            hidden: false
        )]
        private static bool XRCommand(CommandInfo info) {
            int argumentCount = info.args.Count;
            if (argumentCount == 0) {
                ConsoleWindow.Print("Expected arguments.");
                return false;
            }
            string arg = info.args[0].ToLower();
            switch (arg) {
                case "info": { // display vr information
                    if (argumentCount > 1) {
                        ConsoleWindow.Print(ConsoleUtility.UnknownArgumentMessage(info.args, 1));
                        return false;
                    }
                    ConsoleWindow.PrintTable(
                        new string[,] {
                            { "<b>XR Device</b>", string.Empty },
                            { $"<color=#{Colour.Gray.hex}>Has Device</color>", HasDevice ? "true" : "false"},
                            { $"<color=#{Colour.Gray.hex}>Device Active</color>", XRSettings.isDeviceActive ? "true" : "false" },
                            { $"<color=#{Colour.Gray.hex}>Primary Device</color>", XRSettings.loadedDeviceName ?? "None" },
                            { $"<color=#{Colour.Gray.hex}>Is Ready</color>", IsReady ? "true" : "false"},
                            { $"<color=#{Colour.Gray.hex}>Is Active</color>", IsActive ? "true" : "false"},
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
                        ConsoleWindow.Print(ConsoleUtility.UnknownArgumentMessage(info.args, 1));
                        return false;
                    }
                    UpdateState();
                    return true;
                }
                default: {
                    ConsoleWindow.Print(ConsoleUtility.UnknownArgumentMessage(info.args));
                    return false;
                }
            }
        }

        #endregion

        #endregion

    }

}

#endif