// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal class AudioProfilerModule : ProfilerModuleBase
    {
        Vector2 m_PaneScroll_AudioChannels = Vector2.zero;
        Vector2 m_PaneScroll_AudioDSPLeft = Vector2.zero;
        Vector2 m_PaneScroll_AudioDSPRight = Vector2.zero;
        Vector2 m_PaneScroll_AudioClips = Vector2.zero;

        [SerializeField]
        bool m_ShowInactiveDSPChains = false;

        [SerializeField]
        bool m_HighlightAudibleDSPChains = true;

        [SerializeField]
        float m_DSPGraphZoomFactor = 1.0f;

        [SerializeField]
        private AudioProfilerGroupTreeViewState m_AudioProfilerGroupTreeViewState;
        private AudioProfilerGroupView m_AudioProfilerGroupView = null;
        private AudioProfilerGroupViewBackend m_AudioProfilerGroupViewBackend;

        [SerializeField]
        private AudioProfilerClipTreeViewState m_AudioProfilerClipTreeViewState;
        private AudioProfilerClipView m_AudioProfilerClipView = null;
        private AudioProfilerClipViewBackend m_AudioProfilerClipViewBackend;

        private AudioProfilerDSPView m_AudioProfilerDSPView;
        enum ProfilerAudioPopupItems
        {
            Simple = 0,
            Detailed = 1
        }
        ProfilerAudioView m_ShowDetailedAudioPane = (ProfilerAudioView)EditorPrefs.GetInt(k_ViewTypeSettingsKey, (int)ProfilerAudioView.Channels);

        int m_LastAudioProfilerFrame = -1;

        const string k_ViewTypeSettingsKey = "Profiler.AudioProfilerModule.ViewType";
        const string k_ShowInactiveDSPChainsSettingsKey = "Profiler.MemoryProfilerModule.ShowInactiveDSPChains";
        const string k_HighlightAudibleDSPChainsSettingsKey = "Profiler.MemoryProfilerModule.HighlightAudibleDSPChains";
        const string k_DSPGraphZoomFactorSettingsKey = "Profiler.MemoryProfilerModule.DSPGraphZoomFactor";
        const string k_AudioProfilerGroupTreeViewStateSettingsKey = "Profiler.MemoryProfilerModule.AudioProfilerGroupTreeViewState";
        const string k_AudioProfilerClipTreeViewStateSettingsKey = "Profiler.MemoryProfilerModule.AudioProfilerClipTreeViewState";

        public override void OnEnable(IProfilerWindowController profilerWindow)
        {
            base.OnEnable(profilerWindow);

            m_ShowDetailedAudioPane = (ProfilerAudioView)EditorPrefs.GetInt(k_ViewTypeSettingsKey, (int)ProfilerAudioView.Channels);
            m_ShowInactiveDSPChains = EditorPrefs.GetBool(k_ShowInactiveDSPChainsSettingsKey, m_ShowInactiveDSPChains);
            m_HighlightAudibleDSPChains = EditorPrefs.GetBool(k_HighlightAudibleDSPChainsSettingsKey, m_HighlightAudibleDSPChains);
            m_DSPGraphZoomFactor = SessionState.GetFloat(k_DSPGraphZoomFactorSettingsKey, m_DSPGraphZoomFactor);
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

        public override void SaveViewSettings()
        {
            base.SaveViewSettings();
            EditorPrefs.SetInt(k_ViewTypeSettingsKey, (int)m_ShowDetailedAudioPane);
            EditorPrefs.SetBool(k_ShowInactiveDSPChainsSettingsKey, m_ShowInactiveDSPChains);
            EditorPrefs.SetBool(k_HighlightAudibleDSPChainsSettingsKey, m_HighlightAudibleDSPChains);
            SessionState.SetFloat(k_DSPGraphZoomFactorSettingsKey, m_DSPGraphZoomFactor);
            if (m_AudioProfilerGroupTreeViewState != null)
                SessionState.SetString(k_AudioProfilerGroupTreeViewStateSettingsKey, EditorJsonUtility.ToJson(m_AudioProfilerGroupTreeViewState));
            if (m_AudioProfilerGroupTreeViewState != null)
                SessionState.SetString(k_AudioProfilerClipTreeViewStateSettingsKey, EditorJsonUtility.ToJson(m_AudioProfilerClipTreeViewState));
        }

        public override void DrawToolbar(Rect position)
        {
            // This module still needs to be broken apart into Toolbar and View.
        }

        public override void DrawView(Rect position)
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
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    var graphRect = DrawAudioStatsPane(ref m_PaneScroll_AudioDSPLeft);

                    m_PaneScroll_AudioDSPRight = GUI.BeginScrollView(graphRect, m_PaneScroll_AudioDSPRight, new Rect(0, 0, 10000, 20000));

                    var clippingRect = new Rect(m_PaneScroll_AudioDSPRight.x, m_PaneScroll_AudioDSPRight.y, graphRect.width, graphRect.height);

                    if (m_AudioProfilerDSPView == null)
                        m_AudioProfilerDSPView = new AudioProfilerDSPView();

                    ProfilerProperty property = m_ProfilerWindow.CreateProperty();
                    if (CheckFrameData(property))
                    {
                        m_AudioProfilerDSPView.OnGUI(clippingRect, property, m_ShowInactiveDSPChains, m_HighlightAudibleDSPChains, ref m_DSPGraphZoomFactor, ref m_PaneScroll_AudioDSPRight);
                    }
                    if (property != null)
                        property.Dispose();

                    GUI.EndScrollView();

                    m_ProfilerWindow.Repaint();
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

                    ProfilerProperty property = m_ProfilerWindow.CreateProperty();
                    if (CheckFrameData(property))
                    {
                        var currentFrame = m_ProfilerWindow.GetActiveVisibleFrameIndex();
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
                                    m_AudioProfilerClipView = new AudioProfilerClipView(m_ProfilerWindow as EditorWindow, m_AudioProfilerClipTreeViewState);
                                    m_AudioProfilerClipView.Init(treeRect, m_AudioProfilerClipViewBackend);
                                }
                            }
                        }
                        if (m_AudioProfilerClipView != null)
                            m_AudioProfilerClipView.OnGUI(treeRect);
                    }
                    if (property != null)
                        property.Dispose();
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

                    ProfilerProperty property = m_ProfilerWindow.CreateProperty();
                    if (CheckFrameData(property))
                    {
                        var currentFrame = m_ProfilerWindow.GetActiveVisibleFrameIndex();
                        if (currentFrame == -1 || m_LastAudioProfilerFrame != currentFrame)
                        {
                            m_LastAudioProfilerFrame = currentFrame;
                            var sourceItems = property.GetAudioProfilerGroupInfo();
                            if (sourceItems != null && sourceItems.Length > 0)
                            {
                                var items = new List<AudioProfilerGroupInfoWrapper>();
                                foreach (var s in sourceItems)
                                {
                                    bool isGroup = (s.flags & AudioProfilerGroupInfoHelper.AUDIOPROFILER_FLAGS_GROUP) != 0;
                                    if (m_ShowDetailedAudioPane == ProfilerAudioView.Channels && isGroup)
                                        continue;
                                    if (m_ShowDetailedAudioPane == ProfilerAudioView.Groups && !isGroup)
                                        continue;
                                    items.Add(new AudioProfilerGroupInfoWrapper(s, property.GetAudioProfilerNameByOffset(s.assetNameOffset), property.GetAudioProfilerNameByOffset(s.objectNameOffset), m_ShowDetailedAudioPane == ProfilerAudioView.Channels));
                                }
                                m_AudioProfilerGroupViewBackend.SetData(items);
                                if (m_AudioProfilerGroupView == null)
                                {
                                    m_AudioProfilerGroupView = new AudioProfilerGroupView(m_ProfilerWindow as EditorWindow, m_AudioProfilerGroupTreeViewState);
                                    m_AudioProfilerGroupView.Init(treeRect, m_AudioProfilerGroupViewBackend);
                                }
                            }
                        }
                        if (m_AudioProfilerGroupView != null)
                            m_AudioProfilerGroupView.OnGUI(treeRect, m_ShowDetailedAudioPane == ProfilerAudioView.Channels);
                    }
                    if (property != null)
                        property.Dispose();
                }
            }
            else
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                DrawOverviewText(ProfilerArea.Audio, position);
            }
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
            var content = ProfilerDriver.GetOverviewText(ProfilerArea.Audio, m_ProfilerWindow.GetActiveVisibleFrameIndex());
            var textSize = EditorStyles.wordWrappedLabel.CalcSize(GUIContent.Temp(content));
            scrollPos = GUI.BeginScrollView(statsRect, scrollPos, new Rect(0, 0, textSize.x, textSize.y));
            GUI.Label(new Rect(3, 3, textSize.x, textSize.y), content, EditorStyles.wordWrappedLabel);
            GUI.EndScrollView();
            EditorGUI.DrawRect(new Rect(statsRect.xMax - 1, statsRect.y, 1, statsRect.height), Color.black);

            return rightRect;
        }

        static bool CheckFrameData(ProfilerProperty property)
        {
            return property != null && property.frameDataReady;
        }
    }
}
