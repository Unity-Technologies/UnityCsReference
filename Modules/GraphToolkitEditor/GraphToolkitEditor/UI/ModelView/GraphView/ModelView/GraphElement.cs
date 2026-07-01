// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.InternalBridge;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base UI class for models displayed in a <see cref="GraphView"/>.
    /// </summary>
    [UnityRestricted]
    internal abstract class GraphElement : ModelView
    {
        /// <summary>
        /// The name of the disabled overlay element.
        /// </summary>
        public static readonly string disabledOverlayElementName = "disabled-overlay";

        //** USS class names **//

        /// <summary>
        /// The USS class name added to a <see cref="GraphElement"/>.
        /// </summary>
        public static readonly string ussClassName = "ge-graph-element";

        /// <summary>
        /// The USS class name added to a <see cref="GraphElement"/> that can be selected.
        /// </summary>
        public static readonly string selectableUssClassName = ussClassName.WithUssModifier(GraphElementHelper.selectableUssModifier);

        /// <summary>
        /// The USS class name added to a <see cref="GraphElement"/> that is hidden.
        /// </summary>
        public static readonly string hiddenUssClassName = ussClassName.WithUssModifier(GraphElementHelper.hiddenUssModifier);

        /// <summary>
        /// The USS class name added to a <see cref="GraphElement"/> that is visible.
        /// </summary>
        public static readonly string visibleUssClassName = ussClassName.WithUssModifier(GraphElementHelper.visibleUssModifier);

        /// <summary>
        /// The USS class name added to a <see cref="GraphElement"/> when the graph view is zoomed out to the small level.
        /// </summary>
        public static readonly string ussSmallUssClassName = ussClassName.WithUssModifier(GraphElementHelper.smallUssModifier);

        /// <summary>
        /// The USS class name added to a <see cref="GraphElement"/> when the graph view is zoomed out to the very small level.
        /// </summary>
        public static readonly string ussVerySmallUssClassName = ussClassName.WithUssModifier(GraphElementHelper.verySmallUssModifier);

        static readonly CustomStyleProperty<int> k_LayerProperty = new CustomStyleProperty<int>("--layer");
        static readonly CustomStyleProperty<Color> k_MinimapColorProperty = new CustomStyleProperty<Color>("--minimap-color");

        static Color DefaultMinimapColor
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    return new Color(230 / 255f, 230 / 255f, 230 / 255f, 0.5f);
                }

                return new Color(200 / 255f, 200 / 255f, 200 / 255f, 1f);
            }
        }

        int m_Layer;

        bool m_LayerIsInline;

        ClickSelector m_ClickSelector;

        /// <summary>
        /// The container for the content of the <see cref="GraphElement"/>, if any.
        /// </summary>
        VisualElement m_ContentContainer;

        /// <summary>
        /// The model associated with the <see cref="GraphElement"/>.
        /// </summary>
        public GraphElementModel GraphElementModel => Model as GraphElementModel;

        /// <summary>
        /// The graph view that contains the <see cref="GraphElement"/>.
        /// </summary>
        public GraphView GraphView => RootView as GraphView;

        /// <summary>
        /// The layer of the <see cref="GraphElement"/>.
        /// </summary>
        /// <remarks>Layers guarantee that certain types of graph elements are always rendered above others.</remarks>
        public int Layer
        {
            get => m_Layer;
            set
            {
                m_LayerIsInline = true;
                m_Layer = value;
            }
        }

        public Color MinimapColor { get; protected set; } = DefaultMinimapColor;

        public virtual bool ShowInMiniMap { get; set; } = true;

        protected ClickSelector ClickSelector
        {
            get => m_ClickSelector;
            set => this.ReplaceManipulator(ref m_ClickSelector, value);
        }

        internal virtual VisualElement SizeElement => this;

        enum CullingStateTransition
        {
            None,
            Enable,
            Disable
        }

        protected List<GraphViewCullingSource> m_ActiveCullingSources;
        CullingStateTransition m_DelayedStateTransition;
        StyleLength m_CulledWidth = StyleKeyword.Null;
        StyleLength m_CulledHeight = StyleKeyword.Null;
        bool m_WasCulled; // if IsReadyForCulling is false, it can happen that IsCulled is true but the element is not culled yet.

        /// <summary>
        /// The list of <see cref="GraphViewCullingSource"/> that are currently active.
        /// </summary>
        public IReadOnlyList<GraphViewCullingSource> ActiveCullingSources => m_ActiveCullingSources;

        /// <summary>
        /// The container for the content of the <see cref="GraphElement"/>.
        /// </summary>
        public override VisualElement contentContainer => m_ContentContainer ?? this;

        /// <summary>
        /// The <see cref="DynamicBorder"/> used to display selection, hover and highlight. Can be null.
        /// </summary>
        public DynamicBorder Border { get; private set; }

        /// <summary>
        /// Prevent culling from acting on this <see cref="GraphElement"/>.
        /// </summary>
        public bool PreventCulling { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphElement"/> class.
        /// </summary>
        protected GraphElement(bool hasContentContainer = false)
        {
            RegisterCallback<KeyDownEvent>(OnRenameKeyDown);
            focusable = true;

            ContextualMenuManipulator = new GraphViewContextualMenuManipulator(BuildContextualMenu);

            if (hasContentContainer)
            {
                m_ContentContainer = new VisualElement { name = GraphElementHelper.contentContainerName, pickingMode = PickingMode.Ignore };
                m_ContentContainer.AddToClassList(ussClassName.WithUssElement(GraphElementHelper.contentContainerName));
                hierarchy.Add(m_ContentContainer);
            }
        }

        public void ResetLayer()
        {
            int prevLayer = m_Layer;
            m_Layer = 0;
            m_LayerIsInline = false;
            customStyle.TryGetValue(k_LayerProperty, out m_Layer);
            UpdateLayer(prevLayer);
        }

        void UpdateLayer(int prevLayer)
        {
            if (prevLayer != m_Layer)
                GraphView?.ChangeLayer(this);
        }

        /// <inheritdoc />
        protected override void BuildUI()
        {
            base.BuildUI();
            m_DelayedStateTransition = CullingStateTransition.None;
            m_ActiveCullingSources?.Clear();
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            // added here because it should be added last.
            Border = CreateDynamicBorder();
            if (Border != null)
            {
                Border.AddToClassList(ussClassName.WithUssElement("dynamic-border"));
                hierarchy.Add(Border);
            }

            AddToClassList(ussClassName);
            this.AddPackageStylesheet("GraphElement.uss");
        }

        /// <summary>
        /// Creates a <see cref="DynamicBorder"/> for this graph element.
        /// </summary>
        /// <returns>A <see cref="DynamicBorder"/> for this graph element.</returns>
        protected virtual DynamicBorder CreateDynamicBorder()
        {
            return new ExternalDynamicBorder(this);
        }

        /// <summary>
        /// Creates a <see cref="ClickSelector" /> for this element.
        /// </summary>
        /// <returns>A <see cref="ClickSelector" /> for this element.</returns>
        protected virtual ClickSelector CreateClickSelector()
        {
            return new GraphViewClickSelector();
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            DisableCullingForFrame();

            if (GraphElementModel?.IsSelectable() ?? false)
                ClickSelector ??= CreateClickSelector();
            else
                ClickSelector = null;

            EnableInClassList(selectableUssClassName, ClickSelector != null);
        }

        /// <inheritdoc />
        public override void UpdateUISelection(UpdateSelectionVisitor visitor)
        {
            UpdateSelectionVisuals(IsSelected());
        }

        /// <summary>
        /// Update the element so that it looks selected if <paramref name="selected"/> is true, or unselected otherwise.
        /// This will be called by UpdateElementSelection, but might also be called for displaying a selection state without changing the selection.
        /// </summary>
        /// <param name="selected">If the element should look selected.</param>
        /// <seealso cref="FreehandSelector"/>
        /// <seealso cref="RectangleSelector"/>
        public virtual void UpdateSelectionVisuals(bool selected)
        {
            SetCheckedPseudoState(selected);

            if (Border != null)
            {
                Border.Selected = selected;

                // If the element got unselected, it might need to become highlighted.
                // (For example, if the element is a portal and the new selection is its associated portal).
                Border.Highlighted = !selected && ShouldBeHighlighted();
            }
        }

        /// <summary>
        /// Set the visual appearance of the <see cref="GraphElement"/> and its parts depending on the current zoom.
        /// </summary>
        /// <param name="zoom">The current zoom.</param>
        /// <param name="newZoomMode">The <see cref="GraphViewZoomMode"/> that will be active from now.</param>
        /// <param name="oldZoomMode">The <see cref="GraphViewZoomMode"/> that was active before this call.</param>
        public void SetLevelOfDetail(float zoom, GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode)
        {
            SetElementLevelOfDetail(zoom, newZoomMode, oldZoomMode);
            for (var i = 0; i < PartList.Parts.Count; i++)
            {
                var component = PartList.Parts[i];
                (component as GraphElementPart)?.SetLevelOfDetail(zoom, newZoomMode, oldZoomMode);
            }

            if (Border != null)
                Border.Zoom = zoom;
        }

        /// <summary>
        /// Can be overriden to set the visual appearance of the <see cref="GraphElement"/> depending on the current zoom.
        /// </summary>
        /// <param name="zoom">The current zoom.</param>
        /// <param name="newZoomMode">The <see cref="GraphViewZoomMode"/> that will be active from now.</param>
        /// <param name="oldZoomMode">The <see cref="GraphViewZoomMode"/> that was active before this call.</param>
        public virtual void SetElementLevelOfDetail(float zoom, GraphViewZoomMode newZoomMode, GraphViewZoomMode oldZoomMode)
        {
        }

        /// <inheritdoc />
        protected override void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            base.OnCustomStyleResolved(evt);

            if (evt.customStyle.TryGetValue(k_MinimapColorProperty, out var minimapColor))
                MinimapColor = minimapColor;

            int prevLayer = m_Layer;
            if (!m_LayerIsInline)
                evt.customStyle.TryGetValue(k_LayerProperty, out m_Layer);

            schedule.Execute(() => UpdateLayer(prevLayer)).ExecuteLater(0);
        }

        public virtual bool IsMovable()
        {
            return GraphElementModel?.IsMovable() ?? false;
        }

        /// <summary>
        /// Sets the position of the element. This method has no effect if <see cref="ModelView.PositionIsOverriddenByManipulator"/> is set.
        /// </summary>
        /// <param name="position">The position.</param>
        public void SetPosition(Vector2 position)
        {
            if (!PositionIsOverriddenByManipulator)
            {
                SetPositionOverride(position);
            }
        }

        /// <summary>
        /// Unconditionally sets the position of the element.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <remarks>Use this method when the position from the model needs to be overriden during a manipulation.</remarks>
        public virtual void SetPositionOverride(Vector2 position)
        {
            style.left = position.x;
            style.top = position.y;
        }

        /// <summary>
        /// Checks if the underlying graph element model is selected.
        /// </summary>
        /// <returns><c>true</c> if the model is selected, <c>false</c> otherwise.</returns>
        public bool IsSelected()
        {
            return GraphView?.GraphViewModel?.SelectionState?.IsSelected(GraphElementModel) ?? false;
        }

        bool m_OverrideHighlighed;

        public bool OverrideHighlighted
        {
            get => m_OverrideHighlighed;
            set
            {
                if (m_OverrideHighlighed != value)
                {
                    m_OverrideHighlighed = value;
                    if (Border != null)
                        Border.Highlighted = ShouldBeHighlighted();
                }
            }
        }

        /// <summary>
        /// Checks if the underlying graph element model should be highlighted.
        /// </summary>
        /// <remarks>Highlight is the feedback when multiple instances stand out. e.g. variable declarations.</remarks>
        /// <returns><c>true</c> if the model should be highlighted, <c>false</c> otherwise.</returns>
        public bool ShouldBeHighlighted()
        {
            if (OverrideHighlighted)
                return true;

            if (GraphView == null) // This GraphElement has been removed from its view
                return false;

            if (GraphElementModel == null
                || GraphView.GraphViewModel?.SelectionState == null
                || GraphView.GraphViewModel.SelectionState.IsSelected(GraphElementModel))
                return false;

            var declarationModel = (GraphElementModel as IHasDeclarationModel)?.DeclarationModel;
            return declarationModel != null && (GraphView.GraphTool?.HighlighterState?.GetDeclarationModelHighlighted(declarationModel) ?? false);
        }

        /// <summary>
        /// Callback for the KeyDownEvent to handle renames.
        /// </summary>
        /// <param name="e">The event.</param>
        protected internal void OnRenameKeyDown(KeyDownEvent e)
        {
            if (IsRenameKey(e))
            {
                if (GraphElementModel.IsRenamable())
                {
                    // Disable culling entirely when renaming
                    ClearCulling();

                    if (!hierarchy.parent.ChangeCoordinatesTo(GraphView, layout).Overlaps(GraphView.layout))
                    {
                        GraphView.DispatchFrameAndSelectElementsCommand(false, this);
                    }

                    if (Rename())
                    {
                        e.StopPropagation();
                    }
                }
            }
        }

        /// <summary>
        /// Refresh the borders of this <see cref="GraphElement"/>
        /// </summary>
        public virtual void RefreshBorder()
        {
            Border.MarkDirtyRepaint();
        }

        protected override void OnGeometryChanged(GeometryChangedEvent evt)
        {
            base.OnGeometryChanged(evt);
            UpdateGraphViewPartitioning();
            HandleDelayedCullingState();
        }

        /// <summary>
        /// Checks if a <see cref="GraphElement"/> can be space partitioned.
        /// </summary>
        /// <returns>True if it can be partitioned, false otherwise.</returns>
        public virtual bool CanBePartitioned()
        {
            // Only root level models are partitioned.
            return GraphElementModel != null && GraphElementModel.Container == GraphElementModel.GraphModel;
        }

        /// <summary>
        /// Marks this <see cref="GraphElement"/> to be partitioned or repartitioned in the <see cref="GraphView"/>'s <see cref="SpacePartitioningStateComponent"/>.
        /// This should be overriden if the <see cref="GraphElement"/> doesn't have a <see cref="GraphElementModel"/> but still needs to be partitioned.
        /// </summary>
        protected virtual void UpdateGraphViewPartitioning()
        {
            if (!CanBePartitioned())
                return;

            if (GraphView is { GraphViewModel: { SpacePartitioningState: { } } })
            {
                using var updater = GraphView.GraphViewModel.SpacePartitioningState.UpdateScope;
                updater.MarkGraphElementForPartitioning(this);
            }
        }

        /// <summary>
        /// Gets the actual bounding box used for overlapping tests. If you override <see cref="VisualElement.Overlaps"/>, you should override this
        /// to keep it in sync.
        /// </summary>
        /// <returns>The bounding box of the <see cref="GraphElement"/>.</returns>
        public virtual Rect GetBoundingBox()
        {
            return layout;
        }

        /// <summary>
        /// Sets the culling state of this <see cref="GraphElement"/> for a given <see cref="GraphViewCullingSource"/>.
        /// If culling is not supported for the given source, this method does nothing.
        /// </summary>
        /// <param name="state">The new culling state.</param>
        /// <param name="cullingSource">The culling source.</param>
        /// <returns>Whether the actual culling state changed because of this operation.</returns>
        internal bool SetCullingState(GraphViewCullingState state, GraphViewCullingSource cullingSource)
        {
            using var pooledList = ListPool<GraphViewCullingSource>.Get(out var list);
            list.Add(cullingSource);
            return SetCullingState(state, list);
        }

        /// <summary>
        /// Sets the culling state of this <see cref="GraphElement"/> for a given list of <see cref="GraphViewCullingSource"/>.
        /// If culling is not supported for a given source, it is discarded.
        /// </summary>
        /// <param name="state">The new culling state.</param>
        /// <param name="cullingSources">The list of culling sources.</param>
        /// <returns>Whether the actual culling state changed because of this operation.</returns>
        internal bool SetCullingState(GraphViewCullingState state, IReadOnlyList<GraphViewCullingSource> cullingSources)
        {
            var oldState = IsCulled();
            for (var i = 0; i < cullingSources.Count; ++i)
            {
                var cullingSource = cullingSources[i];
                if (SupportsCulling(cullingSource))
                {
                    m_ActiveCullingSources ??= new List<GraphViewCullingSource>();
                    m_ActiveCullingSources.SetCullingSourceState(state, cullingSource);
                }
            }

            var newState = IsCulled();
            return newState != oldState;
        }
        internal void UpdateCulling()
        {
            var transition = IsCulled() ? CullingStateTransition.Enable : CullingStateTransition.Disable;
            HandleCullingTransition(transition);
        }

        /// <summary>
        /// Clears the culling state of all active culling sources.
        /// </summary>
        public virtual void ClearCulling()
        {
            if (!IsCulled())
                return;

            m_ActiveCullingSources.Clear();
            HandleCullingTransition(CullingStateTransition.Disable);
        }

        /// <summary>
        /// Checks if this <see cref="GraphElement"/> supports culling for a given <see cref="GraphViewCullingSource"/>.
        /// </summary>
        /// <param name="cullingSource">The culling source.</param>
        /// <returns>True if culling is supported, false otherwise.</returns>
        public virtual bool SupportsCulling(GraphViewCullingSource cullingSource) => false;

        /// <summary>
        /// Checks if this <see cref="GraphElement"/> is currently culled, from any <see cref="GraphViewCullingSource"/>.
        /// </summary>
        /// <returns>True if it is culled, false otherwise.</returns>
        public virtual bool IsCulled()
        {
            return m_ActiveCullingSources is { Count: > 0 };
        }

        /// <summary>
        /// Checks if this <see cref="GraphElement"/> is currently culled from a given <see cref="GraphViewCullingSource"/>.
        /// </summary>
        /// <param name="cullingSource">The culling source.</param>
        /// <returns>True if it is culled, false otherwise.</returns>
        public virtual bool IsCulled(GraphViewCullingSource cullingSource)
        {
            return m_ActiveCullingSources is { Count: > 0 } && m_ActiveCullingSources.Contains(cullingSource);
        }

        /// <summary>
        /// Disables culling for this <see cref="GraphElement"/> for the current frame.
        /// </summary>
        /// <remarks>This method is currently broken, and will not reactivate culling after a frame. Culling will be
        /// reactivated normally by the culling sources.</remarks>
        public void DisableCullingForFrame()
        {
            // TODO: GTF-1278
            // This causes many issues, therefore we do not reactivate culling when the model is updated.
            // Culling will still be reactivated normally by the culling sources, and even automatically if
            // the update triggers a geometry change that itself triggers a repartitioning of the GraphElement.
            if (!IsCulled() || GraphView?.GraphViewModel?.GraphViewCullingState == null)
                return;

            // We need to execute the MarkGraphElementAsCulled later in order to avoid issues
            // with forward dependencies needing the full GraphElement and its parts to update properly.
            // var oldActiveCullingSources = ListPool<GraphViewCullingSource>.Get();
            // for (var i = 0; i < ActiveCullingSources.Count; ++i)
            //     oldActiveCullingSources.Add(ActiveCullingSources[i]);

            ClearCulling();

            // schedule.Execute(() =>
            // {
            //     // By the time this is executed, in might no longer make sense to cull the GraphElement.
            //     // Make sure to verify if the conditions are still valid for culling.
            //     using var updater = GraphView.GraphViewModel.GraphViewCullingState.UpdateScope;
            //     for (var i = 0; i < oldActiveCullingSources.Count; ++i)
            //     {
            //         var cullingSource = oldActiveCullingSources[i];
            //         if (GraphView.ShouldGraphElementBeCulled(this, cullingSource))
            //             updater.MarkGraphElementAsCulled(this, cullingSource);
            //     }
            //     ListPool<GraphViewCullingSource>.Release(oldActiveCullingSources);
            // }).ExecuteLater(0);
        }

        void HandleCullingTransition(CullingStateTransition transition)
        {
            if (!IsReadyForCulling())
            {
                if (m_DelayedStateTransition == CullingStateTransition.None)
                    m_DelayedStateTransition = transition;
                else
                {
                    if (m_DelayedStateTransition != transition)
                        CancelDelayedCullingState(); // While in flight, if there is a new transition that is opposite of the current one, cancel it.
                }

                // If we haven't set any state yet and the global CullingState is Disabled, don't make any transition.
                if (GraphView == null || GraphView.CullingState == GraphViewCullingState.Disabled)
                    CancelDelayedCullingState();
                return;
            }

            CancelDelayedCullingState();

            if (transition == CullingStateTransition.None)
            {
                return;
            }

            if (transition == CullingStateTransition.Enable)
            {
                EnableCulling();
            }
            else
            {
                DisableCulling();
            }
        }

        /// <summary>
        /// Enables culling for this <see cref="GraphElement"/>. Override this method to implement custom culling.
        /// </summary>
        protected virtual void EnableCulling()
        {
            if (m_WasCulled)
                return;
            m_WasCulled = true;
            CancelDelayedCullingState();
            var size = layout.size;
            m_CulledWidth = style.width;
            m_CulledHeight = style.height;
            style.width = size.x;
            style.height = size.y;

            for (var i = PartList.Parts.Count - 1; i >= 0; --i)
            {
                var modelViewPart = PartList.Parts[i];
                if (modelViewPart is GraphViewPart gvp)
                    gvp.SetCullingState(GraphViewCullingState.Enabled);
                else
                    modelViewPart.Root?.RemoveFromHierarchy();
            }
        }

        /// <summary>
        /// Disables culling for this <see cref="GraphElement"/>. Override this method to implement custom culling.
        /// </summary>
        protected virtual void DisableCulling()
        {
            if (!m_WasCulled)
                return;
            m_WasCulled = false;
            CancelDelayedCullingState();
            Clear();

            style.width = m_CulledWidth;
            style.height = m_CulledHeight;

            foreach (var modelViewPart in PartList.Parts)
            {
                if (modelViewPart is GraphViewPart gvp)
                    gvp.SetCullingState(GraphViewCullingState.Disabled);
                else if (modelViewPart?.Root != null)
                    Add(modelViewPart.Root);
            }
        }

        /// <summary>
        /// Checks if this <see cref="GraphElement"/> is ready for culling.
        /// </summary>
        /// <returns>True if the <see cref="GraphElement"/> can be culled, false if there is any geometry changes left to do.</returns>
        protected virtual bool IsReadyForCulling()
        {
            return !PreventCulling && !float.IsNaN(layout.width) && !float.IsNaN(layout.height);
        }

        void HandleDelayedCullingState()
        {
            if (m_DelayedStateTransition != CullingStateTransition.None)
            {
                // Keep a temp variable on the transition and set it to None before calling HandleCullingTransition.
                // Otherwise, if the element is still not ready for culling, it would set a new transition and clear it right after.
                var oldTransition = m_DelayedStateTransition;
                m_DelayedStateTransition = CullingStateTransition.None;
                HandleCullingTransition(oldTransition);
            }
        }

        void CancelDelayedCullingState()
        {
            // If we are currently waiting for a delayed transition, cancel it.
            m_DelayedStateTransition = CullingStateTransition.None;
        }
    }
}
