// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEditor
{
    [NativeHeader("Runtime/Misc/PlayerSettings.h")]
    [NativeHeader("Runtime/Scripting/ScriptingExportUtility.h")]
    [NativeHeader("Runtime/Graphics/DrawSplashScreenAndWatermarks.h")]
    internal partial class PlayerSettingsSplashScreenEditor
    {
        internal static extern bool licenseAllowsDisabling
        {
            [FreeFunction("GetPlayerSettings().GetSplashScreenSettings().CanDisableSplashScreen")]
            get;
        }

        [FreeFunction("GetSplashScreenBackgroundColor")]
        internal static extern Color GetSplashScreenActualBackgroundColor();

        [FreeFunction("GetSplashScreenBackground")]
        internal static extern Texture2D GetSplashScreenActualBackgroundImage(Rect windowRect);

        [FreeFunction("GetSplashScreenBackgroundUvs")]
        internal static extern Rect GetSplashScreenActualUVs(Rect windowRect);
    }
}
