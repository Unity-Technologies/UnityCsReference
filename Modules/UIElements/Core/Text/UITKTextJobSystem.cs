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
    internal class UITKTextJobSystem
    {
        class ManagedJobData
        {
            public GCHandle selfHandle;

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

        List<ManagedJobData> textJobDatas = new List<ManagedJobData>();
        bool hasPendingTextWork;

        static UnityEngine.Pool.ObjectPool<ManagedJobData> s_JobDataPool = new(() =>
        {
            var inst = new ManagedJobData();
            inst.selfHandle = GCHandle.Alloc(inst);
            return inst;
        }, OnGetManagedJob, inst => { inst.visualElement = null; }, inst => { inst.selfHandle.Free(); }, false);

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

            hasPendingTextWork = true;

            mgc.AddMeshGenerationCallback(PrepareTextJobified, textJobDatas, MeshGenerationCallbackType.WorkThenFork, false);
        }

        void PrepareTextJobified(MeshGenerationContext mgc, object data)
        {
            var textDatas = (List<ManagedJobData>)data;
            TextHandle.InitThreadArrays();
            TextHandle.currentTime = Time.realtimeSinceStartup;

            k_PrepareMainThreadMarker.Begin();
            hasPendingTextWork = false;

            GCHandle managedJobsHandle = GCHandle.Alloc(textDatas);
            var prepareJob = new PrepareTextJobData
            {
                managedJobDataHandle = managedJobsHandle
            };

            k_PrepareMainThreadMarker.End();
            JobHandle jobHandle = prepareJob.Schedule(textDatas.Count, 1);
            mgc.AddMeshGenerationJob(jobHandle);
            mgc.AddMeshGenerationCallback(GenerateTextJobified, managedJobsHandle, MeshGenerationCallbackType.Work, true);
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

                visualElement.uitkTextHandle.ConvertUssToTextGenerationSettings();
                managedJobData.prepareSuccess = visualElement.uitkTextHandle.PrepareFontAsset();
                k_PrepareJobifiedMarker.End();
            }
        }

        void GenerateTextJobified(MeshGenerationContext mgc, object data)
        {
            k_UpdateMainThreadMarker.Begin();
            var managedJobsHandle = (GCHandle)data;
            List<ManagedJobData> managedJobDatas = (List<ManagedJobData>)managedJobsHandle.Target;
            foreach (var textData in managedJobDatas)
            {
                if (!textData.prepareSuccess)
                {
                    textData.visualElement.uitkTextHandle.ConvertUssToTextGenerationSettings();
                    textData.visualElement.uitkTextHandle.PrepareFontAsset();
                }

                var fa = TextUtilities.GetFontAsset(textData.visualElement);
                if (fa.m_CharacterLookupDictionary == null)
                    fa.ReadFontAssetDefinition();
            }

            FontAsset.UpdateFontAssetsInUpdateQueue();

            k_UpdateMainThreadMarker.End();

            mgc.GetTempMeshAllocator(out var allocator);

            var textJob = new GenerateTextJobData
            {
                managedJobDataHandle = managedJobsHandle,
                alloc = allocator
            };
            JobHandle jobHandle = textJob.Schedule(managedJobDatas.Count, 1);

            mgc.AddMeshGenerationJob(jobHandle);
            mgc.AddMeshGenerationCallback(AddDrawEntries, textJob, MeshGenerationCallbackType.Work, true);
        }

        struct GenerateTextJobData : IJobParallelFor
        {
            public GCHandle managedJobDataHandle;
            [ReadOnly] public TempMeshAllocator alloc;

            public void Execute(int index)
            {
                k_ExecuteMarker.Begin();
                List<ManagedJobData> managedJobDatas = (List<ManagedJobData>)managedJobDataHandle.Target;
                ManagedJobData managedJobData = managedJobDatas[index];
                var visualElement = managedJobData.visualElement;
                visualElement.uitkTextHandle.ConvertUssToTextGenerationSettings();
                if (visualElement.uitkTextHandle.m_PreviousGenerationSettingsHash == TextHandle.settings.GetHashCode())
                {
                    visualElement.uitkTextHandle.AddTextInfoToCache();
                }
                var textInfo = visualElement.uitkTextHandle.UpdateFontAssetPrepared();
                var meshInfos = (textInfo).meshInfo;

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

        private static void ConvertMeshInfoToUIRVertex(MeshInfo[] meshInfos, TempMeshAllocator alloc, TextElement visualElement, ref List<Material> materials, ref List<NativeSlice<Vertex>> verticesArray, ref List<NativeSlice<ushort>> indicesArray, ref List<GlyphRenderMode> renderModes)
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
                        vertices[vDst + 0] = MeshGenerator.ConvertTextVertexToUIRVertex(meshInfo, vSrc + 0, pos, isDynamicColor);
                        vertices[vDst + 1] = MeshGenerator.ConvertTextVertexToUIRVertex(meshInfo, vSrc + 1, pos, isDynamicColor);
                        vertices[vDst + 2] = MeshGenerator.ConvertTextVertexToUIRVertex(meshInfo, vSrc + 2, pos, isDynamicColor);
                        vertices[vDst + 3] = MeshGenerator.ConvertTextVertexToUIRVertex(meshInfo, vSrc + 3, pos, isDynamicColor);

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

        void AddDrawEntries(MeshGenerationContext mgc, object data)
        {
            var textData = (GenerateTextJobData)data;
            var managedJobDatas = (List<ManagedJobData>)textData.managedJobDataHandle.Target;
            foreach (var managedJobData in managedJobDatas)
            {
                mgc.Begin(managedJobData.node.GetParentEntry(), managedJobData.visualElement);

                managedJobData.visualElement.uitkTextHandle.HandleLinkAndATagCallbacks();
                mgc.meshGenerator.DrawText(managedJobData.vertices, managedJobData.indices, managedJobData.materials, managedJobData.renderModes);
                managedJobData.visualElement.OnGenerateTextOver(mgc);

                mgc.End();
                managedJobData.Release();
            }

            managedJobDatas.Clear();
            textData.managedJobDataHandle.Free();
        }
    }
}
