// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace UnityEditorInternal.Profiling
{
    // Layout of the ScreenshotTextureInfo metadata entry.
    // Must be kept in sync with profiling::ScreenshotTextureInfo (Runtime/Profiler/Public/Profiler.h).
    [StructLayout(LayoutKind.Sequential)]
    internal struct ScreenshotTextureInfo
    {
        public int Format; // TextureFormat of the captured image; currently always RGBA32
        public int Width;
        public int Height;
    }
}
