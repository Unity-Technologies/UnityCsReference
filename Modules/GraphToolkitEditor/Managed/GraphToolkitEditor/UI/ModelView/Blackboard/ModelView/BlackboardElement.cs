// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.InternalBridge;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Base class for model UI in the blackboard.
    /// </summary>
    [UnityRestricted]
    internal abstract class BlackboardElement : ModelView
    {
        /// <summary>
        /// The USS class name added to a <see cref="BlackboardElement"/>.
        /// </summary>
        public static readonly string ussClassName = "ge-blackboard-element";

        /// <summary>
        /// The USS class name added to a <see cref="BlackboardElement"/> when it can be selected.
        /// </summary>
        public static readonly string selectableUssClassName = ussClassName.WithUssModifier(GraphElementHelper.selectableUssModifier);

        /// <summary>
        /// The USS class name added to the selection border of a <see cref="BlackboardElement"/>.
        /// </summary>
        public static readonly string selectionBorderUssClassName = ussClassName.WithUssElement("selection-border");

        /// <summary>
        /// The view holding this element.
        /// </summary>
        public BlackboardView BlackboardView => RootView as BlackboardView;

        /// <summary>
        /// The model that backs the UI.
        /// </summary>
        public GraphElementModel GraphElementModel => Model as GraphElementModel;


        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardElement"/> class.
        /// </summary>
        public BlackboardElement()
        {
            RegisterCallback<KeyDownEvent>(OnRenameKeyDown);
            focusable = true;

            ContextualMenuManipulator = new ContextualMenuManipulator(BuildContextualMenu);
        }

        protected internal virtual VisualElement CreateSelectionBorder()
        {
            var selectionBorder = new VisualElement();
            selectionBorder.pickingMode = PickingMode.Ignore;
            selectionBorder.AddToClassList(selectionBorderUssClassName);
            return selectionBorder;
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            AddToClassList(ussClassName);

            base.PostBuildUI();
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);

            EnableInClassList(selectableUssClassName, GraphElementModel?.IsSelectable() ?? false);
        }

        /// <inheritdoc />
        public override void UpdateUISelection(UpdateSelectionVisitor visitor)
        {
            if (IsSelected())
            {
                this.SetCheckedPseudoState(true);
            }
            else
            {
                this.SetCheckedPseudoState(false);
            }
        }

        /// <summary>
        /// Checks if the underlying graph element model is selected.
        /// </summary>
        /// <returns>True if the model is selected, false otherwise.</returns>
        public bool IsSelected()
        {
            return BlackboardView?.BlackboardRootViewModel.SelectionState?.IsSelected(GraphElementModel) ?? false;
        }

        /// <summary>
        /// Callback for the KeyDownEvent to handle renames.
        /// </summary>
        /// <param name="e">The event.</param>
        protected virtual void OnRenameKeyDown(KeyDownEvent e)
        {
            if (IsRenameKey(e))
            {
                GraphElementModel graphElementModel = GraphElementModel;
                if (graphElementModel == null)
                    return;

                if (graphElementModel.IsRenamable() && IsSelected())
                {
                    if (Rename())
                    {
                        e.StopPropagation();
                    }
                }
            }
        }
    }
}
