// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Lays out the element it is added to as a grid, draws a line between the cells that meet, and lets the user drag
    /// these lines to resize the grid's tracks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One element with this component replaces a hierarchy of nested split views: each child is a cell, placed with
    /// <c>grid-column</c> and <c>grid-row</c>. A line shows between two tracks wherever cells sit on both sides of it; a
    /// cell that spans the two tracks hides the line there. The lines are drawn in the middle of the gaps
    /// (<c>column-gap</c>, <c>row-gap</c>), under the cells, so the gaps must be at least as wide as the lines.
    /// </para>
    /// <para>
    /// The component sets <c>display: grid</c> on its owner and writes the track sizes to the owner's inline
    /// <c>grid-template-columns</c> and <c>grid-template-rows</c>. <see cref="columns"/> and <see cref="rows"/> give the
    /// initial tracks; when one is empty, the tracks come from the owner's style.
    /// </para>
    /// <para>
    /// Dragging a line resizes the two tracks on its sides and keeps the size of every other track: a length track
    /// gets the new length, and the <c>fr</c> factors are rebalanced. The drag stops where a cell would go below its
    /// <c>min-width</c>/<c>min-height</c> or above its <c>max-width</c>/<c>max-height</c>. Like a split view pane, a
    /// flexible track doesn't grow with its cells' content: its minimum is its cells' minimum size. When the owner is
    /// too small for the cells' minimum sizes, the length tracks shrink, down to these minimums, and grow back when
    /// there is room again. A track whose cells are all hidden (<c>display: none</c>) collapses, like a collapsed split
    /// view pane.
    /// </para>
    /// <para>
    /// In the Editor, the track sizes persist in the view data when the owner is a <see cref="GridViewControl"/> with a
    /// <c>view-data-key</c>: a component does not receive its owner's view data by itself.
    /// </para>
    /// </remarks>
    [VisualElementComponent, HideInInspector]
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    // The source generator declares the style override mask in its part of the struct: the field order is irrelevant.
#pragma warning disable CS0282
    internal partial struct GridViewComponent
#pragma warning restore CS0282
    {
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal const float DefaultLineWidth = 1f;
        internal const float DefaultHandleSize = 10f;

        /// <summary>
        /// The initial column tracks, in the <c>grid-template-columns</c> syntax. Empty to use the owner's style.
        /// </summary>
        [UxmlAttribute] public string columns;

        /// <summary>
        /// The initial row tracks, in the <c>grid-template-rows</c> syntax. Empty to use the owner's style.
        /// </summary>
        [UxmlAttribute] public string rows;

        [StyleProperty("--unity-grid-view-line-color")] public Color lineColor;

        // The color of the line under the pointer, and of the line being dragged.
        [StyleProperty("--unity-grid-view-active-line-color")] public Color activeLineColor;

        [StyleProperty("--unity-grid-view-line-width")] public float lineWidth;

        // The width of the band around a line where the pointer grabs it. It can reach over the cells.
        [StyleProperty("--unity-grid-view-handle-size")] public float handleSize;

        internal GridViewComponentState state;

        [UxmlCreateInstanceMethod]
        internal static GridViewComponent CreateDefault() => new GridViewComponent
        {
            lineColor = new Color(0f, 0f, 0f, 0.35f),
            activeLineColor = Color.white,
            lineWidth = DefaultLineWidth,
            handleSize = DefaultHandleSize,
        };

        /// <summary>
        /// A component with the default line style, to add with <see cref="VisualElement.AddComponent{T}"/>.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static GridViewComponent Create(string columns, string rows)
        {
            var gridView = CreateDefault();
            gridView.columns = columns;
            gridView.rows = rows;
            return gridView;
        }

        [OnComponentAdded]
        static void OnAdded(ref GridViewComponent self, VisualElement owner)
        {
            // A new state every time: a component copied from another element must not share its drag and tracks.
            self.state = new GridViewComponentState(owner);
            self.state.Attach();
        }

        [OnComponentRemoved]
        static void OnRemoved(ref GridViewComponent self, VisualElement owner)
        {
            self.state?.Detach();
            self.state = null;
        }

        [OnComponentChanged]
        static void OnChanged(ref GridViewComponent self, VisualElement owner) => self.state?.Refresh();

        [RegisterCallback(typeof(GeometryChangedEvent))]
        static void OnGeometryChanged(ref GridViewComponent self, VisualElement owner, GeometryChangedEvent evt)
            => self.state?.Refresh();

        // Before the first layout: the tracks a UXML document authored are only known once the element is created.
        [RegisterCallback(typeof(AttachToPanelEvent))]
        static void OnAttachToPanel(ref GridViewComponent self, VisualElement owner, AttachToPanelEvent evt)
            => self.state?.Refresh();

        [RegisterCallback(typeof(DetachFromPanelEvent))]
        static void OnDetachFromPanel(ref GridViewComponent self, VisualElement owner, DetachFromPanelEvent evt)
            => self.state?.CancelInteraction();

        // Trickle-down: the pointer grabs a line before the cell under it sees the event.
        [RegisterCallback(typeof(PointerDownEvent), CallbackOptions.TrickleDown)]
        static void OnPointerDown(ref GridViewComponent self, VisualElement owner, PointerDownEvent evt)
            => self.state?.OnPointerDown(evt, self.handleSize);

        [RegisterCallback(typeof(PointerMoveEvent), CallbackOptions.TrickleDown)]
        static void OnPointerMove(ref GridViewComponent self, VisualElement owner, PointerMoveEvent evt)
            => self.state?.OnPointerMove(evt, self.handleSize);

        [RegisterCallback(typeof(PointerUpEvent), CallbackOptions.TrickleDown)]
        static void OnPointerUp(ref GridViewComponent self, VisualElement owner, PointerUpEvent evt)
            => self.state?.OnPointerUp(evt, self.handleSize);

        [RegisterCallback(typeof(PointerCaptureOutEvent))]
        static void OnPointerCaptureOut(ref GridViewComponent self, VisualElement owner, PointerCaptureOutEvent evt)
            => self.state?.EndDrag();

        // Trickle-down: the pointer can enter a cell under the grab band.
        [RegisterCallback(typeof(MouseOverEvent), CallbackOptions.TrickleDown)]
        static void OnMouseOver(ref GridViewComponent self, VisualElement owner, MouseOverEvent evt)
            => self.state?.RestoreCursor();

        [RegisterCallback(typeof(MouseCaptureOutEvent))]
        static void OnMouseCaptureOut(ref GridViewComponent self, VisualElement owner, MouseCaptureOutEvent evt)
            => self.state?.RestoreCursor();

        [RegisterCallback(typeof(PointerLeaveEvent))]
        static void OnPointerLeave(ref GridViewComponent self, VisualElement owner, PointerLeaveEvent evt)
            => self.state?.ClearHover();

        [GenerateVisualContent]
        static void Paint(ref GridViewComponent self, MeshGenerationContext mgc)
            => self.state?.Paint(mgc, self.lineColor, self.activeLineColor, self.lineWidth);

        // Called by GridViewControl once its view data is available.
        internal static void OnViewDataReady(VisualElement owner)
        {
            ref var self = ref owner.GetComponent<GridViewComponent>();
            self.state?.LoadViewData();
        }
    }
}
