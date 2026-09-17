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
    /// A border around a state. It is used to display the connector dot when a transition is being created or manipulated.
    /// </summary>
    [UnityRestricted]
    internal class StateBorder : ExternalDynamicBorder
    {
        /// <summary>
        /// The USS class name added to a <see cref="StateBorder"/>.
        /// </summary>
        public static readonly string ussClassName = "ge-state-border";

        /// <summary>
        /// The name of a connector.
        /// </summary>
        public static readonly string connectorElementName = "connector";

        /// <summary>
        /// The USS class name added when in manipulation mode.
        /// </summary>
        public static readonly string manipulationModeUssClassName = ussClassName.WithUssModifier("manipulation-mode"); // blue (otherwise gray)

        /// <summary>
        /// The USS class name added when the connector is hidden.
        /// </summary>
        public static readonly string connectorHiddenUssClassName = ussClassName.WithUssModifier("connector-hidden"); // hidden

        const float k_CircleDiameter = 11f;

        /// <summary>
        /// The width of the band inside the state in which a transition can be started, in screen pixels.
        /// </summary>
        const float k_InnerAnchorWidth = k_CircleDiameter / 2;

        /// <summary>
        /// The narrowest the band outside the state is allowed to be, in screen pixels. The band otherwise
        /// keeps the width the border reaches at 100% zoom, whichever is larger.
        /// </summary>
        const float k_MinOuterAnchorWidth = 12f;

        /// <summary>
        /// The widest the outer band is ever allowed to get, in graph units, however far the view is zoomed out.
        /// </summary>
        const float k_MaxOuterAnchorReach = 48f;

        /// <summary>
        /// The largest fraction of the state's smallest side the outer band may span.
        /// </summary>
        const float k_MaxOuterAnchorRatio = 0.5f;

        /// <summary>
        /// The largest fraction of the state's smallest side the inner band may span, so that the two
        /// inner bands never meet in the middle of the state.
        /// </summary>
        const float k_MaxInnerAnchorRatio = 0.25f;

        /// <summary>
        /// How much the outer band has to change before the border is resized, in graph units. Zooming
        /// changes the band continuously; quantizing it keeps a smooth zoom from restyling every state
        /// on every frame, at a cost far below what a user can aim for.
        /// </summary>
        const float k_AnchorBandQuantum = 0.5f;

        string m_VisibilityUssClassCache;
        bool m_IsBorderHovered;
        readonly VisualElement m_Connector;
        readonly ModelView m_StateView;

        float m_ExtraMargin;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateBorder"/> class.
        /// </summary>
        /// <param name="view">The state on which this border is added.</param>
        public StateBorder(ModelView view)
            : base(view)
        {
            m_StateView = view;

            m_Connector = new VisualElement
            {
                name = connectorElementName,
            };
            m_Connector.AddToClassList(ussClassName.WithUssElement(connectorElementName));
            Add(m_Connector);
            HideConnector();

            pickingMode = PickingMode.Position;

            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);

            // The band depends on the resolved padding and on the state's size, neither of which is known
            // when custom styles resolve, so it has to be recomputed once layout has run.
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            this.AddPackageStylesheet("StateBorder.uss");
        }

        /// <inheritdoc />
        protected override float ExtraMargin => m_ExtraMargin;

        /// <summary>
        /// How far past the edge of the state the base border already reaches, in graph units.
        /// </summary>
        float BaseOuterReach => BaseMargin - resolvedStyle.paddingLeft;

        /// <summary>
        /// How far inside the state a point can be and still start a transition, in graph units.
        /// </summary>
        /// <remarks>
        /// Grows as the view zooms out so that the band keeps the size it has at 100% zoom, up to a fraction
        /// of the state's smallest side so that the bands on opposite sides never meet.
        /// </remarks>
        float InnerAnchorReach
        {
            get
            {
                var smallestSide = Mathf.Min(m_StateView.layout.width, m_StateView.layout.height);
                var maxReach = Mathf.Max(k_InnerAnchorWidth, smallestSide * k_MaxInnerAnchorRatio);
                return Mathf.Clamp(ZoomCompensated(k_InnerAnchorWidth), k_InnerAnchorWidth, maxReach);
            }
        }

        /// <summary>
        /// Converts a width expressed in screen pixels to the graph units that span it at the current zoom.
        /// </summary>
        float ZoomCompensated(float screenWidth) => screenWidth / Mathf.Max(Zoom, k_MinZoom);

        /// <summary>
        /// Recomputes how far the border reaches past the state, so that the band in which a transition can be
        /// started keeps the size it has at 100% zoom as the view zooms out.
        /// </summary>
        void UpdateAnchorBand()
        {
            // The band is never narrowed, only widened, so 100% zoom behaves as it did before.
            var baseReach = BaseOuterReach;
            var targetWidth = Mathf.Max(k_MinOuterAnchorWidth, baseReach);
            var smallestSide = Mathf.Min(m_StateView.layout.width, m_StateView.layout.height);
            var maxReach = Mathf.Max(baseReach, Mathf.Min(k_MaxOuterAnchorReach, smallestSide * k_MaxOuterAnchorRatio));
            var reach = Mathf.Clamp(ZoomCompensated(targetWidth), baseReach, maxReach);

            var extraMargin = Mathf.Round((reach - baseReach) / k_AnchorBandQuantum) * k_AnchorBandQuantum;
            if (Mathf.Approximately(extraMargin, m_ExtraMargin))
                return;

            m_ExtraMargin = extraMargin;
            UpdateMargins();
            MarkDirtyRepaint();
        }

        /// <inheritdoc />
        protected override void OnZoomChanged()
        {
            base.OnZoomChanged();
            UpdateAnchorBand();
        }

        /// <inheritdoc />
        protected override void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            base.OnCustomStyleResolved(e);

            // The base reach depends on the border widths resolved above.
            UpdateAnchorBand();
        }

        // Resizing the border changes its own geometry, but the quantized early-out in UpdateAnchorBand
        // settles on a stable value rather than looping.
        void OnGeometryChanged(GeometryChangedEvent e)
        {
            UpdateAnchorBand();
        }

        /// <summary>
        /// Snaps a point to the border of the state.
        /// </summary>
        /// <param name="point">The point to snap, expressed in the <see cref="StateBorder"/> space.</param>
        /// <param name="insideStateIsNone">Whether to return <see cref="AnchorSide.None"/> if the point is inside the state.</param>
        /// <returns>A tuple containing the snapped point, the anchor side, and the offset from the state top or left side.</returns>
        public (Vector2, AnchorSide, float) SnapPointToBorder(Vector2 point, bool insideStateIsNone)
        {
            var anchorSide = GetAnchorSideFromLocalPoint(point, insideStateIsNone);

            if (anchorSide == AnchorSide.None)
                return (point, anchorSide, 0f);

            point = this.ChangeCoordinatesTo(m_StateView, point);
            var offset = anchorSide switch
            {
                AnchorSide.Left => Math.Clamp(point.y, 0, m_StateView.layout.height),
                AnchorSide.Right => Math.Clamp(point.y, 0, m_StateView.layout.height),
                AnchorSide.Top => Math.Clamp(point.x, 0, m_StateView.layout.width),
                AnchorSide.Bottom => Math.Clamp(point.x, 0, m_StateView.layout.width),
                _ => 0f
            };

            var snappedPoint = m_StateView.GetPositionFromAnchorAndOffset(anchorSide, offset, GraphView.Zoom);
            snappedPoint = this.WorldToLocal(snappedPoint);
            return (snappedPoint, anchorSide, offset);
        }

        /// <summary>
        /// Sets the connector dot position.
        /// </summary>
        /// <param name="position">The position, in the local coordinate system.</param>
        void SetConnectorPosition(Vector2 position)
        {
            m_Connector.style.left = position.x - k_CircleDiameter / 2;
            m_Connector.style.top = position.y - k_CircleDiameter / 2;

            m_Connector.MarkDirtyRepaint();
        }

        /// <summary>
        /// Computes the anchor side of a point.
        /// </summary>
        /// <param name="localPosition">The point to compute the anchor side from, in the local coordinate system.</param>
        /// <param name="insideStateIsNone">Whether to return <see cref="AnchorSide.None"/> if the point is inside the state.</param>
        /// <returns>The anchor side of the point.</returns>
        AnchorSide GetAnchorSideFromLocalPoint(Vector2 localPosition, bool insideStateIsNone)
        {
            var borderLayout = layout;
            var stateLayout = m_StateView.layout;

            if (insideStateIsNone)
            {
                var statePosition = this.ChangeCoordinatesTo(m_StateView.parent, localPosition);
                var inset = InnerAnchorReach;
                var insetStateRect = new Rect(stateLayout.position + Vector2.one * inset, stateLayout.size - Vector2.one * inset * 2);
                if (insetStateRect.Contains(statePosition))
                    return AnchorSide.None;
            }

            var center = borderLayout.center - borderLayout.position;
            var position = localPosition - center;
            var ratio = position.y / position.x;
            // The (visible) state layout ratio differs from the (invisible) border layout ratio.
            // To avoid visual confusion, use the state layout ratio to determine the anchor side.
            var ratioTrigger = stateLayout.height / stateLayout.width;

            if (Math.Abs(ratio) > ratioTrigger)
                return position.y > 0 ? AnchorSide.Bottom : AnchorSide.Top;

            return position.x > 0 ? AnchorSide.Right : AnchorSide.Left;
        }

        /// <summary>
        /// Shows or hides the connector dot for a wire.
        /// </summary>
        /// <param name="wireUI">The wire.</param>
        public void ShowConnectorOnWire(AbstractWire wireUI)
        {
            var wireModel = wireUI.WireModel;

            Vector2? point = null;
            if (wireModel.FromNodeGuid == m_StateView.Model.Guid)
            {
                point = wireUI.ChangeCoordinatesTo(this, wireUI.GetFrom());
            }
            else if (wireModel.ToNodeGuid == m_StateView.Model.Guid)
            {
                point = wireUI.ChangeCoordinatesTo(this, wireUI.GetTo());
            }
            else if (wireModel is AbstractGhostTransitionSupportModel ghostTransitionSupportModel)
            {
                if (wireModel.FromNodeGuid == default)
                {
                    point = this.WorldToLocal(ghostTransitionSupportModel.FromWorldPoint);
                }
                else if (wireModel.ToNodeGuid == default)
                {
                    point = this.WorldToLocal(ghostTransitionSupportModel.ToWorldPoint);
                }
            }

            if (point != null)
            {
                SetConnectorPosition(point.Value);
                ShowConnector(wireUI.IsSelected() || wireUI.hasHoverPseudoState);
            }
            else
            {
                HideConnector();
            }
        }

        /// <summary>
        /// Shows the connector dot at the point of the border nearest a position, wherever that position
        /// is on the state. Used to preview where a transition would start while the create transition
        /// modifier is held.
        /// </summary>
        /// <param name="worldPosition">The position to snap the connector dot to, in world space.</param>
        public void ShowAnchorPreview(Vector2 worldPosition)
        {
            MouseAcquiresConnector();

            var (point, anchorSide, _) = SnapPointToBorder(this.WorldToLocal(worldPosition), false);
            if (anchorSide == AnchorSide.None)
            {
                RestoreConnector();
                return;
            }

            SetConnectorPosition(point);
            ShowConnector(false);
        }

        /// <summary>
        /// Ends a preview started by <see cref="ShowAnchorPreview"/>.
        /// </summary>
        /// <remarks>
        /// If the pointer is still over the border the connector goes back to following it, since the
        /// border drives the connector on its own there. Otherwise the connector is put back the way it
        /// was before the preview moved it.
        /// </remarks>
        /// <param name="worldPosition">The current pointer position, in world space.</param>
        public void HideAnchorPreview(Vector2 worldPosition)
        {
            if (ContainsPoint(this.WorldToLocal(worldPosition)))
                DisplayConnectorNear(worldPosition);
            else
                MouseReleasesConnector();
        }

        void DisplayConnectorUnderMouse(IMouseEvent evt)
        {
            DisplayConnectorNear(evt.mousePosition);
        }

        // Snaps the connector dot to the border near a world position, hiding it for a position that is
        // inside the state rather than in the band along its edge.
        void DisplayConnectorNear(Vector2 worldPosition)
        {
            var (circlePosition, anchorSide, _) = SnapPointToBorder(this.WorldToLocal(worldPosition), true);

            if (anchorSide != AnchorSide.None)
            {
                SetConnectorPosition(circlePosition);
                ShowConnector(false);
            }
            else
            {
                RestoreConnector();
            }
        }

        void ShowConnector(bool manipulationMode)
        {
            if (manipulationMode)
            {
                this.ReplaceAndCacheClassName(manipulationModeUssClassName, ref m_VisibilityUssClassCache);
            }
            else
            {
                this.ReplaceAndCacheClassName(null, ref m_VisibilityUssClassCache);
            }
        }

        /// <summary>
        /// Hide the connector dot.
        /// </summary>
        public void HideConnector()
        {
            this.ReplaceAndCacheClassName(connectorHiddenUssClassName, ref m_VisibilityUssClassCache);
        }

        Vector2 m_SavedConnectorPosition;
        bool m_SavedConnectorIsShown;
        bool m_SavedConnectorManipulationMode;

        void MouseAcquiresConnector()
        {
            if (!m_IsBorderHovered)
            {
                m_IsBorderHovered = true;

                m_SavedConnectorIsShown = m_VisibilityUssClassCache != connectorHiddenUssClassName;

                if (m_SavedConnectorIsShown)
                {
                    var connectorRect = m_Connector.layout;
                    m_SavedConnectorPosition = new Vector2(connectorRect.xMin + k_CircleDiameter / 2, connectorRect.yMin + k_CircleDiameter / 2);
                    m_SavedConnectorManipulationMode = m_VisibilityUssClassCache == manipulationModeUssClassName;
                }
            }
        }

        void MouseUpdateConnector(IMouseEvent evt)
        {
            if (m_IsBorderHovered)
            {
                // Snap the connector dot if it's hovering over a transition endpoint, otherwise place it at mouse position
                if (GetTransitionUnderMouse(evt.mousePosition, out var transition, true))
                {
                    var isSingleStateTransition = (transition.Model as TransitionSupportModel)?.IsSelfTransition ?? false;
                    if (isSingleStateTransition)
                    {
                        DisplayConnectorUnderMouse(evt);
                    }
                    else
                    {
                        ShowConnectorOnWire(transition as AbstractWire);
                    }
                }
                else
                {
                    DisplayConnectorUnderMouse(evt);
                }
            }
        }

        void MouseReleasesConnector()
        {
            if (m_IsBorderHovered)
            {
                m_IsBorderHovered = false;
                RestoreConnector();
            }
        }

        void RestoreConnector()
        {
            if (m_SavedConnectorIsShown)
            {
                SetConnectorPosition(m_SavedConnectorPosition);
                ShowConnector(m_SavedConnectorManipulationMode);
            }
            else
            {
                HideConnector();
            }
        }


        void OnMouseEnter(MouseEnterEvent evt)
        {
            if (evt.target != this)
                return;

            // Do not update the connector dot position if left mouse is held down
            if ((evt.pressedButtons & 1 << ((int)MouseButton.LeftMouse)) == 0)
            {
                MouseAcquiresConnector();
                MouseUpdateConnector(evt);
            }
        }

        void OnMouseLeave(MouseLeaveEvent evt)
        {
            if (evt.target != this)
                return;

            // Do not update the connector dot position if left mouse is held down
            if ((evt.pressedButtons & 1 << ((int)MouseButton.LeftMouse)) == 0)
            {
                MouseReleasesConnector();
            }
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            // Do not update the connector dot position if left mouse is held down
            if ((evt.pressedButtons & 1 << ((int)MouseButton.LeftMouse)) == 0)
            {
                MouseUpdateConnector(evt);
            }
        }

        /// <summary>
        /// Returns information on whether or not a transition connected to this state is being moused over.
        /// </summary>
        /// <param name="mousePosition">The world mouse position.</param>
        /// <param name="transition">The transition being hovered over.</param>
        /// <param name="prioritizeSelection">Should a transition that is selected be prioritized over non-selected transitions.</param>
        /// <returns>True if there is a connected state-to-state transition at the mouse position.</returns>
        bool GetTransitionUnderMouse(Vector2 mousePosition, out GraphElement transition, bool prioritizeSelection = false)
        {
            var stateModel = m_StateView?.Model as AbstractNodeModel;

            transition = null;

            if (stateModel == null)
                return false;

            foreach (var wireModel in stateModel.GetConnectedWires())
            {
                var transitionUI = wireModel.GetView<GraphElement>(GraphView);
                if (transitionUI == null)
                    continue;
                if (transitionUI.ContainsPoint(transitionUI.WorldToLocal(mousePosition)))
                {
                    transition = transitionUI;

                    if (!prioritizeSelection || transitionUI.IsSelected())
                        return true;
                }
            }

            return transition != null;
        }
    }
}
