// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.InternalBridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// UI for a <see cref="InputOutputPortsNodeModel"/>.
    /// </summary>
    [UnityRestricted]
    internal class CollapsibleInOutNodeView : NodeView
    {
        /// <summary>
        /// The name of the title container with an icon.
        /// </summary>
        public static readonly string titleIconContainerPartName = "title-icon-container";

        /// <summary>
        /// The USS class name added to collapsed nodes.
        /// </summary>
        public static readonly string collapsedUssClassName = ussClassName.WithUssModifier(GraphElementHelper.collapsedUssModifier);

        /// <summary>
        /// The name of the top container for vertical ports.
        /// </summary>
        public static readonly string topPortContainerPartName = "top-vertical-port-container";

        /// <summary>
        /// The name of the bottom container for vertical ports.
        /// </summary>
        public static readonly string bottomPortContainerPartName = "bottom-vertical-port-container";

        bool m_ShouldRename;

        const float k_ByteToPercentFactor = 100 / 255.0f;
        public byte Progress
        {
            set
            {
                var titleComponent = EditableTitlePart as NodeTitlePart;
                if (titleComponent?.CoroutineProgressBar != null)
                {
                    titleComponent.CoroutineProgressBar.value = value * k_ByteToPercentFactor;
                }
            }
        }

        /// <summary>
        /// The maximum allowed input label width.
        /// </summary>
        public virtual float MaxInputLabelWidth => float.PositiveInfinity;

        protected internal virtual int NodeTitleOptions => EditableTitlePart.Options.SetWidth | NodeTitlePart.Options.ShouldDisplayColor | NodeTitlePart.Options.HasIcon;

        /// <inheritdoc />
        public override void ActivateRename()
        {
            // To activate the rename, the editable title needs to be focusable and visible to be able to grab the focus.
            // However, the node and all its children were hidden in PostBuildUI for Input Label computation.
            // Using this check, the rename will be activated when the editable title is visible again.
            if (Model is IRenamable && NodeModel.IsRenamable())
                m_ShouldRename = true;
        }

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            PartList.AppendPart(VerticalPortContainerPart.Create(topPortContainerPartName, Model, this, ussClassName, BasePortContainerPart.inputPortFilter));

            PartList.AppendPart(NodeTitlePart.Create(titleIconContainerPartName, NodeModel, this, ussClassName, EditableTitlePart.Options.UseEllipsis | EditableTitlePart.Options.SetWidth | NodeTitleOptions));
            PartList.AppendPart(NodeOptionsInspector.Create(nodeOptionsContainerPartName, new[] {Model}, this, ussClassName, ModelInspectorView.NodeOptionsFilterForNode));
            PartList.AppendPart(InOutPortContainerPart.Create(portContainerPartName, Model, this, MaxInputLabelWidth, ussClassName));

            PartList.AppendPart(VerticalPortContainerPart.Create(bottomPortContainerPartName, Model, this, ussClassName, BasePortContainerPart.outputPortFilter));

            PartList.AppendPart(NodeLodCachePart.Create(cachePartName, Model, this, ussClassName));
        }

        // This is used to disable the one frame delay before the node appears in the tests and should no be used anywhere else.
        // Tests become unstable if the node is not displayed immediately.
        internal static bool s_DisableHiddenNodeAtCreation = false;

        /// <inheritdoc />
        protected override EditableTitlePart EditableTitlePart => PartList.GetPart(titleIconContainerPartName) as EditableTitlePart;

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            RegisterCallback<MouseOverEvent>(OnMouseOver);

            if (!s_DisableHiddenNodeAtCreation)
            {
                //Ensure that the first computation of Input labels is done while the node is hidden to avoid a visible jump.
                //We first need a frame to get all the required info ( font, size, style, ... ) of the label in the resolved style to compute each label width
                //then we find the largest input label and set the min-size of all the labels to this.
                visible = false;

                RegisterCallbackOnce<GeometryChangedEvent>(_ =>
                {
                    visible = true;
                    OnSetVisible();
                });
            }
            else
            {
                OnSetVisible();
            }
        }

        /// <inheritdoc />
        protected override void BuildNodeToolbarButtons()
        {
            base.BuildNodeToolbarButtons();

            if (NodeModel.IsCollapsible())
                AddNodeToolbarButton(CreateCollapseButton());
        }

        /// <summary>
        /// Creates a <see cref="CollapseButton"/> to collapse the node.
        /// </summary>
        /// <returns>The button to collapse the node.</returns>
        protected CollapseButton CreateCollapseButton()
        {
            var collapsed = (NodeModel as ICollapsible)?.Collapsed ?? false;
            var collapseButton = new CollapseButton(CollapseButton.collapseButtonName, OnChangeValue, OnShowButton)
            {
                value = collapsed,
                tooltip = "Collapse Node"
            };
            collapseButton.AddToClassList(ussClassName.WithUssElement(CollapseButton.collapseButtonName));

            return collapseButton;
        }

        protected void OnChangeValue(ChangeEvent<bool> evt)
        {
            GraphView.Dispatch(new CollapseNodeCommand(evt.newValue, NodeModel));
            EnableInClassList(collapsedUssClassName, evt.newValue);
        }

        protected void OnShowButton(bool show)
        {
            EnableCollapseButton();
        }

        protected void OnMouseOver(MouseOverEvent evt)
        {
            // It can happen that all ports get connected while the mouse is already over the node (OnMouseEnter won't be called). We need to update the collapse button if that is the case.
            EnableCollapseButton();
        }

        void EnableCollapseButton()
        {
            var collapseButton = GetNodeToolbarButton(CollapseButton.collapseButtonName);
            if (collapseButton is null || NodeModel is not PortNodeModel portHolder || portHolder.GetPorts() == null)
                return;

            var allPortConnected = true;
            foreach (var port in portHolder.GetPorts())
            {
                if (!port.IsConnected())
                {
                    allPortConnected = false;
                    break;
                }
            }

            // When all ports are connected, the collapse button should be disabled.
            collapseButton.SetEnabled(!allPortConnected);
        }

        protected void OnSetVisible()
        {
            // We just changed the node to be visible. We need one more frame of layout to get the editableTitlePart to be visible.
            schedule.Execute(() =>
            {
                var editableTitlePart = EditableTitlePart;
                if (m_ShouldRename && editableTitlePart != null && editableTitlePart.Root.visible)
                {
                    // The editable title is now visible. If the node needs to be renamed, the rename is activated here.
                    editableTitlePart.BeginEditing();
                    m_ShouldRename = false;
                }
            });
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);

            if (visitor.ChangeHints.HasChange(ChangeHint.Layout))
            {
                var collapsed = (NodeModel as ICollapsible)?.Collapsed ?? false;
                EnableInClassList(collapsedUssClassName, collapsed);
                var collapseButton = GetNodeToolbarButton(CollapseButton.collapseButtonName);
                collapseButton?.SetValueWithoutNotify(collapsed);
            }
        }

        /// <inheritdoc />
        protected override bool IsReadyForCulling()
        {
            if(EditableTitlePart != null && !EditableTitlePart.IsLayoutValid())
                return false;
            return visible && base.IsReadyForCulling();
        }
    }
}
