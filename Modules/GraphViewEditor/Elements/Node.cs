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
        public virtual VisualElement mainContainer { get; private set; }
        public virtual VisualElement leftContainer { get; private set; }
        public virtual VisualElement rightContainer { get; private set; }
        public virtual VisualElement titleContainer { get; private set; }
        public virtual VisualElement inputContainer { get; private set; }
        public virtual VisualElement outputContainer { get; private set; }

        private Orientation m_Orientation;
        public Orientation orientation
        {
            get { return m_Orientation; }
            private set
            {
                // If the value hasn't change and we already have a class added, return.
                if (m_Orientation == value && (ClassListContains("vertical") || ClassListContains("horizontal")))
                    return;

                // Clear orientation classes.
                RemoveFromClassList("vertical");
                RemoveFromClassList("horizontal");

                m_Orientation = value;

                AddToClassList(m_Orientation == Orientation.Vertical ? "vertical" : "horizontal");
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
                m_CollapseButton.text = m_Expanded ? "collapse" : "expand";

                RefreshPorts();
            }
        }

        private readonly Label m_TitleLabel;
        public string title
        {
            get { return m_TitleLabel.text; }
            set { m_TitleLabel.text = value; }
        }

        protected readonly Button m_CollapseButton;

        public override Rect GetPosition()
        {
            if (ClassListContains("vertical"))
            {
                return base.GetPosition();
            }
            else
            {
                return new Rect(style.positionLeft, style.positionTop, layout.width, layout.height);
            }
        }

        public override void SetPosition(Rect newPos)
        {
            if (ClassListContains("vertical"))
            {
                base.SetPosition(newPos);
            }
            else
            {
                style.positionType = PositionType.Absolute;
                style.positionLeft = newPos.x;
                style.positionTop = newPos.y;
            }
        }

        // TODO: Remove when removing presenters.
        protected virtual void SetLayoutClassLists(NodePresenter nodePresenter)
        {
            orientation = nodePresenter.orientation;
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
                    portContainer.Insert(index, InstantiatePort(newPres));
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

        private int ShowPorts(bool show, IList<Port> currentPorts)
        {
            int count = 0;
            foreach (var port in currentPorts)
            {
                if ((show || port.connected) && !port.collapsed)
                {
                    port.visible = true;
                    port.RemoveFromClassList("hidden");
                    count++;
                }
                else
                {
                    port.visible = false;
                    port.AddToClassList("hidden");
                }
            }
            return count;
        }

        public void RefreshPorts()
        {
            var nodePresenter = GetPresenter<NodePresenter>();

            var expandedState = expanded;

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

            ShowPorts(expandedState, updatedInputs);
            int outputCount = ShowPorts(expandedState, updatedOutputs);

            // Show output container only if we have one or more child
            if (outputCount > 0)
            {
                if (!mainContainer.Contains(rightContainer))
                {
                    mainContainer.Add(rightContainer);
                }
            }
            else
            {
                if (mainContainer.Contains(rightContainer))
                {
                    mainContainer.Remove(rightContainer);
                }
            }
        }

        // TODO: Remove when removing presenters.
        public override void OnDataChanged()
        {
            base.OnDataChanged();

            var nodePresenter = GetPresenter<NodePresenter>();

            // Just to keep them in sync, but we don't want the logic in
            // the property setter to apply here.
            m_Expanded = nodePresenter.expanded;

            RefreshPorts();

            m_TitleLabel.text = nodePresenter.title;

            m_CollapseButton.text = nodePresenter.expanded ? "collapse" : "expand";

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

        // TODO: Remove when removing presenters.
        public Node() : this(Orientation.Horizontal) {}

        public Node(Orientation nodeOrientation = Orientation.Horizontal)
        {
            var tpl = EditorGUIUtility.Load("UXML/GraphView/Node.uxml") as VisualTreeAsset;
            mainContainer = tpl.CloneTree(null);
            mainContainer.clippingOptions = ClippingOptions.ClipAndCacheContents;
            leftContainer = mainContainer.Q(name: "left");
            rightContainer = mainContainer.Q(name: "right");
            titleContainer = mainContainer.Q(name: "title");
            inputContainer = mainContainer.Q(name: "input");
            outputContainer = mainContainer.Q(name: "output");

            m_TitleLabel = mainContainer.Q<Label>(name: "titleLabel");
            m_CollapseButton = mainContainer.Q<Button>(name: "collapseButton");
            m_CollapseButton.clickable.clicked += ToggleCollapse;

            elementTypeColor = new Color(0.9f, 0.9f, 0.9f, 0.5f);

            Add(mainContainer);
            mainContainer.AddToClassList("mainContainer");

            ClearClassList();
            AddToClassList("node");

            capabilities |= Capabilities.Selectable | Capabilities.Movable | Capabilities.Deletable;
            orientation = nodeOrientation;

            m_Expanded = true;
        }
    }
}
