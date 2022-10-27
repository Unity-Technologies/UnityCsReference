// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable InconsistentNaming

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// UI for a <see cref="AbstractNodeModel"/>.
    /// </summary>
    class Node : GraphElement
    {
        public new static readonly string ussClassName = "ge-node";
        public static readonly string notConnectedModifierUssClassName = ussClassName.WithUssModifier("not-connected");
        public static readonly string emptyModifierUssClassName = ussClassName.WithUssModifier("empty");
        public static readonly string disabledModifierUssClassName = ussClassName.WithUssModifier("disabled");
        public static readonly string unusedModifierUssClassName = ussClassName.WithUssModifier("unused");
        public static readonly string readOnlyModifierUssClassName = ussClassName.WithUssModifier("read-only");
        public static readonly string writeOnlyModifierUssClassName = ussClassName.WithUssModifier("write-only");

        public static readonly string selectionBorderElementName = "selection-border";
        public static readonly string disabledOverlayElementName = "disabled-overlay";
        public static readonly string titleContainerPartName = "title-container";
        public static readonly string nodeSettingsContainerPartName = "node-settings";

        /// <summary>
        /// The name of the port container part.
        /// </summary>
        public static readonly string portContainerPartName = "port-container";

        /// <summary>
        /// The name of the title cache part.
        /// </summary>
        public static readonly string nodeCachePartName = "cache";

        protected VisualElement m_ContentContainer;

        public AbstractNodeModel NodeModel => Model as AbstractNodeModel;

        /// <inheritdoc />
        public override VisualElement contentContainer => m_ContentContainer ?? this;

        /// The <see cref="DynamicBorder"/> used to display selection, hover and highlight.
        protected DynamicBorder Border { get; private set; }

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            PartList.AppendPart(EditableTitlePart.Create(titleContainerPartName, Model, this, ussClassName));
            PartList.AppendPart(SerializedFieldsInspector.Create(nodeSettingsContainerPartName, new[] {Model}, RootView, ussClassName, ModelInspectorView.BasicSettingsFilter));
            PartList.AppendPart(PortContainerPart.Create(portContainerPartName, Model, this, ussClassName));
        }

        /// <inheritdoc />
        protected override void BuildElementUI()
        {
            var selectionBorder = new SelectionBorder { name = selectionBorderElementName };
            selectionBorder.AddToClassList(ussClassName.WithUssElement(selectionBorderElementName));
            Add(selectionBorder);
            m_ContentContainer = selectionBorder.ContentContainer;

            base.BuildElementUI();

            var disabledOverlay = new VisualElement { name = disabledOverlayElementName, pickingMode = PickingMode.Ignore };
            hierarchy.Add(disabledOverlay);

            Border = CreateDynamicBorder();
            Border.AddToClassList(ussClassName.WithUssElement("dynamic-border"));
            hierarchy.Add(Border);
        }

        /// <summary>
        /// Creates a <see cref="DynamicBorder"/> for this node.
        /// </summary>
        /// <returns>A <see cref="DynamicBorder"/> for this node.</returns>
        protected virtual DynamicBorder CreateDynamicBorder()
        {
            return new DynamicBorder(this);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            usageHints = UsageHints.DynamicTransform;
            AddToClassList(ussClassName);
            this.AddStylesheet_Internal("Node.uss");
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            SetPosition(NodeModel.Position);

            EnableInClassList(emptyModifierUssClassName, childCount == 0);
            EnableInClassList(disabledModifierUssClassName, NodeModel.State == ModelState.Disabled);

            if (NodeModel is PortNodeModel portHolder && portHolder.Ports != null)
            {
                bool noPortConnected = portHolder.Ports.All(port => !port.IsConnected());
                EnableInClassList(notConnectedModifierUssClassName, noPortConnected);
            }

            if (Model is VariableNodeModel variableModel)
            {
                EnableInClassList(readOnlyModifierUssClassName, variableModel.VariableDeclarationModel?.Modifiers == ModifierFlags.Read);
                EnableInClassList(writeOnlyModifierUssClassName, variableModel.VariableDeclarationModel?.Modifiers == ModifierFlags.Write);
            }

            tooltip = NodeModel.Tooltip;

            Border.Selected = IsSelected();
        }

        public override void ActivateRename()
        {
            if (!((PartList.GetPart(titleContainerPartName) as EditableTitlePart)?.TitleLabel is EditableLabel label))
                return;

            label.BeginEditing();
        }

        /// <inheritdoc />
        public override void SetElementLevelOfDetail(float zoom, GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode)
        {
            base.SetElementLevelOfDetail(zoom, newZoomMode, oldZoomMode);

            Border.Zoom = zoom;
        }
    }
}
