// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;
using UnityEngine;
using UnityEditor;

namespace UnityEditorInternal
{
    internal class ProfilerChart : Chart
    {
        const string kPrefCharts = "ProfilerChart";

        private static readonly GUIContent performanceWarning =
            new GUIContent("", EditorGUIUtility.LoadIcon("console.warnicon.sml"), "Collecting GPU Profiler data might have overhead. Close graph if you don't need its data");

        private bool m_Active;

        public ProfilerArea m_Area;
        public Chart.ChartType m_Type;
        public float m_DataScale;
        public ChartViewData m_Data;
        public ChartSeriesViewData[] m_Series;

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

        public ProfilerChart(ProfilerArea area, Chart.ChartType type, float dataScale, int seriesCount) : base()
        {
            labelRange = new Vector2(Mathf.Epsilon, Mathf.Infinity);
            m_Area = area;
            m_Type = type;
            m_DataScale = dataScale;
            m_Data = new ChartViewData();
            m_Series = new ChartSeriesViewData[seriesCount];
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
                    LocalizationDatabase.GetLocalizedString("Global Illumination|Graph of the Precomputed Realtime Global Illumination system resource usage."),
                };
            }
            UnityEngine.Debug.Assert(s_LocalizedChartNames.Length == (int)ProfilerArea.AreaCount);
            return s_LocalizedChartNames[(int)m_Area];
        }

        protected override void DoLegendGUI(Rect position, ChartType type, ChartViewData cdata, EventType evtType, bool active)
        {
            Rect warningIconRect = position;
            warningIconRect.xMin = warningIconRect.xMax - performanceWarning.image.width;
            warningIconRect.yMin = warningIconRect.yMax - performanceWarning.image.height;

            base.DoLegendGUI(position, type, cdata, evtType, active);

            if (m_Area == ProfilerArea.GPU)
                GUI.Label(warningIconRect, performanceWarning);
        }

        public virtual int DoChartGUI(int currentFrame, ProfilerArea currentArea)
        {
            if (Event.current.type == EventType.Repaint)
            {
                string[] labels = new string[m_Series.Length];
                for (int s = 0; s < m_Series.Length; s++)
                {
                    string name =
                        m_Data.hasOverlay ?
                        "Selected" + m_Series[s].name :
                        m_Series[s].name;
                    int identifier = ProfilerDriver.GetStatisticsIdentifier(name);
                    labels[s] = ProfilerDriver.GetFormattedStatisticsValue(currentFrame, identifier);
                }
                m_Data.AssignSelectedLabels(labels);
            }

            if (legendHeaderLabel == null)
            {
                string iconName = string.Format("Profiler.{0}", System.Enum.GetName(typeof(ProfilerArea), m_Area));
                legendHeaderLabel = EditorGUIUtility.TextContentWithIcon(GetLocalizedChartName(), iconName);
            }

            return DoGUI(m_Type, currentFrame, m_Data, currentArea == m_Area);
        }

        public void LoadAndBindSettings()
        {
            LoadAndBindSettings(kPrefCharts + m_Area, m_Data);
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
