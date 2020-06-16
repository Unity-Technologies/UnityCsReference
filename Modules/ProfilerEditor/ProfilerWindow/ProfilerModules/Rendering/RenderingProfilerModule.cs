// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using System.Text;
using System.Collections.Generic;
using UnityEditor.Profiling;

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

        const string k_IconName = "Profiler.Rendering";
        const int k_DefaultOrderIndex = 2;
        static readonly string k_Name = LocalizationDatabase.GetLocalizedString("Rendering");
        const string k_RenderCountersCategoryName = "Render";
        static readonly ProfilerCounterData[] k_DefaultRenderAreaCounterNames =
        {
            new ProfilerCounterData()
            {
                m_Name = "Batches Count",
                m_Category = k_RenderCountersCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "SetPass Calls Count",
                m_Category = k_RenderCountersCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Triangles Count",
                m_Category = k_RenderCountersCategoryName,
            },
            new ProfilerCounterData()
            {
                m_Name = "Vertices Count",
                m_Category = k_RenderCountersCategoryName,
            },
        };

        public RenderingProfilerModule(IProfilerWindowController profilerWindow) : base(profilerWindow, k_Name, k_IconName) {}

        protected override int defaultOrderIndex => k_DefaultOrderIndex;
        protected override string legacyPreferenceKey => "ProfilerChartRendering";

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

        public override void DrawDetailsView(Rect position)
        {
            string activeText = string.Empty;

            using (var f = ProfilerDriver.GetRawFrameDataView(m_ProfilerWindow.GetActiveVisibleFrameIndex(), 0))
            {
                if (f.valid)
                {
                    var batchesCount = GetCounterValue(f, "Batches Count");
                    if (batchesCount != -1)
                    {
                        var stringBuilder = new StringBuilder(1024);

                        stringBuilder.Append($"SetPass Calls: {GetCounterValueAsNumber(f, "SetPass Calls Count")}   \tDraw Calls: {GetCounterValueAsNumber(f, "Draw Calls Count")} \t\tTotal Batches: {batchesCount} \tTris: {GetCounterValueAsNumber(f, "Triangles Count")} \tVerts: {GetCounterValueAsNumber(f, "Vertices Count")}");
                        stringBuilder.Append($"\n(Dynamic Batching) \tBatched Draw Calls: {GetCounterValueAsNumber(f, "Dynamic Batched Draw Calls Count")} \tBatches: {GetCounterValueAsNumber(f, "Dynamic Batches Count")} \tTris: {GetCounterValueAsNumber(f, "Dynamic Batched Triangles Count")} \tVerts: {GetCounterValueAsNumber(f, "Dynamic Batched Vertices Count")} \tTime: {GetCounterValue(f, "Dynamic Batching Time") * 1e-6:0.00}ms");
                        stringBuilder.Append($"\n(Static Batching)    \tBatched Draw Calls: {GetCounterValueAsNumber(f, "Static Batched Draw Calls Count")} \tBatches: {GetCounterValueAsNumber(f, "Static Batches Count")} \tTris: {GetCounterValueAsNumber(f, "Static Batched Triangles Count")} \tVerts: {GetCounterValueAsNumber(f, "Static Batched Vertices Count")}");
                        stringBuilder.Append($"\n(Instancing)    \tBatched Draw Calls: {GetCounterValueAsNumber(f, "Instanced Batched Draw Calls Count")} \tBatches: {GetCounterValueAsNumber(f, "Instanced Batches Count")} \tTris: {GetCounterValueAsNumber(f, "Instanced Batched Triangles Count")} \tVerts: {GetCounterValueAsNumber(f, "Instanced Batched Vertices Count")}");

                        stringBuilder.Append($"\nUsed Textures: {GetCounterValue(f, "Used Textures Count")} / {GetCounterValueAsBytes(f, "Used Textures Bytes")}");
                        stringBuilder.Append($"\nRenderTextures: {GetCounterValue(f, "Render Textures Count")} / {GetCounterValueAsBytes(f, "Render Textures Bytes")}");
                        stringBuilder.Append($"\nRenderTexture Changes: {GetCounterValue(f, "Render Textures Changes Count")}");

                        stringBuilder.Append($"\nBuffers Total: {GetCounterValue(f, "Used Buffers Count")} / {GetCounterValueAsBytes(f, "Used Buffers Bytes")}");
                        stringBuilder.Append($"\nVertex Buffer Uploads: {GetCounterValue(f, "Vertex Buffer Upload In Frame Count")} / {GetCounterValueAsBytes(f, "Vertex Buffer Upload In Frame Bytes")}");
                        stringBuilder.Append($"\nIndex Buffer Uploads: {GetCounterValue(f, "Index Buffer Upload In Frame Count")} / {GetCounterValueAsBytes(f, "Index Buffer Upload In Frame Bytes")}");

                        stringBuilder.Append($"\nShadow Casters: {GetCounterValue(f, "Shadow Casters Count")}\n");

                        activeText = stringBuilder.ToString();
                    }
                    else
                    {
                        // Old data compatibility.
                        activeText = ProfilerDriver.GetOverviewText(ProfilerArea.Rendering, m_ProfilerWindow.GetActiveVisibleFrameIndex());
                    }
                }
            }
            float height = EditorStyles.wordWrappedLabel.CalcHeight(GUIContent.Temp(activeText), position.width);

            m_PaneScroll = GUILayout.BeginScrollView(m_PaneScroll, ProfilerWindow.Styles.background);
            EditorGUILayout.SelectableLabel(activeText, EditorStyles.wordWrappedLabel, GUILayout.MinHeight(height));
            GUILayout.EndScrollView();
        }

        protected override List<ProfilerCounterData> CollectDefaultChartCounters()
        {
            return new List<ProfilerCounterData>(k_DefaultRenderAreaCounterNames);
        }
    }
}
