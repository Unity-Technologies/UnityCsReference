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
    internal abstract class ProfilerModuleBase
    {
        protected IProfilerWindowController m_ProfilerWindow;

        protected Vector2 m_PaneScroll;

        public virtual void OnEnable(IProfilerWindowController profilerWindow)
        {
            m_ProfilerWindow = profilerWindow;
        }

        public virtual void OnDisable()
        {
        }

        public virtual void OnClosed()
        {
        }

        public abstract void DrawToolbar(Rect position);
        public abstract void DrawView(Rect position);

        protected void DrawOverviewText(ProfilerArea? area, Rect position)
        {
            if (!area.HasValue)
                return;

            string activeText = ProfilerDriver.GetOverviewText(area.Value, m_ProfilerWindow.GetActiveVisibleFrameIndex());
            float height = EditorStyles.wordWrappedLabel.CalcHeight(GUIContent.Temp(activeText), position.width);

            m_PaneScroll = GUILayout.BeginScrollView(m_PaneScroll, ProfilerWindow.Styles.background);
            EditorGUILayout.SelectableLabel(activeText, EditorStyles.wordWrappedLabel, GUILayout.MinHeight(height));
            GUILayout.EndScrollView();
        }

        protected static void DrawOtherToolbar(ProfilerArea? area)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.contentToolbar);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        public virtual void Clear()
        {
        }
    }
}
