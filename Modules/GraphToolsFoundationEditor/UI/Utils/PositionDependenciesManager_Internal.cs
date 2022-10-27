// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    class PositionDependenciesManager_Internal
    {
        const int k_AlignHorizontalOffset = 30;
        const int k_AlignVerticalOffset = 30;

        readonly GraphView m_GraphView;
        readonly Dictionary<SerializableGUID, Dictionary<SerializableGUID, IDependency>> m_DependenciesByNode = new Dictionary<SerializableGUID, Dictionary<SerializableGUID, IDependency>>();
        readonly Dictionary<SerializableGUID, Dictionary<SerializableGUID, IDependency>> m_PortalDependenciesByNode = new Dictionary<SerializableGUID, Dictionary<SerializableGUID, IDependency>>();
        readonly HashSet<AbstractNodeModel> m_ModelsToMove = new HashSet<AbstractNodeModel>();
        readonly HashSet<AbstractNodeModel> m_TempMovedModels = new HashSet<AbstractNodeModel>();

        Vector2 m_StartPos;
        Preferences m_Preferences;

        public PositionDependenciesManager_Internal(GraphView graphView, Preferences preferences)
        {
            m_GraphView = graphView;
            m_Preferences = preferences;
        }

        void AddWireDependency(AbstractNodeModel parent, IDependency child)
        {
            if (!m_DependenciesByNode.TryGetValue(parent.Guid, out var link))
                m_DependenciesByNode.Add(parent.Guid, new Dictionary<SerializableGUID, IDependency> { { child.DependentNode.Guid, child } });
            else
            {
                if (link.TryGetValue(child.DependentNode.Guid, out IDependency dependency))
                {
                    if (dependency is LinkedNodesDependency linked)
                        linked.Count++;
                    else
                        Debug.LogWarning($"Dependency between nodes {parent} && {child.DependentNode} registered both as a {dependency.GetType().Name} and a {nameof(LinkedNodesDependency)}");
                }
                else
                {
                    link.Add(child.DependentNode.Guid, child);
                }
            }
        }

        internal List<IDependency> GetDependencies_Internal(AbstractNodeModel parent)
        {
            if (!m_DependenciesByNode.TryGetValue(parent.Guid, out var link))
                return null;
            return link.Values.ToList();
        }

        internal List<IDependency> GetPortalDependencies_Internal(WirePortalModel parent)
        {
            if (!m_PortalDependenciesByNode.TryGetValue(parent.Guid, out var link))
                return null;
            return link.Values.ToList();
        }

        public void Remove(SerializableGUID a, SerializableGUID b)
        {
            SerializableGUID parent;
            SerializableGUID child;
            if (m_DependenciesByNode.TryGetValue(a, out var link) &&
                link.TryGetValue(b, out var dependency))
            {
                parent = a;
                child = b;
            }
            else if (m_DependenciesByNode.TryGetValue(b, out link) &&
                     link.TryGetValue(a, out dependency))
            {
                parent = b;
                child = a;
            }
            else
                return;

            if (dependency is LinkedNodesDependency linked)
            {
                linked.Count--;
                if (linked.Count <= 0)
                    link.Remove(child);
            }
            else
                link.Remove(child);
            if (link.Count == 0)
                m_DependenciesByNode.Remove(parent);
        }

        public void Clear()
        {
            foreach (var pair in m_DependenciesByNode)
                pair.Value.Clear();
            m_DependenciesByNode.Clear();

            foreach (var pair in m_PortalDependenciesByNode)
                pair.Value.Clear();
            m_PortalDependenciesByNode.Clear();
        }

        public void LogDependencies()
        {
            if (m_Preferences?.GetBool(BoolPref.DependenciesLogging) ?? false)
            {
                Log("Dependencies :" + String.Join("\r\n", m_DependenciesByNode.Select(n =>
                {
                    var s = String.Join(",", n.Value.Select(p => p.Key));
                    return $"{n.Key}: {s}";
                })));

                Log("Portal Dependencies :" + String.Join("\r\n", m_PortalDependenciesByNode.Select(n =>
                {
                    var s = String.Join(",", n.Value.Select(p => p.Key));
                    return $"{n.Key}: {s}";
                })));
            }
        }

        void Log(string message)
        {
            if (m_Preferences?.GetBool(BoolPref.DependenciesLogging) ?? false)
                Debug.Log(message);
        }

        void ProcessDependency(AbstractNodeModel nodeModel, Vector2 delta, Action<GraphElement, IDependency, Vector2, AbstractNodeModel> dependencyCallback)
        {
            Log($"ProcessDependency {nodeModel}");

            if (!m_DependenciesByNode.TryGetValue(nodeModel.Guid, out var link))
                return;

            foreach (var dependency in link)
            {
                if (m_ModelsToMove.Contains(dependency.Value.DependentNode))
                    continue;
                if (!m_TempMovedModels.Add(dependency.Value.DependentNode))
                {
                    Log($"Skip ProcessDependency {dependency.Value.DependentNode}");
                    continue;
                }

                var graphElement = dependency.Value.DependentNode.GetView<Node>(m_GraphView);
                if (graphElement != null)
                    dependencyCallback(graphElement, dependency.Value, delta, nodeModel);
                else
                    Log($"Cannot find ui node for model: {dependency.Value.DependentNode} dependency from {nodeModel}");

                ProcessDependency(dependency.Value.DependentNode, delta, dependencyCallback);
            }
        }

        void ProcessMovedNodes(Vector2 lastMousePosition, Action<GraphElement, IDependency, Vector2, AbstractNodeModel> dependencyCallback)
        {
            Profiler.BeginSample("GTF.ProcessMovedNodes");

            m_TempMovedModels.Clear();
            Vector2 delta = lastMousePosition - m_StartPos;
            foreach (AbstractNodeModel nodeModel in m_ModelsToMove)
                ProcessDependency(nodeModel, delta, dependencyCallback);

            Profiler.EndSample();
        }

        void ProcessDependencyModel(AbstractNodeModel nodeModel, List<GraphElementModel> outChangedModels,
            Action<IDependency, AbstractNodeModel, List<GraphElementModel>> dependencyCallback)
        {
            Log($"ProcessDependencyModel {nodeModel}");

            if (!m_DependenciesByNode.TryGetValue(nodeModel.Guid, out var link))
                return;

            foreach (var dependency in link)
            {
                if (m_ModelsToMove.Contains(dependency.Value.DependentNode))
                    continue;
                if (!m_TempMovedModels.Add(dependency.Value.DependentNode))
                {
                    Log($"Skip ProcessDependency {dependency.Value.DependentNode}");
                    continue;
                }

                dependencyCallback(dependency.Value, nodeModel, outChangedModels);
                ProcessDependencyModel(dependency.Value.DependentNode, outChangedModels, dependencyCallback);
            }
        }

        void ProcessMovedNodeModels(Action<IDependency, AbstractNodeModel, List<GraphElementModel>> dependencyCallback, List<GraphElementModel> outChangedModels)
        {
            Profiler.BeginSample("GTF.ProcessMovedNodeModel");

            m_TempMovedModels.Clear();
            foreach (AbstractNodeModel nodeModel in m_ModelsToMove)
                ProcessDependencyModel(nodeModel, outChangedModels, dependencyCallback);

            Profiler.EndSample();
        }

        public void UpdateNodeState()
        {
            var processed = new HashSet<SerializableGUID>();
            void SetNodeState(AbstractNodeModel nodeModel, ModelState state)
            {
                if (nodeModel.State == ModelState.Disabled)
                    state = ModelState.Disabled;

                var nodeUI = nodeModel.GetView<Node>(m_GraphView);
                if (nodeUI != null && state == ModelState.Enabled)
                {
                    nodeUI.EnableInClassList(Node.disabledModifierUssClassName, false);
                    nodeUI.EnableInClassList(Node.unusedModifierUssClassName, false);
                }

                Dictionary<SerializableGUID, IDependency> dependencies = null;

                if (nodeModel is WirePortalModel wirePortalModel)
                    m_PortalDependenciesByNode.TryGetValue(wirePortalModel.Guid, out dependencies);

                if ((dependencies == null || !dependencies.Any()) &&
                    !m_DependenciesByNode.TryGetValue(nodeModel.Guid, out dependencies))
                    return;

                foreach (var dependency in dependencies)
                {
                    if (processed.Add(dependency.Key))
                        SetNodeState(dependency.Value.DependentNode, state);
                }
            }

            var graphModel = m_GraphView.GraphModel;
            foreach (var nodeModel in graphModel.NodeAndBlockModels)
            {
                var node = nodeModel.GetView<Node>(m_GraphView);
                if (node == null)
                    continue;

                if (nodeModel.State == ModelState.Disabled)
                {
                    node.EnableInClassList(Node.disabledModifierUssClassName, true);
                    node.EnableInClassList(Node.unusedModifierUssClassName, false);
                }
                else
                {
                    node.EnableInClassList(Node.disabledModifierUssClassName, false);
                    node.EnableInClassList(Node.unusedModifierUssClassName, true);
                }
            }

            foreach (var root in graphModel.Stencil.GetEntryPoints())
            {
                SetNodeState(root, ModelState.Enabled);
            }
        }

        public void ProcessMovedNodes(Vector2 lastMousePosition)
        {
            ProcessMovedNodes(lastMousePosition, OffsetDependency);
        }

        static void OffsetDependency(GraphElement element, IDependency model, Vector2 delta, AbstractNodeModel _)
        {
            Vector2 prevPos = model.DependentNode.Position;
            var pos = prevPos + delta;
            element.SetPosition(pos);
        }

        GraphModel m_GraphModel;

        public void StartNotifyMove(IReadOnlyList<GraphElementModel> selection, Vector2 lastMousePosition)
        {
            m_StartPos = lastMousePosition;
            m_ModelsToMove.Clear();
            m_GraphModel = null;

            foreach (var element in selection)
            {
                if (element is AbstractNodeModel nodeModel)
                {
                    if (m_GraphModel == null)
                        m_GraphModel = nodeModel.GraphModel;
                    else
                        Assert.AreEqual(nodeModel.GraphModel, m_GraphModel);
                    m_ModelsToMove.Add(nodeModel);
                }
            }
        }

        public void CancelMove()
        {
            ProcessMovedNodes(Vector2.zero, (element, model, _, _) =>
            {
                element.SetPosition(model.DependentNode.Position);
            });
            m_ModelsToMove.Clear();
        }

        public List<GraphElementModel> StopNotifyMove()
        {
            var changedModels = new List<GraphElementModel>();

            // case when drag and dropping a declaration to the graph
            if (m_GraphModel == null)
                return changedModels;

            ProcessMovedNodes(Vector2.zero, (element, model, _, _) =>
            {
                model.DependentNode.Position = element.layout.position;
                changedModels.Add(model.DependentNode);
            });

            m_ModelsToMove.Clear();

            return changedModels;
        }

        void AlignDependency(IDependency dependency, AbstractNodeModel prev, List<GraphElementModel> outChangedModels)
        {
            // Warning: Don't try to use the VisualElement.layout Rect as it is not up to date yet.
            // Use Node.GetPosition() when possible

            var parentUI = prev.GetView<Node>(m_GraphView);
            var depUI = dependency.DependentNode.GetView<Node>(m_GraphView);

            if (parentUI == null || depUI == null)
                return;

            switch (dependency)
            {
                case LinkedNodesDependency linked:

                    var input = linked.ParentPort.GetView<Port>(m_GraphView);
                    var output = linked.DependentPort.GetView<Port>(m_GraphView);

                    if (input?.Model != null && output?.Model != null &&
                        ((PortModel)input.Model).Orientation == ((PortModel)output.Model).Orientation)
                    {
                        var depOffset = input.parent.ChangeCoordinatesTo(parentUI.parent, input.layout.min) - output.parent.ChangeCoordinatesTo(depUI.parent, output.layout.min);
                        var parentOffset = parentUI.layout.min - prev.Position;

                        Vector2 position;
                        if (((PortModel)input.Model).Orientation == PortOrientation.Horizontal)
                        {
                            position = new Vector2(
                                prev.Position.x + (linked.ParentPort.Direction == PortDirection.Output
                                    ? parentUI.layout.width + k_AlignHorizontalOffset
                                    : -k_AlignHorizontalOffset - depUI.layout.width),
                                depUI.layout.min.y + depOffset.y - parentOffset.y
                            );
                        }
                        else
                        {
                            position = new Vector2(
                                depUI.layout.min.x + depOffset.x - parentOffset.x,
                                prev.Position.y + (linked.ParentPort.Direction == PortDirection.Output
                                    ? parentUI.layout.height + k_AlignVerticalOffset
                                    : -k_AlignVerticalOffset - depUI.layout.height)
                            );
                        }

                        linked.DependentNode.Position = position;
                        outChangedModels.Add(linked.DependentNode);
                    }
                    break;
            }
        }

        /// <summary>
        /// Aligns the nodes, following the <paramref name="entryPoints"/>.
        /// </summary>
        /// <param name="follow">Set to true to recursively align dependent nodes.</param>
        /// <param name="entryPoints">The elements from where to start the alignment procedure.</param>
        /// <returns>A list of models that where moved.</returns>
        public List<GraphElementModel> AlignNodes(bool follow, IReadOnlyList<GraphElementModel> entryPoints)
        {
            HashSet<AbstractNodeModel> topMostModels = new HashSet<AbstractNodeModel>();
            List<GraphElementModel> changedModels = new List<GraphElementModel>();

            bool anyWire = false;
            foreach (var wireModel in entryPoints.OfType<WireModel>())
            {
                if (!wireModel.GraphModel.Stencil.CreateDependencyFromWire(wireModel, out var dependency, out var parent))
                    continue;
                anyWire = true;

                AlignDependency(dependency, parent, changedModels);
                topMostModels.Add(dependency.DependentNode);
            }

            if (anyWire && !follow)
                return changedModels;

            if (!topMostModels.Any())
            {
                foreach (var nodeModel in entryPoints.OfType<AbstractNodeModel>())
                {
                    topMostModels.Add(nodeModel);
                }
            }

            if (!anyWire && !follow)
            {
                // Align each top-most node then move dependencies by the same delta
                foreach (var model in topMostModels)
                {
                    if (!m_DependenciesByNode.TryGetValue(model.Guid, out var dependencies))
                        continue;
                    foreach (var dependency in dependencies)
                    {
                        AlignDependency(dependency.Value, model, changedModels);
                    }
                }
            }
            else
            {
                // Align recursively
                m_ModelsToMove.UnionWith(topMostModels);
                ProcessMovedNodeModels(AlignDependency, changedModels);
            }

            m_ModelsToMove.Clear();
            m_TempMovedModels.Clear();

            return changedModels;
        }

        public void AddPositionDependency(WireModel model)
        {
            if (model.GraphModel.Stencil == null ||
                !model.GraphModel.Stencil.CreateDependencyFromWire(model, out var dependency, out var parent))
                return;
            AddWireDependency(parent, dependency);
            LogDependencies();
        }

        public void AddPortalDependency(WirePortalModel model)
        {
            var stencil = model.GraphModel.Stencil;

            // Update all portals linked to this portal definition.
            foreach (var portalModel in stencil.GetLinkedPortals(model))
            {
                m_PortalDependenciesByNode[portalModel.Guid] =
                    stencil.GetPortalDependencies(portalModel)
                        .ToDictionary(p => p.Guid, p => (IDependency) new PortalNodesDependency { DependentNode = p });
            }
            LogDependencies();
        }

        public void RemovePortalDependency(AbstractNodeModel model)
        {
            foreach (var dependencies in m_PortalDependenciesByNode.Values)
            {
                dependencies.Remove(model.Guid);
            }

            m_PortalDependenciesByNode.Remove(model.Guid);
        }
    }
}
