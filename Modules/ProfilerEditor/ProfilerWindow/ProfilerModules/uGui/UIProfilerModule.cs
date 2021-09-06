// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    [ProfilerModuleMetadata("UI", typeof(LocalizationResource), IconPath = "Profiler.UI")]
    internal class UIProfilerModule : ProfilerModuleBase
    {
        const int k_DefaultOrderIndex = 10;
        static readonly string k_UIProfilerAvailableOnlyInEditorMode = LocalizationDatabase.GetLocalizedString("Data is only available when profiling Play Mode in the Editor.");

        protected static WeakReference instance;
        [SerializeField]
        UISystemProfiler m_UISystemProfiler;

        public UIProfilerModule() : base(ProfilerModuleChartType.StackedTimeArea) {}

        // Used by UIDetailsProfilerModule
        protected UIProfilerModule(ProfilerModuleChartType defaultChartType) : base(defaultChartType) {}

        static UISystemProfiler sharedUISystemProfiler
        {
            get
            {
                return instance.IsAlive ? (instance.Target as UIProfilerModule)?.m_UISystemProfiler : null;
            }
        }

        internal override ProfilerArea area => ProfilerArea.UI;
        public override bool usesCounters => false;

        private protected override int defaultOrderIndex => k_DefaultOrderIndex;
        private protected override string legacyPreferenceKey => "ProfilerChartUI";

        internal override void OnEnable()
        {
            if (this.GetType() == typeof(UIProfilerModule))
            {
                instance = new WeakReference(this);
            }

            base.OnEnable();

            if (m_UISystemProfiler == null)
                m_UISystemProfiler = new UISystemProfiler();
        }

        internal override void OnDisable()
        {
            base.OnDisable();
            if (m_UISystemProfiler != null)
            {
                m_UISystemProfiler.CurrentAreaChanged(null);
                m_UISystemProfiler.Dispose();
            }
        }

        public override void DrawToolbar(Rect position)
        {
            // This module still needs to be broken apart into Toolbar and View.
            // case-1251139: We draw an empty toolbar when the profiler is not connected to an editor.
            //               This is primary to match the other profiler's UI message display.
            if (!ProfilerWindow.ConnectedToEditor)
                DrawEmptyToolbar();
        }

        public override void DrawDetailsView(Rect position)
        {
            if (ProfilerWindow.ConnectedToEditor)
                sharedUISystemProfiler?.DrawUIPane(ProfilerWindow);
            else
                GUILayout.Label(k_UIProfilerAvailableOnlyInEditorMode);
        }
    }
}
