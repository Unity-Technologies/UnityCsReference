// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/HomeWindow/HomeWindow.h")]
    static class HomeWindow
    {
        // NOTE: Keep in sync with enum in Editor/Src/HomeWindow/HomeWindow.h
        public enum HomeMode
        {
            Login,
            License,
            Launching,
            NewProjectOnly,
            OpenProjectOnly,
            ManageLicense,
            Welcome,
            Tutorial,
        }

        [NativeMethod("StaticShow")]
        public static extern bool Show(HomeMode mode);
    }
}
