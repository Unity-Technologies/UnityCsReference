// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.GraphToolkit.InternalBridge;
using Unity.GraphToolsAuthoringFramework.InternalEditorBridge;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A part to build the UI for a port connector, a circle where the wires connect.
    /// </summary>
    [UnityRestricted]
    internal class PortConnectorPart : BaseModelViewPart
    {
        internal const float k_IndentWidth = 16;
        /// <summary>
        /// The USS class name added to the <see cref="PortConnectorPart"/> <see cref="Root"/>.
        /// </summary>
        public static readonly string ussClassName = "ge-port-connector-part";

        /// <summary>
        /// The USS name of the connector element.
        /// </summary>
        public static readonly string connectorUssName = "connector";

        /// <summary>
        /// The USS name of the expand toggle element.
        /// </summary>
        public static readonly string expandToggleUssName = "expand-toggle";

        /// <summary>
        /// The USS name of the expand spacer element.
        /// </summary>
        public static readonly string expandSpacerUssName = "expand-spacer";

        /// <summary>
        /// The USS name of the "create from port hit box" element.
        /// </summary>
        public static readonly string createFromPortHitBoxUssName = "create-from-port-hit-box";

        /// <summary>
        /// The USS name of the output spacer element.
        /// </summary>
        public static readonly string outputSpacerUssName = "output-spacer";

        /// <summary>
        /// The USS class name added to output spacer element.
        /// </summary>
        public static readonly string outputSpacerUssClassName = ussClassName.WithUssElement(outputSpacerUssName);

        /// <summary>
        /// The name of a label field.
        /// </summary>
        public static readonly string labelName = GraphElementHelper.labelName;

        /// <summary>
        /// Creates a new instance of the <see cref="PortConnectorPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="PortConnectorPart"/>.</returns>
        public static PortConnectorPart Create(string name, Model model, ChildView ownerElement, string parentClassName)
        {
            if (model is PortModel)
            {
                return new PortConnectorPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        /// <summary>
        /// The label for the port.
        /// </summary>
        protected Label m_ConnectorLabel;

        /// <summary>
        /// The element displaying the wire connector.
        /// </summary>
        protected VisualElement m_ConnectorBox;

        /// <summary>
        /// The root element of the part.
        /// </summary>
        protected VisualElement m_Root;

        /// <summary>
        /// The element that is used to start creating a wire from the port.
        /// </summary>
        protected VisualElement m_CreateFromPortHitBox;

        /// <summary>
        /// The toggle of the <see cref="PortConnectorWithIconPart"/> To expand/collapse expandable ports.
        /// </summary>
        protected Toggle m_ExpandToggle;

        /// <summary>
        /// The element which size is used to indent sub ports.
        /// </summary>
        protected VisualElement m_ExpandSpacer;

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <summary>
        /// The element displaying the wire connector.
        /// </summary>
        public VisualElement Connector => m_ConnectorBox;

        /// <summary>
        /// The toggle used to expand/collapse expandable ports.
        /// </summary>
        public Toggle ExpandToggle => m_ExpandToggle;

        internal VisualElement ExpandSpacer => m_ExpandSpacer;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortConnectorPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected PortConnectorPart(string name, Model model, ChildView ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName)
        {
        }

        /// <inheritdoc />
        protected override void BuildUI(VisualElement container)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(ussClassName);
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            m_ConnectorBox = new VisualElement { name = connectorUssName };
            m_ConnectorBox.AddToClassList(ussClassName.WithUssElement(connectorUssName));
            m_ConnectorBox.AddToClassList(m_ParentClassName.WithUssElement(connectorUssName));
            m_Root.Add(m_ConnectorBox);

            var portModel = m_Model as PortModel;
            if (portModel != null)
            {
                m_ExpandSpacer = new VisualElement();
                m_ExpandSpacer.name = expandSpacerUssName;
                m_ExpandSpacer.AddToClassList(m_ParentClassName.WithUssElement(expandSpacerUssName));
                m_Root.Add(m_ExpandSpacer);

                m_ExpandToggle = new Toggle();
                m_ExpandToggle.name = expandToggleUssName;
                m_ExpandToggle.focusable = true;
                m_ExpandToggle.AddToClassList(Foldout.toggleUssClassName);
                m_ExpandToggle.AddToClassList(m_ParentClassName.WithUssElement(expandToggleUssName));
                m_ExpandToggle.RegisterCallback<ChangeEvent<bool>>(evt =>
                {
                    m_OwnerElement.RootView.Dispatch(new ExpandPortCommand(evt.newValue, new[] { portModel }));
                });
                m_ExpandToggle.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.LeftArrow)
                    {
                        m_ExpandToggle.value = false;
                    }
                    else if (evt.keyCode == KeyCode.RightArrow)
                    {
                        m_ExpandToggle.value = true;
                    }
                });

                // The expand toggle should be on top of the port hit box.
                m_Root.Add(m_ExpandToggle);
            }

            if (m_Model is IHasTitle)
            {
                m_ConnectorLabel = new Label { name = labelName };
                m_ConnectorLabel.AddToClassList(ussClassName.WithUssElement(labelName));
                m_ConnectorLabel.AddToClassList(m_ParentClassName.WithUssElement(labelName));
                m_Root.Add(m_ConnectorLabel);
            }

            m_CreateFromPortHitBox = new VisualElement { name = createFromPortHitBoxUssName };
            m_CreateFromPortHitBox.AddToClassList(ussClassName.WithUssElement(createFromPortHitBoxUssName));
            m_CreateFromPortHitBox.RegisterCallback<MouseOverEvent>(OnMouseOverCreateFromPortHitBox);
            m_CreateFromPortHitBox.RegisterCallback<MouseOutEvent>(OnMouseOutCreateFromPortHitBox);

            m_Root.Add(m_CreateFromPortHitBox);

            if (portModel is { Direction: PortDirection.Output, Orientation: PortOrientation.Horizontal })
            {
                var outputSpacer = new VisualElement();
                outputSpacer.AddToClassList(outputSpacerUssClassName);
                m_Root.Add(outputSpacer);
                m_ExpandToggle.BringToFront();
                m_ExpandSpacer.BringToFront();
                m_CreateFromPortHitBox.BringToFront();
            }

            m_Root.RegisterCallbackOnce<GeometryChangedEvent>(evt =>
            {
                UpdateHitBox();
            });

            container.Add(m_Root);
        }

        void OnMouseOutCreateFromPortHitBox(MouseOutEvent evt)
        {
            if (m_OwnerElement is not Port port)
                return;

            EditorGUIUtilityBridge.SetCursor(MouseCursor.Arrow);
            port.Hovering = false;
        }

        void OnMouseOverCreateFromPortHitBox(MouseOverEvent evt)
        {
            if (m_OwnerElement is not Port port || port.PortModel.Capacity == PortCapacity.None || !port.enabledSelf)
                return;

            // A wire is already being dragged. It is not possible to create a wire from the hovered port hit box.
            if (port.GraphView.IsWireDragging)
                return;

            EditorGUIUtilityBridge.SetCursor(MouseCursor.Link);
            port.Hovering = true;
        }

        void UpdateHitBox()
        {
            if (m_OwnerElement is not Port port)
                return;

            var createFromPortHitBoxRect = Root.WorldToLocal(Port.GetPortHitBoxBounds(port, true));
            m_CreateFromPortHitBox.style.left = createFromPortHitBoxRect.xMin;
            m_CreateFromPortHitBox.style.width = createFromPortHitBoxRect.width;
            m_CreateFromPortHitBox.style.top = createFromPortHitBoxRect.yMin;
            m_CreateFromPortHitBox.style.height = createFromPortHitBoxRect.height;
            m_CreateFromPortHitBox.style.position = new StyleEnum<Position>(Position.Absolute);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();
            m_Root.AddPackageStylesheet("PortConnectorPart.uss");
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            if (m_Model is PortModel portModel)
            {
                m_ExpandToggle.SetValueWithoutNotify(portModel.IsExpandedSelf);

                int rank = 0;
                var current = portModel.ParentPort;

                while (current != null)
                {
                    rank++;
                    current = current.ParentPort;
                }

                m_ExpandSpacer.style.minWidth = k_IndentWidth * rank;

                if (m_ConnectorLabel != null)
                {
                    string portLabel = portModel.ComputePortLabel(false);
                    m_ConnectorLabel.text = portLabel;
                    m_ConnectorLabel.tooltip = portModel.ToolTip;
                }

                if (portModel.Orientation == PortOrientation.Vertical)
                {
                    m_CreateFromPortHitBox.tooltip = portModel.ToolTip;
                }
            }
            else if (m_ConnectorLabel != null && visitor.ChangeHints.HasChange(ChangeHint.Data))
                m_ConnectorLabel.text = (m_Model as IHasTitle)?.Title ?? String.Empty;

            m_ConnectorBox.MarkDirtyRepaint();

        }
    }
}
