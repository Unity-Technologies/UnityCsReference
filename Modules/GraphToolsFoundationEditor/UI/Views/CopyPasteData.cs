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
    /// Class used to hold copy paste data.
    /// </summary>
    [Serializable]
    class CopyPasteData
    {
        [SerializeReference]
        List<AbstractNodeModel> m_Nodes;

        [SerializeReference]
        List<WireModel> m_Wires;

        [Serializable]
        internal struct VariableDeclaration_Internal
        {
            [SerializeReference]
            public VariableDeclarationModel m_Model;

            [SerializeField]
            public SerializableGUID m_GroupGUID;

            [SerializeField]
            public int m_GroupIndex;

            [SerializeField]
            public int m_IndexInGroup;
        }

        [Serializable]
        internal struct GroupPath_Internal
        {
            [SerializeField]
            public SerializableGUID m_OriginalGUID;

            [SerializeField]
            public string[] m_Path;

            [SerializeField]
            public bool m_Expanded;
        }

        [SerializeField]
        List<GroupPath_Internal> m_VariableGroupPaths;

        [SerializeField]
        List<VariableDeclaration_Internal> m_VariableDeclarations;

        [SerializeReference]
        List<VariableDeclarationModel> m_ImplicitVariableDeclarations;

        [SerializeField]
        Vector2 m_TopLeftNodePosition;

        [SerializeReference]
        List<StickyNoteModel> m_StickyNotes;

        [SerializeReference]
        List<PlacematModel> m_Placemats;

        /// <summary>
        /// The position where the top-left-most node will be created.
        /// </summary>
        public Vector2 TopLeftNodePosition => m_TopLeftNodePosition;

        /// <summary>
        /// The <see cref="AbstractNodeModel"/>s to paste.
        /// </summary>
        public IReadOnlyList<AbstractNodeModel> Nodes => m_Nodes;

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
        /// The explicitly selected <see cref="VariableDeclarationModel"/>s to paste.
        /// </summary>
        public IReadOnlyList<VariableDeclarationModel> VariableDeclarations => m_VariableDeclarations.Select(v => v.m_Model).ToList();

        /// <summary>
        /// The implicitly selected <see cref="VariableDeclarationModel"/>s to paste, usually from <see cref="VariableNodeModel"/>s.
        /// </summary>
        public IReadOnlyList<VariableDeclarationModel> ImplicitVariableDeclarations => m_ImplicitVariableDeclarations;

        /// <summary>
        /// Whether there is any model to paste.
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty() => (!m_Nodes.Any() && !m_Wires.Any() &&
            !m_VariableDeclarations.Any() && !m_StickyNotes.Any() && !m_Placemats.Any() && !m_VariableGroupPaths.Any());

        internal bool HasVariableContent_Internal()
        {
            return m_VariableDeclarations.Any() || m_VariableGroupPaths.Any();
        }

        internal static CopyPasteData GatherCopiedElementsData_Internal(BlackboardViewStateComponent bbState, IReadOnlyCollection<Model> graphElementModels)
        {
            var originalNodes = graphElementModels.OfType<AbstractNodeModel>().ToList();

            var variableDeclarationsToCopy = graphElementModels
                .OfType<VariableDeclarationModel>()
                .ToList();

            var stickyNotesToCopy = graphElementModels
                .OfType<StickyNoteModel>()
                .ToList();

            var placematsToCopy = graphElementModels
                .OfType<PlacematModel>()
                .ToList();

            var wiresToCopy = graphElementModels
                .OfType<WireModel>()
                .ToList();

            var implicitVariableDeclarations = originalNodes.OfType<VariableNodeModel>()
                .Select(t => t.VariableDeclarationModel).Except(variableDeclarationsToCopy).ToList();

            var topLeftNodePosition = Vector2.positiveInfinity;
            foreach (var n in originalNodes.Where(t => t.IsMovable()))
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

            var originalGroups = graphElementModels.OfType<GroupModel>().ToList();

            var groups = new List<GroupModel>();

            originalGroups.Sort(GroupItemOrderComparer_Internal.Default);

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

            var groupIndices = new Dictionary<GroupModel, int>();
            var cpt = 0;
            var groupPaths = new List<GroupPath_Internal>();
            var groupPath = new List<String>();

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

                groupPaths.Add(new GroupPath_Internal() { m_OriginalGUID = group.Guid, m_Path = groupPath.ToArray(), m_Expanded = bbState?.GetGroupExpanded(group) ?? true });
                groupIndices[group] = cpt++;
            }

            var declarations = new List<VariableDeclaration_Internal>(variableDeclarationsToCopy.Count);

            //First add all variables outside of copied groups
            foreach (var variable in variableDeclarationsToCopy)
            {
                var group = variable.ParentGroup;

                if (groupIndices.ContainsKey(group))
                    continue;

                declarations.Add(new VariableDeclaration_Internal { m_Model = variable, m_GroupGUID = group.Guid, m_GroupIndex = -1, m_IndexInGroup = -1 });
            }

            var inGroupDeclarations = new List<VariableDeclaration_Internal>();
            if (groups.Any())
            {
                var graphModel = groups.First().GraphModel;

                foreach (var variable in graphModel.VariableDeclarations)
                {
                    if (groupIndices.TryGetValue(variable.ParentGroup, out var groupIndex))
                    {
                        var indexInGroup = variable.ParentGroup.Items.IndexOf_Internal(variable);
                        inGroupDeclarations.Add(new VariableDeclaration_Internal { m_Model = variable, m_GroupGUID = variable.ParentGroup.Guid, m_GroupIndex = groupIndex, m_IndexInGroup = indexInGroup });
                    }
                }

                inGroupDeclarations.Sort((a, b) => a.m_IndexInGroup.CompareTo(b.m_IndexInGroup)); // make the variable go in the right order so that the inserts to indexInGroup are always valid.
            }

            declarations.AddRange(inGroupDeclarations);

            var copyPasteData = new CopyPasteData
            {
                m_TopLeftNodePosition = topLeftNodePosition,
                m_Nodes = originalNodes,
                m_Wires = wiresToCopy,
                m_VariableDeclarations = declarations,
                m_ImplicitVariableDeclarations = implicitVariableDeclarations,
                m_VariableGroupPaths = groupPaths,
                m_StickyNotes = stickyNotesToCopy,
                m_Placemats = placematsToCopy
            };

            return copyPasteData;
        }

        static void RecursiveAddGroups(ref int i, List<GroupModel> groups)
        {
            var group = groups[i];

            foreach (var childGroup in group.Items.OfType<GroupModel>())
            {
                groups.Insert(++i, childGroup);
                RecursiveAddGroups(ref i, groups);
            }
        }

        static void RecurseAddMapping(Dictionary<string, GraphElementModel> elementMapping, GraphElementModel originalElement, GraphElementModel newElement)
        {
            elementMapping[originalElement.Guid.ToString()] = newElement;

            if (newElement is IGraphElementContainer container)
                foreach (var subElement in ((IGraphElementContainer)originalElement).GraphElementModels.Zip(
                             container.GraphElementModels, (a, b) => new { originalElement = a, newElement = b }))
                {
                    RecurseAddMapping(elementMapping, subElement.originalElement, subElement.newElement);
                }
        }

        /// <summary>
        /// Paste the data into a graph.
        /// </summary>
        /// <param name="operation">The kind of operation.</param>
        /// <param name="delta">The position delta to apply to new elements.</param>
        /// <param name="graphModelStateUpdater">The graph model updater.</param>
        /// <param name="bbUpdater">The blackboard updater.</param>
        /// <param name="selectionStateUpdater">The selection updater.</param>
        /// <param name="copyPasteData">The data to paste.</param>
        /// <param name="graphModel">The graph model.</param>
        /// <param name="selectedGroup">The currently selected group, which will receive pasted variables.</param>
        /// <returns>A dictionary that maps original element guids to newly pasted models.</returns>
        public static Dictionary<string, GraphElementModel> PasteSerializedData(
            PasteOperation operation, Vector2 delta,
            GraphModelStateComponent.StateUpdater graphModelStateUpdater,
            BlackboardViewStateComponent.StateUpdater bbUpdater,
            SelectionStateComponent.StateUpdater selectionStateUpdater,
            CopyPasteData copyPasteData, GraphModel graphModel, GroupModel selectedGroup)
        {
            var elementMapping = new Dictionary<string, GraphElementModel>();

            var declarationMapping = new Dictionary<string, VariableDeclarationModel>();

            List<GroupModel> createdGroups = new List<GroupModel>();

            using var changeScope = graphModel.ChangeDescriptionScope;

            if (copyPasteData.m_VariableGroupPaths != null)
            {
                for (int i = 0; i < copyPasteData.m_VariableGroupPaths.Count; ++i)
                {
                    var groupPath = copyPasteData.m_VariableGroupPaths[i];
                    var newGroup = graphModel.CreateGroup(groupPath.m_Path.Last());
                    if (groupPath.m_Path.Length == 2)
                    {
                        if (operation == PasteOperation.Duplicate)
                        {
                            graphModel.TryGetModelFromGuid(groupPath.m_OriginalGUID, out GroupModel originalGroup);
                            if (originalGroup != null) // If we duplicate try to put the new group next to the duplicated one.
                            {
                                var parentGroup = originalGroup.ParentGroup;
                                parentGroup.InsertItem(newGroup, parentGroup.Items.IndexOf_Internal(originalGroup) + 1);
                            }
                            else
                            {
                                var parentGroup = selectedGroup ?? graphModel.GetSectionModel(groupPath.m_Path[0]) ?? graphModel.SectionModels.First();
                                parentGroup.InsertItem(newGroup);
                            }
                        }
                        else
                        {
                            var parentGroup = selectedGroup ?? graphModel.GetSectionModel(groupPath.m_Path[0]) ?? graphModel.SectionModels.First();
                            parentGroup.InsertItem(newGroup);
                            bbUpdater?.SetGroupModelExpanded(parentGroup, true);
                        }
                    }
                    else
                    {
                        int j = copyPasteData.m_VariableGroupPaths.FindLastIndex(i - 1, t => t.m_Path.Length == groupPath.m_Path.Length - 1); // our parent group is always the first item above us that have one less path element.
                        var parentGroup = createdGroups[j];
                        parentGroup.InsertItem(newGroup);
                    }

                    bbUpdater?.SetGroupModelExpanded(newGroup, groupPath.m_Expanded);
                    createdGroups.Add(newGroup);
                    selectionStateUpdater.SelectElement(newGroup, true);
                }
            }

            if (copyPasteData.m_VariableDeclarations.Any())
            {
                List<VariableDeclaration_Internal> variableDeclarationModels =
                    copyPasteData.m_VariableDeclarations.ToList();
                List<VariableDeclarationModel> duplicatedModels = new List<VariableDeclarationModel>();

                foreach (var source in variableDeclarationModels)
                {
                    if (!graphModel.Stencil.CanPasteVariable(source.m_Model, graphModel))
                        break;
                    duplicatedModels.Add(graphModel.DuplicateGraphVariableDeclaration(source.m_Model));
                    if (source.m_GroupIndex >= 0) // if we have a valid groupIndex, it means we are in a duplicated group
                    {
                        createdGroups[source.m_GroupIndex].InsertItem(duplicatedModels.Last(), source.m_IndexInGroup);
                    }
                    else if (operation == PasteOperation.Duplicate && graphModel.TryGetModelFromGuid(source.m_GroupGUID, out GroupModel group)) // If we duplicate in the same graph, put the new variable in the same group, after the original.
                    {
                        group.InsertItem(duplicatedModels.Last());
                    }
                    else
                    {
                        selectedGroup?.InsertItem(duplicatedModels.Last());
                    }

                    declarationMapping[source.m_Model.Guid.ToString()] = duplicatedModels.Last();
                }

                var duplicatedParents = new HashSet<GroupModel>(duplicatedModels.Select(t => t.ParentGroup));

                foreach (var duplicatedParent in duplicatedParents)
                {
                    var current = duplicatedParent;
                    while (current != null)
                    {
                        bbUpdater?.SetGroupModelExpanded(current, true);
                        current = current.ParentGroup;
                    }
                }
                selectionStateUpdater?.SelectElements(duplicatedModels, true);
            }

            if (copyPasteData.m_ImplicitVariableDeclarations.Any())
            {
                List<VariableDeclarationModel> variableDeclarationModels =
                    copyPasteData.m_ImplicitVariableDeclarations.ToList();
                List<VariableDeclarationModel> duplicatedModels = new List<VariableDeclarationModel>();

                foreach (var source in variableDeclarationModels)
                {
                    if (!graphModel.TryGetModelFromGuid(source.Guid, out VariableDeclarationModel variable))
                    {
                        if (graphModel.Stencil.CanPasteVariable(source, graphModel))
                        {
                            duplicatedModels.Add(graphModel.DuplicateGraphVariableDeclaration(source, true));
                            declarationMapping[source.Guid.ToString()] = duplicatedModels.Last();
                        }
                    }
                    else
                    {
                        declarationMapping[source.Guid.ToString()] = variable;
                    }
                }
                selectionStateUpdater?.SelectElements(duplicatedModels, true);
            }

            Dictionary<SerializableGUID, DeclarationModel> portalDeclarations = new Dictionary<SerializableGUID, DeclarationModel>();
            List<WirePortalModel> portalModels = new List<WirePortalModel>();
            List<WirePortalModel> existingPortalNodes = graphModel.NodeModels.OfType<WirePortalModel>().ToList();

            foreach (var originalModel in copyPasteData.m_Nodes)
            {
                if (!graphModel.Stencil.CanPasteNode(originalModel, graphModel))
                    continue;
                if (originalModel.NeedsContainer())
                    continue;

                VariableDeclarationModel declarationModel = null;
                var variableNode = originalModel as VariableNodeModel;
                if (variableNode != null)
                {
                    if (!declarationMapping.TryGetValue(variableNode.VariableDeclarationModel.Guid.ToString(), out declarationModel))
                        continue;
                    if (!((Stencil)graphModel.Stencil).CanCreateVariableNode(variableNode.VariableDeclarationModel, graphModel))
                        continue;
                }

                var pastedNode = graphModel.DuplicateNode(originalModel, delta);

                if (pastedNode is WirePortalModel portalNodeModel)
                {
                    if (portalDeclarations.TryGetValue(portalNodeModel.DeclarationModel.Guid, out var newDeclaration))
                    {
                        portalNodeModel.DeclarationModel = newDeclaration;
                    }
                    else if (!portalNodeModel.CanHaveAnotherPortalWithSameDirectionAndDeclaration() ||
                             (portalNodeModel is ISingleInputPortNodeModel &&
                                 copyPasteData.m_Nodes.Any(t=> t is ISingleOutputPortNodeModel and WirePortalModel exit &&
                                     ReferenceEquals(exit.DeclarationModel, portalNodeModel.DeclarationModel))))
                    {
                        var declaration = graphModel.DuplicatePortal(portalNodeModel.DeclarationModel);

                        portalDeclarations[portalNodeModel.DeclarationModel.Guid] = declaration;
                        portalNodeModel.DeclarationModel = declaration;
                    }
                    else
                    {
                        portalModels.Add(portalNodeModel);
                    }
                }

                if (variableNode != null)
                {
                    ((VariableNodeModel)pastedNode).VariableDeclarationModel = declarationModel;
                }

                selectionStateUpdater?.SelectElements(new[] { pastedNode }, true);
                RecurseAddMapping(elementMapping, originalModel, pastedNode);
            }

            foreach (var portal in portalModels)
            {
                // The exit was duplicated as well as the entry, link them.
                if (portalDeclarations.TryGetValue(portal.DeclarationModel.Guid, out var newDeclaration))
                {
                    portal.DeclarationModel = newDeclaration;
                }
                else
                {
                    // If the exit match an entry still in the graph.
                    var existingEntry = existingPortalNodes.FirstOrDefault(t => t.DeclarationModel.Guid == portal.DeclarationModel.Guid);
                    if (existingEntry != null)
                        portal.DeclarationModel = existingEntry.DeclarationModel;
                    else // we have an orphan exit. Create a unique declarationModel for it.
                    {
                        var declarationModel = portal.DeclarationModel;
                        portal.DeclarationModel = graphModel.DuplicatePortal(portal.DeclarationModel);
                        portalDeclarations[declarationModel.Guid] = portal.DeclarationModel;
                    }
                }
            }

            // Avoid using sourceWire.FromPort and sourceWire.ToPort since the wire does not have sufficient context
            // to resolve the PortModel from the PortReference (the wire is not in a GraphModel).
            foreach (var wire in copyPasteData.m_Wires)
            {
                elementMapping.TryGetValue(wire.ToNodeGuid.ToString(), out var newInput);
                elementMapping.TryGetValue(wire.FromNodeGuid.ToString(), out var newOutput);

                var copiedWire = graphModel.DuplicateWire(wire, newInput as AbstractNodeModel, newOutput as AbstractNodeModel);
                if (copiedWire != null)
                {
                    elementMapping.Add(wire.Guid.ToString(), copiedWire);
                    selectionStateUpdater?.SelectElements(new[] { copiedWire }, true);
                }
            }

            foreach (var stickyNote in copyPasteData.m_StickyNotes)
            {
                var newPosition = new Rect(stickyNote.PositionAndSize.position + delta, stickyNote.PositionAndSize.size);
                var pastedStickyNote = graphModel.CreateStickyNote(newPosition);
                pastedStickyNote.Title = stickyNote.Title;
                pastedStickyNote.Contents = stickyNote.Contents;
                pastedStickyNote.Theme = stickyNote.Theme;
                pastedStickyNote.TextSize = stickyNote.TextSize;
                selectionStateUpdater?.SelectElements(new[] { pastedStickyNote }, true);
                elementMapping.Add(stickyNote.Guid.ToString(), pastedStickyNote);
            }

            // Keep placemats relative order
            foreach (var placemat in copyPasteData.m_Placemats)
            {
                var newPosition = new Rect(placemat.PositionAndSize.position + delta, placemat.PositionAndSize.size);
                var newTitle = "Copy of " + placemat.Title;
                var pastedPlacemat = graphModel.CreatePlacemat(newPosition);
                PlacematModel.CopyPlacematParameters(placemat,pastedPlacemat);
                pastedPlacemat.Title = newTitle;
                selectionStateUpdater?.SelectElements(new[] { pastedPlacemat }, true);
                elementMapping.Add(placemat.Guid.ToString(), pastedPlacemat);
            }

            graphModelStateUpdater?.MarkUpdated(changeScope.ChangeDescription);
            return elementMapping;
        }
    }
}
