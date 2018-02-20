// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    // General settings for how the GUI behaves
    [NativeHeader("Modules/IMGUI/GUISkin.bindings.h")]
    partial class GUISettings
    {
        private static extern float Internal_GetCursorFlashSpeed();
    }
}
