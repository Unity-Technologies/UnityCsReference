// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Class used to hold copy paste data.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class CopyPasteData : IDisposable
    {
        [SerializeReference]
        List<Constant> m_Constants;

        [SerializeReference]
        List<AbstractNodeModel> m_Nodes;

        [SerializeReference]
        List<WireModel> m_Wires;

        [Serializable]
        internal struct VariableDeclaration
        {
            [SerializeReference]
            public VariableDeclarationModelBase m_Model;

            [SerializeField]
            public Hash128 m_GroupGUID;

            [SerializeField]
            public int m_GroupIndex;

            [SerializeField]
            public int m_IndexInGroup;
        }

        [Serializable]
        internal struct GroupPath
        {
            [SerializeField]
            public Hash128 m_OriginalGUID;

            [SerializeField]
            public string[] m_Path;

            [SerializeField]
            public bool m_Expanded;
        }

        [SerializeField]
        List<GroupPath> m_VariableGroupPaths;
        List<GroupModel> m_VariableGroups;

        [SerializeField]
        List<VariableDeclaration> m_VariableDeclarations;

        [SerializeReference]
        List<VariableDeclarationModelBase> m_ImplicitVariableDeclarations;

        [SerializeField]
        Vector2 m_TopLeftNodePosition;

        [SerializeReference]
        List<StickyNoteModel> m_StickyNotes;

        [SerializeReference]
        List<PlacematModel> m_Placemats;

        bool m_ShouldRunOnAfterCopy;
        bool m_Disposed;

        /// <summary>
        /// The position where the top-left-most node will be created.
        /// </summary>
        public Vector2 TopLeftNodePosition => m_TopLeftNodePosition;

        /// <summary>
        /// The <see cref="AbstractNodeModel"/>s to paste.
        /// </summary>
        public IReadOnlyList<AbstractNodeModel> Nodes => m_Nodes;

        /// <summary>
        /// The <see cref="Constant"/>s to paste.
        /// </summary>
        public IReadOnlyList<Constant> Constants => m_Constants;

        /// <summary>
        /// The <see cref="WireModel"/>s to paste.
        /// </summary>
        public IReadOnlyList<WireModel> Wires => m_Wires;

        /// <summary>
        /// The <see cref="StickyNoteModel"/>s to paste.
        /// </summary>
        public IReadOnlyList<StickyNoteModel> StickyNotes => m_StickyNotes;

        /// <summary>
        /// The <see cref="PlacematModel"/>s to paste.
        /// </summary>
        public IReadOnlyList<PlacematModel> Placemats => m_Placemats;

        /// <summary>
        /// The explicitly selected <see cref="VariableDeclarationModelBase"/>s to paste.
        /// </summary>
        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        public IReadOnlyList<VariableDeclarationModelBase> VariableDeclarations => m_VariableDeclarations.Select(v => v.m_Model).ToList();
#pragma warning restore UA2001

        /// <summary>
        /// The implicitly selected <see cref="VariableDeclarationModelBase"/>s to paste, usually from <see cref="VariableNodeModel"/>s.
        /// </summary>
        public IReadOnlyList<VariableDeclarationModelBase> ImplicitVariableDeclarations => m_ImplicitVariableDeclarations;

        /// <summary>
        /// Whether there is any model to paste.
        /// </summary>
        public bool IsEmpty() => (m_Nodes.Count == 0 && m_Wires.Count == 0 &&
            m_VariableDeclarations.Count == 0 && m_StickyNotes.Count == 0 && m_Placemats.Count == 0 && m_VariableGroupPaths.Count == 0);

        internal bool HasVariableContent()
        {
            return m_VariableDeclarations.Count > 0 || m_VariableGroupPaths.Count > 0;
        }

        public CopyPasteData(List<Constant> constants)
        {
            m_Constants = constants;
        }

        public CopyPasteData(BlackboardViewStateComponent bbState, IReadOnlyCollection<Model> graphElementModels)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var originalNodes = graphElementModels.OfType<AbstractNodeModel>().ToList();
#pragma warning restore UA2001

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var variableDeclarationsToCopy = graphElementModels
#pragma warning restore UA2001
                .OfType<VariableDeclarationModelBase>()
                .ToList();

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var stickyNotesToCopy = graphElementModels
#pragma warning restore UA2001
                .OfType<StickyNoteModel>()
                .ToList();

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var placematsToCopy = graphElementModels
#pragma warning restore UA2001
                .OfType<PlacematModel>()
                .ToList();

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var wiresToCopy = graphElementModels
#pragma warning restore UA2001
                .OfType<WireModel>()
                .ToList();

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var implicitVariableDeclarations = originalNodes.OfType<VariableNodeModel>()
#pragma warning restore UA2001
                .Select(t => t.VariableDeclarationModel).Except(variableDeclarationsToCopy).ToList();

            var topLeftNodePosition = Vector2.positiveInfinity;
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var n in originalNodes.Where(t => t.IsMovable()))
#pragma warning restore UA2001
            {
                topLeftNodePosition = Vector2.Min(topLeftNodePosition, n.Position);
            }

            foreach (var n in stickyNotesToCopy)
            {
                topLeftNodePosition = Vector2.Min(topLeftNodePosition, n.PositionAndSize.position);
            }

            foreach (var n in placematsToCopy)
            {
                topLeftNodePosition = Vector2.Min(topLeftNodePosition, n.PositionAndSize.position);
            }

            if (topLeftNodePosition == Vector2.positiveInfinity)
            {
                topLeftNodePosition = Vector2.zero;
            }

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var originalGroups = graphElementModels.OfType<GroupModel>().ToList();
#pragma warning restore UA2001

            var groups = new List<GroupModel>();

            // Cache the depth values of each group for more efficient sorting
            var depthByGroup = new Dictionary<GroupModel, int>();
            foreach (var group in originalGroups)
            {
                var depth = 0;
                GroupModelBase current = group;
                while (current.ParentGroup != null)
                {
                    ++depth;
                    current = current.ParentGroup;
                }
                depthByGroup[group] = depth;
            }

            originalGroups.Sort((a, b) =>
            {
                var depthCompare = depthByGroup[a].CompareTo(depthByGroup[b]);
                if (depthCompare != 0)
                    return depthCompare;

                // if both groups are at the same depth, fall back to standard group comparison function
                return GroupItemOrderComparer.Default.Compare(a, b);
            });

            // Make sure all groups inside other selected groups are ignored.
            foreach (var group in originalGroups)
            {
                var current = group.ParentGroup;
                var found = false;
                while (current != null)
                {
                    if (groups.Contains(current))
                    {
                        found = true;
                        break;
                    }

                    current = current.ParentGroup;
                }

                if (!found)
                    groups.Add(group);
            }


            for (var i = 0; i < groups.Count; ++i)
            {
                RecursiveAddGroups(ref i, groups);
            }

            // here groups contains an order list of all copied groups with their entire subgroup hierarchy

            var groupIndices = new Dictionary<GroupModelBase, int>();
            var cpt = 0;
            var groupPaths = new List<GroupPath>();
            var groupPath = new List<String>();
            var groupsToCopy = new List<GroupModel>();

            foreach (var group in groups)
            {
                groupPath.Clear();
                var current = group.ParentGroup;
                groupPath.Add(group.Title);
                while (current != null)
                {
                    if (!groups.Contains(current))
                        break;
                    groupPath.Insert(0, current.Title);
                    current = current.ParentGroup;
                }

                groupPath.Insert(0, group.GetSection().Title);

                groupPaths.Add(new GroupPath() { m_OriginalGUID = group.Guid, m_Path = groupPath.ToArray(), m_Expanded = bbState?.GetGroupExpanded(group) ?? true });
                groupIndices[group] = cpt++;

                groupsToCopy.Add(group);
            }

            var declarations = new List<VariableDeclaration>(variableDeclarationsToCopy.Count);

            //First add all variables outside of copied groups
            foreach (var variable in variableDeclarationsToCopy)
            {
                var group = variable.ParentGroup;

                if (groupIndices.ContainsKey(group))
                    continue;

                declarations.Add(new VariableDeclaration { m_Model = variable, m_GroupGUID = group.Guid, m_GroupIndex = -1, m_IndexInGroup = -1 });
            }

            var inGroupDeclarations = new List<VariableDeclaration>();
            if (groups.Count > 0)
            {
                var graphModel = groups[0].GraphModel;

                foreach (var variable in graphModel.VariableDeclarations)
                {
                    if (groupIndices.TryGetValue(variable.ParentGroup, out var groupIndex))
                    {
                        var indexInGroup = variable.ParentGroup.Items.IndexOf(variable);
                        inGroupDeclarations.Add(new VariableDeclaration { m_Model = variable, m_GroupGUID = variable.ParentGroup.Guid, m_GroupIndex = groupIndex, m_IndexInGroup = indexInGroup });
                    }
                }

                inGroupDeclarations.Sort((a, b) => a.m_IndexInGroup.CompareTo(b.m_IndexInGroup)); // make the variable go in the right order so that the inserts to indexInGroup are always valid.
            }

            declarations.AddRange(inGroupDeclarations);

            foreach (var o in originalNodes)
            {
                o.OnBeforeCopy();
            }
            foreach (var o in wiresToCopy)
            {
                o.OnBeforeCopy();
            }
            foreach (var o in declarations)
            {
                o.m_Model.OnBeforeCopy();
            }
            foreach (var o in implicitVariableDeclarations)
            {
                o.OnBeforeCopy();
            }
            foreach (var o in groupsToCopy)
            {
                o.OnBeforeCopy();
            }
            foreach (var o in stickyNotesToCopy)
            {
                o.OnBeforeCopy();
            }
            foreach (var o in placematsToCopy)
            {
                o.OnBeforeCopy();
            }

            m_ShouldRunOnAfterCopy = true;

            m_TopLeftNodePosition = topLeftNodePosition;
            m_Nodes = originalNodes;
            m_Wires = wiresToCopy;
            m_VariableDeclarations = declarations;
            m_ImplicitVariableDeclarations = implicitVariableDeclarations;
            m_VariableGroupPaths = groupPaths;
            m_VariableGroups = groupsToCopy;
            m_StickyNotes = stickyNotesToCopy;
            m_Placemats = placematsToCopy;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        protected virtual void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            if (!disposing)
                return;

            try
            {
                if (m_ShouldRunOnAfterCopy)
                {
                    foreach (var o in m_Nodes)
                    {
                        o.OnAfterCopy();
                    }

                    foreach (var o in m_Wires)
                    {
                        o.OnAfterCopy();
                    }

                    foreach (var o in m_VariableDeclarations)
                    {
                        o.m_Model.OnAfterCopy();
                    }

                    foreach (var o in m_ImplicitVariableDeclarations)
                    {
                        o.OnAfterCopy();
                    }

                    foreach (var o in m_VariableGroups)
                    {
                        o.OnAfterCopy();
                    }

                    foreach (var o in m_StickyNotes)
                    {
                        o.OnAfterCopy();
                    }

                    foreach (var o in m_Placemats)
                    {
                        o.OnAfterCopy();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                m_Disposed = true;
            }
        }

        static void RecursiveAddGroups(ref int i, List<GroupModel> groups)
        {
            var group = groups[i];

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var childGroup in group.Items.OfType<GroupModel>())
#pragma warning restore UA2001
            {
                groups.Insert(++i, childGroup);
                RecursiveAddGroups(ref i, groups);
            }
        }

        static void RecurseAddMapping(Dictionary<Hash128, AbstractNodeModel> elementMapping, AbstractNodeModel originalElement, AbstractNodeModel newElement)
        {
            elementMapping[originalElement.Guid] = newElement;

            if (newElement is IGraphElementContainer container)
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                foreach (var subElement in ((IGraphElementContainer)originalElement).GetGraphElementModels().Zip(
#pragma warning restore UA2001
                    container.GetGraphElementModels(), (a, b) => new { originalElement = a, newElement = b }))
                {
                    if (subElement.originalElement is AbstractNodeModel originalSubElement && subElement.newElement is AbstractNodeModel newSubElement)
                        RecurseAddMapping(elementMapping, originalSubElement, newSubElement);
                }
        }

        /// <summary>
        /// Paste the data into a graph.
        /// </summary>
        /// <param name="operation">The kind of operation.</param>
        /// <param name="delta">The position delta to apply to new elements.</param>
        /// <param name="bbUpdater">The blackboard updater.</param>
        /// <param name="selectionStateUpdater">The selection updater.</param>
        /// <param name="copyPasteData">The data to paste.</param>
        /// <param name="graphModel">The graph model.</param>
        /// <param name="selectedGroup">The currently selected group, which will receive pasted variables.</param>
        /// <param name="shouldAddCopyStr">Whether to add "Copy of" string to new placemats.</param>
        /// <param name="keepVariableDeclarationGuids">Whether the duplicated models should have the same guid as the source variable declarations.</param>
        /// <returns>A mapping between the original and copied nodes.</returns>
        public static Dictionary<Hash128, AbstractNodeModel> PasteSerializedData(
            PasteOperation operation, Vector2 delta,
            BlackboardViewStateComponent.StateUpdater bbUpdater,
            SelectionStateComponent.StateUpdater selectionStateUpdater,
            CopyPasteData copyPasteData,
            GraphModel graphModel,
            GroupModel selectedGroup, bool shouldAddCopyStr = true, bool keepVariableDeclarationGuids = false)
        {
            var elementMapping = new Dictionary<Hash128, AbstractNodeModel>();
            var declarationMapping = new Dictionary<string, VariableDeclarationModelBase>();
            var createdGroups = new List<GroupModel>();

            if (copyPasteData.m_VariableGroupPaths != null)
            {
                for (int i = 0; i < copyPasteData.m_VariableGroupPaths.Count; ++i)
                {
                    var groupPath = copyPasteData.m_VariableGroupPaths[i];
                    GroupModel newGroup;
                    if (groupPath.m_Path.Length == 2)
                    {
                        if (operation == PasteOperation.Duplicate)
                        {
                            graphModel.TryGetModelFromGuid(groupPath.m_OriginalGUID, out GroupModel originalGroup);
                            if (originalGroup != null) // If we duplicate try to put the new group next to the duplicated one.
                            {
                                var parentGroup = originalGroup.ParentGroup as GroupModel;

                                newGroup = graphModel.CreateGroup(groupPath.m_Path[^1], parentGroup: parentGroup);
                                parentGroup?.InsertItem(newGroup, parentGroup.Items.IndexOf(originalGroup) + 1);
                            }
                            else
                            {
                                var parentGroup = selectedGroup ?? graphModel.GetSectionModel(groupPath.m_Path[0]) ?? graphModel.SectionModels[0];
                                newGroup = graphModel.CreateGroup(groupPath.m_Path[^1], parentGroup: parentGroup);
                                parentGroup.InsertItem(newGroup);
                            }
                        }
                        else
                        {
                            var parentGroup = selectedGroup ?? graphModel.GetSectionModel(groupPath.m_Path[0]) ?? graphModel.SectionModels[0];
                            newGroup = graphModel.CreateGroup(groupPath.m_Path[^1], parentGroup: parentGroup);
                            parentGroup.InsertItem(newGroup);
                            bbUpdater?.SetGroupModelExpanded(parentGroup, true);
                        }
                    }
                    else
                    {
                        int j = copyPasteData.m_VariableGroupPaths.FindLastIndex(i - 1, t => t.m_Path.Length == groupPath.m_Path.Length - 1); // our parent group is always the first item above us that have one less path element.
                        var parentGroup = createdGroups[j];
                        newGroup = graphModel.CreateGroup(groupPath.m_Path[^1], parentGroup: parentGroup);
                        parentGroup.InsertItem(newGroup);
                    }

                    bbUpdater?.SetGroupModelExpanded(newGroup, false);
                    createdGroups.Add(newGroup);

                    // ReSharper disable once SuspiciousTypeConversion.Global
                    (newGroup as ICopyPasteCallbackReceiver)?.OnAfterPaste();
                }

                selectionStateUpdater?.SelectElements(createdGroups, true);
            }

            if (copyPasteData.m_VariableDeclarations is { Count: > 0 })
            {
                var variableDeclarationModels = copyPasteData.m_VariableDeclarations;
                var duplicatedModels = new List<VariableDeclarationModelBase>();

                foreach (var source in variableDeclarationModels)
                {
                    if (!graphModel.CanPasteVariable(source.m_Model))
                        continue;

                    var newDeclaration = graphModel.DuplicateGraphVariableDeclaration(source.m_Model, keepVariableDeclarationGuids);
                    duplicatedModels.Add(newDeclaration);
                    if (source.m_GroupIndex >= 0) // if we have a valid groupIndex, it means we are in a duplicated group
                    {
                        createdGroups[source.m_GroupIndex].InsertItem(duplicatedModels[^1], source.m_IndexInGroup);
                    }
                    else if (operation == PasteOperation.Duplicate && graphModel.TryGetModelFromGuid(source.m_GroupGUID, out GroupModel group)) // If we duplicate in the same graph, put the new variable in the same group, after the original.
                    {
                        group.InsertItem(duplicatedModels[^1]);
                    }
                    else
                    {
                        selectedGroup?.InsertItem(duplicatedModels[^1]);
                    }

                    declarationMapping[source.m_Model.Guid.ToString()] = duplicatedModels[^1];

                    (newDeclaration as ICopyPasteCallbackReceiver)?.OnAfterPaste();
                }

                selectionStateUpdater?.SelectElements(duplicatedModels, true);
            }

            if (copyPasteData.m_ImplicitVariableDeclarations is { Count: > 0 })
            {
                var variableDeclarationModels =
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    copyPasteData.m_ImplicitVariableDeclarations.ToList();
#pragma warning restore UA2001
                var duplicatedModels = new List<VariableDeclarationModelBase>();

                foreach (var source in variableDeclarationModels)
                {
                    if (!graphModel.TryGetModelFromGuid(source.Guid, out VariableDeclarationModelBase variable))
                    {
                        if (source.IsCopiable() && graphModel.CanPasteVariable(source))
                        {
                            var newDeclaration = graphModel.DuplicateGraphVariableDeclaration(source, true);
                            duplicatedModels.Add(newDeclaration);
                            declarationMapping[source.Guid.ToString()] = duplicatedModels[^1];

                            (newDeclaration as ICopyPasteCallbackReceiver)?.OnAfterPaste();
                        }
                    }
                    else
                    {
                        declarationMapping[source.Guid.ToString()] = variable;
                    }
                }
                selectionStateUpdater?.SelectElements(duplicatedModels, true);
            }

            var copyStr = shouldAddCopyStr ? "Copy of " : string.Empty;
            Dictionary<Hash128, DeclarationModel> portalDeclarations = new Dictionary<Hash128, DeclarationModel>();
            List<WirePortalModel> portalModels = new List<WirePortalModel>();
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            List<WirePortalModel> existingPortalNodes = graphModel.NodeModels.OfType<WirePortalModel>().ToList();
#pragma warning restore UA2001

            if (copyPasteData.m_Nodes != null)
            {
                foreach (var originalModel in copyPasteData.m_Nodes)
                {
                    if (!graphModel.CanPasteNode(originalModel))
                        continue;
                    if (originalModel.NeedsContainer())
                        continue;
                    if (!graphModel.AllowPortalCreation && originalModel is WirePortalModel)
                        continue;
                    if (!graphModel.AllowSubgraphCreation && originalModel is SubgraphNodeModel)
                        continue;

                    VariableDeclarationModelBase declarationModel = null;
                    var variableNode = originalModel as VariableNodeModel;
                    if (variableNode != null)
                    {
                        if (variableNode.VariableDeclarationModel is ExternalVariableDeclarationModelBase
                            nodeVariableDeclarationModel)
                        {
                            foreach (var variableDeclaration in graphModel.VariableDeclarations)
                            {
                                if (variableDeclaration is ExternalVariableDeclarationModelBase candidate &&
                                    candidate.RefersToSameVariableAs(nodeVariableDeclarationModel))
                                {
                                    declarationModel = variableDeclaration;
                                    break;
                                }
                            }

                            if (declarationModel == null)
                                continue;
                        }
                        else
                        {
                            if (!declarationMapping.TryGetValue(variableNode.VariableDeclarationModel.Guid.ToString(),
                                    out declarationModel))
                                continue;
                            if (!graphModel.CanCreateVariableNode(variableNode.VariableDeclarationModel, graphModel))
                                continue;
                        }
                    }

                    var pastedNode = graphModel.DuplicateNode(originalModel, delta);

                    if (pastedNode is WirePortalModel portalNodeModel)
                    {
                        if (portalDeclarations.TryGetValue(portalNodeModel.DeclarationModel.Guid,
                                out var newDeclaration))
                        {
                            portalNodeModel.SetDeclarationModel(newDeclaration);
                        }
                        // If the node can not have another portal with the same direction and declaration ( is a data input ) and there is already
                        // one portal node with the same direction and the same Declaration.
                        else if (!portalNodeModel.CanHaveAnotherPortalWithSameDirectionAndDeclaration() &&
                                 graphModel.NodeModels.Exists(t => t is WirePortalModel tWirePortalModel &&
                                                                   tWirePortalModel != pastedNode &&
                                                                   tWirePortalModel.DeclarationModel.Guid ==
                                                                   portalNodeModel.DeclarationModel.Guid &&
                                                                   tWirePortalModel is ISingleOutputPortNodeModel ==
                                                                   portalNodeModel is ISingleOutputPortNodeModel)

                                 // Or if there is in the pasted node, a node with the opposite direction that share the same declaration
                                 ||
                                 (
                                     copyPasteData.m_Nodes.Exists(t => t is WirePortalModel tWirePortalModel &&
                                                                       tWirePortalModel.DeclarationModel.Guid ==
                                                                       portalNodeModel.DeclarationModel.Guid &&
                                                                       tWirePortalModel is ISingleOutputPortNodeModel !=
                                                                       portalNodeModel is ISingleOutputPortNodeModel)
                                 )
                                )
                        {
                            var declaration = graphModel.DuplicatePortal(portalNodeModel.DeclarationModel);
                            declaration.Title = copyStr + portalNodeModel.Title;

                            portalDeclarations[portalNodeModel.DeclarationModel.Guid] = declaration;
                            portalNodeModel.SetDeclarationModel(declaration);

                            (declaration as ICopyPasteCallbackReceiver)?.OnAfterPaste();
                        }
                        else
                        {
                            portalModels.Add(portalNodeModel);
                        }
                    }

                    if (variableNode != null)
                    {
                        ((VariableNodeModel)pastedNode).SetVariableDeclarationModel(declarationModel);
                    }

                    selectionStateUpdater?.SelectElement(pastedNode, true);
                    RecurseAddMapping(elementMapping, originalModel, pastedNode);

                    (pastedNode as ICopyPasteCallbackReceiver)?.OnAfterPaste();
                }
            }

            if (portalModels != null)
            {
                foreach (var portal in portalModels)
                {
                    // The exit was duplicated as well as the entry, link them.
                    if (portalDeclarations.TryGetValue(portal.DeclarationModel.Guid, out var newDeclaration))
                    {
                        portal.SetDeclarationModel(newDeclaration);
                    }
                    else
                    {
                        // If the exit match an entry still in the graph.
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        var existingEntry = existingPortalNodes.FirstOrDefault(t =>
                            t.DeclarationModel.Guid == portal.DeclarationModel.Guid);
#pragma warning restore UA2001
                        if (existingEntry != null)
                            portal.SetDeclarationModel(existingEntry.DeclarationModel);
                        else // we have an orphan exit. Create a unique declarationModel for it.
                        {
                            var declarationModel = portal.DeclarationModel;
                            portal.SetDeclarationModel(graphModel.DuplicatePortal(portal.DeclarationModel));
                            portalDeclarations[declarationModel.Guid] = portal.DeclarationModel;

                            (portal.DeclarationModel as ICopyPasteCallbackReceiver)?.OnAfterPaste();
                        }
                    }
                }
            }

            // Avoid using sourceWire.FromPort and sourceWire.ToPort since the wire does not have sufficient context
            // to resolve the PortModel from the PortReference (the wire is not in a GraphModel).
            if (copyPasteData.m_Wires != null)
            {
                foreach (var wire in copyPasteData.m_Wires)
                {
                    graphModel.TryGetModelFromGuid(wire.ToNodeGuid, out var originalToNode);
                    graphModel.TryGetModelFromGuid(wire.FromNodeGuid, out var originalFromNode);
                    if (!graphModel.AllowPortalCreation &&
                        (originalToNode is WirePortalModel || originalFromNode is WirePortalModel))
                        continue;
                    if (!graphModel.AllowSubgraphCreation &&
                        (originalToNode is SubgraphNodeModel || originalFromNode is SubgraphNodeModel))
                        continue;

                    elementMapping.TryGetValue(wire.ToNodeGuid, out var newInput);
                    elementMapping.TryGetValue(wire.FromNodeGuid, out var newOutput);

                    var copiedWire = graphModel.DuplicateWire(wire, newInput, newOutput);
                    if (copiedWire != null)
                    {
                        selectionStateUpdater?.SelectElement(copiedWire, true);

                        // ReSharper disable once SuspiciousTypeConversion.Global
                        (copiedWire as ICopyPasteCallbackReceiver)?.OnAfterPaste();
                    }
                }
            }

            if (copyPasteData.m_StickyNotes != null)
            {
                foreach (var stickyNote in copyPasteData.m_StickyNotes)
                {
                    var newPosition = new Rect(stickyNote.PositionAndSize.position + delta,
                        stickyNote.PositionAndSize.size);
                    var pastedStickyNote = graphModel.CreateStickyNote(newPosition);
                    pastedStickyNote.Title = stickyNote.Title;
                    pastedStickyNote.Contents = stickyNote.Contents;
                    pastedStickyNote.Theme = stickyNote.Theme;
                    pastedStickyNote.TextSize = stickyNote.TextSize;
                    selectionStateUpdater?.SelectElement(pastedStickyNote, true);

                    // ReSharper disable once SuspiciousTypeConversion.Global
                    (pastedStickyNote as ICopyPasteCallbackReceiver)?.OnAfterPaste();
                }
            }

            // Keep placemats relative order
            if (copyPasteData.m_Placemats != null)
            {
                foreach (var placemat in copyPasteData.m_Placemats)
                {
                    var newPosition = new Rect(placemat.PositionAndSize.position + delta,
                        placemat.PositionAndSize.size);
                    var newTitle = copyStr + placemat.Title;
                    var pastedPlacemat = graphModel.CreatePlacemat(newPosition);
                    PlacematModel.CopyPlacematParameters(placemat, pastedPlacemat);
                    pastedPlacemat.Title = newTitle;
                    selectionStateUpdater?.SelectElement(pastedPlacemat, true);

                    // ReSharper disable once SuspiciousTypeConversion.Global
                    (pastedPlacemat as ICopyPasteCallbackReceiver)?.OnAfterPaste();
                }
            }

            return elementMapping;
        }

        /// <summary>
        /// Removes all the wires from the data.
        /// </summary>
        public void RemoveWires()
        {
            m_Wires.Clear();
        }

        internal class TestAccess
        {
            public readonly CopyPasteData CopyPasteData;

            public TestAccess(CopyPasteData copyPasteData)
            {
                CopyPasteData = copyPasteData;
            }

            public void SetNodes(List<AbstractNodeModel> nodes)
            {
                CopyPasteData.m_Nodes = nodes;
            }
        }
    }
}
