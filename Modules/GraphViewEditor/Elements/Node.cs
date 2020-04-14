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
    public class Node : GraphElement, ICollectibleElement
    {
        public VisualElement mainContainer { get; private set; }
        public VisualElement titleContainer { get; private set; }
        public VisualElement inputContainer { get; private set; }
        public VisualElement outputContainer { get; private set; }
        public VisualElement titleButtonContainer { get; private set; }

        private VisualElement m_InputContainerParent;
        private VisualElement m_OutputContainerParent;

        //This directly contains input and output containers
        public VisualElement topContainer { get; private set; }
        public VisualElement extensionContainer { get; private set; }
        private VisualElement m_CollapsibleArea;

        private GraphView m_GraphView;
        // TODO Maybe make protected and move to GraphElement!
        private GraphView graphView
        {
            get
            {
                if (m_GraphView == null)
                {
                    m_GraphView = GetFirstAncestorOfType<GraphView>();
                }
                return m_GraphView;
            }
        }

        private bool m_Expanded;
        public virtual bool expanded
        {
            get { return m_Expanded; }
            set
            {
                if (m_Expanded == value)
                    return;

                m_Expanded = value;
                RefreshExpandedState();
            }
        }

        public void RefreshExpandedState()
        {
            UpdateExpandedButtonState();

            bool hasPorts = RefreshPorts();

            VisualElement contents = mainContainer.Q("contents");

            if (contents == null)
            {
                return;
            }
            VisualElement divider = contents.Q("divider");

            if (divider != null)
            {
                SetElementVisible(divider, hasPorts);
            }

            UpdateCollapsibleAreaVisibility();
        }

        void UpdateCollapsibleAreaVisibility()
        {
            if (m_CollapsibleArea == null)
            {
                return;
            }

            bool displayBottom = expanded && extensionContainer.childCount > 0;

            if (displayBottom)
            {
                if (m_CollapsibleArea.parent == null)
                {
                    VisualElement contents = mainContainer.Q("contents");

                    if (contents == null)
                    {
                        return;
                    }

                    contents.Add(m_CollapsibleArea);
                }

                m_CollapsibleArea.BringToFront();
            }
            else
            {
                if (m_CollapsibleArea.parent != null)
                {
                    m_CollapsibleArea.RemoveFromHierarchy();
                }
            }
        }

        private readonly Label m_TitleLabel;
        public override string title
        {
            get { return m_TitleLabel != null ? m_TitleLabel.text : string.Empty; }
            set { if (m_TitleLabel != null) m_TitleLabel.text = value; }
        }

        protected readonly VisualElement m_CollapseButton;
        protected readonly VisualElement m_ButtonContainer;

        private const string k_ExpandedStyleClass = "expanded";
        private const string k_CollapsedStyleClass = "collapsed";
        private void UpdateExpandedButtonState()
        {
            RemoveFromClassList(m_Expanded ? k_CollapsedStyleClass : k_ExpandedStyleClass);
            AddToClassList(m_Expanded ? k_ExpandedStyleClass : k_CollapsedStyleClass);
        }

        public override Rect GetPosition()
        {
            if (resolvedStyle.position == Position.Absolute)
                return new Rect(resolvedStyle.left, resolvedStyle.top, layout.width, layout.height);
            return layout;
        }

        public override void SetPosition(Rect newPos)
        {
            style.position = Position.Absolute;
            style.left = newPos.x;
            style.top = newPos.y;
        }

        protected virtual void OnPortRemoved(Port port)
        {}

        public virtual Port InstantiatePort(Orientation orientation, Direction direction, Port.Capacity capacity, Type type)
        {
            return Port.Create<Edge>(orientation, direction, capacity, type);
        }

        private void SetElementVisible(VisualElement element, bool isVisible)
        {
            const string k_HiddenClassList = "hidden";

            if (isVisible)
            {
                // Restore default value for visibility by setting it to StyleKeyword.Null.
                // Setting it to Visibility.Visible would make it visible even if parent is hidden.
                element.style.visibility = StyleKeyword.Null;
                element.RemoveFromClassList(k_HiddenClassList);
            }
            else
            {
                element.style.visibility = Visibility.Hidden;
                element.AddToClassList(k_HiddenClassList);
            }
        }

        private bool AllElementsHidden(VisualElement element)
        {
            for (int i = 0; i < element.childCount; ++i)
            {
                if (element[i].visible)
                    return false;
            }

            return true;
        }

        private int ShowPorts(bool show, IList<Port> currentPorts)
        {
            int count = 0;
            foreach (var port in currentPorts)
            {
                if ((show || port.connected) && !port.collapsed)
                {
                    SetElementVisible(port, true);
                    count++;
                }
                else
                {
                    SetElementVisible(port, false);
                }
            }
            return count;
        }

        public bool RefreshPorts()
        {
            bool expandedState = expanded;

            // Refresh the lists after all additions and everything took place
            var updatedInputs = inputContainer.Query<Port>().ToList();
            var updatedOutputs = outputContainer.Query<Port>().ToList();

            foreach (Port input in updatedInputs)
            {
                // Make sure we don't register these more than once.
                input.OnConnect -= OnPortConnectAction;
                input.OnDisconnect -= OnPortConnectAction;

                input.OnConnect += OnPortConnectAction;
                input.OnDisconnect += OnPortConnectAction;
            }

            foreach (Port output in updatedOutputs)
            {
                // Make sure we don't register these more than once.
                output.OnConnect -= OnPortConnectAction;
                output.OnDisconnect -= OnPortConnectAction;

                output.OnConnect += OnPortConnectAction;
                output.OnDisconnect += OnPortConnectAction;
            }

            int inputCount = ShowPorts(expandedState, updatedInputs);
            int outputCount = ShowPorts(expandedState, updatedOutputs);

            VisualElement divider = topContainer.Q("divider");

            bool outputVisible = outputCount > 0 || !AllElementsHidden(outputContainer);
            bool inputVisible = inputCount > 0 || !AllElementsHidden(inputContainer);

            // Show output container only if we have one or more child
            if (outputVisible)
            {
                if (outputContainer.hierarchy.parent != m_OutputContainerParent)
                {
                    m_OutputContainerParent.hierarchy.Add(outputContainer);
                    outputContainer.BringToFront();
                }
            }
            else
            {
                if (outputContainer.hierarchy.parent == m_OutputContainerParent)
                {
                    outputContainer.RemoveFromHierarchy();
                }
            }

            if (inputVisible)
            {
                if (inputContainer.hierarchy.parent != m_InputContainerParent)
                {
                    m_InputContainerParent.hierarchy.Add(inputContainer);
                    inputContainer.SendToBack();
                }
            }
            else
            {
                if (inputContainer.hierarchy.parent == m_InputContainerParent)
                {
                    inputContainer.RemoveFromHierarchy();
                }
            }

            if (divider != null)
            {
                SetElementVisible(divider, inputVisible && outputVisible);
            }

            return inputVisible || outputVisible;
        }

        private void OnPortConnectAction(Port port)
        {
            bool canCollapse = false;
            var updatedInputs = inputContainer.Query<Port>().ToList();
            var updatedOutputs = outputContainer.Query<Port>().ToList();
            foreach (Port input in updatedInputs)
            {
                if (!input.connected)
                {
                    canCollapse = true;
                    break;
                }
            }

            if (!canCollapse)
            {
                foreach (Port output in updatedOutputs)
                {
                    if (!output.connected)
                    {
                        canCollapse = true;
                        break;
                    }
                }
            }

            if (m_CollapseButton != null)
            {
                if (canCollapse)
                    m_CollapseButton.pseudoStates &= ~PseudoStates.Disabled;
                else
                    m_CollapseButton.pseudoStates |= PseudoStates.Disabled;
            }
        }

        protected virtual void ToggleCollapse()
        {
            expanded = !expanded;
        }

        protected void UseDefaultStyling()
        {
            AddStyleSheetPath("StyleSheets/GraphView/Node.uss");
        }

        public Node() : this("UXML/GraphView/Node.uxml")
        {
            UseDefaultStyling();
        }

        public Node(string uiFile)
        {
            var tpl = EditorGUIUtility.Load(uiFile) as VisualTreeAsset;

            tpl.CloneTree(this);

            VisualElement main = this;
            VisualElement borderContainer = main.Q(name: "node-border");

            if (borderContainer != null)
            {
                borderContainer.style.overflow = Overflow.Hidden;
                mainContainer = borderContainer;
                var selection = main.Q(name: "selection-border");
                if (selection != null)
                {
                    selection.style.overflow = Overflow.Visible; //fixes issues with selection border being clipped when zooming out
                }
            }
            else
            {
                mainContainer = main;
            }

            titleContainer = main.Q(name: "title");
            inputContainer = main.Q(name: "input");

            if (inputContainer != null)
            {
                m_InputContainerParent = inputContainer.hierarchy.parent;
            }

            m_CollapsibleArea = main.Q(name: "collapsible-area");
            extensionContainer = main.Q("extension");
            VisualElement output = main.Q(name: "output");
            outputContainer = output;

            if (outputContainer != null)
            {
                m_OutputContainerParent = outputContainer.hierarchy.parent;
                topContainer = output.parent;
            }

            m_TitleLabel = main.Q<Label>(name: "title-label");
            titleButtonContainer = main.Q(name: "title-button-container");
            m_CollapseButton = main.Q(name: "collapse-button");
            m_CollapseButton.AddManipulator(new Clickable(ToggleCollapse));

            elementTypeColor = new Color(0.9f, 0.9f, 0.9f, 0.5f);

            if (main != this)
            {
                Add(main);
            }

            AddToClassList("node");

            capabilities |= Capabilities.Selectable | Capabilities.Movable | Capabilities.Deletable | Capabilities.Ascendable | Capabilities.Copiable | Capabilities.Snappable;
            usageHints = UsageHints.DynamicTransform;

            m_Expanded = true;
            UpdateExpandedButtonState();
            UpdateCollapsibleAreaVisibility();

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));

            style.position = Position.Absolute;
        }

        void AddConnectionsToDeleteSet(VisualElement container, ref HashSet<GraphElement> toDelete)
        {
            List<GraphElement> toDeleteList = new List<GraphElement>();
            container.Query<Port>().ForEach(elem =>
            {
                if (elem.connected)
                {
                    foreach (Edge c in elem.connections)
                    {
                        if ((c.capabilities & Capabilities.Deletable) == 0)
                            continue;

                        toDeleteList.Add(c);
                    }
                }
            });

            toDelete.UnionWith(toDeleteList);
        }

        void DisconnectAll(DropdownMenuAction a)
        {
            HashSet<GraphElement> toDelete = new HashSet<GraphElement>();

            AddConnectionsToDeleteSet(inputContainer, ref toDelete);
            AddConnectionsToDeleteSet(outputContainer, ref toDelete);
            toDelete.Remove(null);

            if (graphView != null)
            {
                graphView.DeleteElements(toDelete);
            }
            else
            {
                Debug.Log("Disconnecting nodes that are not in a GraphView will not work.");
            }
        }

        DropdownMenuAction.Status DisconnectAllStatus(DropdownMenuAction a)
        {
            VisualElement[] containers =
            {
                inputContainer, outputContainer
            };

            foreach (var container in containers)
            {
                var currentInputs = container.Query<Port>().ToList();
                foreach (var elem in currentInputs)
                {
                    if (elem.connected)
                    {
                        return DropdownMenuAction.Status.Normal;
                    }
                }
            }

            return DropdownMenuAction.Status.Disabled;
        }

        public virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is Node)
            {
                evt.menu.AppendAction("Disconnect all", DisconnectAll, DisconnectAllStatus);
                evt.menu.AppendSeparator();
            }
        }

        void CollectConnectedEdges(HashSet<GraphElement> edgeSet)
        {
            edgeSet.UnionWith(inputContainer.Children().OfType<Port>().SelectMany(c => c.connections)
                .Where(d => (d.capabilities & Capabilities.Deletable) != 0)
                .Cast<GraphElement>());
            edgeSet.UnionWith(outputContainer.Children().OfType<Port>().SelectMany(c => c.connections)
                .Where(d => (d.capabilities & Capabilities.Deletable) != 0)
                .Cast<GraphElement>());
        }

        public virtual void CollectElements(HashSet<GraphElement> collectedElementSet, Func<GraphElement, bool> conditionFunc)
        {
            CollectConnectedEdges(collectedElementSet);
        }
    }
}
