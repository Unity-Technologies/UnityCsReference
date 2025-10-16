// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Class for a state node UI.
    /// </summary>
    [UnityRestricted]
    internal class State : NodeView, INodeWithConnector
    {
        /// <summary>
        /// The name of the <see cref="ModelViewPart"/> for the progress bar.
        /// </summary>
        public static readonly string progressBarPartName = "progress-bar";

        /// <summary>
        /// The USS class name of a <see cref="State"/>.
        /// </summary>
        public new static readonly string ussClassName = "ge-state";

        /// <summary>
        /// The USS class name added to the state that is the default entry point.
        /// </summary>
        public static readonly string defaultEnterUssClassName = ussClassName.WithUssModifier("entry-point");

        TransitionConnector m_TransitionConnector;

        /// <summary>
        /// The state model.
        /// </summary>
        public StateModel StateModel => Model as StateModel;

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
            if (transitionModel.TransitionSupportKind == TransitionSupportKind.StateToState)
            {
                var side = transitionModel.FromNodeAnchorSide;
                var offset = transitionModel.FromNodeAnchorOffset;
                return this.GetPositionFromAnchorAndOffset(side, offset, GraphView.ContentViewContainer.resolvedStyle.scale.value.x);
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
            return this.GetPositionFromAnchorAndOffset(side, offset, GraphView.ContentViewContainer.resolvedStyle.scale.value.x);
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

        /// <inheritdoc />
        protected override DynamicBorder CreateDynamicBorder() => new StateBorder(this);

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            PartList.AppendPart(ProgressBarPart.Create(progressBarPartName, GraphElementModel, this, ussClassName));
            PartList.AppendPart(NodeTitlePart.Create(titleContainerPartName, GraphElementModel, this, ussClassName,
                EditableTitlePart.Options.UseEllipsis | NodeTitlePart.Options.HasIcon));
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            TransitionConnector = new TransitionConnector();

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
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);

            EnableInClassList(defaultEnterUssClassName, StateModel?.IsEntryPoint ?? false);
        }

        internal bool PasteAsNew()
        {
            using var copyPaste = GraphView.GraphTool.ClipboardProvider.DeserializeDataFromClipboard();
            return HandlePasteOperation(PasteOperation.Paste, Transition.pasteTransitionsAsNewCommandName, new Vector2(0, 0), copyPaste);
        }

        /// <inheritdoc />
        public override bool HandlePasteOperation(PasteOperation operation, string operationName, Vector2 delta, CopyPasteData copyPasteData)
        {
            if (!Transition.CanPasteTransitionsAsNew(copyPasteData))
                return false;

            var additivePaste = operationName == Transition.pasteTransitionsAsNewCommandName;
            var transitionsToPaste = new List<TransitionSupportModel>();
            foreach (var wire in copyPasteData.Wires)
            {
                if (wire is TransitionSupportModel transition)
                {
                    transitionsToPaste.Add(transition);
                }
            }
            GraphView.Dispatch(new PasteSingleStateTransitionSupportsCommand(GraphView.GraphModel, StateModel, transitionsToPaste, additivePaste));

            return true;
        }
    }
}
