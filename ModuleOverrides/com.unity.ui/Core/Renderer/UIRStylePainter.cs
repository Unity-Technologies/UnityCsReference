// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine.Assertions;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements.UIR.Implementation
{
    internal class UIRStylePainter : IStylePainter, IDisposable
    {
        internal struct Entry
        {
            public NativeSlice<Vertex> vertices;
            public NativeSlice<UInt16> indices;
            public Material material; // Responsible for enabling immediate clipping
            public Texture custom, font;
            public float fontTexSDFScale;
            public TextureId texture;
            public RenderChainCommand customCommand;
            public BMPAlloc clipRectID;
            public VertexFlags addFlags;
            public bool uvIsDisplacement;
            public bool isTextEntry;
            public bool isClipRegisterEntry;
            public bool isStencilClipped;
        }

        internal struct ClosingInfo
        {
            public bool needsClosing;
            public bool popViewMatrix;
            public bool popScissorClip;
            public bool blitAndPopRenderTexture;
            public bool PopDefaultMaterial;
            public RenderChainCommand clipUnregisterDrawCommand;
            public NativeSlice<Vertex> clipperRegisterVertices;
            public NativeSlice<UInt16> clipperRegisterIndices;
            public int clipperRegisterIndexOffset;
            public bool RestoreStencilClip; // Used when blitAndPopRenderTexture as the clipping is not propagated through the other render texture
        }

        internal struct TempDataAlloc<T> : IDisposable where T : struct
        {
            int maxPoolElemCount; // Requests larger than this will potentially be served individually without pooling
            NativeArray<T> pool;
            List<NativeArray<T>> excess;
            uint takenFromPool;

            public TempDataAlloc(int maxPoolElems)
            {
                maxPoolElemCount = maxPoolElems;
                pool = new NativeArray<T>();
                excess = new List<NativeArray<T>>();
                takenFromPool = 0;
            }

            public void Dispose()
            {
                foreach (var e in excess)
                    e.Dispose();
                excess.Clear();
                if (pool.IsCreated)
                    pool.Dispose();
            }

            internal NativeSlice<T> Alloc(uint count)
            {
                if (takenFromPool + count <= pool.Length)
                {
                    NativeSlice<T> slice = pool.Slice((int)takenFromPool, (int)count);
                    takenFromPool += count;
                    return slice;
                }

                var exceeding = new NativeArray<T>((int)count, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                excess.Add(exceeding);
                return exceeding;
            }

            internal void SessionDone()
            {
                int totalNewSize = pool.Length;
                foreach (var e in excess)
                {
                    if (e.Length < maxPoolElemCount)
                        totalNewSize += e.Length;
                    e.Dispose();
                }
                excess.Clear();
                if (totalNewSize > pool.Length)
                {
                    if (pool.IsCreated)
                        pool.Dispose();
                    pool = new NativeArray<T>(totalNewSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                }
                takenFromPool = 0;
            }
        }

        RenderChain m_Owner;
        List<Entry> m_Entries = new List<Entry>();
        AtlasBase m_Atlas;
        VectorImageManager m_VectorImageManager;
        Entry m_CurrentEntry;
        ClosingInfo m_ClosingInfo;
        internal bool m_StencilClip = false;
        BMPAlloc m_ClipRectID = UIRVEShaderInfoAllocator.infiniteClipRect;
        int m_SVGBackgroundEntryIndex = -1;
        TempDataAlloc<Vertex> m_VertsPool = new TempDataAlloc<Vertex>(8192);
        TempDataAlloc<UInt16> m_IndicesPool = new TempDataAlloc<UInt16>(8192 << 1);
        List<MeshWriteData> m_MeshWriteDataPool;
        int m_NextMeshWriteDataPoolItem;

        // The delegates must be stored to avoid allocations
        MeshBuilder.AllocMeshData.Allocator m_AllocRawVertsIndicesDelegate;
        MeshBuilder.AllocMeshData.Allocator m_AllocThroughDrawMeshDelegate;
        MeshBuilder.AllocMeshData.Allocator m_AllocThroughDrawGradientsDelegate;

        MeshWriteData GetPooledMeshWriteData()
        {
            if (m_NextMeshWriteDataPoolItem == m_MeshWriteDataPool.Count)
                m_MeshWriteDataPool.Add(new MeshWriteData());
            return m_MeshWriteDataPool[m_NextMeshWriteDataPoolItem++];
        }

        MeshWriteData AllocRawVertsIndices(uint vertexCount, uint indexCount, ref MeshBuilder.AllocMeshData allocatorData)
        {
            m_CurrentEntry.vertices = m_VertsPool.Alloc(vertexCount);
            m_CurrentEntry.indices = m_IndicesPool.Alloc(indexCount);
            var mwd = GetPooledMeshWriteData();
            mwd.Reset(m_CurrentEntry.vertices, m_CurrentEntry.indices);
            return mwd;
        }

        MeshWriteData AllocThroughDrawMesh(uint vertexCount, uint indexCount, ref MeshBuilder.AllocMeshData allocatorData)
        {
            return DrawMesh((int)vertexCount, (int)indexCount, allocatorData.texture, allocatorData.material, allocatorData.flags);
        }

        MeshWriteData AllocThroughDrawGradients(uint vertexCount, uint indexCount, ref MeshBuilder.AllocMeshData allocatorData)
        {
            return AddGradientsEntry((int)vertexCount, (int)indexCount, allocatorData.svgTexture, allocatorData.material, allocatorData.flags);
        }

        public UIRStylePainter(RenderChain renderChain)
        {
            m_Owner = renderChain;
            meshGenerationContext = new MeshGenerationContext(this);
            device = renderChain.device;
            m_Atlas = renderChain.atlas;
            m_VectorImageManager = renderChain.vectorImageManager;
            m_AllocRawVertsIndicesDelegate = AllocRawVertsIndices;
            m_AllocThroughDrawMeshDelegate = AllocThroughDrawMesh;
            m_AllocThroughDrawGradientsDelegate = AllocThroughDrawGradients;
            int meshWriteDataPoolStartingSize = 32;
            m_MeshWriteDataPool = new List<MeshWriteData>(meshWriteDataPoolStartingSize);
            for (int i = 0; i < meshWriteDataPoolStartingSize; i++)
                m_MeshWriteDataPool.Add(new MeshWriteData());
        }

        public MeshGenerationContext meshGenerationContext { get; }
        public VisualElement currentElement { get; private set; }
        public UIRenderDevice device { get; }
        public List<Entry> entries { get { return m_Entries; } }
        public ClosingInfo closingInfo { get { return m_ClosingInfo; } }
        public int totalVertices { get; private set; }
        public int totalIndices { get; private set; }

        #region Dispose Pattern

        protected bool disposed { get; private set; }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                m_IndicesPool.Dispose();
                m_VertsPool.Dispose();
            }
            else
                UnityEngine.UIElements.DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern

        public void Begin(VisualElement ve)
        {
            currentElement = ve;
            m_NextMeshWriteDataPoolItem = 0;
            m_SVGBackgroundEntryIndex = -1;
            currentElement.renderChainData.usesLegacyText = currentElement.renderChainData.disableNudging = false;
            currentElement.renderChainData.displacementUVStart = currentElement.renderChainData.displacementUVEnd = 0;
            bool isGroupTransform = (currentElement.renderHints & RenderHints.GroupTransform) != 0;
            if (isGroupTransform)
            {
                var cmd = m_Owner.AllocCommand();
                cmd.owner = currentElement;
                cmd.type = CommandType.PushView;
                m_Entries.Add(new Entry() { customCommand = cmd });
                m_ClosingInfo.needsClosing = m_ClosingInfo.popViewMatrix = true;
            }
            if (currentElement.hierarchy.parent != null)
            {
                m_StencilClip = currentElement.hierarchy.parent.renderChainData.isStencilClipped;
                m_ClipRectID = isGroupTransform ? UIRVEShaderInfoAllocator.infiniteClipRect : currentElement.hierarchy.parent.renderChainData.clipRectID;
            }
            else
            {
                m_StencilClip = false;
                m_ClipRectID = UIRVEShaderInfoAllocator.infiniteClipRect;
            }

            if (ve.subRenderTargetMode != VisualElement.RenderTargetMode.None)
            {
                var cmd = m_Owner.AllocCommand();
                cmd.owner = currentElement;
                cmd.type = CommandType.PushRenderTexture;
                m_Entries.Add(new Entry() { customCommand = cmd });
                m_ClosingInfo.needsClosing = m_ClosingInfo.blitAndPopRenderTexture = true;
                m_ClosingInfo.RestoreStencilClip = m_StencilClip;
                m_StencilClip = false;
            }

            if (ve.defaultMaterial != null)
            {
                var cmd = m_Owner.AllocCommand();
                cmd.owner = currentElement;
                cmd.type = CommandType.PushDefaultMaterial;
                cmd.state.material = ve.defaultMaterial;
                m_Entries.Add(new Entry() { customCommand = cmd });
                m_ClosingInfo.needsClosing = m_ClosingInfo.PopDefaultMaterial = true;
            }
        }

        public void LandClipUnregisterMeshDrawCommand(RenderChainCommand cmd)
        {
            Debug.Assert(m_ClosingInfo.needsClosing);
            m_ClosingInfo.clipUnregisterDrawCommand = cmd;
        }

        public void LandClipRegisterMesh(NativeSlice<Vertex> vertices, NativeSlice<UInt16> indices, int indexOffset)
        {
            Debug.Assert(m_ClosingInfo.needsClosing);
            m_ClosingInfo.clipperRegisterVertices = vertices;
            m_ClosingInfo.clipperRegisterIndices = indices;
            m_ClosingInfo.clipperRegisterIndexOffset = indexOffset;
        }

        public MeshWriteData AddGradientsEntry(int vertexCount, int indexCount, TextureId texture, Material material, MeshGenerationContext.MeshFlags flags)
        {
            var mwd = GetPooledMeshWriteData();
            if (vertexCount == 0 || indexCount == 0)
            {
                mwd.Reset(new NativeSlice<Vertex>(), new NativeSlice<ushort>());
                return mwd;
            }

            m_CurrentEntry = new Entry()
            {
                vertices = m_VertsPool.Alloc((uint)vertexCount),
                indices = m_IndicesPool.Alloc((uint)indexCount),
                material = material,
                texture = texture,
                clipRectID = m_ClipRectID,
                isStencilClipped = m_StencilClip,
                addFlags = VertexFlags.IsSvgGradients
            };

            Debug.Assert(m_CurrentEntry.vertices.Length == vertexCount);
            Debug.Assert(m_CurrentEntry.indices.Length == indexCount);

            mwd.Reset(m_CurrentEntry.vertices, m_CurrentEntry.indices, new Rect(0, 0, 1, 1));
            m_Entries.Add(m_CurrentEntry);
            totalVertices += m_CurrentEntry.vertices.Length;
            totalIndices += m_CurrentEntry.indices.Length;
            m_CurrentEntry = new Entry();
            return mwd;
        }

        public MeshWriteData DrawMesh(int vertexCount, int indexCount, Texture texture, Material material, MeshGenerationContext.MeshFlags flags)
        {
            var mwd = GetPooledMeshWriteData();
            if (vertexCount == 0 || indexCount == 0)
            {
                mwd.Reset(new NativeSlice<Vertex>(), new NativeSlice<ushort>());
                return mwd;
            }

            m_CurrentEntry = new Entry()
            {
                vertices = m_VertsPool.Alloc((uint)vertexCount),
                indices = m_IndicesPool.Alloc((uint)indexCount),
                material = material,
                uvIsDisplacement = (flags & MeshGenerationContext.MeshFlags.UVisDisplacement) == MeshGenerationContext.MeshFlags.UVisDisplacement,
                clipRectID = m_ClipRectID,
                isStencilClipped = m_StencilClip,
                addFlags = VertexFlags.IsSolid
            };

            Debug.Assert(m_CurrentEntry.vertices.Length == vertexCount);
            Debug.Assert(m_CurrentEntry.indices.Length == indexCount);

            Rect uvRegion = new Rect(0, 0, 1, 1);
            if (texture != null)
            {
                // Attempt to override with an atlas.
                if (!((flags & MeshGenerationContext.MeshFlags.SkipDynamicAtlas) == MeshGenerationContext.MeshFlags.SkipDynamicAtlas) && m_Atlas != null && m_Atlas.TryGetAtlas(currentElement, texture as Texture2D, out TextureId atlas, out RectInt atlasRect))
                {
                    m_CurrentEntry.addFlags = VertexFlags.IsDynamic;
                    uvRegion = new Rect(atlasRect.x, atlasRect.y, atlasRect.width, atlasRect.height);
                    m_CurrentEntry.texture = atlas;
                    m_Owner.AppendTexture(currentElement, texture, atlas, true);
                }
                else
                {
                    TextureId id = TextureRegistry.instance.Acquire(texture);
                    m_CurrentEntry.addFlags = VertexFlags.IsTextured;
                    m_CurrentEntry.texture = id;
                    m_Owner.AppendTexture(currentElement, texture, id, false);
                }
            }

            mwd.Reset(m_CurrentEntry.vertices, m_CurrentEntry.indices, uvRegion);
            m_Entries.Add(m_CurrentEntry);
            totalVertices += m_CurrentEntry.vertices.Length;
            totalIndices += m_CurrentEntry.indices.Length;
            m_CurrentEntry = new Entry();
            return mwd;
        }

        public void DrawText(MeshGenerationContextUtils.TextParams textParams, ITextHandle handle, float pixelsPerPoint)
        {
            if (!TextUtilities.IsFontAssigned(textParams))
                return;

            if (currentElement.panel.contextType == ContextType.Editor)
                textParams.fontColor *= textParams.playmodeTintColor;

            if (handle.IsLegacy())
                DrawTextNative(textParams, pixelsPerPoint);
            else
                DrawTextCore(textParams, handle, pixelsPerPoint);
        }

        internal void DrawTextNative(MeshGenerationContextUtils.TextParams textParams, float pixelsPerPoint)
        {
            float scaling = TextUtilities.ComputeTextScaling(currentElement.worldTransform, pixelsPerPoint);
            TextNativeSettings textSettings = MeshGenerationContextUtils.TextParams.GetTextNativeSettings(textParams, scaling);

            Assert.IsNotNull(textSettings.font);

            using (NativeArray<TextVertex> textVertices = TextNative.GetVertices(textSettings))
            {
                if (textVertices.Length == 0)
                    return;

                Vector2 localOffset = TextNative.GetOffset(textSettings, textParams.rect);
                m_CurrentEntry.isTextEntry = true;
                m_CurrentEntry.clipRectID = m_ClipRectID;
                m_CurrentEntry.isStencilClipped = m_StencilClip;
                MeshBuilder.MakeText(textVertices, localOffset,  new MeshBuilder.AllocMeshData() { alloc = m_AllocRawVertsIndicesDelegate });
                m_CurrentEntry.font = textParams.font.material.mainTexture;
                m_Entries.Add(m_CurrentEntry);
                totalVertices += m_CurrentEntry.vertices.Length;
                totalIndices += m_CurrentEntry.indices.Length;
                m_CurrentEntry = new Entry();
                currentElement.renderChainData.usesLegacyText = true;
                currentElement.renderChainData.disableNudging = true;
            }
        }

        internal void DrawTextCore(MeshGenerationContextUtils.TextParams textParams, ITextHandle handle, float pixelsPerPoint)
        {
            TextInfo textInfo = handle.Update(textParams, pixelsPerPoint);
            for (int i = 0; i < textInfo.materialCount; i++)
            {
                if (textInfo.meshInfo[i].vertexCount == 0)
                    continue;

                if (textInfo.meshInfo[i].material.name.Contains("Sprite"))
                {
                    // Assume a sprite asset
                    m_CurrentEntry.clipRectID = m_ClipRectID;
                    m_CurrentEntry.isStencilClipped = m_StencilClip;

                    var texture = textInfo.meshInfo[i].material.mainTexture;
                    TextureId id = TextureRegistry.instance.Acquire(texture);
                    m_CurrentEntry.texture = id;
                    m_Owner.AppendTexture(currentElement, texture, id, false);

                    MeshBuilder.MakeText(
                        textInfo.meshInfo[i],
                        textParams.rect.min,
                        new MeshBuilder.AllocMeshData() { alloc = m_AllocRawVertsIndicesDelegate },
                        VertexFlags.IsTextured);
                }
                else
                {
                    m_CurrentEntry.isTextEntry = true;
                    m_CurrentEntry.clipRectID = m_ClipRectID;
                    m_CurrentEntry.isStencilClipped = m_StencilClip;
                    m_CurrentEntry.fontTexSDFScale = textInfo.meshInfo[i].material.GetFloat(TextShaderUtilities.ID_GradientScale);
                    m_CurrentEntry.font = textInfo.meshInfo[i].material.mainTexture;

                    MeshBuilder.MakeText(
                        textInfo.meshInfo[i],
                        textParams.rect.min,
                        new MeshBuilder.AllocMeshData() { alloc = m_AllocRawVertsIndicesDelegate });
                }
                m_Entries.Add(m_CurrentEntry);
                totalVertices += m_CurrentEntry.vertices.Length;
                totalIndices += m_CurrentEntry.indices.Length;
                m_CurrentEntry = new Entry();
            }
        }

        public void DrawRectangle(MeshGenerationContextUtils.RectangleParams rectParams)
        {
            if (rectParams.rect.width < Mathf.Epsilon || rectParams.rect.height < Mathf.Epsilon)
                return; // Nothing to draw

            if (currentElement.panel.contextType == ContextType.Editor)
                rectParams.color *= rectParams.playmodeTintColor;

            var meshAlloc = new MeshBuilder.AllocMeshData()
            {
                alloc = m_AllocThroughDrawMeshDelegate,
                texture = rectParams.texture,
                material = rectParams.material,
                flags = rectParams.meshFlags
            };

            if (rectParams.vectorImage != null)
                DrawVectorImage(rectParams);
            else if (rectParams.sprite != null)
                DrawSprite(rectParams);
            else if (rectParams.texture != null)
                MeshBuilder.MakeTexturedRect(rectParams, UIRUtility.k_MeshPosZ, meshAlloc);
            else
                MeshBuilder.MakeSolidRect(rectParams, UIRUtility.k_MeshPosZ, meshAlloc);
        }

        public void DrawBorder(MeshGenerationContextUtils.BorderParams borderParams)
        {
            if (currentElement.panel.contextType == ContextType.Editor)
            {
                borderParams.leftColor *= borderParams.playmodeTintColor;
                borderParams.topColor *= borderParams.playmodeTintColor;
                borderParams.rightColor *= borderParams.playmodeTintColor;
                borderParams.bottomColor *= borderParams.playmodeTintColor;
            }

            MeshBuilder.MakeBorder(borderParams, UIRUtility.k_MeshPosZ, new MeshBuilder.AllocMeshData()
            {
                alloc = m_AllocThroughDrawMeshDelegate,
                material = borderParams.material,
                texture = null,
                flags = MeshGenerationContext.MeshFlags.UVisDisplacement
            });
        }

        public void DrawImmediate(Action callback, bool cullingEnabled)
        {
            var cmd = m_Owner.AllocCommand();
            cmd.type = cullingEnabled ? CommandType.ImmediateCull : CommandType.Immediate;
            cmd.owner = currentElement;
            cmd.callback = callback;
            m_Entries.Add(new Entry() { customCommand = cmd });
        }

        public VisualElement visualElement { get { return currentElement; } }

        public void DrawVisualElementBackground()
        {
            if (currentElement.layout.width <= Mathf.Epsilon || currentElement.layout.height <= Mathf.Epsilon)
                return;

            var style = currentElement.computedStyle;
            if (style.backgroundColor != Color.clear)
            {
                // Draw solid color background
                var rectParams = new MeshGenerationContextUtils.RectangleParams
                {
                    rect = GUIUtility.AlignRectToDevice(currentElement.rect),
                    color = style.backgroundColor,
                    playmodeTintColor = currentElement.panel.contextType == ContextType.Editor ? UIElementsUtility.editorPlayModeTintColor : Color.white
                };
                MeshGenerationContextUtils.GetVisualElementRadii(currentElement,
                    out rectParams.topLeftRadius,
                    out rectParams.bottomLeftRadius,
                    out rectParams.topRightRadius,
                    out rectParams.bottomRightRadius);
                DrawRectangle(rectParams);
            }

            var slices = new Vector4(
                style.unitySliceLeft,
                style.unitySliceTop,
                style.unitySliceRight,
                style.unitySliceBottom);

            var radiusParams = new MeshGenerationContextUtils.RectangleParams();
            MeshGenerationContextUtils.GetVisualElementRadii(currentElement,
                out radiusParams.topLeftRadius,
                out radiusParams.bottomLeftRadius,
                out radiusParams.topRightRadius,
                out radiusParams.bottomRightRadius);

            var background = style.backgroundImage;
            if (background.texture != null || background.sprite != null || background.vectorImage != null || background.renderTexture != null)
            {
                // Draw background image (be it from a texture or a vector image)
                var rectParams = new MeshGenerationContextUtils.RectangleParams();

                if (background.texture != null)
                {
                    rectParams = MeshGenerationContextUtils.RectangleParams.MakeTextured(
                        GUIUtility.AlignRectToDevice(currentElement.rect),
                        new Rect(0, 0, 1, 1),
                        background.texture,
                        style.unityBackgroundScaleMode,
                        currentElement.panel.contextType);
                }
                else if (background.sprite != null)
                {
                    rectParams = MeshGenerationContextUtils.RectangleParams.MakeSprite(
                        GUIUtility.AlignRectToDevice(currentElement.rect),
                        background.sprite,
                        style.unityBackgroundScaleMode,
                        currentElement.panel.contextType,
                        radiusParams.HasRadius(Tessellation.kEpsilon),
                        ref slices);
                }
                else if (background.renderTexture != null)
                {
                    rectParams = MeshGenerationContextUtils.RectangleParams.MakeTextured(
                        GUIUtility.AlignRectToDevice(currentElement.rect),
                        new Rect(0, 0, 1, 1),
                        background.renderTexture,
                        style.unityBackgroundScaleMode,
                        currentElement.panel.contextType);
                }
                else if (background.vectorImage != null)
                {
                    rectParams = MeshGenerationContextUtils.RectangleParams.MakeVectorTextured(
                        GUIUtility.AlignRectToDevice(currentElement.rect),
                        new Rect(0, 0, 1, 1),
                        background.vectorImage,
                        style.unityBackgroundScaleMode,
                        currentElement.panel.contextType);
                }

                rectParams.topLeftRadius = radiusParams.topLeftRadius;
                rectParams.topRightRadius = radiusParams.topRightRadius;
                rectParams.bottomRightRadius = radiusParams.bottomRightRadius;
                rectParams.bottomLeftRadius = radiusParams.bottomLeftRadius;

                if (slices != Vector4.zero)
                {
                    rectParams.leftSlice = Mathf.RoundToInt(slices.x);
                    rectParams.topSlice = Mathf.RoundToInt(slices.y);
                    rectParams.rightSlice = Mathf.RoundToInt(slices.z);
                    rectParams.bottomSlice = Mathf.RoundToInt(slices.w);
                }

                if (style.unityBackgroundImageTintColor != Color.clear)
                    rectParams.color = style.unityBackgroundImageTintColor;
                DrawRectangle(rectParams);
            }
        }

        public void DrawVisualElementBorder()
        {
            if (currentElement.layout.width >= Mathf.Epsilon && currentElement.layout.height >= Mathf.Epsilon)
            {
                var style = currentElement.computedStyle;
                if (style.borderLeftColor != Color.clear && style.borderLeftWidth > 0.0f ||
                    style.borderTopColor != Color.clear && style.borderTopWidth > 0.0f ||
                    style.borderRightColor != Color.clear &&  style.borderRightWidth > 0.0f ||
                    style.borderBottomColor != Color.clear && style.borderBottomWidth > 0.0f)
                {
                    var borderParams = new MeshGenerationContextUtils.BorderParams
                    {
                        rect = GUIUtility.AlignRectToDevice(currentElement.rect),
                        leftColor = style.borderLeftColor,
                        topColor = style.borderTopColor,
                        rightColor = style.borderRightColor,
                        bottomColor = style.borderBottomColor,
                        leftWidth = style.borderLeftWidth,
                        topWidth = style.borderTopWidth,
                        rightWidth = style.borderRightWidth,
                        bottomWidth = style.borderBottomWidth,
                        playmodeTintColor = currentElement.panel.contextType == ContextType.Editor ? UIElementsUtility.editorPlayModeTintColor : Color.white
                    };
                    MeshGenerationContextUtils.GetVisualElementRadii(currentElement,
                        out borderParams.topLeftRadius,
                        out borderParams.bottomLeftRadius,
                        out borderParams.topRightRadius,
                        out borderParams.bottomRightRadius);
                    DrawBorder(borderParams);
                }
            }
        }

        public void ApplyVisualElementClipping()
        {
            if (currentElement.renderChainData.clipMethod == ClipMethod.Scissor)
            {
                var cmd = m_Owner.AllocCommand();
                cmd.type = CommandType.PushScissor;
                cmd.owner = currentElement;
                m_Entries.Add(new Entry() { customCommand = cmd });
                m_ClosingInfo.needsClosing = m_ClosingInfo.popScissorClip = true;
            }
            else if (currentElement.renderChainData.clipMethod == ClipMethod.Stencil)
            {
                if (UIRUtility.IsVectorImageBackground(currentElement))
                    GenerateStencilClipEntryForSVGBackground();
                else GenerateStencilClipEntryForRoundedRectBackground();
            }
            m_ClipRectID = currentElement.renderChainData.clipRectID;
        }

        private UInt16[] AdjustSpriteWinding(Sprite sprite)
        {
            var vertices = sprite.vertices;
            var indices = sprite.triangles;
            var newIndices = new UInt16[indices.Length];

            for (int i = 0; i < indices.Length; i += 3)
            {
                var v0 = (Vector3)vertices[indices[i]];
                var v1 = (Vector3)vertices[indices[i + 1]];
                var v2 = (Vector3)vertices[indices[i + 2]];

                var v = (v1 - v0).normalized;
                var w = (v2 - v0).normalized;
                var c = Vector3.Cross(v, w);
                if (c.z >= 0.0f)
                {
                    newIndices[i] = indices[i + 1];
                    newIndices[i + 1] = indices[i];
                    newIndices[i + 2] = indices[i + 2];
                }
                else
                {
                    newIndices[i] = indices[i];
                    newIndices[i + 1] = indices[i + 1];
                    newIndices[i + 2] = indices[i + 2];
                }
            }
            return newIndices;
        }

        public void DrawSprite(MeshGenerationContextUtils.RectangleParams rectParams)
        {
            var sprite = rectParams.sprite;
            System.Diagnostics.Debug.Assert(sprite != null);

            if (sprite.texture == null || sprite.triangles.Length == 0)
                return; // Textureless sprites not supported, should use VectorImage instead

            System.Diagnostics.Debug.Assert(sprite.border == Vector4.zero, "Sliced sprites should be rendered as regular textured rectangles");

            var meshAlloc = new MeshBuilder.AllocMeshData()
            {
                alloc = m_AllocThroughDrawMeshDelegate,
                texture = sprite.texture,
                flags = rectParams.meshFlags
            };

            // Remap vertices inside rect
            var bounds = sprite.bounds;
            var spriteMin = (Vector2)bounds.min;
            var spriteSize = (Vector2)bounds.size;

            var vertexCount = sprite.vertices.Length;
            var vertices = new Vertex[vertexCount];
            var indices = AdjustSpriteWinding(sprite);

            var mwd = meshAlloc.Allocate((uint)vertices.Length, (uint)indices.Length);
            var uvRegion = mwd.uvRegion;

            for (int i = 0; i < vertexCount; ++i)
            {
                var v = sprite.vertices[i];
                v -= rectParams.spriteGeomRect.position;
                v /= rectParams.spriteGeomRect.size;
                v.y = 1.0f - v.y;
                v *= rectParams.rect.size;
                v += rectParams.rect.position;

                var uv = sprite.uv[i];
                uv *= uvRegion.size;
                uv += uvRegion.position;

                vertices[i] = new Vertex() {
                    position = new Vector3(v.x, v.y, Vertex.nearZ),
                    tint = rectParams.color,
                    uv = uv
                };
            }

            mwd.SetAllVertices(vertices);
            mwd.SetAllIndices(indices);
        }

        public void DrawVectorImage(MeshGenerationContextUtils.RectangleParams rectParams)
        {
            var vi = rectParams.vectorImage;
            Debug.Assert(vi != null);

            int settingIndexOffset = 0;

            MeshBuilder.AllocMeshData meshAlloc = new MeshBuilder.AllocMeshData();
            if (vi.atlas != null && m_VectorImageManager != null)
            {
                // The vector image has embedded textures/gradients and we have a manager that can accept the settings.
                // Register the settings and assume that it works.
                var gradientRemap = m_VectorImageManager.AddUser(vi, currentElement);
                settingIndexOffset = gradientRemap.destIndex;
                if (gradientRemap.atlas != TextureId.invalid)
                    // The textures/gradients themselves have also been atlased.
                    meshAlloc.svgTexture = gradientRemap.atlas;
                else
                {
                    // Only the settings were atlased.
                    meshAlloc.svgTexture = TextureRegistry.instance.Acquire(vi.atlas);
                    m_Owner.AppendTexture(currentElement, vi.atlas, meshAlloc.svgTexture, false);
                }

                meshAlloc.alloc = m_AllocThroughDrawGradientsDelegate;
            }
            else
            {
                // The vector image is solid (no textures/gradients)
                meshAlloc.alloc = m_AllocThroughDrawMeshDelegate;
            }

            int entryCountBeforeSVG = m_Entries.Count;
            int finalVertexCount;
            int finalIndexCount;
            MeshBuilder.MakeVectorGraphics(rectParams, settingIndexOffset, meshAlloc, out finalVertexCount, out finalIndexCount);

            Debug.Assert(entryCountBeforeSVG <= m_Entries.Count + 1);
            if (entryCountBeforeSVG != m_Entries.Count)
            {
                m_SVGBackgroundEntryIndex = m_Entries.Count - 1;
                if (finalVertexCount != 0 && finalIndexCount != 0)
                {
                    var svgEntry = m_Entries[m_SVGBackgroundEntryIndex];
                    svgEntry.vertices = svgEntry.vertices.Slice(0, finalVertexCount);
                    svgEntry.indices = svgEntry.indices.Slice(0, finalIndexCount);
                    m_Entries[m_SVGBackgroundEntryIndex] = svgEntry;
                }
            }
        }

        internal void Reset()
        {
            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                return;
            }

            ValidateMeshWriteData();

            m_Entries.Clear(); // Doesn't shrink, good
            m_VertsPool.SessionDone();
            m_IndicesPool.SessionDone();
            m_ClosingInfo = new ClosingInfo();
            m_NextMeshWriteDataPoolItem = 0;
            currentElement = null;
            totalVertices = totalIndices = 0;
        }

        void ValidateMeshWriteData()
        {
            // Loop through the used MeshWriteData and make sure the number of indices/vertices were properly filled.
            // Otherwise, we may end up with garbage in the buffers which may cause glitches/driver crashes.
            for (int i = 0; i < m_NextMeshWriteDataPoolItem; ++i)
            {
                var mwd = m_MeshWriteDataPool[i];
                if (mwd.vertexCount > 0 && mwd.currentVertex < mwd.vertexCount)
                {
                    Debug.LogError("Not enough vertices written in generateVisualContent callback " +
                        "(asked for " + mwd.vertexCount + " but only wrote " + mwd.currentVertex + ")");
                    var v = mwd.m_Vertices[0]; // Duplicate the first vertex
                    while (mwd.currentVertex < mwd.vertexCount)
                        mwd.SetNextVertex(v);
                }
                if (mwd.indexCount > 0 && mwd.currentIndex < mwd.indexCount)
                {
                    Debug.LogError("Not enough indices written in generateVisualContent callback " +
                        "(asked for " + mwd.indexCount + " but only wrote " + mwd.currentIndex + ")");
                    while (mwd.currentIndex < mwd.indexCount)
                        mwd.SetNextIndex(0);
                }
            }
        }

        void GenerateStencilClipEntryForRoundedRectBackground()
        {
            if (currentElement.layout.width <= Mathf.Epsilon || currentElement.layout.height <= Mathf.Epsilon)
                return;

            var style = currentElement.computedStyle;
            Vector2 radTL, radTR, radBL, radBR;
            MeshGenerationContextUtils.GetVisualElementRadii(currentElement, out radTL, out radBL, out radTR, out radBR);
            float widthT = style.borderTopWidth;
            float widthL = style.borderLeftWidth;
            float widthB = style.borderBottomWidth;
            float widthR = style.borderRightWidth;

            var rp = new MeshGenerationContextUtils.RectangleParams()
            {
                rect = GUIUtility.AlignRectToDevice(currentElement.rect),
                color = Color.white,

                // Adjust the radius of the inner masking shape
                topLeftRadius = Vector2.Max(Vector2.zero, radTL - new Vector2(widthL, widthT)),
                topRightRadius = Vector2.Max(Vector2.zero, radTR - new Vector2(widthR, widthT)),
                bottomLeftRadius = Vector2.Max(Vector2.zero, radBL - new Vector2(widthL, widthB)),
                bottomRightRadius = Vector2.Max(Vector2.zero, radBR - new Vector2(widthR, widthB)),
                playmodeTintColor = currentElement.panel.contextType == ContextType.Editor ? UIElementsUtility.editorPlayModeTintColor : Color.white
            };

            // Only clip the interior shape, skipping the border
            rp.rect.x += widthL;
            rp.rect.y += widthT;
            rp.rect.width -= widthL + widthR;
            rp.rect.height -= widthT + widthB;

            // Skip padding, when requested
            if (style.unityOverflowClipBox == OverflowClipBox.ContentBox)
            {
                rp.rect.x += style.paddingLeft.value;
                rp.rect.y += style.paddingTop.value;
                rp.rect.width -= style.paddingLeft.value + style.paddingRight.value;
                rp.rect.height -= style.paddingTop.value + style.paddingBottom.value;
            }

            m_CurrentEntry.clipRectID = m_ClipRectID;
            m_CurrentEntry.isStencilClipped = m_StencilClip;
            m_CurrentEntry.isClipRegisterEntry = true;

            MeshBuilder.MakeSolidRect(rp, UIRUtility.k_MaskPosZ, new MeshBuilder.AllocMeshData() { alloc = m_AllocRawVertsIndicesDelegate });
            if (m_CurrentEntry.vertices.Length > 0 && m_CurrentEntry.indices.Length > 0)
            {
                m_Entries.Add(m_CurrentEntry);
                totalVertices += m_CurrentEntry.vertices.Length;
                totalIndices += m_CurrentEntry.indices.Length;
                m_StencilClip = true; // Draw operations following this one should be clipped if not already
                m_ClosingInfo.needsClosing = true;
            }
            m_CurrentEntry = new Entry();
        }

        void GenerateStencilClipEntryForSVGBackground()
        {
            if (m_SVGBackgroundEntryIndex == -1)
                return;

            var svgEntry = m_Entries[m_SVGBackgroundEntryIndex];

            Debug.Assert(svgEntry.vertices.Length > 0);
            Debug.Assert(svgEntry.indices.Length > 0);

            m_StencilClip = true; // Draw operations following this one should be clipped if not already
            m_CurrentEntry.vertices = svgEntry.vertices;
            m_CurrentEntry.indices = svgEntry.indices;
            m_CurrentEntry.uvIsDisplacement = svgEntry.uvIsDisplacement;
            m_CurrentEntry.clipRectID = m_ClipRectID;
            m_CurrentEntry.isStencilClipped = m_StencilClip;
            m_CurrentEntry.isClipRegisterEntry = true;
            m_ClosingInfo.needsClosing = true;

            // Adjust vertices for stencil clipping
            int vertexCount = m_CurrentEntry.vertices.Length;
            var clipVerts = m_VertsPool.Alloc((uint)vertexCount);
            for (int i = 0; i < vertexCount; i++)
            {
                Vertex v = m_CurrentEntry.vertices[i];
                v.position.z = UIRUtility.k_MaskPosZ;
                clipVerts[i] = v;
            }
            m_CurrentEntry.vertices = clipVerts;
            totalVertices += m_CurrentEntry.vertices.Length;
            totalIndices += m_CurrentEntry.indices.Length;

            m_Entries.Add(m_CurrentEntry);
            m_CurrentEntry = new Entry();
        }
    }

    internal class UIRTextUpdatePainter : IStylePainter, IDisposable
    {
        VisualElement m_CurrentElement;
        int m_TextEntryIndex;
        NativeArray<Vertex> m_DudVerts;
        NativeArray<UInt16> m_DudIndices;
        NativeSlice<Vertex> m_MeshDataVerts;
        Color32 m_XFormClipPages, m_IDs, m_Flags, m_OpacityPagesSettingsIndex;

        public MeshGenerationContext meshGenerationContext { get; }

        public UIRTextUpdatePainter()
        {
            meshGenerationContext = new MeshGenerationContext(this);
        }

        public void Begin(VisualElement ve, UIRenderDevice device)
        {
            Debug.Assert(ve.renderChainData.usesLegacyText && ve.renderChainData.textEntries.Count > 0);
            m_CurrentElement = ve;
            m_TextEntryIndex = 0;
            var oldVertexAlloc = ve.renderChainData.data.allocVerts;
            var oldVertexData = ve.renderChainData.data.allocPage.vertices.cpuData.Slice((int)oldVertexAlloc.start, (int)oldVertexAlloc.size);
            device.Update(ve.renderChainData.data, ve.renderChainData.data.allocVerts.size, out m_MeshDataVerts);
            RenderChainTextEntry firstTextEntry = ve.renderChainData.textEntries[0];
            if (ve.renderChainData.textEntries.Count > 1 || firstTextEntry.vertexCount != m_MeshDataVerts.Length)
                m_MeshDataVerts.CopyFrom(oldVertexData); // Preserve old data because we're not just updating the text vertices, but the entire mesh surrounding it though we won't touch but the text vertices

            // Case 1222517: Background and border are clipped by the parent, which implies that they may have a
            // different clip id when compared to the content, if overflow-clip-box is set to content-box. As a result,
            // we must NOT use the "first vertex" but rather the "first vertex of the first text entry".
            int first = firstTextEntry.firstVertex;
            m_XFormClipPages = oldVertexData[first].xformClipPages;
            m_IDs = oldVertexData[first].ids;
            m_Flags = oldVertexData[first].flags;
            m_OpacityPagesSettingsIndex = oldVertexData[first].opacityPageSettingIndex;
        }

        public void End()
        {
            Debug.Assert(m_TextEntryIndex == m_CurrentElement.renderChainData.textEntries.Count); // Or else element repaint logic diverged for some reason
            m_CurrentElement = null;
        }

        public void Dispose()
        {
            if (m_DudVerts.IsCreated)
                m_DudVerts.Dispose();
            if (m_DudIndices.IsCreated)
                m_DudIndices.Dispose();
        }

        public void DrawRectangle(MeshGenerationContextUtils.RectangleParams rectParams) {}
        public void DrawBorder(MeshGenerationContextUtils.BorderParams borderParams) {}
        public void DrawImmediate(Action callback, bool cullingEnabled) {}

        public VisualElement visualElement { get { return m_CurrentElement; } }

        public MeshWriteData DrawMesh(int vertexCount, int indexCount, Texture texture, Material material, MeshGenerationContext.MeshFlags flags)
        {
            // Ideally we should allow returning 0 here and the client would handle that properly
            if (m_DudVerts.Length < vertexCount)
            {
                if (m_DudVerts.IsCreated)
                    m_DudVerts.Dispose();
                m_DudVerts = new NativeArray<Vertex>(vertexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }
            if (m_DudIndices.Length < indexCount)
            {
                if (m_DudIndices.IsCreated)
                    m_DudIndices.Dispose();
                m_DudIndices = new NativeArray<UInt16>(indexCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }
            return new MeshWriteData() { m_Vertices = m_DudVerts.Slice(0, vertexCount), m_Indices = m_DudIndices.Slice(0, indexCount) };
        }

        public void DrawText(MeshGenerationContextUtils.TextParams textParams, ITextHandle handle, float pixelsPerPoint)
        {
            if (!TextUtilities.IsFontAssigned(textParams))
                return;

            if (m_CurrentElement.panel.contextType == ContextType.Editor)
                textParams.fontColor *= textParams.playmodeTintColor;

            float scaling = TextNative.ComputeTextScaling(m_CurrentElement.worldTransform, pixelsPerPoint);
            TextNativeSettings textSettings = MeshGenerationContextUtils.TextParams.GetTextNativeSettings(textParams, scaling);

            using (NativeArray<TextVertex> textVertices = TextNative.GetVertices(textSettings))
            {
                var textEntry = m_CurrentElement.renderChainData.textEntries[m_TextEntryIndex++];

                Vector2 localOffset = TextNative.GetOffset(textSettings, textParams.rect);
                MeshBuilder.UpdateText(textVertices, localOffset, m_CurrentElement.renderChainData.verticesSpace,
                    m_XFormClipPages, m_IDs, m_Flags, m_OpacityPagesSettingsIndex,
                    m_MeshDataVerts.Slice(textEntry.firstVertex, textEntry.vertexCount));
                textEntry.command.state.font = textParams.font.material.mainTexture;
            }
        }
    }
}
