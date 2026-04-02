// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal class PortHierarchyContainer : PortContainer
    {
        /// <summary>
        /// The USS class name added to a <see cref="PortContainer"/>.
        /// </summary>
        public new static readonly string ussClassName = "ge-port-hierarchy-container";

        /// <summary>
        /// The USS class name added to the content container.
        /// </summary>
        public static readonly string contentContainerUssClassName = ussClassName.WithUssElement(GraphElementHelper.containerName);

        /// <summary>
        /// The USS class name added to the lines element.
        /// </summary>
        public static readonly string linesUssClassName = ussClassName.WithUssElement("lines");

        VisualElement m_ContentContainer;
        VisualElement m_LinesElement;

        Color m_LineColor = Color.gray;
        float m_LineWidth = 1;

        bool m_NoLineOnFirstPort;

        static readonly CustomStyleProperty<Color> k_LineColorProperty = new CustomStyleProperty<Color>("--line-color");
        static readonly CustomStyleProperty<float> k_PortLineWidthProperty = new CustomStyleProperty<float>("--line-width");

        /// <summary>
        /// The color of the lines.
        /// </summary>
        public Color LineColor => m_LineColor;

        /// <summary>
        /// The width of the lines.
        /// </summary>
        public float LineWidth => m_LineWidth;

        /// <inheritdoc />
        public override VisualElement contentContainer => m_ContentContainer;


        /// <summary>
        /// Initializes a new instance of the <see cref="PortHierarchyContainer"/> class.
        /// </summary>
        /// <param name="setupLabelWidth">Whether the label width should be computed based on the largest label.</param>
        /// <param name="maxLabelWidth">If <paramref name="setupLabelWidth"/> is true, sets the maximum width for the labels.</param>
        /// <param name="setCountModifierOnParent">Whether to set the class for the modifier of the number of port on parent <see cref="VisualElement"/> too.</param>
        /// <param name="noLineOnFirstPort">Whether the first line expands to the first port or starts at the first sub port.</param>
        public PortHierarchyContainer(bool setupLabelWidth, float maxLabelWidth, bool setCountModifierOnParent = false, bool noLineOnFirstPort = false)
            : base(setupLabelWidth, maxLabelWidth, setCountModifierOnParent)
        {
            m_NoLineOnFirstPort = noLineOnFirstPort;
            m_ContentContainer = new VisualElement();
            m_ContentContainer.AddToClassList(contentContainerUssClassName);
            hierarchy.Add(m_ContentContainer);

            AddToClassList(ussClassName);
            m_LinesElement = new VisualElement();
            m_LinesElement.AddToClassList(linesUssClassName);
            m_LinesElement.pickingMode = PickingMode.Ignore;
            m_LinesElement.generateVisualContent += GenerateLines;

            hierarchy.Add(m_LinesElement);

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PortHierarchyContainer"/> class.
        /// <param name="noLineOnFirstPort">Whether the first line expands to the first port or starts at the first sub port.</param>
        /// </summary>
        public PortHierarchyContainer(bool noLineOnFirstPort = false) : this(false, float.PositiveInfinity, noLineOnFirstPort: noLineOnFirstPort)
        { }

        void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            var newColor = Color.gray;
            if (evt.customStyle.TryGetValue(k_LineColorProperty, out var portColorValue))
                newColor = portColorValue;

            float newLineWidth = 1;
            if (evt.customStyle.TryGetValue(k_PortLineWidthProperty, out var portLineWidthValue))
                newLineWidth = portLineWidthValue;

            bool dirty = false;
            if (newColor != m_LineColor)
            {
                m_LineColor = newColor;
                dirty = true;
            }

            if (newLineWidth != m_LineWidth)
            {
                m_LineWidth = newLineWidth;
                dirty = true;
            }

            if (dirty)
                MarkDirtyRepaint();
        }

        /// <inheritdoc />
        public override void UpdatePorts(UpdateFromModelVisitor visitor, IEnumerable<PortModel> ports, RootView view)
        {
            base.UpdatePorts(visitor, ports, view);
            m_LinesElement.MarkDirtyRepaint(); //one time to clear the current line
            schedule.Execute(m_LinesElement.MarkDirtyRepaint).ExecuteLater(0); // one time for proper refresh.
        }

        void GenerateLines(MeshGenerationContext mgc)
        {
            //We need one line for each port that is an ancestor to a visible port. Note that this parent port may not be visible itself (if one of its ancestor is collapsed but the sub port is connected).
            Painter2D painter2D = null;

            using var dispose = DictionaryPool<PortModel, Port>.Get(out var parentPortUIs);
            using var dispose2 = ListPool<VisualElement>.Get(out var children);
            children.AddRange(m_ContentContainer.Children());

            // first we collect these ancestor ports
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child is Port port)
                {
                    var parentPort = port.PortModel.ParentPort;
                    while (parentPort != null)
                    {
                        if (!parentPortUIs.TryGetValue(parentPort, out var parent))
                        {
                            var uiParentIndex = children.FindIndex(0, i, t => t is Port p && p.PortModel == parentPort);
                            parentPortUIs[parentPort] = uiParentIndex >= 0 ? (Port)children[uiParentIndex] : null;
                        }
                        parentPort = parentPort.ParentPort;
                    }
                }
            }

            // Then we browse the visible ports
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] is Port visibleSubPort)
                {
                    var parentPortModel = visibleSubPort.PortModel.ParentPort;

                    if (parentPortModel != null
                        && parentPortModel.EmbeddedValue != null
                        && parentPortModel.EmbeddedValue.Type.IsListOrArray())
                        continue;

                    float rank = 0.5f;
                    //we look for the parent port UI and then remove it from the list, ensuring only one line is drawn per parent port when if a parent port has multiple visible descendants.
                    while (parentPortModel != null && parentPortUIs.Remove(parentPortModel, out var parentPortUI))
                    {
                        //we search for visible descendant ports of this parent port
                        int lastVisibleSubPortIndex = i + 1;
                        for (; lastVisibleSubPortIndex < children.Count; lastVisibleSubPortIndex++)
                        {
                            if (children[lastVisibleSubPortIndex] is Port subPort)
                            {
                                if (!subPort.PortModel.IsDescendantOf(parentPortModel))
                                    break;
                            }
                        }

                        float x, startY;

                        if (parentPortUI != null && (!m_NoLineOnFirstPort || parentPortUI != children[0]))
                        {
                            // If the parent port ui exists, we use it to get the line position
                            var connectorPart = parentPortUI.PartList.GetPart(Port.connectorPartName) as PortConnectorPart;
                            if (connectorPart == null)
                                continue;
                            var expandToggle = connectorPart.ExpandToggle;
                            var expandToggleRect = expandToggle.parent.ChangeCoordinatesTo(this, expandToggle.layout);
                            if (!RectIsFinite(ref expandToggleRect))
                                continue;

                            x = expandToggleRect.center.x;
                            startY = expandToggleRect.yMax;
                        }
                        else
                        {
                            // Else use the first visible sub port to get the line position.
                            var connectorPart = visibleSubPort.PartList.GetPart(Port.connectorPartName) as PortConnectorPart;
                            if (connectorPart == null)
                                continue;
                            var expandRect = connectorPart.ExpandSpacer.parent.ChangeCoordinatesTo(this, connectorPart.ExpandSpacer.layout);
                            if (!RectIsFinite(ref expandRect))
                                continue;
                            var portRect = visibleSubPort.parent.ChangeCoordinatesTo(this, visibleSubPort.layout);
                            if (!RectIsFinite(ref portRect))
                                continue;

                            startY = portRect.yMin + 4;
                            x = expandRect.xMax - rank * PortConnectorPart.k_IndentWidth + connectorPart.ExpandSpacer.resolvedStyle.marginRight;
                        }

                        var lastDescendant = m_ContentContainer[lastVisibleSubPortIndex - 1];
                        var lastDescendantRect = lastDescendant.parent.ChangeCoordinatesTo(this, lastDescendant.layout);
                        if (!RectIsFinite(ref lastDescendantRect))
                            continue;


                        if (painter2D == null)
                        {
                            painter2D = mgc.painter2D;
                            painter2D.BeginPath();
                        }
                        painter2D.MoveTo(new Vector2(x, startY));
                        painter2D.LineTo(new Vector2(x, lastDescendantRect.yMax - 4));

                        parentPortModel = parentPortModel.ParentPort;
                        rank++;
                    }
                }
            }

            if (painter2D != null)
            {
                painter2D.strokeColor = m_LineColor;
                painter2D.lineWidth = m_LineWidth;
                painter2D.Stroke();
            }

            bool RectIsFinite(ref Rect r)
            {
                return float.IsFinite(r.x) && float.IsFinite(r.y) && float.IsFinite(r.width) && float.IsFinite(r.height);
            }
        }
    }
}
