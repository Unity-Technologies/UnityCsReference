// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The generated view for a <see cref="State"/>.
    /// </summary>
    /// <remarks>
    /// Passed to <see cref="StateView{T}"/> as its <see cref="StateView{T}.View"/>. Add custom UI to
    /// <see cref="Root"/>. Its contents are cleared each time the state returns from being culled;
    /// cache any custom UI you allocated in <see cref="StateView{T}.OnViewBuilt"/> and re-add it from
    /// <see cref="StateView{T}.OnCullingChanged"/> when <c>cullingEnabled</c> is <c>false</c>.
    /// </remarks>
    public interface IStateView
    {
        /// <summary>
        /// The root <see cref="VisualElement"/> of the state, to which custom UI can be added.
        /// </summary>
        /// <remarks>
        /// The contents of <c>Root</c> are cleared each time the state returns from being culled. Cache
        /// any custom UI you allocated in <see cref="StateView{T}.OnViewBuilt"/> and re-add it from
        /// <see cref="StateView{T}.OnCullingChanged"/> when <c>cullingEnabled</c> is false.
        /// </remarks>
        public VisualElement Root { get; }
    }

    /// <summary>
    /// Class for a state node UI.
    /// </summary>
    [UnityRestricted]
    internal class StateView : NodeView, INodeWithConnector, IStateView, IAccentAnimatableView
    {
        /// <summary>
        /// The name of the <see cref="ModelViewPart"/> for the accent color line.
        /// </summary>
        public static readonly string colorLinePartName = "color-line-container";

        /// <summary>
        /// The USS class name of a <see cref="StateView"/>.
        /// </summary>
        public new static readonly string ussClassName = "ge-state";

        TransitionConnector m_TransitionConnector;
        bool m_AnchorPreviewShown;

        /// <summary>
        /// The state model.
        /// </summary>
        public StateModel StateModel => Model as StateModel;

        /// <inheritdoc />
        protected override IUserModelView BuildUserView()
        {
            if (NodeModel is Implementation.UserStateModelImp userStateModel)
                return GraphView.StateBuilderLookup.Build(userStateModel.Node, this);

            return null;
        }

        /// <summary>
        /// The transition connector manipulator.
        /// </summary>
        public TransitionConnector TransitionConnector
        {
            get => m_TransitionConnector;
            protected set
            {
                m_TransitionConnector = value;
                this.ReplaceManipulator(ref m_TransitionConnector, value);
            }
        }

        /// <summary>
        /// Computes the position of the originating anchor point for a transition.
        /// </summary>
        /// <param name="transitionModel">The transition model.</param>
        /// <returns>The position of the originating anchor point.</returns>
        public virtual Vector2 GetFromPositionForTransition(TransitionSupportModel transitionModel)
        {
            if (!transitionModel.IsSelfTransition)
            {
                var side = transitionModel.FromNodeAnchorSide;
                var offset = transitionModel.FromNodeAnchorOffset;
                return this.GetPositionFromAnchorAndOffset(side, offset, GraphView.Zoom);
            }

            // This is a single state transition; use ToPoint as the FromPoint
            return GetToPositionForTransition(transitionModel);
        }

        /// <summary>
        /// Computes the position of the destination anchor point for a transition.
        /// </summary>
        /// <param name="transitionModel">The transition model.</param>
        /// <returns>The position of the destination anchor point.</returns>
        public virtual Vector2 GetToPositionForTransition(TransitionSupportModel transitionModel)
        {
            var side = transitionModel.ToNodeAnchorSide;
            var offset = transitionModel.ToNodeAnchorOffset;
            return this.GetPositionFromAnchorAndOffset(side, offset, GraphView.Zoom);
        }

        /// <summary>
        /// Displays the connector for a wire.
        /// </summary>
        /// <param name="wire">The wire.</param>
        public virtual void ShowConnector(AbstractWire wire)
        {
            if (Border is StateBorder stateBorder)
            {
                stateBorder.ShowConnectorOnWire(wire);
            }
        }

        /// <summary>
        /// Hides the connector for a wire.
        /// </summary>
        /// <param name="wire">The wire.</param>
        public virtual void HideConnector(AbstractWire wire)
        {
            if (Border is StateBorder stateBorder)
            {
                stateBorder.HideConnector();
            }
        }

        void OnMouseMoveForAnchorPreview(MouseMoveEvent evt)
        {
            UpdateAnchorPreview(TransitionConnector.MatchesCreateTransitionBinding(evt, GraphView?.GraphTool), evt.mousePosition);
        }

        void OnMouseLeaveForAnchorPreview(MouseLeaveEvent evt)
        {
            UpdateAnchorPreview(false, evt.mousePosition);
        }

        // Previews where a transition would start from, so that holding the modifier shows the user
        // the anchor point before they commit to it.
        void UpdateAnchorPreview(bool modifierHeld, Vector2 worldPosition)
        {
            if (Border is not StateBorder stateBorder)
                return;

            // Once a transition is being created the connector belongs to the manipulator.
            if (TransitionConnector is { IsActive: true })
                return;

            var showPreview = modifierHeld && !PlaceholderModelHelper.IsMissingTypeModel(StateModel);

            // Only ever take back a preview this actually started. The border shows the connector by
            // itself while the pointer is over the edge of the state, and clearing it from here on every
            // mouse move would undo that as soon as it appeared.
            if (showPreview)
                stateBorder.ShowAnchorPreview(worldPosition);
            else if (m_AnchorPreviewShown)
                stateBorder.HideAnchorPreview(worldPosition);

            m_AnchorPreviewShown = showPreview;
        }

        /// <inheritdoc />
        protected override DynamicBorder CreateDynamicBorder() => new StateBorder(this);

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            PartList.AppendPart(StateColorLinePart.Create(colorLinePartName, GraphElementModel, this, ussClassName));
            PartList.AppendPart(NodeTitlePart.Create(titleContainerPartName, GraphElementModel, this, ussClassName,
                EditableTitlePart.Options.UseEllipsis | NodeTitlePart.Options.HasIcon));
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            TransitionConnector = new TransitionConnector();

            // The border only sees the mouse when it is over the edge of the state, but the create
            // transition modifier works anywhere on the state, so the preview is driven from here.
            RegisterCallback<MouseMoveEvent>(OnMouseMoveForAnchorPreview);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeaveForAnchorPreview);

            AddToClassList(ussClassName);
            this.AddPackageStylesheet("State.uss");

            // Move the border (which covers the whole state) before the state content, so we can interact with content.
            if (Border.pickingMode == PickingMode.Position)
            {
                Border.RemoveFromHierarchy();
                hierarchy.Insert(0, Border);
            }
        }

        /// <inheritdoc />
        public virtual void BeginAnimating(float animationSpeed)
        {
            var part = PartList.GetPart(colorLinePartName) as NodeColorLinePart;
            if (part == null)
                return;

            part.PlayAnimation(animationSpeed);
        }

        /// <inheritdoc />
        public virtual void StopAnimating()
        {
            var part = PartList.GetPart(colorLinePartName) as NodeColorLinePart;
            if (part == null)
                return;

            part.StopAnimation();
        }

        /// <inheritdoc />
        public virtual void AnimationUpdate(double deltaTime)
        {
            var part = PartList.GetPart(colorLinePartName) as NodeColorLinePart;
            if (part == null)
                return;

            part.UpdateAnimation(deltaTime);
        }

        /// <summary>
        /// Sets the fill amount displayed on the state's accent bar.
        /// </summary>
        /// <param name="percentage">The fill amount, in percent, in the range [-100, 100].</param>
        public virtual void OverrideFillAmount(float percentage)
        {
            var part = PartList.GetPart(colorLinePartName) as NodeColorLinePart;
            if (part == null)
                return;

            part.OverrideFillAmount(percentage);
        }

        /// <inheritdoc />
        public virtual void ClearFillAmountOverride()
        {
            var part = PartList.GetPart(colorLinePartName) as NodeColorLinePart;
            if (part == null)
                return;

            part.ClearFillAmountOverride();
        }

        internal bool PasteAsNew()
        {
            using var copyPaste = GraphView.GraphTool.ClipboardProvider.DeserializeDataFromClipboard();
            return HandlePasteOperation(PasteOperation.Paste, TransitionView.pasteTransitionsAsNewCommandName, new Vector2(0, 0), copyPaste);
        }

        /// <inheritdoc />
        public override bool HandlePasteOperation(PasteOperation operation, string operationName, Vector2 delta, CopyPasteData copyPasteData)
        {
            if (!TransitionView.CanPasteTransitionsAsNew(copyPasteData))
                return false;

            var additivePaste = operationName == TransitionView.pasteTransitionsAsNewCommandName;
            var transitionsToPaste = new List<TransitionSupportModel>();
            foreach (var wire in copyPasteData.Wires)
            {
                if (wire is TransitionSupportModel transition)
                {
                    transitionsToPaste.Add(transition);
                }
            }
            GraphView.Dispatch(new PasteSelfTransitionSupportsCommand(GraphView.GraphModel, StateModel, transitionsToPaste, additivePaste));

            return true;
        }
    }
}
