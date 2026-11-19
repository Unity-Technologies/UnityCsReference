// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Debugger
{
    // Shows the selected element's rendered pixels at an integer zoom, one source pixel per zoom x zoom device pixels.
    internal class PixelMagnifier : VisualElement
    {
        internal const string ussClassName = "unity-debugger-pixel-magnifier";
        internal const string headerUssClassName = ussClassName + "__header";
        internal const string zoomSliderUssClassName = ussClassName + "__zoom-slider";
        internal const string viewportUssClassName = ussClassName + "__viewport";
        internal const string imageUssClassName = ussClassName + "__image";
        internal const string messageUssClassName = ussClassName + "__message";
        internal const string hiddenUssClassName = ussClassName + "--hidden";

        internal const int k_MinZoom = 1;
        internal const int k_MaxZoom = 32;
        internal const int k_MaxCaptureSize = 1024;

        readonly DebuggerContext m_Context;
        readonly SliderInt m_ZoomSlider;
        readonly ScrollView m_Viewport;
        readonly Image m_Image;
        readonly Label m_Message;

        RenderTexture m_Capture;
        bool m_Active;
        bool m_Subscribed;
        bool m_FitPending = true;
        float m_ImagePixelsPerPoint;

        internal RenderTexture capture => m_Capture;
        internal Image image => m_Image;

        public PixelMagnifier(DebuggerContext context)
        {
            m_Context = context;
            AddToClassList(ussClassName);

            var header = new VisualElement();
            header.AddToClassList(headerUssClassName);
            Add(header);

            m_ZoomSlider = new SliderInt("Zoom", k_MinZoom, k_MaxZoom) { showInputField = true };
            m_ZoomSlider.AddToClassList(zoomSliderUssClassName);
            m_ZoomSlider.SetValueWithoutNotify(m_Context.magnifierZoom);
            m_ZoomSlider.RegisterValueChangedCallback(e => SetZoom(e.newValue));
            header.Add(m_ZoomSlider);

            header.Add(new Button(Fit) { text = "Fit", tooltip = "Largest zoom that fits the whole element" });

            m_Viewport = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            m_Viewport.AddToClassList(viewportUssClassName);
            Add(m_Viewport);

            m_Image = new Image { scaleMode = ScaleMode.StretchToFill };
            m_Image.AddToClassList(imageUssClassName);
            m_Viewport.Add(m_Image);

            m_Message = new Label();
            m_Message.AddToClassList(messageUssClassName);
            Add(m_Message);

            m_Context.selection.onSelectedElementChanged += OnSelectionChanged;
            m_Context.selection.onPanelDebugChanged += _ => OnSelectionChanged(null);

            RegisterCallback<AttachToPanelEvent>(_ => OnAttachToPanel());
            RegisterCallback<DetachFromPanelEvent>(_ => SetSubscribed(false));
            RegisterCallback<GeometryChangedEvent>(_ => UpdateImageSize());

            UpdateMessage();
        }

        void OnAttachToPanel()
        {
            UpdateSubscription();
            RequestCapture();
        }

        internal void SetActive(bool active)
        {
            if (m_Active == active)
                return;
            m_Active = active;
            UpdateSubscription();
            RequestCapture();
        }

        void SetZoom(int zoom)
        {
            m_Context.magnifierZoom = Mathf.Clamp(zoom, k_MinZoom, k_MaxZoom);
            m_ZoomSlider.SetValueWithoutNotify(m_Context.magnifierZoom);
            UpdateImageSize();
        }

        void Fit()
        {
            m_FitPending = true;
            TryApplyFit();
        }

        void OnSelectionChanged(VisualElement element)
        {
            m_FitPending = true;
            ReleaseCapture();
            RequestCapture();
        }

        void RequestCapture()
        {
            UpdateMessage();
            if (m_Active && CanCapture(m_Context.selection.element))
                m_Context.selection.element.MarkDirtyRepaint();
        }

        void UpdateSubscription() => SetSubscribed(m_Active && panel != null);

        void SetSubscribed(bool subscribed)
        {
            if (m_Subscribed == subscribed)
                return;
            m_Subscribed = subscribed;
            if (subscribed)
            {
                Panel.afterRepaint += OnAfterRepaint;
            }
            else
            {
                Panel.afterRepaint -= OnAfterRepaint;
                ReleaseCapture();
            }
        }

        bool CanCapture(VisualElement element)
        {
            // Capturing the magnifier's own panel would schedule another repaint of it from inside its repaint.
            return element != null && element.IsLive() && element.panel != null && element.panel != panel;
        }

        void OnAfterRepaint(Panel repaintedPanel)
        {
            if (repaintedPanel == panel)
            {
                // Moving to a display with another scale changes pixelsPerPoint without a GeometryChangedEvent.
                if (m_Capture != null && repaintedPanel.scaledPixelsPerPoint != m_ImagePixelsPerPoint)
                    UpdateImageSize();
                return;
            }

            var element = m_Context.selection.element;
            if (!m_Active || repaintedPanel != m_Context.selection.panel || !CanCapture(element))
                return;

            Capture(element);
        }

        internal bool Capture(VisualElement element)
        {
            var sourcePanel = element.elementPanel;
            float sourcePixelsPerPoint = sourcePanel.scaledPixelsPerPoint;
            var worldBound = element.worldBound;
            int width = Mathf.Clamp(Mathf.RoundToInt(worldBound.width * sourcePixelsPerPoint), 1, k_MaxCaptureSize);
            int height = Mathf.Clamp(Mathf.RoundToInt(worldBound.height * sourcePixelsPerPoint), 1, k_MaxCaptureSize);

            EnsureCapture(width, height, VisualElementCaptureExtensions.GetCaptureReadWrite(sourcePanel));
            if (!VisualElementCaptureExtensions.TryCaptureIntoRenderTextureWithoutRepaint(element, m_Capture))
            {
                ReleaseCapture();
                UpdateMessage("The selected element's panel draws into cameras and can't be captured.");
                return false;
            }

            if (m_Image.image != m_Capture)
                m_Image.image = m_Capture;
            UpdateMessage();
            UpdateImageSize();
            TryApplyFit();
            m_Image.MarkDirtyRepaint();
            return true;
        }

        void EnsureCapture(int width, int height, RenderTextureReadWrite readWrite)
        {
            bool sRGB = readWrite != RenderTextureReadWrite.Linear && QualitySettings.activeColorSpace == ColorSpace.Linear;
            if (m_Capture != null && m_Capture.width == width && m_Capture.height == height && m_Capture.sRGB == sRGB)
                return;

            ReleaseCapture();
            m_Capture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, readWrite)
            {
                name = "UI Toolkit Debugger Magnifier",
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };
            m_Capture.Create();
        }

        internal void ReleaseCapture()
        {
            m_Image.image = null;
            if (m_Capture == null)
                return;
            m_Capture.Release();
            UnityEngine.Object.DestroyImmediate(m_Capture);
            m_Capture = null;
            UpdateImageSize();
        }

        void UpdateImageSize()
        {
            if (m_Capture == null || panel == null)
            {
                m_Image.style.width = StyleKeyword.Null;
                m_Image.style.height = StyleKeyword.Null;
                return;
            }

            // The zoom is in device pixels, so it stays exact whatever the scale of either panel.
            float pixelsPerPoint = ((BaseVisualElementPanel)panel).scaledPixelsPerPoint;
            m_ImagePixelsPerPoint = pixelsPerPoint;
            int zoom = m_Context.magnifierZoom;
            m_Image.style.width = m_Capture.width * zoom / pixelsPerPoint;
            m_Image.style.height = m_Capture.height * zoom / pixelsPerPoint;
        }

        void TryApplyFit()
        {
            if (!m_FitPending || m_Capture == null || panel == null)
                return;

            // The whole scroll view, not its content viewport, so scrollbars shown at the current zoom don't shrink the fit.
            var viewportSize = m_Viewport.layout.size;
            if (float.IsNaN(viewportSize.x) || float.IsNaN(viewportSize.y) || viewportSize.x <= 0 || viewportSize.y <= 0)
                return;

            m_FitPending = false;
            float pixelsPerPoint = ((BaseVisualElementPanel)panel).scaledPixelsPerPoint;
            float widthZoom = viewportSize.x * pixelsPerPoint / m_Capture.width;
            float heightZoom = viewportSize.y * pixelsPerPoint / m_Capture.height;
            SetZoom(Mathf.FloorToInt(Mathf.Min(widthZoom, heightZoom)));
        }

        void UpdateMessage(string message = null)
        {
            if (message == null && m_Capture == null)
            {
                var element = m_Context.selection.element;
                if (element == null)
                    message = "Select an element to magnify.";
                else if (element.panel == panel)
                    message = "The UI Toolkit Debugger can't magnify itself.";
            }

            bool hasMessage = !string.IsNullOrEmpty(message);
            m_Message.text = message;
            m_Message.EnableInClassList(hiddenUssClassName, !hasMessage);
            m_Viewport.EnableInClassList(hiddenUssClassName, hasMessage);
        }
    }
}
