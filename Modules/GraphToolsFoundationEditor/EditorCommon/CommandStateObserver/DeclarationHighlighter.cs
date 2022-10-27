// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CommandStateObserver;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Makes selected models highlighted.
    /// </summary>
    class DeclarationHighlighter : StateObserver
    {
        ToolStateComponent m_ToolState;
        SelectionStateComponent m_SelectionState;
        DeclarationHighlighterStateComponent m_HighlighterState;
        Func<GraphElementModel, DeclarationModel> m_SelectionFilter;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeclarationHighlighter"/> class.
        /// </summary>
        /// <param name="toolState">The tool state.</param>
        /// <param name="selectionState">The selection state; holds the selected models.</param>
        /// <param name="highlighterState">The declaration highlighter state; holds the model to highlight.</param>
        /// <param name="selectionFilter">A filter to extract the declaration models from the selection. If null, all selected <see cref="DeclarationModel"/> will be highlighted.</param>
        public DeclarationHighlighter(ToolStateComponent toolState,
            SelectionStateComponent selectionState,
            DeclarationHighlighterStateComponent highlighterState,
            Func<GraphElementModel, DeclarationModel> selectionFilter = null)
            : base(new IStateComponent[] { toolState, selectionState }, new[] { highlighterState })
        {
            m_ToolState = toolState;
            m_SelectionState = selectionState;
            m_HighlighterState = highlighterState;
            m_SelectionFilter = selectionFilter;
        }

        /// <inheritdoc />
        public override void Observe()
        {
            using (var toolObservation = this.ObserveState(m_ToolState))
            {
                var toolChanged = toolObservation.UpdateType != UpdateType.None;

                using (var selectionObservation = this.ObserveState(m_SelectionState))
                {
                    if (toolChanged || selectionObservation.UpdateType != UpdateType.None)
                    {
                        using (var updater = m_HighlighterState.UpdateScope)
                        {
                            IEnumerable<GraphElementModel> selection = m_SelectionState.GetSelection(m_ToolState.GraphModel);
                            if (m_SelectionFilter != null)
                            {
                                selection = selection.Select(m => m_SelectionFilter(m));
                            }

                            IEnumerable<DeclarationModel> declarationModels = selection.OfType<DeclarationModel>();
                            updater.SetHighlightedDeclarations(m_SelectionState.Guid, declarationModels);
                        }
                    }
                }
            }
        }
    }
}
