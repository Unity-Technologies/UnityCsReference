// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;
using static Unity.Profiling.Editor.UI.SystemsImpactModel;

namespace Unity.Profiling.Editor.UI
{
    // Abstract base type for SystemsImpactModel builders containing data shared across builders.
    abstract class SystemsImpactModelBuilder
    {
        const int k_NumberOfTopSystems = 3;
        protected static readonly string[] k_CpuLegacyStatisticNames = new string[]
        {
                "Rendering",
                "Scripts",
                "Physics",
                "Animation",
                "GarbageCollector",
                "VSync",
                "Global Illumination",
                "UI",
                "Others"
        };
        protected static readonly Color[] k_CpuLegacyStatisticColors = new Color[]
        {
            ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.Render),
            ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.Scripts),
            ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.Physics),
            ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.Animation),
            ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.GC),
            ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.VSync),
            ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.Lighting),
            ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.UI),
            ProfilerUnsafeUtility.GetCategoryColor(ProfilerCategoryColor.Other)
        };
        protected static readonly Color[] k_CpuLegacyStatisticColorBlindColors = new Color[]
        {
            ProfilerColors.colorBlindSafeColors[(int)ProfilerCategoryColor.Render],
            ProfilerColors.colorBlindSafeColors[(int)ProfilerCategoryColor.Scripts],
            ProfilerColors.colorBlindSafeColors[(int)ProfilerCategoryColor.Physics],
            ProfilerColors.colorBlindSafeColors[(int)ProfilerCategoryColor.Animation],
            ProfilerColors.colorBlindSafeColors[(int)ProfilerCategoryColor.GC],
            ProfilerColors.colorBlindSafeColors[(int)ProfilerCategoryColor.VSync],
            ProfilerColors.colorBlindSafeColors[(int)ProfilerCategoryColor.Lighting],
            ProfilerColors.colorBlindSafeColors[(int)ProfilerCategoryColor.UI],
            ProfilerColors.colorBlindSafeColors[(int)ProfilerCategoryColor.Other]
        };

        protected float GetLegacyStatisticValueAsFloat(RawFrameDataView mainThreadData, string legacyStatisticName)
        {
            var legacyStatisticValue = mainThreadData.GetLegacyStatisticValueAsFloat(
                ProfilerArea.CPU,
                legacyStatisticName);

            // It is possible for legacy stats to have (bad) negative time
            // values. For example, we have seen a negative Rendering time
            // in the first frame of the Profiler attaching to a target.
            if (legacyStatisticValue < 0f)
                legacyStatisticValue = 0f;

            return legacyStatisticValue;
        }

        protected SystemsImpactModel BuildModelFromSystemImpacts(Range frameRange, SystemImpact[] systemImpacts)
        {
            // Sort descending.
            Array.Sort(systemImpacts, (a, b) =>
            {
                return b.DurationNs.CompareTo(a.DurationNs);
            });

            // Keep only the top k_NumberOfTopSystems entries.
            var topSystemImpacts = new SystemImpact[k_NumberOfTopSystems];
            Array.Copy(systemImpacts, topSystemImpacts, k_NumberOfTopSystems);

            // Build model.
            return new SystemsImpactModel(frameRange, topSystemImpacts);
        }
    }
}
