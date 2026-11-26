// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Profiling;

namespace Unity.Profiling.Editor
{
    internal class LegacyProfilerAreaUtility
    {
        // The first category is the one used for the area's counters. The others are the categories the native code maps to the area (LegacyProfilerModes.cpp), which the area's data depends on.
        [NoAutoStaticsCleanup] // Fixed content, safe to persist
        static readonly Dictionary<ProfilerArea, string[]> k_ProfilerAreasToCategoriesMap = new Dictionary<ProfilerArea, string[]>()
        {
            { ProfilerArea.CPU, new[] { ProfilerCategory.Scripts.Name } },
            { ProfilerArea.GPU, new[] { ProfilerCategory.GPU.Name } },
            { ProfilerArea.Rendering, new[] { ProfilerCategory.Render.Name } },
            { ProfilerArea.Memory, new[] { ProfilerCategory.Memory.Name } },
            { ProfilerArea.Audio, new[] { ProfilerCategory.Audio.Name, "Audio Job", "Audio Update Job" } },
            { ProfilerArea.Video, new[] { ProfilerCategory.Video.Name } },
            { ProfilerArea.Physics, new[] { ProfilerCategory.Physics.Name } },
            { ProfilerArea.Physics2D, new[] { ProfilerCategory.Physics.Name, ProfilerCategory.Physics2D.Name } },
            { ProfilerArea.NetworkMessages, new[] { ProfilerCategory.Network.Name } },
            { ProfilerArea.NetworkOperations, new[] { ProfilerCategory.Network.Name, "Network Operations" } },
            { ProfilerArea.UI, new[] { ProfilerCategory.Gui.Name, "UI Layout", "UI Render", "Text", ProfilerCategory.UIToolkit.Name } },
            { ProfilerArea.UIDetails, new[] { ProfilerCategory.Gui.Name, "UI Details" } },
            { ProfilerArea.GlobalIllumination, new[] { ProfilerCategory.Lighting.Name } },
            { ProfilerArea.VirtualTexturing, new[] { ProfilerCategory.VirtualTexturing.Name } },
        };

        public static string CounterCategoryNameForLegacyProfilerArea(ProfilerArea area)
        {
            return k_ProfilerAreasToCategoriesMap.TryGetValue(area, out var categoryNames) ? categoryNames[0] : null;
        }

        public static string[] CategoryNamesForLegacyProfilerArea(ProfilerArea area)
        {
            return k_ProfilerAreasToCategoriesMap.TryGetValue(area, out var categoryNames) ? categoryNames : Array.Empty<string>();
        }
    }
}
