// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.UIElements
{
    internal class VisualTreeRepaintUpdater : BaseVisualTreeUpdater
    {
        private HashSet<VisualElement> m_RepaintList = new HashSet<VisualElement>();
        private ImmediateStylePainterImpl m_StylePainter = new ImmediateStylePainterImpl();

        public override string description
        {
            get { return "Repaint"; }
        }

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if ((versionChangeType & VersionChangeType.Repaint) == VersionChangeType.Repaint)
            {
                m_RepaintList.Add(ve);
            }

            // All changes propagate to parent as a repaint change
            PropagateToParents(ve);
        }

        public override void Update()
        {
            Rect clipRect = visualTree.ShouldClip() ? visualTree.layout : GUIClip.topmostRect;

            PaintSubTree(visualTree, Matrix4x4.identity, false, false, clipRect);

            m_RepaintList.Clear();
        }

        private void PropagateToParents(VisualElement ve)
        {
            var parent = ve.hierarchy.parent;
            while (parent != null)
            {
                if (!m_RepaintList.Add(parent))
                {
                    break;
                }

                parent = parent.hierarchy.parent;
            }
        }

        internal bool ShouldUsePixelCache(VisualElement root)
        {
            return panel.allowPixelCaching && root.cacheAsBitmap &&
                root.worldBound.size.magnitude > Mathf.Epsilon;
        }

        private void PaintSubTree(VisualElement root, Matrix4x4 offset, bool shouldClip, bool shouldCache, Rect currentGlobalClip)
        {
            if (root == null || root.panel != panel)
                return;

            if (root.visible == false ||
                root.resolvedStyle.opacity < Mathf.Epsilon ||
                root.resolvedStyle.display == DisplayStyle.None)
                return;

            // update clip
            if (root.ShouldClip())
            {
                var worldBound = VisualElement.ComputeAAAlignedBound(root.rect, offset * root.worldTransform);
                // are we and our children clipped?
                if (!worldBound.Overlaps(currentGlobalClip))
                {
                    return;
                }

                float x1 = Mathf.Max(worldBound.x, currentGlobalClip.x);
                float x2 = Mathf.Min(worldBound.xMax, currentGlobalClip.xMax);
                float y1 = Mathf.Max(worldBound.y, currentGlobalClip.y);
                float y2 = Mathf.Min(worldBound.yMax, currentGlobalClip.yMax);

                // new global clip and hierarchical clip space option.
                currentGlobalClip = new Rect(x1, y1, x2 - x1, y2 - y1);
                shouldClip = true;
                shouldCache = root.cacheAsBitmap;
            }

            var prevElement = m_StylePainter.currentElement;
            m_StylePainter.currentElement = root;

            var repaintData = panel.repaintData;
            if (ShouldUsePixelCache(root))
            {
                // now actually paint the texture to previous group
                // validate cache texture size first
                var worldBound = root.worldBound;

                Rect alignedRect;
                int textureWidth, textureHeight;
                repaintData.currentWorldClip = currentGlobalClip;
                repaintData.currentOffset = offset;
                using (new GUIClip.ParentClipScope(offset * root.worldTransform, currentGlobalClip))
                {
                    alignedRect = GUIUtility.AlignRectToDevice(root.rect, out textureWidth, out textureHeight);
                }

                // Prevent the texture size from going empty, which may occur if the element has a sub-pixel size
                textureWidth = Math.Max(textureWidth, 1);
                textureHeight = Math.Max(textureHeight, 1);

                var cache = root.renderData.pixelCache;

                if (cache != null &&
                    (cache.width != textureWidth || cache.height != textureHeight) &&
                    (!panel.keepPixelCacheOnWorldBoundChange || m_RepaintList.Contains(root)))
                {
                    Object.DestroyImmediate(cache);
                    cache = root.renderData.pixelCache = null;
                }

                // if the child node world transforms are not up to date due to changes below the pixel cache this is fine.
                if (m_RepaintList.Contains(root)
                    || root.renderData.pixelCache == null
                    || !root.renderData.pixelCache.IsCreated())
                {
                    // Recreate as needed
                    if (cache == null)
                    {
                        root.renderData.pixelCache = cache = new RenderTexture(
                            textureWidth,
                            textureHeight,
                            32,         // depth
                            GraphicsFormat.R8G8B8A8_UNorm);
                    }

                    bool hasRoundedBorderRects = (root.computedStyle.borderTopLeftRadius.value.value > 0 ||
                        root.computedStyle.borderTopRightRadius.value.value > 0 ||
                        root.computedStyle.borderBottomLeftRadius.value.value > 0 ||
                        root.computedStyle.borderBottomRightRadius.value.value > 0);

                    RenderTexture temporaryTexture = null;
                    var old = RenderTexture.active;

                    try
                    {
                        // We first render to a temp texture, then blit the result into the result pixelCache again to mask the rounder corners
                        if (hasRoundedBorderRects)
                        {
                            // This should be an sRGB texture to conform with editor texture guidelines, however
                            // converting the cache to sRGB requires a shader to do it, and this whole pixel cache
                            // thing is slated to go away, so we take a short-cut here and use a linear texture
                            // along with the use of manualTex2SRGBEnabled to get the correct results.
                            temporaryTexture = cache = RenderTexture.GetTemporary(textureWidth, textureHeight, 32, GraphicsFormat.R8G8B8A8_UNorm);
                        }

                        // render the texture again to clip the round rect borders
                        RenderTexture.active = cache;

                        GL.Clear(true, true, new Color(0, 0, 0, 0));

                        // Calculate the offset required to translate the origin of the rect to the upper left corner
                        // of the pixel cache. We need to round because the rect will be rounded when rendered.
                        Rect worldAlignedRect = root.LocalToWorld(alignedRect);
                        var childrenOffset = Matrix4x4.Translate(new Vector3(-worldAlignedRect.x, -worldAlignedRect.y, 0));

                        Matrix4x4 offsetWorldTransform = childrenOffset * root.worldTransform;

                        // reset clipping
                        var textureClip = new Rect(0, 0, worldAlignedRect.width, worldAlignedRect.height);
                        repaintData.currentOffset = childrenOffset;

                        using (new GUIClip.ParentClipScope(offsetWorldTransform, textureClip))
                        {
                            // Paint self
                            repaintData.currentWorldClip = textureClip;

                            m_StylePainter.opacity = root.resolvedStyle.opacity;
                            root.Repaint(m_StylePainter);
                            m_StylePainter.opacity = 1.0f;

                            PaintSubTreeChildren(root, childrenOffset, shouldClip, shouldCache, textureClip);
                        }

                        if (hasRoundedBorderRects)
                        {
                            RenderTexture.active = root.renderData.pixelCache;

                            bool oldManualTex2SRGBEnabled = GUIUtility.manualTex2SRGBEnabled;
                            GUIUtility.manualTex2SRGBEnabled = false;
                            using (new GUIClip.ParentClipScope(Matrix4x4.identity, textureClip))
                            {
                                GL.Clear(true, true, new Color(0, 0, 0, 0));

                                var textureParams = TextureStylePainterParameters.GetDefault(root);
                                textureParams.color = Color.white;
                                textureParams.texture = cache;
                                textureParams.scaleMode = ScaleMode.StretchToFill;
                                textureParams.rect = textureClip;

                                textureParams.border.SetWidth(0.0f);

                                // The rect of the temporary texture implicitly carries the scale factor of the
                                // transform. Since we are blitting with an identity matrix, we need to scale the
                                // radius manually.
                                // We assume uniform positive scaling without rotations.
                                Vector4 toScale = new Vector4(1, 0, 0, 0);
                                Vector4 scaled = offsetWorldTransform * toScale;
                                float radiusScale = scaled.x;
                                textureParams.border.SetRadius(
                                    textureParams.border.topLeftRadius * radiusScale,
                                    textureParams.border.topRightRadius * radiusScale,
                                    textureParams.border.bottomRightRadius * radiusScale,
                                    textureParams.border.bottomLeftRadius * radiusScale);

                                // No border is drawn but the rounded corners are clipped properly.
                                // Use premultiply alpha to avoid blending again.
                                textureParams.usePremultiplyAlpha = true;
                                m_StylePainter.DrawTexture(textureParams);
                            }

                            // Redraw the border (border was already drawn in first root.DoRepaint call).
                            using (new GUIClip.ParentClipScope(offsetWorldTransform, textureClip))
                            {
                                m_StylePainter.DrawBorder();
                            }
                            GUIUtility.manualTex2SRGBEnabled = oldManualTex2SRGBEnabled;
                        }
                    }
                    finally
                    {
                        cache = null;
                        if (temporaryTexture != null)
                        {
                            RenderTexture.ReleaseTemporary(temporaryTexture);
                        }
                        RenderTexture.active = old;
                    }
                }

                // now actually paint the texture to previous group
                repaintData.currentWorldClip = currentGlobalClip;
                repaintData.currentOffset = offset;

                bool oldManualTex2SRGBEnabled2 = GUIUtility.manualTex2SRGBEnabled;
                GUIUtility.manualTex2SRGBEnabled = false;
                using (new GUIClip.ParentClipScope(offset * root.worldTransform, currentGlobalClip))
                {
                    var painterParams = new TextureStylePainterParameters
                    {
                        rect = GUIUtility.AlignRectToDevice(root.rect),
                        uv = new Rect(0, 0, 1, 1),
                        texture = root.renderData.pixelCache,
                        color = Color.white,
                        scaleMode = ScaleMode.StretchToFill,
                        usePremultiplyAlpha = true
                    };

                    // We must not reapply the editor Play Mode Tint if it was already applied !
                    var playModeTint = UIElementsUtility.editorPlayModeTintColor;
                    UIElementsUtility.editorPlayModeTintColor = Color.white;

                    m_StylePainter.DrawTexture(painterParams);
                    UIElementsUtility.editorPlayModeTintColor = playModeTint;
                }
                GUIUtility.manualTex2SRGBEnabled = oldManualTex2SRGBEnabled2;
            }
            else
            {
                repaintData.currentOffset = offset;

                using (new GUIClip.ParentClipScope(offset * root.worldTransform, currentGlobalClip))
                {
                    repaintData.currentWorldClip = currentGlobalClip;
                    repaintData.mousePosition = VisualElement.MultiplyMatrix44Point2(root.worldTransformInverse, repaintData.repaintEvent.mousePosition);

                    m_StylePainter.opacity = root.resolvedStyle.opacity;
                    root.Repaint(m_StylePainter);
                    m_StylePainter.opacity = 1.0f;


                    PaintSubTreeChildren(root, offset, shouldClip, shouldCache, currentGlobalClip);
                }
            }

            m_StylePainter.currentElement = prevElement;
        }

        private void PaintSubTreeChildren(VisualElement root, Matrix4x4 offset, bool shouldClip, bool shouldCache, Rect textureClip)
        {
            int count = root.hierarchy.childCount;
            for (int i = 0; i < count; i++)
            {
                VisualElement child = root.hierarchy[i];

                PaintSubTree(child, offset, shouldClip, shouldCache, textureClip);

                if (count != root.hierarchy.childCount)
                {
                    throw new NotImplementedException("Visual tree is read-only during repaint");
                }
            }
        }
    }
}
