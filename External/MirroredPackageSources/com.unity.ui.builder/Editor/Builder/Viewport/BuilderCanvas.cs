using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal enum BuilderCanvasBackgroundMode
    {
        Color,
        Image,
        Camera
    };

    internal class BuilderCanvas : VisualElement
    {
        const string k_ActiveHandleClassName = "unity-builder-canvas--active";
        const string k_HighlightedClassName = "unity-builder-canvas--highlighted";

        VisualElement m_Header;
        VisualElement m_Container;
        Rect m_ThisRectOnStartDrag;
        VisualElement m_DragHoverCoverLayer;
        IVisualElementScheduledItem m_CanvasToGameViewSizeUpdater;

        VisualElement m_DefaultBackgroundElement;
        VisualElement m_CustomBackgroundElement;
        VisualElement m_CheckerboardBackgroundElement;

        Dictionary<string, VisualElement> m_HandleElements;

        public new class UxmlFactory : UxmlFactory<BuilderCanvas, UxmlTraits> {}

        public VisualElement header => m_Header;
        public override VisualElement contentContainer => m_Container == null ? this : m_Container;

        public Label titleLabel { get; }
        public Label editorExtensionsLabel { get; }
        public VisualElement defaultBackgroundElement => m_DefaultBackgroundElement;
        public VisualElement customBackgroundElement => m_CustomBackgroundElement;
        public VisualElement checkerboardBackgroundElement => m_CheckerboardBackgroundElement;

        BuilderDocument m_Document;
        public BuilderDocument document
        {
            get { return m_Document; }
            set
            {
                if (value == m_Document)
                    return;

                m_Document = value;
                SetSizeFromDocumentSettings();
            }
        }

        BuilderSelection m_Selection;

        public void SetSelection(BuilderSelection selection)
        {
            m_Selection = selection;
        }

        public VisualElement documentRootElement { get; set; }
        public VisualElement editorLayer { get; set; }

        private float m_X;
        private float m_Y;
        private float m_Width;
        private float m_Height;
        private float m_ZoomScale = 1.0f;
        private bool m_MatchGameView = false;

        public float x
        {
            get
            {
                return m_X;
            }
            set
            {
                if (m_X == value)
                    return;

                m_X = value;
                if (document != null)
                {
                    document.settings.CanvasX = (int)value;
                    document.SaveSettingsToDisk();
                }

                UpdateRenderSize();
            }
        }
        public float y
        {
            get
            {
                return m_Y;
            }
            set
            {
                if (m_Y == value)
                    return;

                m_Y = value;
                if (document != null)
                {
                    document.settings.CanvasY = (int)value;
                    document.SaveSettingsToDisk();
                }

                UpdateRenderSize();
            }
        }
        public float width
        {
            get
            {
                return m_Width;
            }
            set
            {
                if (m_Width == value)
                    return;

                m_Width = value;
                if (document != null)
                {
                    document.settings.CanvasWidth = (int)value;
                    document.SaveSettingsToDisk();
                }

                UpdateRenderSize();
            }
        }
        public float height
        {
            get
            {
                return m_Height;
            }
            set
            {
                if (m_Height == value)
                    return;

                m_Height = value;
                if (document != null)
                {
                    document.settings.CanvasHeight = (int)value;
                    document.SaveSettingsToDisk();
                }

                UpdateRenderSize();
            }
        }

        public bool matchGameView
        {
            get => m_MatchGameView;
            set
            {
                if (m_MatchGameView == value)
                    return;

                m_MatchGameView = value;
                if (document != null)
                {
                    document.settings.MatchGameView = value;
                    document.SaveSettingsToDisk();
                }
                m_Selection?.NotifyOfStylingChange();

                if (m_MatchGameView)
                {
                    if (m_CanvasToGameViewSizeUpdater != null)
                        return;

                    m_CanvasToGameViewSizeUpdater = schedule.Execute(() =>
                    {
                        if (matchGameView)
                        {
                            width = GameView.GetSizeOfMainGameView().x;
                            height = GameView.GetSizeOfMainGameView().y;
                        }
                    });
                    m_CanvasToGameViewSizeUpdater.Every(BuilderConstants.CanvasGameViewSyncInterval);
                }
                else
                {
                    m_CanvasToGameViewSizeUpdater?.Pause();
                    m_CanvasToGameViewSizeUpdater = null;
                }
            }
        }

        public float zoomScale
        {
            get { return m_ZoomScale; }
            set
            {
                if (m_ZoomScale == value)
                    return;

                m_ZoomScale = value;
                UpdateRenderSize();
            }
        }

        void UpdateRenderSize()
        {
            var newWidth = m_Width * m_ZoomScale;
            var newHeight = m_Height * m_ZoomScale;

            style.left = m_X * m_ZoomScale;
            style.top = m_Y * m_ZoomScale;
            style.width = newWidth;
            style.height = newHeight;

            if (documentRootElement != null)
            {
                documentRootElement.transform.scale = new Vector3(m_ZoomScale, m_ZoomScale, 1);
                documentRootElement.style.right = -(m_Width - newWidth);
                documentRootElement.style.bottom = -(m_Height - newHeight);
            }

            if (editorLayer != null)
            {
                editorLayer.transform.scale = new Vector3(m_ZoomScale, m_ZoomScale, 1);
                editorLayer.style.right = -(m_Width - newWidth);
                editorLayer.style.bottom = -(m_Height - newHeight);
            }
        }

        public BuilderCanvas()
        {
            var builderTemplate = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.UIBuilderPackagePath + "/BuilderCanvas.uxml");
            builderTemplate.CloneTree(this);

            m_Container = this.Q("content-container");
            m_Header = this.Q("header-container");
            titleLabel = this.Q<Label>("title");
            editorExtensionsLabel = this.Q<Label>("tag");

            m_HandleElements = new Dictionary<string, VisualElement>();

            m_HandleElements.Add("left-handle", this.Q("left-handle"));
            m_HandleElements.Add("bottom-handle", this.Q("bottom-handle"));
            m_HandleElements.Add("right-handle", this.Q("right-handle"));
            m_HandleElements.Add("top-handle", this.Q("top-handle"));

            m_HandleElements.Add("bottom-left-handle", this.Q("bottom-left-handle"));
            m_HandleElements.Add("bottom-right-handle", this.Q("bottom-right-handle"));
            m_HandleElements.Add("top-left-handle", this.Q("top-left-handle"));
            m_HandleElements.Add("top-right-handle", this.Q("top-right-handle"));

            m_HandleElements["left-handle"].AddManipulator(new Manipulator(OnStartDrag, OnEndDrag, OnDragLeft));
            m_HandleElements["bottom-handle"].AddManipulator(new Manipulator(OnStartDrag, OnEndDrag, OnDragBottom));
            m_HandleElements["right-handle"].AddManipulator(new Manipulator(OnStartDrag, OnEndDrag, OnDragRight));
            m_HandleElements["top-handle"].AddManipulator(new Manipulator(OnStartDrag, OnEndDrag, OnDragTop));

            m_HandleElements["bottom-left-handle"].AddManipulator(new Manipulator(OnStartDrag, OnEndDrag, OnDragBottomLeft));
            m_HandleElements["bottom-right-handle"].AddManipulator(new Manipulator(OnStartDrag, OnEndDrag, OnDragBottomRight));
            m_HandleElements["top-left-handle"].AddManipulator(new Manipulator(OnStartDrag, OnEndDrag, OnDragTopLeft));
            m_HandleElements["top-right-handle"].AddManipulator(new Manipulator(OnStartDrag, OnEndDrag, OnDragTopRight));

            m_DragHoverCoverLayer = this.Q("drag-hover-cover-layer");

            SetSizeFromDocumentSettings();

            m_DefaultBackgroundElement = this.Q("default-background-element");
            m_CustomBackgroundElement = this.Q("custom-background-element");
            m_CheckerboardBackgroundElement = this.Q("checkerboard-background-container");
        }

        public void SetHighlighted(bool enabled)
        {
            EnableInClassList(k_HighlightedClassName, enabled);
        }

        public void SetSizeFromDocumentSettings()
        {
            if (document == null || document.settings.CanvasWidth < BuilderConstants.CanvasMinWidth)
            {
                ResetSize();
                return;
            }
            x = document.settings.CanvasX;
            y = document.settings.CanvasY;
            width = document.settings.CanvasWidth;
            height = document.settings.CanvasHeight;
            matchGameView = document.settings.MatchGameView;
        }

        public void ResetSize()
        {
            x = y = 0.0f;
            width = BuilderConstants.CanvasInitialWidth;
            height = BuilderConstants.CanvasInitialHeight;
        }

        void OnStartDrag(VisualElement handle)
        {
            m_ThisRectOnStartDrag = new Rect(x, y, width * zoomScale, height * zoomScale);

            m_DragHoverCoverLayer.style.display = DisplayStyle.Flex;
            m_DragHoverCoverLayer.style.cursor = handle.computedStyle.cursor;

            if (matchGameView)
            {
                Builder.ShowWarning(BuilderConstants.DocumentMatchGameViewModeDisabled);
                matchGameView = false;
            }
        }

        void OnEndDrag()
        {
            m_DragHoverCoverLayer.style.display = DisplayStyle.None;
            m_DragHoverCoverLayer.RemoveFromClassList(k_ActiveHandleClassName);
        }

        void OnDragLeft(Vector2 diff)
        {
            var newWidth = (m_ThisRectOnStartDrag.width - diff.x) / m_ZoomScale;
            var oldWidth = width;

            newWidth = Mathf.Max(newWidth, BuilderConstants.CanvasMinWidth);
            width = newWidth;
            x -= width - oldWidth;
        }

        void OnDragBottom(Vector2 diff)
        {
            var newHeight = m_ThisRectOnStartDrag.height + diff.y;
            newHeight = Mathf.Max(newHeight, BuilderConstants.CanvasMinHeight);
            height = newHeight / m_ZoomScale;
        }

        void OnDragRight(Vector2 diff)
        {
            var newWidth = m_ThisRectOnStartDrag.width + diff.x;
            newWidth = Mathf.Max(newWidth, BuilderConstants.CanvasMinWidth);
            width = newWidth / m_ZoomScale;
        }

        void OnDragTop(Vector2 diff)
        {
            var oldHeight = height;
            var newHeight = (m_ThisRectOnStartDrag.height - diff.y) / m_ZoomScale;

            newHeight = Mathf.Max(newHeight, BuilderConstants.CanvasMinHeight);
            height = newHeight;
            y -= height - oldHeight;
        }

        void OnDragBottomLeft(Vector2 diff)
        {
            OnDragBottom(diff);
            OnDragLeft(diff);
        }

        void OnDragBottomRight(Vector2 diff)
        {
            OnDragBottom(diff);
            OnDragRight(diff);
        }

        void OnDragTopLeft(Vector2 diff)
        {
            OnDragTop(diff);
            OnDragLeft(diff);
        }

        void OnDragTopRight(Vector2 diff)
        {
            OnDragTop(diff);
            OnDragRight(diff);
        }

        class Manipulator : MouseManipulator
        {
            Vector2 m_Start;
            protected bool m_Active;

            Action<VisualElement> m_StartDrag;
            Action m_EndDrag;
            Action<Vector2> m_DragAction;

            public Manipulator(Action<VisualElement> startDrag, Action endDrag, Action<Vector2> dragAction)
            {
                m_StartDrag = startDrag;
                m_EndDrag = endDrag;
                m_DragAction = dragAction;
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
                m_Active = false;
            }

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<MouseDownEvent>(OnMouseDown);
                target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
                target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            }

            protected void OnMouseDown(MouseDownEvent e)
            {
                if (m_Active)
                {
                    e.StopImmediatePropagation();
                    return;
                }

                if (CanStartManipulation(e))
                {
                    m_StartDrag(target);
                    m_Start = e.mousePosition;

                    m_Active = true;
                    target.CaptureMouse();
                    e.StopPropagation();

                    target.AddToClassList(k_ActiveHandleClassName);
                }
            }

            protected void OnMouseMove(MouseMoveEvent e)
            {
                if (!m_Active || !target.HasMouseCapture())
                    return;

                Vector2 diff = e.mousePosition - m_Start;

                m_DragAction(diff);

                e.StopPropagation();
            }

            protected void OnMouseUp(MouseUpEvent e)
            {
                if (!m_Active || !target.HasMouseCapture() || !CanStopManipulation(e))
                    return;

                m_Active = false;
                target.ReleaseMouse();
                e.StopPropagation();
                m_EndDrag();

                target.RemoveFromClassList(k_ActiveHandleClassName);
            }
        }
    }
}
