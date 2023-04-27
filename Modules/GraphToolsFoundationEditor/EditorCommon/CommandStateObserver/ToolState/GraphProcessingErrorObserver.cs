// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using Unity.CommandStateObserver;

namespace Unity.GraphToolsFoundation.Editor;

/// <summary>
/// Generates error models from the graph processing results.
/// </summary>
class GraphProcessingErrorObserver : StateObserver
{
    GraphModelStateComponent m_GraphModelStateComponent;
    GraphProcessingStateComponent m_ResultsStateComponent;
    GraphProcessingErrorsStateComponent m_ErrorsStateComponent;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphProcessingErrorObserver"/> class.
    /// </summary>
    public GraphProcessingErrorObserver(GraphModelStateComponent graphModelStateComponent,
        GraphProcessingStateComponent resultsStateComponent,
        GraphProcessingErrorsStateComponent errorsStateComponent)
        : base(new IUndoableStateComponent[] { graphModelStateComponent, resultsStateComponent },
            new[] { errorsStateComponent })
    {
        m_GraphModelStateComponent = graphModelStateComponent;
        m_ResultsStateComponent = resultsStateComponent;
        m_ErrorsStateComponent = errorsStateComponent;
    }

    /// <inheritdoc />
    public override void Observe()
    {
        using var graphObservation = this.ObserveState(m_GraphModelStateComponent);
        using var resultsObservation = this.ObserveState(m_ResultsStateComponent);

        var updateType = resultsObservation.UpdateType.Combine(graphObservation.UpdateType);
        if (updateType != UpdateType.None)
        {
            var stencil = (Stencil)m_GraphModelStateComponent.GraphModel?.Stencil;
            if (stencil != null)
            {
                var errors = m_ResultsStateComponent.Results?
                    .OfType<ErrorsAndWarningsResult>()
                    .SelectMany(r => r.Errors.Select(stencil.CreateProcessingErrorModel).Where(m => m != null));

                using (var updater = m_ErrorsStateComponent.UpdateScope)
                {
                    updater.SetResults(errors);
                }
            }
        }
    }
}
