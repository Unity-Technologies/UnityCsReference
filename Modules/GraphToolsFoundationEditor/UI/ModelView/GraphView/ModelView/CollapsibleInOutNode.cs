// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// UI for a <see cref="InputOutputPortsNodeModel"/>.
    /// </summary>
    class CollapsibleInOutNode : Node
    {
        public static readonly string collapsedUssClassName = ussClassName.WithUssModifier("collapsed");
        public static readonly string collapseButtonPartName = "collapse-button";
        public static readonly string titleIconContainerPartName = "title-icon-container";

        /// <summary>
        /// The name of the top container for vertical ports.
        /// </summary>
        public static readonly string topPortContainerPartName = "top-vertical-port-container";

        /// <summary>
        /// The name of the bottom container for vertical ports.
        /// </summary>
        public static readonly string bottomPortContainerPartName = "bottom-vertical-port-container";

        const float k_ByteToPercentFactor = 100 / 255.0f;
        public byte Progress
        {
            set
            {
                var titleComponent = PartList.GetPart(titleIconContainerPartName) as IconTitleProgressPart;
                if (titleComponent?.CoroutineProgressBar != null)
                {
                    titleComponent.CoroutineProgressBar.value = value * k_ByteToPercentFactor;
                }
            }
        }

        public virtual float MaxInputLabelWidth => float.PositiveInfinity;

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            PartList.AppendPart(VerticalPortContainerPart.Create(topPortContainerPartName, PortDirection.Input, Model, this, ussClassName));

            PartList.AppendPart(IconTitleProgressPart.Create(titleIconContainerPartName, Model, this, ussClassName, true));
            PartList.AppendPart(NodeModeDropDownPart.Create(nodeModeDropDownPartName, Model, this, ussClassName));
            PartList.AppendPart(SerializedFieldsInspector.Create(nodeOptionsContainerPartName, new[] {Model}, RootView, ussClassName, ModelInspectorView.NodeOptionsFilterForNode));
            PartList.AppendPart(InOutPortContainerPart.Create(portContainerPartName, Model, this, MaxInputLabelWidth, ussClassName));

            PartList.AppendPart(VerticalPortContainerPart.Create(bottomPortContainerPartName, PortDirection.Output, Model, this, ussClassName));

            PartList.AppendPart(NodeLodCachePart.Create(nodeCachePartName, Model, this, ussClassName));
        }

        // This is used to disable the one frame delay before the node appears in the tests and should no be used anywhere else.
        // Tests become unstable if the node is not displayed immediately.
        internal static bool s_DisableHiddenNodeAtCreation_Internal = false;

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            var collapseButton = this.SafeQ(collapseButtonPartName);
            collapseButton?.RegisterCallback<ChangeEvent<bool>>(OnCollapseChangeEvent);

            if( !s_DisableHiddenNodeAtCreation_Internal )
            {
                //Ensure that the first computation of Input labels is dones while the node is hidden to avoid a visible jump.
                //We first need a frame to get all the required info ( font, size, style, ... ) of the label in the resolved style to compute each label width
                //then we find the largest input label and set the min-size of all the labels to this.
                visible = false;

                schedule.Execute(()=>
                {
                    visible = true;
                }).ExecuteLater(0);
            }
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            bool collapsed = (NodeModel as ICollapsible)?.Collapsed ?? false;
            EnableInClassList(collapsedUssClassName, collapsed);
        }

        protected void OnCollapseChangeEvent(ChangeEvent<bool> evt)
        {
            GraphView.Dispatch(new CollapseNodeCommand(evt.newValue, NodeModel));
        }
    }
}
