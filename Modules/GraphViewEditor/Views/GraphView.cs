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

    internal
    struct GraphViewChange
    {
        // Operations Pending
        public List<GraphElement> elementsToRemove;
        public List<Edge> edgesToCreate;

        // Operations Completed
        public List<GraphElement> movedElements;
    }

    internal
    abstract class GraphView : DataWatchContainer, ISelection
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

        public delegate GraphViewChange GraphViewChanged(GraphViewChange graphViewChange);
        public GraphViewChanged graphViewChanged { get; set; }

        private GraphViewChange m_GraphViewChange;
        private List<GraphElement> m_ElementsToRemove;

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
            ports = contentViewContainer.Query().Children<Layer>().Descendents<Port>().Build();

            m_ElementsToRemove = new List<GraphElement>();
            m_GraphViewChange.elementsToRemove = m_ElementsToRemove;

            isReframable = true;
            focusIndex = 0;

            RegisterCallback<IMGUIEvent>(OnValidateCommand);
            RegisterCallback<IMGUIEvent>(OnExecuteCommand);
            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
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

        private void RecordSelectionUndoPost()
        {
            m_GraphViewUndoRedoSelection.version++;
            m_SavedSelectionVersion = m_GraphViewUndoRedoSelection.version;

            m_PersistedSelection.version++;
            SavePersistentData();
        }

        void AddLayer(int index)
        {
            m_ContainerLayers.Add(index, new Layer { clippingOptions = ClippingOptions.NoClipping, pickingMode = PickingMode.Ignore });

            foreach (var layer in m_ContainerLayers.OrderBy(t => t.Key).Select(t => t.Value))
            {
                if (layer.parent != null)
                    contentViewContainer.Remove(layer);
                contentViewContainer.Add(layer);
            }
        }

        VisualElement GetLayer(int index)
        {
            return m_ContainerLayers[index];
        }

        public UQuery.QueryState<GraphElement> graphElements { get; private set; }
        public UQuery.QueryState<Node> nodes { get; private set; }
        public UQuery.QueryState<Port> ports;

        [Serializable]
        class PersistedViewTransform
        {
            public Vector3 position = Vector3.zero;
            public Vector3 scale = Vector3.one;
        }
        PersistedViewTransform m_PersistedViewTransform;

        ContentZoomer m_Zoomer;
        int m_ZoomerMaxElementCountWithPixelCacheRegen = 100;
        Vector3 m_MinScale = ContentZoomer.DefaultMinScale;
        Vector3 m_MaxScale = ContentZoomer.DefaultMaxScale;

        public Vector3 minScale
        {
            get { return m_MinScale; }
        }

        public Vector3 maxScale
        {
            get { return m_MaxScale; }
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

        public void SetupZoom(Vector3 minScaleSetup, Vector3 maxScaleSetup)
        {
            m_MinScale = minScaleSetup;
            m_MaxScale = maxScaleSetup;
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
                    m_Zoomer = new ContentZoomer(m_MinScale, m_MaxScale);
                    this.AddManipulator(m_Zoomer);
                }
                else
                {
                    m_Zoomer.minScale = m_MinScale;
                    m_Zoomer.maxScale = m_MaxScale;
                }
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

            transformScale.x = Mathf.Max(Mathf.Min(maxScale.x, transformScale.x), minScale.x);
            transformScale.y = Mathf.Max(Mathf.Min(maxScale.y, transformScale.y), minScale.y);

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
                    c.parent.Remove(c);
                    selection.Remove(c);
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
                if ((elementPresenter.capabilities & Capabilities.Floating) == 0)
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
            graphElement.OnSelected();
            graphElement.selected = true;

            // TODO: Remove when removing presenters.
            if (graphElement.presenter != null)
                graphElement.presenter.selected = true;

            selection.Add(selectable);
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

        void OnEnterPanel(AttachToPanelEvent e)
        {
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
                if ((e.commandName == "Copy" && canCopySelection)
                    || (e.commandName == "Paste" && canPaste)
                    || (e.commandName == "Duplicate" && canDuplicateSelection)
                    || (e.commandName == "Cut" && canCutSelection)
                    || ((e.commandName == "Delete" || e.commandName == "SoftDelete") && canDeleteSelection))
                {
                    evt.StopPropagation();
                    e.Use();
                }
                else if (e.commandName == "FrameSelected")
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
                if (e.commandName == "Copy")
                {
                    CopySelectionCallback();
                    evt.StopPropagation();
                }
                else if (e.commandName == "Paste")
                {
                    PasteCallback();
                    evt.StopPropagation();
                }
                else if (e.commandName == "Duplicate")
                {
                    DuplicateSelectionCallback();
                    evt.StopPropagation();
                }
                else if (e.commandName == "Cut")
                {
                    CutSelectionCallback();
                    evt.StopPropagation();
                }
                else if (e.commandName == "Delete")
                {
                    DeleteSelectionCallback(AskUser.DontAskUser);
                    evt.StopPropagation();
                }
                else if (e.commandName == "SoftDelete")
                {
                    DeleteSelectionCallback(AskUser.AskUser);
                    evt.StopPropagation();
                }
                else if (e.commandName == "FrameSelected")
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
            get { return selection.Count > 0; }
        }

        protected internal void CopySelectionCallback()
        {
            string data = SerializeGraphElements(selection.OfType<GraphElement>());
            if (!string.IsNullOrEmpty(data))
            {
                clipboard = data;
            }
        }

        protected internal virtual bool canCutSelection
        {
            get { return canCopySelection; }
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
            string serializedData = SerializeGraphElements(selection.OfType<GraphElement>());
            UnserializeAndPasteOperation("Duplicate", serializedData);
        }

        protected internal virtual bool canDeleteSelection
        {
            get { return canCopySelection; }
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

            bool attachToContainer = !graphElement.IsFloating();
            if (attachToContainer)
            {
                int newLayer = graphElement.layer;
                if (!m_ContainerLayers.ContainsKey(newLayer))
                {
                    AddLayer(newLayer);
                }
                GetLayer(newLayer).Add(graphElement);
            }
            else
            {
                Add(graphElement);
            }
            if (graphElement.presenter != null)
                graphElement.OnDataChanged();
        }

        public void RemoveElement(GraphElement graphElement)
        {
            bool attachToContainer = !graphElement.IsFloating();
            if (attachToContainer)
            {
                int layer = graphElement.layer;
                if (m_ContainerLayers.ContainsKey(layer))
                    GetLayer(layer).Remove(graphElement);
            }
            else
            {
                Remove(graphElement);
            }
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

        public EventPropagation DeleteSelection()
        {
            // TODO: Remove when removing presenters.
            if (presenter != null)
                return DeleteSelectedPresenters();

            var elementsToRemoveSet = new HashSet<GraphElement>();
            foreach (var selectedElement in selection.Cast<GraphElement>().Where(e => e != null))
            {
                if ((selectedElement.capabilities & Capabilities.Deletable) == 0)
                    continue;

                elementsToRemoveSet.Add(selectedElement);

                var connectorColl = selectedElement as Node;
                if (connectorColl == null)
                    continue;

                elementsToRemoveSet.UnionWith(connectorColl.inputContainer.Children().Cast<Port>().SelectMany(c => c.connections)
                    .Where(d => (d.capabilities & Capabilities.Deletable) != 0)
                    .Cast<GraphElement>());
                elementsToRemoveSet.UnionWith(connectorColl.outputContainer.Children().Cast<Port>().SelectMany(c => c.connections)
                    .Where(d => (d.capabilities & Capabilities.Deletable) != 0)
                    .Cast<GraphElement>());
            }

            m_ElementsToRemove.Clear();
            foreach (GraphElement element in elementsToRemoveSet)
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

            foreach (var elementToRemove in elementsToRemoveList)
            {
                RemoveElement(elementToRemove);
            }

            selection.Clear();

            return (elementsToRemoveList.Count > 0) ? EventPropagation.Stop : EventPropagation.Continue;
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

            List<GraphElement> childrenList = graphElements.ToList();
            childrenList.Reverse();
            return FramePrevNext(childrenList);
        }

        public EventPropagation FrameNext()
        {
            if (contentViewContainer.childCount == 0)
                return EventPropagation.Continue;
            return FramePrevNext(graphElements.ToList());
        }

        // TODO: Do we limit to GraphElements or can we tab through ISelectable's?
        EventPropagation FramePrevNext(List<GraphElement> childrenEnum)
        {
            GraphElement graphElement = null;

            // Start from current selection, if any
            if (selection.Count != 0)
                graphElement = selection[0] as GraphElement;

            for (int i = 0; i < childrenEnum.Count; i++)
            {
                if (childrenEnum[i] == graphElement)
                {
                    if (i < childrenEnum.Count - 1)
                    {
                        graphElement = childrenEnum[i + 1];
                    }
                    else
                    {
                        graphElement = childrenEnum[0];
                    }
                    break;
                }
            }

            if (graphElement == null)
                return EventPropagation.Continue;

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

            if (frameType == FrameType.Selection)
            {
                // Now calculate rectangle to fit all selected elements
                if (selection.Count == 0)
                    return EventPropagation.Continue;

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
                    if ((ge.capabilities & Capabilities.Floating) != 0 ||
                        (ge is Edge))
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
            zoomLevel = Mathf.Clamp(zoomLevel, ContentZoomer.DefaultMinScale.y, 1.0f);

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
