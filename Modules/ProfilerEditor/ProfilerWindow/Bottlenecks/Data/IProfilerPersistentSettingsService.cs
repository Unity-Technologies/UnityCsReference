// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Profiling.Editor
{
    interface IProfilerPersistentSettingsService : IDisposable
    {
        bool IsBottleneckViewVisible { get; set; }

        ulong TargetFrameDurationNs { get; set; }

        int MaximumFrameCount { get; }

        event Action TargetFrameDurationChanged;

        event Action MaximumFrameCountChanged;
    }
}
