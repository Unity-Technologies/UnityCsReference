// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.GraphView
{
    public class Placemat : GraphElement
    {
        internal static readonly Vector2 k_DefaultCollapsedSize = new Vector2(200, 42);
        static readonly Color k_DefaultColor = new Color(0.15f, 0.19f, 0.19f);
        static readonly int k_SelectRectOffset = 3;

        protected GraphView m_GraphView;

        TextField m_TitleField;
        ResizableElement m_Resizer;
        Button m_CollapseButton;

        internal void Init(GraphView graphView)
        {
            m_GraphView = graphView;

            var template = EditorGUIUtility.Load("UXML/GraphView/PlacematElement.uxml") as VisualTreeAsset;
            if (template != null)
                template.CloneTree(this);

            AddStyleSheetPath("StyleSheets/GraphView/Placemat.uss");
            AddToClassList("placemat");
            AddToClassList("selectable");

            pickingMode = PickingMode.Position;

            capabilities |= Capabilities.Deletable | Capabilities.Movable | Capabilities.Selectable | Capabilities.Copiable;
            capabilities &= ~Capabilities.Ascendable;

            focusable = true;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            m_TitleField = this.Q<TextField>();
            m_TitleField.isDelayed = true;
            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));

            m_CollapseButton = this.Q<Button>();
            m_CollapseButton.clicked += () => Collapsed = !Collapsed;
            m_CollapseButton.AddToClassList("placematCollapse");

            m_Resizer = this.Q<ResizableElement>();

            if (Collapsed)
            {
                m_Resizer.style.visibility = Visibility.Hidden;
                m_CollapseButton.RemoveFromClassList("icon-expanded");
            }
            else
            {
                m_Resizer.style.visibility = StyleKeyword.Null;
                m_CollapseButton.AddToClassList("icon-expanded");
            }
        }

        string m_Title;
        public override string title
        {
            get { return m_Title; }
            set
            {
                m_Title = value;
                if (m_TitleField != null)
                    m_TitleField.value = m_Title;
            }
        }

        public virtual int ZOrder { get; set; }

        Color m_Color = k_DefaultColor;
        public virtual Color Color
        {
            get { return m_Color; }
            set
            {
                m_Color = value;
                style.backgroundColor = value;
            }
        }

        public Vector2 UncollapsedSize { get; private set; }

        public Vector2 CollapsedSize
        {
            get
            {
                var actualCollapsedSize = k_DefaultCollapsedSize;
                if (UncollapsedSize.x < k_DefaultCollapsedSize.x)
                    actualCollapsedSize.x = UncollapsedSize.x;

                return actualCollapsedSize;
            }
        }

        public Rect ExpandedPosition => Collapsed ? new Rect(layout.position, UncollapsedSize) : GetPosition();

        public override void SetPosition(Rect newPos)
        {
            if (!Collapsed)
                UncollapsedSize = newPos.size;
            else
                newPos.size = CollapsedSize;

            base.SetPosition(newPos);
        }

        PlacematContainer m_PlacematContainer;

        PlacematContainer Container =>
            m_PlacematContainer ?? (m_PlacematContainer = GetFirstAncestorOfType<PlacematContainer>());

        HashSet<GraphElement> m_CollapsedElements  = new HashSet<GraphElement>();
        public IEnumerable<GraphElement> CollapsedElements => m_CollapsedElements;

        protected internal void SetCollapsedElements(IEnumerable<GraphElement> collapsedElements)
        {
            if (!Collapsed)
                return;

            foreach (var collapsedElement in m_CollapsedElements)
                collapsedElement.style.visibility = StyleKeyword.Null;

            m_CollapsedElements.Clear();

            if (collapsedElements == null)
                return;

            foreach (var collapsedElement in collapsedElements)
            {
                collapsedElement.style.visibility = Visibility.Hidden;
                m_CollapsedElements.Add(collapsedElement);
            }
        }

        internal void HideCollapsedEdges()
        {
            var nodes = new HashSet<Node>(AllCollapsedElements.OfType<Node>());
            foreach (var edge in m_GraphView.edges.ToList())
                if (AnyNodeIsConnectedToPort(nodes, edge.input) && AnyNodeIsConnectedToPort(nodes, edge.output))
                {
                    if (edge.style.visibility != Visibility.Hidden)
                    {
                        edge.style.visibility = Visibility.Hidden;
                        m_CollapsedElements.Add(edge);
                    }
                }
        }

        IEnumerable<GraphElement> AllCollapsedElements
        {
            get
            {
                foreach (var graphElement in CollapsedElements)
                {
                    var placemat = graphElement as Placemat;
                    if (placemat != null && placemat.Collapsed)
                        foreach (var subElement in placemat.AllCollapsedElements)
                            yield return subElement;

                    yield return graphElement;
                }
            }
        }

        bool m_Collapsed;

        public virtual bool Collapsed
        {
            get { return m_Collapsed; }
            set
            {
                if (m_Collapsed != value)
                {
                    m_Collapsed = value;
                    CollapseSelf();
                    ShowHideCollapsedElements();
                }
            }
        }

        void CollapseSelf()
        {
            if (Collapsed)
            {
                layout = new Rect(layout.position, CollapsedSize);

                if (m_Resizer != null)
                    m_Resizer.style.visibility = Visibility.Hidden;
            }
            else
            {
                layout = new Rect(layout.position, UncollapsedSize);

                if (m_Resizer != null)
                    m_Resizer.style.visibility = StyleKeyword.Null;
            }
            m_CollapseButton?.EnableInClassList("icon-expanded", !Collapsed);
            EnableInClassList("collapsed", Collapsed);
        }

        void RebuildCollapsedElements()
        {
            m_CollapsedElements.Clear();

            var graphElements = m_GraphView.graphElements.ToList()
                .Where(e => !(e is Edge) && (e.parent is GraphView.Layer) && (e.capabilities & Capabilities.Selectable) != 0)
                .ToList();

            var collapsedElementsElsewhere = new List<GraphElement>();
            RecurseRebuildCollapsedElements_LocalFunc(this, graphElements, collapsedElementsElsewhere);

            var nodes = new HashSet<Node>(AllCollapsedElements.OfType<Node>());

            foreach (var edge in m_GraphView.edges.ToList())
                if (AnyNodeIsConnectedToPort(nodes, edge.input) && AnyNodeIsConnectedToPort(nodes, edge.output))
                    m_CollapsedElements.Add(edge);

            foreach (var ge in collapsedElementsElsewhere)
                m_CollapsedElements.Remove(ge);
        }

        // TODO: Move to local function of Collapse once we move to C# 7.0 or higher.
        void RecurseRebuildCollapsedElements_LocalFunc(Placemat currentPlacemat, IList<GraphElement> graphElements,
            List<GraphElement> collapsedElementsElsewhere)
        {
            var currRect = currentPlacemat.ExpandedPosition;
            var currentActivePlacematRect = new Rect(
                currRect.x + k_SelectRectOffset,
                currRect.y + k_SelectRectOffset,
                currRect.width - 2 * k_SelectRectOffset,
                currRect.height - 2 * k_SelectRectOffset);
            foreach (var elem in graphElements)
            {
                if (elem.layout.Overlaps(currentActivePlacematRect))
                {
                    var placemat = elem as Placemat;
                    if (placemat != null && placemat.ZOrder > currentPlacemat.ZOrder)
                    {
                        if (placemat.Collapsed)
                            foreach (var cge in placemat.CollapsedElements)
                                collapsedElementsElsewhere.Add(cge);
                        else
                            RecurseRebuildCollapsedElements_LocalFunc(placemat, graphElements, collapsedElementsElsewhere);
                    }

                    if (placemat == null || placemat.ZOrder > currentPlacemat.ZOrder)
                        if (elem.resolvedStyle.visibility == Visibility.Visible)
                            m_CollapsedElements.Add(elem);
                }
            }
        }

        void ShowHideCollapsedElements()
        {
            if (m_GraphView == null)
                return;

            if (Collapsed)
            {
                RebuildCollapsedElements();

                foreach (var ge in m_CollapsedElements)
                    ge.style.visibility = Visibility.Hidden;

                UpdateCollapsedNodeEdges();
            }
            else
            {
                foreach (var ge in m_CollapsedElements)
                    ge.style.visibility = StyleKeyword.Null;

                UpdateCollapsedNodeEdges(); //Update edges just before clearing list
                m_CollapsedElements.Clear();
            }
        }

        static bool AnyNodeIsConnectedToPort(IEnumerable<Node> nodes, Port port)
        {
            foreach (var node in nodes)
            {
                var stackNode = node as StackNode;
                if (stackNode != null && stackNode.contentContainer.Children().Any(n => n == port.node))
                    return true;

                if (node == port.node)
                    return true;
            }

            return false;
        }

        void UpdateCollapsedNodeEdges()
        {
            if (m_GraphView == null)
                return;

            //We need to update all the edges whose either port is in the placemat
            var touchedEdges = new HashSet<Edge>();

            var nodes = new HashSet<Node>(AllCollapsedElements.OfType<Node>());
            foreach (var edge in m_GraphView.edges.ToList())
                if (AnyNodeIsConnectedToPort(nodes, edge.input) || AnyNodeIsConnectedToPort(nodes, edge.output))
                    touchedEdges.Add(edge);

            foreach (var edge in touchedEdges)
                edge.ForceUpdateEdgeControl();
        }

        void OnTitleFieldChange(ChangeEvent<string> evt)
        {
            // Call setter in derived class, if any.
            title = evt.newValue;
        }

        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);
            var mde = evt as PointerDownEvent;
            if (mde != null)
                if (mde.clickCount == 2 && mde.button == (int)MouseButton.LeftMouse)
                    SelectGraphElementsOver();
        }

        void ActOnGraphElementsOver(Action<GraphElement> act)
        {
            var graphElements = m_GraphView.graphElements.ToList()
                .Where(e => !(e is Edge) && (e.parent is GraphView.Layer) && (e.capabilities & Capabilities.Selectable) != 0);

            foreach (var elem in graphElements)
            {
                if (elem.layout.Overlaps(layout))
                    act(elem);
            }
        }

        internal bool ActOnGraphElementsOver(Func<GraphElement, bool> act, bool includePlacemats)
        {
            var graphElements = m_GraphView.graphElements.ToList()
                .Where(e => !(e is Edge) && e.parent is GraphView.Layer && (e.capabilities & Capabilities.Selectable) != 0).ToList();

            return RecurseActOnGraphElementsOver_LocalFunc(this, graphElements, act, includePlacemats);
        }

        // TODO: Move to local function of ActOnGraphElementsOver once we move to C# 7.0 or higher.
        static bool RecurseActOnGraphElementsOver_LocalFunc(Placemat currentPlacemat, List<GraphElement> graphElements,
            Func<GraphElement, bool> act, bool includePlacemats)
        {
            if (currentPlacemat.Collapsed)
            {
                foreach (var elem in currentPlacemat.CollapsedElements)
                {
                    var placemat = elem as Placemat;
                    if (placemat != null && placemat.ZOrder > currentPlacemat.ZOrder)
                        if (RecurseActOnGraphElementsOver_LocalFunc(placemat, graphElements, act, includePlacemats))
                            return true;

                    if (placemat == null || (includePlacemats && placemat.ZOrder > currentPlacemat.ZOrder))
                        if (act(elem))
                            return true;
                }
            }
            else
            {
                var currRect = currentPlacemat.ExpandedPosition;
                var currentActivePlacematRect = new Rect(
                    currRect.x + k_SelectRectOffset,
                    currRect.y + k_SelectRectOffset,
                    currRect.width - 2 * k_SelectRectOffset,
                    currRect.height - 2 * k_SelectRectOffset);

                foreach (var elem in graphElements)
                {
                    if (elem.layout.Overlaps(currentActivePlacematRect))
                    {
                        var placemat = elem as Placemat;
                        if (placemat != null && placemat.ZOrder > currentPlacemat.ZOrder)
                            if (RecurseActOnGraphElementsOver_LocalFunc(placemat, graphElements, act, includePlacemats))
                                return true;

                        if (placemat == null || (includePlacemats && placemat.ZOrder > currentPlacemat.ZOrder))
                            if (elem.resolvedStyle.visibility != Visibility.Hidden)
                                if (act(elem))
                                    return true;
                    }
                }
            }
            return false;
        }

        void SelectGraphElementsOver()
        {
            ActOnGraphElementsOver(e => m_GraphView.AddToSelection(e));
        }

        internal bool WillDragNode(Node node)
        {
            if (Collapsed)
                return AllCollapsedElements.Contains(node);

            return ActOnGraphElementsOver(t => node == t, true);
        }

        internal void GrowToFitElements(List<GraphElement> elements)
        {
            if (elements == null)
                elements = GetHoveringNodes();

            var pos = new Rect();
            if (elements.Count > 0 && ComputeElementBounds(ref pos, elements, MinSizePolicy.DoNotEnsureMinSize))
            {
                // We don't resize to be snug. In other words: we don't ever decrease in size.
                Rect currentRect = GetPosition();
                if (pos.xMin > currentRect.xMin)
                    pos.xMin = currentRect.xMin;

                if (pos.xMax < currentRect.xMax)
                    pos.xMax = currentRect.xMax;

                if (pos.yMin > currentRect.yMin)
                    pos.yMin = currentRect.yMin;

                if (pos.yMax < currentRect.yMax)
                    pos.yMax = currentRect.yMax;

                MakeRectAtLeastMinimalSize(ref pos);
                SetPosition(pos);
            }
        }

        internal void ShrinkToFitElements(List<GraphElement> elements)
        {
            if (elements == null)
                elements = GetHoveringNodes();

            var pos = new Rect();
            if (elements.Count > 0 && ComputeElementBounds(ref pos, elements))
                SetPosition(pos);
        }

        void ResizeToIncludeSelectedNodes()
        {
            List<GraphElement> nodes = m_GraphView.selection.OfType<GraphElement>().Where(e => e is Node).ToList();

            // Now include the selected nodes
            var pos = new Rect();
            if (ComputeElementBounds(ref pos, nodes, MinSizePolicy.DoNotEnsureMinSize))
            {
                // We don't resize to be snug: we only resize enough to contain the selected nodes.
                var currentRect = GetPosition();
                if (pos.xMin > currentRect.xMin)
                    pos.xMin = currentRect.xMin;

                if (pos.xMax < currentRect.xMax)
                    pos.xMax = currentRect.xMax;

                if (pos.yMin > currentRect.yMin)
                    pos.yMin = currentRect.yMin;

                if (pos.yMax < currentRect.yMax)
                    pos.yMax = currentRect.yMax;

                MakeRectAtLeastMinimalSize(ref pos);

                SetPosition(pos);
            }
        }

        internal void GetElementsToMove(bool moveOnlyPlacemat, HashSet<GraphElement> collectedElementsToMove)
        {
            if (Collapsed)
            {
                foreach (var ge in AllCollapsedElements)
                    if (!(ge is Edge))
                        collectedElementsToMove.Add(ge);
            }
            else if (!moveOnlyPlacemat)
            {
                ActOnGraphElementsOver(e =>
                {
                    collectedElementsToMove.Add(e);
                    return false;
                }, true);
            }
        }

        protected virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            var placemat = evt.target as Placemat;

            if (placemat != null)
            {
                evt.menu.AppendAction("Edit Title", a => placemat.StartEditTitle());

                evt.menu.AppendSeparator();

                evt.menu.AppendAction("Change Color...", a =>
                {
                    ColorPicker.Show(c => placemat.Color = c, placemat.Color, showAlpha: false);
                });

                // Resizing section
                evt.menu.AppendSeparator();

                evt.menu.AppendAction(placemat.Collapsed ? "Expand" : "Collapse", a => placemat.Collapsed = !placemat.Collapsed);

                // Gather nodes here so that we don't recycle this code in the resize functions.
                List<GraphElement> hoveringNodes = placemat.GetHoveringNodes();

                evt.menu.AppendAction("Resize/Grow To Fit",
                    a => placemat.GrowToFitElements(hoveringNodes),
                    hoveringNodes.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                evt.menu.AppendAction("Resize/Shrink To Fit",
                    a => placemat.ShrinkToFitElements(hoveringNodes),
                    hoveringNodes.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                evt.menu.AppendAction("Resize/Grow To Fit Selection",
                    a => placemat.ResizeToIncludeSelectedNodes(),
                    s =>
                    {
                        foreach (ISelectable sel in placemat.m_GraphView.selection)
                        {
                            var node = sel as Node;
                            if (node != null && !hoveringNodes.Contains(node))
                                return DropdownMenuAction.Status.Normal;
                        }

                        return DropdownMenuAction.Status.Disabled;
                    });

                var status = placemat.Container.Placemats.Any() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;

                evt.menu.AppendAction("Order/Bring To Front", a => Container.BringToFront(placemat), status);
                evt.menu.AppendAction("Order/Bring Forward", a => Container.CyclePlacemat(placemat, PlacematContainer.CycleDirection.Up), status);
                evt.menu.AppendAction("Order/Send Backward", a => Container.CyclePlacemat(placemat, PlacematContainer.CycleDirection.Down), status);
                evt.menu.AppendAction("Order/Send To Back", a => Container.SendToBack(placemat), status);
            }
        }

        List<GraphElement> GetHoveringNodes()
        {
            var potentialElements = new List<GraphElement>();
            ActOnGraphElementsOver(e => potentialElements.Add(e));

            return potentialElements.Where(e => e is Node).ToList();
        }

        public void StartEditTitle()
        {
            // Focus field and select text (spare the user from having to type enter before editing text).
            m_TitleField?.Q(TextField.textInputUssName)?.Focus();
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_TitleField.RegisterCallback<ChangeEvent<string>>(OnTitleFieldChange);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_TitleField.UnregisterCallback<ChangeEvent<string>>(OnTitleFieldChange);
        }

        internal bool GetPortCenterOverride(Port port, out Vector2 overriddenPosition)
        {
            if (!Collapsed || parent == null)
            {
                overriddenPosition = Vector2.zero;
                return false;
            }

            const int xOffset = 6;
            const int yOffset = 3;
            var halfSize = CollapsedSize * 0.5f;
            var offset = port.orientation == Orientation.Horizontal
                ? new Vector2(port.direction == Direction.Input ? -halfSize.x + xOffset : halfSize.x - xOffset, 0)
                : new Vector2(0, port.direction == Direction.Input ? -halfSize.y + yOffset : halfSize.y - yOffset);

            overriddenPosition = parent.LocalToWorld(layout.center + offset);
            return true;
        }

        // Helper method that calculates how big a Placemat should be to fit the nodes on top of it currently.
        internal const float k_Bounds = 9.0f;
        internal const float k_BoundTop = 29.0f; // Current height of Title

        public enum MinSizePolicy
        {
            EnsureMinSize,
            DoNotEnsureMinSize
        }

        // Returns false if bounds could not be computed.
        public static bool ComputeElementBounds(ref Rect pos, List<GraphElement> elements, MinSizePolicy ensureMinSize = MinSizePolicy.EnsureMinSize)
        {
            if (elements == null || elements.Count == 0)
                return false;

            float minX =  Mathf.Infinity;
            float maxX = -Mathf.Infinity;
            float minY =  Mathf.Infinity;
            float maxY = -Mathf.Infinity;

            foreach (var r in elements.Select(n => n.GetPosition()))
            {
                if (r.xMin < minX)
                    minX = r.xMin;

                if (r.xMax > maxX)
                    maxX = r.xMax;

                if (r.yMin < minY)
                    minY = r.yMin;

                if (r.yMax > maxY)
                    maxY = r.yMax;
            }

            var width = maxX - minX + k_Bounds * 2.0f;
            var height = maxY - minY + k_Bounds * 2.0f + k_BoundTop;

            pos = new Rect(
                minX - k_Bounds,
                minY - (k_BoundTop + k_Bounds),
                width,
                height);

            if (ensureMinSize == MinSizePolicy.EnsureMinSize)
                MakeRectAtLeastMinimalSize(ref pos);

            return true;
        }

        // The next two values need to be the same as USS... however, we can't get the values from there as we need them in a static
        // methods used to create new placemats
        const float k_MinWidth = 200;
        const float k_MinHeight = 100;

        static void MakeRectAtLeastMinimalSize(ref Rect r)
        {
            if (r.width < k_MinWidth)
                r.width = k_MinWidth;

            if (r.height < k_MinHeight)
                r.height = k_MinHeight;
        }
    }
}
