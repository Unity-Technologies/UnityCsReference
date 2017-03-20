// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;
using UnityEngine;
using UnityEditor;

namespace UnityEditorInternal
{
    internal class ProfilerChart
    {
        const string kPrefCharts = "ProfilerChart";

        private bool m_Active;

        public ProfilerArea m_Area;
        public Chart.ChartType m_Type;
        public float m_DataScale;
        public Chart m_Chart;
        public ChartData m_Data;
        public ChartSeries[] m_Series;
        public GUIContent m_Icon;

        private static string[] s_LocalizedChartNames = null;

        public bool active
        {
            get
            {
                return m_Active;
            }
            set
            {
                if (m_Active != value)
                {
                    m_Active = value;
                    ApplyActiveState();
                    SaveActiveState();
                }
            }
        }

        public ProfilerChart(ProfilerArea area, Chart.ChartType type, float dataScale, int seriesCount)
        {
            m_Area = area;
            m_Type = type;
            m_DataScale = dataScale;
            m_Chart = new Chart();
            m_Data = new ChartData();
            m_Series = new ChartSeries[seriesCount];
            m_Active = ReadActiveState();
            ApplyActiveState();
        }

        private string GetLocalizedChartName()
        {
            if (s_LocalizedChartNames == null)
            {
                s_LocalizedChartNames = new string[] {
                    LocalizationDatabase.GetLocalizedString("CPU Usage|Graph out the various CPU areas"),
                    LocalizationDatabase.GetLocalizedString("GPU Usage|Graph out the various GPU areas"),
                    LocalizationDatabase.GetLocalizedString("Rendering"),
                    LocalizationDatabase.GetLocalizedString("Memory|Graph out the various memory usage areas"),
                    LocalizationDatabase.GetLocalizedString("Audio"),
                    LocalizationDatabase.GetLocalizedString("Video"),
                    LocalizationDatabase.GetLocalizedString("Physics"),
                    LocalizationDatabase.GetLocalizedString("Physics (2D)"),
                    LocalizationDatabase.GetLocalizedString("Network Messages"),
                    LocalizationDatabase.GetLocalizedString("Network Operations"),
                    LocalizationDatabase.GetLocalizedString("UI"),
                    LocalizationDatabase.GetLocalizedString("UI Details"),
                };
            }
            UnityEngine.Debug.Assert(s_LocalizedChartNames.Length == (int)ProfilerArea.AreaCount);
            return s_LocalizedChartNames[(int)m_Area];
        }

        public virtual int DoChartGUI(int currentFrame, ProfilerArea currentArea, out Chart.ChartAction action)
        {
            if (Event.current.type == EventType.Repaint)
            {
                string[] labels = new string[m_Series.Length];
                for (int s = 0; s < m_Series.Length; s++)
                {
                    string name =
                        m_Data.hasOverlay ?
                        "Selected" + m_Series[s].identifierName :
                        m_Series[s].identifierName;
                    int identifier = ProfilerDriver.GetStatisticsIdentifier(name);
                    labels[s] = ProfilerDriver.GetFormattedStatisticsValue(currentFrame, identifier);
                }
                m_Data.selectedLabels = labels;
            }

            if (m_Icon == null)
            {
                string iconName = "Profiler." + System.Enum.GetName(typeof(ProfilerArea), m_Area);
                m_Icon = EditorGUIUtility.TextContentWithIcon(GetLocalizedChartName(), iconName);
            }

            return m_Chart.DoGUI(m_Type, currentFrame, m_Data, m_Area, currentArea == m_Area, m_Icon, out action);
        }

        public void LoadAndBindSettings()
        {
            m_Chart.LoadAndBindSettings(kPrefCharts + m_Area, m_Data);
        }

        private void ApplyActiveState()
        {
            // Currently only GPU area supports disabling
            if (m_Area == ProfilerArea.GPU)
                ProfilerDriver.profileGPU = active;
        }

        private bool ReadActiveState()
        {
            if (m_Area == ProfilerArea.GPU)
                return SessionState.GetBool(kPrefCharts + m_Area, false);
            else
                return EditorPrefs.GetBool(kPrefCharts + m_Area, true);
        }

        private void SaveActiveState()
        {
            if (m_Area == ProfilerArea.GPU)
                SessionState.SetBool(kPrefCharts + m_Area, m_Active);
            else
                EditorPrefs.SetBool(kPrefCharts + m_Area, m_Active);
        }
    }
}
