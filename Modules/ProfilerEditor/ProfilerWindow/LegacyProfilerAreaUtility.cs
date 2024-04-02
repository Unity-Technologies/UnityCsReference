// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.Profiling;

namespace Unity.Profiling.Editor
{
    internal class LegacyProfilerAreaUtility
    {
        static readonly Dictionary<ProfilerArea, string> k_ProfilerAreasToCategoriesMap = new Dictionary<ProfilerArea, string>()
        {
            { ProfilerArea.CPU, ProfilerCategory.Scripts.Name },
            { ProfilerArea.GPU, ProfilerCategory.GPU.Name },
            { ProfilerArea.Rendering, ProfilerCategory.Render.Name },
            { ProfilerArea.Memory, ProfilerCategory.Memory.Name },
            { ProfilerArea.Audio, ProfilerCategory.Audio.Name },
            { ProfilerArea.Video, ProfilerCategory.Video.Name },
            { ProfilerArea.Physics, ProfilerCategory.Physics.Name },
            { ProfilerArea.Physics2D, ProfilerCategory.Physics2D.Name },
            { ProfilerArea.NetworkMessages, ProfilerCategory.Network.Name },
            { ProfilerArea.NetworkOperations, ProfilerCategory.Network.Name },
            { ProfilerArea.UI, ProfilerCategory.Gui.Name },
            { ProfilerArea.UIDetails, ProfilerCategory.Gui.Name },
            { ProfilerArea.GlobalIllumination, ProfilerCategory.Lighting.Name },
            { ProfilerArea.VirtualTexturing, ProfilerCategory.VirtualTexturing.Name },
        };

        public static string ProfilerAreaToCategoryName(ProfilerArea area)
        {
            return k_ProfilerAreasToCategoriesMap.TryGetValue(area, out var categoryName) ? categoryName : null;
        }
    }
}
