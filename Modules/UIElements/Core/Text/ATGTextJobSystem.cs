// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements.UIR;
namespace UnityEngine.UIElements;

internal class ATGTextJobSystem
{
    class ManagedJobData
    {
        public TextElement textElement;
        public MeshGenerationNode node;
        // We might want to pool textInfo in order to reduce allocations.
        public NativeTextInfo textInfo;
        public bool success;

        public List<Texture2D> atlases;
        public List<float> sdfScales;
        public List<NativeSlice<Vertex>> vertices;
        public List<NativeSlice<ushort>> indices;
        public List<GlyphRenderMode> renderModes;
        public List<List<List<int>>> textElementIndicesByMesh;
        public List<bool> hasMultipleColorsByMesh;
        public Dictionary<int, HashSet<uint>> missingGlyphsPerFontAsset;
        public bool hasMissingGlyphs;

        public void AcquirePooledData()
        {
            missingGlyphsPerFontAsset = s_MissingGlyphsPool.Get();
            textElementIndicesByMesh = s_TextElementIndicesByMeshPool.Get();
            hasMultipleColorsByMesh = s_HasMultipleColorsByMeshPool.Get();
            atlases = s_AtlasesPool.Get();
            vertices = s_VerticesPool.Get();
            indices = s_IndicesPool.Get();
            renderModes = s_RenderModesPool.Get();
            sdfScales = s_SdfScalesPool.Get();
        }

        public void Release()
        {
            s_JobDataPool.Release(this);
            s_AtlasesPool.Release(atlases);
            s_SdfScalesPool.Release(sdfScales);
            s_VerticesPool.Release(vertices);
            s_IndicesPool.Release(indices);
            s_RenderModesPool.Release(renderModes);
            s_MissingGlyphsPool.Release(missingGlyphsPerFontAsset);
            s_TextElementIndicesByMeshPool.Release(textElementIndicesByMesh);
            s_HasMultipleColorsByMeshPool.Release(hasMultipleColorsByMesh);
        }
    }
    GCHandle textJobDatasHandle;
    List<ManagedJobData> textJobDatas = new List<ManagedJobData>();
    bool hasPendingTextWork;

    static UnityEngine.Pool.ObjectPool<ManagedJobData> s_JobDataPool =
        new(() => new ManagedJobData(), null, inst => inst.textElement = null, null, false);

    static UnityEngine.Pool.ObjectPool<List<Texture2D>> s_AtlasesPool = new(() =>
    {
        var inst = new List<Texture2D>();
        return inst;
    }, null, list => list.Clear(), null, false);

    static UnityEngine.Pool.ObjectPool<List<float>> s_SdfScalesPool = new(() =>
    {
        var inst = new List<float>();
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

    static UnityEngine.Pool.ObjectPool<List<List<List<int>>>> s_TextElementIndicesByMeshPool = new(() =>
    {
        var inst = new List<List<List<int>>>();
        return inst;
    },  null,
        (listOfMeshes) =>
        {
            foreach (var listOfAtlases in listOfMeshes)
            {
                foreach (var listOfIndices in listOfAtlases)
                {
                    listOfIndices.Clear();
                }
            }
        }, null, false);

    static UnityEngine.Pool.ObjectPool<List<bool>> s_HasMultipleColorsByMeshPool = new(() =>
    {
        var inst = new List<bool> ();
        return inst;
    }, null, list => list.Clear(), null, false);

    static UnityEngine.Pool.ObjectPool<Dictionary<int, HashSet<uint>>> s_MissingGlyphsPool = new(() =>
    {
        var inst = new Dictionary<int, HashSet<uint>>();
        return inst;
    },  null,
        (dict) =>
        {
            foreach (var hashSet in dict.Values)
            {
                hashSet.Clear();
            }
    },  null, false);

    static UnityEngine.Pool.ObjectPool<Dictionary<int, HashSet<uint>>> s_AggregatedMissingGlyphsPool = new(() =>
    {
        var inst = new Dictionary<int, HashSet<uint>>();
        return inst;
    }, null,
        (dict) =>
        {
            foreach (var hashSet in dict.Values)
            {
                hashSet.Clear();
            }
        }, null, false);

    internal MeshGenerationCallback m_GenerateTextJobifiedCallback;
    internal MeshGenerationCallback m_PopulateGlyphsCallback;
    internal MeshGenerationCallback m_AddDrawEntriesCallback;

    static readonly ProfilerMarker k_GenerateTextMarker = new("ATGTextJob.GenerateText");
    static readonly ProfilerMarker k_ATGTextJobMarker = new("ATGTextJob");

    static readonly bool k_IsMultiThreaded = (bool)Debug.GetDiagnosticSwitch("EnableMultiThreadingForATG").value;

    public ATGTextJobSystem()
    {
        m_GenerateTextJobifiedCallback = GenerateTextJobified;
        m_PopulateGlyphsCallback = PopulateGlyphs;
        m_AddDrawEntriesCallback = AddDrawEntries;
    }

    public void GenerateText(MeshGenerationContext mgc, TextElement textElement)
    {
        mgc.InsertMeshGenerationNode(out var node);

        ManagedJobData managedJobData = s_JobDataPool.Get();
        managedJobData.textElement = textElement;
        managedJobData.node = node;

        textJobDatas.Add(managedJobData);

        if (hasPendingTextWork)
            return;

        hasPendingTextWork = true;
        textJobDatasHandle = GCHandle.Alloc(textJobDatas);

        var mgct = k_IsMultiThreaded ? MeshGenerationCallbackType.Fork : MeshGenerationCallbackType.Work;
        mgc.AddMeshGenerationCallback(m_GenerateTextJobifiedCallback, null, mgct, false);
    }

    struct GenerateTextJobData : IJobParallelFor
    {
        public GCHandle managedJobDataHandle;
        [ReadOnly] public TempMeshAllocator alloc;

        public void Execute(int index)
        {
            k_GenerateTextMarker.Begin();
            var managedJobDatas = (List<ManagedJobData>)managedJobDataHandle.Target;
            ManagedJobData managedJobData = managedJobDatas[index];
            var ve = managedJobData.textElement;
            var shouldGenerateNativeTextSettings = ve.computedStyle.unityFontDefinition.fontAsset != null;
            if (ve.PostProcessTextVertices != null)
                ve.uitkTextHandle.CacheTextGenerationInfo();
                
            (managedJobData.textInfo, managedJobData.success, bool uvsAreGenerated) = ve.uitkTextHandle.UpdateNative(shouldGenerateNativeTextSettings);

            managedJobData.hasMissingGlyphs = managedJobData.textElement.uitkTextHandle.HasMissingGlyphs(managedJobData.textInfo, ref managedJobData.missingGlyphsPerFontAsset);

            // No missing glyphs means we do not need to return to main thread before converting to UIR
            if (!managedJobData.hasMissingGlyphs)
            {
                managedJobData.textElement.uitkTextHandle.ProcessMeshInfos(managedJobData.textInfo, ref managedJobData.textElementIndicesByMesh, ref managedJobData.hasMultipleColorsByMesh, uvsAreGenerated);
                ConvertMeshInfoToUIRVertex(managedJobData.textInfo.meshInfos, alloc, managedJobData.textElement, managedJobData.textElementIndicesByMesh, managedJobData.hasMultipleColorsByMesh, ref managedJobData.atlases, ref managedJobData.vertices, ref managedJobData.indices, ref managedJobData.renderModes, ref managedJobData.sdfScales);
            }   

            k_GenerateTextMarker.End();
        }
    }

    struct ConvertToUIRVertexJobData : IJobParallelFor
    {
        public GCHandle managedJobDataHandle;
        [ReadOnly] public TempMeshAllocator alloc;

        public void Execute(int index)
        {
            var managedJobDatas = (List<ManagedJobData>)managedJobDataHandle.Target;
            ManagedJobData managedJobData = managedJobDatas[index];
            var ve = managedJobData.textElement;

            if (managedJobData.hasMissingGlyphs)
            {
                managedJobData.textElement.uitkTextHandle.ProcessMeshInfos(managedJobData.textInfo, ref managedJobData.textElementIndicesByMesh, ref managedJobData.hasMultipleColorsByMesh, false);
                ConvertMeshInfoToUIRVertex(managedJobData.textInfo.meshInfos, alloc, managedJobData.textElement, managedJobData.textElementIndicesByMesh, managedJobData.hasMultipleColorsByMesh, ref managedJobData.atlases, ref managedJobData.vertices, ref managedJobData.indices, ref managedJobData.renderModes, ref managedJobData.sdfScales);
            }
        }
    }

    void GenerateTextJobified(MeshGenerationContext mgc, object _)
    {
        k_ATGTextJobMarker.Begin();

        mgc.GetTempMeshAllocator(out var alloc);
        var textJob = new GenerateTextJobData
        {
            managedJobDataHandle = textJobDatasHandle,
            alloc = alloc
        };

        if (textJobDatas.Count > 0)
            textJobDatas[0].textElement.uitkTextHandle.InitTextLib();

        FontAsset.CreateHbFaceIfNeeded();

        for (int i = 0; i < textJobDatas.Count; i++)
        {
            var textData = textJobDatas[i];
            var textElement = textData.textElement;
            var fa = TextUtilities.GetFontAsset(textElement);
            TextUtilities.GetTextSettingsFrom(textElement).UpdateNativeTextSettings();
            fa.EnsureNativeFontAssetIsCreated();
            // Unity Font object needs a call to GetCachedFontAsset() which needs to be called from the main thread.
            if (textElement.computedStyle.unityFontDefinition.fontAsset == null)
                textElement.uitkTextHandle.ConvertUssToNativeTextGenerationSettings();
            textData.AcquirePooledData();
        }

        if (k_IsMultiThreaded)
        {
            var jobHandle = textJob.Schedule(textJobDatas.Count, 1);
            mgc.AddMeshGenerationJob(jobHandle);
            mgc.AddMeshGenerationCallback(m_PopulateGlyphsCallback, null, MeshGenerationCallbackType.Work, true);
        }
        else
        {
            for (int i = 0; i < textJobDatas.Count; i++)
                textJob.Execute(i);
            mgc.AddMeshGenerationCallback(m_PopulateGlyphsCallback, null, MeshGenerationCallbackType.Work, false);
        }

        k_ATGTextJobMarker.End();
    }

    static List<uint> s_GlyphsToAddBuffer = new List<uint>();
    void PopulateGlyphs(MeshGenerationContext mgc, object _)
    {
        Dictionary<int, HashSet<uint>> allUniqueMissingGlyphs = s_AggregatedMissingGlyphsPool.Get();
        bool hasMissingGlyphs = false;
        foreach (var managedJobData in textJobDatas)
        {
            if (!managedJobData.hasMissingGlyphs)
                continue;

            foreach (var pair in managedJobData.missingGlyphsPerFontAsset)
            {
                if (pair.Value.Count == 0)
                    continue;

                hasMissingGlyphs = true;

                if (!allUniqueMissingGlyphs.TryGetValue(pair.Key, out var unicodeSet))
                {
                    unicodeSet = new HashSet<uint>();
                    allUniqueMissingGlyphs[pair.Key] = unicodeSet;
                }

                unicodeSet.UnionWith(pair.Value);
            }
        }

        // No missing unicodes means we already converted our mesh to UIR
        if (!hasMissingGlyphs)
        {
            s_AggregatedMissingGlyphsPool.Release(allUniqueMissingGlyphs);
            AddDrawEntries(mgc, _);
            return;
        }
            

        foreach (var entry in allUniqueMissingGlyphs)
        {
            var textAsset = TextCore.Text.TextAsset.GetTextAssetByID(entry.Key);
            if (textAsset == null || textAsset is not FontAsset fa || entry.Value.Count == 0)
                continue;

            s_GlyphsToAddBuffer.Clear();
            s_GlyphsToAddBuffer.AddRange(entry.Value);

            fa.TryAddGlyphs(s_GlyphsToAddBuffer);
        }

        s_AggregatedMissingGlyphsPool.Release(allUniqueMissingGlyphs);
        FontAsset.UpdateFontAssetsInUpdateQueue();

        mgc.GetTempMeshAllocator(out var alloc);
        var textJob = new ConvertToUIRVertexJobData
        {
            managedJobDataHandle = textJobDatasHandle,
            alloc = alloc
        };

        JobHandle jobHandle = textJob.Schedule(textJobDatas.Count, 1);
        mgc.AddMeshGenerationJob(jobHandle);
        mgc.AddMeshGenerationCallback(m_AddDrawEntriesCallback, null, MeshGenerationCallbackType.Work, true);
    }

    void AddDrawEntries(MeshGenerationContext mgc, object _)
    {
        foreach (var managedJobData in textJobDatas)
        {
            if (managedJobData.success)
            {
                var textInfo = managedJobData.textInfo;

                mgc.Begin(managedJobData.node.GetParentEntry(), managedJobData.textElement, managedJobData.textElement.renderData);
				managedJobData.textElement.PostProcessTextVertices?.Invoke(new TextElement.GlyphsEnumerable(managedJobData.textElement, managedJobData.vertices, textInfo.meshInfos));
                mgc.meshGenerator.DrawText(managedJobData.vertices, managedJobData.indices, managedJobData.atlases, managedJobData.renderModes, managedJobData.sdfScales);
                managedJobData.textElement.OnGenerateTextOverNative(mgc);
                managedJobData.textElement.uitkTextHandle.UpdateATGTextEventHandler();

                mgc.End();
            }

            // We don't need native informations anymore, clear the memory.
            if (!managedJobData.textElement.uitkTextHandle.IsCachedPermanent)
            {
                TextGenerationInfo.Destroy(managedJobData.textElement.uitkTextHandle.textGenerationInfo);
                managedJobData.textElement.uitkTextHandle.textGenerationInfo = IntPtr.Zero;
            }

            managedJobData.Release();
        }

        // get ready for next batch
        textJobDatas.Clear();
        textJobDatasHandle.Free();
        hasPendingTextWork = false;
    }

    static void ConvertMeshInfoToUIRVertex(Span<ATGMeshInfo> meshInfos, TempMeshAllocator alloc, TextElement visualElement, List<List<List<int>>> textElementIndicesByMesh, List<bool> hasMultipleColorsByMesh, ref List<Texture2D> atlases, ref List<NativeSlice<Vertex>> verticesArray, ref List<NativeSlice<ushort>> indicesArray, ref List<GlyphRenderMode> renderModes, ref List<float> sdfScales)
    {
        float inverseScale = 1.0f / visualElement.scaledPixelsPerPoint;

        for (int i = 0; i < meshInfos.Length; i++)
        {
            int atlasCount = 0;
            ATGMeshInfo meshInfo = meshInfos[i];
            FontAsset fa = null;
            SpriteAsset sa = null;
            var textAsset = TextCore.Text.TextAsset.GetTextAssetByID(meshInfo.textAssetId);
            if (textAsset == null)
                continue;
            bool isSprite = false;
            if (textAsset is FontAsset)
            {
                fa = textAsset as FontAsset;
                atlasCount = fa.atlasTextures.Length;
            }
            else
            {
                isSprite = true;
                sa = textAsset as SpriteAsset;
                atlasCount = 1;
            }

            //Debug.Assert((meshInfo.textElementInfos.Length & 0b11) == 0); // Quads only
            int verticesPerAlloc = (int)(UIRenderDevice.maxVerticesPerPage & ~3); // Round down to multiple of 4

            // If multiple colors are required(e.g., color tags are used), then ignore the dynamic-color hint
            // since we cannot store multiple colors for a given text element.
            bool hasMultipleColors = hasMultipleColorsByMesh[i];
            if (hasMultipleColors)
                visualElement.renderData.flags |= RenderDataFlags.IsIgnoringDynamicColorHint;
            else
                visualElement.renderData.flags &= ~RenderDataFlags.IsIgnoringDynamicColorHint;

            for (int j = 0; j < atlasCount; ++j)
            {
                var textElementInfoInAtlas = textElementIndicesByMesh[i][j];
                int remainingVertexCount = textElementInfoInAtlas.Count * 4;
                while (remainingVertexCount > 0)
                {
                    int vertexCount = Mathf.Min(remainingVertexCount, verticesPerAlloc);
                    int quadCount = vertexCount >> 2;
                    int indexCount = quadCount * 6;

                    if (isSprite)
                    {
                        atlases.Add((Texture2D)sa.material.mainTexture);
                        renderModes.Add(GlyphRenderMode.COLOR);
                    }
                    else
                    {
                        atlases.Add(fa.atlasTextures[j]);
                        renderModes.Add(fa.atlasRenderMode);
                    }


                    float sdfScale = 0;
                    if (!isSprite && !TextGeneratorUtilities.IsBitmapRendering(renderModes[^1]))
                        sdfScale = fa.atlasPadding + 1;
                    sdfScales.Add(sdfScale);

                    bool hasGradientScale = !isSprite && fa.atlasRenderMode != GlyphRenderMode.SMOOTH && fa.atlasRenderMode != GlyphRenderMode.COLOR;
                    // TODO, update once ATG supports SpriteAssets
                    bool isDynamicColor = /*meshInfo.applySDF &&*/ !hasMultipleColors && (RenderEvents.NeedsColorID(visualElement) || (hasGradientScale && RenderEvents.NeedsTextCoreSettings(visualElement)));

                    alloc.AllocateTempMesh(vertexCount, indexCount, out var vertices, out var indices);

                    var pos = (visualElement).contentRect.min;
                    for (int vDst = 0,vSrc = 0, k = 0; vDst < vertexCount; vDst += 4, vSrc += 1, k += 6)
                    {
                        var isColorFont = !isSprite && (fa.atlasRenderMode == GlyphRenderMode.COLOR || fa.atlasRenderMode == GlyphRenderMode.COLOR_HINTED);
                        Span<NativeTextElementInfo> textElementInfosSpan = meshInfo.textElementInfos;
                        var tei = textElementInfosSpan[textElementInfoInAtlas[vSrc]];
                        vertices[vDst + 0] = MeshGenerator.ConvertTextVertexToUIRVertex(ref tei.bottomLeft, pos, inverseScale, isDynamicColor, isColorFont);
                        vertices[vDst + 1] = MeshGenerator.ConvertTextVertexToUIRVertex(ref tei.topLeft, pos, inverseScale, isDynamicColor, isColorFont);
                        vertices[vDst + 2] = MeshGenerator.ConvertTextVertexToUIRVertex(ref tei.topRight, pos, inverseScale, isDynamicColor, isColorFont);
                        vertices[vDst + 3] = MeshGenerator.ConvertTextVertexToUIRVertex(ref tei.bottomRight, pos, inverseScale, isDynamicColor, isColorFont);

                        indices[k + 0] = (ushort)(vDst + 0);
                        indices[k + 1] = (ushort)(vDst + 1);
                        indices[k + 2] = (ushort)(vDst + 2);
                        indices[k + 3] = (ushort)(vDst + 2);
                        indices[k + 4] = (ushort)(vDst + 3);
                        indices[k + 5] = (ushort)(vDst + 0);
                    }

                    verticesArray.Add(vertices);
                    indicesArray.Add(indices);

                    remainingVertexCount -= vertexCount;
                }
                Debug.Assert(remainingVertexCount == 0);
            }
        }
    }
}
