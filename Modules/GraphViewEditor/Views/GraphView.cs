// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    internal interface IGraphViewSelection
    {
        int version { get; set; }

        HashSet<string> selectedElements { get; }
    }

    internal class GraphViewUndoRedoSelection : ScriptableObject, IGraphViewSelection, ISerializationCallbackReceiver
    {
        [SerializeField]
        private int m_Version;

        [SerializeField]
        string[] m_SelectedElementsArray;

        [NonSerialized]
        private HashSet<string> m_SelectedElements = new HashSet<string>();

        public int version
        {
            get { return m_Version; }
            set { m_Version = value; }
        }

        public HashSet<string> selectedElements => m_SelectedElements;

        public void OnBeforeSerialize()
        {
            if (m_SelectedElements.Count == 0)
                return;

            m_SelectedElementsArray = new string[m_SelectedElements.Count];

            m_SelectedElements.CopyTo(m_SelectedElementsArray);
        }

        public void OnAfterDeserialize()
        {
            m_SelectedElements.Clear();

            if (m_SelectedElementsArray == null || m_SelectedElementsArray.Length == 0)
                return;

            foreach (string guid in m_SelectedElementsArray)
            {
                m_SelectedElements.Add(guid);
            }
        }
    }

    public struct GraphViewChange
    {
        // Operations Pending
        public List<GraphElement> elementsToRemove;
        public List<Edge> edgesToCreate;

        // Operations Completed
        public List<GraphElement> movedElements;
        public Vector2 moveDelta;
    }

    public struct NodeCreationContext
    {
        public Vector2 screenMousePosition;
        public VisualElement target;
        public int index;
    }

    public abstract class GraphView : VisualElement, ISelection
    {
        // Layer class. Used for queries below.
        internal class Layer : VisualElement {}

        // Delegates and Callbacks
        public Action<NodeCreationContext> nodeCreationRequest { get; set; }
        internal IInsertLocation currentInsertLocation {get; set; }


        public delegate GraphViewChange GraphViewChanged(GraphViewChange graphViewChange);
        public GraphViewChanged graphViewChanged { get; set; }

        public Action<Group, string> groupTitleChanged { get; set; }
        public Action<Group, IEnumerable<GraphElement>> elementsAddedToGroup { get; set; }
        public Action<Group, IEnumerable<GraphElement>> elementsRemovedFromGroup { get; set; }

        public Action<StackNode, int, IEnumerable<GraphElement>> elementsInsertedToStackNode { get; set; }
        public Action<StackNode, IEnumerable<GraphElement>> elementsRemovedFromStackNode { get; set; }

        private GraphViewChange m_GraphViewChange;
        private List<GraphElement> m_ElementsToRemove;

        public delegate void ElementResized(VisualElement visualElement);
        public ElementResized elementResized { get; set; }

        public delegate void ViewTransformChanged(GraphView graphView);
        public ViewTransformChanged viewTransformChanged { get; set; }

        [Serializable]
        class PersistedSelection : IGraphViewSelection, ISerializationCallbackReceiver
        {
            [SerializeField]
            private int m_Version;

            [SerializeField]
            string[] m_SelectedElementsArray;

            [NonSerialized]
            private HashSet<string> m_SelectedElements = new HashSet<string>();

            public int version
            {
                get { return m_Version; }
                set { m_Version = value; }
            }

            public HashSet<string> selectedElements => m_SelectedElements;

            public void OnBeforeSerialize()
            {
                if (m_SelectedElements.Count == 0)
                    return;

                m_SelectedElementsArray = new string[m_SelectedElements.Count];

                m_SelectedElements.CopyTo(m_SelectedElementsArray);
            }

            public void OnAfterDeserialize()
            {
                if (m_SelectedElementsArray == null || m_SelectedElementsArray.Length == 0)
                    return;

                m_SelectedElements.Clear();

                foreach (string guid in m_SelectedElementsArray)
                {
                    m_SelectedElements.Add(guid);
                }
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

        VisualElement graphViewContainer { get; }
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
            float validateFloat = newPosition.x + newPosition.y + newPosition.z + newScale.x + newScale.y + newScale.z;
            if (float.IsInfinity(validateFloat) || float.IsNaN(validateFloat))
                return;

            contentViewContainer.transform.position = newPosition;
            contentViewContainer.transform.scale = newScale;

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

        readonly Dictionary<int, Layer> m_ContainerLayers = new Dictionary<int, Layer>();

        public override VisualElement contentContainer // Contains full content, potentially partially visible
        {
            get { return graphViewContainer; }
        }

        protected GraphView()
        {
            AddToClassList("graphView");

            selection = new List<ISelectable>();
            style.overflow = Overflow.Hidden;

            style.flexDirection = FlexDirection.Column;

            graphViewContainer = new VisualElement();
            graphViewContainer.style.flex = new Flex(1);
            graphViewContainer.style.flexGrow = 1;
            graphViewContainer.pickingMode = PickingMode.Ignore;
            shadow.Add(graphViewContainer);

            contentViewContainer = new ContentViewContainer
            {
                name = "contentViewContainer",
                pickingMode = PickingMode.Ignore
            };

            // make it absolute and 0 sized so it acts as a transform to move children to and fro
            graphViewContainer.Add(contentViewContainer);

            AddStyleSheetPath("StyleSheets/GraphView/GraphView.uss");
            graphElements = contentViewContainer.Query<GraphElement>().Where(e => !(e is Port)).Build();
            nodes = contentViewContainer.Query<Node>().Build();
            edges = this.Query<Layer>().Children<Edge>().Build();
            ports = contentViewContainer.Query().Children<Layer>().Descendents<Port>().Build();

            m_ElementsToRemove = new List<GraphElement>();
            m_GraphViewChange.elementsToRemove = m_ElementsToRemove;

            isReframable = true;
            focusIndex = 0;

            RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);
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
            if (m_PersistedSelection == null)
                return;
            if (graphViewSelection.selectedElements.Count == selection.Count && graphViewSelection.version == m_SavedSelectionVersion)
                return;

            // Update both selection objects' versions.
            m_GraphViewUndoRedoSelection.version = graphViewSelection.version;
            m_PersistedSelection.version = graphViewSelection.version;

            ClearSelectionNoUndoRecord();
            foreach (string guid in graphViewSelection.selectedElements)
            {
                var element = GetElementByGuid(guid);
                if (element == null)
                    continue;

                AddToSelectionNoUndoRecord(element);
            }

            m_SavedSelectionVersion = graphViewSelection.version;

            IGraphViewSelection selectionObjectToSync = m_GraphViewUndoRedoSelection;
            if (graphViewSelection is GraphViewUndoRedoSelection)
                selectionObjectToSync = m_PersistedSelection;

            selectionObjectToSync.selectedElements.Clear();

            foreach (string guid in graphViewSelection.selectedElements)
            {
                selectionObjectToSync.selectedElements.Add(guid);
            }
        }

        internal void RestorePersitentSelectionForElement(GraphElement element)
        {
            if (m_PersistedSelection == null)
                return;

            if (m_PersistedSelection.selectedElements.Count == selection.Count && m_PersistedSelection.version == m_SavedSelectionVersion)
                return;

            if (string.IsNullOrEmpty(element.persistenceKey))
                return;

            if (m_PersistedSelection.selectedElements.Contains(element.persistenceKey))
            {
                AddToSelectionNoUndoRecord(element);
            }
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
            Layer newLayer = new Layer { pickingMode = PickingMode.Ignore };

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
                AddLayer(element.layer);

            bool selected = element.selected;
            if (selected)
                element.UnregisterCallback<DetachFromPanelEvent>(OnSelectedElementDetachedFromPanel);

            GetLayer(element.layer).Add(element);

            if (selected)
                element.RegisterCallback<DetachFromPanelEvent>(OnSelectedElementDetachedFromPanel);
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

        // ISelection implementation
        public List<ISelectable> selection { get; protected set; }

        // functions to ISelection extensions
        public virtual void AddToSelection(ISelectable selectable)
        {
            var graphElement = selectable as GraphElement;
            if (graphElement == null)
                return;

            if (selection.Contains(selectable))
                return;

            AddToSelectionNoUndoRecord(graphElement);

            if (ShouldRecordUndo())
            {
                RecordSelectionUndoPre();
                m_GraphViewUndoRedoSelection.selectedElements.Add(graphElement.persistenceKey);
                m_PersistedSelection.selectedElements.Add(graphElement.persistenceKey);
                RecordSelectionUndoPost();
            }
        }

        private void AddToSelectionNoUndoRecord(GraphElement graphElement)
        {
            graphElement.selected = true;
            selection.Add(graphElement);
            graphElement.OnSelected();

            // To ensure that the selected GraphElement gets unselected if it is removed from the GraphView.
            graphElement.RegisterCallback<DetachFromPanelEvent>(OnSelectedElementDetachedFromPanel);

            contentViewContainer.MarkDirtyRepaint();
        }

        private void RemoveFromSelectionNoUndoRecord(ISelectable selectable)
        {
            var graphElement = selectable as GraphElement;
            if (graphElement == null)
                return;
            graphElement.selected = false;

            selection.Remove(selectable);
            graphElement.OnUnselected();
            graphElement.UnregisterCallback<DetachFromPanelEvent>(OnSelectedElementDetachedFromPanel);
            contentViewContainer.MarkDirtyRepaint();
        }

        public virtual void RemoveFromSelection(ISelectable selectable)
        {
            var graphElement = selectable as GraphElement;
            if (graphElement == null)
                return;

            if (!selection.Contains(selectable))
                return;

            RemoveFromSelectionNoUndoRecord(selectable);

            if (ShouldRecordUndo())
            {
                RecordSelectionUndoPre();
                m_GraphViewUndoRedoSelection.selectedElements.Remove(graphElement.persistenceKey);
                m_PersistedSelection.selectedElements.Remove(graphElement.persistenceKey);
                RecordSelectionUndoPost();
            }
        }

        private bool ClearSelectionNoUndoRecord()
        {
            foreach (var graphElement in selection.OfType<GraphElement>())
            {
                graphElement.selected = false;

                graphElement.OnUnselected();
                graphElement.UnregisterCallback<DetachFromPanelEvent>(OnSelectedElementDetachedFromPanel);
            }

            bool selectionWasNotEmpty = selection.Any();
            selection.Clear();
            contentViewContainer.MarkDirtyRepaint();

            return selectionWasNotEmpty;
        }

        public virtual void ClearSelection()
        {
            bool selectionWasNotEmpty = ClearSelectionNoUndoRecord();

            if (ShouldRecordUndo() && selectionWasNotEmpty)
            {
                RecordSelectionUndoPre();
                m_GraphViewUndoRedoSelection.selectedElements.Clear();
                m_PersistedSelection.selectedElements.Clear();
                RecordSelectionUndoPost();
            }
        }

        private void OnSelectedElementDetachedFromPanel(DetachFromPanelEvent evt)
        {
            RemoveFromSelectionNoUndoRecord(evt.target as ISelectable);
        }

        public virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is UIElements.GraphView.GraphView && nodeCreationRequest != null)
            {
                evt.menu.AppendAction("Create Node", OnContextMenuNodeCreate, DropdownMenu.MenuAction.AlwaysEnabled);
                evt.menu.AppendSeparator();
            }
            if (evt.target is UIElements.GraphView.GraphView || evt.target is Node || evt.target is Group)
            {
                evt.menu.AppendAction("Cut", (a) => { CutSelectionCallback(); },
                    (a) => { return canCutSelection ? DropdownMenu.MenuAction.StatusFlags.Normal : DropdownMenu.MenuAction.StatusFlags.Disabled; });
            }
            if (evt.target is UIElements.GraphView.GraphView || evt.target is Node || evt.target is Group)
            {
                evt.menu.AppendAction("Copy", (a) => { CopySelectionCallback(); },
                    (a) => { return canCopySelection ? DropdownMenu.MenuAction.StatusFlags.Normal : DropdownMenu.MenuAction.StatusFlags.Disabled; });
            }
            if (evt.target is UIElements.GraphView.GraphView)
            {
                evt.menu.AppendAction("Paste", (a) => { PasteCallback(); },
                    (a) => { return canPaste ? DropdownMenu.MenuAction.StatusFlags.Normal : DropdownMenu.MenuAction.StatusFlags.Disabled; });
            }
            if (evt.target is UIElements.GraphView.GraphView || evt.target is Node || evt.target is Group || evt.target is Edge)
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Delete", (a) => { DeleteSelectionCallback(AskUser.DontAskUser); },
                    (a) => { return canDeleteSelection ? DropdownMenu.MenuAction.StatusFlags.Normal : DropdownMenu.MenuAction.StatusFlags.Disabled; });
            }
            if (evt.target is UIElements.GraphView.GraphView || evt.target is Node || evt.target is Group)
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Duplicate", (a) => { DuplicateSelectionCallback(); },
                    (a) => { return canDuplicateSelection ? DropdownMenu.MenuAction.StatusFlags.Normal : DropdownMenu.MenuAction.StatusFlags.Disabled; });
                evt.menu.AppendSeparator();
            }
        }

        void OnContextMenuNodeCreate(DropdownMenu.MenuAction a)
        {
            RequestNodeCreation(null, -1, a.eventInfo.mousePosition);
        }

        private void RequestNodeCreation(VisualElement target, int index, Vector2 position)
        {
            if (nodeCreationRequest == null)
                return;

            GUIView guiView = elementPanel.ownerObject as GUIView;
            if (guiView == null)
                return;

            Vector2 screenPoint = guiView.screenPosition.position + position;

            nodeCreationRequest(new NodeCreationContext() { screenMousePosition = screenPoint, target = target, index = index});
        }

        protected internal override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (elementPanel != null && elementPanel.contextualMenuManager != null)
            {
                elementPanel.contextualMenuManager.DisplayMenuIfEventMatches(evt, this);
            }
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.GetEventTypeId() == DetachFromPanelEvent.TypeId())
            {
                DetachFromPanelEvent dtpe = (DetachFromPanelEvent)evt;

                if (dtpe.destinationPanel == null)
                {
                    Undo.ClearUndo(m_GraphViewUndoRedoSelection);
                    Undo.undoRedoPerformed -= UndoRedoPerformed;
                    ScriptableObject.DestroyImmediate(m_GraphViewUndoRedoSelection);
                    m_GraphViewUndoRedoSelection = null;

                    if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
                        ClearSavedSelection();
                }
            }
            else if (evt.GetEventTypeId() == AttachToPanelEvent.TypeId())
            {
                AttachToPanelEvent atpe = (AttachToPanelEvent)evt;

                if (atpe.originPanel == null)
                {
                    Undo.undoRedoPerformed += UndoRedoPerformed;
                    m_GraphViewUndoRedoSelection = ScriptableObject.CreateInstance<GraphViewUndoRedoSelection>();
                    m_GraphViewUndoRedoSelection.hideFlags = HideFlags.HideAndDontSave;
                }
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

            if (MouseCaptureController.IsMouseCaptured())
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
                case ' ':
                    result = OnInsertNodeKeyDown(evt);
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

        EventPropagation OnInsertNodeKeyDown(KeyDownEvent evt)
        {
            InsertInfo insertInfo = InsertInfo.nil;
            Vector2 worldPosition = evt.originalMousePosition;

            if (currentInsertLocation != null)
            {
                currentInsertLocation.GetInsertInfo(evt.originalMousePosition, out insertInfo);

                if (insertInfo.target != null)
                {
                    worldPosition = insertInfo.target.LocalToWorld(insertInfo.localPosition);
                }
            }

            RequestNodeCreation(insertInfo.target, insertInfo.index, worldPosition);

            return EventPropagation.Stop;
        }

        void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (MouseCaptureController.IsMouseCaptured())
                return;

            if ((evt.commandName == EventCommandNames.Copy && canCopySelection)
                || (evt.commandName == EventCommandNames.Paste && canPaste)
                || (evt.commandName == EventCommandNames.Duplicate && canDuplicateSelection)
                || (evt.commandName == EventCommandNames.Cut && canCutSelection)
                || ((evt.commandName == EventCommandNames.Delete || evt.commandName == EventCommandNames.SoftDelete) && canDeleteSelection))
            {
                evt.StopPropagation();
                if (evt.imguiEvent != null)
                {
                    evt.imguiEvent.Use();
                }
            }
            else if (evt.commandName == EventCommandNames.FrameSelected)
            {
                evt.StopPropagation();
                if (evt.imguiEvent != null)
                {
                    evt.imguiEvent.Use();
                }
            }
        }

        public enum AskUser
        {
            AskUser,
            DontAskUser
        }

        void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (MouseCaptureController.IsMouseCaptured())
                return;

            if (evt.commandName == EventCommandNames.Copy)
            {
                CopySelectionCallback();
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNames.Paste)
            {
                PasteCallback();
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNames.Duplicate)
            {
                DuplicateSelectionCallback();
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNames.Cut)
            {
                CutSelectionCallback();
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNames.Delete)
            {
                DeleteSelectionCallback(AskUser.DontAskUser);
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNames.SoftDelete)
            {
                DeleteSelectionCallback(AskUser.AskUser);
                evt.StopPropagation();
            }
            else if (evt.commandName == EventCommandNames.FrameSelected)
            {
                FrameSelection();
                evt.StopPropagation();
            }

            if (evt.isPropagationStopped && evt.imguiEvent != null)
            {
                evt.imguiEvent.Use();
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
            get { return selection.OfType<Node>().Any() || selection.OfType<Group>().Any(); }
        }

        private void CollectElements(IEnumerable<GraphElement> elements, HashSet<GraphElement> collectedElementSet, Func<GraphElement, bool> conditionFunc)
        {
            foreach (var element in elements.Where(e => e != null && !collectedElementSet.Contains(e) && conditionFunc(e)))
            {
                var node = element as Node;

                if (node != null)
                {
                    CollectConnectedEgdes(collectedElementSet, node);

                    StackNode stackNode = node as StackNode;

                    if (stackNode != null)
                    {
                        CollectElements(stackNode.Children().OfType<GraphElement>(), collectedElementSet, conditionFunc);
                    }
                }
                else
                {
                    var groupNode = element as Group;

                    // If the selected element is a group then visit its contained element
                    if (groupNode != null)
                    {
                        CollectElements(groupNode.containedElements, collectedElementSet, conditionFunc);
                    }
                }

                collectedElementSet.Add(element);
            }
        }

        protected internal virtual void CollectCopyableGraphElements(IEnumerable<GraphElement> elements, HashSet<GraphElement> elementsToCopySet)
        {
            CollectElements(elements, elementsToCopySet, e => (e is Node || e is Group));
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
            get { return selection.OfType<Node>().Any() || selection.OfType<Group>().Any(); }
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
            return ports.ToList().Where(nap =>
                nap.direction != startPort.direction &&
                nap.node != startPort.node &&
                nodeAdapter.GetAdapter(nap.source, startPort.source) != null)
                .ToList();
        }

        public void AddElement(GraphElement graphElement)
        {
            if (graphElement.IsResizable())
            {
                graphElement.shadow.Add(new Resizer());
                graphElement.style.borderBottomWidth = 6;
            }

            int newLayer = graphElement.layer;
            if (!m_ContainerLayers.ContainsKey(newLayer))
            {
                AddLayer(newLayer);
            }
            GetLayer(newLayer).Add(graphElement);

            // Attempt to restore selection on the new element if it
            // was previously selected (same GUID).
            RestorePersitentSelectionForElement(graphElement);
        }

        public void RemoveElement(GraphElement graphElement)
        {
            // TODO : Find a better way to remove a graphElement from its scope when it is removed from the GraphView.
            Scope scope = graphElement.GetContainingScope();
            if (scope != null)
            {
                scope.RemoveElement(graphElement);
            }

            StackNode stack = graphElement.parent as StackNode;
            if (stack != null)
            {
                stack.RemoveElement(graphElement);
                if (elementsRemovedFromStackNode != null)
                {
                    elementsRemovedFromStackNode(stack, new[] {graphElement});
                }
            }

            graphElement.RemoveFromHierarchy();
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

        public virtual EventPropagation DeleteSelection()
        {
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

        public EventPropagation FramePrev(Func<GraphElement, bool> predicate)
        {
            if (this.contentViewContainer.childCount == 0)
                return EventPropagation.Continue;
            List<GraphElement> list = graphElements.ToList().Where(predicate).ToList();
            list.Reverse();
            return this.FramePrevNext(list);
        }

        public EventPropagation FrameNext(Func<GraphElement, bool> predicate)
        {
            if (this.contentViewContainer.childCount == 0)
                return EventPropagation.Continue;
            return this.FramePrevNext(graphElements.ToList().Where(predicate).ToList());
        }

        // TODO: Do we limit to GraphElements or can we tab through ISelectable's?
        EventPropagation FramePrevNext(List<GraphElement> childrenList)
        {
            GraphElement graphElement = null;

            // Start from current selection, if any
            if (selection.Count != 0)
                graphElement = selection[0] as GraphElement;

            int idx = childrenList.IndexOf(graphElement);

            if (idx >= 0 && idx < childrenList.Count - 1)
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
            Rect rectToFit = contentViewContainer.layout;
            Vector3 frameTranslation = Vector3.zero;
            Vector3 frameScaling = Vector3.one;

            if (frameType == FrameType.Selection &&
                (selection.Count == 0 || !selection.Any(e => e.IsSelectable() && !(e is Edge))))
                frameType = FrameType.All;

            if (frameType == FrameType.Selection)
            {
                var graphElement = selection[0] as GraphElement;
                if (graphElement != null)
                    rectToFit = graphElement.ChangeCoordinatesTo(contentViewContainer, graphElement.rect);

                rectToFit = selection.Cast<GraphElement>()
                    .Aggregate(rectToFit, (current, e) => RectUtils.Encompass(current, e.ChangeCoordinatesTo(contentViewContainer, e.rect)));
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

            contentViewContainer.MarkDirtyRepaint();

            UpdatePersistedViewTransform();

            return EventPropagation.Stop;
        }

        public virtual Rect CalculateRectToFitAll(VisualElement container)
        {
            Rect rectToFit = container.layout;
            bool reachedFirstChild = false;

            graphElements.ForEach(ge =>
            {
                if (ge is Edge || ge is Port)
                {
                    return;
                }

                if (!reachedFirstChild)
                {
                    rectToFit = ge.ChangeCoordinatesTo(contentViewContainer, ge.rect);
                    reachedFirstChild = true;
                }
                else
                {
                    rectToFit = RectUtils.Encompass(rectToFit, ge.ChangeCoordinatesTo(contentViewContainer, ge.rect));
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
