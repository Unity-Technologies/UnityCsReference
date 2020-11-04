// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine.Rendering;

namespace UnityEditor.DeviceSimulation
{
    /// <summary>
    /// This class contains a subset of player settings that we need to initialize ScreenSimulation.
    /// </summary>
    [Serializable]
    internal class SimulationPlayerSettings
    {
        public ResolutionScalingMode resolutionScalingMode = ResolutionScalingMode.Disabled;
        public int targetDpi;
        public bool androidStartInFullscreen = true;

        public UIOrientation defaultOrientation = UIOrientation.AutoRotation;
        public bool allowedPortrait = true;
        public bool allowedPortraitUpsideDown = true;
        public bool allowedLandscapeLeft = true;
        public bool allowedLandscapeRight = true;

        public bool autoGraphicsAPI = true;
        public GraphicsDeviceType[] androidGraphicsAPIs = { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 };
        public GraphicsDeviceType[] iOSGraphicsAPIs = { GraphicsDeviceType.Metal };

        public SimulationPlayerSettings()
        {
            var serializedSettings = PlayerSettings.GetSerializedObject();
            serializedSettings.Update();

            resolutionScalingMode = (ResolutionScalingMode)serializedSettings.FindProperty("resolutionScalingMode").intValue;
            targetDpi = serializedSettings.FindProperty("targetPixelDensity").intValue;
            androidStartInFullscreen = serializedSettings.FindProperty("androidStartInFullscreen").boolValue;

            defaultOrientation = PlayerSettings.defaultInterfaceOrientation;
            allowedPortrait = PlayerSettings.allowedAutorotateToPortrait;
            allowedPortraitUpsideDown = PlayerSettings.allowedAutorotateToPortraitUpsideDown;
            allowedLandscapeLeft = PlayerSettings.allowedAutorotateToLandscapeLeft;
            allowedLandscapeRight = PlayerSettings.allowedAutorotateToLandscapeRight;

            if (!PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android))
                androidGraphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            if (!PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.iOS))
                iOSGraphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.iOS);
        }
    }
}
