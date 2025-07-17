// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.Profiling.Editor.UI
{
    readonly struct RangeBottlenecksModel
    {
        public RangeBottlenecksModel(
            ulong[] cpuDurationsNs,
            ulong[] gpuDurationsNs)
        {
            CpuDurationsNs = cpuDurationsNs;
            GpuDurationsNs = gpuDurationsNs;
        }

        public ulong[] CpuDurationsNs { get; }
        public ulong[] GpuDurationsNs { get; }

        public int ComputePercentageOfCpuValuesOverBudget(ulong budget)
        {
            return ComputePercentageOfValuesOverBudget(CpuDurationsNs, budget);
        }

        public int ComputePercentageOfGpuValuesOverBudget(ulong budget)
        {
            return ComputePercentageOfValuesOverBudget(GpuDurationsNs, budget);
        }

        int ComputePercentageOfValuesOverBudget(ReadOnlySpan<ulong> values, ulong budget)
        {
            var numberOfValuesOverBudget = 0;
            for (var i = 0; i < values.Length; ++i)
            {
                if (values[i] > budget)
                    numberOfValuesOverBudget++;
            }

            return Mathf.RoundToInt(((float)numberOfValuesOverBudget / values.Length) * 100);
        }
    }
}
