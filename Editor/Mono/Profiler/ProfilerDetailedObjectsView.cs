// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
    internal class ProfilerDetailedObjectsView : ProfilerDetailedView
    {
        ProfilerHierarchyGUI m_ProfilerHierarchyGUI;

        public ProfilerDetailedObjectsView(ProfilerHierarchyGUI profilerHierarchyGUI, ProfilerHierarchyGUI mainProfilerHierarchyGUI)
            : base(mainProfilerHierarchyGUI)
        {
            m_ProfilerHierarchyGUI = profilerHierarchyGUI;
        }

        public void DoGUI(GUIStyle headerStyle, int frameIndex, ProfilerViewType viewType)
        {
            var prop = GetDetailedProperty(frameIndex, viewType, m_ProfilerHierarchyGUI.sortType);
            if (prop != null)
                m_ProfilerHierarchyGUI.DoGUI(prop, string.Empty, false);
            else
                DrawEmptyPane(headerStyle);
        }

        private ProfilerProperty GetDetailedProperty(int frameIndex, ProfilerViewType viewType, ProfilerColumn sortType)
        {
            if (m_CachedProfilerPropertyConfig.EqualsTo(frameIndex, viewType, sortType))
            {
                return m_CachedProfilerProperty;
            }

            var detailProperty = m_MainProfilerHierarchyGUI.GetDetailedProperty();

            if (m_CachedProfilerProperty != null)
                m_CachedProfilerProperty.Cleanup();

            m_CachedProfilerPropertyConfig.Set(frameIndex, viewType, sortType);
            m_CachedProfilerProperty = detailProperty;

            return detailProperty;
        }
    }
}
