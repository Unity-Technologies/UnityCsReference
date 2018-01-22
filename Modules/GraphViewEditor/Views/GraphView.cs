// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal interface IGraphViewSelection
    {
        int version { get; set; }
        List<string> selectedElements { get; set; }
    }

    internal class GraphViewUndoRedoSelection : ScriptableObject, IGraphViewSelection
    {
        [SerializeField]
        private int m_Version;
        [SerializeField]
        private List<string> m_SelectedElements;

        public int version
        {
            get { return m_Version; }
            set { m_Version = value; }
        }
        public List<string> selectedElements
        {
            get { return m_SelectedElements; }
            set { m_SelectedElements = value; }
        }
    }

    public struct GraphViewChange
    {
        // Operations Pending
        public List<GraphElement> elementsToRemove;
        public List<Edge> edgesToCreate;

        // Operations Completed
        public List<GraphElement> movedElements;
    }

    public struct NodeCreationContext
    {
        public Vector2 screenMousePosition;
    }

    public abstract class GraphView : DataWatchContainer, ISelection
    {
        // Layer class. Used for queries below.
        class Layer : VisualElement {}

        // TODO: Remove when removing presenters.
        private GraphViewPresenter m_Presenter;

        // TODO: Remove when removing presenters.
        public T GetPresenter<T>() where T : GraphViewPresenter
        {
            return presenter as T;
        }

        // Delegates and Callbacks
        public Action<NodeCreationContext> nodeCreationRequest { get; set; }

        public delegate GraphViewChange GraphViewChanged(GraphViewChange graphViewChange);
        public GraphViewChanged graphViewChanged { get; set; }

        public delegate void GroupNodeTitleChanged(GroupNode groupNode, string title);
        public delegate void ElementAddedToGroupNode(GroupNode groupNode, GraphElement element);
        public delegate void ElementRemovedFromGroupNode(GroupNode groupNode, GraphElement element);

        public GroupNodeTitleChanged groupNodeTitleChanged { get; set; }
        public ElementAddedToGroupNode elementAddedToGroupNode { get; set; }
        public ElementRemovedFromGroupNode elementRemovedFromGroupNode { get; set; }

        private GraphViewChange m_GraphViewChange;
        private List<GraphElement> m_ElementsToRemove;

        public delegate void ElementResized(VisualElement visualElement);
        public ElementResized elementResized { get; set; }

        public delegate void ViewTransformChanged(GraphView graphView);
        public ViewTransformChanged viewTransformChanged { get; set; }

        // TODO: Remove when removing presenters.
        public GraphViewPresenter presenter
        {
            get { return m_Presenter; }
            set
            {
                if (m_Presenter == value)
                    return;

                RemoveWatch();
                m_Presenter = value;
                OnDataChanged();
                AddWatch();
            }
        }

        [Serializable]
        class PersistedSelection : IGraphViewSelection
        {
            [SerializeField]
            private int m_Version;
            [SerializeField]
            private List<string> m_SelectedElements;

            public int version
            {
                get { return m_Version; }
                set { m_Version = value; }
            }
            public List<string> selectedElements
            {
                get { return m_SelectedElements; }
                set { m_SelectedElements = value; }
            }
        }
        private const string k_SelectionUndoRedoLabel = "Change GraphView Selection";
        private int m_SavedSelectionVersion;
        private PersistedSelection m_PersistedSelection;
        private GraphViewUndoRedoSelection m_GraphViewUndoRedoSelection;
        private bool m_FontsOverridden = false;

        class ContentViewContainer : VisualElement
        {
            public override bool Overlaps(Rect r)
            {
                return true;
            }
        }

        // TODO: Remove when removing presenters.
        protected GraphViewTypeFactory typeFactory { get; set; }

        public VisualElement contentViewContainer { get; private set; }

        // TODO: Remove!
        public VisualElement viewport
        {
            get { return this; }
        }

        public ITransform viewTransform
        {
            get { return contentViewContainer.transform; }
        }

        public void UpdateViewTransform(Vector3 newPosition, Vector3 newScale)
        {
            contentViewContainer.transform.position = newPosition;
            contentViewContainer.transform.scale = newScale;

            if (m_Presenter != null)
            {
                m_Presenter.position = newPosition;
                m_Presenter.scale = newScale;
            }

            UpdatePersistedViewTransform();

            if (viewTransformChanged != null)
                viewTransformChanged(this);
        }

        bool m_FrameAnimate = false;

        public bool isReframable { get; set; }

        public enum FrameType
        {
            All = 0,
            Selection = 1,
            Origin = 2
        }

        readonly int k_FrameBorder = 30;
        readonly float k_ContentViewWidth = 10000.0f;

        readonly Dictionary<int, Layer> m_ContainerLayers = new Dictionary<int, Layer>();

        protected GraphView()
        {
            selection = new List<ISelectable>();
            clippingOptions = ClippingOptions.ClipContents;

            // Hardcode the content view width to a big value as a work around for a bug in the CSSLayout library.
            // This is a temporary fix to case 958001.
            // TODO: Remove hardcoded width once latest Yoga library is integrated.
            contentViewContainer = new ContentViewContainer
            {
                name = "contentViewContainer",
                clippingOptions = ClippingOptions.NoClipping,
                pickingMode = PickingMode.Ignore,
                style = { width = k_ContentViewWidth }
            };

            // make it absolute and 0 sized so it acts as a transform to move children to and fro
            Add(contentViewContainer);

            // TODO: Remove when removing presenters.
            typeFactory = new GraphViewTypeFactory();
            typeFactory[typeof(EdgePresenter)] = typeof(Edge);

            AddStyleSheetPath("StyleSheets/GraphView/GraphView.uss");
            graphElements = contentViewContainer.Query().Children<Layer>().Children<GraphElement>().Build();
            nodes = this.Query<Layer>().Children<Node>().Build();
            edges = this.Query<Layer>().Children<Edge>().Build();
            ports = contentViewContainer.Query().Children<Layer>().Descendents<Port>().Build();

            m_ElementsToRemove = new List<GraphElement>();
            m_GraphViewChange.elementsToRemove = m_ElementsToRemove;

            isReframable = true;
            focusIndex = 0;

            RegisterCallback<IMGUIEvent>(OnValidateCommand);
            RegisterCallback<IMGUIEvent>(OnExecuteCommand);
            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
            RegisterCallback<ContextualMenuPopulateEvent>(OnContextualMenu);

            // We override the font for all GraphElements here so we can use the fallback system.
            // We load the dummy font first and then we overwrite the fontNames, just like we do
            // with the Editor's default font asset. Loading system fonts directly via
            // Font.CreateDynamicFontFromOSFont() caused TextMesh to generate the wrong bounds.
            //
            // TODO: Add font fallback specifications and use of system fonts to USS.
            if (!m_FontsOverridden && (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor))
            {
                Font graphViewFont = EditorGUIUtility.LoadRequired("GraphView/DummyFont(LucidaGrande).ttf") as Font;

                if (Application.platform == RuntimePlatform.WindowsEditor)
                    graphViewFont.fontNames = new string[] { "Segoe UI", "Helvetica Neue", "Helvetica", "Arial", "Verdana" };
                else if (Application.platform == RuntimePlatform.OSXEditor)
                    graphViewFont.fontNames = new string[] { "Helvetica Neue", "Lucida Grande" };

                m_FontsOverridden = true;
            }
        }

        internal override void ChangePanel(BaseVisualElementPanel p)
        {
            if (p == panel)
                return;

            if (p == null)
            {
                Undo.ClearUndo(m_GraphViewUndoRedoSelection);
                Undo.undoRedoPerformed -= UndoRedoPerformed;
                ScriptableObject.DestroyImmediate(m_GraphViewUndoRedoSelection);
                m_GraphViewUndoRedoSelection = null;

                if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
                    ClearSavedSelection();
            }
            else if (panel == null)
            {
                Undo.undoRedoPerformed += UndoRedoPerformed;
                m_GraphViewUndoRedoSelection = ScriptableObject.CreateInstance<GraphViewUndoRedoSelection>();
                m_GraphViewUndoRedoSelection.selectedElements = new List<string>();
                m_GraphViewUndoRedoSelection.hideFlags = HideFlags.HideAndDontSave;
            }

            base.ChangePanel(p);
        }

        private void ClearSavedSelection()
        {
            if (m_PersistedSelection == null)
                return;

            m_PersistedSelection.selectedElements.Clear();
            SavePersistentData();
        }

        private bool ShouldRecordUndo()
        {
            return m_GraphViewUndoRedoSelection != null &&
                m_PersistedSelection != null &&
                m_SavedSelectionVersion == m_GraphViewUndoRedoSelection.version;
        }

        private void RestoreSavedSelection(IGraphViewSelection graphViewSelection)
        {
            if (graphViewSelection.version == m_SavedSelectionVersion)
                return;

            // Update both selection objects' versions.
            m_GraphViewUndoRedoSelection.version = graphViewSelection.version;
            m_PersistedSelection.version = graphViewSelection.version;

            ClearSelection();
            List<string> invalidGuids = null;
            foreach (string guid in graphViewSelection.selectedElements)
            {
                var element = GetElementByGuid(guid);
                if (element == null)
                {
                    if (invalidGuids == null)
                        invalidGuids = new List<string>();

                    invalidGuids.Add(guid);
                    continue;
                }

                AddToSelection(element);
            }

            // Remove invalid GUIDs.
            if (invalidGuids != null)
            {
                foreach (string guid in invalidGuids)
                    graphViewSelection.selectedElements.Remove(guid);
            }

            m_SavedSelectionVersion = graphViewSelection.version;

            IGraphViewSelection selectionObjectToSync = m_GraphViewUndoRedoSelection;
            if (graphViewSelection is GraphViewUndoRedoSelection)
                selectionObjectToSync = m_PersistedSelection;

            selectionObjectToSync.selectedElements.Clear();
            selectionObjectToSync.selectedElements.AddRange(graphViewSelection.selectedElements);
        }

        private void UndoRedoPerformed()
        {
            RestoreSavedSelection(m_GraphViewUndoRedoSelection);
        }

        private void RecordSelectionUndoPre()
        {
            if (m_GraphViewUndoRedoSelection == null)
                return;

            Undo.RegisterCompleteObjectUndo(m_GraphViewUndoRedoSelection, k_SelectionUndoRedoLabel);
        }

        private IVisualElementScheduledItem m_OnTimerTicker;

        private void RecordSelectionUndoPost()
        {
            m_GraphViewUndoRedoSelection.version++;
            m_SavedSelectionVersion = m_GraphViewUndoRedoSelection.version;

            m_PersistedSelection.version++;

            if (m_OnTimerTicker == null)
            {
                m_OnTimerTicker = schedule.Execute(DelayPersistentDataSave);
            }

            m_OnTimerTicker.ExecuteLater(1);
        }

        private void DelayPersistentDataSave()
        {
            m_OnTimerTicker = null;
            SavePersistentData();
        }

        public void AddLayer(int index)
        {
            Layer newLayer = new Layer { clippingOptions = ClippingOptions.NoClipping, pickingMode = PickingMode.Ignore };

            m_ContainerLayers.Add(index, newLayer);

            int indexOfLayer = m_ContainerLayers.OrderBy(t => t.Key).Select(t => t.Value).ToList().IndexOf(newLayer);

            contentViewContainer.Insert(indexOfLayer, newLayer);
        }

        VisualElement GetLayer(int index)
        {
            return m_ContainerLayers[index];
        }

        internal void ChangeLayer(GraphElement element)
        {
            if (!m_ContainerLayers.ContainsKey(element.layer))
            {
                AddLayer(element.layer);
            }
            GetLayer(element.layer).Add(element);
        }

        public UQuery.QueryState<GraphElement> graphElements { get; private set; }
        public UQuery.QueryState<Node> nodes { get; private set; }
        public UQuery.QueryState<Port> ports;
        public UQuery.QueryState<Edge> edges { get; private set; }

        [Serializable]
        class PersistedViewTransform
        {
            public Vector3 position = Vector3.zero;
            public Vector3 scale = Vector3.one;
        }
        PersistedViewTransform m_PersistedViewTransform;

        ContentZoomer m_Zoomer;
        int m_ZoomerMaxElementCountWithPixelCacheRegen = 100;
        float m_MinScale = ContentZoomer.DefaultMinScale;
        float m_MaxScale = ContentZoomer.DefaultMaxScale;
        float m_ScaleStep = ContentZoomer.DefaultScaleStep;
        float m_ReferenceScale = ContentZoomer.DefaultReferenceScale;

        public float minScale
        {
            get { return m_MinScale; }
        }

        public float maxScale
        {
            get { return m_MaxScale; }
        }

        public float scaleStep
        {
            get { return m_ScaleStep; }
        }

        public float referenceScale
        {
            get { return m_ReferenceScale; }
        }

        public float scale
        {
            get { return viewTransform.scale.x; }
        }

        public int zoomerMaxElementCountWithPixelCacheRegen
        {
            get { return m_ZoomerMaxElementCountWithPixelCacheRegen; }
            set
            {
                if (m_ZoomerMaxElementCountWithPixelCacheRegen == value)
                    return;

                m_ZoomerMaxElementCountWithPixelCacheRegen = value;
                if (m_Presenter != null)
                    m_Zoomer.keepPixelCacheOnZoom = m_Presenter.elements.Count() > m_ZoomerMaxElementCountWithPixelCacheRegen;
            }
        }

        public GraphElement GetElementByGuid(string guid)
        {
            return graphElements.ToList().FirstOrDefault(e => e.persistenceKey == guid);
        }

        public Node GetNodeByGuid(string guid)
        {
            return nodes.ToList().FirstOrDefault(e => e.persistenceKey == guid);
        }

        public Port GetPortByGuid(string guid)
        {
            return ports.ToList().FirstOrDefault(e => e.persistenceKey == guid);
        }

        public Edge GetEdgeByGuid(string guid)
        {
            return graphElements.ToList().OfType<Edge>().FirstOrDefault(e => e.persistenceKey == guid);
        }

        public void SetupZoom(float minScaleSetup, float maxScaleSetup)
        {
            SetupZoom(minScaleSetup, maxScaleSetup, m_ScaleStep, m_ReferenceScale);
        }

        public void SetupZoom(float minScaleSetup, float maxScaleSetup, float scaleStepSetup, float referenceScaleSetup)
        {
            m_MinScale = minScaleSetup;
            m_MaxScale = maxScaleSetup;
            m_ScaleStep = scaleStepSetup;
            m_ReferenceScale = referenceScaleSetup;
            UpdateContentZoomer();
        }

        private void UpdatePersistedViewTransform()
        {
            if (m_PersistedViewTransform == null)
                return;

            m_PersistedViewTransform.position = contentViewContainer.transform.position;
            m_PersistedViewTransform.scale = contentViewContainer.transform.scale;

            SavePersistentData();
        }

        public override void OnPersistentDataReady()
        {
            base.OnPersistentDataReady();

            string key = GetFullHierarchicalPersistenceKey();

            m_PersistedViewTransform = GetOrCreatePersistentData<PersistedViewTransform>(m_PersistedViewTransform, key);

            m_PersistedSelection = GetOrCreatePersistentData<PersistedSelection>(m_PersistedSelection, key);
            if (m_PersistedSelection.selectedElements == null)
                m_PersistedSelection.selectedElements = new List<string>();

            UpdateViewTransform(m_PersistedViewTransform.position, m_PersistedViewTransform.scale);
            RestoreSavedSelection(m_PersistedSelection);
        }

        void UpdateContentZoomer()
        {
            if (m_MinScale != m_MaxScale)
            {
                if (m_Zoomer == null)
                {
                    m_Zoomer = new ContentZoomer();
                    this.AddManipulator(m_Zoomer);
                }

                m_Zoomer.minScale = m_MinScale;
                m_Zoomer.maxScale = m_MaxScale;
                m_Zoomer.scaleStep = m_ScaleStep;
                m_Zoomer.referenceScale = m_ReferenceScale;
            }
            else
            {
                if (m_Zoomer != null)
                    this.RemoveManipulator(m_Zoomer);
            }

            ValidateTransform();
        }

        protected void ValidateTransform()
        {
            if (contentViewContainer == null)
                return;
            Vector3 transformScale = viewTransform.scale;

            transformScale.x = Mathf.Clamp(transformScale.x, minScale, maxScale);
            transformScale.y = Mathf.Clamp(transformScale.y, minScale, maxScale);

            viewTransform.scale = transformScale;
        }

        // TODO: Remove when removing presenters.
        public override void OnDataChanged()
        {
            if (m_Presenter == null)
                return;

            contentViewContainer.transform.position = m_Presenter.position;
            contentViewContainer.transform.scale = m_Presenter.scale != Vector3.zero ? m_Presenter.scale : Vector3.one;
            ValidateTransform();
            UpdatePersistedViewTransform();

            UpdateContentZoomer();

            // process removals
            List<GraphElement> current = graphElements.ToList();

            foreach (var c in current)
            {
                // been removed?
                // edges can now exist in a valid state but without a presenter
                if (c.dependsOnPresenter && !m_Presenter.elements.Contains(c.presenter))
                {
                    selection.Remove(c);
                    RemoveElement(c);
                }
            }

            // process additions
            int elementCount = 0;
            foreach (GraphElementPresenter elementPresenter in m_Presenter.elements)
            {
                elementCount++;

                // been added?
                var found = false;

                // For regular presenters we check inside the contentViewContainer
                // for their VisualElements.
                if (!elementPresenter.isFloating)
                {
                    foreach (var dc in current)
                    {
                        if (dc != null && dc.presenter == elementPresenter)
                        {
                            found = true;
                            break;
                        }
                    }
                }
                // For floating presenters, like the MiniMap, we need to check the
                // directly children of the GraphView for their VisualElements,
                // excluding the contentViewContainer. That's where floating
                // presenters are added.
                else
                {
                    foreach (var dc in Children())
                    {
                        if (dc == contentViewContainer)
                            continue;

                        var graphElement = dc as GraphElement;
                        if (graphElement == null)
                            continue;

                        if (graphElement.presenter == elementPresenter)
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                    InstantiateElement(elementPresenter);
            }

            // Change Zoomer pixel caching setting based on number of GraphElements.
            m_Zoomer.keepPixelCacheOnZoom = elementCount > m_ZoomerMaxElementCountWithPixelCacheRegen;
        }

        // TODO: Remove when removing presenters.
        protected override UnityEngine.Object[] toWatch
        {
            get { return presenter == null ? null : new UnityEngine.Object[] { presenter }; }
        }

        // ISelection implementation
        public List<ISelectable> selection { get; protected set; }

        // functions to ISelection extensions
        public virtual void AddToSelection(ISelectable selectable)
        {
            var graphElement = selectable as GraphElement;
            if (graphElement == null)
                return;
            graphElement.selected = true;

            // TODO: Remove when removing presenters.
            if (graphElement.presenter != null)
                graphElement.presenter.selected = true;

            selection.Add(selectable);
            graphElement.OnSelected();
            contentViewContainer.Dirty(ChangeType.Repaint);

            if (ShouldRecordUndo())
            {
                RecordSelectionUndoPre();
                m_GraphViewUndoRedoSelection.selectedElements.Add(graphElement.persistenceKey);
                m_PersistedSelection.selectedElements.Add(graphElement.persistenceKey);
                RecordSelectionUndoPost();
            }
        }

        public virtual void RemoveFromSelection(ISelectable selectable)
        {
            var graphElement = selectable as GraphElement;
            if (graphElement == null)
                return;
            graphElement.selected = false;

            // TODO: Remove when removing presenters.
            if (graphElement.presenter != null)
                graphElement.presenter.selected = false;

            selection.Remove(selectable);
            graphElement.OnUnselected();
            contentViewContainer.Dirty(ChangeType.Repaint);

            if (ShouldRecordUndo())
            {
                RecordSelectionUndoPre();
                m_GraphViewUndoRedoSelection.selectedElements.Remove(graphElement.persistenceKey);
                m_PersistedSelection.selectedElements.Remove(graphElement.persistenceKey);
                RecordSelectionUndoPost();
            }
        }

        public virtual void ClearSelection()
        {
            foreach (var graphElement in selection.OfType<GraphElement>())
            {
                graphElement.selected = false;

                // TODO: Remove when removing presenters.
                if (graphElement.presenter != null)
                    graphElement.presenter.selected = false;

                graphElement.OnUnselected();
            }

            bool selectionWasNotEmpty = selection.Any();
            selection.Clear();
            contentViewContainer.Dirty(ChangeType.Repaint);

            if (ShouldRecordUndo() && selectionWasNotEmpty)
            {
                RecordSelectionUndoPre();
                m_GraphViewUndoRedoSelection.selectedElements.Clear();
                m_PersistedSelection.selectedElements.Clear();
                RecordSelectionUndoPost();
            }
        }

        public virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is UIElements.GraphView.GraphView && nodeCreationRequest != null)
            {
                evt.menu.AppendAction("Create Node", OnContextMenuNodeCreate, ContextualMenu.MenuAction.AlwaysEnabled);
                evt.menu.AppendSeparator();
            }
            if (evt.target is UIElements.GraphView.GraphView || evt.target is Node || evt.target is GroupNode)
            {
                evt.menu.AppendAction("Cut", (e) => { CutSelectionCallback(); },
                    (e) => { return canCutSelection ? ContextualMenu.MenuAction.StatusFlags.Normal : ContextualMenu.MenuAction.StatusFlags.Disabled; });
            }
            if (evt.target is UIElements.GraphView.GraphView || evt.target is Node || evt.target is GroupNode)
            {
                evt.menu.AppendAction("Copy", (e) => { CopySelectionCallback(); },
                    (e) => { return canCopySelection ? ContextualMenu.MenuAction.StatusFlags.Normal : ContextualMenu.MenuAction.StatusFlags.Disabled; });
            }
            if (evt.target is UIElements.GraphView.GraphView)
            {
                evt.menu.AppendAction("Paste", (e) => { PasteCallback(); },
                    (e) => { return canPaste ? ContextualMenu.MenuAction.StatusFlags.Normal : ContextualMenu.MenuAction.StatusFlags.Disabled; });
            }
            if (evt.target is UIElements.GraphView.GraphView || evt.target is Node || evt.target is GroupNode || evt.target is Edge)
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Delete", (e) => { DeleteSelectionCallback(AskUser.DontAskUser); },
                    (e) => { return canDeleteSelection ? ContextualMenu.MenuAction.StatusFlags.Normal : ContextualMenu.MenuAction.StatusFlags.Disabled; });
            }
            if (evt.target is UIElements.GraphView.GraphView || evt.target is Node || evt.target is GroupNode)
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Duplicate", (e) => { DuplicateSelectionCallback(); },
                    (e) => { return canDuplicateSelection ? ContextualMenu.MenuAction.StatusFlags.Normal : ContextualMenu.MenuAction.StatusFlags.Disabled; });
                evt.menu.AppendSeparator();
            }
        }

        void OnContextMenuNodeCreate(EventBase evt)
        {
            if (nodeCreationRequest == null)
                return;

            GUIView guiView = elementPanel.ownerObject as GUIView;
            if (guiView == null)
                return;

            Vector2 referencePosition;
            if (evt is IMouseEvent)
            {
                referencePosition = (evt as IMouseEvent).mousePosition;
            }
            else
            {
                referencePosition = Vector2.zero;
            }

            Vector2 screenPoint = guiView.screenPosition.position + referencePosition;

            nodeCreationRequest(new NodeCreationContext() { screenMousePosition = screenPoint });
        }

        protected internal override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (elementPanel != null && elementPanel.contextualMenuManager != null)
            {
                elementPanel.contextualMenuManager.DisplayMenuIfEventMatches(evt, this);
            }
        }

        void OnContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // If popping a contextual menu on a GraphElement, add the cut/copy actions.
            BuildContextualMenu(evt);
        }

        void OnEnterPanel(AttachToPanelEvent e)
        {
            // Force DefaultCommonDark.uss since GraphView only has a dark style at the moment
            UIElementsEditorUtility.ForceDarkStyleSheet(this);

            if (isReframable)
                panel.visualTree.RegisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }

        void OnLeavePanel(DetachFromPanelEvent e)
        {
            if (isReframable)
                panel.visualTree.UnregisterCallback<KeyDownEvent>(OnKeyDownShortcut);
        }

        void OnKeyDownShortcut(KeyDownEvent evt)
        {
            if (!isReframable)
                return;

            if (MouseCaptureController.IsMouseCaptureTaken())
                return;

            EventPropagation result = EventPropagation.Continue;
            switch (evt.character)
            {
                case 'a':
                    result = FrameAll();
                    break;

                case 'o':
                    result = FrameOrigin();
                    break;

                case '[':
                    result = FramePrev();
                    break;

                case ']':
                    result = FrameNext();
                    break;
            }

            if (result == EventPropagation.Stop)
            {
                evt.StopPropagation();
                if (evt.imguiEvent != null)
                {
                    evt.imguiEvent.Use();
                }
            }
        }

        void OnValidateCommand(IMGUIEvent evt)
        {
            Event e = evt.imguiEvent;
            if (e != null && e.type == EventType.ValidateCommand)
            {
                if ((e.commandName == EventCommandNames.Copy && canCopySelection)
                    || (e.commandName == EventCommandNames.Paste && canPaste)
                    || (e.commandName == EventCommandNames.Duplicate && canDuplicateSelection)
                    || (e.commandName == EventCommandNames.Cut && canCutSelection)
                    || ((e.commandName == EventCommandNames.Delete || e.commandName == EventCommandNames.SoftDelete) && canDeleteSelection))
                {
                    evt.StopPropagation();
                    e.Use();
                }
                else if (e.commandName == EventCommandNames.FrameSelected)
                {
                    evt.StopPropagation();
                    e.Use();
                }
            }
        }

        public enum AskUser
        {
            AskUser,
            DontAskUser
        }

        void OnExecuteCommand(IMGUIEvent evt)
        {
            Event e = evt.imguiEvent;
            if (e != null && e.type == EventType.ExecuteCommand)
            {
                if (e.commandName == EventCommandNames.Copy)
                {
                    CopySelectionCallback();
                    evt.StopPropagation();
                }
                else if (e.commandName == EventCommandNames.Paste)
                {
                    PasteCallback();
                    evt.StopPropagation();
                }
                else if (e.commandName == EventCommandNames.Duplicate)
                {
                    DuplicateSelectionCallback();
                    evt.StopPropagation();
                }
                else if (e.commandName == EventCommandNames.Cut)
                {
                    CutSelectionCallback();
                    evt.StopPropagation();
                }
                else if (e.commandName == EventCommandNames.Delete)
                {
                    DeleteSelectionCallback(AskUser.DontAskUser);
                    evt.StopPropagation();
                }
                else if (e.commandName == EventCommandNames.SoftDelete)
                {
                    DeleteSelectionCallback(AskUser.AskUser);
                    evt.StopPropagation();
                }
                else if (e.commandName == EventCommandNames.FrameSelected)
                {
                    FrameSelection();
                    evt.StopPropagation();
                }

                if (evt.isPropagationStopped)
                {
                    e.Use();
                }
            }
        }

        // The system clipboard is unreliable, at least on Windows.
        // For testing clipboard operations on GraphView,
        // set m_UseInternalClipboard to true.
        internal bool m_UseInternalClipboard = false;
        string m_Clipboard = string.Empty;

        internal string clipboard
        {
            get
            {
                if (m_UseInternalClipboard)
                {
                    return m_Clipboard;
                }
                else
                {
                    return EditorGUIUtility.systemCopyBuffer;
                }
            }

            set
            {
                if (m_UseInternalClipboard)
                {
                    m_Clipboard = value;
                }
                else
                {
                    EditorGUIUtility.systemCopyBuffer = value;
                }
            }
        }

        protected internal virtual bool canCopySelection
        {
            get { return selection.OfType<Node>().Any() || selection.OfType<GroupNode>().Any(); }
        }

        private void CollectElements(IEnumerable<GraphElement> elements, HashSet<GraphElement> collectedElementSet, Func<GraphElement, bool> conditionFunc)
        {
            foreach (var element in elements.Where(e => e != null && !collectedElementSet.Contains(e) && conditionFunc(e)))
            {
                var node = element as Node;

                if (node != null)
                {
                    CollectConnectedEgdes(collectedElementSet, node);
                }
                else
                {
                    var groupNode = element as GroupNode;

                    // If the selected element is a group then visit its contained element
                    if (groupNode != null)
                    {
                        CollectElements(groupNode.containedElements, collectedElementSet, conditionFunc);
                    }
                }

                collectedElementSet.Add(element);
            }
        }

        private void CollectCopyableGraphElements(IEnumerable<GraphElement> elements, HashSet<GraphElement> elementsToCopySet)
        {
            CollectElements(elements, elementsToCopySet, e => (e is Node || e is GroupNode));
        }

        protected internal void CopySelectionCallback()
        {
            var elementsToCopySet = new HashSet<GraphElement>();

            CollectCopyableGraphElements(selection.OfType<GraphElement>(), elementsToCopySet);

            string data = SerializeGraphElements(elementsToCopySet);

            if (!string.IsNullOrEmpty(data))
            {
                clipboard = data;
            }
        }

        protected internal virtual bool canCutSelection
        {
            get { return selection.OfType<Node>().Any() || selection.OfType<GroupNode>().Any(); }
        }

        protected internal void CutSelectionCallback()
        {
            CopySelectionCallback();
            DeleteSelectionOperation("Cut", AskUser.DontAskUser);
        }

        protected internal virtual bool canPaste
        {
            get { return CanPasteSerializedData(clipboard); }
        }

        protected internal void PasteCallback()
        {
            UnserializeAndPasteOperation("Paste", clipboard);
        }

        protected internal virtual bool canDuplicateSelection
        {
            get { return canCopySelection; }
        }

        protected internal void DuplicateSelectionCallback()
        {
            var elementsToCopySet = new HashSet<GraphElement>();

            CollectCopyableGraphElements(selection.OfType<GraphElement>(), elementsToCopySet);

            string serializedData = SerializeGraphElements(elementsToCopySet);

            UnserializeAndPasteOperation("Duplicate", serializedData);
        }

        protected internal virtual bool canDeleteSelection
        {
            get
            {
                return selection.Cast<GraphElement>().Where(e => e != null && (e.capabilities & Capabilities.Deletable) != 0).Any();
            }
        }

        protected internal void DeleteSelectionCallback(AskUser askUser)
        {
            DeleteSelectionOperation("Delete", askUser);
        }

        public delegate string SerializeGraphElementsDelegate(IEnumerable<GraphElement> elements);
        public delegate bool CanPasteSerializedDataDelegate(string data);
        public delegate void UnserializeAndPasteDelegate(string operationName, string data);
        public delegate void DeleteSelectionDelegate(string operationName, AskUser askUser);

        public SerializeGraphElementsDelegate serializeGraphElements { get; set; }
        public CanPasteSerializedDataDelegate canPasteSerializedData { get; set; }
        public UnserializeAndPasteDelegate unserializeAndPaste { get; set; }
        public DeleteSelectionDelegate deleteSelection { get; set; }

        const string m_SerializedDataMimeType = "application/vnd.unity.graphview.elements";

        protected string SerializeGraphElements(IEnumerable<GraphElement> elements)
        {
            if (serializeGraphElements != null)
            {
                string data = serializeGraphElements(elements);
                if (!string.IsNullOrEmpty(data))
                {
                    data = m_SerializedDataMimeType + " " + data;
                }
                return data;
            }
            else
            {
                return string.Empty;
            }
        }

        protected bool CanPasteSerializedData(string data)
        {
            if (canPasteSerializedData != null)
            {
                if (data.StartsWith(m_SerializedDataMimeType))
                {
                    return canPasteSerializedData(data.Substring(m_SerializedDataMimeType.Length + 1));
                }
                else
                {
                    return canPasteSerializedData(data);
                }
            }
            if (data.StartsWith(m_SerializedDataMimeType))
            {
                return true;
            }
            return false;
        }

        protected void UnserializeAndPasteOperation(string operationName, string data)
        {
            if (unserializeAndPaste != null)
            {
                if (data.StartsWith(m_SerializedDataMimeType))
                {
                    unserializeAndPaste(operationName, data.Substring(m_SerializedDataMimeType.Length + 1));
                }
                else
                {
                    unserializeAndPaste(operationName, data);
                }
            }
        }

        protected void DeleteSelectionOperation(string operationName, AskUser askUser)
        {
            if (deleteSelection != null)
            {
                deleteSelection(operationName, askUser);
            }
            else
            {
                DeleteSelection();
            }
        }

        public virtual List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(nap => nap.IsConnectable() &&
                nap.orientation == startPort.orientation &&
                nap.direction != startPort.direction &&
                nodeAdapter.GetAdapter(nap.source, startPort.source) != null)
                .ToList();
        }

        public void AddElement(GraphElement graphElement)
        {
            if (graphElement.IsResizable())
            {
                graphElement.Add(new Resizer());
                graphElement.style.borderBottom = 6;
            }

            int newLayer = graphElement.layer;
            if (!m_ContainerLayers.ContainsKey(newLayer))
            {
                AddLayer(newLayer);
            }
            GetLayer(newLayer).Add(graphElement);

            if (graphElement.presenter != null)
                graphElement.OnDataChanged();
        }

        public void RemoveElement(GraphElement graphElement)
        {
            graphElement.RemoveFromHierarchy();
        }

        // TODO: Remove when removing presenters.
        protected void InstantiateElement(GraphElementPresenter elementPresenter)
        {
            // call factory
            GraphElement newElem = typeFactory.Create(elementPresenter);

            if (newElem == null)
            {
                return;
            }

            newElem.SetPosition(elementPresenter.position);
            newElem.presenter = elementPresenter;

            if (elementPresenter.isFloating)
                Add(newElem);
            else
                AddElement(newElem);
        }

        // TODO: Remove when removing presenters.
        private EventPropagation DeleteSelectedPresenters()
        {
            // and DeleteSelection would call that method.
            if (presenter == null)
                return EventPropagation.Stop;

            var elementsToRemove = new HashSet<GraphElementPresenter>();
            foreach (var selectedElement in selection.Cast<GraphElement>()
                     .Where(e => e != null && e.presenter != null))
            {
                if ((selectedElement.presenter.capabilities & Capabilities.Deletable) == 0)
                    continue;

                elementsToRemove.Add(selectedElement.presenter);

                var connectorColl = selectedElement.GetPresenter<NodePresenter>();
                if (connectorColl == null)
                    continue;

                elementsToRemove.UnionWith(connectorColl.inputPorts.SelectMany(c => c.connections)
                    .Where(d => (d.capabilities & Capabilities.Deletable) != 0)
                    .Cast<GraphElementPresenter>());
                elementsToRemove.UnionWith(connectorColl.outputPorts.SelectMany(c => c.connections)
                    .Where(d => (d.capabilities & Capabilities.Deletable) != 0)
                    .Cast<GraphElementPresenter>());
            }

            foreach (var b in elementsToRemove)
                presenter.RemoveElement(b);

            // Notify the ends of connections that the connection is going way.
            foreach (var connection in elementsToRemove.OfType<EdgePresenter>())
            {
                connection.output = null;
                connection.input = null;

                if (connection.output != null)
                {
                    connection.output.Disconnect(connection);
                }

                if (connection.input != null)
                {
                    connection.input.Disconnect(connection);
                }
            }

            return (elementsToRemove.Count > 0) ? EventPropagation.Stop : EventPropagation.Continue;
        }

        private void CollectConnectedEgdes(HashSet<GraphElement> elementsToRemoveSet, Node node)
        {
            elementsToRemoveSet.UnionWith(node.inputContainer.Children().OfType<Port>().SelectMany(c => c.connections)
                .Where(d => (d.capabilities & Capabilities.Deletable) != 0)
                .Cast<GraphElement>());
            elementsToRemoveSet.UnionWith(node.outputContainer.Children().OfType<Port>().SelectMany(c => c.connections)
                .Where(d => (d.capabilities & Capabilities.Deletable) != 0)
                .Cast<GraphElement>());
        }

        private void CollectDeletableGraphElements(IEnumerable<GraphElement> elements, HashSet<GraphElement> elementsToRemoveSet)
        {
            CollectElements(elements, elementsToRemoveSet, e => (e.capabilities & Capabilities.Deletable) == Capabilities.Deletable);
        }

        public EventPropagation DeleteSelection()
        {
            // TODO: Remove when removing presenters.
            if (presenter != null)
                return DeleteSelectedPresenters();

            var elementsToRemoveSet = new HashSet<GraphElement>();

            CollectDeletableGraphElements(selection.OfType<GraphElement>(), elementsToRemoveSet);

            DeleteElements(elementsToRemoveSet);

            selection.Clear();

            return (elementsToRemoveSet.Count > 0) ? EventPropagation.Stop : EventPropagation.Continue;
        }

        public void DeleteElements(IEnumerable<GraphElement> elementsToRemove)
        {
            m_ElementsToRemove.Clear();
            foreach (GraphElement element in elementsToRemove)
                m_ElementsToRemove.Add(element);

            List<GraphElement> elementsToRemoveList = m_ElementsToRemove;
            if (graphViewChanged != null)
            {
                elementsToRemoveList = graphViewChanged(m_GraphViewChange).elementsToRemove;
            }

            // Notify the ends of connections that the connection is going way.
            foreach (var connection in elementsToRemoveList.OfType<Edge>())
            {
                if (connection.output != null)
                    connection.output.Disconnect(connection);

                if (connection.input != null)
                    connection.input.Disconnect(connection);

                connection.output = null;
                connection.input = null;
            }

            foreach (GraphElement element in elementsToRemoveList)
            {
                RemoveElement(element);
            }
        }

        public EventPropagation FrameAll()
        {
            return Frame(FrameType.All);
        }

        public EventPropagation FrameSelection()
        {
            return Frame(FrameType.Selection);
        }

        public EventPropagation FrameOrigin()
        {
            return Frame(FrameType.Origin);
        }

        public EventPropagation FramePrev()
        {
            if (contentViewContainer.childCount == 0)
                return EventPropagation.Continue;

            List<GraphElement> childrenList = graphElements.ToList().Where(e => e.IsSelectable() && !(e is Edge)).OrderByDescending(e => e.controlid).ToList();
            return FramePrevNext(childrenList);
        }

        public EventPropagation FrameNext()
        {
            if (contentViewContainer.childCount == 0)
                return EventPropagation.Continue;

            List<GraphElement> childrenList = graphElements.ToList().Where(e => e.IsSelectable() && !(e is Edge)).OrderBy(e => e.controlid).ToList();
            return FramePrevNext(childrenList);
        }

        // TODO: Do we limit to GraphElements or can we tab through ISelectable's?
        EventPropagation FramePrevNext(List<GraphElement> childrenList)
        {
            GraphElement graphElement = null;

            // Start from current selection, if any
            if (selection.Count != 0)
                graphElement = selection[0] as GraphElement;

            int idx = childrenList.IndexOf(graphElement);

            if (idx < 0)
                return EventPropagation.Continue;

            if (idx < childrenList.Count - 1)
                graphElement = childrenList[idx + 1];
            else
                graphElement = childrenList[0];

            // New selection...
            ClearSelection();
            AddToSelection(graphElement);

            // ...and frame this new selection
            return Frame(FrameType.Selection);
        }

        EventPropagation Frame(FrameType frameType)
        {
            var rectToFit = contentViewContainer.layout;
            var frameTranslation = Vector3.zero;
            var frameScaling = Vector3.one;

            if (frameType == FrameType.Selection &&
                (selection.Count == 0 || !selection.Any(e => e.IsSelectable() && !(e is Edge))))
                frameType = FrameType.All;

            if (frameType == FrameType.Selection)
            {
                var graphElement = selection[0] as GraphElement;
                if (graphElement != null)
                    rectToFit = graphElement.localBound;

                rectToFit = selection.OfType<GraphElement>()
                    .Aggregate(rectToFit, (current, e) => RectUtils.Encompass(current, e.localBound));
                CalculateFrameTransform(rectToFit, layout, k_FrameBorder, out frameTranslation, out frameScaling);
            }
            else if (frameType == FrameType.All)
            {
                rectToFit = CalculateRectToFitAll(contentViewContainer);
                CalculateFrameTransform(rectToFit, layout, k_FrameBorder, out frameTranslation, out frameScaling);
            } // else keep going if (frameType == FrameType.Origin)

            if (m_FrameAnimate)
            {
                // TODO Animate framing
                // RMAnimation animation = new RMAnimation();
                // parent.Animate(parent)
                //       .Lerp(new string[] {"m_Scale", "m_Translation"},
                //             new object[] {parent.scale, parent.translation},
                //             new object[] {frameScaling, frameTranslation}, 0.08f);
            }
            else
            {
                Matrix4x4.TRS(frameTranslation, Quaternion.identity, frameScaling);

                UpdateViewTransform(frameTranslation, frameScaling);
            }

            contentViewContainer.Dirty(ChangeType.Repaint);

            UpdatePersistedViewTransform();

            return EventPropagation.Stop;
        }

        public virtual Rect CalculateRectToFitAll(VisualElement container)
        {
            var rectToFit = container.layout;
            var reachedFirstChild = false;

            graphElements.ForEach(ge =>
                {
                    if (ge is Edge)
                    {
                        return;
                    }

                    if (!reachedFirstChild)
                    {
                        rectToFit = ge.localBound;
                        reachedFirstChild = true;
                    }
                    else
                    {
                        rectToFit = RectUtils.Encompass(rectToFit, ge.localBound);
                    }
                });

            return rectToFit;
        }

        public static void CalculateFrameTransform(Rect rectToFit, Rect clientRect, int border, out Vector3 frameTranslation, out Vector3 frameScaling)
        {
            // bring slightly smaller screen rect into GUI space
            var screenRect = new Rect
            {
                xMin = border,
                xMax = clientRect.width - border,
                yMin = border,
                yMax = clientRect.height - border
            };

            Matrix4x4 m = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
            Rect identity = GUIUtility.ScreenToGUIRect(screenRect);

            // measure zoom level necessary to fit the canvas rect into the screen rect
            float zoomLevel = Math.Min(identity.width / rectToFit.width, identity.height / rectToFit.height);

            // clamp
            zoomLevel = Mathf.Clamp(zoomLevel, ContentZoomer.DefaultMinScale, 1.0f);

            var transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(zoomLevel, zoomLevel, 1.0f));

            var edge = new Vector2(clientRect.width, clientRect.height);
            var origin = new Vector2(0, 0);

            var r = new Rect
            {
                min = origin,
                max = edge
            };

            var parentScale = new Vector3(transform.GetColumn(0).magnitude,
                    transform.GetColumn(1).magnitude,
                    transform.GetColumn(2).magnitude);
            Vector2 offset = r.center - (rectToFit.center * parentScale.x);

            // Update output values before leaving
            frameTranslation = new Vector3(offset.x, offset.y, 0.0f);
            frameScaling = parentScale;

            GUI.matrix = m;
        }
    }
}
