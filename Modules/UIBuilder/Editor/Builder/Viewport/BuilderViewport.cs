// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements.Bindings;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace Unity.UI.Builder
{
    internal class BuilderViewport : BuilderPaneContent, IBuilderSelectionNotifier, IShortcutToolContext
    {
        static readonly string s_PreviewModeClassName = "unity-builder-viewport--preview";
        static readonly float s_CanvasViewportMinWidthDiff = 30;
        static readonly float s_CanvasViewportMinHeightDiff = 36;

        BuilderPaneWindow m_PaneWindow;

        VisualElement m_Toolbar;
        VisualElement m_ViewportWrapper;
        VisualElement m_Viewport;
        VisualElement m_Surface;
        BuilderCanvas m_Canvas;
        VisualElement m_SharedStylesAndDocumentElement;
        VisualElement m_StyleSelectorElementContainer;
        VisualElement m_DocumentRootElement;
        VisualElement m_EditorLayer;
        TextField m_TextEditor;
        VisualElement m_PickOverlay;
        VisualElement m_HighlightOverlay;
        BuilderParentTracker m_BuilderParentTracker;
        BuilderSelectionIndicator m_BuilderSelectionIndicator;
        BuilderPlacementIndicator m_BuilderPlacementIndicator;
        BuilderResizer m_BuilderResizer;
        BuilderMover m_BuilderMover;
        BuilderZoomer m_BuilderZoomer;
        BuilderPanner m_BuilderPanner;
        BuilderViewportDragger m_BuilderViewportDragger;
        CheckerboardBackground m_CheckerboardBackground;
        BuilderNotifications m_Notifications;

        BuilderSelection m_Selection;
        BuilderElementContextMenu m_ContextMenuManipulator;

        List<VisualElement> m_PickedElements = new List<VisualElement>();

        internal const int k_AnimationDuration = 250;
        ValueAnimation<float> m_ZoomAnimation;
        ValueAnimation<Vector2> m_ContentOffsetAnimation;
        readonly Action<VisualElement, float> m_ZoomAnimationAction;
        readonly Action<VisualElement, Vector2> m_ContentOffsetAnimationAction;

        List<VisualElement> m_MatchingExplorerItems = new List<VisualElement>();

        public BuilderPaneWindow paneWindow => m_PaneWindow;
        public VisualElement toolbar => m_Toolbar;
        public VisualElement viewportWrapper => m_ViewportWrapper;
        public BuilderCanvas canvas => m_Canvas;
        public BuilderSelection selection => m_Selection;
        public BuilderNotifications notifications => m_Notifications;

        string m_SubTitle;
        public string subTitle
        {
            get
            {
                if (pane == null)
                    return m_SubTitle;
                else
                    return pane.subTitle;
            }
            set
            {
                m_SubTitle = value;
                if (pane != null)
                    pane.subTitle = value;
            }
        }

        private float m_ZoomScale = 1.0f;
        public float zoomScale
        {
            get { return m_ZoomScale; }
            set
            {
                if (m_ZoomScale == value)
                    return;

                m_ZoomScale = value;
                if (m_PaneWindow.document)
                    m_PaneWindow.document.viewportZoomScale = value;
                m_Canvas.zoomScale = value;
                m_PaneWindow.document.RefreshStyle(m_DocumentRootElement);
            }
        }

        public float targetZoomScale
        {
            get
            {
                if (m_ZoomAnimation?.isRunning == true)
                    return m_ZoomAnimation.to;
                return zoomScale;
            }
        }

        private Vector2 m_ContentOffset = Vector2.zero;

        public Vector2 contentOffset
        {
            get { return m_ContentOffset; }
            set
            {
                if (m_ContentOffset == value)
                    return;

                m_ContentOffset = value;
                if (m_PaneWindow.document)
                    m_PaneWindow.document.viewportContentOffset = value;

                UpdateSurface();
            }
        }

        public Vector2 targetContentOffset
        {
            get
            {
                if (m_ContentOffsetAnimation?.isRunning == true)
                    return m_ContentOffsetAnimation.to;
                return contentOffset;
            }
        }

        void UpdateSurface()
        {
            m_Surface.style.left = m_ContentOffset.x;
            m_Surface.style.top = m_ContentOffset.y;
            if (m_CheckerboardBackground != null)
            {
                m_CheckerboardBackground.MarkDirtyRepaint();
            }
        }

        public BuilderParentTracker parentTracker => m_BuilderParentTracker;
        public BuilderSelectionIndicator selectionIndicator => m_BuilderSelectionIndicator;
        public BuilderPlacementIndicator placementIndicator => m_BuilderPlacementIndicator;
        public BuilderResizer resizer => m_BuilderResizer;
        public BuilderMover mover => m_BuilderMover;
        public BuilderViewportDragger viewportDragger => m_BuilderViewportDragger;
        public BuilderZoomer zoomer => m_BuilderZoomer;

        public VisualElement sharedStylesAndDocumentElement => m_SharedStylesAndDocumentElement;
        public VisualElement styleSelectorElementContainer => m_StyleSelectorElementContainer;
        public VisualElement documentRootElement => m_DocumentRootElement;
        public VisualElement pickOverlay => m_PickOverlay;
        public VisualElement highlightOverlay => m_HighlightOverlay;
        public VisualElement editorLayer => m_EditorLayer;
        public TextField textEditor => m_TextEditor;

        public BuilderBindingsCache bindingsCache { get; set; }
        public bool isPreviewEnabled { get; private set; }

        bool IShortcutToolContext.active => true;

        public BuilderViewport(BuilderPaneWindow paneWindow, BuilderSelection selection, BuilderElementContextMenu contextMenuManipulator, BuilderBindingsCache bindingsCache = null)
        {
            m_PaneWindow = paneWindow;
            m_Selection = selection;
            m_ContextMenuManipulator = contextMenuManipulator;
            this.bindingsCache = bindingsCache;
            m_ZoomAnimationAction = (_, v) => zoomScale = v;
            m_ContentOffsetAnimationAction = (_, v) => contentOffset = v;

            AddToClassList("unity-builder-viewport");

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.UIBuilderPackagePath + "/BuilderViewport.uxml");
            template.CloneTree(this);

            m_Toolbar = this.Q("toolbar");
            m_ViewportWrapper = this.Q("viewport-wrapper");
            m_Viewport = this.Q("viewport");
            m_Surface = this.Q("viewport-surface");
            m_Surface.pickingMode = PickingMode.Ignore;
            m_Canvas = this.Q<BuilderCanvas>("canvas");
            m_Canvas.document = paneWindow.document;
            m_Canvas.SetSelection(selection);
            m_SharedStylesAndDocumentElement = this.Q("shared-styles-and-document");
            m_SharedStylesAndDocumentElement.pseudoStates |= PseudoStates.Root; // To apply variables of the active theme that are defined in the :root selector

            m_StyleSelectorElementContainer = this.Q(BuilderConstants.StyleSelectorElementContainerName);
            m_DocumentRootElement = this.Q("document");
            m_DocumentRootElement.StretchToParentSize();

            // Fix UUM-16248 : Ensure the size of the document cannot be changed inside the builder with the :root selector
            m_DocumentRootElement.style.width = StyleKeyword.Initial;
            m_DocumentRootElement.style.height = StyleKeyword.Initial;
            m_DocumentRootElement.style.minWidth = StyleKeyword.Initial;
            m_DocumentRootElement.style.maxWidth = StyleKeyword.Initial;
            m_DocumentRootElement.style.minHeight = StyleKeyword.Initial;
            m_DocumentRootElement.style.maxHeight = StyleKeyword.Initial;

            m_Canvas.documentRootElement = m_DocumentRootElement;
            m_EditorLayer = this.Q("__unity-editor-layer");
            m_EditorLayer.AddToClassList(BuilderConstants.HiddenStyleClassName);
            m_TextEditor = this.Q<TextField>("__unity-text-editor");
            m_Canvas.editorLayer = m_EditorLayer;
            m_PickOverlay = this.Q("pick-overlay");
            m_HighlightOverlay = this.Q("highlight-overlay");
            m_BuilderParentTracker = this.Q<BuilderParentTracker>("parent-tracker");
            m_BuilderSelectionIndicator = this.Q<BuilderSelectionIndicator>("selection-indicator");
            m_BuilderPlacementIndicator = this.Q<BuilderPlacementIndicator>("placement-indicator");
            m_BuilderResizer = this.Q<BuilderResizer>("resizer");
            m_BuilderMover = this.Q<BuilderMover>("mover");
            m_BuilderZoomer = new BuilderZoomer(this);
            m_BuilderPanner = new BuilderPanner(this);

            m_Notifications = this.Q<BuilderNotifications>("notifications");

            m_BuilderViewportDragger = new BuilderViewportDragger(paneWindow, paneWindow.rootVisualElement, selection, this, m_BuilderParentTracker);

            m_BuilderMover.parentTracker = m_BuilderParentTracker;

            m_PickOverlay.RegisterCallback<MouseDownEvent>(OnPick);
            m_PickOverlay.RegisterCallback<MouseMoveEvent>(OnHover);
            m_PickOverlay.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            m_BuilderViewportDragger.RegisterCallbacksOnTarget(m_PickOverlay);
            m_Viewport.RegisterCallback<MouseDownEvent>(OnMissPick);
            m_Canvas.header.AddManipulator(new Clickable(OnCanvasHeaderClick));
            m_ContextMenuManipulator?.RegisterCallbacksOnTarget(m_Viewport);

            // Make sure this gets focus when the pane gets focused.
            primaryFocusable = this;
            focusable = true;

            // Restore the zoom scale
            zoomScale = paneWindow.document.viewportZoomScale;
            contentOffset = paneWindow.document.viewportContentOffset;

            // Repaint bug workaround.
            m_CheckerboardBackground = this.Q<CheckerboardBackground>();
            RegisterCallback<BlurEvent>(e =>
            {
                ShortcutIntegration.instance.contextManager.DeregisterToolContext(this);
                m_CheckerboardBackground.MarkDirtyRepaint();
            });

            RegisterCallback<DetachFromPanelEvent>(e =>
            {
                ShortcutIntegration.instance.contextManager.DeregisterToolContext(this);
            });

            RegisterCallback<FocusEvent>(e =>
            {
                ShortcutIntegration.instance.contextManager.RegisterToolContext(this);
                m_CheckerboardBackground.MarkDirtyRepaint();
            });

            m_Canvas.RegisterCallback<GeometryChangedEvent>(e => { m_CheckerboardBackground.MarkDirtyRepaint(); });
        }

        private void ResetViewTransform()
        {
            contentOffset = BuilderConstants.ViewportInitialContentOffset;
            zoomScale = BuilderConstants.ViewportInitialZoom;
        }

        public void SetViewFromDocumentSetting()
        {
            contentOffset = m_PaneWindow.document.viewportContentOffset;
            zoomScale = m_PaneWindow.document.viewportZoomScale;
            canvas.SetSizeFromDocumentSettings();
        }

        public void ResetView()
        {
            ResetViewTransform();
            canvas.ResetSize();
            CenterCanvas();

            // Clear selection state
            m_PickedElements.Clear();
        }

        public void SetTargetZoomScale(float targetZoomScale, Action<VisualElement, float> updateAction = null, int animationDuration = k_AnimationDuration)
        {
            if (m_ZoomAnimation?.isRunning == true)
                m_ZoomAnimation.Stop();

            m_ZoomAnimation = m_Viewport.experimental.animation.Start(zoomScale, targetZoomScale, animationDuration, updateAction ?? m_ZoomAnimationAction);
        }

        public void SetTargetContentOffset(Vector2 targetOffset, int animationDuration = k_AnimationDuration)
        {
            if (m_ContentOffsetAnimation?.isRunning == true)
                m_ContentOffsetAnimation.Stop();

            m_ContentOffsetAnimation = m_Viewport.experimental.animation.Start(contentOffset, targetOffset, animationDuration, m_ContentOffsetAnimationAction);
        }

        [EventInterest(EventInterestOptions.Inherit)]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

            if (pane == null)
                return;

            pane.subTitle = m_SubTitle;
        }

        public void ResizeCanvasToFitViewport()
        {
            const float kMargin = 8f;

            if (canvas.matchGameView)
            {
                Builder.ShowWarning(BuilderConstants.DocumentMatchGameViewModeDisabled);
                canvas.matchGameView = false;
            }

            ResetViewTransform();
            m_Canvas.x = m_Canvas.y = 0.0f;

            var maxCanvasWidth = m_Viewport.resolvedStyle.width - s_CanvasViewportMinWidthDiff - 2 * kMargin;
            var maxCanvasHeight = m_Viewport.resolvedStyle.height - s_CanvasViewportMinHeightDiff - 2 * kMargin;

            var currentWidth = m_Canvas.width;
            var currentHeight = m_Canvas.height;

            if (currentWidth > maxCanvasWidth)
                m_Canvas.width = maxCanvasWidth;

            if (currentHeight > maxCanvasHeight)
                m_Canvas.height = maxCanvasHeight;

            CenterCanvas();
        }

        public void FitViewport(VisualElement target = null)
        {
            float elementWidth = target == null ? m_Canvas.width : target.worldBound.width / zoomScale;
            float elementHeight = target == null ? m_Canvas.height : target.worldBound.height / zoomScale;
            if (elementWidth == 0 || elementHeight == 0)
                return;

            float aspectRatio = elementWidth / elementHeight;

            float targetZoom;
            if (m_Viewport.resolvedStyle.height * aspectRatio > m_Viewport.resolvedStyle.width)
                targetZoom = m_Viewport.resolvedStyle.width / elementWidth;
            else
                targetZoom = m_Viewport.resolvedStyle.height / elementHeight;

            var targetOffset = target == null ? Vector2.zero : m_Canvas.worldBound.min - target.worldBound.min;

            // Scale the target zoom
            targetOffset /= zoomScale / targetZoom;

            // Center the target 
            targetOffset += new Vector2(
                (m_Viewport.resolvedStyle.width - (elementWidth * targetZoom)) / 2,
                (m_Viewport.resolvedStyle.height - (elementHeight * targetZoom)) / 2);

            SetTargetZoomScale(targetZoom);
            SetTargetContentOffset(targetOffset);
        }

        void OnCanvasHeaderClick(EventBase obj)
        {
            m_Selection.Select(null, documentRootElement);
        }

        void CenterCanvas()
        {
            contentOffset = new Vector2((m_Viewport.resolvedStyle.width - m_Canvas.width) / 2, (m_Viewport.resolvedStyle.height - m_Canvas.height) / 2);
        }

        public VisualElement PickElement(Vector2 mousePosition, List<VisualElement> pickedElements = null)
        {
            var pickAllFunc = typeof(Panel).GetMethod("PickAll", BindingFlags.Static | BindingFlags.NonPublic);
            var pickedElement = pickAllFunc.Invoke(obj: null, parameters: new object[] { m_DocumentRootElement, mousePosition, pickedElements, true }) as VisualElement;

            if (pickedElement == null)
                return null;

            if (pickedElement == m_DocumentRootElement)
                return null;

            // Don't allow selection of elements inside template instances.
            pickedElement = pickedElement.GetClosestElementPartOfCurrentDocument();

            return pickedElement;
        }

        void OnPick(MouseDownEvent evt)
        {
            // Do not prevent zoom and pan
            if (evt.button == 2 || (evt.actionKey && evt.altKey || (evt.button == (int)MouseButton.RightMouse && evt.altKey)))
                return;

            m_PickedElements.Clear();
            var pickedElement = PickElement(evt.mousePosition, m_PickedElements);

            if (pickedElement != null)
            {
                m_Selection.Select(this, pickedElement);
                SetInnerSelection(pickedElement);

                if (evt.clickCount == 2)
                {
                    var posInViewport = m_PickOverlay.ChangeCoordinatesTo(this, evt.localMousePosition);
                    BuilderInPlaceTextEditingUtilities.OpenEditor(pickedElement, this.ChangeCoordinatesTo(pickedElement, posInViewport));
                }
            }
            else
            {
                ClearInnerSelection();
                m_Selection.ClearSelection(this);
            }

            if (evt.button == (int)MouseButton.RightMouse)
            {
                if (pickedElement != null && m_ContextMenuManipulator != null)
                {
                    pickedElement.SetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName, pickedElement);
                    m_ContextMenuManipulator.RegisterCallbacksOnTarget(pickedElement);
                    m_ContextMenuManipulator.DisplayContextMenu(evt, pickedElement);
                    evt.StopPropagation();
                }
            }
            else
            {
                evt.StopPropagation();
            }
        }

        void ClearMatchingExplorerItems()
        {
            foreach (var item in m_MatchingExplorerItems)
                item.RemoveFromClassList(BuilderConstants.ExplorerItemHoverClassName);

            m_MatchingExplorerItems.Clear();
        }

        void HighlightMatchingExplorerItems()
        {
            foreach (var item in m_MatchingExplorerItems)
                item.AddToClassList(BuilderConstants.ExplorerItemHoverClassName);
        }

        void OnHover(MouseMoveEvent evt)
        {
            var pickedElement = PickElement(evt.mousePosition);

            if (pickedElement != null)
            {
                // Don't allow selection of elements inside template instances.
                pickedElement = pickedElement.GetClosestElementPartOfCurrentDocument();

                parentTracker.Activate(pickedElement);

                ClearMatchingExplorerItems();

                // Highlight corresponding element in Explorer (if visible).
                var explorerItem = pickedElement.GetProperty(BuilderConstants.ElementLinkedExplorerItemVEPropertyName) as BuilderExplorerItem;
                var explorerItemRow = explorerItem?.row();
                if (explorerItemRow != null)
                    m_MatchingExplorerItems.Add(explorerItemRow);

                // Highlight matching selectors in the Explorer (if visible).
                var matchingSelectors = BuilderSharedStyles.GetMatchingSelectorsOnElement(pickedElement);
                if (matchingSelectors != null)
                {
                    foreach (var selectorStr in matchingSelectors)
                    {
                        var selectorElement = BuilderSharedStyles.FindSelectorElement(m_DocumentRootElement, selectorStr);
                        if (selectorElement == null)
                            continue;

                        var selectorItem = selectorElement.GetProperty(BuilderConstants.ElementLinkedExplorerItemVEPropertyName) as BuilderExplorerItem;
                        var selectorItemRow = selectorItem?.row();
                        if (selectorItemRow == null)
                            continue;

                        m_MatchingExplorerItems.Add(selectorItemRow);
                    }
                }

                HighlightMatchingExplorerItems();
            }
            else
            {
                parentTracker.Deactivate();

                ClearMatchingExplorerItems();
            }

            evt.StopPropagation();
        }

        void OnMouseLeave(MouseLeaveEvent evt)
        {
            if (evt.button == 2)
                return;

            parentTracker.Deactivate();

            ClearMatchingExplorerItems();
        }

        void OnMissPick(MouseDownEvent evt)
        {
            ClearInnerSelection();
            m_Selection.ClearSelection(this);
        }

        private void OnDoNotShowAgainButtonPressed()
        {
            BuilderProjectSettings.BlockNotification(BuilderConstants.previewNotificationKey);
            m_Notifications.ClearNotifications(BuilderConstants.previewNotificationKey);
        }

        [Shortcut("UI Builder/Frame Selected", typeof(BuilderViewport), KeyCode.F)]
        static void OnFrameSelectedShortcut(ShortcutArguments args)
        {
            var builderViewPort = args.context as BuilderViewport;
            builderViewPort.FitViewport(builderViewPort.selection.selection.FirstOrDefault());
        }

        public void SetPreviewMode(bool mode)
        {
            isPreviewEnabled = mode;

            if (mode)
            {
                var boundElements = new List<VisualElement>();
                DataBindingUtility.GetBoundElements(panel, boundElements);

                var documentHasBindings = false;
                foreach (var boundElement in boundElements)
                {
                    var bindingsInfo = new List<BindingInfo>();
                    DataBindingUtility.GetBindingsForElement(boundElement, bindingsInfo);

                    foreach (var bindingInfo in bindingsInfo)
                    {
                        if (bindingInfo.binding is SerializedObjectBindingBase) continue;
                        documentHasBindings = true;
                        break;
                    }

                    if (documentHasBindings)
                    {
                        break;
                    }
                }

                if (documentHasBindings)
                {
                    var isNotificationBlocked = BuilderProjectSettings.IsNotificationBlocked(BuilderConstants.previewNotificationKey);

                    if (!isNotificationBlocked)
                    {
                        var notificationData = new BuilderNotifications.NotificationData
                        {
                            key = BuilderConstants.previewNotificationKey,
                            message = BuilderConstants.BindingsOnPreviewModeNotification,
                            actionButtonText = BuilderConstants.DoNotShowAgainNotificationButtonText,
                            onActionButtonClicked = OnDoNotShowAgainButtonPressed,
                            showDismissButton =  true,
                            notificationType = BuilderNotifications.NotificationType.Warning,
                        };

                        m_Notifications.AddNotification(notificationData);
                    }
                }

                m_ViewportWrapper.AddToClassList(s_PreviewModeClassName);
                m_Viewport.AddToClassList(s_PreviewModeClassName);
                m_PickOverlay.AddToClassList(s_PreviewModeClassName);
                if (panel is Panel p)
                    p.styleAnimationSystem = new StylePropertyAnimationSystem();
            }
            else
            {
                m_Notifications.ClearNotifications(BuilderConstants.previewNotificationKey);
                m_ViewportWrapper.RemoveFromClassList(s_PreviewModeClassName);
                m_Viewport.RemoveFromClassList(s_PreviewModeClassName);
                m_PickOverlay.RemoveFromClassList(s_PreviewModeClassName);
                if (panel is Panel p)
                    p.styleAnimationSystem = new EmptyStylePropertyAnimationSystem();
            }
        }

        void SetInnerSelection(VisualElement selectedElement)
        {
            if (selectedElement.resolvedStyle.display == DisplayStyle.None)
            {
                ClearInnerSelection();
                return;
            }

            m_BuilderResizer.Activate(m_PaneWindow, m_Selection, m_PaneWindow.document.visualTreeAsset, selectedElement, bindingsCache);
            m_BuilderMover.Activate(m_PaneWindow, m_Selection, m_PaneWindow.document.visualTreeAsset, selectedElement, bindingsCache);

            m_Canvas.SetHighlighted(false);
            switch (m_Selection.selectionType)
            {
                case BuilderSelectionType.Element:
                case BuilderSelectionType.ElementInTemplateInstance:
                case BuilderSelectionType.ElementInControlInstance:
                    m_BuilderSelectionIndicator.Activate(m_Selection, m_PaneWindow.document.visualTreeAsset, selectedElement, bindingsCache);
                    break;
                case BuilderSelectionType.VisualTreeAsset:
                    m_Canvas.SetHighlighted(true);
                    m_BuilderSelectionIndicator.Deactivate();
                    break;
                default:
                    m_BuilderSelectionIndicator.Deactivate();
                    break;
            }
        }

        void ClearInnerSelection()
        {
            m_BuilderResizer.Deactivate();
            m_BuilderMover.Deactivate();
            m_BuilderSelectionIndicator.Deactivate();
            m_Canvas.SetHighlighted(false);
        }

        public void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {
            m_BuilderSelectionIndicator.OnHierarchyChanged(element);
        }

        public void SelectionChanged()
        {
            if (m_Selection.isEmpty || m_Selection.selectionCount > 1)
                ClearInnerSelection();
            else
                SetInnerSelection(m_Selection.selection.First());
        }

        public void StylingChanged(List<string> styles, BuilderStylingChangeType changeType)
        {
            m_Canvas.editorExtensionsLabel.style.display = paneWindow.document.fileSettings.editorExtensionMode
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            if (m_Selection.isEmpty || styles == null)
                return;

            if (styles.Contains("display"))
            {
                SetInnerSelection(m_Selection.selection.First());
            }

            if (m_Selection.selectionType == BuilderSelectionType.Element ||
                m_Selection.selectionType == BuilderSelectionType.VisualTreeAsset)
            {
                m_BuilderSelectionIndicator.canvasStyleControls.UpdateButtonIcons(styles);
            }
        }
    }
}
