// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.DeviceSimulation
{
    internal static class SimulatorUtilities
    {
        public static bool IsLandscape(ScreenOrientation orientation)
        {
            if (orientation == ScreenOrientation.Landscape || orientation == ScreenOrientation.LandscapeLeft ||
                orientation == ScreenOrientation.LandscapeRight)
                return true;

            return false;
        }
    }
}
