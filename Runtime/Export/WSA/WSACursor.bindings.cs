// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.WSA
{
    [NativeConditional("PLATFORM_WINRT")]
    [NativeHeader("PlatformDependent/MetroPlayer/MetroCursor.h")]
    public static class Cursor
    {
        [FreeFunction("Cursors::SetHardwareCursor")]
        public static extern void SetCustomCursor(uint id);
    }
}
