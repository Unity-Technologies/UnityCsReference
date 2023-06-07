// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CommandStateObserver;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    class InspectorSelectionObserver_Internal : StateObserver
    {
        ToolStateComponent m_ToolState;
        GraphModelStateComponent m_GraphModelStateComponent;
        List<SelectionStateComponent> m_SelectionStates;
        ModelInspectorStateComponent m_ModelInspectorState;

        static IStateComponent[] MakeArray(IStateComponent first, IStateComponent second, IReadOnlyList<IStateComponent> remaining)
        {
            var size = remaining.Count + 2;
            var result = new IStateComponent[size];
            result[0] = first;
            result[1] = second;
            for (var i = 0; i < remaining.Count; i++)
            {
                result[i + 2] = remaining[i];
            }

            return result;
        }

        public InspectorSelectionObserver_Internal(ToolStateComponent toolState, GraphModelStateComponent graphModelState,
            IReadOnlyList<SelectionStateComponent> selectionStates, ModelInspectorStateComponent modelInspectorState)
            : base(MakeArray(toolState, graphModelState, selectionStates),
                new IStateComponent[] { modelInspectorState })
        {
            m_ToolState = toolState;
            m_GraphModelStateComponent = graphModelState;
            m_SelectionStates = selectionStates.ToList();
            m_ModelInspectorState = modelInspectorState;
        }

        public override void Observe()
        {
            var graphModel = m_GraphModelStateComponent.GraphModel;
            if (graphModel == null)
                return;

            var selectionObservations = this.ObserveStates(m_SelectionStates);
            try
            {
                using (var toolObservation = this.ObserveState(m_ToolState))
                using (var gvObservation = this.ObserveState(m_GraphModelStateComponent))
                {
                    var selectionUpdateType = UpdateTypeExtensions.Combine(selectionObservations.Select(s => s.UpdateType));
                    var updateType = toolObservation.UpdateType.Combine(selectionUpdateType);

                    if (updateType != UpdateType.None || gvObservation.UpdateType == UpdateType.Complete)
                    {
                        var selection = m_SelectionStates.SelectMany(s => s.GetSelection(graphModel));
                        var selectedModels = m_ToolState.CurrentGraph.GetGraphModel().Stencil.GetModelsDisplayableInInspector(selection).Distinct().ToList();

                        using (var updater = m_ModelInspectorState.UpdateScope)
                        {
                            updater.SetInspectedModels(selectedModels, graphModel);
                        }
                    }
                }
            }
            finally
            {
                foreach (var selectionObservation in selectionObservations)
                {
                    selectionObservation?.Dispose();
                }
            }
        }
    }
}
