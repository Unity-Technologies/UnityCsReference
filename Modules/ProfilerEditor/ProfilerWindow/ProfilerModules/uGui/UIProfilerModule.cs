// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal class UIProfilerModule : ProfilerModuleBase
    {
        const string k_IconName = "Profiler.UI";
        const int k_DefaultOrderIndex = 10;
        static readonly string k_Name = LocalizationDatabase.GetLocalizedString("UI");

        protected static WeakReference instance;
        [SerializeField]
        UISystemProfiler m_UISystemProfiler;

        public UIProfilerModule(IProfilerWindowController profilerWindow) : base(profilerWindow, k_Name, k_IconName, Chart.ChartType.StackedFill) {}
        protected UIProfilerModule(IProfilerWindowController profilerWindow, string name, string iconName, Chart.ChartType chartType) : base(profilerWindow, name, iconName, chartType) {}  // Used by UIDetailsProfilerModule

        static UISystemProfiler sharedUISystemProfiler
        {
            get
            {
                return instance.IsAlive ? (instance.Target as UIProfilerModule)?.m_UISystemProfiler : null;
            }
        }

        public override ProfilerArea area => ProfilerArea.UI;

        protected override int defaultOrderIndex => k_DefaultOrderIndex;
        protected override string legacyPreferenceKey => "ProfilerChartUI";

        public override void OnEnable()
        {
            if (this.GetType() == typeof(UIProfilerModule))
            {
                instance = new WeakReference(this);
            }

            base.OnEnable();

            if (m_UISystemProfiler == null)
                m_UISystemProfiler = new UISystemProfiler();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            sharedUISystemProfiler?.CurrentAreaChanged(null);
        }

        public override void DrawToolbar(Rect position)
        {
            // This module still needs to be broken apart into Toolbar and View.
        }

        public override void DrawDetailsView(Rect position)
        {
            sharedUISystemProfiler?.DrawUIPane(m_ProfilerWindow);
        }
    }
}
