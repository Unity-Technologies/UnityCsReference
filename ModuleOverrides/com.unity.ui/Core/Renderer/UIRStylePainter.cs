// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine.Assertions;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements.UIR
{
    internal class UIRStylePainter : IStylePainter
    {
        internal struct Entry
        {
            // In an entry, the winding order is ALWAYS clockwise (front-facing).
            // If needed, the winding order will be fixed when it's translated into a rendering command.
            // The vertices and indices are stored in temp cpu-only memory.
            public NativeSlice<Vertex> vertices;
            public NativeSlice<UInt16> indices;
            public Material material; // Responsible for enabling immediate clipping
            public float fontTexSDFScale;
            public TextureId texture;
            public RenderChainCommand customCommand;
            public BMPAlloc clipRectID;
            public VertexFlags addFlags;
            public bool uvIsDisplacement;
            public bool isTextEntry;
            public bool isClipRegisterEntry;

            // The stencil ref applies to the entry ONLY. For a given VisualElement, the value may differ between
            // the entries (e.g. background vs content if the element is a mask and changes the ref).
            public int stencilRef;
            // The mask depth should equal ref or ref+1. It determines the winding order of the resulting command:
            // stencilRef     => clockwise (front-facing)
            // stencilRef + 1 => counter-clockwise (back-facing)
            public int maskDepth;
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
            public int maskStencilRef; // What's the stencil ref value used before pushing/popping the mask?
        }

        struct RepeatRectUV
        {
            public Rect rect;
            public Rect uv;
        }

        RenderChain m_Owner;
        List<Entry> m_Entries = new List<Entry>();
        AtlasBase m_Atlas;
        VectorImageManager m_VectorImageManager;
        Entry m_CurrentEntry;
        ClosingInfo m_ClosingInfo;

        int m_MaskDepth;
        int m_StencilRef;

        BMPAlloc m_ClipRectID = UIRVEShaderInfoAllocator.infiniteClipRect;
        int m_SVGBackgroundEntryIndex = -1;
        TempAllocator<Vertex> m_VertsPool;
        TempAllocator<UInt16> m_IndicesPool;
        List<MeshWriteData> m_MeshWriteDataPool;
        int m_NextMeshWriteDataPoolItem;

        List<RepeatRectUV>[] m_RepeatRectUVList = null;

        // The delegates must be stored to avoid allocations
        MeshBuilder.AllocMeshData.Allocator m_AllocRawVertsIndicesDelegate;
        MeshBuilder.AllocMeshData.Allocator m_AllocThroughDrawMeshDelegate;

        MeshWriteData GetPooledMeshWriteData()
        {
            if (m_NextMeshWriteDataPoolItem == m_MeshWriteDataPool.Count)
                m_MeshWriteDataPool.Add(new MeshWriteData());
            return m_MeshWriteDataPool[m_NextMeshWriteDataPoolItem++];
        }

        MeshWriteData AllocRawVertsIndices(uint vertexCount, uint indexCount, ref MeshBuilder.AllocMeshData allocatorData)
        {
            m_CurrentEntry.vertices = m_VertsPool.Alloc((int)vertexCount);
            m_CurrentEntry.indices = m_IndicesPool.Alloc((int)indexCount);
            var mwd = GetPooledMeshWriteData();
            mwd.Reset(m_CurrentEntry.vertices, m_CurrentEntry.indices);
            return mwd;
        }

        MeshWriteData AllocThroughDrawMesh(uint vertexCount, uint indexCount, ref MeshBuilder.AllocMeshData allocatorData)
        {
            return DrawMesh((int)vertexCount, (int)indexCount, allocatorData.texture, allocatorData.material, allocatorData.flags);
        }

        public UIRStylePainter(RenderChain renderChain)
        {
            m_Owner = renderChain;
            meshGenerationContext = new MeshGenerationContext(this);
            m_Atlas = renderChain.atlas;
            m_VectorImageManager = renderChain.vectorImageManager;
            m_AllocRawVertsIndicesDelegate = AllocRawVertsIndices;
            m_AllocThroughDrawMeshDelegate = AllocThroughDrawMesh;
            int meshWriteDataPoolStartingSize = 32;
            m_MeshWriteDataPool = new List<MeshWriteData>(meshWriteDataPoolStartingSize);
            for (int i = 0; i < meshWriteDataPoolStartingSize; i++)
                m_MeshWriteDataPool.Add(new MeshWriteData());
            m_VertsPool = renderChain.vertsPool;
            m_IndicesPool = renderChain.indicesPool;
        }

        public MeshGenerationContext meshGenerationContext { get; }
        public VisualElement currentElement { get; private set; }
        public List<Entry> entries { get { return m_Entries; } }
        public ClosingInfo closingInfo { get { return m_ClosingInfo; } }
        public int totalVertices { get; private set; }
        public int totalIndices { get; private set; }

        public void Begin(VisualElement ve)
        {
            currentElement = ve;
            m_NextMeshWriteDataPoolItem = 0;
            m_SVGBackgroundEntryIndex = -1;
            currentElement.renderChainData.displacementUVStart = currentElement.renderChainData.displacementUVEnd = 0;

            m_MaskDepth = 0;
            m_StencilRef = 0;
            VisualElement parent = currentElement.hierarchy.parent;
            if (parent != null)
            {
                m_MaskDepth = parent.renderChainData.childrenMaskDepth;
                m_StencilRef = parent.renderChainData.childrenStencilRef;
            }

            bool isGroupTransform = (currentElement.renderHints & RenderHints.GroupTransform) != 0;
            if (isGroupTransform)
            {
                var cmd = m_Owner.AllocCommand();
                cmd.owner = currentElement;
                cmd.type = CommandType.PushView;
                m_Entries.Add(new Entry() { customCommand = cmd });
                m_ClosingInfo.needsClosing = m_ClosingInfo.popViewMatrix = true;
            }
            if (parent != null)
                m_ClipRectID = isGroupTransform ? UIRVEShaderInfoAllocator.infiniteClipRect : parent.renderChainData.clipRectID;
            else
                m_ClipRectID = UIRVEShaderInfoAllocator.infiniteClipRect;

            if (ve.subRenderTargetMode != VisualElement.RenderTargetMode.None)
            {
                var cmd = m_Owner.AllocCommand();
                cmd.owner = currentElement;
                cmd.type = CommandType.PushRenderTexture;
                m_Entries.Add(new Entry() { customCommand = cmd });
                m_ClosingInfo.needsClosing = m_ClosingInfo.blitAndPopRenderTexture = true;
                if (m_MaskDepth > 0 || m_StencilRef > 0)
                    Debug.LogError("The RenderTargetMode feature must not be used within a stencil mask.");
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

            if (meshGenerationContext.hasPainter2D)
                meshGenerationContext.painter2D.Reset(); // Reset vector API before client usage
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
                vertices = m_VertsPool.Alloc(vertexCount),
                indices = m_IndicesPool.Alloc(indexCount),
                material = material,
                texture = texture,
                clipRectID = m_ClipRectID,
                stencilRef = m_StencilRef,
                maskDepth = m_MaskDepth,
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
                vertices = m_VertsPool.Alloc(vertexCount),
                indices = m_IndicesPool.Alloc(indexCount),
                material = material,
                uvIsDisplacement = (flags & MeshGenerationContext.MeshFlags.UVisDisplacement) == MeshGenerationContext.MeshFlags.UVisDisplacement,
                clipRectID = m_ClipRectID,
                stencilRef = m_StencilRef,
                maskDepth = m_MaskDepth,
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

        internal void TryAtlasTexture(Texture texture, MeshGenerationContext.MeshFlags flags, out Rect outUVRegion, out bool outIsAtlas, out TextureId outTextureId, out VertexFlags outAddFlags)
        {
            outUVRegion = new Rect(0, 0, 1, 1);
            outIsAtlas = false;
            outTextureId = new TextureId();
            outAddFlags = VertexFlags.IsSolid;

            if (texture == null)
                return;

            bool skipDynamicAtlas = (flags & MeshGenerationContext.MeshFlags.SkipDynamicAtlas) == MeshGenerationContext.MeshFlags.SkipDynamicAtlas;

            // Attempt to override with an atlas.
            if (!skipDynamicAtlas && m_Atlas != null && m_Atlas.TryGetAtlas(currentElement, texture as Texture2D, out TextureId atlas, out RectInt atlasRect))
            {
                outAddFlags = VertexFlags.IsDynamic;
                outUVRegion = new Rect(atlasRect.x, atlasRect.y, atlasRect.width, atlasRect.height);
                outIsAtlas = true;
                outTextureId = atlas;
            }
            else
            {
                outAddFlags = VertexFlags.IsTextured;
                outTextureId = TextureRegistry.instance.Acquire(texture);
            }
        }

        internal void BuildEntryFromNativeMesh(MeshWriteDataInterface meshData, Texture texture, TextureId textureId, bool isAtlas, Material material, MeshGenerationContext.MeshFlags flags, Rect uvRegion, VertexFlags addFlags)
        {
            if (meshData.vertexCount == 0 || meshData.indexCount == 0)
                return;
            NativeSlice<Vertex> vertices;
            NativeSlice<UInt16> indices;
            unsafe
            {
                vertices = UIRenderDevice.PtrToSlice<Vertex>((void*)meshData.vertices, meshData.vertexCount);
                indices = UIRenderDevice.PtrToSlice<UInt16>((void*)meshData.indices, meshData.indexCount);
            }
            if (vertices.Length == 0 || indices.Length == 0)
                return;
            m_CurrentEntry = new Entry()
            {
                vertices = m_VertsPool.Alloc(vertices.Length),
                indices = m_IndicesPool.Alloc(indices.Length),
                material = material,
                uvIsDisplacement = (flags & MeshGenerationContext.MeshFlags.UVisDisplacement) == MeshGenerationContext.MeshFlags.UVisDisplacement,
                clipRectID = m_ClipRectID,
                stencilRef = m_StencilRef,
                maskDepth = m_MaskDepth,
                addFlags = VertexFlags.IsSolid
            };
            if (textureId.index >= 0)
            {
                m_CurrentEntry.addFlags = addFlags;
                m_CurrentEntry.texture = textureId;
                m_Owner.AppendTexture(currentElement, texture, textureId, isAtlas);
            }
            Debug.Assert(m_CurrentEntry.vertices.Length == vertices.Length);
            Debug.Assert(m_CurrentEntry.indices.Length == indices.Length);
            m_CurrentEntry.vertices.CopyFrom(vertices);
            m_CurrentEntry.indices.CopyFrom(indices);
            m_Entries.Add(m_CurrentEntry);
            totalVertices += m_CurrentEntry.vertices.Length;
            totalIndices += m_CurrentEntry.indices.Length;
            m_CurrentEntry = new Entry();
        }

        internal void BuildGradientEntryFromNativeMesh(MeshWriteDataInterface meshData, TextureId svgTextureId)
        {
            if (meshData.vertexCount == 0 || meshData.indexCount == 0)
                return;
            NativeSlice<Vertex> vertices;
            NativeSlice<UInt16> indices;
            unsafe
            {
                vertices = UIRenderDevice.PtrToSlice<Vertex>((void*)meshData.vertices, meshData.vertexCount);
                indices = UIRenderDevice.PtrToSlice<UInt16>((void*)meshData.indices, meshData.indexCount);
            }
            if (vertices.Length == 0 || indices.Length == 0)
                return;
            m_CurrentEntry = new Entry()
            {
                vertices = m_VertsPool.Alloc(vertices.Length),
                indices = m_IndicesPool.Alloc(indices.Length),
                texture = svgTextureId,
                clipRectID = m_ClipRectID,
                stencilRef = m_StencilRef,
                maskDepth = m_MaskDepth,
                addFlags = VertexFlags.IsSvgGradients
            };
            Debug.Assert(m_CurrentEntry.vertices.Length == vertices.Length);
            Debug.Assert(m_CurrentEntry.indices.Length == indices.Length);
            m_CurrentEntry.vertices.CopyFrom(vertices);
            m_CurrentEntry.indices.CopyFrom(indices);
            m_Entries.Add(m_CurrentEntry);
            totalVertices += m_CurrentEntry.vertices.Length;
            totalIndices += m_CurrentEntry.indices.Length;
            m_CurrentEntry = new Entry();
        }

        public void BuildRawEntryFromNativeMesh(MeshWriteDataInterface meshData)
        {
            if (meshData.vertexCount == 0 || meshData.indexCount == 0)
                return;
            NativeSlice<Vertex> vertices;
            NativeSlice<UInt16> indices;
            unsafe
            {
                vertices = UIRenderDevice.PtrToSlice<Vertex>((void*)meshData.vertices, meshData.vertexCount);
                indices = UIRenderDevice.PtrToSlice<UInt16>((void*)meshData.indices, meshData.indexCount);
            }
            if (vertices.Length == 0 || indices.Length == 0)
                return;
            m_CurrentEntry.vertices = m_VertsPool.Alloc((int)meshData.vertexCount);
            m_CurrentEntry.indices = m_IndicesPool.Alloc((int)meshData.indexCount);
            m_CurrentEntry.vertices.CopyFrom(vertices);

            m_CurrentEntry.indices.CopyFrom(indices);
        }

        public void DrawText(TextInfo textInfo, Vector2 offset)
        {
            DrawTextInfo(textInfo, offset, true);
        }

        private TextCore.Text.TextInfo m_TextInfo = new TextCore.Text.TextInfo();

        public void DrawText(string text, Vector2 pos, float fontSize, Color color, FontAsset font)
        {
            var textSettings = TextUtilities.GetTextSettingsFrom(currentElement);

            m_TextInfo.Clear();
            var textGenerationSettings = new TextCore.Text.TextGenerationSettings() {
                text = text,
                screenRect = Rect.zero,
                fontAsset = font,
                textSettings = textSettings,
                fontSize = fontSize,
                color = color,
                material = font.material,
                inverseYAxis = true
            };
            TextCore.Text.TextGenerator.GenerateText(textGenerationSettings, m_TextInfo);

            DrawTextInfo(m_TextInfo, pos, false);
        }

        private void DrawTextInfo(TextCore.Text.TextInfo textInfo, Vector2 offset, bool useHints)
        {
            for (int i = 0; i < textInfo.materialCount; i++)
            {
                if (textInfo.meshInfo[i].vertexCount == 0)
                    continue;

                m_CurrentEntry.clipRectID = m_ClipRectID;
                m_CurrentEntry.stencilRef = m_StencilRef;
                m_CurrentEntry.maskDepth = m_MaskDepth;

                // It will need to be updated once we support BitMap font.
                // Alternatively we could look at the MainText texture format (RGBA vs 8bit Alpha)
                if (!textInfo.meshInfo[i].material.HasProperty(TextShaderUtilities.ID_GradientScale))
                {
                    // Assume a sprite asset
                    var texture = textInfo.meshInfo[i].material.mainTexture;
                    TextureId id = TextureRegistry.instance.Acquire(texture);
                    m_CurrentEntry.texture = id;
                    m_Owner.AppendTexture(currentElement, texture, id, false);

                    MeshBuilder.MakeText(
                        textInfo.meshInfo[i],
                        offset,
                        new MeshBuilder.AllocMeshData() { alloc = m_AllocRawVertsIndicesDelegate },
                        VertexFlags.IsTextured);
                }
                else
                {
                    var texture = textInfo.meshInfo[i].material.mainTexture;
                    var sdfScale = textInfo.meshInfo[i].material.GetFloat(TextShaderUtilities.ID_GradientScale);

                    m_CurrentEntry.isTextEntry = true;
                    m_CurrentEntry.fontTexSDFScale = sdfScale;
                    m_CurrentEntry.texture = TextureRegistry.instance.Acquire(texture);
                    m_Owner.AppendTexture(currentElement, texture, m_CurrentEntry.texture, false);

                    bool isDynamicColor = useHints && RenderEvents.NeedsColorID(currentElement);
                    // Set the dynamic-color hint on TextCore fancy-text or the EditorUIE shader applies the
                    // tint over the fragment output, affecting the outline/shadows.
                    if (useHints)
                        isDynamicColor = isDynamicColor || RenderEvents.NeedsTextCoreSettings(currentElement);

                    MeshBuilder.MakeText(
                        textInfo.meshInfo[i],
                        offset,
                        new MeshBuilder.AllocMeshData() { alloc = m_AllocRawVertsIndicesDelegate },
                        VertexFlags.IsText,
                        isDynamicColor);
                }
                m_Entries.Add(m_CurrentEntry);
                totalVertices += m_CurrentEntry.vertices.Length;
                totalIndices += m_CurrentEntry.indices.Length;
                m_CurrentEntry = new Entry();
            }
        }

        public void DrawRectangle(MeshGenerationContextUtils.RectangleParams rectParams)
        {
            if (rectParams.rect.width < UIRUtility.k_Epsilon || rectParams.rect.height < UIRUtility.k_Epsilon)
                return; // Nothing to draw

            if (currentElement.panel.contextType == ContextType.Editor)
                rectParams.color *= rectParams.playmodeTintColor;

            if (rectParams.vectorImage != null)
                DrawVectorImage(rectParams);
            else if (rectParams.sprite != null)
                DrawSprite(rectParams);
            else
            {
                Rect uvRegion;
                bool isAtlas;
                TextureId textureId;
                VertexFlags addFlags;
                TryAtlasTexture(rectParams.texture, rectParams.meshFlags, out uvRegion, out isAtlas, out textureId, out addFlags);

                MeshWriteDataInterface meshData;
                if (rectParams.texture != null)
                    meshData = MeshBuilderNative.MakeTexturedRect(rectParams.ToNativeParams(uvRegion), UIRUtility.k_MeshPosZ);
                else
                    meshData = MeshBuilderNative.MakeSolidRect(rectParams.ToNativeParams(uvRegion), UIRUtility.k_MeshPosZ);

                BuildEntryFromNativeMesh(meshData, rectParams.texture, textureId, isAtlas, rectParams.material, rectParams.meshFlags, uvRegion, addFlags);
            }
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

            var meshData = MeshBuilderNative.MakeBorder(borderParams.ToNativeParams(), UIRUtility.k_MeshPosZ);
            BuildEntryFromNativeMesh(meshData, null, new TextureId(), false, null, MeshGenerationContext.MeshFlags.None, new Rect(0,0,1,1), VertexFlags.IsSolid);
        }

        public void DrawImmediate(Action callback, bool cullingEnabled)
        {
            var cmd = m_Owner.AllocCommand();
            cmd.type = cullingEnabled ? CommandType.ImmediateCull : CommandType.Immediate;
            cmd.owner = currentElement;
            cmd.callback = callback;
            m_Entries.Add(new Entry() { customCommand = cmd });
        }

        public void DrawVectorImage(VectorImage vectorImage, Vector2 offset, Angle rotationAngle, Vector2 scale)
        {
            if (vectorImage == null)
                return;

            int settingIndexOffset = 0;
            var svgTexture = new TextureId();
            MeshWriteData mwd;

            bool hasGradients = vectorImage.atlas != null;
            if (hasGradients)
            {
                RegisterVectorImageGradient(vectorImage, out settingIndexOffset, out svgTexture);
                mwd = AddGradientsEntry(vectorImage.vertices.Length, vectorImage.indices.Length, svgTexture, null, MeshGenerationContext.MeshFlags.None);
            }
            else
            {
                mwd = DrawMesh(vectorImage.vertices.Length, vectorImage.indices.Length, null, null, MeshGenerationContext.MeshFlags.None);
            }

            var matrix = Matrix4x4.TRS(offset, Quaternion.AngleAxis(rotationAngle.ToDegrees(), Vector3.forward), new Vector3(scale.x, scale.y, 1.0f));
            bool flipWinding = (scale.x < 0.0f) ^ (scale.y < 0.0f);

            int vertexCount = vectorImage.vertices.Length;
            for (int i = 0; i < vertexCount; ++i)
            {
                var v = vectorImage.vertices[i];
                var flags = v.flags;
                var p = matrix.MultiplyPoint3x4(v.position);
                p.z = Vertex.nearZ;

                uint settingIndex = (uint)(v.settingIndex + settingIndexOffset);
                var opc = new Color32(0, 0, (byte)(settingIndex >> 8), (byte)settingIndex);

                mwd.SetNextVertex(new Vertex() { position = p, tint = v.tint, uv = v.uv, opacityColorPages = opc, flags = v.flags, circle = v.circle });
            }

            if (!flipWinding)
                mwd.SetAllIndices(vectorImage.indices);
            else
            {
                var inds = vectorImage.indices;
                for (int i = 0; i < inds.Length; i +=3)
                {
                    mwd.SetNextIndex(inds[i]);
                    mwd.SetNextIndex(inds[i+2]);
                    mwd.SetNextIndex(inds[i+1]);
                }
            }
        }

        public VisualElement visualElement { get { return currentElement; } }

        public void DrawVisualElementBackground()
        {
            if (currentElement.layout.width <= UIRUtility.k_Epsilon || currentElement.layout.height <= UIRUtility.k_Epsilon)
                return;

            var style = currentElement.computedStyle;
            if (style.backgroundColor != Color.clear)
            {
                // Draw solid color background
                var rectParams = new MeshGenerationContextUtils.RectangleParams
                {
                    rect = currentElement.rect,
                    color = style.backgroundColor,
                    colorPage = ColorPage.Init(m_Owner, currentElement.renderChainData.backgroundColorID),
                    playmodeTintColor = currentElement.panel.contextType == ContextType.Editor ? UIElementsUtility.editorPlayModeTintColor : Color.white
                };
                MeshGenerationContextUtils.GetVisualElementRadii(currentElement,
                    out rectParams.topLeftRadius,
                    out rectParams.bottomLeftRadius,
                    out rectParams.topRightRadius,
                    out rectParams.bottomRightRadius);

                MeshGenerationContextUtils.AdjustBackgroundSizeForBorders(currentElement, ref rectParams.rect);

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
                float sliceScale = visualElement.resolvedStyle.unitySliceScale;

                if (background.texture != null)
                {
                    rectParams = MeshGenerationContextUtils.RectangleParams.MakeTextured(
                        currentElement.rect,
                        new Rect(0, 0, 1, 1),
                        background.texture,
                        ScaleMode.ScaleToFit,
                        currentElement.panel.contextType);

                    rectParams.rect = new Rect(0, 0, rectParams.texture.width, rectParams.texture.height);
                }
                else if (background.sprite != null)
                {
                    rectParams = MeshGenerationContextUtils.RectangleParams.MakeSprite(
                        currentElement.rect,
                        background.sprite,
                        ScaleMode.StretchToFill,
                        currentElement.panel.contextType,
                        radiusParams.HasRadius(MeshBuilderNative.kEpsilon),
                        ref slices,
                        true);

                    rectParams.rect = new Rect(0, 0, background.sprite.rect.width, background.sprite.rect.height);

                    sliceScale *= UIElementsUtility.PixelsPerUnitScaleForElement(visualElement, background.sprite);
                }
                else if (background.renderTexture != null)
                {
                    rectParams = MeshGenerationContextUtils.RectangleParams.MakeTextured(
                        currentElement.rect,
                        new Rect(0, 0, 1, 1),
                        background.renderTexture,
                        ScaleMode.ScaleToFit,
                        currentElement.panel.contextType);

                    rectParams.rect = new Rect(0, 0, rectParams.texture.width, rectParams.texture.height);

                }
                else if (background.vectorImage != null)
                {
                    rectParams = MeshGenerationContextUtils.RectangleParams.MakeVectorTextured(
                        currentElement.rect,
                        new Rect(0, 0, 1, 1),
                        background.vectorImage,
                        ScaleMode.ScaleToFit,
                        currentElement.panel.contextType);

                    rectParams.rect = new Rect(0, 0, rectParams.vectorImage.size.x, rectParams.vectorImage.size.y);
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

                    rectParams.sliceScale = sliceScale;
                }

                rectParams.color = style.unityBackgroundImageTintColor;
                rectParams.colorPage = ColorPage.Init(m_Owner, currentElement.renderChainData.tintColorID);
                rectParams.backgroundPositionX = style.backgroundPositionX;
                rectParams.backgroundPositionY = style.backgroundPositionY;
                rectParams.backgroundRepeat = style.backgroundRepeat;
                rectParams.backgroundSize = style.backgroundSize;

                MeshGenerationContextUtils.AdjustBackgroundSizeForBorders(currentElement, ref rectParams.rect);

                if (rectParams.texture != null)
                {
                    DrawRectangleRepeat(rectParams, currentElement.rect);
                }
                else if (rectParams.vectorImage != null)
                {
                    DrawRectangleRepeat(rectParams, currentElement.rect);
                }
                else
                {
                    DrawRectangle(rectParams);
                }
            }
        }

        private void DrawRectangleRepeat(MeshGenerationContextUtils.RectangleParams rectParams, Rect totalRect)
        {
            var uv = new Rect(0, 0, 1, 1);
            var targetRect = rectParams.rect;

            if (m_RepeatRectUVList == null)
            {
                m_RepeatRectUVList = new List<RepeatRectUV>[2];
                m_RepeatRectUVList[0] = new List<RepeatRectUV>();
                m_RepeatRectUVList[1] = new List<RepeatRectUV>();
            }
            else
            {
                m_RepeatRectUVList[0].Clear();
                m_RepeatRectUVList[1].Clear();
            }

            if (rectParams.backgroundSize.sizeType != BackgroundSizeType.Length)
            {
                if (rectParams.backgroundSize.sizeType == BackgroundSizeType.Contain)
                {
                    float ratioX = totalRect.width / targetRect.width;
                    float ratioY = totalRect.height / targetRect.height;

                    Rect rect = targetRect;
                    if (ratioX < ratioY)
                    {
                        rect.width = totalRect.width;
                        rect.height = targetRect.height * totalRect.width / targetRect.width;
                    }
                    else
                    {
                        rect.width = targetRect.width * totalRect.height / targetRect.height;
                        rect.height = totalRect.height;
                    }

                    targetRect = rect;
                }
                else if (rectParams.backgroundSize.sizeType == BackgroundSizeType.Cover)
                {
                    float ratioX = totalRect.width / targetRect.width;
                    float ratioY = totalRect.height / targetRect.height;

                    Rect rect = targetRect;
                    if (ratioX > ratioY)
                    {
                        rect.width = totalRect.width;
                        rect.height = targetRect.height * totalRect.width / targetRect.width;
                    }
                    else
                    {
                        rect.width = targetRect.width * totalRect.height / targetRect.height;
                        rect.height = totalRect.height;
                    }

                    targetRect = rect;
                }
            }
            else
            {
                if (!rectParams.backgroundSize.x.IsNone() || !rectParams.backgroundSize.y.IsNone())
                {
                    if ((!rectParams.backgroundSize.x.IsNone()) && (rectParams.backgroundSize.y.IsAuto()))
                    {
                        Rect rect = targetRect;
                        if (rectParams.backgroundSize.x.unit == LengthUnit.Percent)
                        {
                            rect.width = totalRect.width * rectParams.backgroundSize.x.value / 100.0f;
                            rect.height = rect.width * targetRect.height / targetRect.width;
                        }
                        else if (rectParams.backgroundSize.x.unit == LengthUnit.Pixel)
                        {
                            rect.width = rectParams.backgroundSize.x.value;
                            rect.height = rect.width * targetRect.height / targetRect.width;
                        }
                        targetRect = rect;
                    }
                    else if ((!rectParams.backgroundSize.x.IsNone()) && (!rectParams.backgroundSize.y.IsNone()))
                    {
                        Rect rect = targetRect;
                        if (!rectParams.backgroundSize.x.IsAuto())
                        {
                            if (rectParams.backgroundSize.x.unit == LengthUnit.Percent)
                            {
                                rect.width = totalRect.width * rectParams.backgroundSize.x.value / 100.0f;
                            }
                            else if (rectParams.backgroundSize.x.unit == LengthUnit.Pixel)
                            {
                                rect.width = rectParams.backgroundSize.x.value;
                            }
                        }

                        if (!rectParams.backgroundSize.y.IsAuto())
                        {
                            if (rectParams.backgroundSize.y.unit == LengthUnit.Percent)
                            {
                                rect.height = totalRect.height * rectParams.backgroundSize.y.value / 100.0f;
                            }
                            else if (rectParams.backgroundSize.y.unit == LengthUnit.Pixel)
                            {
                                rect.height = rectParams.backgroundSize.y.value;
                            }

                            if (rectParams.backgroundSize.x.IsAuto())
                            {
                                rect.width = rect.height * targetRect.width / targetRect.height;
                            }
                        }
                        targetRect = rect;
                    }
                }
            }

            // Skip invalid size
            if ((targetRect.size.x <= UIRUtility.k_Epsilon) || (targetRect.size.y <= UIRUtility.k_Epsilon))
            {
                return;
            }

            // Skip empty background
            if ((totalRect.size.x <= UIRUtility.k_Epsilon) || (totalRect.size.y <= UIRUtility.k_Epsilon))
            {
                return;
            }

            // Adjust size when background-repeat is round and other axis background-size is auto
            if ((rectParams.backgroundSize.x.IsAuto()) && (rectParams.backgroundRepeat.y == Repeat.Round))
            {
                int count = (int)((totalRect.size[1] + targetRect.size[1] * 0.5f) / targetRect.size[1]);
                count = Math.Max(count, 1);

                float new_size = (totalRect.size[1] / count);
                Rect rect = new Rect();
                rect.height = new_size;
                rect.width = rect.height * targetRect.width / targetRect.height;
                targetRect = rect;
            }
            else if ((rectParams.backgroundSize.y.IsAuto()) && (rectParams.backgroundRepeat.x == Repeat.Round))
            {
                int count = (int)((totalRect.size[0] + targetRect.size[0] * 0.5f) / targetRect.size[0]);
                count = Math.Max(count, 1);

                float new_size = (totalRect.size[0] / count);
                Rect rect = new Rect();
                rect.width = new_size;
                rect.height = rect.width * targetRect.height / targetRect.width;
                targetRect = rect;
            }

            for (int axis = 0; axis < 2; ++axis)
            {
                Repeat repeat = (axis == 0) ? rectParams.backgroundRepeat.x : rectParams.backgroundRepeat.y;

                BackgroundPosition backgroundPosition = (axis == 0) ? rectParams.backgroundPositionX : rectParams.backgroundPositionY;

                float linear_size = 0;
                if (repeat == Repeat.NoRepeat)
                {
                    RepeatRectUV repeatRectUV;
                    Rect rect = targetRect;

                    repeatRectUV.uv = uv;
                    repeatRectUV.rect = rect;
                    linear_size = rect.size[axis];
                    m_RepeatRectUVList[axis].Add(repeatRectUV);
                }
                else if (repeat == Repeat.Repeat)
                {
                    Rect rect = targetRect;

                    int count = (int)(totalRect.size[axis] / targetRect.size[axis]);

                    if (backgroundPosition.keyword == BackgroundPositionKeyword.Center)
                    {
                        if ((count % 2) == 1)
                        {
                            count += 2;
                        }
                        else
                        {
                            count++;
                        }
                    }
                    else
                    {
                        count += 2;
                    }

                    for (int i = 0; i < count; ++i)
                    {
                        Vector2 r = rect.position;
                        r[axis] = (i * targetRect.size[axis]);
                        rect.position = r;

                        RepeatRectUV s;
                        s.rect = rect;
                        s.uv = uv;

                        linear_size += s.rect.size[axis];

                        m_RepeatRectUVList[axis].Add(s);
                    }
                }
                else if (repeat == Repeat.Space)
                {
                    Rect rect = targetRect;

                    int count = (int)(totalRect.size[axis] / targetRect.size[axis]);

                    if (count >= 0)
                    {
                        RepeatRectUV s;
                        s.rect = rect;
                        s.uv = uv;
                        m_RepeatRectUVList[axis].Add(s);
                        linear_size = targetRect.size[axis];
                    }

                    if (count >= 2)
                    {
                        RepeatRectUV s;

                        Vector2 r = rect.position;
                        r[axis] = totalRect.size[axis] - targetRect.size[axis];
                        rect.position = r;

                        s.rect = rect;
                        s.uv = uv;

                        m_RepeatRectUVList[axis].Add(s);
                        linear_size = totalRect.size[axis];
                    }

                    if (count > 2)
                    {
                        float spaceOffset = (totalRect.size[axis] - targetRect.size[axis] * count) / (count - 1);

                        for (int i = 0; i < (count - 2); ++i)
                        {
                            RepeatRectUV s;
                            Vector2 r = rect.position;
                            r[axis] = (targetRect.size[axis] + spaceOffset) * (1 + i);
                            rect.position = r;

                            s.rect = rect;
                            s.uv = uv;

                            m_RepeatRectUVList[axis].Add(s);
                        }
                    }
                }
                else if (repeat == Repeat.Round)
                {
                    int count = (int)((totalRect.size[axis] + targetRect.size[axis] * 0.5f) / targetRect.size[axis]);
                    count = Math.Max(count, 1);

                    float new_size = (totalRect.size[axis] / count);

                    if (backgroundPosition.keyword == BackgroundPositionKeyword.Center)
                    {
                        if ((count % 2) == 1)
                        {
                            count += 2;
                        }
                        else
                        {
                            count++;
                        }
                    }
                    else
                    {
                        count++;
                    }

                    Rect rect = targetRect;
                    Vector2 d = rect.size;

                    d[axis] = new_size;
                    rect.size = d;

                    for (int i = 0; i < count; ++i)
                    {
                        RepeatRectUV s;
                        Vector2 r = rect.position;
                        r[axis] = new_size * i;
                        rect.position = r;
                        s.rect = rect;
                        s.uv = uv;
                        m_RepeatRectUVList[axis].Add(s);

                        linear_size += s.rect.size[axis];
                    }
                }

                // Adjust for position
                float offset = 0;

                if (backgroundPosition.keyword == BackgroundPositionKeyword.Center)
                {
                    offset = (totalRect.size[axis] - linear_size) * 0.5f;
                }
                else if (repeat != Repeat.Space)
                {
                    if (backgroundPosition.offset.unit == LengthUnit.Percent)
                    {
                        offset = (totalRect.size[axis] - targetRect.size[axis]) * backgroundPosition.offset.value / 100.0f;
                    }
                    else if (backgroundPosition.offset.unit == LengthUnit.Pixel)
                    {
                        offset = backgroundPosition.offset.value;
                    }

                    if ((backgroundPosition.keyword == BackgroundPositionKeyword.Right) || (backgroundPosition.keyword == BackgroundPositionKeyword.Bottom))
                    {

                        offset = (totalRect.size[axis] - linear_size) - offset;
                    }
                }

                // adjust offset position for repeat and round
                if (repeat == Repeat.Repeat || repeat == Repeat.Round)
                {
                    float size = targetRect.size[axis];
                    if (size > UIRUtility.k_Epsilon)
                    {
                        if (offset < -size)
                        {
                            int mod = (int)(-offset/size);
                            offset += mod * size;
                        }

                        if (offset > 0.0f)
                        {
                            int mod = (int)(offset/size);
                            offset -= (1 + mod) * size;
                        }
                    }
                }

                for (int i = 0; i < m_RepeatRectUVList[axis].Count; ++i)
                {
                    RepeatRectUV item = m_RepeatRectUVList[axis][i];
                    Vector2 pos = item.rect.position;

                    pos[axis] += offset;
                    item.rect.position = pos;
                    m_RepeatRectUVList[axis][i] = item;
                }
            }

            Rect originalUV = new Rect(uv);

            foreach (var y in m_RepeatRectUVList[1])
            {
                targetRect.y = y.rect.y;
                targetRect.height = y.rect.height;
                uv.y = y.uv.y;
                uv.height = y.uv.height;

                if (targetRect.y < totalRect.y)
                {
                    float left = totalRect.y - targetRect.y;
                    float right = targetRect.height - left;

                    float total = left + right;
                    float new_height = originalUV.height * right / total;
                    float new_y = originalUV.height * left / total;

                    uv.y = new_y + originalUV.y;
                    uv.height = new_height;

                    targetRect.y = totalRect.y;
                    targetRect.height = right;
                }

                if (targetRect.yMax > totalRect.yMax)
                {
                    float right = targetRect.yMax - totalRect.yMax;
                    float left = targetRect.height - right;
                    float total = left + right;

                    float new_height = uv.height * left / total;
                    uv.height = new_height;
                    uv.y = uv.yMax - new_height;
                    targetRect.height = left;
                }

                if (rectParams.vectorImage == null)
                {
                    // offset y
                    float before = uv.y - originalUV.y;
                    float after = originalUV.yMax - uv.yMax;
                    uv.y += (after - before);
                }

                foreach (var x in m_RepeatRectUVList[0])
                {
                    targetRect.x = x.rect.x;
                    targetRect.width = x.rect.width;
                    uv.x = x.uv.x;
                    uv.width = x.uv.width;

                    if (targetRect.x < totalRect.x)
                    {
                        float left = totalRect.x - targetRect.x;
                        float right = targetRect.width - left;

                        float total = left + right;
                        float new_width = uv.width * right / total;
                        float new_x = originalUV.x + originalUV.width * left / total;

                        uv.x = new_x;
                        uv.width = new_width;

                        targetRect.x = totalRect.x;
                        targetRect.width = right;
                    }

                    if (targetRect.xMax > totalRect.xMax)
                    {
                        float right = targetRect.xMax - totalRect.xMax;
                        float left = targetRect.width - right;
                        float total = left + right;

                        float new_width = uv.width * left / total;
                        uv.width = new_width;
                        targetRect.width = left;
                    }

                    StampRectangleWithSubRect(rectParams, targetRect, uv);
                }
            }
        }

        void StampRectangleWithSubRect(MeshGenerationContextUtils.RectangleParams rectParams, Rect targetRect, Rect targetUV)
        {
            if (targetRect.width < UIRUtility.k_Epsilon || targetRect.height < UIRUtility.k_Epsilon)
                return;

            // Remap the subRect inside the targetRect
            var fullRect = targetRect;
            fullRect.size /= targetUV.size;
            fullRect.position -= new Vector2(targetUV.position.x, 1.0f - targetUV.position.y - targetUV.size.y) * fullRect.size;

            var subRect = rectParams.subRect;
            subRect.position *= fullRect.size;
            subRect.position += fullRect.position;
            subRect.size *= fullRect.size;

            if (rectParams.HasSlices(UIRUtility.k_Epsilon))
            {
                // Use the full target rect when working with slices. The content will stretch to the full target.
                rectParams.rect = targetRect;
            }
            else
            {
                // Find where the subRect intersects with the targetRect.
                var rect = MeshGenerationContextUtils.RectangleParams.RectIntersection(subRect, targetRect);
                if (rect.size.x < UIRUtility.k_Epsilon || rect.size.y < UIRUtility.k_Epsilon)
                    return;

                if (rect.size != subRect.size)
                {
                    // There was an intersection, we need to adjust the UVs
                    var sizeRatio = rect.size / subRect.size;
                    var newUVSize = rectParams.uv.size * sizeRatio;
                    var uvDiff = rectParams.uv.size - newUVSize;
                    if (rect.x > subRect.x)
                    {
                        float overflow = ((subRect.xMax - rect.xMax) / subRect.width) * rectParams.uv.size.x;
                        rectParams.uv.x += uvDiff.x - overflow;
                    }
                    if (rect.yMax < subRect.yMax)
                    {
                        float overflow = ((rect.y - subRect.y) / subRect.height) * rectParams.uv.size.y;
                        rectParams.uv.y += uvDiff.y - overflow;
                    }

                    rectParams.uv.size = newUVSize;
                }

                rectParams.rect = rect;
            }

            DrawRectangle(rectParams);
        }

        public void DrawVisualElementBorder()
        {
            if (currentElement.layout.width >= UIRUtility.k_Epsilon && currentElement.layout.height >= UIRUtility.k_Epsilon)
            {
                var style = currentElement.resolvedStyle;
                if (style.borderLeftColor != Color.clear && style.borderLeftWidth > 0.0f ||
                    style.borderTopColor != Color.clear && style.borderTopWidth > 0.0f ||
                    style.borderRightColor != Color.clear && style.borderRightWidth > 0.0f ||
                    style.borderBottomColor != Color.clear && style.borderBottomWidth > 0.0f)
                {
                    var borderParams = new MeshGenerationContextUtils.BorderParams
                    {
                        rect = currentElement.rect,
                        leftColor = style.borderLeftColor,
                        topColor = style.borderTopColor,
                        rightColor = style.borderRightColor,
                        bottomColor = style.borderBottomColor,
                        leftWidth = style.borderLeftWidth,
                        topWidth = style.borderTopWidth,
                        rightWidth = style.borderRightWidth,
                        bottomWidth = style.borderBottomWidth,
                        leftColorPage = ColorPage.Init(m_Owner, currentElement.renderChainData.borderLeftColorID),
                        topColorPage = ColorPage.Init(m_Owner, currentElement.renderChainData.borderTopColorID),
                        rightColorPage = ColorPage.Init(m_Owner, currentElement.renderChainData.borderRightColorID),
                        bottomColorPage = ColorPage.Init(m_Owner, currentElement.renderChainData.borderBottomColorID),
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
                if (m_MaskDepth > m_StencilRef) // We can't push a mask at ref+1.
                {
                    ++m_StencilRef;
                    Debug.Assert(m_MaskDepth == m_StencilRef);
                }
                m_ClosingInfo.maskStencilRef = m_StencilRef;
                if (UIRUtility.IsVectorImageBackground(currentElement))
                    GenerateStencilClipEntryForSVGBackground();
                else GenerateStencilClipEntryForRoundedRectBackground();
                ++m_MaskDepth;
            }
            m_ClipRectID = currentElement.renderChainData.clipRectID;
        }

        private UInt16[] AdjustSpriteWinding(Vector2[] vertices, ushort[] indices)
        {
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
            var spriteVertices = sprite.vertices;
            var spriteIndices = sprite.triangles;
            var spriteUV = sprite.uv;

            var vertexCount = sprite.vertices.Length;
            var vertices = new Vertex[vertexCount];
            var indices = AdjustSpriteWinding(spriteVertices, spriteIndices);

            var mwd = meshAlloc.Allocate((uint)vertices.Length, (uint)indices.Length);
            var uvRegion = mwd.uvRegion;

            for (int i = 0; i < vertexCount; ++i)
            {
                var v = spriteVertices[i];
                v -= rectParams.spriteGeomRect.position;
                v /= rectParams.spriteGeomRect.size;
                v.y = 1.0f - v.y;
                v *= rectParams.rect.size;
                v += rectParams.rect.position;

                var uv = spriteUV[i];
                uv *= uvRegion.size;
                uv += uvRegion.position;

                vertices[i] = new Vertex()
                {
                    position = new Vector3(v.x, v.y, Vertex.nearZ),
                    tint = rectParams.color,
                    uv = uv
                };
            }

            mwd.SetAllVertices(vertices);
            mwd.SetAllIndices(indices);
        }

        public void RegisterVectorImageGradient(VectorImage vi, out int settingIndexOffset, out TextureId texture)
        {
            texture = new TextureId();

            // The vector image has embedded textures/gradients and we have a manager that can accept the settings.
            // Register the settings and assume that it works.
            var gradientRemap = m_VectorImageManager.AddUser(vi, currentElement);
            settingIndexOffset = gradientRemap.destIndex;
            if (gradientRemap.atlas != TextureId.invalid)
                // The textures/gradients themselves have also been atlased.
                texture = gradientRemap.atlas;
            else
            {
                // Only the settings were atlased.
                texture = TextureRegistry.instance.Acquire(vi.atlas);
                m_Owner.AppendTexture(currentElement, vi.atlas, texture, false);
            }
        }

        public void DrawVectorImage(MeshGenerationContextUtils.RectangleParams rectParams)
        {
            var vi = rectParams.vectorImage;
            Debug.Assert(vi != null);

            int settingIndexOffset = 0;

            TextureId svgTexture = new TextureId();
            bool isUsingGradients = (vi.atlas != null && m_VectorImageManager != null);
            if (isUsingGradients)
            {
                RegisterVectorImageGradient(vi, out settingIndexOffset, out svgTexture);
            }

            int entryCountBeforeSVG = m_Entries.Count;
            int finalVertexCount;
            int finalIndexCount;
            MakeVectorGraphics(rectParams, isUsingGradients, svgTexture, settingIndexOffset, out finalVertexCount, out finalIndexCount);

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

        void MakeVectorGraphics(MeshGenerationContextUtils.RectangleParams rectParams, bool isUsingGradients, TextureId svgTexture, int settingIndexOffset, out int finalVertexCount, out int finalIndexCount)
        {
            var vi = rectParams.vectorImage;
            Debug.Assert(vi != null);
            finalVertexCount = 0;
            finalIndexCount = 0;
            // Convert the VectorImage's serializable vertices to Vertex instances
            int vertexCount = vi.vertices.Length;
            var vertices = new Vertex[vertexCount];
            for (int i = 0; i < vertexCount; ++i)
            {
                var v = vi.vertices[i];
                vertices[i] = new Vertex() {
                    position = v.position,
                    tint = v.tint,
                    uv = v.uv,
                    opacityColorPages = new Color32(0, 0, (byte)(v.settingIndex >> 8), (byte)v.settingIndex),
                    flags = v.flags,
                    circle = v.circle
                };
            }
            MeshWriteDataInterface meshData;
            if (rectParams.leftSlice <= UIRUtility.k_Epsilon &&
                rectParams.topSlice <= UIRUtility.k_Epsilon &&
                rectParams.rightSlice <= UIRUtility.k_Epsilon &&
                rectParams.bottomSlice <= UIRUtility.k_Epsilon)
            {
                meshData = MeshBuilderNative.MakeVectorGraphicsStretchBackground(vertices, vi.indices, vi.size.x, vi.size.y, rectParams.rect, rectParams.uv, rectParams.scaleMode, rectParams.color, settingIndexOffset, ref finalVertexCount, ref finalIndexCount);
            }
            else
            {
                var sliceLTRB = new Vector4(rectParams.leftSlice, rectParams.topSlice, rectParams.rightSlice, rectParams.bottomSlice);
                meshData = MeshBuilderNative.MakeVectorGraphics9SliceBackground(vertices, vi.indices, vi.size.x, vi.size.y, rectParams.rect, sliceLTRB, rectParams.color, settingIndexOffset);
            }
            if (isUsingGradients)
                BuildGradientEntryFromNativeMesh(meshData, svgTexture);
            else
                BuildEntryFromNativeMesh(meshData, null, new TextureId(), false, null, MeshGenerationContext.MeshFlags.None, new Rect(0, 0, 1, 1), VertexFlags.IsSolid);
        }

        internal void Reset()
        {
            ValidateMeshWriteData();

            m_Entries.Clear(); // Doesn't shrink, good
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
            if (currentElement.layout.width <= UIRUtility.k_Epsilon || currentElement.layout.height <= UIRUtility.k_Epsilon)
                return;

            var resolvedStyle = currentElement.resolvedStyle;
            Vector2 radTL, radTR, radBL, radBR;
            MeshGenerationContextUtils.GetVisualElementRadii(currentElement, out radTL, out radBL, out radTR, out radBR);
            float widthT = resolvedStyle.borderTopWidth;
            float widthL = resolvedStyle.borderLeftWidth;
            float widthB = resolvedStyle.borderBottomWidth;
            float widthR = resolvedStyle.borderRightWidth;

            var rp = new MeshGenerationContextUtils.RectangleParams()
            {
                rect = currentElement.rect,
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
            if (currentElement.computedStyle.unityOverflowClipBox == OverflowClipBox.ContentBox)
            {
                rp.rect.x += resolvedStyle.paddingLeft;
                rp.rect.y += resolvedStyle.paddingTop;
                rp.rect.width -= resolvedStyle.paddingLeft + resolvedStyle.paddingRight;
                rp.rect.height -= resolvedStyle.paddingTop + resolvedStyle.paddingBottom;
            }

            m_CurrentEntry.clipRectID = m_ClipRectID;
            m_CurrentEntry.stencilRef = m_StencilRef;
            m_CurrentEntry.maskDepth = m_MaskDepth;
            m_CurrentEntry.isClipRegisterEntry = true;

            var nativeParams = rp.ToNativeParams(new Rect(0,0,1,1));

            var meshData = MeshBuilderNative.MakeSolidRect(nativeParams, UIRUtility.k_MaskPosZ);
            if (meshData.vertexCount > 0 && meshData.indexCount > 0)
            {
                BuildRawEntryFromNativeMesh(meshData);
                m_Entries.Add(m_CurrentEntry);
                totalVertices += m_CurrentEntry.vertices.Length;
                totalIndices += m_CurrentEntry.indices.Length;
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

            m_CurrentEntry.vertices = svgEntry.vertices;
            m_CurrentEntry.indices = svgEntry.indices;
            m_CurrentEntry.uvIsDisplacement = svgEntry.uvIsDisplacement;
            m_CurrentEntry.clipRectID = m_ClipRectID;
            m_CurrentEntry.stencilRef = m_StencilRef;
            m_CurrentEntry.maskDepth = m_MaskDepth;
            m_CurrentEntry.isClipRegisterEntry = true;
            m_ClosingInfo.needsClosing = true;

            // Adjust vertices for stencil clipping
            int vertexCount = m_CurrentEntry.vertices.Length;
            var clipVerts = m_VertsPool.Alloc(vertexCount);
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
}
