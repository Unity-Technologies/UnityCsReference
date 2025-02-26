// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements.UIR;
using System.Collections.Generic;
using Unity.Profiling;

namespace UnityEngine.UIElements
{
    class UITKTextJobSystem
    {
        class ManagedJobData
        {
            public TextElement visualElement;
            public MeshGenerationNode node;
            public List<Material> materials;
            public List<GlyphRenderMode> renderModes;
            public List<NativeSlice<Vertex>> vertices;
            public List<NativeSlice<ushort>> indices;
            public bool prepareSuccess;

            public void Release()
            {
                if (materials != null)
                {
                    s_MaterialPool.Release(materials);
                    s_VerticesPool.Release(vertices);
                    s_IndicesPool.Release(indices);
                    s_RenderModesPool.Release(renderModes);
                }
                s_JobDataPool.Release(this);
            }
        }

        static readonly ProfilerMarker k_ExecuteMarker = new("TextJob.GenerateText");
        static readonly ProfilerMarker k_UpdateMainThreadMarker = new("TextJob.UpdateMainThread");
        static readonly ProfilerMarker k_PrepareMainThreadMarker = new("TextJob.PrepareMainThread");
        static readonly ProfilerMarker k_PrepareJobifiedMarker = new("TextJob.PrepareJobified");

        private GCHandle textJobDatasHandle;
        List<ManagedJobData> textJobDatas = new List<ManagedJobData>();
        bool hasPendingTextWork;

        static UnityEngine.Pool.ObjectPool<ManagedJobData> s_JobDataPool = new(() =>
        {
            var inst = new ManagedJobData();
            return inst;
        }, OnGetManagedJob, inst => { inst.visualElement = null; }, null, false);

        static UnityEngine.Pool.ObjectPool<List<Material>> s_MaterialPool = new(() =>
        {
            var inst = new List<Material>();
            return inst;
        }, null, list => list.Clear(), null, false);

        static UnityEngine.Pool.ObjectPool<List<GlyphRenderMode>> s_RenderModesPool = new(() =>
        {
            var inst = new List<GlyphRenderMode>();
            return inst;
        }, null, list => list.Clear(), null, false);

        static UnityEngine.Pool.ObjectPool<List<NativeSlice<Vertex>>> s_VerticesPool = new(() =>
        {
            var inst = new List<NativeSlice<Vertex>>();
            return inst;
        }, null, list => list.Clear(), null, false);

        static UnityEngine.Pool.ObjectPool<List<NativeSlice<ushort>>> s_IndicesPool = new(() =>
        {
            var inst = new List<NativeSlice<ushort>>();
            return inst;
        }, null, list => list.Clear(), null, false);

        internal MeshGenerationCallback m_PrepareTextJobifiedCallback;
        internal MeshGenerationCallback m_GenerateTextJobifiedCallback;
        internal MeshGenerationCallback m_AddDrawEntriesCallback;

        public UITKTextJobSystem()
        {
            m_PrepareTextJobifiedCallback = PrepareTextJobified;
            m_GenerateTextJobifiedCallback = GenerateTextJobified;
            m_AddDrawEntriesCallback = AddDrawEntries;
        }

        private static void OnGetManagedJob(ManagedJobData managedJobData)
        {
            managedJobData.vertices = null;
            managedJobData.indices = null;
            managedJobData.materials = null;
            managedJobData.renderModes = null;
            managedJobData.prepareSuccess = false;
        }

        internal void GenerateText(MeshGenerationContext mgc, TextElement textElement)
        {
            mgc.InsertMeshGenerationNode(out var node);

            ManagedJobData managedJobData = s_JobDataPool.Get();
            managedJobData.visualElement = textElement;
            managedJobData.node = node;

            textJobDatas.Add(managedJobData);

            if (hasPendingTextWork)
                return;

            // schedule the batch only once
            // textJobDatas will be filled when the callback is invoked

            hasPendingTextWork = true;
            textJobDatasHandle = GCHandle.Alloc(textJobDatas);

            mgc.AddMeshGenerationCallback(m_PrepareTextJobifiedCallback, null, MeshGenerationCallbackType.WorkThenFork, false);
        }

        internal void PrepareTextJobified(MeshGenerationContext mgc, object _)
        {
            TextHandle.InitThreadArrays();
            PanelTextSettings.InitializeDefaultPanelTextSettingsIfNull();
            TextHandle.UpdateCurrentFrame();
            // Load the default editor text settings while on the  main thread
            _ = TextUtilities.textSettings;


            k_PrepareMainThreadMarker.Begin();

            hasPendingTextWork = false;

            var prepareJob = new PrepareTextJobData
            {
                managedJobDataHandle = textJobDatasHandle
            };

            k_PrepareMainThreadMarker.End();
            TextCore.Text.TextGenerator.IsExecutingJob = true;
            JobHandle jobHandle = prepareJob.Schedule(textJobDatas.Count, 1);
            mgc.AddMeshGenerationJob(jobHandle);
            mgc.AddMeshGenerationCallback(m_GenerateTextJobifiedCallback, null, MeshGenerationCallbackType.Work, true);
        }

        struct PrepareTextJobData : IJobParallelFor
        {
            public GCHandle managedJobDataHandle;

            public void Execute(int index)
            {
                k_PrepareJobifiedMarker.Begin();
                List<ManagedJobData> managedJobDatas = (List<ManagedJobData>)managedJobDataHandle.Target;
                ManagedJobData managedJobData = managedJobDatas[index];
                var visualElement = managedJobData.visualElement;

                managedJobData.prepareSuccess = visualElement.uitkTextHandle.ConvertUssToTextGenerationSettings();
                if (managedJobData.prepareSuccess)
                    managedJobData.prepareSuccess = visualElement.uitkTextHandle.PrepareFontAsset();
                k_PrepareJobifiedMarker.End();
            }
        }

        void GenerateTextJobified(MeshGenerationContext mgc, object _)
        {
            TextCore.Text.TextGenerator.IsExecutingJob = false;
            k_UpdateMainThreadMarker.Begin();
            foreach (var textData in textJobDatas)
            {
                // Loading line breaking rules is done here because it requires the main thread
                var settings = TextUtilities.GetTextSettingsFrom(textData.visualElement);
                settings?.lineBreakingRules?.LoadLineBreakingRules();

                if (textData.prepareSuccess)
                    continue;

                textData.visualElement.uitkTextHandle.ConvertUssToTextGenerationSettings();
                textData.visualElement.uitkTextHandle.PrepareFontAsset();
            }

            FontAsset.UpdateFontAssetsInUpdateQueue();

            k_UpdateMainThreadMarker.End();

            mgc.GetTempMeshAllocator(out var allocator);

            var textJob = new GenerateTextJobData
            {
                managedJobDataHandle = textJobDatasHandle,
                alloc = allocator
            };

            TextHandle.UpdateCurrentFrame();

            TextCore.Text.TextGenerator.IsExecutingJob = true;
            JobHandle jobHandle = textJob.Schedule(textJobDatas.Count, 1);
            mgc.AddMeshGenerationJob(jobHandle);
            mgc.AddMeshGenerationCallback(m_AddDrawEntriesCallback, null, MeshGenerationCallbackType.Work, true);
        }

        struct GenerateTextJobData : IJobParallelFor
        {
            public GCHandle managedJobDataHandle;
            [ReadOnly] public TempMeshAllocator alloc;

            public void Execute(int index)
            {
                k_ExecuteMarker.Begin();
                var managedJobDatas = (List<ManagedJobData>)managedJobDataHandle.Target;
                ManagedJobData managedJobData = managedJobDatas[index];
                var visualElement = managedJobData.visualElement;
                visualElement.uitkTextHandle.UpdateMesh();

                var textInfo = visualElement.uitkTextHandle.textInfo;
                var meshInfos = textInfo.meshInfo;

                List<Material> materials = null;
                List<NativeSlice<Vertex>> verticesArray = null;
                List<NativeSlice<ushort>> indicesArray = null;
                List<GlyphRenderMode> renderModes = null;

                ConvertMeshInfoToUIRVertex(meshInfos, alloc, visualElement, ref materials, ref verticesArray, ref indicesArray, ref renderModes);

                managedJobData.materials = materials;
                managedJobData.vertices = verticesArray;
                managedJobData.indices = indicesArray;
                managedJobData.renderModes = renderModes;

                visualElement.uitkTextHandle.HandleATag();
                visualElement.uitkTextHandle.HandleLinkTag();

                k_ExecuteMarker.End();
            }
        }

        static void ConvertMeshInfoToUIRVertex(MeshInfo[] meshInfos, TempMeshAllocator alloc, TextElement visualElement, ref List<Material> materials, ref List<NativeSlice<Vertex>> verticesArray, ref List<NativeSlice<ushort>> indicesArray, ref List<GlyphRenderMode> renderModes)
        {
            lock (s_MaterialPool)
            {
                materials = s_MaterialPool.Get();
                verticesArray = s_VerticesPool.Get();
                indicesArray = s_IndicesPool.Get();
                renderModes = s_RenderModesPool.Get();
            }

            var pos = (visualElement).contentRect.min;

            // If multiple colors are required(e.g., color tags are used), then ignore the dynamic-color hint
            // since we cannot store multiple colors for a given text element.
            bool hasMultipleColors = visualElement.uitkTextHandle.textInfo.hasMultipleColors;
            if (hasMultipleColors)
                visualElement.renderChainData.flags |= RenderDataFlags.IsIgnoringDynamicColorHint;
            else
                visualElement.renderChainData.flags &= ~RenderDataFlags.IsIgnoringDynamicColorHint;

            for (int i = 0; i < meshInfos.Length; i++)
            {
                var meshInfo = meshInfos[i];
                Debug.Assert((meshInfo.vertexCount & 0b11) == 0); // Quads only
                int verticesPerAlloc = (int)(UIRenderDevice.maxVerticesPerPage & ~3); // Round down to multiple of 4

                int remainingVertexCount = meshInfo.vertexCount;
                int vSrc = 0;
                while (remainingVertexCount > 0)
                {
                    int vertexCount = Mathf.Min(remainingVertexCount, verticesPerAlloc);
                    int quadCount = vertexCount >> 2;
                    int indexCount = quadCount * 6;

                    materials.Add(meshInfo.material);
                    renderModes.Add(meshInfo.glyphRenderMode);

                    bool hasGradientScale = meshInfo.glyphRenderMode != GlyphRenderMode.SMOOTH && meshInfo.glyphRenderMode != GlyphRenderMode.COLOR;
                    bool isDynamicColor = meshInfo.applySDF && !hasMultipleColors && (RenderEvents.NeedsColorID(visualElement) || (hasGradientScale && RenderEvents.NeedsTextCoreSettings(visualElement)));

                    alloc.AllocateTempMesh(vertexCount, indexCount, out var vertices, out var indices);

                    for (int vDst = 0, j = 0; vDst < vertexCount; vDst += 4, vSrc += 4, j += 6)
                    {
                        vertices[vDst + 0] = MeshGenerator.ConvertTextVertexToUIRVertex(meshInfo.vertexData[vSrc + 0], pos, isDynamicColor);
                        vertices[vDst + 1] = MeshGenerator.ConvertTextVertexToUIRVertex(meshInfo.vertexData[vSrc + 1], pos, isDynamicColor);
                        vertices[vDst + 2] = MeshGenerator.ConvertTextVertexToUIRVertex(meshInfo.vertexData[vSrc + 2], pos, isDynamicColor);
                        vertices[vDst + 3] = MeshGenerator.ConvertTextVertexToUIRVertex(meshInfo.vertexData[vSrc + 3], pos, isDynamicColor);

                        indices[j + 0] = (ushort)(vDst + 0);
                        indices[j + 1] = (ushort)(vDst + 1);
                        indices[j + 2] = (ushort)(vDst + 2);
                        indices[j + 3] = (ushort)(vDst + 2);
                        indices[j + 4] = (ushort)(vDst + 3);
                        indices[j + 5] = (ushort)(vDst + 0);
                    }

                    verticesArray.Add(vertices);
                    indicesArray.Add(indices);

                    remainingVertexCount -= vertexCount;
                }

                Debug.Assert(remainingVertexCount == 0);
            }
        }

        void AddDrawEntries(MeshGenerationContext mgc, object _)
        {
            TextCore.Text.TextGenerator.IsExecutingJob = false;
            foreach (var managedJobData in textJobDatas)
            {
                mgc.Begin(managedJobData.node.GetParentEntry(), managedJobData.visualElement);

                managedJobData.visualElement.uitkTextHandle.HandleLinkAndATagCallbacks();
                mgc.meshGenerator.DrawText(managedJobData.vertices, managedJobData.indices, managedJobData.materials, managedJobData.renderModes);
                managedJobData.visualElement.OnGenerateTextOver(mgc);

                mgc.End();
                managedJobData.Release();
            }

            // get ready for next batch
            textJobDatas.Clear();
            textJobDatasHandle.Free();
        }
    }
}
