// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.UIElements.Experimental;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Debugger
{
    [Flags]
    internal enum OverlayContent
    {
        Content = 1 << 0,
        Padding = 1 << 1,
        Border = 1 << 2,
        Margin = 1 << 3,
        All = Content | Padding | Border | Margin
    }

    internal class OverlayData
    {
        public OverlayData(VisualElement ve, float alpha)
        {
            this.element = ve;
            this.alpha = alpha;
            this.defaultAlpha = alpha;
        }

        public VisualElement element;
        public float alpha;
        public float defaultAlpha;
        public OverlayContent content;
        private ValueAnimation<float> m_animation;

        public void StartFadeOutAnimation(VisualElement container, int duration)
        {
            if (m_animation != null)
            {
                m_animation.Stop();
                m_animation.durationMs = duration;
                m_animation.Start();
            }
            else
            {
                m_animation = container.experimental.animation.Start(defaultAlpha, 0, duration, (ve, value) =>
                {
                    alpha = value;
                    ve.MarkDirtyRepaint();
                }).Ease(Easing.OutCubic).KeepAlive();
            }
        }
    }

    internal abstract class BaseOverlayPainter
    {
        protected Dictionary<VisualElement, OverlayData> m_OverlayData = new Dictionary<VisualElement, OverlayData>();
        protected List<VisualElement> m_CleanUpOverlay = new List<VisualElement>();

        // World-space projection state, only valid during Draw(). When the debugged panel is a
        // world-space panel, its elements are in world space while the debugger overlay panel maps
        // 1:1 to the target display, so geometry must be projected through the rendering camera.
        // A panel may batch multiple root panel components on different layers, each potentially
        // rendered by a different camera, so the camera is resolved per panel component.
        private bool m_ProjectToScreen;
        private Vector2 m_ProjectionTargetSize;
        private int m_ProjectionTargetDisplay;
        private Camera[] m_SortedCameras;
        private readonly Dictionary<IPanelComponent, Camera> m_ComponentCameras = new();
        private VisualElement m_LastProjectionElement;
        private Matrix4x4 m_LastDocumentToWorld;
        private Camera m_LastProjectionCamera;

        public void Draw(MeshGenerationContext mgc)
        {
            if (!BeginOverlayProjection(mgc))
                return;

            try
            {
                DrawContent(mgc);
            }
            finally
            {
                EndOverlayProjection();
            }
        }

        protected virtual void DrawContent(MeshGenerationContext mgc)
        {
            PaintAllOverlay(mgc);

            foreach (var ve in m_CleanUpOverlay)
            {
                m_OverlayData.Remove(ve);
            }
            m_CleanUpOverlay.Clear();
        }

        // Returns false when the debugged panel is world-space but the projection state cannot be
        // built; nothing should be drawn in that case.
        private bool BeginOverlayProjection(MeshGenerationContext mgc)
        {
            m_ProjectToScreen = false;

            var overlayPanel = mgc.visualElement?.elementPanel as Panel;
            if (overlayPanel?.overlayedOverPanel is not BaseRuntimePanel { drawsInCameras: true } runtimePanel)
                return true;

            var targetSize = PanelDebug.GetTargetDisplaySize(runtimePanel);
            if (targetSize == Vector2.zero)
                return false;

            m_ProjectionTargetSize = targetSize;
            m_ProjectionTargetDisplay = runtimePanel.targetDisplay;

            // Non-allocating equivalent of Camera.allCameras (same as world-space picking).
            Array.Resize(ref m_SortedCameras, Camera.allCamerasCount);
            Camera.GetAllCameras(m_SortedCameras);
            Array.Sort(m_SortedCameras, (a, b) => a.depth.CompareTo(b.depth));

            // Only activate the projection once all the state above is fully assigned.
            m_ProjectToScreen = true;
            return true;
        }

        private void EndOverlayProjection()
        {
            m_ComponentCameras.Clear();
            m_LastProjectionElement = null;
            m_LastProjectionCamera = null;
            m_ProjectToScreen = false;
        }

        private Camera GetCameraForComponent(IPanelComponent panelComponent)
        {
            if (m_ComponentCameras.TryGetValue(panelComponent, out var camera))
                return camera;

            camera = FindComponentCamera(panelComponent);
            m_ComponentCameras[panelComponent] = camera; // Also cache misses
            return camera;
        }

        // Closest to deepest, take the first camera that can see the component's layer.
        private Camera FindComponentCamera(IPanelComponent panelComponent)
        {
            for (var i = m_SortedCameras.Length - 1; i >= 0; i--)
            {
                var camera = m_SortedCameras[i];
                if (camera.targetDisplay != m_ProjectionTargetDisplay)
                    continue;

                if ((camera.cullingMask & (1 << panelComponent.gameObject.layer)) != 0)
                    return camera;
            }

            return null;
        }

        // True while drawing for a world-space target panel; geometry must go through TryProjectPoint.
        protected bool projectionActive => m_ProjectToScreen;

        protected bool TryProjectPoint(Camera camera, Vector3 worldPoint, out Vector2 overlayPoint)
        {
            var screenPoint = camera.WorldToScreenPoint(worldPoint);
            if (screenPoint.z <= 0)
            {
                overlayPoint = default;
                return false;
            }

            overlayPoint = new Vector2(screenPoint.x, m_ProjectionTargetSize.y - screenPoint.y);
            return true;
        }

        // ve.worldTransform only maps up to document space. For world-space panels, the transform
        // of the GameObject holding the root panel component must be composed in to reach world
        // space, and the projection must go through the camera that renders that component.
        // The result is cached for the last element since painters typically project many
        // primitives for the same element in a row.
        protected bool TryGetElementProjection(VisualElement ve, out Matrix4x4 documentToWorld, out Camera camera)
        {
            if (ve == m_LastProjectionElement)
            {
                documentToWorld = m_LastDocumentToWorld;
                camera = m_LastProjectionCamera;
                return camera != null;
            }

            m_LastProjectionElement = ve;
            m_LastProjectionCamera = null;
            documentToWorld = default;
            camera = null;

            var panelComponent = ve.FindRootPanelComponent();
            if (panelComponent == null)
                return false;

            camera = GetCameraForComponent(panelComponent);
            if (camera == null)
                return false;

            documentToWorld = panelComponent.gameObject.transform.localToWorldMatrix;
            m_LastDocumentToWorld = documentToWorld;
            m_LastProjectionCamera = camera;
            return true;
        }

        // Temporarily makes the camera behave like it would in a normal Update by recomputing the
        // pixelRect from the rect and the display size, because outside of the camera's own render
        // loop the pixelRect reflects whatever surface was rendered last (same as world-space
        // picking). Keep the window as narrow as possible around the WorldToScreenPoint calls and
        // restore the returned rect with EndCameraScreenProjection.
        protected Rect BeginCameraScreenProjection(Camera camera)
        {
            var oldPixelRect = camera.pixelRect;
            camera.pixelRect = new Rect(camera.rect.position * m_ProjectionTargetSize, camera.rect.size * m_ProjectionTargetSize);
            return oldPixelRect;
        }

        protected static void EndCameraScreenProjection(Camera camera, Rect oldPixelRect)
        {
            camera.pixelRect = oldPixelRect;
        }

        private bool TryProjectRect(VisualElement ve, Rect localRect, out Vector2 p0, out Vector2 p1, out Vector2 p2, out Vector2 p3)
        {
            p0 = p1 = p2 = p3 = default;

            if (!TryGetElementProjection(ve, out var documentToWorld, out var camera))
                return false;

            var m = documentToWorld * ve.worldTransform;
            var oldPixelRect = BeginCameraScreenProjection(camera);
            bool projected =
                TryProjectPoint(camera, m.MultiplyPoint3x4(new Vector3(localRect.xMin, localRect.yMin, 0)), out p0) &&
                TryProjectPoint(camera, m.MultiplyPoint3x4(new Vector3(localRect.xMax, localRect.yMin, 0)), out p1) &&
                TryProjectPoint(camera, m.MultiplyPoint3x4(new Vector3(localRect.xMax, localRect.yMax, 0)), out p2) &&
                TryProjectPoint(camera, m.MultiplyPoint3x4(new Vector3(localRect.xMin, localRect.yMax, 0)), out p3);
            EndCameraScreenProjection(camera, oldPixelRect);
            return projected;
        }

        private void PaintAllOverlay(MeshGenerationContext mgc)
        {
            foreach (var kvp in m_OverlayData)
            {
                var overlayData = kvp.Value;

                DrawOverlayData(mgc, overlayData);
                if (overlayData.alpha < UIRUtility.k_Epsilon)
                {
                    m_CleanUpOverlay.Add(kvp.Key);
                }
            }
        }

        public int overlayCount
        {
            get { return m_OverlayData.Count; }
        }

        public void ClearOverlay()
        {
            m_OverlayData.Clear();
        }

        protected abstract void DrawOverlayData(MeshGenerationContext mgc, OverlayData overlayData);

        // Draws a rect expressed in the element's local coordinates.
        protected void DrawElementRect(MeshGenerationContext mgc, VisualElement ve, Rect localRect, Color color, float alpha)
        {
            if (!projectionActive)
            {
                DrawRect(mgc, ve.LocalToWorld(localRect), color, alpha);
                return;
            }

            if (!TryProjectRect(ve, localRect, out var p0, out var p1, out var p2, out var p3))
                return;

            color.a = alpha;
            DrawQuad(mgc, p0, p1, p2, p3, color);
        }

        // Draws the outline of a rect expressed in the element's local coordinates.
        protected void DrawElementBorder(MeshGenerationContext mgc, VisualElement ve, Rect localRect, Color color, float alpha)
        {
            if (!projectionActive)
            {
                DrawBorder(mgc, ve.LocalToWorld(localRect), color, alpha);
                return;
            }

            if (!TryProjectRect(ve, localRect, out var p0, out var p1, out var p2, out var p3))
                return;

            DrawLine(mgc, p0, p1, color, alpha);
            DrawLine(mgc, p1, p2, color, alpha);
            DrawLine(mgc, p2, p3, color, alpha);
            DrawLine(mgc, p3, p0, color, alpha);
        }

        protected void DrawQuad(MeshGenerationContext mgc, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, Color color)
        {
            color *= mgc.visualElement?.playModeTintColor ?? Color.white;

            var mesh = mgc.Allocate(4, 6);
            mesh.SetNextVertex(new Vertex() { position = new Vector3(p0.x, p0.y, Vertex.nearZ), tint = color });
            mesh.SetNextVertex(new Vertex() { position = new Vector3(p1.x, p1.y, Vertex.nearZ), tint = color });
            mesh.SetNextVertex(new Vertex() { position = new Vector3(p2.x, p2.y, Vertex.nearZ), tint = color });
            mesh.SetNextVertex(new Vertex() { position = new Vector3(p3.x, p3.y, Vertex.nearZ), tint = color });

            mesh.SetNextIndex(0);
            mesh.SetNextIndex(1);
            mesh.SetNextIndex(2);
            mesh.SetNextIndex(0);
            mesh.SetNextIndex(2);
            mesh.SetNextIndex(3);
        }

        protected void DrawLine(MeshGenerationContext mgc, Vector2 v0, Vector2 v1, Color wireColor, float alpha)
        {
            var v = (v1 - v0);
            var leftPerp = new Vector2(v.y, -v.x).normalized * 0.5f;
            var p0 = v0 + leftPerp;
            var p1 = v1 + leftPerp;
            var p2 = v1 - leftPerp;
            var p3 = v0 - leftPerp;

            wireColor.a = alpha;
            DrawQuad(mgc, p0, p1, p2, p3, wireColor);
        }

        protected void DrawRect(MeshGenerationContext mgc, Rect rect, Color color, float alpha)
        {
            if (mgc == null)
                throw new NullReferenceException("The MeshGenerationContext is null");

            color.a = alpha;

            var playModeTintColor = mgc.visualElement?.playModeTintColor ?? Color.white;
            var rectParams = UnityEngine.UIElements.UIR.MeshGenerator.RectangleParams.MakeSolid(rect, color, playModeTintColor);
            mgc.meshGenerator.DrawRectangle(rectParams);
        }

        protected void DrawBorder(MeshGenerationContext mgc, Rect rect, Color color, float alpha)
        {
            if (mgc == null)
                throw new NullReferenceException("The MeshGenerationContext is null");

            color.a = alpha;
            rect.xMin++;
            rect.xMax--;
            rect.yMin++;
            rect.yMax--;
            var width = rect.xMax - rect.xMin;
            var height = rect.yMax - rect.yMin;

            var topRect = new Rect(rect.xMin, rect.yMin, width, 1);
            var bottomRect = new Rect(rect.xMin, rect.yMax, width, 1);
            var rightRect = new Rect(rect.xMax, rect.yMin, 1, height);
            var lefRect = new Rect(rect.xMin, rect.yMin, 1, height);

            var playModeTintColor = mgc.visualElement?.playModeTintColor ?? Color.white;
            var rectParams = UnityEngine.UIElements.UIR.MeshGenerator.RectangleParams.MakeSolid(topRect, color, playModeTintColor);
            mgc.meshGenerator.DrawRectangle(rectParams);

            rectParams = UnityEngine.UIElements.UIR.MeshGenerator.RectangleParams.MakeSolid(bottomRect, color, playModeTintColor);
            mgc.meshGenerator.DrawRectangle(rectParams);

            rectParams = UnityEngine.UIElements.UIR.MeshGenerator.RectangleParams.MakeSolid(rightRect, color, playModeTintColor);
            mgc.meshGenerator.DrawRectangle(rectParams);

            rectParams = UnityEngine.UIElements.UIR.MeshGenerator.RectangleParams.MakeSolid(lefRect, color, playModeTintColor);
            mgc.meshGenerator.DrawRectangle(rectParams);
        }
    }

    internal partial class HighlightOverlayPainter : BaseOverlayPainter
    {
        internal static readonly PrefColor kHighlightContentColor = new PrefColor("UI Toolkit Debugger/Highlight Content", 0.1f, 0.6f, 0.9f, 0.4f, 0.1f, 0.6f, 0.9f, 0.4f);
        internal static readonly PrefColor kHighlightPaddingColor = new PrefColor("UI Toolkit Debugger/Highlight Padding", 0.1f, 0.9f, 0.1f, 0.4f, 0.1f, 0.9f, 0.1f, 0.4f);
        internal static readonly PrefColor kHighlightBorderColor = new PrefColor("UI Toolkit Debugger/Highlight Border", 1.0f, 1.0f, 0.4f, 0.4f, 1.0f, 1.0f, 0.4f, 0.4f);
        internal static readonly PrefColor kHighlightMarginColor = new PrefColor("UI Toolkit Debugger/Highlight Margin", 1.0f, 0.6f, 0.0f, 0.4f, 1.0f, 0.6f, 0.0f, 0.4f);

        [OnCodeInitializing]
        static void Init()
        {
            // Intentionally left empty to trigger the `PrefColor` registration.
        }

        private const float kDefaultHighlightAlpha = 0.4f;

        private Rect[] m_MarginRects = new Rect[4];
        private Rect[] m_BorderRects = new Rect[4];
        private Rect[] m_PaddingRects = new Rect[4];

        public void AddOverlay(VisualElement ve, OverlayContent content = OverlayContent.All, float alpha = kDefaultHighlightAlpha)
        {
            OverlayData overlayData = null;
            if (!m_OverlayData.TryGetValue(ve, out overlayData))
            {
                overlayData = new OverlayData(ve, alpha);
                m_OverlayData[ve] = overlayData;
            }

            overlayData.content = content;
        }

        protected override void DrawOverlayData(MeshGenerationContext mgc, OverlayData od)
        {
            DrawHighlights(mgc, od);
        }

        private void DrawHighlights(MeshGenerationContext mgc, OverlayData od)
        {
            var ve = od.element;

            FillHighlightRects(od.element);

            var contentFlag = od.content;
            if ((contentFlag & OverlayContent.Content) == OverlayContent.Content)
            {
                DrawElementRect(mgc, ve, ve.contentRect, kHighlightContentColor.Color, kHighlightContentColor.Color.a);
            }

            if ((contentFlag & OverlayContent.Padding) == OverlayContent.Padding)
            {
                for (int i = 0; i < 4; i++)
                {
                    DrawElementRect(mgc, ve, m_PaddingRects[i], kHighlightPaddingColor.Color, kHighlightPaddingColor.Color.a);
                }
            }

            if ((contentFlag & OverlayContent.Border) == OverlayContent.Border)
            {
                for (int i = 0; i < 4; i++)
                {
                    DrawElementRect(mgc, ve, m_BorderRects[i], kHighlightBorderColor.Color, kHighlightBorderColor.Color.a);
                }
            }

            if ((contentFlag & OverlayContent.Margin) == OverlayContent.Margin)
            {
                for (int i = 0; i < 4; i++)
                {
                    DrawElementRect(mgc, ve, m_MarginRects[i], kHighlightMarginColor.Color, kHighlightMarginColor.Color.a);
                }
            }
        }

        // Fills the padding/border/margin rects in the element's local coordinates.
        private void FillHighlightRects(VisualElement ve)
        {
            var style = ve.resolvedStyle;
            Rect contentRect = ve.contentRect;

            // Paddings
            float paddingLeft = style.paddingLeft;
            float paddingRight = style.paddingRight;
            float paddingBottom = style.paddingBottom;
            float paddingTop = style.paddingTop;

            Rect paddingLeftRect = Rect.zero;
            Rect paddingRightRect = Rect.zero;
            Rect paddingBottomRect = Rect.zero;
            Rect paddingTopRect = Rect.zero;

            paddingLeftRect = new Rect(contentRect.xMin - paddingLeft, contentRect.yMin,
                paddingLeft, contentRect.height);

            paddingRightRect = new Rect(contentRect.xMax, contentRect.yMin,
                paddingRight, contentRect.height);

            paddingTopRect = new Rect(contentRect.xMin - paddingLeft, contentRect.yMin - paddingTop,
                contentRect.width + paddingLeft + paddingRight, paddingTop);

            paddingBottomRect = new Rect(contentRect.xMin - paddingLeft, contentRect.yMax,
                contentRect.width + paddingLeft + paddingRight, paddingBottom);

            m_PaddingRects[0] = paddingLeftRect;
            m_PaddingRects[1] = paddingRightRect;
            m_PaddingRects[2] = paddingTopRect;
            m_PaddingRects[3] = paddingBottomRect;

            // Borders
            float borderLeft = style.borderLeftWidth;
            float borderRight = style.borderRightWidth;
            float borderBottom = style.borderBottomWidth;
            float borderTop = style.borderTopWidth;

            Rect borderLeftRect = Rect.zero;
            Rect borderRightRect = Rect.zero;
            Rect borderBottomRect = Rect.zero;
            Rect borderTopRect = Rect.zero;

            borderLeftRect = new Rect(paddingLeftRect.xMin - borderLeft, paddingTopRect.yMin,
                borderLeft, paddingLeftRect.height + paddingBottomRect.height + paddingTopRect.height);

            borderRightRect = new Rect(paddingRightRect.xMax, paddingTopRect.yMin,
                borderRight, paddingRightRect.height + paddingBottomRect.height + paddingTopRect.height);

            borderTopRect = new Rect(paddingTopRect.xMin - borderLeft, paddingTopRect.yMin - borderTop,
                paddingTopRect.width + borderLeft + borderRight, borderTop);

            borderBottomRect = new Rect(paddingBottomRect.xMin - borderLeft, paddingBottomRect.yMax,
                paddingBottomRect.width + borderLeft + borderRight, borderBottom);

            m_BorderRects[0] = borderLeftRect;
            m_BorderRects[1] = borderRightRect;
            m_BorderRects[2] = borderTopRect;
            m_BorderRects[3] = borderBottomRect;

            // Margins
            float marginLeft = style.marginLeft;
            float marginRight = style.marginRight;
            float marginBotton = style.marginBottom;
            float marginTop = style.marginTop;

            Rect marginLeftRect = Rect.zero;
            Rect marginRightRect = Rect.zero;
            Rect marginBottomRect = Rect.zero;
            Rect marginTopRect = Rect.zero;

            marginLeftRect = new Rect(borderLeftRect.xMin - marginLeft, borderTopRect.yMin,
                marginLeft, borderLeftRect.height + borderBottomRect.height + borderTopRect.height);

            marginRightRect = new Rect(borderRightRect.xMax, borderTopRect.yMin,
                marginRight, borderRightRect.height + borderBottomRect.height + borderTopRect.height);

            marginTopRect = new Rect(borderTopRect.xMin - marginLeft, borderTopRect.yMin - marginTop,
                borderTopRect.width + marginLeft + marginRight, marginTop);

            marginBottomRect = new Rect(borderBottomRect.xMin - marginLeft, borderBottomRect.yMax,
                borderBottomRect.width + marginLeft + marginRight, marginBotton);

            m_MarginRects[0] = marginLeftRect;
            m_MarginRects[1] = marginRightRect;
            m_MarginRects[2] = marginTopRect;
            m_MarginRects[3] = marginBottomRect;
        }
    }

    internal class RepaintOverlayPainter : BaseOverlayPainter
    {
        internal static readonly PrefColor kRepaintColor = new PrefColor("UI Toolkit Debugger/Repaint Overlay", 0f, 1f, 0f, 1f, 0f, 1f, 0f, 1f);
        internal static readonly PrefColor kRepaintOutlineColor = new PrefColor("UI Toolkit Debugger/Repaint Overlay Outline", 0f, 1f, 0f, 1f, 0f, 1f, 0f, 1f);
        private static readonly float kDefaultAlpha = 1.0f;
        private static readonly int kOverlayFadeOutDuration = 500;

        public void AddOverlay(VisualElement ve, VisualElement debugContainer)
        {
            if (debugContainer == null)
                throw new ArgumentNullException("debugContainer");
            if (ve == null)
                throw new ArgumentNullException("ve");

            OverlayData overlayData = null;
            if (!m_OverlayData.TryGetValue(ve, out overlayData))
            {
                overlayData = new OverlayData(ve, kDefaultAlpha);
                m_OverlayData[ve] = overlayData;
            }
            overlayData.StartFadeOutAnimation(debugContainer, kOverlayFadeOutDuration);
        }

        protected override void DrawOverlayData(MeshGenerationContext mgc, OverlayData od)
        {
            DrawElementRect(mgc, od.element, od.element.rect, kRepaintColor.Color, kRepaintColor.Color.a);
            DrawElementBorder(mgc, od.element, od.element.rect, kRepaintOutlineColor.Color, kRepaintOutlineColor.Color.a * 4);
        }
    }

    internal class LayoutOverlayPainter : BaseOverlayPainter
    {
        internal static readonly PrefColor kBoundColor = new PrefColor("UI Toolkit Debugger/Layout Overlay Outline", 0.5f, 0.5f, 0.5f, 1f, 0.5f, 0.5f, 0.5f, 1f);
        internal static readonly PrefColor kSelectedBoundColor = new PrefColor("UI Toolkit Debugger/Layout Overlay Outline Selected", 0f, 1f, 0f, 1f, 0f, 1f, 0f, 1f);

        private static readonly float kDefaultAlpha = 1.0f;

        public VisualElement selectedElement;

        public void AddOverlay(VisualElement ve)
        {
            if (ve == null)
                throw new ArgumentNullException("ve");

            OverlayData overlayData = null;
            if (!m_OverlayData.TryGetValue(ve, out overlayData))
            {
                overlayData = new OverlayData(ve, kDefaultAlpha);
                m_OverlayData[ve] = overlayData;
            }
        }

        protected override void DrawContent(MeshGenerationContext mgc)
        {
            base.DrawContent(mgc);

            if (selectedElement == null)
                return;

            var color = kSelectedBoundColor.Color;
            DrawElementBorder(mgc, selectedElement, selectedElement.rect, color, color.a);
        }

        protected override void DrawOverlayData(MeshGenerationContext mgc, OverlayData od)
        {
            var color = kBoundColor.Color;
            DrawElementBorder(mgc, od.element, od.element.rect, color, color.a);
        }
    }

    internal class WireframeOverlayPainter : BaseOverlayPainter
    {
        private static readonly float kDefaultAlpha = 1.0f;
        private static readonly PrefColor kUnselectedColor = new PrefColor("UI Toolkit Debugger/Wireframe", 0.5f, 0.5f, 0.5f, 1f, 0.5f, 0.5f, 0.5f, 1f);
        private static readonly PrefColor kSelectedColor = new PrefColor("UI Toolkit Debugger/Wireframe Selected", 1.0f, 1.0f, 0.0f, 1.0f, 1.0f, 1.0f, 0.0f, 1.0f);

        public VisualElement selectedElement;

        public void AddOverlay(VisualElement ve)
        {
            if (ve == null)
                throw new ArgumentNullException("ve");

            OverlayData overlayData = null;
            if (!m_OverlayData.TryGetValue(ve, out overlayData))
            {
                overlayData = new OverlayData(ve, kDefaultAlpha);
                m_OverlayData[ve] = overlayData;
            }
        }

        protected override void DrawContent(MeshGenerationContext mgc)
        {
            base.DrawContent(mgc);

            if (selectedElement != null)
                DrawWireframe(mgc, selectedElement, kSelectedColor.Color, kSelectedColor.Color.a);
        }

        protected override void DrawOverlayData(MeshGenerationContext mgc, OverlayData od)
        {
            DrawWireframe(mgc, od.element, kUnselectedColor.Color, kUnselectedColor.Color.a);
        }

        void DrawWireframe(MeshGenerationContext mgc, VisualElement ve, Color wireColor, float alpha)
        {
            var verts = new List<Vector3>(64);
            var cmd = ve.renderData.firstHeadCommand;

            // Mesh vertices are stored in element-local space; the transforms (ElementInfo x/y
            // offset, bone matrix and render-tree transform) are applied at render time and
            // compose into the element's worldTransform (local to document space).
            var toDocument = ve.worldTransform;

            while (cmd != null && cmd.owner.owner == ve)
            {
                if (cmd.type == UnityEngine.UIElements.UIR.CommandType.Draw)
                {
                    var allocPage = cmd.mesh.allocPage;
                    var indexSlice = allocPage.indices.cpuData.SliceAs<ushort>();
                    var vertSlice = allocPage.vertices.cpuData.SliceAs<Vertex>();
                    for (int i = 0; i < cmd.indexCount; ++i)
                    {
                        var index = indexSlice[(int)cmd.mesh.allocIndices.start + cmd.indexOffset + i];
                        var vert = vertSlice[index];
                        verts.Add(toDocument.MultiplyPoint3x4(vert.position));
                    }
                }
                cmd = cmd.next;
            }

            // The verts are in document space at this point; bring them to world space before projection.
            if (projectionActive)
            {
                if (!TryGetElementProjection(ve, out var documentToWorld, out var camera))
                    return;

                for (int i = 0; i < verts.Count; i++)
                    verts[i] = documentToWorld.MultiplyPoint3x4(verts[i]);

                var oldPixelRect = BeginCameraScreenProjection(camera);
                DrawTriangles(mgc, verts, camera, wireColor, alpha);
                EndCameraScreenProjection(camera, oldPixelRect);
            }
            else
            {
                DrawTriangles(mgc, verts, null, wireColor, alpha);
            }
        }

        void DrawTriangles(MeshGenerationContext mgc, List<Vector3> verts, Camera camera, Color wireColor, float alpha)
        {
            var count = verts.Count;
            for (int i = 0; i < count; i += 3)
            {
                if (!TryGetOverlayPoint(verts[i], camera, out var v0) ||
                    !TryGetOverlayPoint(verts[i + 1], camera, out var v1) ||
                    !TryGetOverlayPoint(verts[i + 2], camera, out var v2))
                    continue;

                DrawLine(mgc, v0, v1, wireColor, alpha);
                DrawLine(mgc, v1, v2, wireColor, alpha);
                DrawLine(mgc, v2, v0, wireColor, alpha);
            }
        }

        bool TryGetOverlayPoint(Vector3 vert, Camera camera, out Vector2 overlayPoint)
        {
            if (!projectionActive)
            {
                overlayPoint = vert;
                return true;
            }

            return TryProjectPoint(camera, vert, out overlayPoint);
        }
    }
}
