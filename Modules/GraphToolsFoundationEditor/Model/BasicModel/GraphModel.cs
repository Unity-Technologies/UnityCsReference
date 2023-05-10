// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CommandStateObserver;
using UnityEditor;
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
    abstract class GraphModel : Model, IGraphElementContainer
    {
        [SerializeReference]
        List<AbstractNodeModel> m_GraphNodeModels;

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

        List<IPlaceholder> m_Placeholders;

        [SerializeField]
        [HideInInspector]
        List<GraphElementMetaData> m_GraphElementMetaData;

        SerializedValueDictionary<Hash128, PlaceholderData> m_PlaceholderData;

        /// <summary>
        /// Holds created variables names to make creation of unique names faster.
        /// </summary>
        HashSet<string> m_ExistingVariableNames;

        [SerializeField]
        [HideInInspector]
        string m_StencilTypeName; // serialized as string, resolved as type by ISerializationCallbackReceiver

        Type m_StencilType;

        // As this field is not serialized, use GetElementsByGuid() to access it.
        Dictionary<Hash128, GraphElementModel> m_ElementsByGuid;

        PortWireIndex_Internal<WireModel> m_PortWireIndex;

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

                Stencil = (StencilBase)Activator.CreateInstance(m_StencilType, this);
                Assert.IsNotNull(Stencil);
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

        public IReadOnlyList<IPlaceholder> Placeholders => m_Placeholders;

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

        Stack<GraphChangeDescription> m_GraphChangeDescriptionStack;

        /// <summary>
        /// The current <see cref="GraphChangeDescription"/>. This object contains the current changes applied to the graph up until now. Can be null.
        /// </summary>
        public GraphChangeDescription CurrentGraphChangeDescription => m_GraphChangeDescriptionStack.Count > 0 ? m_GraphChangeDescriptionStack.Peek() : null;

        /// <summary>
        /// A <see cref="GraphChangeDescriptionScope"/>. Use this to gather a <see cref="GraphChangeDescription"/>. <see cref="GraphChangeDescriptionScope"/> can be nested
        /// and each scope provide the <see cref="GraphChangeDescription"/> related to their scope only. When a scope is disposed, their related <see cref="GraphChangeDescription"/>
        /// is merged back into the parent scope, if any.
        /// </summary>
        public GraphChangeDescriptionScope ChangeDescriptionScope => new(this);

        public virtual Type GetSectionModelType()
        {
            return typeof(SectionModel);
        }

        /// <summary>
        /// Instantiates a new <see cref="SectionModel"/>.
        /// </summary>
        /// <param name="sectionName">The name of the section.</param>
        /// <returns>A new <see cref="SectionModel"/>.</returns>
        protected virtual SectionModel InstantiateSection(string sectionName)
        {
            var section = Instantiate<SectionModel>(GetSectionModelType());
            section.Title = sectionName;
            section.GraphModel = this;
            return section;
        }

        /// <summary>
        /// Creates a new <see cref="SectionModel"/> and adds it to the graph.
        /// </summary>
        /// <param name="sectionName">The name of the section.</param>
        /// <returns>A new <see cref="SectionModel"/>.</returns>
        public virtual SectionModel CreateSection(string sectionName)
        {
            var section = InstantiateSection(sectionName);
            AddSection(section);
            return section;
        }

        /// <summary>
        /// Adds a new <see cref="SectionModel"/> to the graph.
        /// </summary>
        /// <param name="section">The section model to add.</param>
        protected virtual void AddSection(SectionModel section)
        {
            RegisterElement(section);
            m_SectionModels.Add(section);
            CurrentGraphChangeDescription?.AddNewModels(section);
        }

        /// <summary>
        /// Removes a <see cref="SectionModel"/> from the graph.
        /// </summary>
        /// <param name="section">The section model to remove.</param>
        protected virtual void RemoveSection(SectionModel section)
        {
            UnregisterElement(section);
            m_SectionModels.Remove(section);
            CurrentGraphChangeDescription?.AddDeletedModels(section);
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

            foreach (var variable in variablesInGroup)
            {
                if (variable is VariableDeclarationPlaceholder)
                    variable.ParentGroup.RemoveItem(variable);
            }

            if (VariableDeclarations == null) return;

            foreach (var variable in VariableDeclarations.Where(v => v != null))
            {
                if (!variablesInGroup.Contains(variable))
                    GetSectionModel(Stencil.GetVariableSection(variable)).InsertItem(variable);
            }
        }

        /// <summary>
        /// The index that maps ports to the wires connected to them.
        /// </summary>
        internal PortWireIndex_Internal<WireModel> PortWireIndex_Internal => m_PortWireIndex ??= new PortWireIndex_Internal<WireModel>(WireModels);

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphModel"/> class.
        /// </summary>
        protected GraphModel()
        {
            m_GraphNodeModels = new List<AbstractNodeModel>();
            m_GraphWireModels = new List<WireModel>();
            m_GraphStickyNoteModels = new List<StickyNoteModel>();
            m_GraphPlacematModels = new List<PlacematModel>();
            m_GraphVariableModels = new List<VariableDeclarationModel>();
            m_GraphPortalModels = new List<DeclarationModel>();
            m_SectionModels = new List<SectionModel>();
            m_GraphElementMetaData = new List<GraphElementMetaData>();

            m_ExistingVariableNames = new HashSet<string>();

            m_Placeholders = new List<IPlaceholder>();
            m_PlaceholderData = new SerializedValueDictionary<Hash128, PlaceholderData>();

            m_GraphChangeDescriptionStack = new Stack<GraphChangeDescription>();
        }

        /// <summary>
        /// Gets the list of wires that are connected to a port.
        /// </summary>
        /// <param name="portModel">The port for which we want the list of connected wires.</param>
        /// <returns>The list of wires connected to the port.</returns>
        public virtual IReadOnlyList<WireModel> GetWiresForPort(PortModel portModel)
        {
            return PortWireIndex_Internal.GetWiresForPort(portModel);
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
                m_PortWireIndex?.WireReordered(wireModel, reorderType);
                ApplyReorderToGraph(fromPort);

                var siblingWires = fromPort.GetConnectedWires().ToList();
                CurrentGraphChangeDescription?.AddChangedModels(siblingWires, ChangeHint.GraphTopology);
                CurrentGraphChangeDescription?.AddChangedModel(fromPort, ChangeHint.GraphTopology);
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
            m_PortWireIndex?.WirePortsChanged(wireModel, oldPort, port);

            if (oldPort != null)
            {
                CurrentGraphChangeDescription?.AddChangedModel(oldPort, ChangeHint.GraphTopology);
                if (oldPort.PortType == PortType.MissingPort && !oldPort.GetConnectedWires().Any())
                    oldPort.NodeModel?.RemoveUnusedMissingPort(oldPort);
            }
            if (port != null)
                CurrentGraphChangeDescription?.AddChangedModel(port, ChangeHint.GraphTopology);
            if (wireModel != null)
                CurrentGraphChangeDescription?.AddChangedModel(wireModel, ChangeHint.GraphTopology);

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
            CurrentGraphChangeDescription?.AddChangedModels(models, ChangeHint.Layout);
        }

        /// <summary>
        /// Reorders <see cref="m_GraphWireModels"/> after the <see cref="PortWireIndex_Internal"/> has been reordered.
        /// </summary>
        /// <param name="fromPort">The port from which the reordered wires start.</param>
        void ApplyReorderToGraph(PortModel fromPort)
        {
            var orderedList = GetWiresForPort(fromPort);
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
        /// Gets all ports in the graph.
        /// </summary>
        /// <returns>All ports in the graph.</returns>
        public IEnumerable<PortModel> GetPortModels()
        {
            return GetElementsByGuid().Values.OfType<PortModel>();
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

            if (startPortModel.PortDataType == typeof(ExecutionFlow) != (compatiblePortModel.PortDataType == typeof(ExecutionFlow)))
                return false;

            if (startPortModel.PortType == PortType.MissingPort || compatiblePortModel.PortType == PortType.MissingPort)
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
        protected virtual Dictionary<Hash128, GraphElementModel> GetElementsByGuid()
        {
            if (m_ElementsByGuid == null)
                BuildElementByGuidDictionary();

            return m_ElementsByGuid;
        }

        /// <summary>
        /// Registers an element so that the GraphModel can find it through its GUID.
        /// </summary>
        /// <param name="model">The model.</param>
        protected virtual void RegisterElement(GraphElementModel model)
        {
            if (model == null)
                return;

            if (!GetElementsByGuid().TryAdd(model.Guid, model))
            {
                if (GetElementsByGuid()[model.Guid] != model)
                    Debug.LogError("A model is already registered with this GUID");
            }

            foreach (var subModel in model.DependentModels)
            {
                RegisterElement(subModel);
            }
        }

        /// <summary>
        /// Unregisters an element so that the GraphModel can no longer find it through its GUID.
        /// </summary>
        /// <param name="model">The model.</param>
        protected virtual void UnregisterElement(GraphElementModel model)
        {
            GetElementsByGuid().Remove(model.Guid);

            foreach (var subModel in model.DependentModels)
            {
                UnregisterElement(subModel);
            }
        }

        /// <summary>
        /// Retrieves a graph element model from its GUID.
        /// </summary>
        /// <param name="guid">The guid of the model to retrieve.</param>
        /// <param name="model">The model matching the guid, or null if no model were found.</param>
        /// <returns>True if the model was found. False otherwise.</returns>
        public virtual bool TryGetModelFromGuid(Hash128 guid, out GraphElementModel model)
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
        public bool TryGetModelFromGuid<T>(Hash128 guid, out T model) where T : GraphElementModel
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
            if (Stencil is { AllowPortalCreation: false } && nodeModel is WirePortalModel)
            {
                throw new ArgumentException("Portal creation is disabled by the Stencil.", nameof(nodeModel));
            }

            if (nodeModel.NeedsContainer())
                throw new ArgumentException("Can't add a node model that does not need a container to the graph");
            RegisterElement(nodeModel);
            AddMetaData(nodeModel, m_GraphNodeModels.Count);
            m_GraphNodeModels.Add(nodeModel);
            CurrentGraphChangeDescription?.AddNewModels(nodeModel);
        }

        void AddMetaData(Model model, int index = -1)
        {
            m_GraphElementMetaData.Add(new GraphElementMetaData(model, index));
        }

        /// <summary>
        /// Replaces node model at index.
        /// </summary>
        /// <param name="index">Index of the node model in the NodeModels list.</param>
        /// <param name="nodeModel">The new node model.</param>
        protected virtual void ReplaceNode(int index, AbstractNodeModel nodeModel)
        {
            if (Stencil is { AllowPortalCreation: false } && nodeModel is WirePortalModel)
            {
                throw new ArgumentException("Portal creation is disabled by the Stencil", nameof(nodeModel));
            }

            if (index < 0 || index >= m_GraphNodeModels.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var oldModel = m_GraphNodeModels[index];

            UnregisterElement(oldModel);
            RegisterElement(nodeModel);
            var indexInMetadata = m_GraphElementMetaData.FindIndex(m => m.Index == index);

            m_GraphElementMetaData[indexInMetadata] = new GraphElementMetaData(nodeModel, index);
            m_GraphNodeModels[index] = nodeModel;
            CurrentGraphChangeDescription?.AddNewModels(nodeModel)
                ?.AddDeletedModels(oldModel);
        }

        /// <summary>
        /// Removes a node model from the graph.
        /// </summary>
        /// <param name="nodeModel"></param>
        protected virtual void RemoveNode(AbstractNodeModel nodeModel)
        {
            if (nodeModel == null)
                return;

            UnregisterElement(nodeModel);

            var indexToRemove = m_GraphNodeModels.IndexOf(nodeModel);
            if (indexToRemove != -1)
            {
                RemoveFromMetadata(indexToRemove, PlaceholderModelHelper.ModelToMissingTypeCategory_Internal(nodeModel));
                m_GraphNodeModels.RemoveAt(indexToRemove);
                CurrentGraphChangeDescription?.AddDeletedModels(nodeModel);
            }
        }

        void RemoveFromMetadata(int index, ManagedMissingTypeModelCategory category)
        {
            var metadataIndexToRemove = -1;
            for (var i = 0; i < m_GraphElementMetaData.Count; i++)
            {
                var metadata = m_GraphElementMetaData[i];

                if (metadata.Category != category || metadata.Index < index)
                    continue;

                if (metadata.Index == index)
                {
                    metadataIndexToRemove = i;
                    continue;
                }

                // Update the index of other same category elements positioned after the removed model.
                m_GraphElementMetaData[i].Index = metadata.Index - 1;
            }

            if (metadataIndexToRemove != -1)
            {
                m_PlaceholderData.Remove(m_GraphElementMetaData[metadataIndexToRemove].Guid);
                m_GraphElementMetaData.RemoveAt(metadataIndexToRemove);
            }
        }

        public void RegisterBlockNode(BlockNodeModel blockNodeModel)
        {
            RegisterElement(blockNodeModel);
        }

        public void UnregisterBlockNode(BlockNodeModel blockNodeModel)
        {
            UnregisterElement(blockNodeModel);
        }

        public void RegisterNodePreview(NodePreviewModel nodePreviewModel)
        {
            RegisterElement(nodePreviewModel);
        }

        public void UnregisterNodePreview(NodePreviewModel nodePreviewModel)
        {
            UnregisterElement(nodePreviewModel);
        }

        public void RegisterPort(PortModel portModel)
        {
            if (!portModel?.NodeModel?.SpawnFlags.IsOrphan() ?? false)
                RegisterElement(portModel);
        }

        public void UnregisterPort(PortModel portModel)
        {
            if (!portModel?.NodeModel?.SpawnFlags.IsOrphan() ?? false)
                UnregisterElement(portModel);
        }

        /// <inheritdoc />
        public virtual void RemoveElements(IReadOnlyCollection<GraphElementModel> elementModels)
        {
            foreach (var element in elementModels)
            {
                switch (element)
                {
                    case IPlaceholder placeholder:
                        RemovePlaceholder(placeholder);
                        break;
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
                    case BlockNodeModel blockNodeModel:
                        UnregisterBlockNode(blockNodeModel);
                        break;
                    case AbstractNodeModel nodeModel:
                        RemoveNode(nodeModel);
                        break;
                    case PortModel portModel:
                        UnregisterPort(portModel);
                        break;
                    case SectionModel sectionModel:
                        RemoveSection(sectionModel);
                        break;
                    case GroupModel groupModel:
                        RemoveGroup(groupModel);
                        break;
                    default:
                        UnregisterElement(element);
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
            if (Stencil is { AllowPortalCreation: false })
            {
                throw new InvalidOperationException("Portal creation is disabled by the Stencil");
            }

            RegisterElement(declarationModel);
            AddMetaData(declarationModel, m_GraphPortalModels.Count);
            m_GraphPortalModels.Add(declarationModel);
            CurrentGraphChangeDescription?.AddNewModels(declarationModel);
        }

        /// <summary>
        /// Duplicates a portal declaration model and adds it to the graph.
        /// </summary>
        /// <param name="declarationModel">The portal declaration to duplicate.</param>
        /// <returns>The new portal declaration model.</returns>
        public virtual DeclarationModel DuplicatePortal(DeclarationModel declarationModel)
        {
            if (Stencil is { AllowPortalCreation: false })
            {
                throw new InvalidOperationException("Portal creation is disabled by the Stencil");
            }

            var newDeclarationModel = declarationModel.Clone();

            RegisterElement(newDeclarationModel);
            m_GraphPortalModels.Add(newDeclarationModel);
            newDeclarationModel.GraphModel = this;
            CurrentGraphChangeDescription?.AddNewModels(newDeclarationModel);
            return newDeclarationModel;
        }

        /// <summary>
        /// Removes a portal declaration model from the graph.
        /// </summary>
        /// <param name="declarationModel">The portal declaration to remove.</param>
        protected virtual void RemovePortal(DeclarationModel declarationModel)
        {
            if (declarationModel == null)
                return;

            UnregisterElement(declarationModel);

            var indexToRemove = m_GraphPortalModels.IndexOf(declarationModel);
            if (indexToRemove != -1)
            {
                m_GraphPortalModels.RemoveAt(indexToRemove);
                RemoveFromMetadata(indexToRemove, ManagedMissingTypeModelCategory.PortalDeclaration);
                CurrentGraphChangeDescription?.AddDeletedModels(declarationModel);
            }
        }

        /// <summary>
        /// Adds a wire to the graph.
        /// </summary>
        /// <param name="wireModel">The wire to add.</param>
        protected virtual void AddWire(WireModel wireModel)
        {
            RegisterElement(wireModel);
            AddMetaData(wireModel, m_GraphWireModels.Count);
            m_GraphWireModels.Add(wireModel);
            m_PortWireIndex?.WireAdded(wireModel);
            CurrentGraphChangeDescription?.AddNewModels(wireModel);
        }

        /// <summary>
        /// Removes a wire from th graph.
        /// </summary>
        /// <param name="wireModel">The wire to remove.</param>
        protected virtual void RemoveWire(WireModel wireModel)
        {
            if (wireModel == null)
                return;

            UnregisterElement(wireModel);

            var indexToRemove = m_GraphWireModels.IndexOf(wireModel);
            if (indexToRemove != -1)
            {
                m_GraphWireModels.RemoveAt(indexToRemove);
                RemoveFromMetadata(indexToRemove, ManagedMissingTypeModelCategory.Wire);
                CurrentGraphChangeDescription?.AddDeletedModels(wireModel);
            }
            m_PortWireIndex?.WireRemoved(wireModel);

            // Remove missing port with no connections.
            if (wireModel.ToPort?.PortType == PortType.MissingPort && (!wireModel.ToPort?.GetConnectedWires().Any() ?? false))
                wireModel.ToPort?.NodeModel?.RemoveUnusedMissingPort(wireModel.ToPort);

            if (wireModel.FromPort?.PortType == PortType.MissingPort && (!wireModel.FromPort?.GetConnectedWires().Any() ?? false))
                wireModel.FromPort?.NodeModel?.RemoveUnusedMissingPort(wireModel.FromPort);
        }

        /// <summary>
        /// Adds a sticky note to the graph.
        /// </summary>
        /// <param name="stickyNoteModel">The sticky note to add.</param>
        protected virtual void AddStickyNote(StickyNoteModel stickyNoteModel)
        {
            RegisterElement(stickyNoteModel);
            m_GraphStickyNoteModels.Add(stickyNoteModel);
            CurrentGraphChangeDescription?.AddNewModels(stickyNoteModel);
        }

        /// <summary>
        /// Removes a sticky note from the graph.
        /// </summary>
        /// <param name="stickyNoteModel">The sticky note to remove.</param>
        protected virtual void RemoveStickyNote(StickyNoteModel stickyNoteModel)
        {
            UnregisterElement(stickyNoteModel);
            m_GraphStickyNoteModels.Remove(stickyNoteModel);
            CurrentGraphChangeDescription?.AddDeletedModels(stickyNoteModel);
        }

        /// <summary>
        /// Adds a placemat to the graph.
        /// </summary>
        /// <param name="placematModel">The placemat to add.</param>
        protected virtual void AddPlacemat(PlacematModel placematModel)
        {
            RegisterElement(placematModel);
            m_GraphPlacematModels.Add(placematModel);
            CurrentGraphChangeDescription?.AddNewModels(placematModel);
        }

        /// <summary>
        /// Removes a placemat from the graph.
        /// </summary>
        /// <param name="placematModel">The placemat to remove.</param>
        protected virtual void RemovePlacemat(PlacematModel placematModel)
        {
            UnregisterElement(placematModel);
            m_GraphPlacematModels.Remove(placematModel);
            CurrentGraphChangeDescription?.AddDeletedModels(placematModel);
        }

        /// <summary>
        /// Adds a variable declaration to the graph.
        /// </summary>
        /// <param name="variableDeclarationModel">The variable declaration to add.</param>
        protected virtual void AddVariableDeclaration(VariableDeclarationModel variableDeclarationModel)
        {
            RegisterElement(variableDeclarationModel);
            AddMetaData(variableDeclarationModel, m_GraphVariableModels.Count);
            m_GraphVariableModels.Add(variableDeclarationModel);
            m_ExistingVariableNames.Add(variableDeclarationModel.Title);
            CurrentGraphChangeDescription?.AddNewModels(variableDeclarationModel);
        }

        /// <summary>
        /// Removes a variable declaration from the graph.
        /// </summary>
        /// <param name="variableDeclarationModel">The variable declaration to remove.</param>
        protected virtual GroupModel RemoveVariableDeclaration(VariableDeclarationModel variableDeclarationModel)
        {
            if (variableDeclarationModel == null)
                return null;

            UnregisterElement(variableDeclarationModel);

            var indexToRemove = m_GraphVariableModels.IndexOf(variableDeclarationModel);
            if (indexToRemove != -1)
            {
                RemoveFromMetadata(indexToRemove, ManagedMissingTypeModelCategory.VariableDeclaration);
                m_GraphVariableModels.RemoveAt(indexToRemove);
                CurrentGraphChangeDescription?.AddDeletedModels(variableDeclarationModel);
            }
            m_ExistingVariableNames.Remove(variableDeclarationModel.Title);

            var parent = variableDeclarationModel.ParentGroup;
            parent?.RemoveItem(variableDeclarationModel);
            return parent;
        }

        /// <summary>
        /// Instantiates an object of type <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type of the object to instantiate.</param>
        /// <typeparam name="TBaseType">A base type for <paramref name="type"/>.</typeparam>
        /// <returns>A new object.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="type"/> does not derive from <typeparamref name="TBaseType"/></exception>
        protected static TBaseType Instantiate<TBaseType>(Type type)
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
            m_ElementsByGuid = new Dictionary<Hash128, GraphElementModel>();

            foreach (var model in m_GraphNodeModels)
            {
                RegisterElement(model);
            }

            foreach (var model in m_GraphWireModels)
            {
                RegisterElement(model);
            }

            foreach (var model in m_GraphStickyNoteModels)
            {
                RegisterElement(model);
            }

            foreach (var model in m_GraphPlacematModels)
            {
                RegisterElement(model);
            }

            // Some variables may not be under any section.
            foreach (var model in m_GraphVariableModels)
            {
                RegisterElement(model);
            }

            foreach (var model in m_GraphPortalModels)
            {
                RegisterElement(model);
            }

            foreach (var model in m_SectionModels)
            {
                RegisterElement(model);
            }
        }

        /// <summary>
        /// Creates a new node in the graph.
        /// </summary>
        /// <param name="nodeTypeToCreate">The type of the new node to create.</param>
        /// <param name="nodeName">The name of the node to create.</param>
        /// <param name="position">The position of the node to create.</param>
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the node is created.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <returns>The newly created node.</returns>
        public virtual AbstractNodeModel CreateNode(Type nodeTypeToCreate, string nodeName, Vector2 position,
            Hash128 guid = default, Action<AbstractNodeModel> initializationCallback = null, SpawnFlags spawnFlags = SpawnFlags.None)
        {
            if (Stencil is { AllowPortalCreation: false } && typeof(WirePortalModel).IsAssignableFrom(nodeTypeToCreate))
            {
                throw new ArgumentException("Portal creation is disabled by the Stencil.", nameof(nodeTypeToCreate));
            }

            var nodeModel = InstantiateNode(nodeTypeToCreate, nodeName, position, guid, initializationCallback, spawnFlags);

            if (!spawnFlags.IsOrphan() && nodeModel.Container == this)
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
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the node is created.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <returns>The newly created node.</returns>
        protected virtual AbstractNodeModel InstantiateNode(Type nodeTypeToCreate, string nodeName, Vector2 position,
            Hash128 guid = default, Action<AbstractNodeModel> initializationCallback = null, SpawnFlags spawnFlags = SpawnFlags.None)
        {
            if (nodeTypeToCreate == null)
                throw new ArgumentNullException(nameof(nodeTypeToCreate));

            if (Stencil is { AllowPortalCreation: false } && typeof(WirePortalModel).IsAssignableFrom(nodeTypeToCreate))
            {
                throw new ArgumentException("Portal creation is disabled by the Stencil.", nameof(nodeTypeToCreate));
            }

            AbstractNodeModel nodeModel;
            if (typeof(Constant).IsAssignableFrom(nodeTypeToCreate))
                nodeModel = new ConstantNodeModel { Value = (Constant)Activator.CreateInstance(nodeTypeToCreate) };
            else if (typeof(AbstractNodeModel).IsAssignableFrom(nodeTypeToCreate))
                nodeModel = (AbstractNodeModel)Activator.CreateInstance(nodeTypeToCreate);
            else
                throw new ArgumentOutOfRangeException(nameof(nodeTypeToCreate));

            if (nodeModel is IHasTitle titled)
                titled.Title = nodeName ?? nodeTypeToCreate.Name;

            nodeModel.SpawnFlags = spawnFlags;
            nodeModel.Position = position;
            if (guid.isValid)
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
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <returns>The newly created variable node.</returns>
        public virtual VariableNodeModel CreateVariableNode(VariableDeclarationModel declarationModel,
            Vector2 position, Hash128 guid = default, SpawnFlags spawnFlags = SpawnFlags.Default)
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
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <returns>The newly created subgraph node.</returns>
        public virtual SubgraphNodeModel CreateSubgraphNode(GraphModel referenceGraph, Vector2 position, Hash128 guid = default, SpawnFlags spawnFlags = SpawnFlags.Default)
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
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the constant node is created.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <returns>The newly created constant node.</returns>
        public virtual ConstantNodeModel CreateConstantNode(TypeHandle constantTypeHandle, string constantName, Vector2 position, Hash128 guid = default, Action<ConstantNodeModel> initializationCallback = null, SpawnFlags spawnFlags = SpawnFlags.None)
        {
            var constantType = Stencil.GetConstantType(constantTypeHandle);
            Debug.Assert(typeof(Constant).IsAssignableFrom(constantType));

            void PreDefineSetup(AbstractNodeModel model)
            {
                var constantNodeModel = model as ConstantNodeModel;
                Debug.Assert(constantNodeModel != null);
                constantNodeModel.Value.Initialize(constantTypeHandle);
                initializationCallback?.Invoke(constantNodeModel);
            }

            return CreateNode(constantType, constantName, position, guid, PreDefineSetup, spawnFlags) as ConstantNodeModel;
        }

        /// <summary>
        /// Duplicates a node and adds it to the graph.
        /// </summary>
        /// <param name="sourceNode">The node to duplicate. The node does not have to be in this graph.</param>
        /// <param name="delta">The position offset for the new node.</param>
        /// <returns>The new node.</returns>
        public virtual AbstractNodeModel DuplicateNode(AbstractNodeModel sourceNode, Vector2 delta)
        {
            if (Stencil is { AllowPortalCreation: false } && sourceNode is WirePortalModel)
            {
                throw new ArgumentException("Portal creation is disabled by the Stencil.", nameof(sourceNode));
            }

            var pastedNodeModel = sourceNode.Clone();

            // Set graphmodel BEFORE define node as it is commonly use during Define
            pastedNodeModel.GraphModel = this;
            pastedNodeModel.OnDuplicateNode(sourceNode);

            AddNode(pastedNodeModel);
            pastedNodeModel.Position += delta;

            return pastedNodeModel;
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

        /// <summary>
        /// Deletes a collection of nodes from the graph.
        /// </summary>
        /// <param name="nodeModels">The nodes to delete.</param>
        /// <param name="deleteConnections">Whether or not to delete the wires connected to the nodes.</param>
        public virtual void DeleteNodes(IReadOnlyCollection<AbstractNodeModel> nodeModels, bool deleteConnections)
        {
            var deletedElementsByContainer = new Dictionary<IGraphElementContainer, List<GraphElementModel>>();

            foreach (var nodeModel in nodeModels.Where(n => n.IsDeletable()))
            {
                if (!deletedElementsByContainer.TryGetValue(nodeModel.Container, out var deletedElements))
                {
                    deletedElements = new List<GraphElementModel>();
                    deletedElementsByContainer.Add(nodeModel.Container, deletedElements);
                }

                deletedElements.Add(nodeModel);

                if (deleteConnections)
                {
                    var connectedWires = nodeModel.GetConnectedWires().ToList();
                    DeleteWires(connectedWires);
                }

                // If all the portals with the given declaration are deleted, delete the declaration.
                if (nodeModel is WirePortalModel wirePortalModel &&
                    wirePortalModel.DeclarationModel != null &&
                    !this.FindReferencesInGraph<WirePortalModel>(wirePortalModel.DeclarationModel).Except(nodeModels).Any())
                {
                    if (wirePortalModel.DeclarationModel is PortalDeclarationPlaceholder placeholderModel)
                    {
                        RemovePlaceholder(placeholderModel);
                    }
                    else
                    {
                        RemovePortal(wirePortalModel.DeclarationModel);
                    }
                }

                nodeModel.OnDeleteNode();
            }

            foreach (var container in deletedElementsByContainer)
            {
                container.Key.RemoveElements(container.Value);
            }
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
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <returns>The newly created wire</returns>
        public virtual WireModel CreateWire(PortModel toPort, PortModel fromPort, Hash128 guid = default)
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
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <returns>The newly created wire</returns>
        protected virtual WireModel InstantiateWire(PortModel toPort, PortModel fromPort, Hash128 guid = default)
        {
            var wireType = GetWireType(toPort, fromPort);
            var wireModel = Instantiate<WireModel>(wireType);
            wireModel.GraphModel = this;
            if (guid.isValid)
                wireModel.SetGuid(guid);
            wireModel.SetPorts(toPort, fromPort);
            return wireModel;
        }

        /// <summary>
        /// Deletes wires from the graph.
        /// </summary>
        /// <param name="wireModels">The list of wires to delete.</param>
        public virtual void DeleteWires(IReadOnlyCollection<WireModel> wireModels)
        {
            // Call ToList on the collection to prevent iteration over the PortWireIndex.m_WiresByPort
            foreach (var wireModel in wireModels.Where(e => e != null && e.IsDeletable()).ToList())
            {
                if (wireModel is WirePlaceholder placeholder)
                {
                    RemovePlaceholder(placeholder);
                }
                else
                {
                    wireModel.ToPort?.NodeModel?.OnDisconnection(wireModel.ToPort, wireModel.FromPort);
                    wireModel.FromPort?.NodeModel?.OnDisconnection(wireModel.FromPort, wireModel.ToPort);

                    CurrentGraphChangeDescription?.AddChangedModel(wireModel.ToPort, ChangeHint.GraphTopology);
                    CurrentGraphChangeDescription?.AddChangedModel(wireModel.FromPort, ChangeHint.GraphTopology);

                    RemoveWire(wireModel);
                }
            }
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
        public void DeleteStickyNotes(IReadOnlyCollection<StickyNoteModel> stickyNoteModels)
        {
            foreach (var stickyNoteModel in stickyNoteModels.Where(s => s.IsDeletable()))
            {
                RemoveStickyNote(stickyNoteModel);
                stickyNoteModel.Destroy();
            }
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
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <returns>The newly created placemat</returns>
        public PlacematModel CreatePlacemat(Rect position, Hash128 guid = default, SpawnFlags spawnFlags = SpawnFlags.Default)
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
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <returns>The newly created placemat</returns>
        protected virtual PlacematModel InstantiatePlacemat(Rect position, Hash128 guid)
        {
            var placematModelType = GetPlacematType();
            var placematModel = Instantiate<PlacematModel>(placematModelType);
            placematModel.TitleFontSize = 18;
            placematModel.TitleAlignment = TextAlignment.Left;
            placematModel.PositionAndSize = position;
            placematModel.GraphModel = this;
            if (guid.isValid)
                placematModel.SetGuid(guid);
            return placematModel;
        }

        /// <summary>
        /// Deletes placemats from the graph.
        /// </summary>
        /// <param name="placematModels">The list of placemats to delete.</param>
        public void DeletePlacemats(IReadOnlyCollection<PlacematModel> placematModels)
        {
            foreach (var placematModel in placematModels.Where(p => p.IsDeletable()))
            {
                RemovePlacemat(placematModel);
                placematModel.Destroy();
            }
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
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <param name="spawnFlags">The flags specifying how the variable declaration is to be spawned.</param>
        /// <returns>The newly created variable declaration.</returns>
        public virtual VariableDeclarationModel CreateGraphVariableDeclaration(TypeHandle variableDataType, string variableName,
            ModifierFlags modifierFlags, bool isExposed, GroupModel group = null, int indexInGroup = int.MaxValue, Constant initializationModel = null, Hash128 guid = default,
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
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the variable declaration is created.</param>
        /// <param name="spawnFlags">The flags specifying how the variable declaration is to be spawned.</param>
        /// <returns>The newly created variable declaration.</returns>
        public virtual VariableDeclarationModel CreateGraphVariableDeclaration(Type variableTypeToCreate,
            TypeHandle variableDataType, string variableName, ModifierFlags modifierFlags, bool isExposed,
            GroupModel group = null, int indexInGroup = Int32.MaxValue, Constant initializationModel = null,
            Hash128 guid = default, Action<VariableDeclarationModel, Constant> initializationCallback = null,
            SpawnFlags spawnFlags = SpawnFlags.None)
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

            m_PlaceholderData[guid] = new PlaceholderData { GroupTitle = variableDeclaration.ParentGroup.Title };

            return variableDeclaration;
        }

        /// <summary>
        /// Generates a unique name for a variable declaration in the graph.
        /// </summary>
        /// <param name="originalName">The name of the variable declaration.</param>
        /// <returns>The unique name for the variable declaration.</returns>
        protected virtual string GenerateGraphVariableDeclarationUniqueName(string originalName)
        {
            var names = m_ExistingVariableNames.ToArray();
            var name = ObjectNames.GetUniqueName(names, originalName);
            return name;
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
        /// <param name="guid">The guid to assign to the newly created item. If none is provided, a new
        /// guid will be generated for it.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the variable declaration is created.</param>
        /// <returns>The newly created variable declaration.</returns>
        protected virtual VariableDeclarationModel InstantiateVariableDeclaration(Type variableTypeToCreate,
            TypeHandle variableDataType, string variableName, ModifierFlags modifierFlags, bool isExposed,
            Constant initializationModel, Hash128 guid, Action<VariableDeclarationModel, Constant> initializationCallback = null)
        {
            var variableDeclaration = Instantiate<VariableDeclarationModel>(variableTypeToCreate);

            if (guid.isValid)
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
            // Group is not added to the graph: it will be added to a section.
            RegisterElement(group);
            CurrentGraphChangeDescription?.AddNewModels(group);
        }

        protected virtual void RemoveGroup(GroupModel group)
        {
            UnregisterElement(group);
            CurrentGraphChangeDescription?.AddDeletedModels(group);
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
        public virtual void DeleteVariableDeclarations(IReadOnlyCollection<VariableDeclarationModel> variableModels, bool deleteUsages = true)
        {
            foreach (var variableModel in variableModels.Where(v => v.IsDeletable()))
            {
                if (variableModel is VariableDeclarationPlaceholder placeholderModel)
                    RemovePlaceholder(placeholderModel);

                RemoveVariableDeclaration(variableModel);

                if (deleteUsages)
                {
                    var nodesToDelete = this.FindReferencesInGraph(variableModel).Cast<AbstractNodeModel>().ToList();
                    DeleteNodes(nodesToDelete, deleteConnections: true);
                }
            }
        }

        /// <summary>
        /// Deletes the given group models.
        /// </summary>
        /// <param name="groupModels">The group models to delete.</param>
        public void DeleteGroups(IReadOnlyCollection<GroupModel> groupModels)
        {
            var deletedModels = new List<GraphElementModel>();
            var deletedVariables = new List<VariableDeclarationModel>();

            void RecurseRemoveGroup(GroupModel groupModel)
            {
                RemoveGroup(groupModel);
                foreach (var item in groupModel.Items)
                {
                    if (item is VariableDeclarationModel variable)
                        deletedVariables.Add(variable);
                    else if (item is GroupModel group)
                        RecurseRemoveGroup(group);
                    else
                        deletedModels.Add(item as GraphElementModel);
                }
            }

            foreach (var groupModel in groupModels.Where(v => v.IsDeletable()))
            {
                groupModel.ParentGroup?.RemoveItem(groupModel);
                RecurseRemoveGroup(groupModel);
            }

            DeleteVariableDeclarations(deletedVariables);
            CurrentGraphChangeDescription?.AddDeletedModels(deletedModels);
        }

        protected virtual Type GetPortalType()
        {
            return typeof(DeclarationModel);
        }

        /// <summary>
        /// Creates a new declaration model representing a portal and optionally add it to the graph.
        /// </summary>
        /// <param name="portalName">The name of the portal</param>
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <param name="spawnFlags">The flags specifying how the portal is to be spawned.</param>
        /// <returns>The newly created declaration model</returns>
        public virtual DeclarationModel CreateGraphPortalDeclaration(string portalName, Hash128 guid = default, SpawnFlags spawnFlags = SpawnFlags.None)
        {
            if (Stencil is { AllowPortalCreation: false })
            {
                throw new InvalidOperationException("Portal creation is disabled by the Stencil");
            }

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
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <returns>The newly created declaration model</returns>
        protected virtual DeclarationModel InstantiatePortalDeclaration(string portalName, Hash128 guid = default)
        {
            if (Stencil is { AllowPortalCreation: false })
            {
                throw new InvalidOperationException("Portal creation is disabled by the Stencil");
            }

            var portalModelType = GetPortalType();
            var portalModel = Instantiate<DeclarationModel>(portalModelType);
            portalModel.Title = portalName;
            if (guid.isValid)
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
            if (Stencil is { AllowPortalCreation: false })
            {
                throw new InvalidOperationException("Portal creation is disabled by the Stencil");
            }

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
        public virtual void CreatePortalsFromWire(WireModel wireModel, Vector2 entryPortalPosition, Vector2 exitPortalPosition, int portalHeight,
            Dictionary<PortModel, WirePortalModel> existingPortalEntries, Dictionary<PortModel, List<WirePortalModel>> existingPortalExits)
        {
            if (Stencil is { AllowPortalCreation: false })
            {
                throw new InvalidOperationException("Portal creation is disabled by the Stencil");
            }

            var inputPortModel = wireModel.ToPort;
            var outputPortModel = wireModel.FromPort;

            // Only a single portal per output port. Don't recreate if we already created one.
            WirePortalModel portalEntry = null;

            if (outputPortModel != null && !existingPortalEntries.TryGetValue(wireModel.FromPort, out portalEntry))
            {
                portalEntry = CreateEntryPortalFromPort(outputPortModel, entryPortalPosition, portalHeight);
                wireModel.SetPort(WireSide.To, (portalEntry as ISingleInputPortNodeModel)?.InputPort);
                existingPortalEntries[outputPortModel] = portalEntry;
                CurrentGraphChangeDescription?.AddChangedModel(wireModel, ChangeHint.Layout);
            }
            else
            {
                DeleteWires(new[] { wireModel });
            }

            // We can have multiple portals on input ports however
            if (!existingPortalExits.TryGetValue(wireModel.ToPort, out var portalExits))
            {
                portalExits = new List<WirePortalModel>();
                existingPortalExits[wireModel.ToPort] = portalExits;
            }

            var portalExit = CreateExitPortalToPort(inputPortModel, exitPortalPosition, portalHeight, portalEntry);
            portalExits.Add(portalExit);

            CreateWire(inputPortModel, (portalExit as ISingleOutputPortNodeModel)?.OutputPort);
        }

        /// <summary>
        /// Creates an exit portal matching a port.
        /// </summary>
        /// <param name="outputPortModel">The output port model to which the portal will be connected.</param>
        /// <param name="position">The desired position of the entry portal.</param>
        /// <param name="height">The desired height of the entry portal.</param>
        /// <returns>The created entry portal.</returns>
        public virtual WirePortalModel CreateEntryPortalFromPort(PortModel outputPortModel, Vector2 position, int height)
        {
            if (Stencil is { AllowPortalCreation: false })
            {
                throw new InvalidOperationException("Portal creation is disabled by the Stencil");
            }

            WirePortalModel portalEntry;

            if (!(outputPortModel.NodeModel is InputOutputPortsNodeModel nodeModel))
                return null;

            if (outputPortModel.PortType == PortType.Execution)
                portalEntry = this.CreateNode<ExecutionWirePortalEntryModel>();
            else
                portalEntry = this.CreateNode<DataWirePortalEntryModel>(initializationCallback: n => n.PortDataTypeHandle = outputPortModel.DataTypeHandle);

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

            return portalEntry;
        }

        /// <summary>
        /// Creates an exit portal matching a port.
        /// </summary>
        /// <param name="inputPortModel">The input port model to which the portal will be connected.</param>
        /// <param name="position">The desired position of the exit portal.</param>
        /// <param name="height">The desired height of the exit portal.</param>
        /// <param name="entryPortal">The corresponding entry portal.</param>
        /// <returns>The created exit portal.</returns>
        public virtual WirePortalModel CreateExitPortalToPort(PortModel inputPortModel, Vector2 position, int height, WirePortalModel entryPortal)
        {
            if (Stencil is { AllowPortalCreation: false })
            {
                throw new InvalidOperationException("Portal creation is disabled by the Stencil");
            }

            WirePortalModel portalExit;

            if (inputPortModel.PortType == PortType.Execution)
                portalExit = this.CreateNode<ExecutionWirePortalExitModel>();
            else
                portalExit = this.CreateNode<DataWirePortalExitModel>(initializationCallback: n => n.PortDataTypeHandle = inputPortModel.DataTypeHandle);

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

            Stencil.OnGraphModelEnabled();
        }

        /// <summary>
        /// Tasks to perform when the <see cref="GraphAsset"/> is disabled.
        /// </summary>
        public virtual void OnDisable()
        {
            Stencil.OnGraphModelDisabled();
        }

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
            foreach (var nodeModel in NodeModels)
            {
                RecurseDefineNode(nodeModel);
            }

            Asset.Dirty = true;
        }

        /// <summary>
        /// Updates the graph model when loading the graph.
        /// </summary>
        public virtual void OnLoadGraph()
        {
            AddGraphPlaceholders();

            // This is necessary because we can load a graph in the tool without OnEnable(),
            // which calls OnDefineNode(), being called (yet).
            // Also, PortModel.OnAfterDeserialized(), which resets port caches, is not necessarily called,
            // since the graph may already have been loaded by the AssetDatabase a long time ago.

            // The goal of this is to create the missing ports when subgraph variables get deleted.
            foreach (var nodeModel in NodeModels.OfType<NodeModel>())
                nodeModel.DefineNode();

            foreach (var wireModel in WireModels)
            {
                if (wireModel == null)
                    continue;
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
            var existingGuids = new Dictionary<Hash128, int>(nodesAndBlocks.Count);

            for (var i = 0; i < nodesAndBlocks.Count; i++)
            {
                AbstractNodeModel node = nodesAndBlocks[i];

                Assert.IsTrue(node.GraphModel != null, $"Node {i} {node} graph is null");
                Assert.IsNotNull(node, $"Node {i} is null");
                Assert.IsTrue(ReferenceEquals(this, node.GraphModel), $"Node {i} graph is not matching its actual graph");
                Assert.IsFalse(!node.Guid.isValid, $"Node {i} ({node.GetType()}) has an empty Guid");
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
        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();

            if (StencilType != null)
                m_StencilTypeName = StencilType.AssemblyQualifiedName;
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

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
            foreach (var model in m_GraphNodeModels.Where(m => m != null))
            {
                model.GraphModel = this;
            }

            foreach (var model in m_GraphWireModels.Where(m => m != null))
            {
                model.GraphModel = this;
            }

            foreach (var model in m_GraphStickyNoteModels.Where(m => m != null))
            {
                model.GraphModel = this;
            }

            foreach (var model in m_GraphPlacematModels.Where(m => m != null))
            {
                model.GraphModel = this;
            }

            foreach (var model in m_GraphVariableModels.Where(m => m != null))
            {
                model.GraphModel = this;
            }

            foreach (var model in m_GraphPortalModels.Where(m => m != null))
            {
                model.GraphModel = this;
            }

            foreach (var model in m_SectionModels.Where(m => m != null))
            {
                model.GraphModel = this;
            }

            m_ExistingVariableNames = new HashSet<string>(VariableDeclarations.Count);
            foreach (var declarationModel in VariableDeclarations)
            {
                if (declarationModel != null) // in case of bad serialized graph - breaks a test if not tested
                    m_ExistingVariableNames.Add(declarationModel.Title);
            }

            ResetCaches();
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
                {
                    RemoveSection(section);
                }
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
            }

            foreach (var placemat in sourceGraphModel.PlacematModels)
            {
                var newPosition = new Rect(placemat.PositionAndSize.position, placemat.PositionAndSize.size);
                var pastedPlacemat = CreatePlacemat(newPosition);
                pastedPlacemat.Title = placemat.Title;
                pastedPlacemat.Color = placemat.Color;
            }
        }

        void AddGraphPlaceholders()
        {
            RemoveUnmanagedNullElements();

            // Get the indexes of null models (used to create placeholders for models which data was not serialized properly).
            var remainingNullModelIndexes = new List<(ManagedMissingTypeModelCategory, int)>();
            var contextWithNullBlocks = new List<ContextNodeModel>();
            for (var i = 0; i < NodeModels.Count; i++)
            {
                var node = NodeModels.ElementAt(i);
                if (node == null)
                {
                    var metadata = m_GraphElementMetaData.Where(m => m.Category is ManagedMissingTypeModelCategory.Node or ManagedMissingTypeModelCategory.ContextNode).FirstOrDefault(m => m.Index == i);
                    if (metadata != null)
                        remainingNullModelIndexes.Add((metadata.Category, i));
                }
                else if (node is ContextNodeModel contextNodeModel && contextNodeModel.GraphElementModels.Any(ge => ge == null))
                {
                    contextWithNullBlocks.Add(contextNodeModel);
                }
            }
            for (var i = 0; i < VariableDeclarations.Count; i++)
            {
                if (VariableDeclarations[i] == null)
                    remainingNullModelIndexes.Add((ManagedMissingTypeModelCategory.VariableDeclaration, i));
            }
            for (var i = 0; i < WireModels.Count; i++)
            {
                if (WireModels[i] == null)
                    remainingNullModelIndexes.Add((ManagedMissingTypeModelCategory.Wire, i));
            }
            for (var i = 0; i < PortalDeclarations.Count; i++)
            {
                if (PortalDeclarations[i] == null)
                    remainingNullModelIndexes.Add((ManagedMissingTypeModelCategory.PortalDeclaration, i));
            }

            // Add new placeholders using managed references with missing types.
            foreach (var referenceWithMissingType in SerializationUtility.GetManagedReferencesWithMissingTypes(Asset))
            {
                if (YamlParsingHelper_Internal.TryParseGUID(referenceWithMissingType.serializedData, hashGuidFieldName_Internal, obsoleteGuidFieldName_Internal, 0, out var guid))
                {
                    var metadataIndex = m_GraphElementMetaData.FindIndex(m => m.Guid == guid);

                    var metadata = metadataIndex == -1 ? null : m_GraphElementMetaData[metadataIndex];
                    if (TryGetModelFromGuid(guid, out _))
                    {
                        Debug.LogWarning("There is already an existing model with that guid.");
                        if (metadata != null)
                            remainingNullModelIndexes.Remove((metadata.Category, metadata.Index));
                    }
                    else if (metadata == null)
                    {
                        // Blocks are not added to the metadata. Check if it is a block node. If yes, create a placeholder for it.
                        PlaceholderModelHelper.TryCreatePlaceholder_Internal(this, ManagedMissingTypeModelCategory.BlockNode, referenceWithMissingType, guid, out _);
                    }
                    else if (PlaceholderModelHelper.TryCreatePlaceholder_Internal(this, metadata.Category, referenceWithMissingType, guid, out var createdPlaceholder))
                    {
                        SaveNodePositionForMetadata(createdPlaceholder);
                        remainingNullModelIndexes.Remove((metadata.Category, metadata.Index));
                    }
                }
            }

            // Create placeholders for null models for which the data is not serialized anymore.
            foreach (var (category, index) in remainingNullModelIndexes)
            {
                var metadata = m_GraphElementMetaData.FirstOrDefault(m => m.Category == category && m.Index == index);
                if (metadata != null)
                {
                    if (metadata.ToRemove)
                    {
                        RemoveMetaDataModel(metadata);
                    }
                    else if (!TryGetModelFromGuid(metadata.Guid, out _))
                    {
                        switch (metadata.Category)
                        {
                            case ManagedMissingTypeModelCategory.Node:
                                CreateNodePlaceholder_Internal(PlaceholderModelHelper.missingTypeWontBeRestored, m_PlaceholderData[metadata.Guid].Position, metadata.Guid);
                                break;
                            case ManagedMissingTypeModelCategory.VariableDeclaration:
                                CreateVariableDeclarationPlaceholder_Internal(PlaceholderModelHelper.missingTypeWontBeRestored, metadata.Guid);
                                break;
                            case ManagedMissingTypeModelCategory.Wire:
                                RemoveWire(WireModels[index]); // We don't have the data for the ports.
                                break;
                            case ManagedMissingTypeModelCategory.PortalDeclaration:
                                CreatePortalDeclarationPlaceholder_Internal(PlaceholderModelHelper.missingTypeWontBeRestored, metadata.Guid);
                                break;
                            case ManagedMissingTypeModelCategory.ContextNode:
                                CreateContextNodePlaceholder_Internal(PlaceholderModelHelper.missingTypeWontBeRestored, m_PlaceholderData[metadata.Guid].Position, metadata.Guid);
                                break;
                        }
                    }
                }
            }
            foreach (var context in contextWithNullBlocks)
            {
                for (var i = 0; i < context.BlockCount; ++i)
                {
                    var block = context.GetBlock(i);
                    // Create placeholders for null models for which the data is not serialized anymore.
                    if (block == null && context.BlockPlaceholders.All(t => t.Guid != context.BlockGuids[i]))
                        CreateBlockNodePlaceholder_Internal(PlaceholderModelHelper.missingTypeWontBeRestored, context.BlockGuids[i], context);
                }
            }
        }

        void SaveNodePositionForMetadata(IPlaceholder createdPlaceholder)
        {
            // The node position needs to be kept in the metadata to be able to recreate the placeholder at the right position.
            if (createdPlaceholder is not NodePlaceholder nodeModel)
                return;

            if (m_PlaceholderData.ContainsKey(nodeModel.Guid))
                m_PlaceholderData[nodeModel.Guid].Position = nodeModel.Position;
            else
                m_PlaceholderData[nodeModel.Guid] = new PlaceholderData { Position = nodeModel.Position };
        }

        void RemoveUnmanagedNullElements()
        {
            m_GraphStickyNoteModels.RemoveAll(t => t == null);
            m_GraphPlacematModels.RemoveAll(t => t == null);
            m_SectionModels.ForEach(t => t.Repair());
        }

        void RemovePlaceholder(IPlaceholder placeholder)
        {
            if (TryGetModelFromGuid(placeholder.Guid, out var model))
            {
                UnregisterElement(model);
                CurrentGraphChangeDescription?.AddDeletedModels(model);
            }

            // Clear the serialized data related to the null object the user wants to remove.
            SerializationUtility.ClearManagedReferenceWithMissingType(Asset, placeholder.ReferenceId);

            var metadata = m_GraphElementMetaData.FirstOrDefault(m => m.Guid == placeholder.Guid);

            // It is not possible to distinguish the index of objects with a missing type in the serialization. Hence, we keep a flag and remove the corresponding null object on the next graph reload.
            if (metadata != null)
                metadata.ToRemove = true;

            // Remove the placeholder
            m_Placeholders.Remove(placeholder);
        }

        void RemoveMetaDataModel(GraphElementMetaData metadata)
        {
            if (metadata.Index == -1)
                return;

            switch (metadata.Category)
            {
                case ManagedMissingTypeModelCategory.Node:
                case ManagedMissingTypeModelCategory.ContextNode:
                    m_GraphNodeModels.RemoveAt(metadata.Index);
                    break;
                case ManagedMissingTypeModelCategory.VariableDeclaration:
                    m_GraphVariableModels.RemoveAt(metadata.Index);
                    break;
                case ManagedMissingTypeModelCategory.Wire:
                    m_GraphWireModels.RemoveAt(metadata.Index);
                    break;
                case ManagedMissingTypeModelCategory.PortalDeclaration:
                    m_GraphPortalModels.RemoveAt(metadata.Index);
                    break;
            }

            // Remove the associated metadata
            RemoveFromMetadata(metadata.Index, metadata.Category);
        }

        internal NodePlaceholder CreateNodePlaceholder_Internal(string nodeName, Vector2 position, Hash128 guid, long referenceId = -1)
        {
            var node = InstantiateNode(typeof(NodePlaceholder), nodeName, position, guid) as NodePlaceholder;
            RegisterElement(node);
            CurrentGraphChangeDescription?.AddNewModels(node);

            if (node != null && referenceId != -1)
                node.ReferenceId = referenceId;

            m_Placeholders.Add(node);

            return node;
        }

        internal ContextNodePlaceholder CreateContextNodePlaceholder_Internal(string nodeName, Vector2 position, Hash128 guid, IEnumerable<BlockNodeModel> blocks = null, long referenceId = -1)
        {
            var contextNode = InstantiateNode(typeof(ContextNodePlaceholder), nodeName, position, guid) as ContextNodePlaceholder;
            RegisterElement(contextNode);
            CurrentGraphChangeDescription?.AddNewModels(contextNode);

            if (contextNode != null && referenceId != -1)
                contextNode.ReferenceId = referenceId;

            m_Placeholders.Add(contextNode);

            if (contextNode is ContextNodeModel contextNodeModel)
            {
                var blockGuids = blocks?.Select(b => b.Guid).ToList();
                if (blockGuids == null)
                {
                    if (m_PlaceholderData.TryGetValue(guid, out var data))
                        blockGuids = data.BlockGuids;
                }
                else
                {
                    // Keep the blocks' guids to be able to recreate the placeholder in case the missing type serialized data is lost.
                    m_PlaceholderData ??= new SerializedValueDictionary<Hash128, PlaceholderData>();
                    if (m_PlaceholderData.ContainsKey(guid))
                        m_PlaceholderData[guid].BlockGuids = blockGuids;
                    else
                        m_PlaceholderData[guid] = new PlaceholderData { BlockGuids = blockGuids };
                }

                if (blockGuids != null)
                {
                    foreach (var blockGuid in blockGuids)
                        CreateBlockNodePlaceholder_Internal("! Missing ! The context node has a missing type.", blockGuid, contextNodeModel);
                }
            }

            return contextNode;
        }

        internal BlockNodePlaceholder CreateBlockNodePlaceholder_Internal(string nodeName, Hash128 guid, ContextNodeModel contextNodeModel, long referenceId = -1)
        {
            var node = InstantiateNode(typeof(BlockNodePlaceholder), nodeName, Vector2.zero, guid) as BlockNodePlaceholder;
            RegisterElement(node);
            CurrentGraphChangeDescription?.AddNewModels(node);

            if (node != null && referenceId != -1)
                node.ReferenceId = referenceId;

            if (node is BlockNodeModel blockNodeModel && contextNodeModel != null)
                contextNodeModel.InsertBlock(blockNodeModel, spawnFlags: SpawnFlags.Orphan);

            return node;
        }

        internal VariableDeclarationPlaceholder CreateVariableDeclarationPlaceholder_Internal(string variableName, Hash128 guid, long referenceId = -1)
        {
            var variableDeclaration = InstantiateVariableDeclaration(typeof(VariableDeclarationPlaceholder), TypeHandle.MissingType,
                variableName, ModifierFlags.None, false, null, guid) as VariableDeclarationPlaceholder;

            RegisterElement(variableDeclaration);
            CurrentGraphChangeDescription?.AddNewModels(variableDeclaration);

            if (variableDeclaration != null && referenceId != -1)
                variableDeclaration.ReferenceId = referenceId;

            var group = m_PlaceholderData.TryGetValue(guid, out var data) ? GetSectionModel(data.GroupTitle) : GetSectionModel(variableDeclaration?.GraphModel.Stencil.GetVariableSection(variableDeclaration));
            group.InsertItem(variableDeclaration);

            m_Placeholders.Add(variableDeclaration);

            return variableDeclaration;
        }

        internal PortalDeclarationPlaceholder CreatePortalDeclarationPlaceholder_Internal(string portalName, Hash128 guid, long referenceId = -1)
        {
            var portalModel = Instantiate<PortalDeclarationPlaceholder>(typeof(PortalDeclarationPlaceholder));
            portalModel.Title = portalName;

            if (guid.isValid)
                portalModel.SetGuid(guid);

            portalModel.GraphModel = this;

            RegisterElement(portalModel);
            CurrentGraphChangeDescription?.AddNewModels(portalModel);

            if (referenceId != -1)
                portalModel.ReferenceId = referenceId;

            m_Placeholders.Add(portalModel);

            return portalModel;
        }

        internal WirePlaceholder CreateWirePlaceholder_Internal(PortModel toPort, PortModel fromPort, Hash128 guid, long referenceId = -1)
        {
            var existing = this.GetWireConnectedToPorts(toPort, fromPort);
            if (existing != null)
                return existing as WirePlaceholder;

            var wireModel = Instantiate<WirePlaceholder>(typeof(WirePlaceholder));
            wireModel.GraphModel = this;

            if (guid.isValid)
                wireModel.SetGuid(guid);

            wireModel.SetPorts(toPort, fromPort);

            RegisterElement(wireModel);
            CurrentGraphChangeDescription?.AddNewModels(wireModel);

            if (referenceId != -1)
                wireModel.ReferenceId = referenceId;

            m_Placeholders.Add(wireModel);
            m_PortWireIndex?.WireAdded(wireModel);

            return wireModel;
        }

        /// <inheritdoc />
        public virtual void Repair()
        {
            m_GraphNodeModels.RemoveAll(t => t is null or IPlaceholder);
            m_GraphNodeModels.RemoveAll(t => t is VariableNodeModel variable && variable.DeclarationModel == null);

            foreach (var container in m_GraphNodeModels.OfType<IGraphElementContainer>())
            {
                container.Repair();
            }

            var validGuids = new HashSet<Hash128>(m_GraphNodeModels.Select(t => t.Guid));

            m_GraphWireModels.RemoveAll(t => t is null or IPlaceholder);
            m_GraphWireModels.RemoveAll(t => !validGuids.Contains(t.FromNodeGuid) || !validGuids.Contains(t.ToNodeGuid));
            m_GraphStickyNoteModels.RemoveAll(t => t == null);
            m_GraphPlacematModels.RemoveAll(t => t == null);
            m_GraphVariableModels.RemoveAll(t => t is null);
            m_GraphPortalModels.RemoveAll(t => t is null);
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

        internal GraphChangeDescription PushNewGraphChangeDescription_Internal()
        {
            var changes = new GraphChangeDescription();
            m_GraphChangeDescriptionStack.Push(changes);
            return changes;
        }

        internal void PopGraphChangeDescription_Internal()
        {
            if (!m_GraphChangeDescriptionStack.TryPop(out var currentScopeChange))
                return;
            if (!m_GraphChangeDescriptionStack.TryPeek(out var outerScopeChanges))
                return;
            outerScopeChanges.Union(currentScopeChange);
        }
    }
}
