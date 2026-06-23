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
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_BlackboardContentState.BlackboardModel.GraphModel.SectionModels.SelectMany(t => t.ContainedModels).Where(t => t.IsSelectable());
#pragma warning restore UA2001

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
        protected override bool CanPaste()
        {
            if (!base.CanPaste())
                return false;

            using var copyPasteData = m_ClipboardProvider.DeserializeDataFromClipboard();
            if (copyPasteData == null)
                return false;

            var nodeCount = copyPasteData.Nodes?.Count ?? 0;
            var stickyCount = copyPasteData.StickyNotes?.Count ?? 0;
            var placematCount = copyPasteData.Placemats?.Count ?? 0;

            // Graph-canvas content (nodes, sticky notes, placemats) should not be pasteable in the blackboard.
            if (nodeCount > 0 || stickyCount > 0 || placematCount > 0)
                return false;

            var hasOnePastableVariable = false;
            if (copyPasteData.HasVariableContent())
            {
                foreach (var variableDeclaration in copyPasteData.VariableDeclarations)
                {
                    hasOnePastableVariable =
                        m_BlackboardContentState.BlackboardModel.GraphModel.CanPasteVariable(variableDeclaration);

                    if (hasOnePastableVariable)
                        break;
                }
            }

            return hasOnePastableVariable;
        }

        /// <inheritdoc />
        protected override CopyPasteData BuildCopyPasteData(HashSet<GraphElementModel> elementsToCopySet)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var copyPaste = new CopyPasteData(m_BlackboardViewState, elementsToCopySet.ToList());
#pragma warning restore UA2001
            return copyPaste;
        }
    }
}
