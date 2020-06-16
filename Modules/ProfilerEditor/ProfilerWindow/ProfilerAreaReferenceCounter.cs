// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditor.Profiling
{
    // With the addition of Profiler Counters, users can create modules than use counters from the built-in ProfilerAreas. Therefore we use a reference count to manage the areas in use and only disable one when no modules are using it.
    internal class ProfilerAreaReferenceCounter
    {
        const int k_InvalidIndex = -1;

        int[] areaReferenceCounts;

        public ProfilerAreaReferenceCounter()
        {
            var count = Enum.GetNames(typeof(ProfilerArea)).Length;
            areaReferenceCounts = new int[count];
        }

        public void IncrementArea(ProfilerArea area)
        {
            var index = IndexForArea(area);
            if (index == k_InvalidIndex)
            {
                return;
            }

            var referenceCount = areaReferenceCounts[index];
            bool wasZeroBeforeIncrement = (referenceCount == 0);
            areaReferenceCounts[index] = referenceCount + 1;

            if (wasZeroBeforeIncrement)
            {
                ProfilerDriver.SetAreaEnabled(area, true);
            }
        }

        public void DecrementArea(ProfilerArea area)
        {
            var index = IndexForArea(area);
            if (index == k_InvalidIndex)
            {
                return;
            }

            var referenceCount = areaReferenceCounts[index];
            areaReferenceCounts[index] = Mathf.Max(referenceCount - 1, 0);

            bool isZero = areaReferenceCounts[index] == 0;
            if (isZero)
            {
                ProfilerDriver.SetAreaEnabled(area, false);
            }
        }

        int IndexForArea(ProfilerArea area)
        {
            var index = (int)area;
            if (index < 0 || index >= areaReferenceCounts.Length)
            {
                index = k_InvalidIndex;
            }
            return index;
        }
    }

    internal static class ProfilerAreaReferenceCounterUtility
    {
        public static IEnumerable<ProfilerArea> ProfilerCategoryNameToArea(string categoryName)
        {
            // Map a counter's category to a built-in Profiler area. This is part of the transition away from ProfilerArea and to Category/Counter pairs. A counter's category potentially needs to be mapped to a ProfilerArea in order to ensure that an area is not disabled when a module may be using counters associated with the legacy ProfilerArea. It is expected a category that does not correspond to any ProfilerArea returns an empty collection. It is expected that a category that corresponds to many ProfilerAreas returns all corresponding ProfilerAreas. No category corresponds to the CPU or GPU areas. Therefore, all ProfilerAreas should be accounted for here minus CPU and GPU. This is asserted in the test ProfilerAreaReferenceCounterUtilityTests_HandlesEveryBuiltInProfilerAreaExceptCPUAndGPU.

            var areas = new List<ProfilerArea>();
            switch (categoryName)
            {
                case "Render":
                {
                    areas.Add(ProfilerArea.Rendering);
                    break;
                }

                case "Memory":
                {
                    areas.Add(ProfilerArea.Memory);
                    break;
                }

                case "Audio":
                {
                    areas.Add(ProfilerArea.Audio);
                    break;
                }

                case "Video":
                {
                    areas.Add(ProfilerArea.Video);
                    break;
                }

                case "Physics":
                {
                    // Both Physics and Physics2D modules will remain active if any counter from the Physics category is active.
                    areas.Add(ProfilerArea.Physics);
                    areas.Add(ProfilerArea.Physics2D);
                    break;
                }

                case "Network":
                {
                    // Both NetworkMessages and NetworkOperations modules will remain active if any counter from the Network category is active.
                    areas.Add(ProfilerArea.NetworkMessages);
                    areas.Add(ProfilerArea.NetworkOperations);
                    break;
                }

                case "GUI":
                {
                    // Both UI and UIDetails modules will remain active if any counter from the GUI category is active.
                    areas.Add(ProfilerArea.UI);
                    areas.Add(ProfilerArea.UIDetails);
                    break;
                }

                case "Lighting":
                {
                    areas.Add(ProfilerArea.GlobalIllumination);
                    break;
                }
            }

            return areas;
        }
    }
}
