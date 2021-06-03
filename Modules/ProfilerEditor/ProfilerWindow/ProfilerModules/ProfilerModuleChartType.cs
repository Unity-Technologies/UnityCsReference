// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Profiling.Editor
{
    public enum ProfilerModuleChartType
    {
        Line = 0,
        StackedTimeArea, // Legacy. Stacked charts were originally only built for use with time, e.g. they show an FPS grid and don't scale appropriately for other units.
    }
}
