// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public class Node : GraphElement
    {
        public VisualElement mainContainer { get; private set; }
        public VisualElement titleContainer { get; private set; }
        public VisualElement inputContainer { get; private set; }
        public VisualElement outputContainer { get; private set; }

        //This directly contains input and output containers
        public VisualElement topContainer { get; private set; }
        public VisualElement extensionContainer { get; private set; }
        private VisualElement m_CollapsibleArea;

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
        public string title
        {
            get { return m_TitleLabel.text; }
            set { m_TitleLabel.text = value; }
        }

        protected readonly VisualElement m_CollapseButton;

        private const string k_ExpandedStyleClass = "expanded";
        private const string k_CollapsedStyleClass = "collapsed";
        private void UpdateExpandedButtonState()
        {
            RemoveFromClassList(m_Expanded ? k_CollapsedStyleClass : k_ExpandedStyleClass);
            AddToClassList(m_Expanded ? k_ExpandedStyleClass : k_CollapsedStyleClass);
        }

        public override Rect GetPosition()
        {
            return new Rect(style.positionLeft, style.positionTop, layout.width, layout.height);
        }

        public override void SetPosition(Rect newPos)
        {
            style.positionType = PositionType.Absolute;
            style.positionLeft = newPos.x;
            style.positionTop = newPos.y;
        }

        // TODO: Remove when removing presenters.
        protected virtual void SetLayoutClassLists(NodePresenter nodePresenter)
        {
        }

        protected virtual void OnPortRemoved(Port port)
        {}

        // TODO: Remove when removing presenters.
        private void ProcessRemovedPorts(IList<Port> currentPorts, VisualElement portContainer, IList<PortPresenter> currentPresenters)
        {
            foreach (var port in currentPorts)
            {
                bool contains = false;
                var inputPres = port.GetPresenter<PortPresenter>();
                foreach (var newPres in currentPresenters)
                {
                    if (newPres == inputPres)
                    {
                        contains = true;
                        break;
                    }
                }

                if (!contains)
                {
                    port.OnConnect -= OnPortConnectAction;
                    port.OnDisconnect -= OnPortConnectAction;
                    OnPortRemoved(port);
                    portContainer.Remove(port);
                }
            }
        }

        // TODO: Remove when removing presenters.
        private void ProcessAddedPorts(IList<Port> currentPorts, VisualElement portContainer, IList<PortPresenter> currentPresenters)
        {
            int index = 0;
            foreach (var newPres in currentPresenters)
            {
                bool contains = false;
                foreach (Port currPort in currentPorts)
                {
                    if (newPres == currPort.GetPresenter<PortPresenter>())
                    {
                        contains = true;
                        break;
                    }
                }

                if (!contains)
                {
                    Port newPort = InstantiatePort(newPres);
                    newPort.OnConnect += OnPortConnectAction;
                    newPort.OnDisconnect += OnPortConnectAction;
                    portContainer.Insert(index, newPort);
                }

                index++;
            }
        }

        public virtual Port InstantiatePort(Orientation orientation, Direction direction, Type type)
        {
            return Port.Create<Edge>(orientation, direction, type);
        }

        public virtual Port InstantiatePort(PortPresenter newPres)
        {
            return Port.Create<EdgePresenter, Edge>(newPres);
        }

        private void SetElementVisible(VisualElement element, bool isVisible)
        {
            const string k_HiddenClassList = "hidden";

            element.visible = isVisible;
            if (isVisible)
            {
                element.RemoveFromClassList(k_HiddenClassList);
            }
            else
            {
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
            var nodePresenter = GetPresenter<NodePresenter>();

            bool expandedState = expanded;

            // TODO: Remove when removing presenters.
            if (nodePresenter != null)
            {
                var currentInputs = inputContainer.Query<Port>().ToList();
                var currentOutputs = outputContainer.Query<Port>().ToList();

                ProcessRemovedPorts(currentInputs, inputContainer, nodePresenter.inputPorts);
                ProcessRemovedPorts(currentOutputs, outputContainer, nodePresenter.outputPorts);

                ProcessAddedPorts(currentInputs, inputContainer, nodePresenter.inputPorts);
                ProcessAddedPorts(currentOutputs, outputContainer, nodePresenter.outputPorts);

                expandedState = nodePresenter.expanded;
            }

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
                if (outputContainer.shadow.parent != topContainer)
                {
                    topContainer.Add(outputContainer);
                    outputContainer.BringToFront();
                }
            }
            else
            {
                if (outputContainer.shadow.parent == topContainer)
                {
                    outputContainer.RemoveFromHierarchy();
                }
            }

            if (inputVisible)
            {
                if (inputContainer.shadow.parent != topContainer)
                {
                    topContainer.Add(inputContainer);
                    inputContainer.SendToBack();
                }
            }
            else
            {
                if (inputContainer.shadow.parent == topContainer)
                {
                    inputContainer.RemoveFromHierarchy();
                }
            }

            SetElementVisible(divider, inputVisible && outputVisible);

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

            if (canCollapse)
                m_CollapseButton.pseudoStates &= ~PseudoStates.Disabled;
            else
                m_CollapseButton.pseudoStates |= PseudoStates.Disabled;
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();

            var nodePresenter = GetPresenter<NodePresenter>();

            // Just to keep them in sync, but we don't want the logic in
            // the property setter to apply here.
            m_Expanded = nodePresenter.expanded;

            RefreshExpandedState();

            m_TitleLabel.text = nodePresenter.title;

            SetLayoutClassLists(nodePresenter);
        }

        // TODO: Remove when removing presenters.
        private void ToggleCollapsePresenter()
        {
            var nodePresenter = GetPresenter<NodePresenter>();
            nodePresenter.expanded = !nodePresenter.expanded;
        }

        protected virtual void ToggleCollapse()
        {
            // TODO: Remove when removing presenters.
            if (GetPresenter<NodePresenter>() != null)
            {
                ToggleCollapsePresenter();
                return;
            }

            expanded = !expanded;
        }

        public Node()
        {
            clippingOptions = ClippingOptions.NoClipping;
            var tpl = EditorGUIUtility.Load("UXML/GraphView/Node.uxml") as VisualTreeAsset;

            tpl.CloneTree(this, new Dictionary<string, VisualElement>());

            VisualElement main = this;
            VisualElement borderContainer = main.Q(name: "node-border");

            if (borderContainer != null)
            {
                borderContainer.clippingOptions = ClippingOptions.ClipAndCacheContents;
                mainContainer = borderContainer;
            }
            else
            {
                mainContainer = main;
            }

            titleContainer = main.Q(name: "title");
            inputContainer = main.Q(name: "input");
            m_CollapsibleArea = main.Q(name: "collapsible-area");
            extensionContainer = main.Q("extension");
            VisualElement output = main.Q(name: "output");
            outputContainer = output;

            topContainer = output.parent;

            m_TitleLabel = main.Q<Label>(name: "title-label");
            m_CollapseButton = main.Q<VisualElement>(name: "collapse-button");
            m_CollapseButton.AddManipulator(new Clickable(ToggleCollapse));

            elementTypeColor = new Color(0.9f, 0.9f, 0.9f, 0.5f);

            if (main != this)
            {
                Add(main);
            }

            AddToClassList("node");

            capabilities |= Capabilities.Selectable | Capabilities.Movable | Capabilities.Deletable | Capabilities.Ascendable;

            m_Expanded = true;
            UpdateExpandedButtonState();
            UpdateCollapsibleAreaVisibility();

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
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

        void DisconnectAll(EventBase evt)
        {
            HashSet<GraphElement> toDelete = new HashSet<GraphElement>();

            AddConnectionsToDeleteSet(inputContainer, ref toDelete);
            AddConnectionsToDeleteSet(outputContainer, ref toDelete);
            toDelete.Remove(null);

            GraphView graphView = GetFirstAncestorOfType<GraphView>();
            if (graphView != null)
            {
                graphView.DeleteElements(toDelete);
            }
            else
            {
                Debug.Log("Disconnecting nodes that are not in a GraphView will not work.");
            }
        }

        ContextualMenu.MenuAction.StatusFlags DisconnectAllStatus(EventBase evt)
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
                        return ContextualMenu.MenuAction.StatusFlags.Normal;
                    }
                }
            }

            return ContextualMenu.MenuAction.StatusFlags.Disabled;
        }

        public virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is Node)
            {
                evt.menu.AppendAction("Disconnect all", DisconnectAll, DisconnectAllStatus);
                evt.menu.AppendSeparator();
            }
        }
    }
}
