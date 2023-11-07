// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine.Profiling;

namespace Unity.Profiling.Editor
{
    // Used by the Profiler window to manage the collection states of Profiler categories. Any categories being used by modules in the Profiler window will be enabled for collection on the target device. Once a category is no longer being used by any modules, it will be disabled for collection on the target device.
    internal class ProfilerCategoryActivator
    {
        Dictionary<string, UInt16> categoryReferenceCounts;

        public ProfilerCategoryActivator()
        {
            categoryReferenceCounts = new Dictionary<string, ushort>();
        }

        public void RetainCategory(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName))
                return;

            categoryReferenceCounts.TryGetValue(categoryName, out var referenceCount);

            bool wasZero = (referenceCount == 0);
            categoryReferenceCounts[categoryName] = ++referenceCount;

            if (wasZero)
                ProfilerDriver.SetCategoryEnabled(categoryName, true);
        }

        public void ReleaseCategory(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName))
                return;

            if (categoryReferenceCounts.TryGetValue(categoryName, out var referenceCount))
            {
                if (referenceCount > 0)
                    referenceCount--;

                categoryReferenceCounts[categoryName] = referenceCount;

                if (referenceCount == 0)
                    ProfilerDriver.SetCategoryEnabled(categoryName, false);
            }
        }
    }
}
