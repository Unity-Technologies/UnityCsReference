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

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            PartList.AppendPart(VerticalPortContainerPart.Create(topPortContainerPartName, PortDirection.Input, Model, this, ussClassName));

            PartList.AppendPart(IconTitleProgressPart.Create(titleIconContainerPartName, Model, this, ussClassName, true));
            PartList.AppendPart(SerializedFieldsInspector.Create(nodeSettingsContainerPartName, new[] {Model}, RootView, ussClassName, ModelInspectorView.BasicSettingsFilter));
            PartList.AppendPart(InOutPortContainerPart.Create(portContainerPartName, Model, this, ussClassName));

            PartList.AppendPart(VerticalPortContainerPart.Create(bottomPortContainerPartName, PortDirection.Output, Model, this, ussClassName));

            PartList.AppendPart(NodeLodCachePart.Create(nodeCachePartName, Model, this, ussClassName));
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            var collapseButton = this.SafeQ(collapseButtonPartName);
            collapseButton?.RegisterCallback<ChangeEvent<bool>>(OnCollapseChangeEvent);
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
