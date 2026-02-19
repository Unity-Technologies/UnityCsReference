// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling.Editor;

namespace UnityEditor.Profiling
{
    [System.Serializable]
    struct ProfilerCounterData : IEquatable<ProfilerCounterData>
    {
        public string m_Category;
        public string m_Name;
        public string m_Description;

        public static ProfilerCounterData FromProfilerCounterDescriptor(in ProfilerCounterDescriptor counter)
        {
            return new ProfilerCounterData()
            {
                m_Name = counter.Name,
                m_Category = counter.CategoryName,
                m_Description = counter.Description
            };
        }

        public ProfilerCounterDescriptor ToProfilerCounterDescriptor()
        {
            return new ProfilerCounterDescriptor(m_Name, m_Description, m_Category);
        }

        public bool Equals(ProfilerCounterData other)
        {
            if (m_Category != other.m_Category)
                return false;
            if (m_Name != other.m_Name)
                return false;

            if (string.IsNullOrEmpty(m_Description) && string.IsNullOrEmpty(other.m_Description))
                return true;
            return m_Description  == other.m_Description;
        }
    }

    static class ProfilerCounterDataUtility
    {
        public static ProfilerCounterDescriptor[] ConvertFromLegacyCounterDatas(List<ProfilerCounterData> legacyCounters)
        {
            if (legacyCounters == null)
                return System.Array.Empty<ProfilerCounterDescriptor>();

            var counters = new List<ProfilerCounterDescriptor>(legacyCounters.Count);
            foreach (var legacyCounter in legacyCounters)
            {
                var counter = legacyCounter.ToProfilerCounterDescriptor();
                counters.Add(counter);
            }

            return counters.ToArray();
        }

        public static List<ProfilerCounterData> ConvertToLegacyCounterDatas(ProfilerCounterDescriptor[] counters)
        {
            if (counters == null)
                return new List<ProfilerCounterData>();

            var legacyCounters = new List<ProfilerCounterData>(counters.Length);
            foreach (var counter in counters)
            {
                var legacyCounter = ProfilerCounterData.FromProfilerCounterDescriptor(counter);
                legacyCounters.Add(legacyCounter);
            }

            return legacyCounters;
        }
    }
}
