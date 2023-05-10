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

        public static readonly string disabledOverlayElementName = "disabled-overlay";
        public static readonly string titleContainerPartName = "title-container";

        public static readonly string nodeOptionsContainerPartName = "node-options";
        [Obsolete("nodeSettingsContainerPartName has been deprecated. Use nodeOptionsContainerPartName instead.")]
        public static readonly string nodeSettingsContainerPartName = "node-options";
        public static readonly string nodeModeDropDownPartName = "node-mode-drop-down";

        /// <summary>
        /// The name of the port container part.
        /// </summary>
        public static readonly string portContainerPartName = "port-container";

        /// <summary>
        /// The name of the title cache part.
        /// </summary>
        public static readonly string nodeCachePartName = "cache";

        public AbstractNodeModel NodeModel => Model as AbstractNodeModel;

        VisualElement m_ShowNodePreviewButton;
        VisualElement m_CollapseButton;

        public Node():base(true)
        {}

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            PartList.AppendPart(NodeTitlePart.Create(titleContainerPartName, NodeModel, this, ussClassName));
            PartList.AppendPart(NodeModeDropDownPart.Create(nodeModeDropDownPartName, Model, this, ussClassName));
            PartList.AppendPart(NodeOptionsInspector.Create(nodeOptionsContainerPartName, new[] {Model}, this, ussClassName, ModelInspectorView.NodeOptionsFilterForNode));
            PartList.AppendPart(HorizontalPortContainerPart.Create(portContainerPartName, Model, this, ussClassName));
        }

        /// <inheritdoc />
        protected override void BuildElementUI()
        {
            base.BuildElementUI();

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
            this.AddStylesheet_Internal("Node.uss");

            m_CollapseButton = this.SafeQ(NodeTitlePart.collapseButtonPartName);
            m_ShowNodePreviewButton = this.SafeQ(NodeTitlePart.previewButtonPartName);
            m_ShowNodePreviewButton?.RegisterCallback<ChangeEvent<bool>>(OnShowNodePreviewChangeEvent);
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
        }

        public override void ActivateRename()
        {
            if (!((PartList.GetPart(titleContainerPartName) as EditableTitlePart)?.TitleLabel is EditableLabel label))
                return;

            label.BeginEditing();
        }

        void OnMouseEnter(MouseEnterEvent evt)
        {
            ShowButtons(true);
        }

        void OnMouseLeave(MouseLeaveEvent evt)
        {
            ShowButtons(false);
        }

        public void ShowButtons(bool show)
        {
            m_CollapseButton?.EnableInClassList(ToggleIconButton.visibleUssClassName, show);

            if (NodeModel.HasNodePreview)
            {
                m_ShowNodePreviewButton?.EnableInClassList(ToggleIconButton.visibleUssClassName, show);

                var nodePreview = NodeModel.NodePreviewModel.GetView<NodePreview>(RootView);
                nodePreview?.ShowClosePreviewButtonElement(show);
            }
        }

        protected void OnShowNodePreviewChangeEvent(ChangeEvent<bool> evt)
        {
            GraphView.Dispatch(new ShowNodePreviewCommand(evt.newValue, NodeModel));
        }

        /// <inheritdoc />
        public override void SetElementLevelOfDetail(float zoom, GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode)
        {
            base.SetElementLevelOfDetail(zoom, newZoomMode, oldZoomMode);

            if (NodeModel.HasNodePreview)
                SetToggleButtonLevelOfDetail(m_ShowNodePreviewButton);

            SetToggleButtonLevelOfDetail(m_CollapseButton);

            void SetToggleButtonLevelOfDetail(VisualElement button)
            {
                var buttonIcon = button?.SafeQ(ToggleIconButton.iconElementName);
                if (buttonIcon == null)
                    return;

                const int minSize = 9;
                const int normalSize = 16;
                const int marginLeft = 4;
                var scale = 1.0f / zoom;

                switch (newZoomMode)
                {
                    case GraphViewZoomMode.Normal:
                        button.style.marginLeft = marginLeft;
                        buttonIcon.style.width = normalSize;
                        buttonIcon.style.height = normalSize;
                        break;
                    case GraphViewZoomMode.Medium:
                    case GraphViewZoomMode.Small:
                    case GraphViewZoomMode.VerySmall:
                        button.style.marginLeft = marginLeft * scale;
                        buttonIcon.style.width = minSize * scale;
                        buttonIcon.style.height = minSize * scale;
                        break;
                }
            }
        }
    }
}
