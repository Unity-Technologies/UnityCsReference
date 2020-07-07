// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditorInternal.Profiling;
using UnityEngine;

namespace UnityEditor.Profiling.ModuleEditor
{
    [System.Serializable]
    class ModuleData
    {
        public const int k_MaximumChartCountersCount = UnityEditorInternal.ProfilerChart.k_MaximumSeriesCount;

        [SerializeField] string m_Name;
        [SerializeField] List<ProfilerCounterData> m_ChartCounters = new List<ProfilerCounterData>();
        [SerializeField] List<ProfilerCounterData> m_DetailCounters = new List<ProfilerCounterData>();
        [SerializeField] bool m_IsEditable;
        [SerializeField] string m_CurrentProfilerModuleName;
        [SerializeField] EditedState m_EditedState;

        public ModuleData(string name, bool isEditable, bool newlyCreatedModule = false)
        {
            m_Name = name;
            m_CurrentProfilerModuleName = m_Name;
            m_IsEditable = isEditable;
            m_EditedState = (newlyCreatedModule) ? EditedState.Created : EditedState.NoChanges;
        }

        public bool isEditable => m_IsEditable;
        public EditedState editedState => m_EditedState;
        public string name => m_Name;
        public string currentProfilerModuleName => m_CurrentProfilerModuleName;
        public List<ProfilerCounterData> chartCounters => m_ChartCounters;
        // We currently don't allow users to specify detail counters in the UI. Instead we mirror the chart counters in the details view.
        public List<ProfilerCounterData> detailCounters => m_ChartCounters;
        public bool hasMaximumChartCounters => m_ChartCounters.Count >= k_MaximumChartCountersCount;

        public static List<ModuleData> CreateDataRepresentationOfProfilerModules(List<ProfilerModuleBase> modules)
        {
            var moduleDatas = new List<ModuleData>(modules.Count);
            for (int i = 0; i < modules.Count; i++)
            {
                var module = modules[i];
                var moduleData = CreateWithProfilerModule(module);
                moduleDatas.Add(moduleData);
            }

            return moduleDatas;
        }

        static ModuleData CreateWithProfilerModule(ProfilerModuleBase module)
        {
            var isEditable = module is DynamicProfilerModule;
            var moduleData = new ModuleData(module.name, isEditable);

            var chartCounters = new List<ProfilerCounterData>(module.chartCounters);
            moduleData.m_ChartCounters = chartCounters;

            var detailCounters = new List<ProfilerCounterData>(module.detailCounters);
            moduleData.m_DetailCounters = detailCounters;

            return moduleData;
        }

        public void SetName(string name)
        {
            m_Name = name;
            SetUpdatedEditedStateIfNoChanges();
        }

        public void AddChartCounter(ProfilerCounterData counter)
        {
            m_ChartCounters.Add(counter);
            SetUpdatedEditedStateIfNoChanges();
        }

        public void RemoveChartCounterAtIndex(int index)
        {
            m_ChartCounters.RemoveAt(index);
            SetUpdatedEditedStateIfNoChanges();
        }

        public void SetUpdatedEditedStateForOrderIndexChange()
        {
            SetUpdatedEditedStateIfNoChanges();
        }

        public bool ContainsChartCounter(ProfilerCounterData counter)
        {
            bool containsChartCounter = false;
            foreach (var chartCounter in m_ChartCounters)
            {
                if (chartCounter.m_Category.Equals(counter.m_Category) &&
                    chartCounter.m_Name.Equals(counter.m_Name))
                {
                    containsChartCounter = true;
                    break;
                }
            }

            return containsChartCounter;
        }

        public bool ContainsChartCounter(string counter, string category)
        {
            return ContainsChartCounter(new ProfilerCounterData()
            {
                m_Name = counter,
                m_Category = category,
            });
        }

        void SetUpdatedEditedStateIfNoChanges()
        {
            if (m_EditedState == EditedState.NoChanges)
            {
                m_EditedState = EditedState.Updated;
            }
        }

        public enum EditedState
        {
            NoChanges,
            Updated,
            Created
        }
    }
}
