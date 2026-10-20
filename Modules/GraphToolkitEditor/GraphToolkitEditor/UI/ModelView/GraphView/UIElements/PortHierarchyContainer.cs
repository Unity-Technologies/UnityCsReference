// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
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

        /// <summary>
        /// The USS class name added to each clickable line element.
        /// </summary>
        public static readonly string lineUssClassName = ussClassName.WithUssElement("line");

        /// <summary>
        /// The USS class name added to a line when it or the expand toggle of its parent port is hovered.
        /// </summary>
        public static readonly string lineHighlightedUssClassName = lineUssClassName.WithUssModifier("highlighted");

        /// <summary>
        /// The USS class name added to the expand toggle of a port when it or the line of the port is hovered.
        /// </summary>
        public static readonly string expandToggleHighlightedUssClassName = ussClassName.WithUssElement("expand-toggle").WithUssModifier("highlighted");

        const float k_LineHitWidth = 8;

        struct LineSegment
        {
            public PortModel ParentPort;
            public float X;
            public float StartY;
            public float EndY;
            // False when the parent port is not displayed, as there is no expand toggle to act on.
            public bool IsInteractive;
        }

        VisualElement m_ContentContainer;
        VisualElement m_LinesElement;
        RootView m_RootView;
        PortModel m_HighlightedPort;
        Toggle m_HighlightedExpandToggle;
        HierarchyLine m_HoveredLine;

        bool m_NoLineOnFirstPort;

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
            m_ContentContainer.RegisterCallback<GeometryChangedEvent>(_ => RefreshLines());
            hierarchy.Add(m_ContentContainer);

            AddToClassList(ussClassName);
            m_LinesElement = new VisualElement();
            m_LinesElement.AddToClassList(linesUssClassName);
            m_LinesElement.pickingMode = PickingMode.Ignore;

            hierarchy.Add(m_LinesElement);

            // The line and the expand toggle of a port are highlighted together when either is hovered.
            // Every element entered inside the container sends a PointerOverEvent, so no PointerOutEvent is needed.
            RegisterCallback<PointerOverEvent>(OnPointerOver);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PortHierarchyContainer"/> class.
        /// <param name="noLineOnFirstPort">Whether the first line expands to the first port or starts at the first sub port.</param>
        /// </summary>
        public PortHierarchyContainer(bool noLineOnFirstPort = false) : this(false, float.PositiveInfinity, noLineOnFirstPort: noLineOnFirstPort)
        { }

        /// <inheritdoc />
        public override void UpdatePorts(UpdateFromModelVisitor visitor, IEnumerable<PortModel> ports, RootView view)
        {
            m_RootView = view;
            base.UpdatePorts(visitor, ports, view);
            RefreshLines(); //one time to clear the current lines
            schedule.Execute(RefreshLines).ExecuteLater(0); // one time for proper refresh.
        }

        void RefreshLines()
        {
            using var dispose = ListPool<LineSegment>.Get(out var segments);
            ComputeLineSegments(segments);

            var hoveredLinePort = m_HoveredLine?.ParentPort;

            while (m_LinesElement.childCount > segments.Count)
            {
                m_LinesElement.RemoveAt(m_LinesElement.childCount - 1);
            }
            while (m_LinesElement.childCount < segments.Count)
            {
                m_LinesElement.Add(CreateLineElement());
            }

            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                var line = (HierarchyLine)m_LinesElement[i];
                line.ParentPort = segment.ParentPort;
                line.StrokeX = segment.X;
                line.pickingMode = segment.IsInteractive ? PickingMode.Position : PickingMode.Ignore;
                line.style.left = segment.X - k_LineHitWidth * 0.5f;
                line.style.top = segment.StartY;
                line.style.height = Mathf.Max(0, segment.EndY - segment.StartY);
                line.EnableInClassList(lineHighlightedUssClassName, m_HighlightedPort != null && line.ParentPort == m_HighlightedPort);
            }

            // The hovered line receives no pointer event when it is removed, reused for another port or made non-interactive, so its highlight is cleared here.
            if (m_HoveredLine != null && (m_HoveredLine.parent == null || m_HoveredLine.ParentPort != hoveredLinePort || m_HoveredLine.pickingMode == PickingMode.Ignore))
            {
                m_HoveredLine = null;
                SetHighlightedPort(null);
            }
        }

        void OnPointerOver(PointerOverEvent evt)
        {
            m_HoveredLine = evt.target as HierarchyLine;
            SetHighlightedPort(m_HoveredLine != null ? m_HoveredLine.ParentPort : GetExpandTogglePort(evt.target as VisualElement));
        }

        void OnPointerLeave(PointerLeaveEvent evt)
        {
            m_HoveredLine = null;
            SetHighlightedPort(null);
        }

        PortModel GetExpandTogglePort(VisualElement element)
        {
            for (; element != null && element != this; element = element.hierarchy.parent)
            {
                if (element is Toggle toggle && toggle.GetFirstAncestorOfType<Port>() is { } port
                    && port.PartList.GetPart(Port.connectorPartName) is PortConnectorPart connectorPart
                    && connectorPart.ExpandToggle == toggle)
                    return port.PortModel;
            }
            return null;
        }

        void SetHighlightedPort(PortModel portModel)
        {
            if (m_HighlightedPort == portModel)
                return;
            m_HighlightedPort = portModel;

            for (int i = 0; i < m_LinesElement.childCount; i++)
            {
                var line = (HierarchyLine)m_LinesElement[i];
                line.EnableInClassList(lineHighlightedUssClassName, portModel != null && line.ParentPort == portModel);
            }

            m_HighlightedExpandToggle?.RemoveFromClassList(expandToggleHighlightedUssClassName);
            m_HighlightedExpandToggle = null;
            if (portModel == null)
                return;

            for (var i = 0; i < m_ContentContainer.childCount; i++)
            {
                if (m_ContentContainer[i] is not Port port || port.PortModel != portModel)
                    continue;

                if (port.PartList.GetPart(Port.connectorPartName) is PortConnectorPart { ExpandToggle: not null } connectorPart)
                {
                    m_HighlightedExpandToggle = connectorPart.ExpandToggle;
                    m_HighlightedExpandToggle.AddToClassList(expandToggleHighlightedUssClassName);
                }
                break;
            }
        }

        HierarchyLine CreateLineElement()
        {
            var line = new HierarchyLine();
            line.AddToClassList(lineUssClassName);
            line.style.width = k_LineHitWidth;

            // Node selection and dragging start from MouseDownEvent, so it is stopped for a toggle click.
            // PointerDownEvent is left alone so that pan gestures (middle, right or Alt drag) still reach the graph view.
            line.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (IsToggleClick(evt.button, evt.altKey))
                    evt.StopPropagation();
            });
            line.RegisterCallback<ClickEvent>(evt =>
            {
                if (!IsToggleClick(evt.button, evt.altKey))
                    return;

                var portModel = line.ParentPort;
                if (portModel != null && m_RootView != null && portModel.GraphModel.CanExpandPort(portModel))
                    m_RootView.Dispatch(new ExpandPortCommand(!portModel.IsExpandedSelf, new[] { portModel }));
                evt.StopPropagation();
            });
            return line;
        }

        static bool IsToggleClick(int button, bool altKey) => button == (int)MouseButton.LeftMouse && !altKey;

        internal class TestAccess
        {
            readonly PortHierarchyContainer m_Container;

            public TestAccess(PortHierarchyContainer container) => m_Container = container;

            public VisualElement GetLine(PortModel parentPort)
            {
                for (var i = 0; i < m_Container.m_LinesElement.childCount; i++)
                {
                    if (m_Container.m_LinesElement[i] is HierarchyLine line && line.ParentPort == parentPort)
                        return line;
                }
                return null;
            }
        }

        class HierarchyLine : VisualElement
        {
            [NoAutoStaticsCleanup] // CSS custom property descriptor; value is a fixed CSS property name
            static readonly CustomStyleProperty<Color> k_StrokeColorProperty = new CustomStyleProperty<Color>("--line-stroke-color");
            [NoAutoStaticsCleanup] // CSS custom property descriptor; value is a fixed CSS property name
            static readonly CustomStyleProperty<float> k_StrokeWidthProperty = new CustomStyleProperty<float>("--line-stroke-width");

            Color m_StrokeColor = Color.gray;
            float m_StrokeWidth = 1;
            float m_StrokeX;

            public PortModel ParentPort { get; set; }

            // Kept apart from layout.x, which is snapped to whole pixels and would offset the stroke from the arrow center.
            public float StrokeX
            {
                set
                {
                    if (Mathf.Approximately(m_StrokeX, value))
                        return;
                    m_StrokeX = value;
                    MarkDirtyRepaint();
                }
            }

            public HierarchyLine()
            {
                generateVisualContent += GenerateStroke;
                RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
                RegisterCallback<GeometryChangedEvent>(_ => MarkDirtyRepaint());
            }

            void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
            {
                m_StrokeColor = evt.customStyle.TryGetValue(k_StrokeColorProperty, out var color) ? color : Color.gray;
                m_StrokeWidth = evt.customStyle.TryGetValue(k_StrokeWidthProperty, out var width) ? width : 1;
                MarkDirtyRepaint();
            }

            void GenerateStroke(MeshGenerationContext mgc)
            {
                var x = m_StrokeX - layout.x;
                // Round caps extend past the path ends by half the width, so the path is shortened to keep the line within its segment.
                var capOffset = Mathf.Min(m_StrokeWidth * 0.5f, layout.height * 0.5f);
                var painter2D = mgc.painter2D;
                painter2D.strokeColor = m_StrokeColor;
                painter2D.lineWidth = m_StrokeWidth;
                painter2D.lineCap = LineCap.Round;
                painter2D.BeginPath();
                painter2D.MoveTo(new Vector2(x, capOffset));
                painter2D.LineTo(new Vector2(x, layout.height - capOffset));
                painter2D.Stroke();
            }
        }

        void ComputeLineSegments(List<LineSegment> segments)
        {
            //We need one line for each port that is an ancestor to a visible port. Note that this parent port may not be visible itself (if one of its ancestor is collapsed but the sub port is connected).
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
                            // Align on the arrow image rather than the whole toggle, whose bounds include the input border.
                            var expandArrow = connectorPart.ExpandToggle.Q(className: Toggle.checkmarkUssClassName) ?? connectorPart.ExpandToggle;
                            var expandArrowRect = expandArrow.parent.ChangeCoordinatesTo(this, expandArrow.layout);
                            if (!RectIsFinite(ref expandArrowRect))
                                continue;

                            x = expandArrowRect.center.x;
                            startY = expandArrowRect.yMax;
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

                        segments.Add(new LineSegment
                        {
                            ParentPort = parentPortModel,
                            X = x,
                            StartY = startY,
                            EndY = lastDescendantRect.yMax - 4,
                            IsInteractive = parentPortUI != null,
                        });

                        parentPortModel = parentPortModel.ParentPort;
                        rank++;
                    }
                }
            }

            bool RectIsFinite(ref Rect r)
            {
                return float.IsFinite(r.x) && float.IsFinite(r.y) && float.IsFinite(r.width) && float.IsFinite(r.height);
            }
        }
    }
}
