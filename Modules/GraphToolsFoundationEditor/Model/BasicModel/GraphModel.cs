// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Spawn flags dictates multiple operations during the NodeModels creation.
    /// </summary>
    [Flags]
    enum SpawnFlags
    {
        None = 0,
        Reserved0 = 1 << 0,
        Reserved1 = 1 << 1,
        /// <summary>
        /// The created NodeModel is not added to a Graph. Useful for display only purposes.
        /// </summary>
        Orphan = 1 << 2,
        /// <summary>
        /// Equivalent to None.
        /// </summary>
        Default = None,
    }

    /// <summary>
    /// Verbosity of <see cref="GraphModel.CheckIntegrity"/>.
    /// </summary>
    enum Verbosity
    {
        Errors,
        Verbose
    }

    /// <summary>
    /// Extension methods for <see cref="SpawnFlags"/>.
    /// </summary>
    static class SpawnFlagsExtensions
    {
        /// <summary>
        /// Whether <paramref name="f"/> has the <see cref="SpawnFlags.Orphan"/> set.
        /// </summary>
        /// <param name="f">The flag set to check.</param>
        /// <returns>True if <paramref name="f"/> has the <see cref="SpawnFlags.Orphan"/> set.</returns>
        public static bool IsOrphan(this SpawnFlags f) => (f & SpawnFlags.Orphan) != 0;
    }

    /// <summary>
    /// A model that represents a graph.
    /// </summary>
    [Serializable]
    [MovedFrom(false, "Unity.GraphToolsFoundation.Editor", "Unity.GraphTools.Foundation.Model")]
    abstract class GraphModel : Model, IGraphElementContainer, ISerializationCallbackReceiver
    {
        static List<ChangeHint> s_GroupingChangeHint = new() { ChangeHint.Grouping };

        [SerializeReference]
        List<AbstractNodeModel> m_GraphNodeModels;

        [SerializeReference]
        List<BadgeModel> m_BadgeModels;

        [SerializeReference, FormerlySerializedAs("m_GraphEdgeModels")]
        List<WireModel> m_GraphWireModels;

        [SerializeReference]
        List<StickyNoteModel> m_GraphStickyNoteModels;

        [SerializeReference]
        List<PlacematModel> m_GraphPlacematModels;

        [SerializeReference]
        List<VariableDeclarationModel> m_GraphVariableModels;

        [SerializeReference]
        List<DeclarationModel> m_GraphPortalModels;

        [SerializeReference]
        List<SectionModel> m_SectionModels;

        /// <summary>
        /// Holds created variables names to make creation of unique names faster.
        /// </summary>
        HashSet<string> m_ExistingVariableNames;

        [SerializeField]
        [HideInInspector]
        string m_StencilTypeName; // serialized as string, resolved as type by ISerializationCallbackReceiver

        Type m_StencilType;

        // As this field is not serialized, use GetElementsByGuid() to access it.
        Dictionary<SerializableGUID, GraphElementModel> m_ElementsByGuid;

        PortWireIndex_Internal m_PortWireIndex;

        /// <summary>
        /// The default stencil type, used when <see cref="StencilType"/> is set to null.
        /// </summary>
        public virtual Type DefaultStencilType => null;

        /// <summary>
        ///  The stencil type. Used to instantiate a stencil for this graph.
        /// </summary>
        public Type StencilType
        {
            get => m_StencilType;
            set
            {
                if (value == null)
                    value = DefaultStencilType;
                Assert.IsTrue(typeof(StencilBase).IsAssignableFrom(value));
                m_StencilType = value;

                Stencil = (StencilBase)Activator.CreateInstance(m_StencilType);
                Assert.IsNotNull(Stencil);
                Stencil.GraphModel = this;
            }
        }

        /// <summary>
        /// The stencil for this graph.
        /// </summary>
        public StencilBase Stencil { get; protected set; }

        /// <summary>
        /// The asset that holds this graph.
        /// </summary>
        /// <remarks>GTF currently assumes that each graph has a backing asset in the AssetDatabase.</remarks>
        public GraphAsset Asset { get; set; }

        /// <inheritdoc />
        public virtual IEnumerable<GraphElementModel> GraphElementModels => GetElementsByGuid().Values.Where(t => t.Container == this);

        /// <summary>
        /// The nodes of the graph.
        /// </summary>
        public virtual IReadOnlyList<AbstractNodeModel> NodeModels => m_GraphNodeModels;

        /// <summary>
        /// The nodes and blocks of the graph.
        /// </summary>
        public IEnumerable<AbstractNodeModel> NodeAndBlockModels
        {
            get
            {
                IEnumerable<AbstractNodeModel> allModels = NodeModels;
                foreach (var nodeModel in NodeModels)
                {
                    if (nodeModel is ContextNodeModel contextModel)
                    {
                        allModels = allModels.Concat(contextModel.GraphElementModels.OfType<AbstractNodeModel>());
                    }
                }

                return allModels;
            }
        }

        /// <summary>
        /// The wires of the graph.
        /// </summary>
        public virtual IReadOnlyList<WireModel> WireModels => m_GraphWireModels;

        /// <summary>
        /// The badges of the graph.
        /// </summary>
        public virtual IReadOnlyList<BadgeModel> BadgeModels => m_BadgeModels;

        /// <summary>
        /// The sticky note of the graph.
        /// </summary>
        public virtual IReadOnlyList<StickyNoteModel> StickyNoteModels => m_GraphStickyNoteModels;

        /// <summary>
        /// The placemats of the graph.
        /// </summary>
        public virtual IReadOnlyList<PlacematModel> PlacematModels => m_GraphPlacematModels;

        /// <summary>
        /// The variables of the graph.
        /// </summary>
        public virtual IReadOnlyList<VariableDeclarationModel> VariableDeclarations => m_GraphVariableModels;

        /// <summary>
        /// The portals of the graph.
        /// </summary>
        public virtual IReadOnlyList<DeclarationModel> PortalDeclarations => m_GraphPortalModels;

        /// <summary>
        /// All the section models.
        /// </summary>
        public virtual IReadOnlyList<SectionModel> SectionModels => m_SectionModels;

        /// <summary>
        /// The name of the current graph.
        /// </summary>
        public virtual string Name => Asset != null ? Asset.Name : "";

        public virtual Type GetSectionModelType()
        {
            return typeof(SectionModel);
        }

        protected virtual SectionModel InstantiateSection(string sectionName)
        {
            var section = Instantiate<SectionModel>(GetSectionModelType());
            section.Title = sectionName;
            section.GraphModel = this;
            return section;
        }

        public virtual SectionModel CreateSection(string sectionName)
        {
            var section = InstantiateSection(sectionName);
            AddSection(section);
            return section;
        }

        protected virtual void AddSection(SectionModel section)
        {
            RegisterElement(section);
            m_SectionModels.Add(section);
        }

        /// <summary>
        /// Gets a <see cref="SectionModel"/> by its name.
        /// </summary>
        /// <param name="sectionName">The name of the section.</param>
        /// <returns>The section model, or null if not found.</returns>
        public virtual SectionModel GetSectionModel(string sectionName = "")
        {
            return m_SectionModels.Find(t => t.Title == sectionName);
        }

        /// <summary>
        /// Checks that all variables are referenced in a group. Otherwise adds the variables in their valid section.
        /// Also cleans up no longer existing sections.
        /// </summary>
        internal void CheckGroupConsistency_Internal()
        {
            if (Stencil == null)
                return;

            void RecurseGetReferencedGroupItem<T>(GroupModel root, HashSet<T> result)
                where T : IGroupItemModel
            {
                foreach (var item in root.Items)
                {
                    if (item is T tItem)
                        result.Add(tItem);
                    if (item is GroupModel subGroup)
                        RecurseGetReferencedGroupItem(subGroup, result);
                }
            }

            var variablesInGroup = new HashSet<VariableDeclarationModel>();

            CleanupSections(Stencil.SectionNames);
            foreach (var group in SectionModels)
                RecurseGetReferencedGroupItem(group, variablesInGroup);

            if (VariableDeclarations == null) return;

            foreach (var variable in VariableDeclarations)
            {
                if (!variablesInGroup.Contains(variable))
                    GetSectionModel(Stencil.GetVariableSection(variable)).InsertItem(variable);
            }
        }

        /// <summary>
        /// The index that maps ports to the wires connected to them.
        /// </summary>
        internal PortWireIndex_Internal PortWireIndex_Internal => m_PortWireIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphModel"/> class.
        /// </summary>
        protected GraphModel()
        {
            AssignNewGuid();

            m_GraphNodeModels = new List<AbstractNodeModel>();
            m_GraphWireModels = new List<WireModel>();
            m_BadgeModels = new List<BadgeModel>();
            m_GraphStickyNoteModels = new List<StickyNoteModel>();
            m_GraphPlacematModels = new List<PlacematModel>();
            m_GraphVariableModels = new List<VariableDeclarationModel>();
            m_GraphPortalModels = new List<DeclarationModel>();
            m_SectionModels = new List<SectionModel>();
            m_ExistingVariableNames = new HashSet<string>();

            m_PortWireIndex = new PortWireIndex_Internal(this);
        }

        /// <summary>
        /// Gets the list of wires that are connected to a port.
        /// </summary>
        /// <param name="portModel">The port for which we want the list of connected wires.</param>
        /// <returns>The list of wires connected to the port.</returns>
        public virtual IReadOnlyList<WireModel> GetWiresForPort(PortModel portModel)
        {
            return m_PortWireIndex.GetWiresForPort(portModel);
        }

        /// <summary>
        /// Changes the order of a wire among its siblings.
        /// </summary>
        /// <param name="wireModel">The wire to move.</param>
        /// <param name="reorderType">The type of move to do.</param>
        internal void ReorderWire_Internal(WireModel wireModel, ReorderType reorderType)
        {
            var fromPort = wireModel.FromPort;
            if (fromPort != null && fromPort.HasReorderableWires)
            {
                PortWireIndex_Internal.ReorderWire(wireModel, reorderType);
                ApplyReorderToGraph(fromPort);
            }
        }

        /// <summary>
        /// Updates a wire when one of its port changes.
        /// </summary>
        /// <param name="wireModel">The wire to update.</param>
        /// <param name="oldPort">The previous port value.</param>
        /// <param name="port">The new port value.</param>
        internal void UpdateWire_Internal(WireModel wireModel, PortModel oldPort, PortModel port)
        {
            PortWireIndex_Internal.UpdateWire(wireModel, oldPort, port);

            // when moving a wire to a new node, make sure it gets stored matching its new place.
            if (oldPort != null && port != null
                && oldPort.NodeModel != port.NodeModel
                && ReferenceEquals(port, wireModel.FromPort)
                && wireModel.FromPort.HasReorderableWires)
            {
                ApplyReorderToGraph(wireModel.FromPort);
            }
        }

        /// <summary>
        /// Reorders some placemats around following a <see cref="ZOrderMove"/>.
        /// </summary>
        /// <param name="models">The placemats to reorder.</param>
        /// <param name="reorderType">The way to reorder placemats.</param>
        public virtual void ReorderPlacemats(IReadOnlyList<PlacematModel> models, ZOrderMove reorderType)
        {
            m_GraphPlacematModels.ReorderElements(models, (ReorderType)reorderType);
        }

        /// <summary>
        /// Reorders <see cref="m_GraphWireModels"/> after the <see cref="PortWireIndex_Internal"/> has been reordered.
        /// </summary>
        /// <param name="fromPort">The port from which the reordered wires start.</param>
        void ApplyReorderToGraph(PortModel fromPort)
        {
            var orderedList = PortWireIndex_Internal.GetWiresForPort(fromPort);
            if (orderedList.Count == 0)
                return;

            // How this works:
            // graph has wires [A, B, C, D, E, F] and [B, D, E] are reorderable wires
            // say D has been moved to first place by a user
            // reorderable wires have been reordered as [D, B, E]
            // find indices for any of (D, B, E) in the graph: [1, 3, 4]
            // place [D, B, E] at those indices, we get [A, D, C, B, E, F]

            var indices = new List<int>(orderedList.Count);

            // find the indices of every wire potentially affected by the reorder
            for (int i = 0; i < WireModels.Count; i++)
            {
                if (orderedList.Contains(WireModels[i]))
                    indices.Add(i);
            }

            // place every reordered wire at an index that is part of the collection.
            for (int i = 0; i < orderedList.Count; i++)
            {
                m_GraphWireModels[indices[i]] = orderedList[i];
            }
        }

        /// <summary>
        /// Determines whether two ports can be connected together by a wire.
        /// </summary>
        /// <param name="startPortModel">The port from which the wire would come from.</param>
        /// <param name="compatiblePortModel">The port to which the wire would got to.</param>
        /// <returns>True if the two ports can be connected. False otherwise.</returns>
        protected virtual bool IsCompatiblePort(PortModel startPortModel, PortModel compatiblePortModel)
        {
            if (startPortModel.Capacity == PortCapacity.None || compatiblePortModel.Capacity == PortCapacity.None)
                return false;

            var startWirePortalModel = startPortModel.NodeModel as WirePortalModel;

            if ((startPortModel.PortDataType == typeof(ExecutionFlow)) != (compatiblePortModel.PortDataType == typeof(ExecutionFlow)))
                return false;

            // No good if ports belong to same node that does not allow self connect
            if (compatiblePortModel == startPortModel ||
                (compatiblePortModel.NodeModel != null || startPortModel.NodeModel != null) &&
                !startPortModel.NodeModel.AllowSelfConnect && compatiblePortModel.NodeModel == startPortModel.NodeModel)
                return false;

            // No good if it's on the same portal either.
            if (compatiblePortModel.NodeModel is WirePortalModel wirePortalModel)
            {
                if (wirePortalModel.DeclarationModel.Guid == startWirePortalModel?.DeclarationModel.Guid)
                    return false;
            }

            // This is true for all ports
            if (compatiblePortModel.Direction == startPortModel.Direction ||
                compatiblePortModel.PortType != startPortModel.PortType)
                return false;

            return Stencil.CanAssignTo(compatiblePortModel.DataTypeHandle, startPortModel.DataTypeHandle);
        }

        /// <summary>
        /// Gets a list of ports that can be connected to <paramref name="startPortModel"/>.
        /// </summary>
        /// <param name="portModels">The list of candidate ports.</param>
        /// <param name="startPortModel">The port to which the connection originates (can be an input or output port).</param>
        /// <returns>A list of ports that can be connected to <paramref name="startPortModel"/>.</returns>
        public virtual List<PortModel> GetCompatiblePorts(IReadOnlyList<PortModel> portModels, PortModel startPortModel)
        {
            return portModels.Where(pModel =>
                {
                    return IsCompatiblePort(startPortModel, pModel);
                })
                .ToList();
        }
        /// <summary>
        /// Returns the dictionary associating a <see cref="GraphElementModel" /> with its GUID.
        /// </summary>
        /// <returns>the dictionary associating a <see cref="GraphElementModel" /> with its GUID.</returns>
        protected virtual Dictionary<SerializableGUID, GraphElementModel> GetElementsByGuid()
        {
            if (m_ElementsByGuid == null)
                BuildElementByGuidDictionary();

            return m_ElementsByGuid;
        }

        /// <summary>
        /// Registers an element so that the GraphModel can find it through its GUID.
        /// </summary>
        /// <param name="model">The model.</param>
        public virtual void RegisterElement(GraphElementModel model)
        {
            GetElementsByGuid().Add(model.Guid, model);
        }

        /// <summary>
        /// Unregisters an element so that the GraphModel can no longer find it through its GUID.
        /// </summary>
        /// <param name="model">The model.</param>
        public virtual void UnregisterElement(GraphElementModel model)
        {
            GetElementsByGuid().Remove(model.Guid);
        }

        /// <summary>
        /// Retrieves a graph element model from its GUID.
        /// </summary>
        /// <param name="guid">The guid of the model to retrieve.</param>
        /// <param name="model">The model matching the guid, or null if no model were found.</param>
        /// <returns>True if the model was found. False otherwise.</returns>
        public virtual bool TryGetModelFromGuid(SerializableGUID guid, out GraphElementModel model)
        {
            return GetElementsByGuid().TryGetValue(guid, out model);
        }

        /// <summary>
        /// Retrieves a graph element model of type <typeparamref name="T"/> from its GUID.
        /// </summary>
        /// <param name="guid">The guid of the model to retrieve.</param>
        /// <param name="model">The model matching the guid and type, or null if no model were found.</param>
        /// <typeparam name="T">The type of the model to retrieve.</typeparam>
        /// <returns>True if the model was found and is of the requested type. False otherwise.</returns>
        public bool TryGetModelFromGuid<T>(SerializableGUID guid, out T model) where T : GraphElementModel
        {
            var returnValue = TryGetModelFromGuid(guid, out var graphElementModel);
            model = graphElementModel as T;
            return returnValue && graphElementModel != null;
        }

        /// <summary>
        /// Adds a node model to the graph.
        /// </summary>
        /// <param name="nodeModel">The node model to add.</param>
        protected virtual void AddNode(AbstractNodeModel nodeModel)
        {
            if (nodeModel.NeedsContainer())
                throw new ArgumentException("Can't add a node model that does not need a container to the graph");
            RegisterElement(nodeModel);
            m_GraphNodeModels.Add(nodeModel);
        }

        /// <summary>
        /// Replaces node model at index.
        /// </summary>
        /// <param name="index">Index of the node model in the NodeModels list.</param>
        /// <param name="nodeModel">The new node model.</param>
        protected virtual void ReplaceNode(int index, AbstractNodeModel nodeModel)
        {
            UnregisterElement(nodeModel);
            RegisterElement(nodeModel);
            m_GraphNodeModels[index] = nodeModel;
        }

        /// <summary>
        /// Removes a node model from the graph.
        /// </summary>
        /// <param name="nodeModel"></param>
        protected virtual void RemoveNode(AbstractNodeModel nodeModel)
        {
            UnregisterElement(nodeModel);
            m_GraphNodeModels.Remove(nodeModel);
        }

        /// <inheritdoc />
        public virtual void RemoveElements(IReadOnlyCollection<GraphElementModel> elementModels)
        {
            foreach (var element in elementModels)
            {
                switch (element)
                {
                    case StickyNoteModel stickyNoteModel:
                        RemoveStickyNote(stickyNoteModel);
                        break;
                    case PlacematModel placematModel:
                        RemovePlacemat(placematModel);
                        break;
                    case VariableDeclarationModel variableDeclarationModel:
                        RemoveVariableDeclaration(variableDeclarationModel);
                        break;
                    case WireModel wireModel:
                        RemoveWire(wireModel);
                        break;
                    case AbstractNodeModel nodeModel:
                        RemoveNode(nodeModel);
                        break;
                    case BadgeModel badgeModel:
                        RemoveBadge(badgeModel);
                        break;
                }
            }
        }

        /// <summary>
        /// Adds a portal declaration model to the graph.
        /// </summary>
        /// <param name="declarationModel">The portal declaration to add.</param>
        protected virtual void AddPortal(DeclarationModel declarationModel)
        {
            RegisterElement(declarationModel);
            m_GraphPortalModels.Add(declarationModel);
        }


        /// <summary>
        /// Duplicates a portal declaration model and adds it to the graph.
        /// </summary>
        /// <param name="declarationModel">The portal declaration to duplicate.</param>
        public virtual DeclarationModel DuplicatePortal(DeclarationModel declarationModel)
        {
            var newDeclarationModel = declarationModel.Clone();

            RegisterElement(newDeclarationModel);
            m_GraphPortalModels.Add(newDeclarationModel);
            newDeclarationModel.GraphModel = this;
            return newDeclarationModel;
        }

        /// <summary>
        /// Removes a portal declaration model from the graph.
        /// </summary>
        /// <param name="declarationModel">The portal declaration to remove.</param>
        protected virtual void RemovePortal(DeclarationModel declarationModel)
        {
            UnregisterElement(declarationModel);
            m_GraphPortalModels.Remove(declarationModel);
        }

        /// <summary>
        /// Adds a wire to the graph.
        /// </summary>
        /// <param name="wireModel">The wire to add.</param>
        protected virtual void AddWire(WireModel wireModel)
        {
            RegisterElement(wireModel);
            m_GraphWireModels.Add(wireModel);

            m_PortWireIndex.AddWire(wireModel);
        }

        /// <summary>
        /// Removes a wire from th graph.
        /// </summary>
        /// <param name="wireModel">The wire to remove.</param>
        protected virtual void RemoveWire(WireModel wireModel)
        {
            UnregisterElement(wireModel);
            m_GraphWireModels.Remove(wireModel);

            m_PortWireIndex.RemoveWire(wireModel);
        }

        /// <summary>
        /// Adds a badge to the graph.
        /// </summary>
        /// <param name="badgeModel">The badge to add.</param>
        public virtual void AddBadge(BadgeModel badgeModel)
        {
            RegisterElement(badgeModel);
            badgeModel.GraphModel = this;
            m_BadgeModels.Add(badgeModel);
        }

        /// <summary>
        /// Removes a badge from the graph.
        /// </summary>
        /// <param name="badgeModel">The badge to remove.</param>
        public virtual void RemoveBadge(BadgeModel badgeModel)
        {
            UnregisterElement(badgeModel);
            badgeModel.GraphModel = null;
            m_BadgeModels.Remove(badgeModel);
        }

        /// <summary>
        /// Adds a sticky note to the graph.
        /// </summary>
        /// <param name="stickyNoteModel">The sticky note to add.</param>
        protected virtual void AddStickyNote(StickyNoteModel stickyNoteModel)
        {
            RegisterElement(stickyNoteModel);
            m_GraphStickyNoteModels.Add(stickyNoteModel);
        }

        /// <summary>
        /// Removes a sticky note from the graph.
        /// </summary>
        /// <param name="stickyNoteModel">The sticky note to remove.</param>
        protected virtual void RemoveStickyNote(StickyNoteModel stickyNoteModel)
        {
            UnregisterElement(stickyNoteModel);
            m_GraphStickyNoteModels.Remove(stickyNoteModel);
        }

        /// <summary>
        /// Adds a placemat to the graph.
        /// </summary>
        /// <param name="placematModel">The placemat to add.</param>
        protected virtual void AddPlacemat(PlacematModel placematModel)
        {
            RegisterElement(placematModel);
            m_GraphPlacematModels.Add(placematModel);
        }

        /// <summary>
        /// Removes a placemat from the graph.
        /// </summary>
        /// <param name="placematModel">The placemat to remove.</param>
        protected virtual void RemovePlacemat(PlacematModel placematModel)
        {
            UnregisterElement(placematModel);
            m_GraphPlacematModels.Remove(placematModel);
        }

        /// <summary>
        /// Adds a variable declaration to the graph.
        /// </summary>
        /// <param name="variableDeclarationModel">The variable declaration to add.</param>
        protected virtual void AddVariableDeclaration(VariableDeclarationModel variableDeclarationModel)
        {
            RegisterElement(variableDeclarationModel);
            m_GraphVariableModels.Add(variableDeclarationModel);
            m_ExistingVariableNames.Add(variableDeclarationModel.Title);
        }

        /// <summary>
        /// Removes a variable declaration from the graph.
        /// </summary>
        /// <param name="variableDeclarationModel">The variable declaration to remove.</param>
        protected virtual GroupModel RemoveVariableDeclaration(VariableDeclarationModel variableDeclarationModel)
        {
            UnregisterElement(variableDeclarationModel);
            m_GraphVariableModels.Remove(variableDeclarationModel);
            m_ExistingVariableNames.Remove(variableDeclarationModel.Title);

            var parent = variableDeclarationModel.ParentGroup;
            parent?.RemoveItem(variableDeclarationModel);
            return parent;
        }

        void RecursiveBuildElementByGuid(GraphElementModel model)
        {
            GetElementsByGuid().TryAdd(model.Guid, model);

            if (model is IGraphElementContainer container)
            {
                foreach (var element in container.GraphElementModels)
                    RecursiveBuildElementByGuid(element);
            }
        }

        /// <summary>
        /// Instantiates an object of type <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of the object to instantiate.</param>
        /// <typeparam name="TBaseType">A base type for <paramref name="type"/>.</typeparam>
        /// <returns>A new object.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="type"/> does not derive from <typeparamref name="TBaseType"/></exception>
        protected TBaseType Instantiate<TBaseType>(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            TBaseType obj;
            if (typeof(TBaseType).IsAssignableFrom(type))
                obj = (TBaseType)Activator.CreateInstance(type);
            else
                throw new ArgumentOutOfRangeException(nameof(type));

            return obj;
        }

        /// <summary>
        /// Rebuilds the dictionary mapping guids to graph element models.
        /// </summary>
        /// <remarks>
        /// Override this function if your graph models holds new graph elements types.
        /// Ensure that all additional graph element model are added to the guid to model mapping.
        /// </remarks>
        protected virtual void BuildElementByGuidDictionary()
        {
            m_ElementsByGuid = new Dictionary<SerializableGUID, GraphElementModel>();

            foreach (var model in m_GraphNodeModels)
            {
                RecursiveBuildElementByGuid(model);
            }

            foreach (var model in m_BadgeModels)
            {
                RecursiveBuildElementByGuid(model);
            }

            foreach (var model in m_GraphWireModels)
            {
                RecursiveBuildElementByGuid(model);
            }

            foreach (var model in m_GraphStickyNoteModels)
            {
                RecursiveBuildElementByGuid(model);
            }

            foreach (var model in m_GraphPlacematModels)
            {
                RecursiveBuildElementByGuid(model);
            }

            // Some variables may not be under any section.
            foreach (var model in m_GraphVariableModels)
            {
                RecursiveBuildElementByGuid(model);
            }

            foreach (var model in m_GraphPortalModels)
            {
                RecursiveBuildElementByGuid(model);
            }

            foreach (var model in m_SectionModels)
            {
                RecursiveBuildElementByGuid(model);
            }
        }

        /// <summary>
        /// Creates a new node in the graph.
        /// </summary>
        /// <param name="nodeTypeToCreate">The type of the new node to create.</param>
        /// <param name="nodeName">The name of the node to create.</param>
        /// <param name="position">The position of the node to create.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the node is created.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <returns>The newly created node.</returns>
        public virtual AbstractNodeModel CreateNode(Type nodeTypeToCreate, string nodeName, Vector2 position,
            SerializableGUID guid = default, Action<AbstractNodeModel> initializationCallback = null, SpawnFlags spawnFlags = SpawnFlags.None)
        {
            var nodeModel = InstantiateNode(nodeTypeToCreate, nodeName, position, guid, initializationCallback);

            if (!spawnFlags.IsOrphan())
            {
                AddNode(nodeModel);
            }

            return nodeModel;
        }

        /// <summary>
        /// Instantiates a new node.
        /// </summary>
        /// <param name="nodeTypeToCreate">The type of the new node to create.</param>
        /// <param name="nodeName">The name of the node to create.</param>
        /// <param name="position">The position of the node to create.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the node is created.</param>
        /// <returns>The newly created node.</returns>
        protected virtual AbstractNodeModel InstantiateNode(Type nodeTypeToCreate, string nodeName, Vector2 position,
            SerializableGUID guid = default, Action<AbstractNodeModel> initializationCallback = null)
        {
            if (nodeTypeToCreate == null)
                throw new ArgumentNullException(nameof(nodeTypeToCreate));

            AbstractNodeModel nodeModel;
            if (typeof(Constant).IsAssignableFrom(nodeTypeToCreate))
                nodeModel = new ConstantNodeModel { Value = (Constant)Activator.CreateInstance(nodeTypeToCreate) };
            else if (typeof(AbstractNodeModel).IsAssignableFrom(nodeTypeToCreate))
                nodeModel = (AbstractNodeModel)Activator.CreateInstance(nodeTypeToCreate);
            else
                throw new ArgumentOutOfRangeException(nameof(nodeTypeToCreate));

            if (nodeModel is IHasTitle titled)
                titled.Title = nodeName ?? nodeTypeToCreate.Name;

            nodeModel.Position = position;
            if (guid.Valid)
                nodeModel.SetGuid(guid);
            nodeModel.GraphModel = this;
            initializationCallback?.Invoke(nodeModel);
            nodeModel.OnCreateNode();

            return nodeModel;
        }

        /// <summary>
        /// Creates a new variable node in the graph.
        /// </summary>
        /// <param name="declarationModel">The declaration for the variable.</param>
        /// <param name="position">The position of the node to create.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <returns>The newly created variable node.</returns>
        public virtual VariableNodeModel CreateVariableNode(VariableDeclarationModel declarationModel,
            Vector2 position, SerializableGUID guid = default, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var nodeType = GetVariableNodeType();
            Debug.Assert(typeof(VariableNodeModel).IsAssignableFrom(nodeType));

            var initializationCallback = new Action<AbstractNodeModel>(n =>
            {
                var variableNodeModel = n as VariableNodeModel;
                Debug.Assert(variableNodeModel != null);
                variableNodeModel.DeclarationModel = declarationModel;
            });

            return CreateNode(nodeType, declarationModel.DisplayTitle, position, guid, initializationCallback, spawnFlags) as VariableNodeModel;
        }

        /// <summary>
        /// Creates a new subgraph node in the graph.
        /// </summary>
        /// <param name="referenceGraph">The Graph Model of the reference graph.</param>
        /// <param name="position">The position of the node to create.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <returns>The newly created subgraph node.</returns>
        public virtual SubgraphNodeModel CreateSubgraphNode(GraphModel referenceGraph, Vector2 position, SerializableGUID guid = default, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            if (referenceGraph.IsContainerGraph())
            {
                Debug.LogWarning("Failed to create the subgraph node. Container graphs cannot be referenced by a subgraph node.");
                return null;
            }

            var nodeType = GetSubgraphNodeType();
            Debug.Assert(typeof(SubgraphNodeModel).IsAssignableFrom(nodeType));

            var initializationCallback = new Action<AbstractNodeModel>(n =>
            {
                var subgraphNodeModel = n as SubgraphNodeModel;
                Debug.Assert(subgraphNodeModel != null);
                subgraphNodeModel.SubgraphModel = referenceGraph;
            });

            return CreateNode(nodeType, referenceGraph.Name, position, guid, initializationCallback, spawnFlags) as SubgraphNodeModel;
        }

        /// <summary>
        /// Creates a new constant node in the graph.
        /// </summary>
        /// <param name="constantTypeHandle">The type of the new constant node to create.</param>
        /// <param name="constantName">The name of the constant node to create.</param>
        /// <param name="position">The position of the node to create.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the constant node is created.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <returns>The newly created constant node.</returns>
        public virtual ConstantNodeModel CreateConstantNode(TypeHandle constantTypeHandle, string constantName, Vector2 position, SerializableGUID guid = default, Action<ConstantNodeModel> initializationCallback = null, SpawnFlags spawnFlags = SpawnFlags.None)
        {
            var constantType = Stencil.GetConstantType(constantTypeHandle);
            Debug.Assert(typeof(Constant).IsAssignableFrom(constantType));

            void PreDefineSetup(AbstractNodeModel model)
            {
                var constantNodeModel = model as ConstantNodeModel;
                Debug.Assert(constantNodeModel != null);
                constantNodeModel.Initialize(constantTypeHandle);
                initializationCallback?.Invoke(constantNodeModel);
            }

            return CreateNode(constantType, constantName, position, guid, PreDefineSetup, spawnFlags) as ConstantNodeModel;
        }

        /// <summary>
        /// Deletes all badges from the graph.
        /// </summary>
        /// <returns>The graph elements affected by this operation.</returns>
        public virtual IReadOnlyCollection<GraphElementModel> DeleteBadges()
        {
            var deletedBadges = new List<GraphElementModel>(m_BadgeModels);

            foreach (var model in deletedBadges)
            {
                UnregisterElement(model);
            }

            m_BadgeModels.Clear();

            return deletedBadges;
        }

        /// <summary>
        /// Deletes all badges of type <typeparamref name="T"/> from the graph.
        /// </summary>
        /// <returns>The graph elements affected by this operation.</returns>
        public virtual IReadOnlyCollection<GraphElementModel> DeleteBadgesOfType<T>() where T : BadgeModel
        {
            var deletedBadges = m_BadgeModels
                .Where(b => b is T)
                .ToList();

            foreach (var model in deletedBadges)
            {
                UnregisterElement(model);
            }

            m_BadgeModels = m_BadgeModels
                .Where(b => !(b is T))
                .ToList();

            return deletedBadges;
        }

        /// <summary>
        /// Duplicates a node and adds it to the graph.
        /// </summary>
        /// <param name="sourceNode">The node to duplicate. The node does not have to be in this graph.</param>
        /// <param name="delta">The position offset for the new node.</param>
        /// <returns>The new node.</returns>
        public virtual AbstractNodeModel DuplicateNode(AbstractNodeModel sourceNode, Vector2 delta)
        {
            var pastedNodeModel = sourceNode.Clone();

            // Set graphmodel BEFORE define node as it is commonly use during Define
            pastedNodeModel.GraphModel = this;
            pastedNodeModel.AssignNewGuid();
            pastedNodeModel.OnDuplicateNode(sourceNode);

            AddNode(pastedNodeModel);
            pastedNodeModel.Position += delta;

            if (pastedNodeModel is IGraphElementContainer container)
            {
                foreach (var element in container.GraphElementModels)
                    RecursivelyRegisterAndAssignNewGuid(element);
            }

            return pastedNodeModel;
        }

        protected void RecursivelyRegisterAndAssignNewGuid(GraphElementModel model)
        {
            model.AssignNewGuid();
            RegisterElement(model);
            if (model is IGraphElementContainer c)
            {
                foreach (var element in c.GraphElementModels)
                    RecursivelyRegisterAndAssignNewGuid(element);
            }
        }

        /// <summary>
        /// Duplicates a wire and add it to the graph.
        /// </summary>
        /// <param name="sourceWire">The wire to duplicate.</param>
        /// <param name="targetInputNode">If not null, the new input node for the wire.</param>
        /// <param name="targetOutputNode">If not null, the new output node for the wire.</param>
        /// <returns>The new wire.</returns>
        public virtual WireModel DuplicateWire(WireModel sourceWire, AbstractNodeModel targetInputNode, AbstractNodeModel targetOutputNode)
        {
            // If target node is null, reuse the original wire endpoint.
            // Avoid using sourceWire.FromPort and sourceWire.ToPort since the wire may not have sufficient context
            // to resolve the PortModel from the PortReference (sourceWire may not be in a GraphModel).

            if (targetInputNode == null)
            {
                TryGetModelFromGuid(sourceWire.ToNodeGuid, out targetInputNode);
            }

            if (targetOutputNode == null)
            {
                TryGetModelFromGuid(sourceWire.FromNodeGuid, out targetOutputNode);
            }

            PortModel inputPortModel = null;
            PortModel outputPortModel = null;
            if (targetInputNode != null && targetOutputNode != null)
            {
                inputPortModel = (targetInputNode as InputOutputPortsNodeModel)?.InputsById[sourceWire.ToPortId];
                outputPortModel = (targetOutputNode as InputOutputPortsNodeModel)?.OutputsById[sourceWire.FromPortId];
            }

            if (inputPortModel != null && outputPortModel != null)
            {
                if (inputPortModel.Capacity == PortCapacity.Single && inputPortModel.GetConnectedWires().Any())
                    return null;
                if (outputPortModel.Capacity == PortCapacity.Single && outputPortModel.GetConnectedWires().Any())
                    return null;

                return CreateWire(inputPortModel, outputPortModel);
            }

            return null;
        }

        /// <summary>
        /// Duplicates variable or constant modes connected to multiple ports so there is a single node per wire.
        /// </summary>
        /// <param name="nodeOffset">The offset to apply to the position of the duplicated node.</param>
        /// <param name="outputPortModel">The output port of the node to duplicate.</param>
        /// <returns>The newly itemized node.</returns>
        public virtual InputOutputPortsNodeModel CreateItemizedNode(int nodeOffset, ref PortModel outputPortModel)
        {
            if (outputPortModel.IsConnected())
            {
                var offset = Vector2.up * nodeOffset;
                var nodeToConnect = DuplicateNode(outputPortModel.NodeModel, offset) as InputOutputPortsNodeModel;
                outputPortModel = nodeToConnect?.OutputsById[outputPortModel.UniqueName];
                return nodeToConnect;
            }

            return null;
        }

        public virtual IReadOnlyCollection<GraphElementModel> DeleteNodes(IReadOnlyCollection<AbstractNodeModel> nodeModels, bool deleteConnections)
        {
            var deletedModels = new List<GraphElementModel>();

            var deletedElementsByContainer = new Dictionary<IGraphElementContainer, List<GraphElementModel>>();

            foreach (var nodeModel in nodeModels.Where(n => n.IsDeletable()))
            {
                if (!deletedElementsByContainer.TryGetValue(nodeModel.Container, out var deletedElements))
                {
                    deletedElements = new List<GraphElementModel>();
                    deletedElementsByContainer.Add(nodeModel.Container, deletedElements);
                }

                deletedElements.Add(nodeModel);

                deletedModels.Add(nodeModel);

                if (deleteConnections)
                {
                    var connectedWires = nodeModel.GetConnectedWires().ToList();
                    deletedModels.AddRange(DeleteWires(connectedWires));
                }

                // If this all the portals with the given declaration are deleted, delete the declaration.
                if (nodeModel is WirePortalModel wirePortalModel &&
                    wirePortalModel.DeclarationModel != null &&
                    !this.FindReferencesInGraph<WirePortalModel>(wirePortalModel.DeclarationModel).Except(nodeModels).Any())
                {
                    RemovePortal(wirePortalModel.DeclarationModel);
                    deletedModels.Add(wirePortalModel.DeclarationModel);
                }

                nodeModel.Destroy();
            }

            foreach (var container in deletedElementsByContainer)
            {
                container.Key.RemoveElements(container.Value);
            }

            return deletedModels;
        }

        /// <summary>
        /// Returns the type of wire to instantiate between two ports.
        /// </summary>
        /// <param name="toPort">The destination port.</param>
        /// <param name="fromPort">The origin port.</param>
        /// <returns>The wire model type.</returns>
        protected virtual Type GetWireType(PortModel toPort, PortModel fromPort)
        {
            return typeof(WireModel);
        }

        /// <summary>
        /// Creates a wire and add it to the graph.
        /// </summary>
        /// <param name="toPort">The port from which the wire originates.</param>
        /// <param name="fromPort">The port to which the wire goes.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <returns>The newly created wire</returns>
        public virtual WireModel CreateWire(PortModel toPort, PortModel fromPort, SerializableGUID guid = default)
        {
            var existing = this.GetWireConnectedToPorts(toPort, fromPort);
            if (existing != null)
                return existing;

            var wireModel = InstantiateWire(toPort, fromPort, guid);
            AddWire(wireModel);
            return wireModel;
        }

        /// <summary>
        /// Instantiates a wire.
        /// </summary>
        /// <param name="toPort">The port from which the wire originates.</param>
        /// <param name="fromPort">The port to which the wire goes.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <returns>The newly created wire</returns>
        protected virtual WireModel InstantiateWire(PortModel toPort, PortModel fromPort, SerializableGUID guid = default)
        {
            var wireType = GetWireType(toPort, fromPort);
            var wireModel = Instantiate<WireModel>(wireType);
            wireModel.GraphModel = this;
            if (guid.Valid)
                wireModel.SetGuid(guid);
            wireModel.SetPorts(toPort, fromPort);
            return wireModel;
        }

        /// <summary>
        /// Deletes wires from the graph.
        /// </summary>
        /// <param name="wireModels">The list of wires to delete.</param>
        /// <returns>A list of graph element models that were deleted by this operation.</returns>
        public virtual IReadOnlyCollection<GraphElementModel> DeleteWires(IReadOnlyCollection<WireModel> wireModels)
        {
            var deletedModels = new List<GraphElementModel>();

            foreach (var wireModel in wireModels.Where(e => e != null && e.IsDeletable()))
            {
                wireModel.ToPort?.NodeModel?.OnDisconnection(wireModel.ToPort, wireModel.FromPort);
                wireModel.FromPort?.NodeModel?.OnDisconnection(wireModel.FromPort, wireModel.ToPort);

                RemoveWire(wireModel);
                deletedModels.Add(wireModel);
            }

            return deletedModels;
        }

        /// <summary>
        /// Returns the type of sticky note to instantiate.
        /// </summary>
        /// <returns>The sticky note model type.</returns>
        protected virtual Type GetStickyNoteType()
        {
            return typeof(StickyNoteModel);
        }

        /// <summary>
        /// Creates a new sticky note and optionally add it to the graph.
        /// </summary>
        /// <param name="position">The position of the sticky note to create.</param>
        /// <param name="spawnFlags">The flags specifying how the sticky note is to be spawned.</param>
        /// <returns>The newly created sticky note</returns>
        public StickyNoteModel CreateStickyNote(Rect position, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var stickyNoteModel = InstantiateStickyNote(position);
            if (!spawnFlags.IsOrphan())
            {
                AddStickyNote(stickyNoteModel);
            }

            return stickyNoteModel;
        }

        /// <summary>
        /// Instantiates a new sticky note.
        /// </summary>
        /// <param name="position">The position of the sticky note to create.</param>
        /// <returns>The newly created sticky note</returns>
        protected virtual StickyNoteModel InstantiateStickyNote(Rect position)
        {
            var stickyNoteModelType = GetStickyNoteType();
            var stickyNoteModel = Instantiate<StickyNoteModel>(stickyNoteModelType);
            stickyNoteModel.PositionAndSize = position;
            stickyNoteModel.GraphModel = this;
            return stickyNoteModel;
        }

        /// <summary>
        /// Removes and destroys sticky notes from the graph.
        /// </summary>
        /// <param name="stickyNoteModels">The sticky notes to remove.</param>
        /// <returns>The graph elements affected by this operation.</returns>
        public IReadOnlyCollection<GraphElementModel> DeleteStickyNotes(IReadOnlyCollection<StickyNoteModel> stickyNoteModels)
        {
            var deletedModels = new List<GraphElementModel>();

            foreach (var stickyNoteModel in stickyNoteModels.Where(s => s.IsDeletable()))
            {
                RemoveStickyNote(stickyNoteModel);
                stickyNoteModel.Destroy();
                deletedModels.Add(stickyNoteModel);
            }

            return deletedModels;
        }

        protected virtual Type GetPlacematType()
        {
            return typeof(PlacematModel);
        }

        /// <summary>
        /// Creates a new placemat and optionally add it to the graph.
        /// </summary>
        /// <param name="position">The position of the placemat to create.</param>
        /// <param name="spawnFlags">The flags specifying how the sticky note is to be spawned.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <returns>The newly created placemat</returns>
        public PlacematModel CreatePlacemat(Rect position, SerializableGUID guid = default, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var placematModel = InstantiatePlacemat(position, guid);
            if (!spawnFlags.IsOrphan())
            {
                AddPlacemat(placematModel);
            }

            return placematModel;
        }

        /// <summary>
        /// Instantiates a new placemat.
        /// </summary>
        /// <param name="position">The position of the placemat to create.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <returns>The newly created placemat</returns>
        protected virtual PlacematModel InstantiatePlacemat(Rect position, SerializableGUID guid)
        {
            var placematModelType = GetPlacematType();
            var placematModel = Instantiate<PlacematModel>(placematModelType);
            placematModel.PositionAndSize = position;
            placematModel.GraphModel = this;
            if (guid.Valid)
                placematModel.SetGuid(guid);
            return placematModel;
        }

        /// <summary>
        /// Deletes placemats from the graph.
        /// </summary>
        /// <param name="placematModels">The list of placemats to delete.</param>
        /// <returns>A list of graph element models that were deleted by this operation.</returns>
        public IReadOnlyCollection<GraphElementModel> DeletePlacemats(IReadOnlyCollection<PlacematModel> placematModels)
        {
            var deletedModels = new List<GraphElementModel>();

            foreach (var placematModel in placematModels.Where(p => p.IsDeletable()))
            {
                RemovePlacemat(placematModel);
                placematModel.Destroy();
                deletedModels.Add(placematModel);
            }

            return deletedModels;
        }

        protected virtual Type GetVariableNodeType()
        {
            return typeof(VariableNodeModel);
        }

        protected virtual Type GetSubgraphNodeType()
        {
            return typeof(SubgraphNodeModel);
        }

        /// <summary>
        /// Returns the type of variable declaration to instantiate.
        /// </summary>
        /// <returns>The variable declaration model type.</returns>
        protected virtual Type GetVariableDeclarationType()
        {
            return typeof(VariableDeclarationModel);
        }

        /// <summary>
        /// Creates a new variable declaration in the graph.
        /// </summary>
        /// <param name="variableDataType">The type of data the new variable declaration to create represents.</param>
        /// <param name="variableName">The name of the new variable declaration to create.</param>
        /// <param name="modifierFlags">The modifier flags of the new variable declaration to create.</param>
        /// <param name="isExposed">Whether the variable is exposed externally or not.</param>
        /// <param name="group">The group in which the variable is added. If null, it will go to the root group.</param>
        /// <param name="indexInGroup">The index of the variable in the group. For indexInGroup &lt;= 0, The item will be added at the beginning. For indexInGroup &gt;= Items.Count, items will be added at the end.</param>
        /// <param name="initializationModel">The initialization model of the new variable declaration to create. Can be <code>null</code>.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="spawnFlags">The flags specifying how the variable declaration is to be spawned.</param>
        /// <returns>The newly created variable declaration.</returns>
        public virtual VariableDeclarationModel CreateGraphVariableDeclaration(TypeHandle variableDataType, string variableName,
            ModifierFlags modifierFlags, bool isExposed, GroupModel group = null, int indexInGroup = int.MaxValue, Constant initializationModel = null, SerializableGUID guid = default,
            SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            return CreateGraphVariableDeclaration(GetVariableDeclarationType(), variableDataType, variableName,
                modifierFlags, isExposed, group, indexInGroup, initializationModel, guid, InitCallback, spawnFlags);

            void InitCallback(VariableDeclarationModel variableDeclaration, Constant initModel)
            {
                if (variableDeclaration != null)
                {
                    variableDeclaration.VariableFlags = VariableFlags.None;

                    if (initModel != null) variableDeclaration.InitializationModel = initModel;
                }
            }
        }

        /// <summary>
        /// Rename an existing variable declaration in the graph with a new unique name.
        /// </summary>
        /// <param name="variable">The variable to rename.</param>
        /// <param name="expectedNewName">The new name we want to give to the variable.</param>
        public virtual void RenameVariable(VariableDeclarationModel variable, string expectedNewName)
        {
            m_ExistingVariableNames.Remove(variable.Title);
            var newName = GenerateGraphVariableDeclarationUniqueName(expectedNewName);
            m_ExistingVariableNames.Add(newName);
            variable.Title = newName;
        }

        /// <summary>
        /// Creates a new variable declaration in the graph.
        /// </summary>
        /// <param name="variableTypeToCreate">The type of variable declaration to create.</param>
        /// <param name="variableDataType">The type of data the new variable declaration to create represents.</param>
        /// <param name="variableName">The name of the new variable declaration to create.</param>
        /// <param name="modifierFlags">The modifier flags of the new variable declaration to create.</param>
        /// <param name="isExposed">Whether the variable is exposed externally or not.</param>
        /// <param name="group">The group in which the variable is added. If null, it will go to the root group.</param>
        /// <param name="indexInGroup">The index of the variable in the group. For indexInGroup &lt;= 0, The item will be added at the beginning. For indexInGroup &gt;= Items.Count, items will be added at the end.</param>
        /// <param name="initializationModel">The initialization model of the new variable declaration to create. Can be <code>null</code>.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the variable declaration is created.</param>
        /// <param name="spawnFlags">The flags specifying how the variable declaration is to be spawned.</param>
        /// <returns>The newly created variable declaration.</returns>
        public virtual VariableDeclarationModel CreateGraphVariableDeclaration(Type variableTypeToCreate, TypeHandle variableDataType, string variableName, ModifierFlags modifierFlags, bool isExposed, GroupModel group = null, int indexInGroup = Int32.MaxValue, Constant initializationModel = null, SerializableGUID guid = default, Action<VariableDeclarationModel, Constant> initializationCallback = null, SpawnFlags spawnFlags = SpawnFlags.None)
        {
            var variableDeclaration = InstantiateVariableDeclaration(variableTypeToCreate, variableDataType,
                variableName, modifierFlags, isExposed, initializationModel, guid, initializationCallback);

            if (initializationModel == null && !spawnFlags.IsOrphan())
                variableDeclaration.CreateInitializationValue();

            if (!spawnFlags.IsOrphan())
                AddVariableDeclaration(variableDeclaration);

            if (group != null)
                group.InsertItem(variableDeclaration, indexInGroup);
            else
            {
                var section = variableDeclaration.GraphModel.GetSectionModel(variableDeclaration.GraphModel.Stencil.GetVariableSection(variableDeclaration));

                section.InsertItem(variableDeclaration, indexInGroup);
            }

            return variableDeclaration;
        }

        /// <summary>
        /// Generates a unique name for a variable declaration in the graph.
        /// </summary>
        /// <param name="originalName">The name of the variable declaration.</param>
        /// <returns>The unique name for the variable declaration.</returns>
        protected virtual string GenerateGraphVariableDeclarationUniqueName(string originalName)
        {
            originalName = originalName.Trim();

            if (!m_ExistingVariableNames.Contains(originalName))
                return originalName;

            var i = 1;
            do
            {
                originalName = originalName.FormatWithNamingScheme(i++);
            } while (m_ExistingVariableNames.Contains(originalName));

            return originalName;

        }

        /// <summary>
        /// Instantiates a new variable declaration.
        /// </summary>
        /// <param name="variableTypeToCreate">The type of variable to create.</param>
        /// <param name="variableDataType">The type of data the new variable declaration to create represents.</param>
        /// <param name="variableName">The name of the new variable declaration to create.</param>
        /// <param name="modifierFlags">The modifier flags of the new variable declaration to create.</param>
        /// <param name="isExposed">Whether the variable is exposed externally or not.</param>
        /// <param name="initializationModel">The initialization model of the new variable declaration to create. Can be <code>null</code>.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item. If none is provided, a new
        /// SerializableGUID will be generated for it.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the variable declaration is created.</param>
        /// <returns>The newly created variable declaration.</returns>
        protected virtual VariableDeclarationModel InstantiateVariableDeclaration(Type variableTypeToCreate,
            TypeHandle variableDataType, string variableName, ModifierFlags modifierFlags, bool isExposed,
            Constant initializationModel, SerializableGUID guid, Action<VariableDeclarationModel, Constant> initializationCallback = null)
        {
            var variableDeclaration = Instantiate<VariableDeclarationModel>(variableTypeToCreate);

            if (guid.Valid)
                variableDeclaration.SetGuid(guid);
            variableDeclaration.GraphModel = this;
            variableDeclaration.DataType = variableDataType;
            variableDeclaration.Title = GenerateGraphVariableDeclarationUniqueName(variableName);
            variableDeclaration.IsExposed = isExposed;
            variableDeclaration.Modifiers = modifierFlags;

            initializationCallback?.Invoke(variableDeclaration, initializationModel);

            return variableDeclaration;
        }

        public virtual Type GetGroupModelType()
        {
            return typeof(GroupModel);
        }

        protected virtual GroupModel InstantiateGroup(string title)
        {
            var groupType = GetGroupModelType();
            var group = Instantiate<GroupModel>(groupType);
            group.Title = title;
            group.GraphModel = this;
            return group;
        }

        protected virtual void AddGroup(GroupModel group)
        {
            RegisterElement(group);
        }

        /// <summary>
        /// Creates a new group.
        /// </summary>
        /// <param name="title">The title of the new group.</param>
        /// <param name="items">An optional list of items that will be added to the group.</param>
        /// <returns>A new group.</returns>
        public virtual GroupModel CreateGroup(string title, IReadOnlyCollection<IGroupItemModel> items = null)
        {
            var group = InstantiateGroup(title);
            AddGroup(group);

            if (items != null)
            {
                foreach (var item in items)
                    group.InsertItem(item);
            }

            return group;
        }

        /// <summary>
        /// Duplicates a variable declaration.
        /// </summary>
        /// <param name="sourceModel">The variable declaration to duplicate.</param>
        /// <param name="keepGuid">Whether the duplicated model should have the same guid as the <paramref name="sourceModel"/>.</param>
        /// <returns>The duplicated variable declaration.</returns>
        public virtual TDeclType DuplicateGraphVariableDeclaration<TDeclType>(TDeclType sourceModel, bool keepGuid = false) where TDeclType : VariableDeclarationModel
        {
            var uniqueName = sourceModel.Title;
            var copy = sourceModel.Clone();
            copy.GraphModel = this;
            if (keepGuid)
                copy.SetGuid(sourceModel.Guid);
            copy.Title = GenerateGraphVariableDeclarationUniqueName(uniqueName);
            if (copy.InitializationModel != null)
            {
                copy.CreateInitializationValue();
                copy.InitializationModel.ObjectValue = sourceModel.InitializationModel.ObjectValue;
            }

            AddVariableDeclaration(copy);

            if (sourceModel.ParentGroup != null && sourceModel.ParentGroup.GraphModel == this)
                sourceModel.ParentGroup.InsertItem(copy, -1);
            else
            {
                var section = GetSectionModel(Stencil.GetVariableSection(copy));
                section.InsertItem(copy, -1);
            }

            return copy;
        }

        /// <summary>
        /// Deletes the given variable declaration models, with the option of also deleting the corresponding variable models.
        /// </summary>
        /// <remarks>If <paramref name="deleteUsages"/> is <c>false</c>, the user has to take care of deleting the corresponding variable models prior to this call.</remarks>
        /// <param name="variableModels">The variable declaration models to delete.</param>
        /// <param name="deleteUsages">Whether or not to delete the corresponding variable models.</param>
        /// <returns>The list of deleted models.</returns>
        public virtual GraphChangeDescription DeleteVariableDeclarations(IReadOnlyCollection<VariableDeclarationModel> variableModels, bool deleteUsages = true)
        {
            var changedModelsDict = new Dictionary<GraphElementModel, IReadOnlyList<ChangeHint>>();
            var deletedModels = new List<GraphElementModel>();

            foreach (var variableModel in variableModels.Where(v => v.IsDeletable()))
            {
                var parent = RemoveVariableDeclaration(variableModel);

                changedModelsDict[parent] = s_GroupingChangeHint;
                deletedModels.Add(variableModel);

                if (deleteUsages)
                {
                    var nodesToDelete = this.FindReferencesInGraph(variableModel).Cast<AbstractNodeModel>().ToList();
                    deletedModels.AddRange(DeleteNodes(nodesToDelete, deleteConnections: true));
                }
            }

            return new GraphChangeDescription(null, changedModelsDict, deletedModels);
        }

        /// <summary>
        /// Deletes the given group models.
        /// </summary>
        /// <param name="groupModels">The group models to delete.</param>
        /// <returns>The list of deleted models.</returns>
        public GraphChangeDescription DeleteGroups(IReadOnlyCollection<GroupModel> groupModels)
        {
            var changedModelsDict = new Dictionary<GraphElementModel, IReadOnlyList<ChangeHint>>();
            var deletedModels = new List<GraphElementModel>();
            var deletedVariables = new List<VariableDeclarationModel>();

            void RecurseAddVariables(GroupModel groupModel)
            {
                foreach (var item in groupModel.Items)
                {
                    deletedModels.Add(item as GraphElementModel);
                    if (item is VariableDeclarationModel variable)
                        deletedVariables.Add(variable);
                    else if (item is GroupModel group)
                        RecurseAddVariables(group);
                }
            }

            foreach (var groupModel in groupModels.Where(v => v.IsDeletable()))
            {
                if (groupModel.ParentGroup != null)
                {
                    var changedModels = groupModel.ParentGroup.RemoveItem(groupModel);
                    foreach (var changedModel in changedModels)
                    {
                        changedModelsDict[changedModel] = s_GroupingChangeHint;
                    }
                }

                RecurseAddVariables(groupModel);
                deletedModels.Add(groupModel);
            }

            var result = DeleteVariableDeclarations(deletedVariables);
            result.Union(null, changedModelsDict, deletedModels);
            return result;
        }

        protected virtual Type GetPortalType()
        {
            return typeof(DeclarationModel);
        }

        /// <summary>
        /// Creates a new declaration model representing a portal and optionally add it to the graph.
        /// </summary>
        /// <param name="portalName">The name of the portal</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <param name="spawnFlags">The flags specifying how the portal is to be spawned.</param>
        /// <returns>The newly created declaration model</returns>
        public virtual DeclarationModel CreateGraphPortalDeclaration(string portalName, SerializableGUID guid = default, SpawnFlags spawnFlags = SpawnFlags.None)
        {
            var decl = InstantiatePortalDeclaration(portalName, guid);

            if (!spawnFlags.IsOrphan())
            {
                AddPortal(decl);
            }

            return decl;
        }

        /// <summary>
        /// Instantiates a new portal model.
        /// </summary>
        /// <param name="portalName">The name of the portal</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        /// <returns>The newly created declaration model</returns>
        protected virtual DeclarationModel InstantiatePortalDeclaration(string portalName, SerializableGUID guid = default)
        {
            var portalModelType = GetPortalType();
            var portalModel = Instantiate<DeclarationModel>(portalModelType);
            portalModel.Title = portalName;
            if (guid.Valid)
                portalModel.SetGuid(guid);
            portalModel.GraphModel = this;
            return portalModel;
        }

        /// <summary>
        /// Creates a portal opposite to <paramref name="wirePortalModel"/>.
        /// </summary>
        /// <param name="wirePortalModel">The portal for which an opposite portal should be created.</param>
        /// <param name="position">The position of the new portal.</param>
        /// <param name="spawnFlags">Creation flags for the new portal.</param>
        /// <returns>The new portal.</returns>
        public virtual WirePortalModel CreateOppositePortal(WirePortalModel wirePortalModel, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            WirePortalModel createdPortal = null;
            Type oppositeType = null;
            switch (wirePortalModel)
            {
                case ExecutionWirePortalEntryModel _:
                    oppositeType = typeof(ExecutionWirePortalExitModel);
                    break;
                case ExecutionWirePortalExitModel _:
                    oppositeType = typeof(ExecutionWirePortalEntryModel);
                    break;
                case DataWirePortalEntryModel _:
                    oppositeType = typeof(DataWirePortalExitModel);
                    break;
                case DataWirePortalExitModel _:
                    oppositeType = typeof(DataWirePortalEntryModel);
                    break;
            }

            if (oppositeType != null)
                createdPortal = CreateNode(oppositeType, wirePortalModel.Title, position, spawnFlags: spawnFlags, initializationCallback: n => ((WirePortalModel)n).PortDataTypeHandle = wirePortalModel.PortDataTypeHandle) as WirePortalModel;

            if (createdPortal != null)
                createdPortal.DeclarationModel = wirePortalModel.DeclarationModel;

            return createdPortal;
        }

        /// <summary>
        /// Creates a pair of portals from a wire.
        /// </summary>
        /// <param name="wireModel">The wire to transform.</param>
        /// <param name="entryPortalPosition">The desired position of the entry portal.</param>
        /// <param name="exitPortalPosition">The desired position of the exit portal.</param>
        /// <param name="portalHeight">The desired height of the portals.</param>
        /// <param name="existingPortalEntries">The existing portal entries.</param>
        /// <param name="existingPortalExits">The existing portal exits.</param>
        /// <returns>The new, changed and/or deleted models.</returns>
        public virtual GraphChangeDescription CreatePortalsFromWire(WireModel wireModel, Vector2 entryPortalPosition, Vector2 exitPortalPosition, int portalHeight,
            Dictionary<PortModel, WirePortalModel> existingPortalEntries, Dictionary<PortModel, List<WirePortalModel>> existingPortalExits)
        {
            var newModels = new List<GraphElementModel>();
            var inputPortModel = wireModel.ToPort;
            var outputPortModel = wireModel.FromPort;

            // Only a single portal per output port. Don't recreate if we already created one.
            WirePortalModel portalEntry = null;

            var shouldDeleteWire = false;
            if (outputPortModel != null && !existingPortalEntries.TryGetValue(wireModel.FromPort, out portalEntry))
            {
                portalEntry = CreateEntryPortalFromPort(outputPortModel, entryPortalPosition, portalHeight, newModels);
                wireModel.SetPort(WireSide.To, (portalEntry as ISingleInputPortNodeModel)?.InputPort);
                existingPortalEntries[outputPortModel] = portalEntry;
            }
            else
            {
                DeleteWires(new[] { wireModel });
                shouldDeleteWire = true;
            }

            // We can have multiple portals on input ports however
            if (!existingPortalExits.TryGetValue(wireModel.ToPort, out var portalExits))
            {
                portalExits = new List<WirePortalModel>();
                existingPortalExits[wireModel.ToPort] = portalExits;
            }

            var portalExit = CreateExitPortalToPort(inputPortModel, exitPortalPosition, portalHeight, portalEntry, newModels);
            portalExits.Add(portalExit);

            var newExitWire = CreateWire(inputPortModel, (portalExit as ISingleOutputPortNodeModel)?.OutputPort);
            newModels.Add(newExitWire);

            return shouldDeleteWire ?
                new GraphChangeDescription(newModels, null, new[] { wireModel }) :
                new GraphChangeDescription(newModels, new Dictionary<GraphElementModel, IReadOnlyList<ChangeHint>> { { wireModel, new[] { ChangeHint.Layout } } }, null);
        }

        /// <summary>
        /// Creates an exit portal matching a port.
        /// </summary>
        /// <param name="outputPortModel">The output port model to which the portal will be connected.</param>
        /// <param name="position">The desired position of the entry portal.</param>
        /// <param name="height">The desired height of the entry portal.</param>
        /// <param name="newModels">On exit, contains the newly created models will have been appended to this list.</param>
        /// <returns>The created entry portal.</returns>
        public virtual WirePortalModel CreateEntryPortalFromPort(PortModel outputPortModel, Vector2 position, int height, List<GraphElementModel> newModels = null)
        {
            WirePortalModel portalEntry;

            if (!(outputPortModel.NodeModel is InputOutputPortsNodeModel nodeModel))
                return null;

            if (outputPortModel.PortType == PortType.Execution)
                portalEntry = this.CreateNode<ExecutionWirePortalEntryModel>();
            else
                portalEntry = this.CreateNode<DataWirePortalEntryModel>(initializationCallback: n => n.PortDataTypeHandle = outputPortModel.DataTypeHandle);

            newModels?.Add(portalEntry);

            portalEntry.Position = position;

            // y offset based on port order. hurgh.
            var idx = nodeModel.OutputsByDisplayOrder.IndexOf_Internal(outputPortModel);
            portalEntry.Position += Vector2.down * (height * idx + 16); // Fudgy.

            string portalName;
            if (nodeModel is ConstantNodeModel constantNodeModel)
                portalName = constantNodeModel.Type.FriendlyName();
            else
            {
                portalName = nodeModel.Title ?? "";
                var portName = outputPortModel.Title ?? "";
                if (!string.IsNullOrEmpty(portName))
                    portalName += " - " + portName;
            }

            portalEntry.DeclarationModel = CreateGraphPortalDeclaration(portalName);
            newModels?.Add(portalEntry.DeclarationModel);

            return portalEntry;
        }

        /// <summary>
        /// Creates an exit portal matching a port.
        /// </summary>
        /// <param name="inputPortModel">The input port model to which the portal will be connected.</param>
        /// <param name="position">The desired position of the exit portal.</param>
        /// <param name="height">The desired height of the exit portal.</param>
        /// <param name="entryPortal">The corresponding entry portal.</param>
        /// <param name="newModels">On exit, contains the newly created models will have been appended to this list.</param>
        /// <returns>The created exit portal.</returns>
        public virtual WirePortalModel CreateExitPortalToPort(PortModel inputPortModel, Vector2 position, int height, WirePortalModel entryPortal, List<GraphElementModel> newModels = null)
        {
            WirePortalModel portalExit;

            if (inputPortModel.PortType == PortType.Execution)
                portalExit = this.CreateNode<ExecutionWirePortalExitModel>();
            else
                portalExit = this.CreateNode<DataWirePortalExitModel>(initializationCallback: n => n.PortDataTypeHandle = inputPortModel.DataTypeHandle);

            newModels?.Add(portalExit);

            portalExit.Position = position;
            {
                if (inputPortModel.NodeModel is InputOutputPortsNodeModel nodeModel)
                {
                    // y offset based on port order. hurgh.
                    var idx = nodeModel.InputsByDisplayOrder.IndexOf_Internal(inputPortModel);
                    portalExit.Position += Vector2.down * (height * idx + 16); // Fudgy.
                }
            }

            portalExit.DeclarationModel = entryPortal.DeclarationModel;

            return portalExit;
        }

        /// <summary>
        /// Tasks to perform when the <see cref="GraphAsset"/> is enabled.
        /// </summary>
        public virtual void OnEnable()
        {
            foreach (var (_, model) in GetElementsByGuid())
            {
                if (model is null)
                    continue;

                model.GraphModel = this;
            }

            foreach (var nodeModel in NodeModels)
            {
                RecurseDefineNode(nodeModel);
            }

            MigrateNodes();

            CheckGroupConsistency_Internal();
        }

        /// <summary>
        /// Tasks to perform when the <see cref="GraphAsset"/> is disabled.
        /// </summary>
        public virtual void OnDisable() { }

        void RecurseDefineNode(AbstractNodeModel nodeModel)
        {
            (nodeModel as NodeModel)?.DefineNode();
            if (nodeModel is IGraphElementContainer container)
            {
                foreach (var subNodeModel in container.GraphElementModels.OfType<AbstractNodeModel>())
                {
                    RecurseDefineNode(subNodeModel);
                }
            }
        }

        /// <summary>
        /// Callback to migrate nodes from an old graph to the new models.
        /// </summary>
        protected virtual void MigrateNodes() { }

        /// <summary>
        /// Tasks to perform when an undo or redo operation is performed.
        /// </summary>
        public virtual void UndoRedoPerformed()
        {
            OnEnable();
            Asset.Dirty = true;
        }

        /// <summary>
        /// Updates the graph model when loading the graph.
        /// </summary>
        public virtual void OnLoadGraph()
        {
            // This is necessary because we can load a graph in the tool without OnEnable(),
            // which calls OnDefineNode(), being called (yet).
            // Also, PortModel.OnAfterDeserialized(), which resets port caches, is not necessarily called,
            // since the graph may already have been loaded by the AssetDatabase a long time ago.

            // The goal of this is to create the missing ports when subgraph variables get deleted.

            foreach (var nodeModel in NodeModels.OfType<NodeModel>())
                nodeModel.DefineNode();

            foreach (var wireModel in WireModels)
            {
                wireModel.UpdatePortFromCache();
                wireModel.ResetPortCache();
            }
        }

        /// <summary>
        /// Checks the integrity of the graph.
        /// </summary>
        /// <param name="errors">Verbosity level for logs to the console.</param>
        /// <returns>True if the graph is correct, false otherwise.</returns>
        public virtual bool CheckIntegrity(Verbosity errors)
        {
            Assert.IsTrue((Object)Asset, "graph asset is invalid");
            bool failed = false;
            for (var i = 0; i < WireModels.Count; i++)
            {
                var wire = WireModels[i];

                Assert.IsTrue(ReferenceEquals(this, wire.GraphModel), $"Wire {i} graph is not matching its actual graph");

                if (wire.ToPort == null)
                {
                    failed = true;
                    Debug.Log($"Wire {i} toPort is null, output: {wire.FromPort}");
                }
                else
                {
                    Assert.IsTrue(ReferenceEquals(this, wire.ToPort.GraphModel), $"Wire {i} ToPort graph is not matching its actual graph");
                }

                if (wire.FromPort == null)
                {
                    failed = true;
                    Debug.Log($"Wire {i} output is null, toPort: {wire.ToPort}");
                }
                else
                {
                    Assert.IsTrue(ReferenceEquals(this, wire.FromPort.GraphModel), $"Wire {i} FromPort graph is not matching its actual graph");
                }
            }

            CheckNodeList();
            CheckVariableDeclarations();
            CheckPortalDeclarations();
            CheckPlacemats();
            CheckStickyNotes();
            CheckSectionsAndGroups();

            if (!failed && errors == Verbosity.Verbose)
                Debug.Log("Integrity check succeeded");
            return !failed;
        }

        void CheckNodeList()
        {
            var nodesAndBlocks = NodeAndBlockModels.ToList();
            var existingGuids = new Dictionary<SerializableGUID, int>(nodesAndBlocks.Count);

            for (var i = 0; i < nodesAndBlocks.Count; i++)
            {
                AbstractNodeModel node = nodesAndBlocks[i];

                Assert.IsTrue(node.GraphModel != null, $"Node {i} {node} graph is null");
                Assert.IsNotNull(node, $"Node {i} is null");
                Assert.IsTrue(ReferenceEquals(this, node.GraphModel), $"Node {i} graph is not matching its actual graph");
                Assert.IsFalse(!node.Guid.Valid, $"Node {i} ({node.GetType()}) has an empty Guid");
                Assert.IsFalse(existingGuids.TryGetValue(node.Guid, out var oldIndex), $"duplicate GUIDs: Node {i} ({node.GetType()}) and Node {oldIndex} have the same guid {node.Guid}");
                existingGuids.Add(node.Guid, i);

                if (node.Destroyed)
                    continue;

                if (node is InputOutputPortsNodeModel portHolder)
                {
                    CheckNodePorts(portHolder.InputsById);
                    CheckNodePorts(portHolder.OutputsById);
                }

                if (node is VariableNodeModel variableNode && variableNode.DeclarationModel != null)
                {
                    var originalDeclarations = VariableDeclarations.Where(d => d.Guid == variableNode.DeclarationModel.Guid).ToList();
                    Assert.IsTrue(originalDeclarations.Count <= 1);
                    var originalDeclaration = originalDeclarations.SingleOrDefault();
                    Assert.IsNotNull(originalDeclaration, $"Variable Node {i} {variableNode.Title} has a declaration model, but it was not present in the graph's variable declaration list");
                    Assert.IsTrue(ReferenceEquals(originalDeclaration, variableNode.DeclarationModel), $"Variable Node {i} {variableNode.Title} has a declaration model that was not ReferenceEquals() to the matching one in the graph");
                }
            }
        }

        void CheckNodePorts(IReadOnlyDictionary<string, PortModel> portsById)
        {
            foreach (var kv in portsById)
            {
                string portId = kv.Value.UniqueName;
                Assert.AreEqual(kv.Key, portId, $"Node {kv.Key} port and its actual id {portId} mismatch");
                Assert.IsTrue(ReferenceEquals(this, kv.Value.GraphModel), $"Port {portId} graph is not matching its actual graph");
            }
        }

        void CheckVariableDeclarations()
        {
            for (var i = 0; i < VariableDeclarations.Count; i++)
            {
                var declaration = VariableDeclarations[i];
                Assert.IsTrue(ReferenceEquals(this, declaration.GraphModel), $"VariableDeclarations {i} graph is not matching its actual graph");
            }
        }

        void CheckPortalDeclarations()
        {
            for (var i = 0; i < PortalDeclarations.Count; i++)
            {
                var declaration = PortalDeclarations[i];
                Assert.IsTrue(ReferenceEquals(this, declaration.GraphModel), $"PortalDeclarations {i} graph is not matching its actual graph");
            }
        }

        void CheckPlacemats()
        {
            for (var i = 0; i < PlacematModels.Count; i++)
            {
                var placematModel = PlacematModels[i];
                Assert.IsTrue(ReferenceEquals(this, placematModel.GraphModel), $"Placemat {i} graph is not matching its actual graph");
            }
        }

        void CheckStickyNotes()
        {
            for (var i = 0; i < StickyNoteModels.Count; i++)
            {
                var stickyNoteModel = StickyNoteModels[i];
                Assert.IsTrue(ReferenceEquals(this, stickyNoteModel.GraphModel), $"StickyNote {i} graph is not matching its actual graph");
            }
        }

        void CheckSectionsAndGroups()
        {
            for (var i = 0; i < SectionModels.Count; i++)
            {
                var sectionModel = SectionModels[i];
                CheckSectionsAndGroupsRecursive(sectionModel);
            }
        }

        void CheckSectionsAndGroupsRecursive(GroupModel groupModel)
        {
            Assert.IsTrue(ReferenceEquals(this, groupModel.GraphModel), $"Group {groupModel} graph is not matching its actual graph");

            foreach (var groupItemModel in groupModel.Items.OfType<GroupModel>())
            {
                CheckSectionsAndGroupsRecursive(groupItemModel);
            }
        }

        /// <inheritdoc />
        public virtual void OnBeforeSerialize()
        {
            if (StencilType != null)
                m_StencilTypeName = StencilType.AssemblyQualifiedName;
        }

        /// <inheritdoc />
        public virtual void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(m_StencilTypeName))
            {
                StencilType = TypeHandleHelpers.GetTypeFromName_Internal(m_StencilTypeName) ?? DefaultStencilType;
            }

            if (m_GraphWireModels == null)
                m_GraphWireModels = new List<WireModel>();

            if (m_GraphStickyNoteModels == null)
                m_GraphStickyNoteModels = new List<StickyNoteModel>();

            if (m_GraphPlacematModels == null)
                m_GraphPlacematModels = new List<PlacematModel>();

            if (m_GraphNodeModels == null)
                m_GraphNodeModels = new List<AbstractNodeModel>();

            // Set the graph model on all elements.
            foreach (var model in m_GraphNodeModels)
            {
                RecursiveSetGraphModel(model);
            }

            foreach (var model in m_BadgeModels)
            {
                RecursiveSetGraphModel(model);
            }

            foreach (var model in m_GraphWireModels)
            {
                RecursiveSetGraphModel(model);
            }

            foreach (var model in m_GraphStickyNoteModels)
            {
                RecursiveSetGraphModel(model);
            }

            foreach (var model in m_GraphPlacematModels)
            {
                RecursiveSetGraphModel(model);
            }

            foreach (var model in m_GraphVariableModels)
            {
                RecursiveSetGraphModel(model);
            }

            foreach (var model in m_GraphPortalModels)
            {
                RecursiveSetGraphModel(model);
            }

            foreach (var model in m_SectionModels)
            {
                RecursiveSetGraphModel(model);
            }

            m_ExistingVariableNames = new HashSet<string>(VariableDeclarations.Count);
            foreach (var declarationModel in VariableDeclarations)
            {
                if (declarationModel != null) // in case of bad serialized graph - breaks a test if not tested
                    m_ExistingVariableNames.Add(declarationModel.Title);
            }

            ResetCaches();
        }

        protected void RecursiveSetGraphModel(GraphElementModel model)
        {
            if (model == null)
                return;

            model.GraphModel = this;

            if (model is IGraphElementContainer container)
            {
                foreach (var element in container.GraphElementModels)
                    RecursiveSetGraphModel(element);
            }
        }

        /// <summary>
        /// Resets internal caches.
        /// </summary>
        protected virtual void ResetCaches()
        {
            m_ElementsByGuid = null;
            m_PortWireIndex?.MarkDirty();
        }

        /// <summary>
        /// Cleans the stored sections based on the the given section names.
        /// </summary>
        /// <param name="sectionNames">The section that should exist.</param>
        protected virtual void CleanupSections(IEnumerable<string> sectionNames)
        {
            if (m_SectionModels == null)
                m_SectionModels = new List<SectionModel>();
            HashSet<string> sectionHash = new HashSet<string>(sectionNames);
            foreach (var section in m_SectionModels.ToList())
            {
                if (!sectionHash.Contains(section.Title))
                    m_SectionModels.Remove(section);
            }

            foreach (var sectionName in sectionNames)
            {
                if (m_SectionModels.All(t => t.Title != sectionName))
                {
                    CreateSection(sectionName);
                }
            }
        }

        /// <summary>
        /// Makes this graph a clone of <paramref name="sourceGraphModel"/>.
        /// </summary>
        /// <param name="sourceGraphModel">The source graph.</param>
        public virtual void CloneGraph(GraphModel sourceGraphModel)
        {
            ResetCaches();

            m_GraphNodeModels = new List<AbstractNodeModel>();
            m_GraphWireModels = new List<WireModel>();
            m_GraphStickyNoteModels = new List<StickyNoteModel>();
            m_GraphPlacematModels = new List<PlacematModel>();
            m_GraphVariableModels = new List<VariableDeclarationModel>();
            m_GraphPortalModels = new List<DeclarationModel>();

            var elementMapping = new Dictionary<string, GraphElementModel>();
            var nodeMapping = new Dictionary<AbstractNodeModel, AbstractNodeModel>();
            var variableMapping = new Dictionary<VariableDeclarationModel, VariableDeclarationModel>();

            if (sourceGraphModel.VariableDeclarations.Any())
            {
                List<VariableDeclarationModel> variableDeclarationModels =
                    sourceGraphModel.VariableDeclarations.ToList();

                foreach (var sourceModel in variableDeclarationModels)
                {
                    var copy = DuplicateGraphVariableDeclaration(sourceModel);
                    variableMapping.Add(sourceModel, copy);
                }
            }

            foreach (var sourceNode in sourceGraphModel.NodeModels)
            {
                var pastedNode = DuplicateNode(sourceNode, Vector2.zero);
                nodeMapping[sourceNode] = pastedNode;
            }

            foreach (var nodeModel in nodeMapping)
            {
                elementMapping.Add(nodeModel.Key.Guid.ToString(), nodeModel.Value);
            }

            foreach (var sourceWire in sourceGraphModel.WireModels)
            {
                elementMapping.TryGetValue(sourceWire.ToNodeGuid.ToString(), out var newInput);
                elementMapping.TryGetValue(sourceWire.FromNodeGuid.ToString(), out var newOutput);

                DuplicateWire(sourceWire, newInput as AbstractNodeModel, newOutput as AbstractNodeModel);
                elementMapping.Add(sourceWire.Guid.ToString(), sourceWire);
            }

            foreach (var sourceVariableNode in sourceGraphModel.NodeModels.Where(model => model is VariableNodeModel))
            {
                elementMapping.TryGetValue(sourceVariableNode.Guid.ToString(), out var newNode);

                if (newNode != null)
                    ((VariableNodeModel)newNode).DeclarationModel =
                        variableMapping[((VariableNodeModel)sourceVariableNode).VariableDeclarationModel];
            }

            foreach (var stickyNote in sourceGraphModel.StickyNoteModels)
            {
                var newPosition = new Rect(stickyNote.PositionAndSize.position, stickyNote.PositionAndSize.size);
                var pastedStickyNote = CreateStickyNote(newPosition);
                pastedStickyNote.Title = stickyNote.Title;
                pastedStickyNote.Contents = stickyNote.Contents;
                pastedStickyNote.Theme = stickyNote.Theme;
                pastedStickyNote.TextSize = stickyNote.TextSize;
                elementMapping.Add(stickyNote.Guid.ToString(), pastedStickyNote);
            }

            List<PlacematModel> pastedPlacemats = new List<PlacematModel>();
            foreach (var placemat in sourceGraphModel.PlacematModels)
            {
                var newPosition = new Rect(placemat.PositionAndSize.position, placemat.PositionAndSize.size);
                var pastedPlacemat = CreatePlacemat(newPosition);
                pastedPlacemat.Title = placemat.Title;
                pastedPlacemat.Color = placemat.Color;
                pastedPlacemat.Collapsed = placemat.Collapsed;
                pastedPlacemat.HiddenElementsGuid = placemat.HiddenElementsGuid;
                pastedPlacemats.Add(pastedPlacemat);
                elementMapping.Add(placemat.Guid.ToString(), pastedPlacemat);
            }

            // Update hidden content to new node ids.
            foreach (var pastedPlacemat in pastedPlacemats)
            {
                if (pastedPlacemat.Collapsed)
                {
                    List<string> pastedHiddenContent = new List<string>();
                    foreach (var guid in pastedPlacemat.HiddenElementsGuid)
                    {
                        if (elementMapping.TryGetValue(guid, out GraphElementModel pastedElement))
                        {
                            pastedHiddenContent.Add(pastedElement.Guid.ToString());
                        }
                    }

                    pastedPlacemat.HiddenElementsGuid = pastedHiddenContent;
                }
            }
        }

        /// <inheritdoc />
        public virtual void Repair()
        {
            m_GraphNodeModels.RemoveAll(t => t == null);
            m_GraphNodeModels.RemoveAll(t => t is VariableNodeModel variable && variable.DeclarationModel == null);

            foreach (var container in m_GraphNodeModels.OfType<IGraphElementContainer>())
            {
                container.Repair();
            }

            var validGuids = new HashSet<SerializableGUID>(m_GraphNodeModels.Select(t => t.Guid));

            m_BadgeModels.RemoveAll(t => t == null);
            m_BadgeModels.RemoveAll(t => t.ParentModel == null);
            m_GraphWireModels.RemoveAll(t => t == null);
            m_GraphWireModels.RemoveAll(t => !validGuids.Contains(t.FromNodeGuid) || !validGuids.Contains(t.ToNodeGuid));
            m_GraphStickyNoteModels.RemoveAll(t => t == null);
            m_GraphPlacematModels.RemoveAll(t => t == null);
            m_GraphVariableModels.RemoveAll(t => t == null);
            m_GraphPortalModels.RemoveAll(t => t == null);
            m_SectionModels.ForEach(t => t.Repair());
        }

        /// <summary>
        /// Checks whether the graph is a Container Graph or not. If it is not a Container Graph, it is an Asset Graph.
        /// </summary>
        /// <remarks>
        /// A Container Graph is a graph that cannot be nested inside of another graph, and can be referenced by a game object or scene.
        /// An Asset Graph is a graph that can have exposed inputs/outputs, making it so that it can be nested inside of another graph, and can be referenced by a game object or scene.
        /// </remarks>
        /// <returns>True if the graph is a container graph, false otherwise.</returns>
        public virtual bool IsContainerGraph() => false;

        /// <summary>
        /// Checks the conditions to specify whether the Asset Graph can be a subgraph or not.
        /// </summary>
        /// <remarks>
        /// A subgraph is an Asset Graph that is nested inside of another graph asset, and can be referenced by a game object or scene.
        /// </remarks>
        /// <returns>True if the Asset Graph can be a subgraph, false otherwise.</returns>
        public virtual bool CanBeSubgraph() => !IsContainerGraph();
    }
}
