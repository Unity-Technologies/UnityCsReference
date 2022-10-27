// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CommandStateObserver;

namespace Unity.GraphToolsFoundation.Editor
{
    class WireOrderObserver_Internal : StateObserver
    {
        SelectionStateComponent m_SelectionState;
        GraphModelStateComponent m_GraphModelState;

        public WireOrderObserver_Internal(SelectionStateComponent selectionState, GraphModelStateComponent graphModelState)
            : base(new[] { selectionState },
                new[] { graphModelState })
        {
            m_SelectionState = selectionState;
            m_GraphModelState = graphModelState;
        }

        public override void Observe()
        {
            using (var selObs = this.ObserveState(m_SelectionState))
            {
                List<GraphElementModel> changedModels = null;

                if (selObs.UpdateType == UpdateType.Complete)
                {
                    changedModels = m_GraphModelState.GraphModel.WireModels.Concat<GraphElementModel>(m_GraphModelState.GraphModel.NodeModels).ToList();
                }
                else if (selObs.UpdateType == UpdateType.Partial)
                {
                    var changeset = m_SelectionState.GetAggregatedChangeset(selObs.LastObservedVersion);
                    var selectionChangedModels = changeset.ChangedModels.Select(m_GraphModelState.GraphModel.GetModel).Where(m => m != null);
                    changedModels = selectionChangedModels.ToList();
                }

                if (changedModels != null)
                {
                    var portsToUpdate = new HashSet<PortModel>();

                    foreach (var model in changedModels.OfType<WireModel>())
                    {
                        if (model.FromPort != null && model.FromPort.HasReorderableWires)
                        {
                            portsToUpdate.Add(model.FromPort);
                        }
                    }

                    foreach (var model in changedModels.OfType<PortNodeModel>())
                    {
                        foreach (var port in model.Ports
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
                                var connectedWires = portModel.GetConnectedWires().ToList();

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

                                updater.MarkChanged(connectedWires, ChangeHint.Style);
                            }
                        }
                    }
                }
            }
        }
    }
}
