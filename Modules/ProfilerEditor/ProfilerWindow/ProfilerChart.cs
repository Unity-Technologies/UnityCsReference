// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;
using UnityEngine;
using UnityEditor;
using UnityEngine.Profiling;
using System;

namespace UnityEditorInternal
{
    internal class ProfilerChart : Chart
    {
        const string kPrefCharts = "ProfilerChart";

        private static readonly GUIContent performanceWarning =
            new GUIContent("", EditorGUIUtility.LoadIcon("console.warnicon.sml"), L10n.Tr("Collecting GPU Profiler data might have overhead. Close graph if you don't need its data"));

        private bool m_Active;

        public ProfilerArea m_Area;
        public Chart.ChartType m_Type;
        public float m_DataScale;
        public ChartViewData m_Data;
        public ChartSeriesViewData[] m_Series;
        // For some charts, every line is scaled individually, so every data series gets their own range based on their own max scale.
        // For charts that share their scale (like the Networking charts) all series get adjusted to the total max of the chart.
        // Shared scale is only used for line charts and should only be used when every series shares the same data unit.
        public bool m_SharedScale;

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
            graphRange = new Vector2(Mathf.Epsilon, Mathf.Infinity);
            m_Area = area;
            m_Type = type;
            m_DataScale = dataScale;
            m_Data = new ChartViewData();
            m_Series = new ChartSeriesViewData[seriesCount];
            m_Active = ReadActiveState();
            ApplyActiveState();
        }

        public override void Close()
        {
            base.Close();
            active = false;
        }

        /// <summary>
        /// Callback parameter will be either true if a state change occured
        /// </summary>
        /// <param name="onSeriesToggle"></param>
        public void SetOnSeriesToggleCallback(Action<bool> onSeriesToggle)
        {
            onDoSeriesToggle = onSeriesToggle;
        }

        private string GetLocalizedChartName()
        {
            if (s_LocalizedChartNames == null)
            {
                s_LocalizedChartNames = new string[]
                {
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
            UnityEngine.Debug.Assert(s_LocalizedChartNames.Length == Profiler.areaCount);
            return s_LocalizedChartNames[(int)m_Area];
        }

        protected override void DoLegendGUI(Rect position, ChartType type, ChartViewData cdata, EventType evtType, bool active)
        {
            base.DoLegendGUI(position, type, cdata, evtType, active);

            if (m_Area == ProfilerArea.GPU)
            {
                const float rightMmargin = 2f;
                const float topMargin = 4f;
                const float iconSize = 16f;
                var padding = GUISkin.current.label.padding;
                float width = iconSize + padding.horizontal;

                GUI.Label(new Rect(position.xMax - width - rightMmargin, position.y + topMargin, width, iconSize + padding.vertical), performanceWarning);
            }
        }

        public virtual int DoChartGUI(int currentFrame, bool active)
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
                    labels[s] = ProfilerDriver.GetFormattedCounterValue(currentFrame, m_Area, name);
                }
                m_Data.AssignSelectedLabels(labels);
            }

            if (legendHeaderLabel == null)
            {
                string iconName = string.Format("Profiler.{0}", System.Enum.GetName(typeof(ProfilerArea), m_Area));
                legendHeaderLabel = EditorGUIUtility.TextContentWithIcon(GetLocalizedChartName(), iconName);
            }

            return DoGUI(m_Type, currentFrame, m_Data, active);
        }

        public void LoadAndBindSettings()
        {
            LoadAndBindSettings(kPrefCharts + m_Area, m_Data);
        }

        private void ApplyActiveState()
        {
            // Opening/Closing CPU chart should not set the CPU area as that would set Profiler.enabled.
            if (m_Area != ProfilerArea.CPU)
                ProfilerDriver.SetAreaEnabled(m_Area, active);
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
