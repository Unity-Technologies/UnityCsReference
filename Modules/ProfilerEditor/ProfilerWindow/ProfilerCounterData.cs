// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Profiling.Editor;

namespace UnityEditor.Profiling
{
    [System.Serializable]
    struct ProfilerCounterData
    {
        public string m_Category;
        public string m_Name;

        public static ProfilerCounterData FromProfilerCounterDescriptor(ProfilerCounterDescriptor counter)
        {
            return new ProfilerCounterData()
            {
                m_Name = counter.Name,
                m_Category = counter.CategoryName,
            };
        }

        public ProfilerCounterDescriptor ToProfilerCounterDescriptor()
        {
            return new ProfilerCounterDescriptor(m_Name, m_Category);
        }
    }

    static class ProfilerCounterDataUtility
    {
        public static ProfilerCounterDescriptor[] ConvertFromLegacyCounterDatas(List<ProfilerCounterData> legacyCounters)
        {
            var capacity = (legacyCounters != null) ? legacyCounters.Count : 0;
            var counters = new List<ProfilerCounterDescriptor>(capacity);
            foreach (var legacyCounter in legacyCounters)
            {
                var counter = legacyCounter.ToProfilerCounterDescriptor();
                counters.Add(counter);
            }

            return counters.ToArray();
        }

        public static List<ProfilerCounterData> ConvertToLegacyCounterDatas(ProfilerCounterDescriptor[] counters)
        {
            var capacity = (counters != null) ? counters.Length : 0;
            var legacyCounters = new List<ProfilerCounterData>(capacity);
            foreach (var counter in counters)
            {
                var legacyCounter = ProfilerCounterData.FromProfilerCounterDescriptor(counter);
                legacyCounters.Add(legacyCounter);
            }

            return legacyCounters;
        }
    }
}
