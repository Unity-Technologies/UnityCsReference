// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.CSO;
using UnityEngine.Pool;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Makes selected models highlighted.
    /// </summary>
    [UnityRestricted]
    internal class DeclarationHighlighter : StateObserver
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
            : base(new IStateComponent[] { toolState, selectionState }, new IStateComponent[] { highlighterState })
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
                            using var disposeSelection = ListPool<DeclarationModel>.Get(out var filteredSelection);

                            foreach (var model in selection)
                            {
                                if (m_SelectionFilter != null)
                                {
                                    var declaration = m_SelectionFilter(model);
                                    if (declaration != null)
                                        filteredSelection.Add(declaration);
                                }
                                else
                                {
                                    if (model is DeclarationModel declaration)
                                        filteredSelection.Add(declaration);
                                }
                            }

                            updater.SetHighlightedDeclarations(m_SelectionState.Guid, filteredSelection);
                        }
                    }
                }
            }
        }
    }
}
