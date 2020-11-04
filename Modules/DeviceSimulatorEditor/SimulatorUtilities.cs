// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.DeviceSimulation
{
    internal enum ResolutionScalingMode
    {
        Disabled = 0,
        FixedDpi = 1
    }

    internal enum SimulationState{ Enabled, Disabled }

    [Serializable]
    internal class SimulatorSerializationStates
    {
        public int scale = 20;
        public bool fitToScreenEnabled = true;
        public int rotationDegree;
        public bool highlightSafeAreaEnabled;
        public string friendlyName = string.Empty;
    }

    internal static class SimulatorUtilities
    {
        public static ScreenOrientation ToScreenOrientation(UIOrientation original)
        {
            switch (original)
            {
                case UIOrientation.Portrait:
                    return ScreenOrientation.Portrait;
                case UIOrientation.PortraitUpsideDown:
                    return ScreenOrientation.PortraitUpsideDown;
                case UIOrientation.LandscapeLeft:
                    return ScreenOrientation.LandscapeLeft;
                case UIOrientation.LandscapeRight:
                    return ScreenOrientation.LandscapeRight;
                case UIOrientation.AutoRotation:
                    return ScreenOrientation.AutoRotation;
            }
            throw new ArgumentException($"Unexpected value of UIOrientation {original}");
        }

        public static ScreenOrientation RotationToScreenOrientation(int angle)
        {
            ScreenOrientation orientation = ScreenOrientation.Portrait;
            if (angle > 315 || angle <= 45)
            {
                orientation = ScreenOrientation.Portrait;
            }
            else if (angle > 45 && angle <= 135)
            {
                orientation = ScreenOrientation.LandscapeRight;
            }
            else if (angle > 135 && angle <= 225)
            {
                orientation = ScreenOrientation.PortraitUpsideDown;
            }
            else if (angle > 225 && angle <= 315)
            {
                orientation = ScreenOrientation.LandscapeLeft;
            }
            return orientation;
        }

        public static bool IsLandscape(ScreenOrientation orientation)
        {
            if (orientation == ScreenOrientation.Landscape || orientation == ScreenOrientation.LandscapeLeft ||
                orientation == ScreenOrientation.LandscapeRight)
                return true;

            return false;
        }
    }
}
