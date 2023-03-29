// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base UI class for models displayed in a <see cref="GraphView"/>.
    /// </summary>
    abstract class GraphElement : ModelView
    {
        static readonly CustomStyleProperty<int> k_LayerProperty = new CustomStyleProperty<int>("--layer");
        static readonly CustomStyleProperty<Color> k_MinimapColorProperty = new CustomStyleProperty<Color>("--minimap-color");

        static Color DefaultMinimapColor
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    return new Color(230/255f, 230/255f, 230/255f, 0.5f);
                }

                return new Color(200/255f, 200/255f, 200/255f, 1f);
            }
        }

        public static readonly string ussClassName = "ge-graph-element";
        public static readonly string selectableModifierUssClassName = ussClassName.WithUssModifier("selectable");
        public static readonly string contentContainerElementName = "content-container";

        int m_Layer;

        bool m_LayerIsInline;

        ClickSelector m_ClickSelector;

        /// <summary>
        /// The container for the content of the <see cref="GraphElement"/>, if any.
        /// </summary>
        VisualElement m_ContentContainer;

        public GraphElementModel GraphElementModel => Model as GraphElementModel;

        public GraphView GraphView => RootView as GraphView;

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

        /// <summary>
        /// The container for the content of the <see cref="GraphElement"/>.
        /// </summary>
        public override VisualElement contentContainer => m_ContentContainer ?? this;

        /// The <see cref="DynamicBorder"/> used to display selection, hover and highlight. Can be null.
        public DynamicBorder Border { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphElement"/> class.
        /// </summary>
        protected GraphElement(bool hasContentContainer = false)
        {
            RegisterCallback<KeyDownEvent>(OnRenameKeyDown_Internal);
            focusable = true;

            ContextualMenuManipulator = new GraphViewContextualMenuManipulator(BuildContextualMenu);

            if (hasContentContainer)
            {
                m_ContentContainer = new VisualElement { name = contentContainerElementName, pickingMode = PickingMode.Ignore };
                m_ContentContainer.AddToClassList(ussClassName.WithUssElement(contentContainerElementName));
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
                GraphView?.ChangeLayer_Internal(this);
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
            this.AddStylesheet_Internal("GraphElement.uss");
        }

        /// <summary>
        /// Creates a <see cref="DynamicBorder"/> for this graph element.
        /// </summary>
        /// <returns>A <see cref="DynamicBorder"/> for this graph element.</returns>
        protected virtual DynamicBorder CreateDynamicBorder()
        {
            return new DynamicBorder(this);
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
        protected override void UpdateElementFromModel()
        {
            if (GraphElementModel?.IsSelectable() ?? false)
                ClickSelector ??= CreateClickSelector();
            else
                ClickSelector = null;

            EnableInClassList(selectableModifierUssClassName, ClickSelector != null);

            if (IsSelected())
            {
                pseudoStates |= PseudoStates.Checked;
            }
            else
            {
                pseudoStates &= ~PseudoStates.Checked;
            }

            if (Border != null)
                Border.Selected = IsSelected();
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

            UpdateLayer(prevLayer);
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

        /// <summary>
        /// Checks if the underlying graph element model should be highlighted.
        /// </summary>
        /// <remarks>Highlight is the feedback when multiple instances stand out. e.g. variable declarations.</remarks>
        /// <returns><c>true</c> if the model should be highlighted, <c>false</c> otherwise.</returns>
        public bool ShouldBeHighlighted()
        {
            if (GraphElementModel == null
                || GraphView.GraphViewModel?.SelectionState == null
                || GraphView.GraphViewModel.SelectionState.IsSelected(GraphElementModel))
                return false;

            var declarationModel = (GraphElementModel as IHasDeclarationModel)?.DeclarationModel;
            return declarationModel != null && GraphView.GraphTool.HighlighterState.GetDeclarationModelHighlighted(declarationModel);
        }

        /// <summary>
        /// Callback for the KeyDownEvent to handle renames.
        /// </summary>
        /// <param name="e">The event.</param>
        protected internal void OnRenameKeyDown_Internal(KeyDownEvent e)
        {
            if (IsRenameKey(e))
            {
                if (GraphElementModel.IsRenamable())
                {
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
    }
}
