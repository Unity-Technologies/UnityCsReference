// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal class BlackboardViewSelection : ViewSelection
    {
        protected readonly BlackboardContentStateComponent m_BlackboardContentState;
        protected readonly BlackboardViewStateComponent m_BlackboardViewState;

        /// <inheritdoc />
        public override IEnumerable<GraphElementModel> SelectableModels =>
            m_BlackboardContentState.BlackboardModel.GraphModel.SectionModels.SelectMany(t => t.ContainedModels).Where(t => t.IsSelectable());

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardViewSelection"/> class.
        /// </summary>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="blackboardContentState">The blackboard content state.</param>
        /// <param name="blackboardViewState">The blackboard view state.</param>
        /// <param name="clipboardProvider">The clipboard provider.</param>
        public BlackboardViewSelection(SelectionStateComponent selectionState,
                                       BlackboardContentStateComponent blackboardContentState,
                                       BlackboardViewStateComponent blackboardViewState, ClipboardProvider clipboardProvider)
            : base(selectionState, clipboardProvider)
        {
            m_BlackboardContentState = blackboardContentState;
            m_BlackboardViewState = blackboardViewState;
        }

        /// <inheritdoc />
        public override IReadOnlyList<GraphElementModel> GetSelection()
        {
            return m_SelectionState?.GetSelection(m_BlackboardContentState.BlackboardModel.GraphModel) ?? s_EmptyList;
        }

        /// <inheritdoc />
        protected override CopyPasteData BuildCopyPasteData(HashSet<GraphElementModel> elementsToCopySet)
        {
            var copyPaste = new CopyPasteData(m_BlackboardViewState, elementsToCopySet.ToList());
            return copyPaste;
        }
    }
}
