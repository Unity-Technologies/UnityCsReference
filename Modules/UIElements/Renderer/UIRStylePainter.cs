// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    internal class UIRStylePainter : IStylePainterInternal
    {
        private IUIRenderDevice m_RenderDevice;
        private UIRenderData m_PaintData;
        private State m_State;
        private UIRAtlasManager m_AtlasManager;

        public UIRStylePainter(IUIRenderDevice renderDevice, UIRAtlasManager atlasManager)
        {
            m_RenderDevice = renderDevice;
            m_State = new State();
            m_AtlasManager = atlasManager;
        }

        public IUIRenderDevice renderDevice
        {
            get { return m_RenderDevice; }
        }

        private VisualElement currentElement
        {
            get { return m_PaintData.visualElement; }
        }

        private bool isTransformedByViewMatrix
        {
            get { return m_PaintData.effectiveViewTransformData != null; }
        }

        private Matrix4x4 currentWorldTransform
        {
            get
            {
                if (isTransformedByViewMatrix)
                {
                    Matrix4x4 inverseViewTransform = m_PaintData.effectiveViewTransformData.visualElement.worldTransform.inverse;
                    return currentElement.worldTransform * inverseViewTransform;
                }
                else
                {
                    return currentElement.worldTransform;
                }
            }
        }

        public uint currentTransformID
        {
            get { return m_PaintData.effectiveSkinningTransformId; }
        }

        public uint currentClippingRectID
        {
            get { return m_PaintData.effectiveClippingRectId; }
        }

        private bool isSkinned
        {
            get { return m_PaintData.effectiveSkinningTransformData != null; }
        }

        private RenderHint currentRenderHint
        {
            get { return currentElement.renderHint; }
        }

        public bool isFontDirty { get; set; }

        public Matrix4x4 projection { get; set; }

        private static bool RequiresRepaint(UIRenderData paintData)
        {
            return paintData.isDirty || paintData.visualElement.renderHint == RenderHint.ImmediateMode;
        }

        public void Paint(UIRenderData paintData)
        {
            if (!RequiresRepaint(paintData))
                return;

            // Immediate mode require to always repaint
            if (paintData.visualElement.renderHint == RenderHint.ImmediateMode && paintData.cachedImmediateRenderer != null)
            {
                UpdateImmediatePaint(paintData);
                if (!paintData.isDirty)
                    return; // We can skip the styling repaint if not dirty
            }

            BeginPaint(paintData);
            currentElement.Repaint(this);
            EndPaint();
        }

        public void PostPaint(UIRenderData paintData)
        {
            var maskRenderer = paintData.cachedMaskRenderer;
            if (maskRenderer != null && maskRenderer.maskUnregister == null)
            {
                // The creation of the unregistration mesh is postponed after the traversal of the descendants
                // to allow contiguous draw ranges.
                MeshHandle maskUnregister;
                MaskRenderer.MakeMeshRegisterUnregister(m_RenderDevice, maskRenderer.maskRegister.mesh, out maskUnregister);
                maskRenderer.maskUnregister = new MeshNode { mesh = maskUnregister };
            }
        }

        private void ResetPaintData()
        {
            m_PaintData.usesTextures = false;
            m_PaintData.ResetInnerChain(m_RenderDevice);
        }

        private void BeginPaint(UIRenderData paintData)
        {
            Debug.Assert(m_PaintData == null, "BeginPaint currently painting another element.");
            Debug.Assert(paintData.visualElement != null, "BeginPaint trying to paint null VisualElement.");

            m_PaintData = paintData;
            ResetPaintData();

            if ((currentRenderHint & RenderHint.ViewTransform) == RenderHint.ViewTransform)
            {
                var zoomPan = new ZoomPanRenderer { viewMatrix = paintData.visualElement.worldTransform };
                m_PaintData.QueueRenderer(zoomPan);
            }

            opacity = currentElement.resolvedStyle.opacity;
        }

        private void EndPaint()
        {
            if (m_PaintData.innerBegin == null)
                m_PaintData.QueueRenderer(m_PaintData.emptyRenderer);

            m_PaintData.OnRepaint();
            Cleanup();
        }

        public void ApplyClipping()
        {
            if (!currentElement.ShouldClip())
            {
                UIRUtility.RemoveClippingRect(m_RenderDevice, m_PaintData);
                return;
            }

            // Don't apply clipping for immediate mode elements to prevent creating clipping
            // buffers that won't be used. Immediate mode elements rely on IMGUI clipping masks instead.
            if ((currentElement.renderHint & RenderHint.ImmediateMode) == RenderHint.ImmediateMode)
                return;

            bool clipWithMask = false;
            bool clipWithScissors = false;
            bool clipWithShader = false;

            bool isRoundRect = UIRUtility.IsRoundRect(currentElement);
            bool mustClipWithScissors = (currentRenderHint & RenderHint.ClipWithScissors) != 0;
            bool supportsShaderClipping = renderDevice.supportsFragmentClipping;

            if (isRoundRect)
            {
                clipWithMask = true;
                Debug.Assert(!mustClipWithScissors);
            }
            else
            {
                clipWithShader = supportsShaderClipping;
                clipWithScissors = !supportsShaderClipping || mustClipWithScissors;
            }

            if (clipWithMask)
            {
                var rectParams = RectStylePainterParameters.GetDefault(currentElement);

                // Only clip the interior shape, skipping the border
                rectParams.rect.x += rectParams.border.leftWidth;
                rectParams.rect.y += rectParams.border.topWidth;
                rectParams.rect.width -= (rectParams.border.leftWidth + rectParams.border.rightWidth);
                rectParams.rect.height -= (rectParams.border.topWidth + rectParams.border.bottomWidth);

                rectParams.rect.width = Mathf.Max(0.0f, rectParams.rect.width);
                rectParams.rect.height = Mathf.Max(0.0f, rectParams.rect.height);

                // Adjust the radius of the inner masking shape.  Unfortunately, the inner corner can have
                // an ellipse shape if the border widths aren't uniform across the shape.  Since we cannot express
                // different x/y radius for the outer shape, we take the mean of the two adjacent widths instead.
                rectParams.border.topLeftRadius -= (rectParams.border.leftWidth + rectParams.border.topWidth) / 2.0f;
                rectParams.border.topRightRadius -= (rectParams.border.rightWidth + rectParams.border.topWidth) / 2.0f;
                rectParams.border.bottomRightRadius -= (rectParams.border.rightWidth + rectParams.border.bottomWidth) / 2.0f;
                rectParams.border.bottomLeftRadius -= (rectParams.border.leftWidth + rectParams.border.bottomWidth) / 2.0f;

                rectParams.border.topLeftRadius = Mathf.Max(0.0f, rectParams.border.topLeftRadius);
                rectParams.border.topRightRadius = Mathf.Max(0.0f, rectParams.border.topRightRadius);
                rectParams.border.bottomRightRadius = Mathf.Max(0.0f, rectParams.border.bottomRightRadius);
                rectParams.border.bottomLeftRadius = Mathf.Max(0.0f, rectParams.border.bottomLeftRadius);

                rectParams.border.SetWidth(0);

                MeshHandle maskRegister = UIRMeshBuilder.MakeRectMeshHandle(m_RenderDevice, rectParams, GetRenderTransform(), null, currentTransformID, currentClippingRectID);
                if (maskRegister == null)
                    return;

                var maskRegNode = new MeshNode { mesh = maskRegister };
                var maskRenderer = new MaskRenderer { maskRegister = maskRegNode, state = new State() };
                m_PaintData.QueueRenderer(maskRenderer);
            }

            if (clipWithShader)
            {
                if (m_PaintData.overridesClippingRect)
                    UIRUtility.UpdateClippingRect(renderDevice, m_PaintData);
                else
                    CreateClippingRect(m_PaintData);
            }

            if (clipWithScissors)
            {
                // Fallback for low-end devices.
                var clip = currentElement.rect;

                // Clip is in local space. Get into skinning-transform space.
                TransformRelativeToParent(ref clip);
                m_PaintData.QueueRenderer(new ScissorClipRenderer { scissorArea = clip, transformID = currentTransformID });
            }
        }

        internal void BeginText(UIRenderData paintData)
        {
            m_PaintData = paintData;
            opacity = currentElement.resolvedStyle.opacity;
        }

        internal void EndText()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            m_PaintData = null;
            opacity = 1.0f;

            m_State.material = null;
            m_State.font = null;
            m_State.custom = null;
        }

        private static void UpdateImmediatePaint(UIRenderData paintData)
        {
            var ve = paintData.visualElement;
            var imRenderer = paintData.cachedImmediateRenderer;
            imRenderer.worldTransform = ve.worldTransform;
            imRenderer.worldClip = ve.worldClip;
        }

        void CreateClippingRect(UIRenderData renderData)
        {
            var device = renderDevice;
            renderData.clippingRectAlloc = device.AllocateClipping();
            renderData.overridesClippingRect = true;
            UIRUtility.UpdateClippingRect(device, renderData);
        }

        public void DrawMesh(MeshStylePainterParameters painterParameters, out NativeSlice<UIVertex> vertexData, out NativeSlice<ushort> indexData, out ushort indexOffset)
        {
            NativeSlice<Vertex> vertexDataOrg;
            MeshHandle mesh = m_RenderDevice.Allocate(painterParameters.vertexCount, painterParameters.indexCount, out vertexDataOrg, out indexData, out indexOffset);
            vertexData = vertexDataOrg.SliceConvert<UIVertex>();

            SetTexture(null);
            SetMaterial(painterParameters.material);
            QueueMeshNode(new MeshNode { mesh = mesh });
        }

        public void DrawRect(RectStylePainterParameters painterParams)
        {
            painterParams.color *= m_OpacityColor;

            var mesh = UIRMeshBuilder.MakeRectMeshHandle(m_RenderDevice, painterParams, GetRenderTransform(), null, currentTransformID, currentClippingRectID);
            if (mesh == null)
                // This can happen in cases where the width or height is close to zero.
                return;
            var meshNode = new MeshNode { mesh = mesh };

            SetTexture(null);
            QueueMeshNode(meshNode);
        }

        public void DrawText(TextStylePainterParameters painterParams)
        {
            DrawText(painterParams, null);
        }

        public void DrawText(TextStylePainterParameters painterParams, MeshHandle oldMeshHandle)
        {
            // WARNING: Do not use currentWorldTransform!
            float scaling = TextNative.ComputeTextScaling(currentElement.worldTransform, GUIUtility.pixelsPerPoint);
            TextNativeSettings textSettings = painterParams.GetTextNativeSettings(scaling);
            textSettings.color *= m_OpacityColor;

            // Calculate the aligned offset in local space.
            Matrix4x4 localToClip = projection * currentElement.worldTransform;
            Vector2 localOffset = TextNative.GetOffset(textSettings, painterParams.rect);
            localOffset = AlignPointToDevice(localToClip, localOffset);

            Matrix4x4 textTransform = GetRenderTransform() * Matrix4x4.Translate(localOffset);

            using (NativeArray<TextVertex> vertices = TextNative.GetVertices(textSettings))
            {
                if (oldMeshHandle == null)
                {
                    var meshNode = new MeshNode
                    {
                        mesh = UIRMeshBuilder.MakeTextMeshHandle(m_RenderDevice, vertices, textTransform, null, currentTransformID, currentClippingRectID)
                    };
                    SetFontTexture(painterParams.font.material.mainTexture);
                    QueueMeshNode(meshNode);

                    m_PaintData.cachedTextRenderer = m_PaintData.innerLastQueued;
                    m_PaintData.textMeshHandle = meshNode.mesh;
                    m_PaintData.textParams = painterParams;
                }
                else
                {
                    UIRMeshBuilder.UpdateTextMeshHandle(m_RenderDevice, vertices, textTransform, oldMeshHandle, currentTransformID, currentClippingRectID);
                    UpdateFontTextureIfNeeded(painterParams.font.material.mainTexture, m_PaintData.cachedTextRenderer);
                }
            }
        }

        private void UpdateFontTextureIfNeeded(Texture fontTexture, RendererBase renderer)
        {
            if (renderer == null)
                return;

            if (renderer.type == RendererTypes.MeshRenderer)
            {
                var meshRenderer = (UnityEngine.UIElements.UIR.MeshRenderer)renderer;
                if (meshRenderer.state.font != fontTexture)
                    meshRenderer.state.font = fontTexture;
            }
        }

        internal void OnFontTextureRebuilt(Font font)
        {
            isFontDirty = true;
        }

        private static Vector2 GetRenderTargetSize()
        {
            Vector2 targetSize;

            RenderTexture rt = RenderTexture.active;
            if (rt != null)
            {
                targetSize.x = rt.width;
                targetSize.y = rt.height;
            }
            else
            {
                targetSize.x = Screen.width;
                targetSize.y = Screen.height;
            }

            return targetSize;
        }

        private static Vector2 AlignPointToDevice(Matrix4x4 localToDevice, Vector2 localPoint)
        {
            Vector2 targetSize = GetRenderTargetSize();

            if (targetSize.x <= 0 || targetSize.y <= 0)
            {
                // This is a safety net used when called outside a GUI clip (e.g. watermarks).
                return localPoint;
            }

            Vector3 point = localToDevice.MultiplyPoint(localPoint);

            // Convert from -1..1 to 0..1
            point += new Vector3(1, 1, 0);
            point *= 0.5f;

            // Scale to texture size and round.
            // It is VERY important to use an uncommon rounding point to prevent jitter.
            point.x = Mathf.Floor(point.x * targetSize.x + 0.491476f);
            point.y = Mathf.Floor(point.y * targetSize.y + 0.491476f);

            // Bring back to 0..1
            point.x /= targetSize.x;
            point.y /= targetSize.y;

            // Bring back to -1..1
            point *= 2.0f;
            point -= new Vector3(1, 1, 0);

            // Bring back to local space.
            point = localToDevice.inverse.MultiplyPoint(point);

            return point;
        }

        public void DrawTexture(TextureStylePainterParameters painterParams)
        {
            m_PaintData.usesTextures = true;

            painterParams.color *= m_OpacityColor;

            // Handle scaling mode
            Rect screenRect = painterParams.rect;
            Rect sourceRect = painterParams.uv != Rect.zero ? painterParams.uv : new Rect(0, 0, 1, 1);
            Texture texture = painterParams.texture;

            ScaleMode scaleMode = painterParams.scaleMode;
            Rect textureRect = screenRect;

            /// Comparing aspects ratio is error-prone because the <c>screenRect</c> may end up being scaled by the
            /// transform and the corners will end up being pixel aligned, possibly resulting in blurriness.
            float srcAspect = (texture.width * sourceRect.width) / (texture.height * sourceRect.height);
            float destAspect = screenRect.width / screenRect.height;
            switch (scaleMode)
            {
                case ScaleMode.StretchToFill:
                    break;

                case ScaleMode.ScaleAndCrop:
                    if (destAspect > srcAspect)
                    {
                        float stretch = sourceRect.height * (srcAspect / destAspect);
                        float crop = (sourceRect.height - stretch) * 0.5f;
                        sourceRect = new Rect(sourceRect.x, sourceRect.y + crop, sourceRect.width, stretch);
                    }
                    else
                    {
                        float stretch = sourceRect.width * (destAspect / srcAspect);
                        float crop = (sourceRect.width - stretch) * 0.5f;
                        sourceRect = new Rect(sourceRect.x + crop, sourceRect.y, stretch, sourceRect.height);
                    }
                    break;

                case ScaleMode.ScaleToFit:
                    if (destAspect > srcAspect)
                    {
                        float stretch = srcAspect / destAspect;
                        textureRect = new Rect(screenRect.xMin + screenRect.width * (1.0f - stretch) * .5f, screenRect.yMin, stretch * screenRect.width, screenRect.height);
                    }
                    else
                    {
                        float stretch = destAspect / srcAspect;
                        textureRect = new Rect(screenRect.xMin, screenRect.yMin + screenRect.height * (1.0f - stretch) * .5f, screenRect.width, stretch * screenRect.height);
                    }
                    break;
            }

            // Attempt to override with an atlas.
            VertexFlags vertexFlags;
            RectInt atlasRect;
            if (m_AtlasManager != null && m_AtlasManager.TryGetLocation(texture as Texture2D, out atlasRect))
            {
                vertexFlags = VertexFlags.IsTextured;
                sourceRect = new Rect(
                    sourceRect.x * texture.width + atlasRect.x,
                    sourceRect.y * texture.height + atlasRect.y,
                    sourceRect.width * atlasRect.width,
                    sourceRect.height * atlasRect.height);
            }
            else
                vertexFlags = VertexFlags.IsCustom;

            painterParams.texture = texture;
            painterParams.rect = textureRect;
            painterParams.uv = sourceRect;
            var meshNode = new MeshNode
            {
                mesh = UIRMeshBuilder.MakeTextureMeshHandle(m_RenderDevice, painterParams, GetRenderTransform(), null, currentTransformID, currentClippingRectID, vertexFlags)
            };

            SetTexture(VertexFlagsUtil.TypeIsEqual(vertexFlags, VertexFlags.IsCustom) ? texture : null);
            QueueMeshNode(meshNode);
        }

        public void DrawImmediate(Action callback)
        {
            var renderer = new ImmediateRenderer()
            {
                immediateHandler = new ImmediateRenderer.DrawImmediateDelegate(callback),
                worldTransform = currentElement.worldTransform,
                worldClip = currentElement.worldClip
            };
            m_PaintData.QueueRenderer(renderer);
        }

        public void DrawBackground()
        {
            if (currentElement.layout.width < 0f || currentElement.layout.height < 0f)
                return;

            var style = currentElement.computedStyle;

            if (style.backgroundColor != Color.clear)
            {
                var painterParams = RectStylePainterParameters.GetDefault(currentElement);
                painterParams.border.SetWidth(0.0f);
                DrawRect(painterParams);
            }
            if (style.backgroundImage.value.texture != null)
            {
                var painterParams = TextureStylePainterParameters.GetDefault(currentElement);
                if (style.unityBackgroundImageTintColor != Color.clear)
                {
                    painterParams.color = style.unityBackgroundImageTintColor.value;
                }
                painterParams.border.SetWidth(0.0f);
                DrawTexture(painterParams);
            }
        }

        public void DrawBorder()
        {
            if (currentElement.layout.width < 0f || currentElement.layout.height < 0f)
                return;

            var style = currentElement.computedStyle;
            if (style.borderColor != Color.clear && (style.borderLeftWidth.value > 0.0f || style.borderTopWidth.value > 0.0f || style.borderRightWidth.value > 0.0f || style.borderBottomWidth.value > 0.0f))
            {
                var painterParams = RectStylePainterParameters.GetDefault(currentElement);
                painterParams.color = style.borderColor.value;
                DrawRect(painterParams);
            }
        }

        public void DrawText(string text)
        {
            if (!string.IsNullOrEmpty(text) && currentElement.contentRect.width > 0.0f && currentElement.contentRect.height > 0.0f)
            {
                DrawText(TextStylePainterParameters.GetDefault(currentElement, text));
            }
        }

        Color m_OpacityColor = Color.white;
        public float opacity
        {
            get
            {
                return m_OpacityColor.a;
            }
            set
            {
                m_OpacityColor.a = value;
            }
        }

        private void SetTexture(Texture texture)
        {
            bool queueNewRenderer = (m_PaintData.innerLastQueued == null || m_PaintData.innerLastQueued.type != RendererTypes.MeshRenderer);
            if (m_State.material != null)
            {
                // Restore default material
                m_State.material = null;
                queueNewRenderer = true;
            }

            if (m_State.custom != texture)
            {
                m_State.custom = texture;
                queueNewRenderer = true;
            }

            if (queueNewRenderer)
            {
                QueueNewMeshRenderer();
            }
        }

        private void SetFontTexture(Texture texture)
        {
            bool queueNewRenderer = (m_PaintData.innerLastQueued == null || m_PaintData.innerLastQueued.type != RendererTypes.MeshRenderer);
            if (m_State.material != null)
            {
                // Restore default material
                m_State.material = null;
                queueNewRenderer = true;
            }

            if (m_State.font != texture)
            {
                m_State.font = texture;
                queueNewRenderer = true;
            }

            if (queueNewRenderer)
            {
                QueueNewMeshRenderer();
            }
        }

        private void SetMaterial(Material material)
        {
            if ((m_State.material != material) || m_PaintData.innerLastQueued == null || m_PaintData.innerLastQueued.type != RendererTypes.MeshRenderer)
            {
                m_State.material = material;
                m_State.custom = null;
                m_State.font = null;
                QueueNewMeshRenderer();
            }
        }

        private void QueueMeshNode(MeshNode meshNode)
        {
            if (m_PaintData.innerLastQueued == null || m_PaintData.innerLastQueued.type != RendererTypes.MeshRenderer)
                QueueNewMeshRenderer();

            if (m_PaintData.effectiveMaskRendererData != null)
                MaskRenderer.MakeMeshMasked(meshNode.mesh);

            var meshRenderer = (UIR.MeshRenderer)m_PaintData.innerLastQueued;
            if (meshRenderer.meshChain == null)
            {
                meshRenderer.meshChain = meshNode;
            }
            else
            {
                var node = meshRenderer.meshChain;
                while (node.next != null)
                {
                    node = node.next;
                }

                node.next = meshNode;
            }
        }

        private void QueueNewMeshRenderer()
        {
            m_PaintData.QueueRenderer(new UIR.MeshRenderer() {state = new State(m_State)});
        }

        public Matrix4x4 GetRenderTransform()
        {
            if (isSkinned)
                return m_PaintData.effectiveSkinningTransformData.visualElement.worldTransform.inverse * currentElement.worldTransform;
            if (isTransformedByViewMatrix)
                return m_PaintData.effectiveViewTransformData.visualElement.worldTransform.inverse * currentElement.worldTransform;
            return currentElement.worldTransform;
        }

        public Matrix4x4 GetParentTransform()
        {
            if (isSkinned)
                return m_PaintData.effectiveSkinningTransformData.visualElement.worldTransform;
            if (isTransformedByViewMatrix)
                return m_PaintData.effectiveViewTransformData.visualElement.worldTransform;
            return currentElement.worldTransform;
        }

        private void TransformRelativeToParent(ref Rect r)
        {
            if (isSkinned)
            {
                r = currentElement.ChangeCoordinatesTo(m_PaintData.effectiveSkinningTransformData.visualElement, r);
            }
            else if (isTransformedByViewMatrix)
            {
                r = currentElement.ChangeCoordinatesTo(m_PaintData.effectiveViewTransformData.visualElement, r);
            }
            else
            {
                r = currentElement.LocalToWorld(r);
            }
        }
    }
}
