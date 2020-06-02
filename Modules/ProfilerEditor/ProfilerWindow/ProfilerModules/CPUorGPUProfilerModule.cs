// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling.LowLevel;
using UnityEngine;
using UnityEditor;
using UnityEditor.Profiling;

namespace UnityEditorInternal.Profiling
{
    interface IProfilerSampleNameProvider
    {
        string GetItemName(HierarchyFrameDataView frameData, int itemId);
    }

    [Serializable]
    internal abstract class CPUorGPUProfilerModule : ProfilerModuleBase, IProfilerSampleNameProvider
    {
        [SerializeField]
        protected ProfilerViewType m_ViewType = ProfilerViewType.Timeline;

        // internal because it is used by performance tests
        [SerializeField]
        internal bool updateViewLive;

        protected bool fetchData
        {
            get { return !(m_ProfilerWindow == null || (m_ProfilerWindow.IsRecording() && (ProfilerDriver.IsConnectionEditor()))) || updateViewLive; }
        }

        const string k_ViewTypeSettingsKey = "ViewType";
        const string k_HierarchyViewSettingsKeyPrefix = "HierarchyView.";
        protected abstract string SettingsKeyPrefix { get; }
        string ViewTypeSettingsKey { get { return SettingsKeyPrefix + k_ViewTypeSettingsKey; } }
        string HierarchyViewSettingsKeyPrefix { get { return SettingsKeyPrefix + k_HierarchyViewSettingsKeyPrefix; } }

        string ProfilerViewFilteringOptionsKey => SettingsKeyPrefix + nameof(m_ProfilerViewFilteringOptions);

        protected abstract ProfilerViewType DefaultViewTypeSetting { get; }


        [Flags]
        public enum ProfilerViewFilteringOptions
        {
            None = 0,
            CollapseEditorBoundarySamples = 1 << 0, // Session based override, default to off
            ShowFullScriptingMethodNames = 1 << 1,
            ShowExecutionFlow = 1 << 2,
        }

        static readonly GUIContent[] k_ProfilerViewFilteringOptions =
        {
            EditorGUIUtility.TrTextContent("Collapse EditorOnly Samples", "Samples that are only created due to profiling the editor are collapsed by default, renamed to EditorOnly [<FunctionName>] and any GC Alloc incurred by them will not be accumulated."),
            EditorGUIUtility.TrTextContent("Show Full Scripting Method Names", "Display fully qualified method names including assembly name and namespace."),
            EditorGUIUtility.TrTextContent("Show Flow Events", "Visualize job scheduling and execution."),
        };

        [SerializeField]
        int m_ProfilerViewFilteringOptions = (int)ProfilerViewFilteringOptions.CollapseEditorBoundarySamples;

        [SerializeField]
        protected ProfilerFrameDataHierarchyView m_FrameDataHierarchyView;

        // Used by Tests/PerformanceTests/Profiler ProfilerWindowTests.CPUViewTests.SelectAndDisplayDetailsForAFrame_WithSearchFiltering to avoid brittle tests due to reflection
        internal ProfilerFrameDataHierarchyView FrameDataHierarchyView => m_FrameDataHierarchyView;

        internal ProfilerViewFilteringOptions ViewOptions => (ProfilerViewFilteringOptions)m_ProfilerViewFilteringOptions;

        // Used by Tests/PerformanceTests/Profiler ProfilerWindowTests.CPUViewTests
        internal ProfilerViewType ViewType
        {
            get { return m_ViewType; }
            set { m_ViewType = value; }
        }

        public override void OnEnable(IProfilerWindowController profilerWindow)
        {
            base.OnEnable(profilerWindow);
            if (m_FrameDataHierarchyView == null)
                m_FrameDataHierarchyView = new ProfilerFrameDataHierarchyView(HierarchyViewSettingsKeyPrefix);
            m_FrameDataHierarchyView.OnEnable(this, profilerWindow, false);
            m_FrameDataHierarchyView.viewTypeChanged += CPUOrGPUViewTypeChanged;
            m_FrameDataHierarchyView.selectionChanged += CPUOrGPUViewSelectionChanged;
            m_ProfilerWindow.selectionChanged += m_FrameDataHierarchyView.SetSelectionFromLegacyPropertyPath;
            m_ViewType = (ProfilerViewType)EditorPrefs.GetInt(ViewTypeSettingsKey, (int)DefaultViewTypeSetting);
            m_ProfilerViewFilteringOptions = SessionState.GetInt(ProfilerViewFilteringOptionsKey, m_ProfilerViewFilteringOptions);
        }

        public override void SaveViewSettings()
        {
            base.SaveViewSettings();
            EditorPrefs.SetInt(ViewTypeSettingsKey, (int)m_ViewType);
            SessionState.SetInt(ProfilerViewFilteringOptionsKey, m_ProfilerViewFilteringOptions);
            m_FrameDataHierarchyView?.SaveViewSettings();
        }

        public override void OnDisable()
        {
            SaveViewSettings();
            base.OnDisable();
            m_FrameDataHierarchyView?.OnDisable();

            Clear();
        }

        public override void DrawToolbar(Rect position)
        {
            // Hierarchy view still needs to be broken apart into Toolbar and View.
        }

        public void DrawOptionsMenuPopup()
        {
            var position = GUILayoutUtility.GetRect(ProfilerWindow.Styles.optionsButtonContent, EditorStyles.toolbarButton);
            if (GUI.Button(position, ProfilerWindow.Styles.optionsButtonContent, EditorStyles.toolbarButton))
            {
                var pm = new GenericMenu();
                for (var i = 0; i < k_ProfilerViewFilteringOptions.Length; i++)
                {
                    var option = (ProfilerViewFilteringOptions)(1 << i);
                    if (ViewType == ProfilerViewType.Timeline && option == ProfilerViewFilteringOptions.CollapseEditorBoundarySamples)
                        continue;

                    if (option == ProfilerViewFilteringOptions.ShowExecutionFlow && ViewType != ProfilerViewType.Timeline)
                        continue;

                    pm.AddItem(k_ProfilerViewFilteringOptions[i], OptionEnabled(option), () => ToggleOption(option));
                }
                pm.Popup(position, -1);
            }
        }

        bool OptionEnabled(ProfilerViewFilteringOptions option)
        {
            return (option & (ProfilerViewFilteringOptions)m_ProfilerViewFilteringOptions) != ProfilerViewFilteringOptions.None;
        }

        protected virtual void ToggleOption(ProfilerViewFilteringOptions option)
        {
            m_ProfilerViewFilteringOptions = (int)((ProfilerViewFilteringOptions)m_ProfilerViewFilteringOptions ^ option);
            SessionState.SetInt(ProfilerViewFilteringOptionsKey, m_ProfilerViewFilteringOptions);
            m_FrameDataHierarchyView.Clear();
        }

        public override void DrawView(Rect position)
        {
            m_FrameDataHierarchyView.DoGUI(fetchData ? GetFrameDataView() : null, fetchData, ref updateViewLive);
        }

        HierarchyFrameDataView GetFrameDataView()
        {
            var viewMode = HierarchyFrameDataView.ViewModes.Default;
            if (m_ViewType == ProfilerViewType.Hierarchy)
                viewMode |= HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName;
            return m_ProfilerWindow.GetFrameDataView(m_FrameDataHierarchyView.threadName, viewMode | GetFilteringMode(), m_FrameDataHierarchyView.sortedProfilerColumn, m_FrameDataHierarchyView.sortedProfilerColumnAscending);
        }

        protected virtual HierarchyFrameDataView.ViewModes GetFilteringMode()
        {
            return HierarchyFrameDataView.ViewModes.Default;
        }

        void CPUOrGPUViewSelectionChanged(int id)
        {
            var frameDataView = GetFrameDataView();
            if (frameDataView == null || !frameDataView.valid)
                return;

            m_ProfilerWindow.SetSelectedPropertyPath(frameDataView.GetItemPath(id));
        }

        protected void CPUOrGPUViewTypeChanged(ProfilerViewType viewtype)
        {
            if (m_ViewType == viewtype)
                return;

            m_ViewType = viewtype;
        }

        public override void Clear()
        {
            base.Clear();
            m_FrameDataHierarchyView?.Clear();
        }

        public void Repaint()
        {
            m_ProfilerWindow.Repaint();
        }

        const int k_AnyFullManagedMarker = (int)(MarkerFlags.ScriptInvoke | MarkerFlags.ScriptDeepProfiler);
        public string GetItemName(HierarchyFrameDataView frameData, int itemId)
        {
            var name = frameData.GetItemName(itemId);
            if ((ViewOptions & ProfilerViewFilteringOptions.ShowFullScriptingMethodNames) != 0)
                return name;

            var flags = frameData.GetItemMarkerFlags(itemId);
            if (((int)flags & k_AnyFullManagedMarker) == 0)
                return name;

            var namespaceDelimiterIndex = name.IndexOf(':');
            if (namespaceDelimiterIndex == -1)
                return name;
            ++namespaceDelimiterIndex;
            if (namespaceDelimiterIndex < name.Length && name[namespaceDelimiterIndex] == ':')
                return name.Substring(namespaceDelimiterIndex + 1);

            return name;
        }
    }
}
