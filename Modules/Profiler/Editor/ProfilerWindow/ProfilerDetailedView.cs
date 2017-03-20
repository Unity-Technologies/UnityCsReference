// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
    internal abstract class ProfilerDetailedView
    {
        protected struct CachedProfilerPropertyConfig
        {
            public bool EqualsTo(int frameIndex, ProfilerViewType viewType, ProfilerColumn sortType)
            {
                return this.frameIndex == frameIndex &&
                    this.sortType == sortType &&
                    this.viewType == viewType &&
                    this.propertyPath == ProfilerDriver.selectedPropertyPath;
            }

            public void Set(int frameIndex, ProfilerViewType viewType, ProfilerColumn sortType)
            {
                this.frameIndex = frameIndex;
                this.sortType = sortType;
                this.viewType = viewType;
                this.propertyPath = ProfilerDriver.selectedPropertyPath;
            }

            public string propertyPath;
            public int frameIndex;
            public ProfilerColumn sortType;
            public ProfilerViewType viewType;
        }

        protected readonly ProfilerHierarchyGUI m_MainProfilerHierarchyGUI;
        protected CachedProfilerPropertyConfig m_CachedProfilerPropertyConfig;
        protected ProfilerProperty m_CachedProfilerProperty;

        static class Styles
        {
            public static GUIContent emptyText = new GUIContent("");
            public static GUIContent selectLineText = new GUIContent("Select Line for the detailed information");
        }

        protected ProfilerDetailedView(ProfilerHierarchyGUI mainProfilerHierarchyGUI)
        {
            m_MainProfilerHierarchyGUI = mainProfilerHierarchyGUI;
        }

        public void ResetCachedProfilerProperty()
        {
            if (m_CachedProfilerProperty != null)
            {
                m_CachedProfilerProperty.Cleanup();
                m_CachedProfilerProperty = null;
            }

            m_CachedProfilerPropertyConfig.frameIndex = -1;
        }

        protected void DrawEmptyPane(GUIStyle headerStyle)
        {
            GUILayout.Box(Styles.emptyText, headerStyle);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.Label(Styles.selectLineText, EditorStyles.wordWrappedLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}
