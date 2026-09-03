// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    [ProfilerModuleMetadata("Audio", typeof(LocalizationResource), IconPath = "Profiler.Audio")]
    internal class AudioProfilerModule : ProfilerModuleBase
    {
        const int k_DefaultOrderIndex = 4;

        /// <summary>
        /// ID for SAP profiler frame metadata. Must match kSAPProfilerGuidBytes in SAPProfiler.h.
        /// Spells "SAPProfiler00000" in ASCII.
        /// </summary>
        static readonly byte[] k_SAPProfilerGuidBytes = {
            (byte)'S', (byte)'A', (byte)'P', (byte)'P', (byte)'r', (byte)'o', (byte)'f', (byte)'i',
            (byte)'l', (byte)'e', (byte)'r', (byte)'0', (byte)'0', (byte)'0', (byte)'0', (byte)'0'
        };

        /// <summary>
        /// Tags for different SAP profiler data types. Must match MetadataTag enum in SAPProfiler.h.
        /// </summary>
        enum SAPMetadataTag
        {
            ProcessorInfo = 0,
            SummaryInfo = 1,
            Names = 2
        }

        Vector2 m_PaneScroll_AudioChannels = Vector2.zero;
        Vector2 m_PaneScroll_AudioDSPLeft = Vector2.zero;
        Vector2 m_PaneScroll_AudioDSPRight_ScrollPos = Vector2.zero;
        Vector2 m_PaneScroll_AudioDSPRight_Size = new Vector2(10000, 20000);
        Vector2 m_PaneScroll_AudioClips = Vector2.zero;
        Vector2 m_PaneScroll_Processors = Vector2.zero;

        [SerializeField]
        bool m_ShowInactiveDSPChains = false;

        [SerializeField]
        bool m_HighlightAudibleDSPChains = true;

        [SerializeField]
        float m_DSPGraphZoomFactor = 1.0f;

        [SerializeField]
        bool m_DSPGraphHorizontalLayout = false;

        // NOTE: This was originally tagged [SerializeField], but the type was not serializable. UUM-132549
        private AudioProfilerGroupTreeViewState m_AudioProfilerGroupTreeViewState;
        private AudioProfilerGroupView m_AudioProfilerGroupView = null;
        private AudioProfilerGroupViewBackend m_AudioProfilerGroupViewBackend;

        // NOTE: This was originally tagged [SerializeField], but the type was not serializable. UUM-132549
        private AudioProfilerClipTreeViewState m_AudioProfilerClipTreeViewState;
        private AudioProfilerClipView m_AudioProfilerClipView = null;
        private AudioProfilerClipViewBackend m_AudioProfilerClipViewBackend;

        private AudioProfilerDSPView m_AudioProfilerDSPView;

        // SAP Processors view
        private SAPProfilerProcessorTreeViewState m_SAPProfilerProcessorTreeViewState;
        private SAPProfilerProcessorView m_SAPProfilerProcessorView = null;
        private SAPProfilerProcessorViewBackend m_SAPProfilerProcessorViewBackend;

        // Cached collections for SAP profiler to avoid GC allocations
        private readonly List<SAPProfilerSummary> m_SAPSummaries = new List<SAPProfilerSummary>();
        private readonly Dictionary<int, ulong> m_SAPDtmBufferTimes = new Dictionary<int, ulong>();
        private readonly List<SAPProfilerProcessorInfoWrapper> m_SAPItems = new List<SAPProfilerProcessorInfoWrapper>();
        // Cache resolved source asset names so they persist after assets are unloaded (e.g., exiting play mode)
        private readonly Dictionary<ulong, string> m_SourceAssetNameCache = new Dictionary<ulong, string>();
        enum ProfilerAudioPopupItems
        {
            Simple = 0,
            Detailed = 1
        }
        ProfilerAudioView m_ShowDetailedAudioPane;

        int m_LastAudioProfilerFrame = -1;

        const string k_ViewTypeSettingsKey = "Profiler.AudioProfilerModule.ViewType";
        const string k_ShowInactiveDSPChainsSettingsKey = "Profiler.MemoryProfilerModule.ShowInactiveDSPChains";
        const string k_HighlightAudibleDSPChainsSettingsKey = "Profiler.MemoryProfilerModule.HighlightAudibleDSPChains";
        const string k_DSPGraphZoomFactorSettingsKey = "Profiler.MemoryProfilerModule.DSPGraphZoomFactor";
        const string k_DSPGraphHorizontalLayoutSettingsKey = "Profiler.MemoryProfilerModule.DSPGraphHorizontalLayout";
        const string k_AudioProfilerGroupTreeViewStateSettingsKey = "Profiler.MemoryProfilerModule.AudioProfilerGroupTreeViewState";
        const string k_AudioProfilerClipTreeViewStateSettingsKey = "Profiler.MemoryProfilerModule.AudioProfilerClipTreeViewState";

        internal override ProfilerArea area => ProfilerArea.Audio;
        public override bool usesCounters => false;

        private protected override int defaultOrderIndex => k_DefaultOrderIndex;
        private protected override string legacyPreferenceKey => "ProfilerChartAudio";

        internal override void OnEnable()
        {
            base.OnEnable();

            m_ShowDetailedAudioPane = (ProfilerAudioView)EditorPrefs.GetInt(k_ViewTypeSettingsKey, (int)ProfilerAudioView.Channels);
            m_ShowInactiveDSPChains = EditorPrefs.GetBool(k_ShowInactiveDSPChainsSettingsKey, m_ShowInactiveDSPChains);
            m_HighlightAudibleDSPChains = EditorPrefs.GetBool(k_HighlightAudibleDSPChainsSettingsKey, m_HighlightAudibleDSPChains);
            m_DSPGraphZoomFactor = SessionState.GetFloat(k_DSPGraphZoomFactorSettingsKey, m_DSPGraphZoomFactor);
            m_DSPGraphHorizontalLayout = EditorPrefs.GetBool(k_DSPGraphHorizontalLayoutSettingsKey, m_DSPGraphHorizontalLayout);
            var restoredAudioProfilerGroupTreeViewState = SessionState.GetString(k_AudioProfilerGroupTreeViewStateSettingsKey, string.Empty);
            if (!string.IsNullOrEmpty(restoredAudioProfilerGroupTreeViewState))
            {
                try
                {
                    m_AudioProfilerGroupTreeViewState = JsonUtility.FromJson<AudioProfilerGroupTreeViewState>(restoredAudioProfilerGroupTreeViewState);
                }
                catch{} // Never mind, we'll fall back to the default
            }
            var restoredAudioProfilerClipTreeViewState = SessionState.GetString(k_AudioProfilerClipTreeViewStateSettingsKey, string.Empty);
            if (!string.IsNullOrEmpty(restoredAudioProfilerClipTreeViewState))
            {
                try
                {
                    m_AudioProfilerClipTreeViewState = JsonUtility.FromJson<AudioProfilerClipTreeViewState>(restoredAudioProfilerClipTreeViewState);
                }
                catch{} // Never mind, we'll fall back to the default
            }
        }

        internal override void OnDisable()
        {
            base.OnDisable();

            // Clear cached source asset names to avoid holding stale references
            // across domain reloads or when profiling different sessions
            m_SourceAssetNameCache.Clear();
        }

        internal override void SaveViewSettings()
        {
            base.SaveViewSettings();
            EditorPrefs.SetInt(k_ViewTypeSettingsKey, (int)m_ShowDetailedAudioPane);
            EditorPrefs.SetBool(k_ShowInactiveDSPChainsSettingsKey, m_ShowInactiveDSPChains);
            EditorPrefs.SetBool(k_HighlightAudibleDSPChainsSettingsKey, m_HighlightAudibleDSPChains);
            SessionState.SetFloat(k_DSPGraphZoomFactorSettingsKey, m_DSPGraphZoomFactor);
            EditorPrefs.SetBool(k_DSPGraphHorizontalLayoutSettingsKey, m_DSPGraphHorizontalLayout);
            if (m_AudioProfilerGroupTreeViewState != null)
                SessionState.SetString(k_AudioProfilerGroupTreeViewStateSettingsKey, EditorJsonUtility.ToJson(m_AudioProfilerGroupTreeViewState));
            if (m_AudioProfilerGroupTreeViewState != null)
                SessionState.SetString(k_AudioProfilerClipTreeViewStateSettingsKey, EditorJsonUtility.ToJson(m_AudioProfilerClipTreeViewState));
        }

        public override void DrawToolbar(Rect position)
        {
            // This module still needs to be broken apart into Toolbar and View.
        }

        public override void DrawDetailsView(Rect position)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            ProfilerAudioView newShowDetailedAudioPane = m_ShowDetailedAudioPane;
            if (AudioDeepProfileToggle())
            {
                if (GUILayout.Toggle(newShowDetailedAudioPane == ProfilerAudioView.Channels, "Channels", EditorStyles.toolbarButton)) newShowDetailedAudioPane = ProfilerAudioView.Channels;
                if (GUILayout.Toggle(newShowDetailedAudioPane == ProfilerAudioView.Groups, "Groups", EditorStyles.toolbarButton)) newShowDetailedAudioPane = ProfilerAudioView.Groups;
                if (GUILayout.Toggle(newShowDetailedAudioPane == ProfilerAudioView.ChannelsAndGroups, "Channels and groups", EditorStyles.toolbarButton)) newShowDetailedAudioPane = ProfilerAudioView.ChannelsAndGroups;
                if (Unsupported.IsDeveloperMode() && GUILayout.Toggle(newShowDetailedAudioPane == ProfilerAudioView.DSPGraph, "DSP Graph", EditorStyles.toolbarButton)) newShowDetailedAudioPane = ProfilerAudioView.DSPGraph;
                if (Unsupported.IsDeveloperMode() && GUILayout.Toggle(newShowDetailedAudioPane == ProfilerAudioView.Clips, "Clips", EditorStyles.toolbarButton)) newShowDetailedAudioPane = ProfilerAudioView.Clips;
                if (GUILayout.Toggle(newShowDetailedAudioPane == ProfilerAudioView.Processors, "Processors", EditorStyles.toolbarButton)) newShowDetailedAudioPane = ProfilerAudioView.Processors;
                if (newShowDetailedAudioPane != m_ShowDetailedAudioPane)
                {
                    m_ShowDetailedAudioPane = newShowDetailedAudioPane;
                    m_LastAudioProfilerFrame = -1; // force update
                }
                if (m_ShowDetailedAudioPane == ProfilerAudioView.DSPGraph)
                {
                    m_ShowInactiveDSPChains = GUILayout.Toggle(m_ShowInactiveDSPChains, "Show inactive", EditorStyles.toolbarButton);
                    if (m_ShowInactiveDSPChains)
                        m_HighlightAudibleDSPChains = GUILayout.Toggle(m_HighlightAudibleDSPChains, "Highlight audible", EditorStyles.toolbarButton);
                    m_DSPGraphHorizontalLayout = GUILayout.Toggle(m_DSPGraphHorizontalLayout, "Horizontal Layout", EditorStyles.toolbarButton);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    var graphRect = DrawAudioStatsPane(ref m_PaneScroll_AudioDSPLeft);

                    m_PaneScroll_AudioDSPRight_ScrollPos = GUI.BeginScrollView(graphRect, m_PaneScroll_AudioDSPRight_ScrollPos, new Rect(0, 0, m_PaneScroll_AudioDSPRight_Size.x, m_PaneScroll_AudioDSPRight_Size.y));

                    var clippingRect = new Rect(m_PaneScroll_AudioDSPRight_ScrollPos.x, m_PaneScroll_AudioDSPRight_ScrollPos.y, graphRect.width, graphRect.height);

                    if (m_AudioProfilerDSPView == null)
                        m_AudioProfilerDSPView = new AudioProfilerDSPView();

#pragma warning disable CS0618
                    ProfilerProperty property = ProfilerWindow.CreateProperty();
#pragma warning restore CS0618
                    if (property != null &&
                        property.frameDataReady)
                    {
                        using (property)
                        {
                            m_AudioProfilerDSPView.OnGUI(clippingRect, property, m_ShowInactiveDSPChains, m_HighlightAudibleDSPChains, m_DSPGraphHorizontalLayout, ref m_DSPGraphZoomFactor, ref m_PaneScroll_AudioDSPRight_ScrollPos, ref m_PaneScroll_AudioDSPRight_Size);
                        }
                    }

                    GUI.EndScrollView();
                }
                else if (m_ShowDetailedAudioPane == ProfilerAudioView.Clips)
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    var treeRect = DrawAudioStatsPane(ref m_PaneScroll_AudioClips);

                    // TREE
                    if (m_AudioProfilerClipTreeViewState == null)
                        m_AudioProfilerClipTreeViewState = new AudioProfilerClipTreeViewState();

                    if (m_AudioProfilerClipViewBackend == null)
                        m_AudioProfilerClipViewBackend = new AudioProfilerClipViewBackend(m_AudioProfilerClipTreeViewState);

#pragma warning disable CS0618
                    ProfilerProperty property = ProfilerWindow.CreateProperty();
#pragma warning restore CS0618
                    if (property == null)
                        return;

                    using (property)
                    {
                        if (!property.frameDataReady)
                            return;

                        var currentFrame = ProfilerWindow.GetActiveVisibleFrameIndex();
                        if (currentFrame == -1 || m_LastAudioProfilerFrame != currentFrame)
                        {
                            m_LastAudioProfilerFrame = currentFrame;
                            var sourceItems = property.GetAudioProfilerClipInfo();
                            if (sourceItems != null && sourceItems.Length > 0)
                            {
                                var items = new List<AudioProfilerClipInfoWrapper>();
                                foreach (var s in sourceItems)
                                {
                                    items.Add(new AudioProfilerClipInfoWrapper(s, property.GetAudioProfilerNameByOffset(s.assetNameOffset)));
                                }
                                m_AudioProfilerClipViewBackend.SetData(items);
                                if (m_AudioProfilerClipView == null)
                                {
                                    m_AudioProfilerClipView = new AudioProfilerClipView(ProfilerWindow as EditorWindow, m_AudioProfilerClipTreeViewState);
                                    m_AudioProfilerClipView.Init(treeRect, m_AudioProfilerClipViewBackend);
                                }
                            }
                        }
                        if (m_AudioProfilerClipView != null)
                            m_AudioProfilerClipView.OnGUI(treeRect);
                    }
                }
                else if (m_ShowDetailedAudioPane == ProfilerAudioView.Processors)
                {
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    var treeRect = DrawAudioStatsPane(ref m_PaneScroll_Processors);

                    // TREE
                    if (m_SAPProfilerProcessorTreeViewState == null)
                        m_SAPProfilerProcessorTreeViewState = new SAPProfilerProcessorTreeViewState();

                    if (m_SAPProfilerProcessorViewBackend == null)
                        m_SAPProfilerProcessorViewBackend = new SAPProfilerProcessorViewBackend(m_SAPProfilerProcessorTreeViewState);

                    var currentFrame = ProfilerWindow.GetActiveVisibleFrameIndex();
                    if (currentFrame == -1)
                        return;

                    if (m_LastAudioProfilerFrame != currentFrame)
                    {
                        m_LastAudioProfilerFrame = currentFrame;

                        // Get frame data using the modern FrameMetaData API
                        using (var frameData = ProfilerDriver.GetRawFrameDataView(currentFrame, 0))
                        {
                            if (!frameData.valid)
                                return;

                            // Fetch SAP profiler data from frame metadata
                            using (var sourceItems = frameData.GetFrameMetaData<SAPProfilerProcessorInfo>(k_SAPProfilerGuidBytes, (int)SAPMetadataTag.ProcessorInfo))
                            using (var sourceSummaries = frameData.GetFrameMetaData<SAPProfilerSummary>(k_SAPProfilerGuidBytes, (int)SAPMetadataTag.SummaryInfo))
                            using (var namesBuffer = frameData.GetFrameMetaData<byte>(k_SAPProfilerGuidBytes, (int)SAPMetadataTag.Names))
                            {
                                // Build summaries list and DTM lookup (reuse cached collections)
                                m_SAPSummaries.Clear();
                                m_SAPDtmBufferTimes.Clear();
                                if (sourceSummaries.Length > 0)
                                {
                                    for (int i = 0; i < sourceSummaries.Length; i++)
                                    {
                                        var summary = sourceSummaries[i];
                                        m_SAPSummaries.Add(summary);
                                        m_SAPDtmBufferTimes[summary.dtmIdentifier] = summary.DspBufferTimeNs;
                                    }
                                }

                                // Build processor items (reuse cached collection)
                                m_SAPItems.Clear();
                                if (sourceItems.Length > 0)
                                {
                                    for (int i = 0; i < sourceItems.Length; i++)
                                    {
                                        var s = sourceItems[i];
                                        var typeName = GetNameFromBuffer(namesBuffer, s.typeNameOffset);
                                        if (string.IsNullOrEmpty(typeName))
                                            typeName = "<Unknown>";
                                        var category = GetNameFromBuffer(namesBuffer, s.categoryNameOffset);
                                        // Resolve source asset entity ID to name (with caching to avoid repeated lookups)
                                        string sourceName = null;
                                        if (s.sourceAssetEntityId != 0)
                                        {
                                            if (!m_SourceAssetNameCache.TryGetValue(s.sourceAssetEntityId, out sourceName))
                                            {
                                                var entityId = EntityId.FromULong(s.sourceAssetEntityId);
                                                var sourceObj = EditorUtility.EntityIdToObject(entityId);
                                                sourceName = sourceObj != null ? sourceObj.name : null;
                                                // Cache even null results to avoid repeated failed lookups
                                                m_SourceAssetNameCache[s.sourceAssetEntityId] = sourceName;
                                            }
                                        }
                                        // Look up correct DSP buffer time for this processor's DTM
                                        ulong dspBufferTimeNs = m_SAPDtmBufferTimes.TryGetValue(s.dtmIdentifier, out var time) ? time : 0;
                                        m_SAPItems.Add(new SAPProfilerProcessorInfoWrapper(s, typeName, category, sourceName, dspBufferTimeNs, i));
                                    }
                                }
                            }
                        }

                        // Create view if needed (show even with no processors, as long as we have DTM summaries)
                        if (m_SAPProfilerProcessorView == null && m_SAPSummaries.Count > 0)
                        {
                            m_SAPProfilerProcessorView = new SAPProfilerProcessorView(ProfilerWindow as EditorWindow, m_SAPProfilerProcessorTreeViewState);
                            m_SAPProfilerProcessorView.Init(treeRect, m_SAPProfilerProcessorViewBackend);
                        }
                        m_SAPProfilerProcessorViewBackend.SetData(m_SAPItems, m_SAPSummaries);
                    }
                    // Show view if we have DTM data (even with no processors)
                    if (m_SAPProfilerProcessorView != null && m_SAPProfilerProcessorViewBackend.dtmDataList.Count > 0)
                        m_SAPProfilerProcessorView.OnGUI(treeRect);
                }
                else
                {
                    bool resetAllAudioClipPlayCountsOnPlay = GUILayout.Toggle(AudioUtil.resetAllAudioClipPlayCountsOnPlay, "Reset play count on play", EditorStyles.toolbarButton);
                    if (resetAllAudioClipPlayCountsOnPlay != AudioUtil.resetAllAudioClipPlayCountsOnPlay)
                        AudioUtil.resetAllAudioClipPlayCountsOnPlay = resetAllAudioClipPlayCountsOnPlay;
                    if (Unsupported.IsDeveloperMode())
                    {
                        GUILayout.Space(5);
                        bool showAllGroups = EditorPrefs.GetBool("AudioProfilerShowAllGroups");
                        bool newShowAllGroups = GUILayout.Toggle(showAllGroups, "Show all groups (dev mode only)", EditorStyles.toolbarButton);
                        if (showAllGroups != newShowAllGroups)
                            EditorPrefs.SetBool("AudioProfilerShowAllGroups", newShowAllGroups);
                    }
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    var treeRect = DrawAudioStatsPane(ref m_PaneScroll_AudioChannels);

                    // TREE
                    if (m_AudioProfilerGroupTreeViewState == null)
                        m_AudioProfilerGroupTreeViewState = new AudioProfilerGroupTreeViewState();

                    if (m_AudioProfilerGroupViewBackend == null)
                        m_AudioProfilerGroupViewBackend = new AudioProfilerGroupViewBackend(m_AudioProfilerGroupTreeViewState);

#pragma warning disable CS0618
                    ProfilerProperty property = ProfilerWindow.CreateProperty();
#pragma warning restore CS0618
                    if (property == null)
                        return;

                    using (property)
                    {
                        if (!property.frameDataReady)
                            return;

                        var currentFrame = ProfilerWindow.GetActiveVisibleFrameIndex();
                        if (currentFrame == -1 || m_LastAudioProfilerFrame != currentFrame)
                        {
                            m_LastAudioProfilerFrame = currentFrame;
                            var sourceItems = property.GetAudioProfilerGroupInfo();
                            if (sourceItems != null && sourceItems.Length > 0)
                            {
                                var items = new List<AudioProfilerGroupInfoWrapper>();
#pragma warning disable CS0618
                                var parentMapping = new Dictionary<int, AudioProfilerGroupInfo>();
#pragma warning restore CS0618
                                foreach (var s in sourceItems)
                                    parentMapping.Add(s.uniqueId, s);
                                foreach (var s in sourceItems)
                                {
                                    bool isGroup = (s.flags & AudioProfilerGroupInfoHelper.AUDIOPROFILER_FLAGS_GROUP) != 0;
                                    if (m_ShowDetailedAudioPane == ProfilerAudioView.Channels && isGroup)
                                        continue;
                                    if (m_ShowDetailedAudioPane == ProfilerAudioView.Groups && !isGroup)
                                        continue;
                                    var wrapper = new AudioProfilerGroupInfoWrapper(s, property.GetAudioProfilerNameByOffset(s.assetNameOffset), property.GetAudioProfilerNameByOffset(s.objectNameOffset), m_ShowDetailedAudioPane == ProfilerAudioView.Channels);
                                    if (parentMapping.TryGetValue(s.parentId, out var parent))
                                        wrapper.parentName = property.GetAudioProfilerNameByOffset(parent.objectNameOffset);
                                    else
                                        wrapper.parentName = "ROOT";
                                    items.Add(wrapper);
                                }
                                m_AudioProfilerGroupViewBackend.SetData(items);
                                if (m_AudioProfilerGroupView == null)
                                {
                                    m_AudioProfilerGroupView = new AudioProfilerGroupView(ProfilerWindow as EditorWindow, m_AudioProfilerGroupTreeViewState);
                                    m_AudioProfilerGroupView.Init(treeRect, m_AudioProfilerGroupViewBackend);
                                }
                            }
                            else
                            {
                                var items = new List<AudioProfilerGroupInfoWrapper>();
                                m_AudioProfilerGroupViewBackend.SetData(items);
                                m_AudioProfilerGroupView = null;
                            }
                        }
                        if (m_AudioProfilerGroupView != null)
                            m_AudioProfilerGroupView.OnGUI(treeRect, m_ShowDetailedAudioPane);
                    }
                }
            }
            else
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                DrawDetailsViewText(position);
            }

            ProfilerWindow.Repaint();
        }

        bool AudioDeepProfileToggle()
        {
            int toggleFlags = (int)ProfilerCaptureFlags.Channels;
            if (Unsupported.IsDeveloperMode())
                toggleFlags |= (int)ProfilerCaptureFlags.Clips | (int)ProfilerCaptureFlags.DSPNodes;
            ProfilerAudioPopupItems oldShowDetailedAudioPane = (AudioSettings.profilerCaptureFlags & toggleFlags) != 0 ? ProfilerAudioPopupItems.Detailed : ProfilerAudioPopupItems.Simple;
            ProfilerAudioPopupItems newShowDetailedAudioPane = (ProfilerAudioPopupItems)EditorGUILayout.EnumPopup(oldShowDetailedAudioPane, EditorStyles.toolbarDropDownLeft, GUILayout.Width(70f));
            if (oldShowDetailedAudioPane != newShowDetailedAudioPane)
                ProfilerDriver.SetAudioCaptureFlags((AudioSettings.profilerCaptureFlags & ~toggleFlags) | (newShowDetailedAudioPane == ProfilerAudioPopupItems.Detailed ? toggleFlags : 0));
            return (AudioSettings.profilerCaptureFlags & toggleFlags) != 0;
        }

        Rect DrawAudioStatsPane(ref Vector2 scrollPos)
        {
            var totalRect = GUILayoutUtility.GetRect(20f, 20000f, 10, 10000f);
            var statsRect = new Rect(totalRect.x, totalRect.y, 230f, totalRect.height);
            var rightRect = new Rect(statsRect.xMax, totalRect.y, totalRect.width - statsRect.width, totalRect.height);

            // STATS
            var content = ProfilerDriver.GetOverviewText(area, ProfilerWindow.GetActiveVisibleFrameIndex());
            var textSize = EditorStyles.wordWrappedLabel.CalcSize(GUIContent.Temp(content));
            scrollPos = GUI.BeginScrollView(statsRect, scrollPos, new Rect(0, 0, textSize.x, textSize.y));
            GUI.Label(new Rect(3, 3, textSize.x, textSize.y), content, EditorStyles.wordWrappedLabel);
            GUI.EndScrollView();
            EditorGUI.DrawRect(new Rect(statsRect.xMax - 1, statsRect.y, 1, statsRect.height), Color.black);

            return rightRect;
        }

        /// <summary>
        /// Extracts a null-terminated string from a byte buffer at the given offset.
        /// </summary>
        static string GetNameFromBuffer(Unity.Collections.NativeArray<byte> buffer, int offset)
        {
            if (buffer.Length == 0 || offset < 0 || offset >= buffer.Length)
                return string.Empty;

            // Find the null terminator
            int end = offset;
            while (end < buffer.Length && buffer[end] != 0)
                end++;

            if (end == offset)
                return string.Empty;

            // Copy bytes and decode as UTF-8
            int length = end - offset;
            var bytes = new byte[length];
            for (int i = 0; i < length; i++)
                bytes[i] = buffer[offset + i];

            return System.Text.Encoding.UTF8.GetString(bytes);
        }
    }
}
