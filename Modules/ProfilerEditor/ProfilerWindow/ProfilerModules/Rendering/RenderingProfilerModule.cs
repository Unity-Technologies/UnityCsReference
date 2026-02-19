// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using System.Text;
using System.Collections.Generic;
using UnityEditor.Profiling;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    [ProfilerModuleMetadata("Rendering", typeof(LocalizationResource), IconPath = "Profiler.Rendering")]
    internal class RenderingProfilerModule : ProfilerModuleBase
    {
        internal static class Styles
        {
            public static readonly GUIContent frameDebugger = EditorGUIUtility.TrTextContent("Open Frame Debugger", "Frame Debugger for current game view");
            public static readonly GUIContent noFrameDebugger = EditorGUIUtility.TrTextContent("Frame Debugger", "Open Frame Debugger (Current frame needs to be selected)");
        }

        const int k_DefaultOrderIndex = 2;
        static readonly string k_RenderCountersCategoryName = ProfilerCategory.Render.Name;
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

        internal override ProfilerArea area => ProfilerArea.Rendering;
        private protected override int defaultOrderIndex => k_DefaultOrderIndex;
        private protected override string legacyPreferenceKey => "ProfilerChartRendering";

        public override void DrawToolbar(Rect position)
        {
            if (UnityEditor.MPE.ProcessService.level != UnityEditor.MPE.ProcessLevel.Main)
                return;
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button(GUI.enabled
                ? Styles.frameDebugger
                : Styles.noFrameDebugger, EditorStyles.toolbarButtonLeft))
            {
                FrameDebuggerWindow.OpenWindowAndToggleEnabled();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        public override void DrawDetailsView(Rect position)
        {
            string activeText = string.Empty;

            using (var f = ProfilerDriver.GetRawFrameDataView(ProfilerWindow.GetActiveVisibleFrameIndex(), 0))
            {
                if (f.valid)
                {
                    var batchesCount = GetCounterValue(f, "Batches Count");
                    var setPassCalls = GetCounterValue(f, "SetPass Calls Count");
                    var stringBuilder = new StringBuilder(1024);

                    // old GfxDeviceStats counters
                    // if "Batches Count" is available, it's an earlier version with old graphics stats, so we display the old stats
                    if (batchesCount != -1)
                    {
                        stringBuilder.Append($"SetPass Calls: {GetCounterValueAsNumber(f, "SetPass Calls Count")}   \tDraw Calls: {GetCounterValueAsNumber(f, "Draw Calls Count")} \t\tBatches: {batchesCount} \tTriangles: {GetCounterValueAsNumber(f, "Triangles Count")} \tVertices: {GetCounterValueAsNumber(f, "Vertices Count")}");
                        stringBuilder.Append($"\n(Dynamic Batching)\tBatched Draw Calls: {GetCounterValueAsNumber(f, "Dynamic Batched Draw Calls Count")} \tBatches: {GetCounterValueAsNumber(f, "Dynamic Batches Count")} \tTriangles: {GetCounterValueAsNumber(f, "Dynamic Batched Triangles Count")} \tVertices: {GetCounterValueAsNumber(f, "Dynamic Batched Vertices Count")} \tTime: {GetCounterValue(f, "Dynamic Batching Time") * 1e-6:0.00}ms");
                        stringBuilder.Append($"\n(Static Batching)\t\tBatched Draw Calls: {GetCounterValueAsNumber(f, "Static Batched Draw Calls Count")} \tBatches: {GetCounterValueAsNumber(f, "Static Batches Count")} \tTriangles: {GetCounterValueAsNumber(f, "Static Batched Triangles Count")} \tVertices: {GetCounterValueAsNumber(f, "Static Batched Vertices Count")}");
                        stringBuilder.Append($"\n(Instancing)\t\tBatched Draw Calls: {GetCounterValueAsNumber(f, "Instanced Batched Draw Calls Count")} \tBatches: {GetCounterValueAsNumber(f, "Instanced Batches Count")} \tTriangles: {GetCounterValueAsNumber(f, "Instanced Batched Triangles Count")} \tVertices: {GetCounterValueAsNumber(f, "Instanced Batched Vertices Count")}");

                        stringBuilder.Append($"\nUsed Textures: {GetCounterValue(f, "Used Textures Count")} / {GetCounterValueAsBytes(f, "Used Textures Bytes")}");
                        stringBuilder.Append($"\nRender Textures: {GetCounterValue(f, "Render Textures Count")} / {GetCounterValueAsBytes(f, "Render Textures Bytes")}");
                        stringBuilder.Append($"\nRender Textures Changes: {GetCounterValue(f, "Render Textures Changes Count")}");

                        stringBuilder.Append($"\nUsed Buffers: {GetCounterValue(f, "Used Buffers Count")} / {GetCounterValueAsBytes(f, "Used Buffers Bytes")}");
                        stringBuilder.Append($"\nVertex Buffer Upload In Frame: {GetCounterValue(f, "Vertex Buffer Upload In Frame Count")} / {GetCounterValueAsBytes(f, "Vertex Buffer Upload In Frame Bytes")}");
                        stringBuilder.Append($"\nIndex Buffer Upload In Frame: {GetCounterValue(f, "Index Buffer Upload In Frame Count")} / {GetCounterValueAsBytes(f, "Index Buffer Upload In Frame Bytes")}");

                        stringBuilder.Append($"\nShadow Casters: {GetCounterValue(f, "Shadow Casters Count")}\n");

                        activeText = stringBuilder.ToString();
                    }
                    // new GfxDeviceStats counters
                    // If "batches count" not available, it's the current updated graphics stats, so we display more new counters
                    else if (setPassCalls != -1)
                    {
                        // Get main stats
                        var triangles = GetCounterValue(f, "Triangles Count");
                        var vertices = GetCounterValue(f, "Vertices Count");
                        
                        // Calculate total draw calls (sum of all types)
                        long totalDrawCalls = 0;
                        long standardDrawCalls = GetCounterValue(f, "Standard Draw Calls Count");
                        long standardIndirectDrawCalls = GetCounterValue(f, "Standard Indirect Draw Calls Count");
                        long standardInstancedDrawCalls = GetCounterValue(f, "Standard Instanced Draw Calls Count");
                        long srpBatcherDrawCalls = GetCounterValue(f, "SRP Batcher Draw Calls Count");
                        long brgDrawCalls = GetCounterValue(f, "BRG Draw Calls Count");
                        long brgIndirectDrawCalls = GetCounterValue(f, "BRG Indirect Draw Calls Count");
                        long nullGeometryDrawCalls = GetCounterValue(f, "Null Geometry Draw Calls Count");
                        long nullGeometryIndirectDrawCalls = GetCounterValue(f, "Null Geometry Indirect Draw Calls Count");
                        
                        if (standardDrawCalls != -1) totalDrawCalls += standardDrawCalls;
                        if (standardIndirectDrawCalls != -1) totalDrawCalls += standardIndirectDrawCalls;
                        if (standardInstancedDrawCalls != -1) totalDrawCalls += standardInstancedDrawCalls;
                        if (srpBatcherDrawCalls != -1) totalDrawCalls += srpBatcherDrawCalls;
                        if (brgDrawCalls != -1) totalDrawCalls += brgDrawCalls;
                        if (brgIndirectDrawCalls != -1) totalDrawCalls += brgIndirectDrawCalls;
                        if (nullGeometryDrawCalls != -1) totalDrawCalls += nullGeometryDrawCalls;
                        if (nullGeometryIndirectDrawCalls != -1) totalDrawCalls += nullGeometryIndirectDrawCalls;

                        stringBuilder.Append($"SetPass Calls: {GetCounterValueAsNumber(f, "SetPass Calls Count")}   \tDraw Calls: {FormatNumber(totalDrawCalls)} \tTriangles: {GetCounterValueAsNumber(f, "Triangles Count")} \tVertices: {GetCounterValueAsNumber(f, "Vertices Count")}");
                        
                        // Draw Calls breakdown
                        stringBuilder.Append($"\nDraw Calls Breakdown: Standard: {GetCounterValueAsNumber(f, "Standard Draw Calls Count")}, Standard Indirect: {GetCounterValueAsNumber(f, "Standard Indirect Draw Calls Count")}, Standard Instanced: {GetCounterValueAsNumber(f, "Standard Instanced Draw Calls Count")}, SRP Batcher: {GetCounterValueAsNumber(f, "SRP Batcher Draw Calls Count")}, BRG: {GetCounterValueAsNumber(f, "BRG Draw Calls Count")}, BRG Indirect: {GetCounterValueAsNumber(f, "BRG Indirect Draw Calls Count")}, Null Geometry: {GetCounterValueAsNumber(f, "Null Geometry Draw Calls Count")}, Null Geometry Indirect: {GetCounterValueAsNumber(f, "Null Geometry Indirect Draw Calls Count")}");
                        
                        // Instances breakdown
                        stringBuilder.Append($"\nInstances Breakdown: Standard: {GetCounterValueAsNumber(f, "Standard Instances Count")}, Standard Indirect: {GetCounterValueAsNumber(f, "Standard Indirect Instances Count")}, Standard Instanced: {GetCounterValueAsNumber(f, "Standard Instanced Instances Count")}, SRP Batcher: {GetCounterValueAsNumber(f, "SRP Batcher Instances Count")}, BRG: {GetCounterValueAsNumber(f, "BRG Instances Count")}, BRG Indirect: {GetCounterValueAsNumber(f, "BRG Indirect Instances Count")}, Null Geometry: {GetCounterValueAsNumber(f, "Null Geometry Instances Count")}, Null Geometry Indirect: {GetCounterValueAsNumber(f, "Null Geometry Indirect Instances Count")}");

                        stringBuilder.Append($"\nUsed Buffers: {GetCounterValueAsNumber(f, "Used Buffers Count")} / {GetCounterValueAsBytes(f, "Used Buffers Bytes")}");
                        stringBuilder.Append($"\nVBO Uploads: {GetCounterValueAsNumber(f, "Vertex Buffer Upload In Frame Count")} / {GetCounterValueAsBytes(f, "Vertex Buffer Upload In Frame Bytes")}");
                        stringBuilder.Append($"\nIBO Uploads: {GetCounterValueAsNumber(f, "Index Buffer Upload In Frame Count")} / {GetCounterValueAsBytes(f, "Index Buffer Upload In Frame Bytes")}");

                        var usedTextureCount = GetCounterValue(f, "Used Textures Count");
                        if (usedTextureCount != -1)
                        {
                            stringBuilder.Append($"\nUsed Textures: {GetCounterValueAsNumber(f, "Used Textures Count")} / {GetCounterValueAsBytes(f, "Used Textures Bytes")}");
                        }

                        stringBuilder.Append($"\nRender Textures Changes: {GetCounterValueAsNumber(f, "Render Textures Changes Count")}");
                        stringBuilder.Append($"\nVisible Skinned Meshes: {GetCounterValueAsNumber(f, "Visible Skinned Meshes Count")}");
                        stringBuilder.Append($"\nUpdated Skinned Meshes: {GetCounterValueAsNumber(f, "Updated Skinned Meshes Count")}");

                        activeText = stringBuilder.ToString();
                    }
                    else
                    {
                        //deprecated data compatibility - fall back to legacy GetOverviewText
                        activeText = ProfilerDriver.GetOverviewText(ProfilerArea.Rendering, ProfilerWindow.GetActiveVisibleFrameIndex());
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
