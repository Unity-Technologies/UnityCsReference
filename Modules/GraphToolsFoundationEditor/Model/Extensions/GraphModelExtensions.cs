// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Extension methods for <see cref="GraphModel"/>.
    /// </summary>
    static class GraphModelExtensions
    {
        static readonly Vector2 k_PortalOffset = Vector2.right * 150;

        public static IEnumerable<IHasDeclarationModel> FindReferencesInGraph(this GraphModel self, DeclarationModel variableDeclarationModel)
        {
            return self.NodeModels.OfType<IHasDeclarationModel>().Where(v => v.DeclarationModel != null && variableDeclarationModel.Guid == v.DeclarationModel.Guid);
        }

        public static IEnumerable<T> FindReferencesInGraph<T>(this GraphModel self, DeclarationModel variableDeclarationModel) where T : IHasDeclarationModel
        {
            return self.FindReferencesInGraph(variableDeclarationModel).OfType<T>();
        }

        /// <summary>
        /// Creates a new node in a graph.
        /// </summary>
        /// <param name="self">The graph to add a node to.</param>
        /// <param name="nodeName">The name of the node to create.</param>
        /// <param name="position">The position of the node to create.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the node is created.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <typeparam name="TNodeType">The type of the new node to create.</typeparam>
        /// <returns>The newly created node.</returns>
        public static TNodeType CreateNode<TNodeType>(this GraphModel self, string nodeName = "", Vector2 position = default,
            SerializableGUID guid = default, Action<TNodeType> initializationCallback = null, SpawnFlags spawnFlags = SpawnFlags.Default)
            where TNodeType : AbstractNodeModel
        {
            Action<AbstractNodeModel> setupWrapper = null;
            if (initializationCallback != null)
            {
                setupWrapper = n => initializationCallback.Invoke(n as TNodeType);
            }

            return (TNodeType)self.CreateNode(typeof(TNodeType), nodeName, position, guid, setupWrapper, spawnFlags);
        }

        /// <summary>
        /// Creates a new variable declaration in the graph.
        /// </summary>
        /// <param name="self">The graph to add a variable declaration to.</param>
        /// <param name="variableDataType">The type of data the new variable declaration to create represents.</param>
        /// <param name="variableName">The name of the new variable declaration to create.</param>
        /// <param name="modifierFlags">The modifier flags of the new variable declaration to create.</param>
        /// <param name="isExposed">Whether the variable is exposed externally or not.</param>
        /// <param name="group">The group in which the variable is added. If null, it will go to the root group.</param>
        /// <param name="indexInGroup">THe index of the variable in the group. For indexInGroup &lt;= 0, The item will be added at the beginning. For indexInGroup &gt;= Items.Count, items will be added at the end.</param>
        /// <param name="initializationModel">The initialization model of the new variable declaration to create. Can be <code>null</code>.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the variable declaration is created.</param>
        /// <param name="spawnFlags">The flags specifying how the variable declaration is to be spawned.</param>
        /// <typeparam name="TDeclType">The type of variable declaration to create.</typeparam>
        /// <returns>The newly created variable declaration.</returns>
        public static TDeclType CreateGraphVariableDeclaration<TDeclType>(this GraphModel self, TypeHandle variableDataType,
            string variableName, ModifierFlags modifierFlags, bool isExposed, GroupModel group = null, int indexInGroup = int.MaxValue, Constant initializationModel = null,
            SerializableGUID guid = default, Action<TDeclType, Constant> initializationCallback = null,
            SpawnFlags spawnFlags = SpawnFlags.Default)
            where TDeclType : VariableDeclarationModel
        {
            return (TDeclType)self.CreateGraphVariableDeclaration(typeof(TDeclType), variableDataType, variableName,
                modifierFlags, isExposed, group, indexInGroup, initializationModel, guid, (d, c) => initializationCallback?.Invoke((TDeclType)d, c), spawnFlags);
        }

        public static AbstractNodeModel CreateOppositePortal(this GraphModel self, WirePortalModel wirePortalModel, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var offset = Vector2.zero;
            switch (wirePortalModel)
            {
                case ISingleInputPortNodeModel _:
                    offset = k_PortalOffset;
                    break;
                case ISingleOutputPortNodeModel _:
                    offset = -k_PortalOffset;
                    break;
            }
            var currentPos = wirePortalModel?.Position ?? Vector2.zero;
            return self.CreateOppositePortal(wirePortalModel, currentPos + offset, spawnFlags);
        }

        public static void DeleteVariableDeclaration(this GraphModel self,
            VariableDeclarationModel variableDeclarationToDelete, bool deleteUsages)
        {
            self.DeleteVariableDeclarations(new[] { variableDeclarationToDelete }, deleteUsages);
        }

        public static void DeleteNode(this GraphModel self, AbstractNodeModel nodeToDelete, bool deleteConnections)
        {
            self.DeleteNodes(new[] { nodeToDelete }, deleteConnections);
        }

        public static void DeleteWire(this GraphModel self, WireModel wireToDelete)
        {
            self.DeleteWires(new[] { wireToDelete });
        }

        public static void DeleteStickyNote(this GraphModel self, StickyNoteModel stickyNoteToDelete)
        {
            self.DeleteStickyNotes(new[] { stickyNoteToDelete });
        }

        public static void DeletePlacemat(this GraphModel self, PlacematModel placematToDelete)
        {
            self.DeletePlacemats(new[] { placematToDelete });
        }

        struct ElementsByType
        {
            public HashSet<StickyNoteModel> StickyNoteModels;
            public HashSet<PlacematModel> PlacematModels;
            public HashSet<VariableDeclarationModel> VariableDeclarationsModels;
            public HashSet<GroupModel> GroupModels;
            public HashSet<WireModel> WireModels;
            public HashSet<AbstractNodeModel> NodeModels;
        }

        static void RecursiveSortElements(ref ElementsByType elementsByType, IEnumerable<GraphElementModel> graphElementModels)
        {
            foreach (var element in graphElementModels)
            {
                if (element is IGraphElementContainer container)
                    RecursiveSortElements(ref elementsByType, container.GraphElementModels);
                switch (element)
                {
                    case StickyNoteModel stickyNoteModel:
                        elementsByType.StickyNoteModels.Add(stickyNoteModel);
                        break;
                    case PlacematModel placematModel:
                        elementsByType.PlacematModels.Add(placematModel);
                        break;
                    case VariableDeclarationModel variableDeclarationModel:
                        elementsByType.VariableDeclarationsModels.Add(variableDeclarationModel);
                        break;
                    case GroupModel groupModel:
                        elementsByType.GroupModels.Add(groupModel);
                        break;
                    case WireModel wireModel:
                        elementsByType.WireModels.Add(wireModel);
                        break;
                    case AbstractNodeModel nodeModel:
                        elementsByType.NodeModels.Add(nodeModel);
                        break;
                }
            }
        }

        public static void DeleteElements(this GraphModel self,
            IReadOnlyCollection<GraphElementModel> graphElementModels)
        {
            ElementsByType elementsByType;

            elementsByType.StickyNoteModels = new HashSet<StickyNoteModel>();
            elementsByType.PlacematModels = new HashSet<PlacematModel>();
            elementsByType.VariableDeclarationsModels = new HashSet<VariableDeclarationModel>();
            elementsByType.GroupModels = new HashSet<GroupModel>();
            elementsByType.WireModels = new HashSet<WireModel>();
            elementsByType.NodeModels = new HashSet<AbstractNodeModel>();

            RecursiveSortElements(ref elementsByType, graphElementModels);

            // Add nodes that would be backed by declaration models.
            elementsByType.NodeModels.UnionWith(elementsByType.VariableDeclarationsModels.SelectMany(d => self.FindReferencesInGraph<IHasDeclarationModel>(d).OfType<AbstractNodeModel>()));

            // Add wires connected to the deleted nodes.
            var allWires = self.WireModels.Union(self.Placeholders.OfType<WireModel>()).ToList();
            foreach (var portModel in elementsByType.NodeModels.OfType<PortNodeModel>().SelectMany(n => n.Ports))
                elementsByType.WireModels.UnionWith(allWires.Where(e => e != null && (e.ToPort == portModel || e.FromPort == portModel)));

            self.DeleteVariableDeclarations(elementsByType.VariableDeclarationsModels, deleteUsages: false);
            self.DeleteGroups(elementsByType.GroupModels);
            self.DeleteStickyNotes(elementsByType.StickyNoteModels);
            self.DeletePlacemats(elementsByType.PlacematModels);
            self.DeleteWires(elementsByType.WireModels);
            self.DeleteNodes(elementsByType.NodeModels, deleteConnections: false);
        }

        /// <summary>
        /// Gets the model for a GUID.
        /// </summary>
        /// <param name="self">The graph from which to get the model.</param>
        /// <param name="guid">The GUID of the model to get.</param>
        /// <returns>The model found, or null.</returns>
        public static GraphElementModel GetModel(this GraphModel self, SerializableGUID guid)
        {
            self.TryGetModelFromGuid(guid, out var model);
            return model;
        }

        /// <summary>
        /// Find the single wire that is connected to both port.
        /// </summary>
        /// <param name="self">The graph model.</param>
        /// <param name="toPort">The port the wire is going to.</param>
        /// <param name="fromPort">The port the wire is coming from.</param>
        /// <returns>The wire that connects the two ports, or null.</returns>
        public static WireModel GetWireConnectedToPorts(this GraphModel self, PortModel toPort, PortModel fromPort)
        {
            var wires = self?.GetWiresForPort(toPort);
            if (wires != null)
                foreach (var wire in wires)
                {
                    if (wire.ToPort == toPort && wire.FromPort == fromPort)
                        return wire;
                }

            return null;
        }

        public static void QuickCleanup(this GraphModel self)
        {
            var toRemove = self.WireModels.Where(e => e?.ToPort == null || e.FromPort == null).Cast<GraphElementModel>()
                .Concat(self.NodeModels.Where(m => m.Destroyed))
                .ToList();
            self.DeleteElements(toRemove);
        }

        /// <summary>
        /// Gets a list of subgraph nodes on the current graph that reference the current graph.
        /// </summary>
        /// <returns>A list of subgraph nodes on the current graph that reference the current graph.</returns>
        public static IEnumerable<SubgraphNodeModel> GetRecursiveSubgraphNodes(this GraphModel self)
        {
            var recursiveSubgraphNodeModels = new List<SubgraphNodeModel>();

            var subgraphNodeModels = self.NodeModels.OfType<SubgraphNodeModel>().ToList();
            if (subgraphNodeModels.Any())
            {
                recursiveSubgraphNodeModels.AddRange(subgraphNodeModels.Where(subgraphNodeModel => subgraphNodeModel.SubgraphModel == self));
            }

            return recursiveSubgraphNodeModels;
        }

        /// <summary>
        /// A version of <see cref="GraphModel.Name"/> usable in C# scripts.
        /// </summary>
        public static string GetFriendlyScriptName(this GraphModel graphModel) => graphModel?.Name?.CodifyString_Internal() ?? "";
    }
}
