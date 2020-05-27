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
    internal class RenderingProfilerModule : ProfilerModuleBase
    {
        internal static class Styles
        {
            public static readonly GUIContent frameDebugger = EditorGUIUtility.TrTextContent("Open Frame Debugger", "Frame Debugger for current game view");
            public static readonly GUIContent noFrameDebugger = EditorGUIUtility.TrTextContent("Frame Debugger", "Open Frame Debugger (Current frame needs to be selected)");
        }

        public override void DrawToolbar(Rect position)
        {
            if (UnityEditor.MPE.ProcessService.level != UnityEditor.MPE.ProcessLevel.Master)
                return;
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button(GUI.enabled
                ? Styles.frameDebugger
                : Styles.noFrameDebugger, EditorStyles.toolbarButtonLeft))
            {
                FrameDebuggerWindow dbg = FrameDebuggerWindow.ShowFrameDebuggerWindow();
                dbg.EnableIfNeeded();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        public override void DrawView(Rect position)
        {
            DrawOverviewText(ProfilerArea.Rendering, position);
        }
    }
}
