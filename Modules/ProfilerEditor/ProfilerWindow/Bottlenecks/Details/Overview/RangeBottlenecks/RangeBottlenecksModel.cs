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

        static int ComputePercentageOfValuesOverBudget(ReadOnlySpan<ulong> values, ulong budget)
        {
            var numberOfValuesOverBudget = 0;
            var numberOfEmptyFrames = 0; // Avoid incomplete frames
            foreach (var value in values)
            {
                if (value == 0)
                    numberOfEmptyFrames++;
                else if (value > budget)
                    numberOfValuesOverBudget++;
            }

            // If we've somehow got all empty frames, just return 0 immediately, we don't have enough frames for any
            // valid percentage to be generated
            if (numberOfEmptyFrames >= values.Length)
                return 0;

            return Mathf.RoundToInt((float)numberOfValuesOverBudget / (values.Length - numberOfEmptyFrames) * 100);
        }
    }
}
