// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEditorInternal;

namespace UnityEditor.Profiling.ModuleEditor
{
    class CounterCollector
    {
        public SortedDictionary<string, List<string>> LoadUnityCounters()
        {
            var availableCounterHandles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(availableCounterHandles);

            var unityCounters = new SortedDictionary<string, List<string>>();
            foreach (var availableCounterHandle in availableCounterHandles)
            {
                var description = ProfilerRecorderHandle.GetDescription(availableCounterHandle);
                if ((description.Flags & MarkerFlags.Counter) != 0 &&
                    (description.Flags & MarkerFlags.Script) == 0)
                {
                    var counterName = description.Name;
                    var categoryName = description.Category.Name;
                    AddToCountersCollection(categoryName, counterName, unityCounters);
                }
            }

            return unityCounters;
        }

        public SortedDictionary<string, List<string>> LoadUserCounters()
        {
            var userCounters = new SortedDictionary<string, List<string>>();
            using (var frameData = ProfilerDriver.GetRawFrameDataView(ProfilerDriver.lastFrameIndex, 0))
            {
                if (frameData.valid)
                {
                    var markers = new List<FrameDataView.MarkerInfo>();
                    frameData.GetMarkers(markers);

                    foreach (var markerInfo in markers)
                    {
                        if ((markerInfo.flags & MarkerFlags.Counter) != 0 &&
                            (markerInfo.flags & MarkerFlags.Script) != 0)
                        {
                            var counterName = markerInfo.name;
                            var categoryInfo = frameData.GetCategoryInfo(markerInfo.category);
                            var categoryName = categoryInfo.name;
                            AddToCountersCollection(categoryName, counterName, userCounters);
                        }
                    }
                }
            }

            return userCounters;
        }

        void AddToCountersCollection(string categoryName, string counterName, SortedDictionary<string, List<string>> collection)
        {
            if (collection.TryGetValue(categoryName, out List<string> counters))
            {
                // If we could use SortedSet rather than List (we are limited to .NET 3.5 currently), it would prevent the need to do this ordering and duplicate checking manually.
                for (int i = 0; i < counters.Count; i++)
                {
                    int index = counters.BinarySearch(counterName);
                    if (index < 0) // Prevent duplicates.
                    {
                        counters.Insert(~index, counterName);
                    }
                }
            }
            else
            {
                collection.Add(categoryName, new List<string>()
                {
                    counterName
                });
            }
        }
    }
}
