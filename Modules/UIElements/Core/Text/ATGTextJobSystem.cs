// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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

        public void Release()
        {
            s_JobDataPool.Release(this);
        }
    }
    GCHandle textJobDatasHandle;
    List<ManagedJobData> textJobDatas = new List<ManagedJobData>();
    bool hasPendingTextWork;

    static UnityEngine.Pool.ObjectPool<ManagedJobData> s_JobDataPool =
        new(() => new ManagedJobData(), null, inst => inst.textElement = null, null, false);

    internal MeshGenerationCallback m_GenerateTextJobifiedCallback;
    internal MeshGenerationCallback m_AddDrawEntriesCallback;

    static readonly ProfilerMarker k_GenerateTextMarker = new("ATGTextJob.GenerateText");
    static readonly ProfilerMarker k_ATGTextJobMarker = new("ATGTextJob");
    static readonly bool k_IsMultiThreaded = (bool)Debug.GetDiagnosticSwitch("EnableMultiThreadingForATG").value;
    
    public ATGTextJobSystem()
    {
        m_GenerateTextJobifiedCallback = GenerateTextJobified;
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

        public void Execute(int index)
        {
            k_GenerateTextMarker.Begin();
            var managedJobDatas = (List<ManagedJobData>)managedJobDataHandle.Target;
            ManagedJobData managedJobData = managedJobDatas[index];
            var ve = managedJobData.textElement;
            var shouldGenerateNativeTextSettings = ve.computedStyle.unityFontDefinition.fontAsset != null;
            (managedJobData.textInfo, managedJobData.success) = ve.uitkTextHandle.UpdateNative(shouldGenerateNativeTextSettings);
            k_GenerateTextMarker.End();
        }
    }

    void GenerateTextJobified(MeshGenerationContext mgc, object _)
    {
        k_ATGTextJobMarker.Begin();

        var textJob = new GenerateTextJobData
        {
            managedJobDataHandle = textJobDatasHandle,
        };

        if (textJobDatas.Count > 0)
            textJobDatas[0].textElement.uitkTextHandle.InitTextLib();

        FontAsset.CreateHbFaceIfNeeded();

        for(int i = 0; i < textJobDatas.Count; i++)
        {
            var textData = textJobDatas[i];
            var textElement = textData.textElement;
            var fa = TextUtilities.GetFontAsset(textElement);
            TextUtilities.GetTextSettingsFrom(textElement).UpdateNativeTextSettings();
            fa.EnsureNativeFontAssetIsCreated();
            // Unity Font object needs a call to GetCachedFontAsset() which needs to be called from the main thread.
            if (textElement.computedStyle.unityFontDefinition.fontAsset == null)
                textElement.uitkTextHandle.ConvertUssToNativeTextGenerationSettings();
        }

        if (k_IsMultiThreaded)
        {
            var jobHandle = textJob.Schedule(textJobDatas.Count, 1);
            mgc.AddMeshGenerationJob(jobHandle);
        }
        else
            for (int i = 0; i < textJobDatas.Count; i++)
                textJob.Execute(i);

        mgc.AddMeshGenerationCallback(m_AddDrawEntriesCallback, null, MeshGenerationCallbackType.Work, true);
        k_ATGTextJobMarker.End();
    }


    List<Texture2D> atlases = new List<Texture2D>();
    List<float> sdfScalesArray = new List<float>();
    List<NativeSlice<Vertex>> verticesArray = new List<NativeSlice<Vertex>>();
    List<NativeSlice<ushort>> indicesArray = new List<NativeSlice<ushort>>();
    List<GlyphRenderMode> renderModes = new List<GlyphRenderMode>();
    void AddDrawEntries(MeshGenerationContext mgc, object _)
    {
        foreach (var managedJobData in textJobDatas)
        {
            if (managedJobData.success)
            {
                var textInfo = managedJobData.textInfo;
                managedJobData.textElement.uitkTextHandle.ProcessMeshInfos(textInfo);
                managedJobData.textElement.uitkTextHandle.UpdateATGTextEventHandler();

                // Call Texture.Apply for all texture still dirty
                // There are no other place where we are calling this to export the texture to the gpu
                // for the ATG. This is as late as it could be right now.

                // I am putting it here as this will only be call if an ATG-text has been modified
                // and it will not be called when non-atg text only are modified
                // Trying to keep the codepath separated for now.

                // Finally, calling this once per text element is not optimal but the code underneath
                // should simply return if there is nothing to apply
                FontAsset.UpdateFontAssetsInUpdateQueue();

                mgc.GetTempMeshAllocator(out var alloc);
                ConvertMeshInfoToUIRVertex(textInfo.meshInfos, alloc, managedJobData.textElement, ref atlases, ref verticesArray, ref indicesArray, ref renderModes, ref sdfScalesArray);

                mgc.Begin(managedJobData.node.GetParentEntry(), managedJobData.textElement);

                mgc.meshGenerator.DrawText(verticesArray, indicesArray, atlases, renderModes, sdfScalesArray);
                managedJobData.textElement.OnGenerateTextOverNative(mgc);

                atlases.Clear();
                verticesArray.Clear();
                indicesArray.Clear();
                renderModes.Clear();
                sdfScalesArray.Clear();
                mgc.End();
            }

            managedJobData.Release();
        }

        // get ready for next batch
        hasPendingTextWork = false;
        textJobDatas.Clear();
        textJobDatasHandle.Free();
    }

    static void ConvertMeshInfoToUIRVertex(ATGMeshInfo[] meshInfos, TempMeshAllocator alloc, TextElement visualElement, ref List<Texture2D> atlases, ref List<NativeSlice<Vertex>> verticesArray, ref List<NativeSlice<ushort>> indicesArray, ref List<GlyphRenderMode> renderModes, ref List<float> sdfScales)
    {
        float inverseScale = 1.0f / visualElement.scaledPixelsPerPoint;

        for (int i = 0; i < meshInfos.Length; i++)
        {
            var meshInfo = meshInfos[i];
            //Debug.Assert((meshInfo.textElementInfos.Length & 0b11) == 0); // Quads only
            int verticesPerAlloc = (int)(UIRenderDevice.maxVerticesPerPage & ~3); // Round down to multiple of 4

            // If multiple colors are required(e.g., color tags are used), then ignore the dynamic-color hint
            // since we cannot store multiple colors for a given text element.
            bool hasMultipleColors = meshInfo.hasMultipleColors;
            if (hasMultipleColors)
                visualElement.renderChainData.flags |= RenderDataFlags.IsIgnoringDynamicColorHint;
            else
                visualElement.renderChainData.flags &= ~RenderDataFlags.IsIgnoringDynamicColorHint;

            for (int j = 0; j < meshInfo.textElementInfoIndicesByAtlas.Count; ++j)
            {
                var textElementInfoInAtlas = meshInfo.textElementInfoIndicesByAtlas[j];
                int remainingVertexCount = textElementInfoInAtlas.Count * 4;
                while (remainingVertexCount > 0)
                {
                    int vertexCount = Mathf.Min(remainingVertexCount, verticesPerAlloc);
                    int quadCount = vertexCount >> 2;
                    int indexCount = quadCount * 6;

                    var fa = meshInfo.fontAsset;
                    atlases.Add(fa.atlasTextures[j]);
                    renderModes.Add(fa.atlasRenderMode);

                    float sdfScale = 0;
                    if (!TextGeneratorUtilities.IsBitmapRendering(renderModes[^1]) && atlases[^1].format == TextureFormat.Alpha8)
                        sdfScale = fa.atlasPadding + 1;
                    sdfScales.Add(sdfScale);

                    bool hasGradientScale = fa.atlasRenderMode != GlyphRenderMode.SMOOTH && fa.atlasRenderMode != GlyphRenderMode.COLOR;
                    // TODO, update once ATG supports SpriteAssets
                    bool isDynamicColor = /*meshInfo.applySDF &&*/ !hasMultipleColors && (RenderEvents.NeedsColorID(visualElement) || (hasGradientScale && RenderEvents.NeedsTextCoreSettings(visualElement)));

                    alloc.AllocateTempMesh(vertexCount, indexCount, out var vertices, out var indices);

                    var pos = (visualElement).contentRect.min;
                    for (int vDst = 0,vSrc = 0, k = 0; vDst < vertexCount; vDst += 4, vSrc += 1, k += 6)
                    {
                        var isColorFont = fa.atlasRenderMode == GlyphRenderMode.COLOR || fa.atlasRenderMode == GlyphRenderMode.COLOR_HINTED;
                        var tei = meshInfo.textElementInfos[textElementInfoInAtlas[vSrc]];
                        vertices[vDst + 0] = MeshGenerator.ConvertTextVertexToUIRVertex(tei.bottomLeft, pos, isDynamicColor, isColorFont);
                        vertices[vDst + 1] = MeshGenerator.ConvertTextVertexToUIRVertex(tei.topLeft, pos, isDynamicColor, isColorFont);
                        vertices[vDst + 2] = MeshGenerator.ConvertTextVertexToUIRVertex(tei.topRight, pos, isDynamicColor, isColorFont);
                        vertices[vDst + 3] = MeshGenerator.ConvertTextVertexToUIRVertex(tei.bottomRight, pos, isDynamicColor, isColorFont);

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
