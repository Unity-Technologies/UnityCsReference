// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Profiling.Editor
{
    internal interface IProfilerPersistentSettingsService : IDisposable
    {
        interface IValue
        {
            public string Get();
            public void Set(string value);
            public void Delete();
            public IValue Rename(string newKey);
        }

        bool IsBottleneckViewVisible { get; set; }

        ulong TargetFrameDurationNs { get; set; }

        int MaximumFrameCount { get; }

        int BottleneckDetailsViewSelectedSummaryType { get; set; }

        event Action TargetFrameDurationChanged;

        event Action MaximumFrameCountChanged;

        IValue ChartCountersOrder(string chartNameKey);
        IValue ChartCountersVisible(string chartNameKey);
    }
}
