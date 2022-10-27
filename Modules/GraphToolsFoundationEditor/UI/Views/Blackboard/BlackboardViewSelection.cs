// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    class BlackboardViewSelection : ViewSelection
    {
        protected readonly BlackboardViewStateComponent m_BlackboardViewState;

        /// <inheritdoc />
        public override IEnumerable<GraphElementModel> SelectableModels
        {
            get => m_GraphModelState.GraphModel.SectionModels.SelectMany(t => t.ContainedModels).Where(t => t.IsSelectable());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardViewSelection"/> class.
        /// </summary>
        /// <param name="view">The view used to dispatch commands.</param>
        /// <param name="viewModel">The blackboard view model.</param>
        public BlackboardViewSelection(RootView view, BlackboardViewModel viewModel)
            : base(view, viewModel.GraphModelState, viewModel.SelectionState)
        {
            m_BlackboardViewState = viewModel.ViewState;
        }

        /// <inheritdoc />
        protected override CopyPasteData BuildCopyPasteData(HashSet<GraphElementModel> elementsToCopySet)
        {
            var copyPaste = CopyPasteData.GatherCopiedElementsData_Internal(m_BlackboardViewState, elementsToCopySet.ToList());
            return copyPaste;
        }
    }
}
