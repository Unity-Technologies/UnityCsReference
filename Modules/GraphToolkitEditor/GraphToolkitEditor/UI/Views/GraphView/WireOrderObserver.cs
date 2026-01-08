// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.CSO;

namespace Unity.GraphToolkit.Editor
{
    class WireOrderObserver : StateObserver
    {
        SelectionStateComponent m_SelectionState;
        GraphModelStateComponent m_GraphModelState;

        public WireOrderObserver(SelectionStateComponent selectionState, GraphModelStateComponent graphModelState)
            : base(new IStateComponent[] { selectionState },
                   new IStateComponent[] { graphModelState })
        {
            m_SelectionState = selectionState;
            m_GraphModelState = graphModelState;
        }

        public override void Observe()
        {
            if (m_GraphModelState.GraphModel == null)
                return;

            using (var selObs = this.ObserveState(m_SelectionState))
            {
                List<GraphElementModel> changedModels = null;

                if (selObs.UpdateType == UpdateType.Complete)
                {
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    changedModels = m_GraphModelState.GraphModel.WireModels.Concat<GraphElementModel>(m_GraphModelState.GraphModel.NodeModels).ToList();
#pragma warning restore RS0030
                }
                else if (selObs.UpdateType == UpdateType.Partial)
                {
                    var changeset = m_SelectionState.GetAggregatedChangeset(selObs.LastObservedVersion);
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    var selectionChangedModels = changeset.ChangedModels.Select(m_GraphModelState.GraphModel.GetModel).Where(m => m != null);
#pragma warning restore RS0030
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    changedModels = selectionChangedModels.ToList();
#pragma warning restore RS0030
                }

                if (changedModels != null)
                {
                    var portsToUpdate = new HashSet<PortModel>();

                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    foreach (var model in changedModels.OfType<WireModel>())
#pragma warning restore RS0030
                    {
                        if (model.FromPort != null && model.FromPort.HasReorderableWires)
                        {
                            portsToUpdate.Add(model.FromPort);
                        }
                    }

                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    foreach (var model in changedModels.OfType<PortNodeModel>())
#pragma warning restore RS0030
                    {
                        #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        foreach (var port in model.GetPorts()
#pragma warning restore RS0030
                                 .Where(p => p.HasReorderableWires))
                        {
                            portsToUpdate.Add(port);
                        }
                    }

                    if (portsToUpdate.Count > 0)
                    {
                        using (var updater = m_GraphModelState.UpdateScope)
                        {
                            foreach (var portModel in portsToUpdate)
                            {
                                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                                var connectedWires = portModel.GetConnectedWires().ToList();
#pragma warning restore RS0030

                                var selected = m_SelectionState.IsSelected(portModel.NodeModel);
                                if (!selected)
                                {
                                    foreach (var wireModel in connectedWires)
                                    {
                                        selected = m_SelectionState.IsSelected(wireModel);
                                        if (selected)
                                            break;
                                    }
                                }
                                if (selected)
                                    updater.MarkChanged(connectedWires, ChangeHint.NeedsRedraw);
                            }
                        }
                    }
                }
            }
        }
    }
}
