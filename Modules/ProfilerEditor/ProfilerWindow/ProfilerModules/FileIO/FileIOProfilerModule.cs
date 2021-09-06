// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    [ProfilerModuleMetadata("File Access", typeof(LocalizationResource), IconPath = "Profiler.FileAccess")]
    internal class FileIOProfilerModule : ProfilerModuleBase
    {
        FileIOProfilerView m_FileIOProfilerView;

        static readonly string[] k_FileIOChartCounterNames =
        {
            "Files Opened",
            "Files Closed",
            "File Seeks",
            "Reads in Flight",
            "File Handles Open"
        };

        static readonly string k_FileIOCountersCategoryName = ProfilerCategory.FileIO.Name;

        public FileIOProfilerModule() : base(ProfilerModuleChartType.Line) {}

        private protected override int defaultOrderIndex => 14;

        internal override void OnEnable()
        {
            base.OnEnable();
            if (m_FileIOProfilerView == null)
            {
                m_FileIOProfilerView = new FileIOProfilerView();
            }
        }

        private protected override bool ReadActiveState()
        {
            return EditorPrefs.GetBool(activeStatePreferenceKey, false);
        }

        private protected override void SaveActiveState()
        {
            EditorPrefs.SetBool(activeStatePreferenceKey, active);
        }

        public override void DrawToolbar(Rect position)
        {
            m_FileIOProfilerView.DrawToolbar(position);
        }

        public override void DrawDetailsView(Rect position)
        {
            m_FileIOProfilerView?.DrawUIPane(ProfilerWindow);
        }

        protected override List<ProfilerCounterData> CollectDefaultChartCounters()
        {
            var chartCounters = new List<ProfilerCounterData>(k_FileIOChartCounterNames.Length);
            foreach (var counterName in k_FileIOChartCounterNames)
            {
                chartCounters.Add(new ProfilerCounterData()
                {
                    m_Name = counterName,
                    m_Category = k_FileIOCountersCategoryName,
                });
            }

            return chartCounters;
        }
    }
}
