// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine.TextCore;
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

        public List<Texture2D> atlases = new();
        public List<float> sdfScales = new();
        public List<NativeSlice<Vertex>> vertices = new();
        public List<NativeSlice<ushort>> indices = new();
        public List<GlyphRenderMode> renderModes = new();
        public List<List<List<int>>> textElementIndicesByMesh = new();
        public List<bool> hasMultipleColorsByMesh = new();
        // Key: FontAsset ID
        // Value: Set of missing glyphs (glyphID) for that font asset.
        public Dictionary<EntityId, HashSet<uint>> missingGlyphsPerFontAsset = new();
        public bool hasMissingGlyphs;

        public void Clear()
        {
            textElement = null;
            node = default;
            textInfo = default;
            success = false;
            hasMissingGlyphs = false;

            atlases.Clear();
            sdfScales.Clear();
            vertices.Clear();
            indices.Clear();
            renderModes.Clear();
            hasMultipleColorsByMesh.Clear();

            foreach (var listOfAtlases in textElementIndicesByMesh)
            {
                foreach (var listOfIndices in listOfAtlases)
                {
                    listOfIndices.Clear();
                }
            }

            foreach (var hashSet in missingGlyphsPerFontAsset.Values)
            {
                hashSet.Clear();
            }
        }
    }

    GCHandle textJobDatasHandle;
    List<ManagedJobData> textJobDatas = new List<ManagedJobData>();
    bool hasPendingTextWork;

    static readonly UnityEngine.Pool.ObjectPool<ManagedJobData> s_JobDataPool =
       new(() => new ManagedJobData(),   // Creates a new instance with its own collections
           null,                          // No action needed on get
           inst => inst.Clear(),          // On release, just clear the internal data
           null,
           false);

    static UnityEngine.Pool.ObjectPool<Dictionary<EntityId, HashSet<uint>>> s_AggregatedMissingGlyphsPool = new(() =>
    {
        var inst = new Dictionary<EntityId, HashSet<uint>>();
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
    static readonly ProfilerMarker k_PrepareShapingMarker = new("LayoutUpdater.PrepareShaping");

    static readonly bool k_IsMultiThreaded = (bool)Debug.GetDiagnosticSwitch("EnableMultiThreadingForATG").value;

    public ATGTextJobSystem()
    {
        m_GenerateTextJobifiedCallback = GenerateTextJobified;
        m_PopulateGlyphsCallback = PopulateGlyphs;
        m_AddDrawEntriesCallback = AddDrawEntries;
    }


    static bool PrepareTextElementForJobsOnMainThread(TextElement textElement)
    {
        textElement.uitkTextHandle.EnsureIsReadyForJobs();

        // Pre-load font assets from font tags before jobs
        if (textElement.enableRichText)
        {
            var textSettings = TextUtilities.GetTextSettingsFrom(textElement);
            RichTextTagParser.PreloadFontAssetsFromTags(textElement.renderedTextString, textSettings);
            RichTextTagParser.PreloadSpriteAssetsFromTags(textElement.renderedTextString, textSettings);
            RichTextTagParser.PreloadGradientAssetsFromTags(textElement.renderedTextString, textSettings);
        }

        return true;
    }

    List<TextElement> m_PrepareShapingDataList = new();

    struct PrepareShapingJob : IJobFor
    {
        public GCHandle managedJobDataHandle;
        public void Execute(int index)
        {
            var managedJobDatas = (List<TextElement>)managedJobDataHandle.Target;
            TextElement textElement = managedJobDatas[index];
            textElement.uitkTextHandle.ShapeText();
        }
    }

    // The goal of this method is to prepare the text shaping for all TextElements
    // that will get measured in the following layout pass
    // This is not currently perfectly accurate, but the rest of the system accounts for that.
    // What matters is not elements are picked up in the common case to improve performance
    internal void PrepareShapingBeforeLayout(BaseVisualElementPanel panel)
    {
        // Nothing to do if the layout is not dirty
        if (!panel.visualTree.layoutNode.IsDirty)
            return;

        // Depending on ATG configuration, it's possible that no TextElements have been registered.
        if (!panel.textElementRegistry.IsValueCreated)
            return;

        using var _ = k_PrepareShapingMarker.Auto();

        foreach (TextElement textElement in panel.textElementRegistry.Value)
        {
            // Note about display: none
            // Setting display: none on a LayoutNode will not mark its children dirty
            // This means that we could accidentally pre-shape elements that are not displayed and won't be measured
            // However, there is no direct way to compute this information from a node.
            // We would need a pretraversal of the hierarchy to update "areAncestorsAndSelfDisplayed" before the layout pass
            // However usage of display: none for large hierarchies is usually to hide elements after they were displayed
            // In which case they layoutNode will not be dirty so this code path would not be hit.
            if (textElement.layoutNode.IsDirty)
            {
                if (TextUtilities.IsAdvancedTextEnabledForElement(textElement) // Only advanced text elements need shaping
                    && TextElement.AnySizeAutoOrNone(ref textElement.computedStyle)) // Only elements without fixed width/height get measured
                {
                    if (PrepareTextElementForJobsOnMainThread(textElement))
                        m_PrepareShapingDataList.Add(textElement);
                }
            }
        }
        if (m_PrepareShapingDataList.Count > 0)
        {
            var handle = GCHandle.Alloc(m_PrepareShapingDataList);

            var job = new PrepareShapingJob
            {
                managedJobDataHandle = handle
            };
            var jobHandle = job.ScheduleParallelByRef(m_PrepareShapingDataList.Count, 1, default);
            jobHandle.Complete();
            handle.Free();
            m_PrepareShapingDataList.Clear();
        }
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

    struct GenerateTextJobData : IJobFor
    {
        public GCHandle managedJobDataHandle;
        [ReadOnly] public TempMeshAllocator alloc;

        public void Execute(int index)
        {
            k_GenerateTextMarker.Begin();
            var managedJobDatas = (List<ManagedJobData>)managedJobDataHandle.Target;
            ManagedJobData managedJobData = managedJobDatas[index];
            var ve = managedJobData.textElement;
            if (ve.PostProcessTextVertices != null)
                ve.uitkTextHandle.CacheTextGenerationInfo();

            (managedJobData.textInfo, managedJobData.success) = ve.uitkTextHandle.UpdateNative();

            managedJobData.hasMissingGlyphs = managedJobData.textElement.uitkTextHandle.HasMissingGlyphs(managedJobData.textInfo, ref managedJobData.missingGlyphsPerFontAsset);

            // No missing glyphs means we do not need to return to main thread before converting to UIR
            if (!managedJobData.hasMissingGlyphs)
            {
                managedJobData.textElement.uitkTextHandle.ProcessMeshInfos(managedJobData.textInfo, ref managedJobData.textElementIndicesByMesh, ref managedJobData.hasMultipleColorsByMesh);
                ConvertMeshInfoToUIRVertex(managedJobData.textInfo.meshInfos, alloc, managedJobData.textElement, managedJobData.textElementIndicesByMesh, managedJobData.hasMultipleColorsByMesh, ref managedJobData.atlases, ref managedJobData.vertices, ref managedJobData.indices, ref managedJobData.renderModes, ref managedJobData.sdfScales);
            }

            k_GenerateTextMarker.End();
        }
    }

    struct ConvertToUIRVertexJobData : IJobFor
    {
        public GCHandle managedJobDataHandle;
        [ReadOnly] public TempMeshAllocator alloc;

        public void Execute(int index)
        {
            var managedJobDatas = (List<ManagedJobData>)managedJobDataHandle.Target;
            ManagedJobData managedJobData = managedJobDatas[index];

            if (managedJobData.hasMissingGlyphs)
            {
                managedJobData.textElement.uitkTextHandle.ProcessMeshInfos(managedJobData.textInfo, ref managedJobData.textElementIndicesByMesh, ref managedJobData.hasMultipleColorsByMesh);
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

        for (int i = textJobDatas.Count - 1; i >= 0; i--)
        {
            var textData = textJobDatas[i];
            var textElement = textData.textElement;
            bool valid = PrepareTextElementForJobsOnMainThread(textElement);
            if (!valid)
                textJobDatas.RemoveAt(i);
        }

        if (k_IsMultiThreaded)
        {
            var jobHandle = textJob.ScheduleParallelByRef(textJobDatas.Count, 1, default);
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
        Dictionary<EntityId, HashSet<uint>> allUniqueMissingGlyphs = s_AggregatedMissingGlyphsPool.Get();
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
            var textAsset = Object.FindObjectFromInstanceIDThreadSafe(entry.Key) as TextCore.Text.TextAsset;
            if (textAsset == null || textAsset is not FontAsset fa || entry.Value.Count == 0)
                continue;

            s_GlyphsToAddBuffer.Clear();
            s_GlyphsToAddBuffer.AddRange(entry.Value);

            fa.TryAddGlyphs(s_GlyphsToAddBuffer);
        }

        s_AggregatedMissingGlyphsPool.Release(allUniqueMissingGlyphs);
        FontAsset.CreateHbFaceIfNeeded();
        FontAsset.UpdateFontAssetsInUpdateQueue();

        mgc.GetTempMeshAllocator(out var alloc);
        var textJob = new ConvertToUIRVertexJobData
        {
            managedJobDataHandle = textJobDatasHandle,
            alloc = alloc
        };

        JobHandle jobHandle = textJob.ScheduleParallelByRef(textJobDatas.Count, 1, default);
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

            s_JobDataPool.Release(managedJobData);
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
            var textAsset = Object.FindObjectFromInstanceIDThreadSafe(meshInfo.textAssetId) as TextCore.Text.TextAsset;
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
                int vSrc = 0;
                while (remainingVertexCount > 0)
                {
                    int vertexCount = Mathf.Min(remainingVertexCount, verticesPerAlloc);
                    int quadCount = vertexCount >> 2;
                    int indexCount = quadCount * 6;

                    if (isSprite)
                    {
                        atlases.Add((Texture2D)sa.spriteSheet);
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
                    bool isDynamicColor = visualElement.PostProcessTextVertices == null && !hasMultipleColors && (RenderEvents.NeedsColorID(visualElement) || (hasGradientScale && RenderEvents.NeedsTextCoreSettings(visualElement)));

                    alloc.AllocateTempMesh(vertexCount, indexCount, out var vertices, out var indices);

                    var pos = (visualElement).contentRect.min;
                    for (int vDst = 0, k = 0; vDst < vertexCount; vDst += 4, vSrc += 1, k += 6)
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
