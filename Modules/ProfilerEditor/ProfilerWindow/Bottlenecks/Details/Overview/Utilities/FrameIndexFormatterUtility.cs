// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Profiling.Editor.UI
{
    static class FrameIndexFormatterUtility
    {
        public static string DisplayStringForFrameIndex(int frameIndex)
        {
            // The Profiler window displays frames as the frame index plus one.
            return (frameIndex + 1).ToString();
        }
    }
}
