// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable InconsistentNaming

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// UI for a <see cref="AbstractNodeModel"/>.
    /// </summary>
    [UnityRestricted]
    internal class NodeView : GraphElement
    {
        /// <summary>
        /// The USS class name added to a <see cref="NodeView"/>.
        /// </summary>
        public new static readonly string ussClassName = "ge-node";

        /// <summary>
        /// The USS class name added to nodes that are not connected to other nodes.
        /// </summary>
        public static readonly string notConnectedUssClassName = ussClassName.WithUssModifier(GraphElementHelper.notConnectedUssModifier);

        /// <summary>
        /// The USS class name added to empty nodes.
        /// </summary>
        public static readonly string emptyUssClassName = ussClassName.WithUssModifier(GraphElementHelper.emptyUssModifier);

        /// <summary>
        /// The USS class name added to disabled nodes.
        /// </summary>
        public static readonly string disabledNodeUssClassName = ussClassName.WithUssModifier(GraphElementHelper.disabledUssModifier);

        /// <summary>
        /// The USS class name added to unused nodes.
        /// </summary>
        public static readonly string unusedUssClassName = ussClassName.WithUssModifier(GraphElementHelper.unusedUssModifier);

        /// <summary>
        /// The USS class name added to read-only nodes.
        /// </summary>
        public static readonly string readOnlyUssClassName = ussClassName.WithUssModifier(GraphElementHelper.readOnlyUssModifier);

        /// <summary>
        /// The USS class name added to write-only nodes.
        /// </summary>
        public static readonly string writeOnlyUssClassName = ussClassName.WithUssModifier(GraphElementHelper.writeOnlyUssModifier);

        /// <summary>
        /// The USS class name added if any input port is connected.
        /// </summary>
        public static readonly string hasConnectedInputUssClassName = ussClassName.WithUssModifier("has-connected-input");

        /// <summary>
        /// The USS class name added if any output port is connected.
        /// </summary>
        public static readonly string hasConnectedOutputUssClassName = ussClassName.WithUssModifier("has-connected-output");

        /// <summary>
        /// The name of the <see cref="ModelViewPart"/> for the title container.
        /// </summary>
        public static readonly string titleContainerPartName = GraphElementHelper.titleContainerName;

        /// <summary>
        /// The name of the <see cref="ModelViewPart"/> for the node options container.
        /// </summary>
        public static readonly string nodeOptionsContainerPartName = "node-options";

        /// <summary>
        /// The name of the <see cref="ModelViewPart"/> for the port container.
        /// </summary>
        public static readonly string portContainerPartName = "port-container";

        /// <summary>
        /// The name of the <see cref="ModelViewPart"/> for the LOD cache.
        /// </summary>
        public static readonly string cachePartName = "cache";

        /// <summary>
        /// The <see cref="AbstractNodeModel"/> associated with the <see cref="NodeView"/>.
        /// </summary>
        public AbstractNodeModel NodeModel => Model as AbstractNodeModel;

        bool m_ShowToolbarButtons;

        List<NodeToolbarButton> m_NodeToolbarButtons = new List<NodeToolbarButton>();
        public IReadOnlyList<NodeToolbarButton> NodeToolbarButtons => m_NodeToolbarButtons;

        /// <summary>
        /// The editable title part used in the node, that will be in editing mode when the node is created.
        /// </summary>
        protected virtual EditableTitlePart EditableTitlePart => PartList.GetPart(titleContainerPartName) as EditableTitlePart;

        public NodeView() : base(true)
        { }

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            PartList.AppendPart(NodeTitlePart.Create(titleContainerPartName, NodeModel, this, ussClassName));
            PartList.AppendPart(NodeOptionsInspector.Create(nodeOptionsContainerPartName, new[] { Model }, this, ussClassName, ModelInspectorView.NodeOptionsFilterForNode));
            PartList.AppendPart(HorizontalPortContainerPart.Create(portContainerPartName, Model, this, ussClassName));
        }

        /// <inheritdoc />
        protected override void BuildUI()
        {
            base.BuildUI();

            var disabledOverlay = new VisualElement { name = disabledOverlayElementName, pickingMode = PickingMode.Ignore };
            hierarchy.Add(disabledOverlay);

            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            usageHints = UsageHints.DynamicTransform;
            AddToClassList(ussClassName);
            this.AddPackageStylesheet("Node.uss");

            // Add the NodeToolbarButtons
            BuildNodeToolbarButtons();

            // Add buttons to the title part
            if (EditableTitlePart is NodeTitlePart nodeTitlePart)
            {
                foreach (var b in NodeToolbarButtons)
                    nodeTitlePart.AddNodeToolbarButton(b);
            }
        }

        /// <summary>
        /// Builds the list of <see cref="NodeToolbarButton"/>'s. Overrides this function to add more buttons to the node.
        /// </summary>
        /// <remarks>Created buttons need to be added using <see cref="AddNodeToolbarButton"/>.</remarks>
        protected virtual void BuildNodeToolbarButtons()
        {
            if (NodeModel.HasNodePreview)
                AddNodeToolbarButton(CreateShowNodePreviewButton());
        }

        /// <summary>
        /// Adds a button to the list of <see cref="NodeToolbarButtons"/>.
        /// </summary>
        /// <param name="button">The button to add.</param>
        protected void AddNodeToolbarButton(NodeToolbarButton button)
        {
            m_NodeToolbarButtons.Add(button);
        }

        public override bool HasModelDependenciesChanged() => NodeModel is InputOutputPortsNodeModel nodeModel && nodeModel.NodeOptions.Count > 0;

        public override void AddModelDependencies()
        {
            base.AddModelDependencies();

            if (NodeModel is InputOutputPortsNodeModel nodeModel)
            {
                foreach (var nodeOption in nodeModel.NodeOptions)
                {
                    Dependencies.AddModelDependency(nodeOption.PortModel);
                }
            }
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);

            if (visitor.ChangeHints.HasChange(ChangeHint.Layout))
            {
                SetPosition(NodeModel.Position);
            }

            EnableInClassList(emptyUssClassName, childCount == 0);

            if (visitor.ChangeHints.HasChange(ChangeHint.Data))
            {
                EnableInClassList(disabledNodeUssClassName, NodeModel.State == ModelState.Disabled);
                if (NodeModel.HasNodePreview)
                {
                    var showPreviewButton = GetNodeToolbarButton(ShowPreviewButton.previewButtonName);
                    showPreviewButton?.SetValueWithoutNotify(NodeModel.NodePreviewModel?.ShowNodePreview ?? false);
                }
            }

            if (NodeModel is PortNodeModel portHolder && portHolder.GetPorts() != null)
            {
                var hasAnyConnected = false;
                var hasInputConnected = false;
                var hasOutputConnected = false;

                foreach (var port in portHolder.GetPorts())
                {
                    if (port.IsConnected())
                    {
                        hasAnyConnected = true;

                        if (port.Direction == PortDirection.Input)
                            hasInputConnected = true;
                        else if (port.Direction == PortDirection.Output)
                            hasOutputConnected = true;

                        if (hasInputConnected && hasOutputConnected)
                            break;
                    }
                }

                EnableInClassList(notConnectedUssClassName, !hasAnyConnected);
                EnableInClassList(hasConnectedInputUssClassName, hasInputConnected);
                EnableInClassList(hasConnectedOutputUssClassName, hasOutputConnected);
            }

            if (Model is VariableNodeModel variableModel)
            {
                // can't use changeHints here since they hint at changes on the node model itself, not on the VariableDeclarationModel
                EnableInClassList(readOnlyUssClassName, variableModel.VariableDeclarationModel?.Modifiers == ModifierFlags.Read);
                EnableInClassList(writeOnlyUssClassName, variableModel.VariableDeclarationModel?.Modifiers == ModifierFlags.Write);
            }
        }

        public override void ActivateRename()
        {
            EditableTitlePart?.BeginEditing();
        }

        void OnMouseEnter(MouseEnterEvent evt)
        {
            ShowNodeToolbarButtons(true);
        }

        void OnMouseLeave(MouseLeaveEvent evt)
        {
            ShowNodeToolbarButtons(false);
        }

        /// <summary>
        /// Creates a <see cref="ShowPreviewButton"/> to show or hide the Node Preview of the node.
        /// </summary>
        /// <returns>The button to show or hide the Node Preview of the node.</returns>
        protected ShowPreviewButton CreateShowNodePreviewButton()
        {
            var showPreview = NodeModel?.NodePreviewModel?.ShowNodePreview ?? false;
            var showNodePreviewButton = new ShowPreviewButton(ShowPreviewButton.previewButtonName, "Show Node preview",
                "Hide Node preview", OnChangeShowNodePreview, OnShowButton)
            { value = showPreview };
            showNodePreviewButton.AddToClassList(ussClassName.WithUssElement(ShowPreviewButton.previewButtonName));

            return showNodePreviewButton;

            void OnChangeShowNodePreview(ChangeEvent<bool> evt)
            {
                GraphView.Dispatch(new ShowNodePreviewCommand(evt.newValue, NodeModel));
            }

            void OnShowButton(bool show)
            {
                var nodePreview = NodeModel.NodePreviewModel.GetView<NodePreview>(RootView);
                nodePreview?.ShowClosePreviewButtonElement(show);
            }
        }

        /// <summary>
        /// Shows the <see cref="NodeToolbarButton"/>'s on the node. These buttons only appear when hovering on the node.
        /// </summary>
        /// <param name="show">Whether the <see cref="NodeToolbarButtons"/>s are shown on the node.</param>
        public void ShowNodeToolbarButtons(bool show)
        {
            m_ShowToolbarButtons = show;
            foreach (var button in NodeToolbarButtons)
            {
                button.EnableInClassList(visibleUssClassName, show);
                button.EnableInClassList(hiddenUssClassName, !show);
                button.OnShowButton(show);
            }

            if (m_ShowToolbarButtons)
                UpdateButtonsLOD();
        }

        /// <summary>
        /// Gets a <see cref="NodeToolbarButton"/> using its name.
        /// </summary>
        /// <param name="buttonName">The name of the button.</param>
        /// <returns>The first button that matches the given name.</returns>
        public NodeToolbarButton GetNodeToolbarButton(string buttonName)
        {
            for (var i = 0; i < NodeToolbarButtons.Count; i++)
            {
                var button = NodeToolbarButtons[i];
                if (button.name == buttonName)
                    return button;
            }

            return null;
        }

        /// <inheritdoc />
        public override void SetElementLevelOfDetail(float zoom, GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode)
        {
            base.SetElementLevelOfDetail(zoom, newZoomMode, oldZoomMode);

            if (m_ShowToolbarButtons)
            {
                UpdateButtonsLOD();
            }
        }

        void UpdateButtonsLOD()
        {
            foreach (var toolbarButton in NodeToolbarButtons)
            {
                SetNodeToolbarButtonLevelOfDetail(toolbarButton);
            }

            void SetNodeToolbarButtonLevelOfDetail(VisualElement button)
            {
                var buttonIcon = button?.SafeQ(GraphElementHelper.iconName);
                if (buttonIcon == null)
                    return;

                const int minSize = 9;
                const int normalSize = 16;
                const int marginLeft = 4;
                var scale = 1.0f / GraphView.Zoom;

                if (GraphView.Zoom > GraphView.MediumZoom)
                {
                    button.style.marginLeft = marginLeft;
                    buttonIcon.style.width = normalSize;
                    buttonIcon.style.height = normalSize;
                }
                else
                {
                    button.style.marginLeft = marginLeft * scale;
                    buttonIcon.style.width = minSize * scale;
                    buttonIcon.style.height = minSize * scale;
                }
            }
        }

        /// <inheritdoc />
        public override bool SupportsCulling(GraphViewCullingSource cullingSource)
        {
            // Nodes support any kind of culling sources.
            return true;
        }
    }
}
