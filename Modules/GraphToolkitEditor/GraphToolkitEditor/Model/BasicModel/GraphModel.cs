// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Spawn flags. A spawn flag dictates multiple operations during the NodeModels creation.
    /// </summary>
    [Flags]
    [UnityRestricted]
    internal enum SpawnFlags
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
    [UnityRestricted]
    internal enum Verbosity
    {
        /// <summary>
        /// The lowest verbosity level. Displays only errors.
        /// </summary>
        Errors,
        /// <summary>
        /// The highest verbosity level. Displays the most detailed information.
        /// </summary>
        Verbose
    }

    /// <summary>
    /// Extension methods for <see cref="SpawnFlags"/>.
    /// </summary>
    [UnityRestricted]
    internal static class SpawnFlagsExtensions
    {
        /// <summary>
        /// Whether <paramref name="f"/> has the <see cref="SpawnFlags.Orphan"/> set.
        /// </summary>
        /// <param name="f">The flag set to check.</param>
        /// <returns>True if <paramref name="f"/> has the <see cref="SpawnFlags.Orphan"/> set.</returns>
        public static bool IsOrphan(this SpawnFlags f) => (f & SpawnFlags.Orphan) != 0;
    }

    struct ElementsByType
    {
        public HashSet<StickyNoteModel> StickyNoteModels;
        public HashSet<PlacematModel> PlacematModels;
        public HashSet<VariableDeclarationModelBase> VariableDeclarationsModels;
        public HashSet<GroupModel> GroupModels;
        public HashSet<WireModel> WireModels;
        public HashSet<AbstractNodeModel> NodeModels;

        public ElementsByType(IEnumerable<GraphElementModel> graphElementModels)
        {
            StickyNoteModels = new HashSet<StickyNoteModel>();
            PlacematModels = new HashSet<PlacematModel>();
            VariableDeclarationsModels = new HashSet<VariableDeclarationModelBase>();
            GroupModels = new HashSet<GroupModel>();
            WireModels = new HashSet<WireModel>();
            NodeModels = new HashSet<AbstractNodeModel>();

            RecursiveSortElements(graphElementModels);
        }

        void RecursiveSortElements(IEnumerable<GraphElementModel> graphElementModels)
        {
            foreach (var element in graphElementModels)
            {
                if (element is IGraphElementContainer container)
                    RecursiveSortElements(container.GetGraphElementModels());
                switch (element)
                {
                    case StickyNoteModel stickyNoteModel:
                        StickyNoteModels.Add(stickyNoteModel);
                        break;
                    case PlacematModel placematModel:
                        PlacematModels.Add(placematModel);
                        break;
                    case VariableDeclarationModelBase variableDeclarationModel:
                        VariableDeclarationsModels.Add(variableDeclarationModel);
                        break;
                    case GroupModel groupModel:
                        GroupModels.Add(groupModel);
                        break;
                    case WireModel wireModel:
                        WireModels.Add(wireModel);
                        break;
                    case AbstractNodeModel nodeModel:
                        NodeModels.Add(nodeModel);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// A model that represents a graph.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal abstract partial class GraphModel : Model, IGraphElementContainer, IHasTitle, IObjectClonedCallbackReceiver, ICopyPasteCallbackReceiver
    {
        class DirtyScope : IDisposable
        {
            GraphModel m_GraphModel;
            bool m_PreventDirty;
            bool m_Dirty;

            public bool Dirty
            {
                get => m_Dirty;
                set
                {
                    if (m_PreventDirty)
                    {
                        return;
                    }

                    m_Dirty = value;
                }
            }

            /// <summary>
            /// Creates an instance of a <see cref="DirtyScope"/>.
            /// </summary>
            /// <param name="graphModel">The graph associated with the dirty scope.</param>
            /// <param name="preventDirty">Whether the dirty scope must be blocked.</param>
            /// <remarks>A dirty scope accumulates calls to <see cref="GraphModel.SetGraphObjectDirty"/>.</remarks>
            public DirtyScope(GraphModel graphModel, bool preventDirty)
            {
                m_GraphModel = graphModel;
                m_PreventDirty = preventDirty;

                if (!m_PreventDirty)
                {
                    m_Dirty = m_GraphModel.GraphObject != null && m_GraphModel.GraphObject.Dirty;
                }
                else
                {
                    m_Dirty = false;
                }
            }

            /// <inheritdoc />
            public void Dispose()
            {
                m_GraphModel.EndDirtyScope();
            }
        }

        static NullGraphChangeDescription s_NullGraphChangeDescription = new();
        Stack<GraphChangeDescription> m_GraphChangeDescriptionStack;
        Stack<DirtyScope> m_DirtyScopes;

        /// <summary>
        /// The name of the default section.
        /// </summary>
        public static string DefaultSectionName => string.Empty;

        [SerializeField, HideInInspector]
        string m_Name;

        [SerializeReference]
        List<AbstractNodeModel> m_GraphNodeModels;

        [SerializeReference, FormerlySerializedAs("m_GraphEdgeModels")]
        List<WireModel> m_GraphWireModels;

        [SerializeReference]
        List<StickyNoteModel> m_GraphStickyNoteModels;

        [SerializeReference]
        List<PlacematModel> m_GraphPlacematModels;

        [SerializeReference]
        List<VariableDeclarationModelBase> m_GraphVariableModels;

        [SerializeReference]
        List<DeclarationModel> m_GraphPortalModels;

        [SerializeReference]
        List<SectionModel> m_SectionModels;

        [SerializeReference]
        List<GraphModel> m_LocalSubgraphs;

        List<IPlaceholder> m_Placeholders;

        [FormerlySerializedAs("m_Bounds")]
        [SerializeField, HideInInspector]
        Rect m_LastKnownBounds;

        [SerializeField]
        [HideInInspector]
        List<GraphElementMetaData> m_GraphElementMetaData;

        [FormerlySerializedAs("m_DefaultEnterState")]
        [SerializeReference]
        [HideInInspector]
        AbstractNodeModel m_EntryPoint;

        SerializedValueDictionary<Hash128, PlaceholderData> m_PlaceholderData;

        /// <summary>
        /// Holds created variables names to make creation of unique names faster.
        /// </summary>
        HashSet<string> m_ExistingVariableNames;

        // As this field is not serialized, use GetElementsByGuid() to access it.
        Dictionary<Hash128, GraphElementModel> m_ElementsByGuid;

        PortWireIndex<WireModel> m_PortWireIndex;

        GraphProcessorContainer m_GraphProcessorContainer;

        /// <summary>
        /// Creates a helper for creating subgraph nodes within the graph.
        /// </summary>
        protected virtual SubgraphCreationHelper CreateSubgraphCreationHelper() => new SubgraphCreationHelper();

        /// <inheritdoc />
        string IHasTitle.Title
        {
            get => Name;
            set {}
        }

        /// <summary>
        /// The list of valid section names for the graph.
        /// </summary>
        public virtual IReadOnlyList<string> AdditionalSectionNames { get; } = Array.Empty<string>();

        /// <summary>
        /// Whether to show the default section in the blackboard. Default is true.
        /// </summary>
        public virtual bool ShowDefaultSectionInBlackboard => true;

        /// <summary>
        /// The graph object that holds this graph.
        /// </summary>
        public GraphObject GraphObject { get; private set; }

        /// <summary>
        /// Whether it is allowed to have multiple instances of a data output variable.
        /// </summary>
        public virtual AllowMultipleDataOutputInstances AllowMultipleDataOutputInstances => AllowMultipleDataOutputInstances.AllowWithWarning;

        /// <summary>
        /// Whether it is allowed to create <see cref="WirePortalModel"/> and add them to the graph.
        /// </summary>
        public virtual bool AllowPortalCreation => true;

        /// <summary>
        /// Whether it is allowed to create sub-graphs.
        /// </summary>
        public virtual bool AllowSubgraphCreation => !IsStateMachineGraph; // State machine graphs do not allow subgraphs.

        /// <summary>
        /// Whether the graph is a state machine graph.
        /// </summary>
        // TODO: Right now, only used to add the correct items in the context menu of the graph view. Could be used for other use cases.
        public virtual bool IsStateMachineGraph => false;

        /// <summary>
        /// Whether the delete and reconnect feature is enabled or not.
        /// </summary>
        /// <remarks>The delete and reconnect feature allows to replace a node that is connected to an upstream
        /// and downstream node by a wire connecting directly the upstream and downstream nodes.</remarks>
        public virtual bool AllowDeleteAndReconnect => false;

        /// <summary>
        /// Whether it is allowed to create <see cref="VariableDeclarationModelBase"/>s that have an <see cref="VariableScope.Exposed"/> scope.
        /// </summary>
        public virtual bool AllowExposedVariableCreation => false;

        /// <summary>
        /// Whether to hide the ports editor when the port is connected. Default is true.
        /// </summary>
        public virtual bool HideConnectedPortsEditor => true;

        /// <summary>
        /// Whether a node dependencies are moved with the node.
        /// </summary>
        public virtual bool MoveNodeDependenciesByDefault => false;

        /// <inheritdoc />
        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
        public virtual IEnumerable<GraphElementModel> GetGraphElementModels() => GetElementsByGuid().Values.Where(t => ReferenceEquals(t.Container, this));
#pragma warning restore UA2001

        /// <summary>
        /// The nodes of the graph.
        /// </summary>
        public virtual IReadOnlyList<AbstractNodeModel> NodeModels => m_GraphNodeModels;

        /// <summary>
        /// The placeholders of the graph.
        /// </summary>
        /// <remarks>Placeholders are models created to substitute for missing models until they are resolved.</remarks>
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
                        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        allModels = allModels.Concat(contextModel.GetGraphElementModels().OfType<AbstractNodeModel>());
#pragma warning restore UA2001
                    }
                }

                return allModels;
            }
        }

        /// <summary>
        /// The node used as the graph entry point, if there is one.
        /// </summary>
        public virtual AbstractNodeModel EntryPoint
        {
            get => m_EntryPoint;
            set
            {
                if (m_EntryPoint == value)
                    return;

                if (m_EntryPoint != null)
                    CurrentGraphChangeDescription.AddChangedModel(m_EntryPoint, ChangeHint.Data);
                m_EntryPoint = value;
                if (m_EntryPoint != null)
                    CurrentGraphChangeDescription.AddChangedModel(m_EntryPoint, ChangeHint.Data);
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
        public virtual IReadOnlyList<VariableDeclarationModelBase> VariableDeclarations => m_GraphVariableModels;

        /// <summary>
        /// Gets the variable declarations in the order they are displayed in the blackboard.
        /// </summary>
        public IReadOnlyList<VariableDeclarationModelBase> GetVariableDeclarationsByDisplayOrder()
        {
            var orderedVariables = new List<VariableDeclarationModelBase>();
            foreach (var sectionModel in SectionModels)
            {
                PopulateVariableList(sectionModel, orderedVariables);
            }

            return orderedVariables;

            void PopulateVariableList(GroupModelBase groupItemModel, List<VariableDeclarationModelBase> variableList)
            {
                // GroupModelBase.Items are already in display order, so just iterate through them
                foreach (var containedModel in groupItemModel.Items)
                {
                    switch (containedModel)
                    {
                        // If it's a variable, add it to the list
                        case VariableDeclarationModelBase vdm:
                            variableList.Add(vdm);
                            break;

                        // If it's a group, recurse into it
                        case GroupModelBase groupItem:
                            PopulateVariableList(groupItem, variableList);
                            break;
                    }
                }
            }
        }

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
        public virtual string Name
        {
            get
            {
                if (GraphObject != null && GraphObject.GraphModel == this)
                    return GraphObject.name;

                return m_Name ?? string.Empty;
            }
            set
            {
                if (GraphObject == null)
                {
                    // Not attached to a GraphObject. In this case, the name is stored in the graph model.
                    m_Name = value;
                }
                else if (GraphObject.GraphModel == this)
                {
                    // Main graph name is the same as the object name
                    GraphObject.name = value;
                }
                else if (ParentGraph != null)
                {
                    // Local subgraph case.
                    m_Name = SubgraphCreationHelper.GenerateLocalGraphUniqueName(ParentGraph.m_LocalSubgraphs, this, value);
                }
                m_Name = value;
            }
        }

        /// <summary>
        /// The current <see cref="GraphChangeDescription"/>. This object contains the current changes applied to the graph up until now. Can be null.
        /// </summary>
        public GraphChangeDescription CurrentGraphChangeDescription => m_GraphChangeDescriptionStack.Count > 0 ? m_GraphChangeDescriptionStack.Peek() : s_NullGraphChangeDescription;

        /// <summary>
        /// A <see cref="GraphChangeDescriptionScope"/>. Use this to gather a <see cref="GraphChangeDescription"/>. <see cref="GraphChangeDescriptionScope"/> can be nested
        /// and each scope provide the <see cref="GraphChangeDescription"/> related to their scope only. When a scope is disposed, their related <see cref="GraphChangeDescription"/>
        /// is merged back into the parent scope, if any.
        /// </summary>
        // This attribute is needed, otherwise when inspecting the GraphModel during debugging it will create a new
        // change scope and m_GraphChangeDescriptionStack will be modified.
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        public GraphChangeDescriptionScope ChangeDescriptionScope => new(this);

        /// <summary>
        /// The last known bounds of the graph in the ui, ie the bounding box containing all the elements of the graph.
        /// </summary>
        /// <remarks>
        /// This value is only updated when the graph is modified in the editor.
        /// </remarks>
        public Rect LastKnownBounds
        {
            get => m_LastKnownBounds;
            set
            {
                if (value != m_LastKnownBounds)
                {
                    m_LastKnownBounds = value;
                    SetGraphObjectDirty();
                }
            }
        }

        internal void SetGraphObject(GraphObject graphObject)
        {
            if (!object.ReferenceEquals(graphObject, GraphObject))
            {
                if (GraphObject != null && GraphObject != graphObject && GraphObject.GraphModel == this)
                {
                    GraphObject.DetachGraphModel();
                }
                GraphObject = graphObject;
            }

            if (m_LocalSubgraphs != null)
            {
                foreach (var localSubgraph in m_LocalSubgraphs)
                {
                    localSubgraph.SetGraphObject(graphObject);
                }
            }
        }

        /// <summary>
        /// Starts a scope to accumulate calls to <see cref="SetGraphObjectDirty"/>. When the last scope is disposed, <see cref="GraphObject.Dirty"/> is called once if needed.
        /// </summary>
        /// <returns>An <see cref="IDisposable"/> creating a scope.</returns>
        /// <remarks>The goal in accumulating <see cref="SetGraphObjectDirty"/> calls is to reduce the number of native calls to make the graph object dirty.</remarks>
        public IDisposable AssetDirtyScope()
        {
            var scope = new DirtyScope(this, false);
            m_DirtyScopes.Push(scope);
            return scope;
        }

        /// <summary>
        /// Starts a scope to block calls to <see cref="SetGraphObjectDirty"/>. While the scope is in effect, calls to <see cref="SetGraphObjectDirty"/> will have no effect.
        /// </summary>
        /// <returns>An <see cref="IDisposable"/> creating a scope.</returns>
        /// <remarks>Calls to <see cref="SetGraphObjectDirty"/> usually need to be blocked during assets I/O operations.</remarks>
        public IDisposable BlockAssetDirtyScope()
        {
            var scope = new DirtyScope(this, true);
            m_DirtyScopes.Push(scope);
            return scope;
        }

        /// <summary>
        /// Sets the <see cref="GraphObject.Dirty"/> flag on the graph object that holds this graph.
        /// </summary>
        /// <remarks>
        /// Calls to <see cref="AssetDirtyScope"/> and <see cref="BlockAssetDirtyScope"/> modify the behavior of this method.
        /// </remarks>
        public void SetGraphObjectDirty()
        {
            if (m_DirtyScopes.Count > 0)
            {
                m_DirtyScopes.Peek().Dirty = true;
            }
            else if (GraphObject != null)
            {
                GraphObject.Dirty = true;
            }
        }

        void EndDirtyScope()
        {
            if (m_DirtyScopes.Count > 0)
            {
                var lastScope = m_DirtyScopes.Pop();
                if (lastScope.Dirty)
                {
                    SetGraphObjectDirty();
                }
            }
        }

        /// <summary>
        /// Gets a version of <see cref="GraphModel.Name"/> usable in C# scripts.
        /// </summary>
        public string GetScriptFriendlyName() => Name?.CodifyString() ?? "";

        /// <summary>
        /// Gets a <see cref="GraphReference"/> for this graph.
        /// </summary>
        /// <param name="allowLocalReference">If true, a local <see cref="GraphReference"/> will be returned, if possible.</param>
        /// <returns>A <see cref="GraphReference"/> for this graph.</returns>
        public GraphReference GetGraphReference(bool allowLocalReference = false)
        {
            return ParentGraph?.GetGraphModelReference(this, allowLocalReference) ?? new GraphReference(this);
        }

        /// <summary>
        /// Creates a local subgraph.
        /// </summary>
        /// <param name="graphModelType">The type of the graph model.</param>
        /// <param name="graphName">The name of the local subgraph.</param>
        /// <param name="graphTemplate">The template of the local subgraph, if any.</param>
        /// <returns>The created local subgraph model.</returns>
        public GraphModel CreateLocalSubgraph(Type graphModelType, string graphName, GraphTemplate graphTemplate = null)
        {
            if (!AllowSubgraphCreation)
                return null;

            var localSubgraph = GraphObject.CreateGraphModel(graphModelType);
            m_LocalSubgraphs ??= new List<GraphModel>();
            m_LocalSubgraphs.Add(localSubgraph);
            localSubgraph.ParentGraph = this;
            localSubgraph.Name = graphName;
            graphTemplate?.InitLocalSubgraphsPreOnEnable(localSubgraph);
            localSubgraph.OnEnable();
            graphTemplate?.InitBasicGraph(localSubgraph);
            return localSubgraph;
        }

        internal virtual GraphModel DuplicateLocalSubGraph(GraphModel sourceGraphModel, string name)
        {
            var newSubgraph = CreateLocalSubgraph(
                sourceGraphModel.GetType(),
                name);

            newSubgraph?.CloneGraph(sourceGraphModel, true);

            return newSubgraph;
        }

        internal GraphModel AddLocalSubgraph(GraphModel localSubgraph)
        {
            if (!AllowSubgraphCreation)
                return null;

            if (m_LocalSubgraphs != null)
            {
                foreach (var subgraph in m_LocalSubgraphs)
                {
                    if (subgraph.Guid == localSubgraph.Guid)
                    {
                        return subgraph;
                    }
                }
            }

            m_LocalSubgraphs ??= new List<GraphModel>();
            m_LocalSubgraphs.Add(localSubgraph);
            localSubgraph.ParentGraph = this;
            localSubgraph.SetGraphObject(GraphObject);
            return localSubgraph;
        }

        /// <summary>
        /// Removes a local subgraph.
        /// </summary>
        /// <param name="localSubgraph">The subgraph to remove.</param>
        public void RemoveLocalSubgraph(GraphModel localSubgraph)
        {
            if (m_LocalSubgraphs != null && m_LocalSubgraphs.Remove(localSubgraph))
            {
                localSubgraph.GraphObject = null;
            }
        }

        /// <summary>
        /// Gets the type of section model.
        /// </summary>
        public virtual Type GetSectionModelType()
        {
            return typeof(SectionModel);
        }

        /// <summary>
        /// Gets the type of graph object to create when a graph model is extracted from a local subgraph to a new asset.
        /// </summary>
        /// <returns>The graph object type to create. Must inherit from <see cref="GraphToolkit.Editor.GraphObject"/>>.</returns>
        public virtual Type GetPreferredSubGraphObjectType() => GraphObject.GetType();

        /// <summary>
        /// Instantiates a new <see cref="SectionModel"/>.
        /// </summary>
        /// <param name="sectionName">The name of the section.</param>
        /// <returns>A new <see cref="SectionModel"/>.</returns>
        protected virtual SectionModel InstantiateSection(string sectionName)
        {
            var section = ModelHelpers.Instantiate<SectionModel>(GetSectionModelType());
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
            CurrentGraphChangeDescription.AddNewModel(section);
        }

        /// <summary>
        /// Removes a <see cref="SectionModel"/> from the graph.
        /// </summary>
        /// <param name="section">The section model to remove.</param>
        protected virtual void RemoveSection(SectionModel section)
        {
            UnregisterElement(section);
            m_SectionModels.Remove(section);
            CurrentGraphChangeDescription.AddDeletedModel(section);
        }

        /// <summary>
        /// Gets a <see cref="SectionModel"/> by its name.
        /// </summary>
        /// <param name="sectionName">The name of the section.</param>
        /// <returns>The section model, or null if not found.</returns>
        public virtual SectionModel GetSectionModel(string sectionName)
        {
            return m_SectionModels.Find(t => t.Title == sectionName);
        }

        /// <summary>
        /// Returns a valid section for a given variable. Default is to return the first section.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>A valid section for a given variable. Default is to return the first section.</returns>
        public virtual string GetVariableSection(VariableDeclarationModelBase variable)
        {
            return DefaultSectionName;
        }

        /// <summary>
        /// Indicates whether the given variable must appear in the blackboard.
        /// </summary>
        /// <param name="v">The variable.</param>
        /// <returns>True if the variable must be visible in the blackboard.</returns>
        public virtual bool IsVariableVisibleInBlackboard(VariableDeclarationModelBase v)
        {
            return true;
        }

        /// <summary>
        /// Indicates whether a given variable can be converted and moved to the section named <paramref name="sectionName"/>.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <param name="sectionName">The target section.</param>
        /// <returns>True if the given variable can be converted.</returns>
        /// <remarks>To be moved to another section, a variable needs to be converted to ensure it complies with the requirements of the new section.</remarks>
        public virtual bool CanConvertVariable(VariableDeclarationModelBase variable, string sectionName)
        {
            return false;
        }

        /// <summary>
        /// Converts a variable to go to the section named sectionName. Either a new variable or the same variable can be returned.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <param name="sectionName">The target section.</param>
        /// <returns>The converted variable.</returns>
        public virtual VariableDeclarationModelBase ConvertVariable(VariableDeclarationModelBase variable, string sectionName)
        {
            return null;
        }

        /// <summary>
        /// Checks that all variables are referenced in a group. Otherwise, adds the variables in their valid section.
        /// Also cleans up no longer existing sections.
        /// </summary>
        protected internal void CheckGroupConsistency()
        {
            void RecurseGetReferencedGroupItem<T>(GroupModelBase root, HashSet<T> result)
                where T : IGroupItemModel
            {
                foreach (var item in root.Items)
                {
                    if (item is T tItem)
                        result.Add(tItem);
                    if (item is GroupModelBase subGroup)
                        RecurseGetReferencedGroupItem(subGroup, result);
                }
            }

            using var assetDirtyScope = AssetDirtyScope();

            var variablesInGroup = new HashSet<VariableDeclarationModelBase>();

            CleanupSections();
            foreach (var group in SectionModels)
                RecurseGetReferencedGroupItem(group, variablesInGroup);

            foreach (var variable in variablesInGroup)
            {
                if (variable is VariableDeclarationPlaceholder && variable.ParentGroup is GroupModel gm)
                    gm.RemoveItem(variable);
            }

            if (VariableDeclarations == null) return;

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var variable in VariableDeclarations.Where(v => v != null))
#pragma warning restore UA2001
            {
                if (!variablesInGroup.Contains(variable))
                    GetSectionModel(GetVariableSection(variable)).InsertItem(variable);
            }
        }

        /// <summary>
        /// The index that maps ports to the wires connected to them.
        /// </summary>
        internal PortWireIndex<WireModel> PortWireIndex => m_PortWireIndex ??= new PortWireIndex<WireModel>(WireModels);

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphModel"/> class.
        /// </summary>
        protected GraphModel()
        {
            m_GraphNodeModels = new List<AbstractNodeModel>();
            m_GraphWireModels = new List<WireModel>();
            m_GraphStickyNoteModels = new List<StickyNoteModel>();
            m_GraphPlacematModels = new List<PlacematModel>();
            m_GraphVariableModels = new List<VariableDeclarationModelBase>();
            m_GraphPortalModels = new List<DeclarationModel>();
            m_SectionModels = new List<SectionModel>();
            m_GraphElementMetaData = new List<GraphElementMetaData>();

            m_ExistingVariableNames = new HashSet<string>();

            m_Placeholders = new List<IPlaceholder>();
            m_PlaceholderData = new SerializedValueDictionary<Hash128, PlaceholderData>();

            m_GraphChangeDescriptionStack = new Stack<GraphChangeDescription>();
            m_DirtyScopes = new Stack<DirtyScope>();
        }

        /// <summary>
        /// Gets the list of wires that are connected to a port.
        /// </summary>
        /// <param name="portModel">The port for which we want the list of connected wires.</param>
        /// <returns>The list of wires connected to the port.</returns>
        public virtual IReadOnlyList<WireModel> GetWiresForPort(PortModel portModel)
        {
            return PortWireIndex.GetWiresForPort(portModel);
        }

        /// <summary>
        /// Changes the order of a wire among its siblings.
        /// </summary>
        /// <param name="wireModel">The wire to move.</param>
        /// <param name="reorderType">The type of move to do.</param>
        internal void ReorderWire(WireModel wireModel, ReorderType reorderType)
        {
            var fromPort = wireModel.FromPort;
            if (fromPort != null && fromPort.HasReorderableWires)
            {
                m_PortWireIndex?.WireReordered(wireModel, reorderType);
                ApplyReorderToGraph(fromPort);

                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var siblingWires = fromPort.GetConnectedWires().ToList();
#pragma warning restore UA2001
                CurrentGraphChangeDescription.AddChangedModels(siblingWires, ChangeHint.GraphTopology);
                CurrentGraphChangeDescription.AddChangedModel(fromPort, ChangeHint.GraphTopology);
            }
        }

        /// <summary>
        /// Updates a wire when one of its port changes.
        /// </summary>
        /// <param name="wireModel">The wire to update.</param>
        /// <param name="oldPort">The previous port value.</param>
        /// <param name="port">The new port value.</param>
        internal void UpdateWire(WireModel wireModel, PortModel oldPort, PortModel port)
        {
            m_PortWireIndex?.WirePortsChanged(wireModel, oldPort, port);

            if (oldPort != null)
            {
                CurrentGraphChangeDescription.AddChangedModel(oldPort, ChangeHint.GraphTopology);
                if (oldPort.PortType == PortType.MissingPort && !oldPort.GetConnectedWires().HasAny())
                    oldPort.NodeModel?.RemoveUnusedMissingPort(oldPort);
            }

            if (port != null)
                CurrentGraphChangeDescription.AddChangedModel(port, ChangeHint.GraphTopology);
            if (wireModel != null)
                CurrentGraphChangeDescription.AddChangedModel(wireModel, ChangeHint.GraphTopology);

            // when moving a wire to a new node, make sure it gets stored matching its new place.
            if (wireModel != null &&
                wireModel.GraphModel == this &&
                oldPort != null && port != null &&
                oldPort.NodeModel != port.NodeModel &&
                ReferenceEquals(port, wireModel.FromPort) &&
                wireModel.FromPort.HasReorderableWires)
            {
                ApplyReorderToGraph(wireModel.FromPort);
            }
        }

        /// <summary>
        /// Reorders the placemats according to a <see cref="ZOrderMove"/>.
        /// </summary>
        /// <param name="models">The placemats to reorder.</param>
        /// <param name="reorderType">The way to reorder placemats.</param>
        public virtual void ReorderPlacemats(IReadOnlyList<PlacematModel> models, ZOrderMove reorderType)
        {
            m_GraphPlacematModels.ReorderElements(models, (ReorderType)reorderType);
            CurrentGraphChangeDescription.AddChangedModels(models, ChangeHint.Layout);
        }

        /// <summary>
        /// Reorders a placemat to be in front of another placemat.
        /// </summary>
        /// <param name="targetPlacemat">The placemat to reorder.</param>
        /// <param name="backgroundPlacemat">The reference placemat.</param>
        public virtual void BringPlacematAfter(PlacematModel targetPlacemat, PlacematModel backgroundPlacemat)
        {
            m_GraphPlacematModels.Remove(targetPlacemat);
            int index = m_GraphPlacematModels.IndexOf(backgroundPlacemat);
            m_GraphPlacematModels.Insert(index + 1, targetPlacemat);

            CurrentGraphChangeDescription.AddChangedModel(targetPlacemat, ChangeHint.Layout);
        }

        /// <summary>
        /// Reorders <see cref="m_GraphWireModels"/> after the <see cref="PortWireIndex"/> has been reordered.
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

            // When duplicating wires, it may happen that the new wire (present in orderedList) is not yet part of WireModels.
            // If so, we can't reorder the wires yet.
            if (indices.Count < orderedList.Count)
                return;

            // place every reordered wire at an index that is part of the collection.
            for (int i = 0; i < orderedList.Count; i++)
            {
                m_GraphWireModels[indices[i]] = orderedList[i];
            }

            SetGraphObjectDirty();
        }

        /// <summary>
        /// Gets all ports in the graph.
        /// </summary>
        /// <returns>All ports in the graph.</returns>
        public IEnumerable<PortModel> GetPortModels()
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return GetElementsByGuid().Values.OfType<PortModel>();
#pragma warning restore UA2001
        }

        /// <summary>
        /// Determines whether two ports can be connected together by a wire.
        /// </summary>
        /// <param name="startPortModel">The port from which the wire would come from.</param>
        /// <param name="compatiblePortModel">The port to which the wire would go to.</param>
        /// <returns>True if the two ports can be connected. False otherwise.</returns>
        public virtual bool IsCompatiblePort(PortModel startPortModel, PortModel compatiblePortModel)
        {
            if (startPortModel.Capacity == PortCapacity.None || compatiblePortModel.Capacity == PortCapacity.None)
                return false;

            var startWirePortalModel = startPortModel.NodeModel as WirePortalModel;

            if (startPortModel.PortType != compatiblePortModel.PortType)
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
                if (wirePortalModel.DeclarationModel.Guid == (startWirePortalModel?.DeclarationModel)?.Guid)
                    return false;
            }

            // This is true for all ports
            if (compatiblePortModel.Direction == startPortModel.Direction ||
                compatiblePortModel.PortType != startPortModel.PortType)
                return false;

            if (startPortModel.Direction == PortDirection.Output)
                return CanAssignTo(compatiblePortModel, startPortModel);
            return CanAssignTo(startPortModel, compatiblePortModel);
        }

        /// <summary>
        /// Gets a list of ports that can be connected to <paramref name="startPortModel"/>.
        /// </summary>
        /// <param name="portModels">The list of candidate ports.</param>
        /// <param name="startPortModel">The port to which the connection originates (can be an input or output port).</param>
        /// <returns>A list of ports that can be connected to <paramref name="startPortModel"/>.</returns>
        public virtual List<PortModel> GetCompatiblePorts(IReadOnlyList<PortModel> portModels, PortModel startPortModel)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return portModels.Where(pModel =>
#pragma warning restore UA2001
            {
                return IsCompatiblePort(startPortModel, pModel);
            })
                .ToList();
        }

        /// <summary>
        /// Gets the entry points of the <see cref="GraphModel"/>.
        /// </summary>
        /// <returns>The entry points of the <see cref="GraphModel"/>.</returns>
        public virtual IEnumerable<AbstractNodeModel> GetEntryPoints()
        {
            return Array.Empty<AbstractNodeModel>();
        }

        /// <summary>
        /// Gets a list of subgraph nodes on the current graph that reference the current graph.
        /// </summary>
        /// <returns>A list of subgraph nodes on the current graph that reference the current graph.</returns>
        public IEnumerable<SubgraphNodeModel> GetSelfReferringSubgraphNodes()
        {
            var selfReferringSubgraphNodeModels = new List<SubgraphNodeModel>();
            foreach (var nodeModel in NodeModels)
            {
                if (nodeModel is SubgraphNodeModel subgraphNodeModel && subgraphNodeModel.GetSubgraphModel() == this)
                {
                    selfReferringSubgraphNodeModels.Add(subgraphNodeModel);
                }
            }

            return selfReferringSubgraphNodeModels;
        }

        /// <summary>
        /// Calls <see cref="SubgraphNodeModel.Update"/> recursively on all subgraph nodes in the graph.
        /// </summary>
        public void UpdateSubGraphs()
        {
            using var dirtyScope = AssetDirtyScope();

            foreach (var recursiveSubgraphNode in GetSelfReferringSubgraphNodes())
                recursiveSubgraphNode.Update();
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
                if (GetElementsByGuid()[model.Guid] != model && model is not IPlaceholder)
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
            UIDependencies.RemoveModel(model);
        }

        /// <summary>
        /// Gets the model for a GUID.
        /// </summary>
        /// <param name="guid">The GUID of the model to get.</param>
        /// <returns>The model found, or null.</returns>
        public GraphElementModel GetModel(Hash128 guid)
        {
            TryGetModelFromGuid(guid, out var model);
            return model;
        }

        /// <summary>
        /// Retrieves a graph element model from its unique identifier (GUID).
        /// </summary>
        /// <param name="guid">The guid of the model to retrieve.</param>
        /// <param name="model">The model matching the identifier, or null if no model were found.</param>
        /// <returns><c>true</c> if the model was found.</returns>
        /// <remarks>
        /// This method is useful when storing GUIDs instead of full model references and retrieving the corresponding model when needed.
        /// It allows efficient lookup of a <see cref="GraphElementModel"/> without maintaining direct references.
        /// </remarks>
        public bool TryGetModelFromGuid(Hash128 guid, out GraphElementModel model)
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
            if (!AllowPortalCreation && nodeModel is WirePortalModel)
            {
                throw new ArgumentException("Portal creation is disabled.", nameof(nodeModel));
            }

            if (!AllowSubgraphCreation && nodeModel is SubgraphNodeModel)
            {
                throw new ArgumentException("Subgraph creation is disabled.", nameof(nodeModel));
            }

            if (nodeModel.NeedsContainer())
                throw new ArgumentException("Can't add a node model that does not need a container to the graph");

            RegisterElement(nodeModel);
            AddMetaData(nodeModel, m_GraphNodeModels.Count);
            m_GraphNodeModels.Add(nodeModel);

            EntryPoint ??= nodeModel;

            CurrentGraphChangeDescription.AddNewModel(nodeModel);
        }

        void AddMetaData(Model model, int index = -1)
        {
            m_GraphElementMetaData.Add(new GraphElementMetaData(model, index));
        }

        /// <summary>
        /// Replaces the node model at the specified index in <see cref="NodeModels"/> list with another one.
        /// </summary>
        /// <param name="index">Index of the node model in the NodeModels list.</param>
        /// <param name="nodeModel">The new node model.</param>
        protected virtual void ReplaceNode(int index, AbstractNodeModel nodeModel)
        {
            if (!AllowPortalCreation && nodeModel is WirePortalModel)
            {
                throw new ArgumentException("Portal creation is disabled.", nameof(nodeModel));
            }

            if (!AllowSubgraphCreation && nodeModel is SubgraphNodeModel)
            {
                throw new ArgumentException("Subgraph creation is disabled.", nameof(nodeModel));
            }

            if (index < 0 || index >= m_GraphNodeModels.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var oldModel = m_GraphNodeModels[index];

            UnregisterElement(oldModel);
            RegisterElement(nodeModel);
            var indexInMetadata = m_GraphElementMetaData.FindIndex(m => m.Index == index);

            m_GraphElementMetaData[indexInMetadata] = new GraphElementMetaData(nodeModel, index);
            m_GraphNodeModels[index] = nodeModel;

            if (m_EntryPoint == oldModel)
            {
                m_EntryPoint = nodeModel;
            }

            CurrentGraphChangeDescription.AddNewModel(nodeModel)?.AddDeletedModel(oldModel);
        }

        /// <summary>
        /// Removes a node model from the graph.
        /// </summary>
        /// <param name="nodeModel">The <see cref="AbstractNodeModel"/> to remove.</param>
        protected virtual void RemoveNode(AbstractNodeModel nodeModel)
        {
            if (nodeModel == null)
                return;

            UnregisterElement(nodeModel);

            var indexToRemove = -1;
            for (var i = 0; i < m_GraphNodeModels.Count; i++)
            {
                if (m_GraphNodeModels[i] == null)
                    continue;
                if (nodeModel.Guid == m_GraphNodeModels[i].Guid)
                {
                    indexToRemove = i;
                    break;
                }
            }

            if (indexToRemove != -1)
            {
                RemoveFromMetadata(indexToRemove, PlaceholderModelHelper.ModelToMissingTypeCategory(nodeModel));
                m_GraphNodeModels.RemoveAt(indexToRemove);
                InsertNullReferencesWhileHasMissingTypes(ManagedMissingTypeModelCategory.Node, indexToRemove);

                if (m_EntryPoint == nodeModel)
                {
                    EntryPoint = NodeModels.Count > 0 ? NodeModels[0] : null;
                }

                CurrentGraphChangeDescription.AddDeletedModel(nodeModel);
            }
        }

        void RemoveFromMetadata(int index, ManagedMissingTypeModelCategory category)
        {
            // While missing types aren't resolved, we don't remove anything.
            if (SerializationUtility.HasManagedReferencesWithMissingTypes(GraphObject))
                return;

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

        /// <summary>
        /// Registers a block node to the graph.
        /// </summary>
        /// <param name="blockNodeModel">The block node.</param>
        public void RegisterBlockNode(BlockNodeModel blockNodeModel)
        {
            RegisterElement(blockNodeModel);
        }

        /// <summary>
        /// Unregisters a block node from the graph.
        /// </summary>
        /// <param name="blockNodeModel">The block node.</param>
        public void UnregisterBlockNode(BlockNodeModel blockNodeModel)
        {
            UnregisterElement(blockNodeModel);
        }

        /// <summary>
        /// Registers a node preview to the graph.
        /// </summary>
        /// <param name="nodePreviewModel">The node preview.</param>
        public void RegisterNodePreview(NodePreviewModel nodePreviewModel)
        {
            RegisterElement(nodePreviewModel);
        }

        /// <summary>
        /// Unregisters a node preview from the graph.
        /// </summary>
        /// <param name="nodePreviewModel">The node preview.</param>
        public void UnregisterNodePreview(NodePreviewModel nodePreviewModel)
        {
            UnregisterElement(nodePreviewModel);
        }

        /// <summary>
        /// Registers a port to the graph.
        /// </summary>
        /// <param name="portModel">The port.</param>
        public void RegisterPort(PortModel portModel)
        {
            if (!portModel?.NodeModel?.SpawnFlags.IsOrphan() ?? false)
                RegisterElement(portModel);
        }

        /// <summary>
        /// Unregisters a port from the graph.
        /// </summary>
        /// <param name="portModel">The port.</param>
        public void UnregisterPort(PortModel portModel)
        {
            if (!portModel?.NodeModel?.SpawnFlags.IsOrphan() ?? false)
                UnregisterElement(portModel);
        }

        /// <summary>
        /// Deletes graph element models in the graph.
        /// </summary>
        /// <param name="graphElementModels">The elements to delete.</param>
        public void DeleteElements(IReadOnlyCollection<GraphElementModel> graphElementModels)
        {
            using var assetDirtyScope = AssetDirtyScope();

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var initialVariables = new HashSet<VariableDeclarationModelBase>(VariableDeclarations.Where(v => v != null && v.IsInputOrOutput));
#pragma warning restore UA2001

            var elementsByType = new ElementsByType(graphElementModels);

            // Add nodes that would be backed by declaration models.
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            elementsByType.NodeModels.UnionWith(elementsByType.VariableDeclarationsModels.SelectMany(FindReferencesInGraph<AbstractNodeModel>));
#pragma warning restore UA2001

            // Add wires connected to the deleted nodes.
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var allWires = WireModels.Union(Placeholders.OfType<WireModel>()).ToList();
#pragma warning restore UA2001
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var portModel in elementsByType.NodeModels.OfType<PortNodeModel>().SelectMany(n => n.GetPorts()))
#pragma warning restore UA2001
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                elementsByType.WireModels.UnionWith(allWires.Where(e => e != null && (e.ToPort == portModel || e.FromPort == portModel)));
#pragma warning restore UA2001

            var statePortModels = new HashSet<StatePortModel>();
            foreach (var wireModel in elementsByType.WireModels)
            {
                if (wireModel is TransitionSupportModel transitionSupportModel && transitionSupportModel.TransitionSupportKind != TransitionSupportKind.StateToState && transitionSupportModel.ToPort is StatePortModel toPortModel)
                {
                    statePortModels.Add(toPortModel);
                }
            }

            DeleteVariableDeclarations(elementsByType.VariableDeclarationsModels, deleteUsages: false);
            DeleteGroups(elementsByType.GroupModels);
            DeleteStickyNotes(elementsByType.StickyNoteModels);
            DeletePlacemats(elementsByType.PlacematModels);
            DeleteWires(elementsByType.WireModels);
            DeleteNodes(elementsByType.NodeModels, deleteConnections: false);

            if (elementsByType.VariableDeclarationsModels.Count > 0)
            {
                // Find out if there were any deleted I/O variable declaration.
                foreach (var variableDeclaration in VariableDeclarations)
                {
                    if (variableDeclaration != null && variableDeclaration.IsInputOrOutput)
                    {
                        initialVariables.Remove(variableDeclaration);
                    }
                }

                if (initialVariables.Count > 0)
                {
                    foreach (var recursiveSubgraphNode in GetSelfReferringSubgraphNodes())
                        recursiveSubgraphNode.Update();
                }
            }

            foreach (var statePortModel in statePortModels)
            {
                statePortModel.UpdateAllOffsets();
            }
        }

        void IGraphElementContainer.RemoveContainerElements(IReadOnlyCollection<GraphElementModel> elementModels)
        {
            RemoveElements(elementModels);
        }

        /// <summary>
        /// Removes elements from the lists of graph element models of the graph.
        /// </summary>
        /// <param name="elementModels">The elements to remove.</param>
        /// <remarks>To delete elements from the graph, call <see cref="DeleteElements"/> instead.</remarks>
        protected virtual void RemoveElements(IReadOnlyCollection<GraphElementModel> elementModels)
        {
            using var assetDirtyScope = AssetDirtyScope();

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
                    case VariableDeclarationModelBase variableDeclarationModel:
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
            if (!AllowPortalCreation)
            {
                throw new InvalidOperationException("Portal creation is disabled.");
            }

            RegisterElement(declarationModel);
            AddMetaData(declarationModel, m_GraphPortalModels.Count);
            m_GraphPortalModels.Add(declarationModel);
            CurrentGraphChangeDescription.AddNewModel(declarationModel);
        }

        /// <summary>
        /// Duplicates a portal declaration model and adds it to the graph.
        /// </summary>
        /// <param name="declarationModel">The portal declaration to duplicate.</param>
        /// <returns>The new portal declaration model.</returns>
        public virtual DeclarationModel DuplicatePortal(DeclarationModel declarationModel)
        {
            if (!AllowPortalCreation)
            {
                throw new InvalidOperationException("Portal creation is disabled.");
            }

            var newDeclarationModel = declarationModel.Clone();

            RegisterElement(newDeclarationModel);
            m_GraphPortalModels.Add(newDeclarationModel);
            newDeclarationModel.GraphModel = this;
            CurrentGraphChangeDescription.AddNewModel(newDeclarationModel);
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

            var indexToRemove = -1;
            for (var i = 0; i < m_GraphPortalModels.Count; i++)
            {
                if (m_GraphPortalModels[i] == null)
                    continue;
                if (declarationModel.Guid == m_GraphPortalModels[i].Guid)
                {
                    indexToRemove = i;
                    break;
                }
            }

            if (indexToRemove != -1)
            {
                m_GraphPortalModels.RemoveAt(indexToRemove);
                RemoveFromMetadata(indexToRemove, ManagedMissingTypeModelCategory.PortalDeclaration);
                InsertNullReferencesWhileHasMissingTypes(ManagedMissingTypeModelCategory.PortalDeclaration, indexToRemove);
                CurrentGraphChangeDescription.AddDeletedModel(declarationModel);
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
            CurrentGraphChangeDescription.AddNewModel(wireModel);
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

            var indexToRemove = -1;
            for (var i = 0; i < m_GraphWireModels.Count; i++)
            {
                if (m_GraphWireModels[i] == null)
                    continue;
                if (wireModel.Guid == m_GraphWireModels[i].Guid)
                {
                    indexToRemove = i;
                    break;
                }
            }

            if (indexToRemove != -1)
            {
                m_GraphWireModels.RemoveAt(indexToRemove);
                RemoveFromMetadata(indexToRemove, ManagedMissingTypeModelCategory.Wire);
                InsertNullReferencesWhileHasMissingTypes(ManagedMissingTypeModelCategory.Wire, indexToRemove);
                CurrentGraphChangeDescription.AddDeletedModel(wireModel);
            }

            m_PortWireIndex?.WireRemoved(wireModel);

            // Remove missing port with no connections.
            if (wireModel.ToPort?.PortType == PortType.MissingPort && (!wireModel.ToPort?.GetConnectedWires().HasAny() ?? false))
                wireModel.ToPort?.NodeModel?.RemoveUnusedMissingPort(wireModel.ToPort);

            if (wireModel.FromPort?.PortType == PortType.MissingPort && (!wireModel.FromPort?.GetConnectedWires().HasAny() ?? false))
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
            CurrentGraphChangeDescription.AddNewModel(stickyNoteModel);
        }

        /// <summary>
        /// Removes a sticky note from the graph.
        /// </summary>
        /// <param name="stickyNoteModel">The sticky note to remove.</param>
        protected virtual void RemoveStickyNote(StickyNoteModel stickyNoteModel)
        {
            UnregisterElement(stickyNoteModel);
            m_GraphStickyNoteModels.Remove(stickyNoteModel);
            CurrentGraphChangeDescription.AddDeletedModel(stickyNoteModel);
        }

        /// <summary>
        /// Adds a placemat to the graph.
        /// </summary>
        /// <param name="placematModel">The placemat to add.</param>
        protected virtual void AddPlacemat(PlacematModel placematModel)
        {
            RegisterElement(placematModel);
            m_GraphPlacematModels.Add(placematModel);
            CurrentGraphChangeDescription.AddNewModel(placematModel);
        }

        /// <summary>
        /// Removes a placemat from the graph.
        /// </summary>
        /// <param name="placematModel">The placemat to remove.</param>
        protected virtual void RemovePlacemat(PlacematModel placematModel)
        {
            UnregisterElement(placematModel);
            m_GraphPlacematModels.Remove(placematModel);
            CurrentGraphChangeDescription.AddDeletedModel(placematModel);
        }

        /// <summary>
        /// Adds a variable declaration to the graph.
        /// </summary>
        /// <param name="variableDeclarationModel">The variable declaration to add.</param>
        protected virtual void AddVariableDeclaration(VariableDeclarationModelBase variableDeclarationModel)
        {
            RegisterElement(variableDeclarationModel);
            AddMetaData(variableDeclarationModel, m_GraphVariableModels.Count);
            m_GraphVariableModels.Add(variableDeclarationModel);
            m_ExistingVariableNames.Add(variableDeclarationModel.Title);
            CurrentGraphChangeDescription.AddNewModel(variableDeclarationModel);
        }

        /// <summary>
        /// Removes a variable declaration from the graph.
        /// </summary>
        /// <param name="variableDeclarationModel">The variable declaration to remove.</param>
        /// <returns>The group from which the variable declaration was removed.</returns>
        protected virtual GroupModelBase RemoveVariableDeclaration(VariableDeclarationModelBase variableDeclarationModel)
        {
            if (variableDeclarationModel == null)
                return null;

            UnregisterElement(variableDeclarationModel);

            var indexToRemove = -1;
            for (var i = 0; i < m_GraphVariableModels.Count; i++)
            {
                if (m_GraphVariableModels[i] == null)
                    continue;
                if (variableDeclarationModel.Guid == m_GraphVariableModels[i].Guid)
                {
                    indexToRemove = i;
                    break;
                }
            }

            if (indexToRemove != -1)
            {
                RemoveFromMetadata(indexToRemove, ManagedMissingTypeModelCategory.VariableDeclaration);
                m_GraphVariableModels.RemoveAt(indexToRemove);
                InsertNullReferencesWhileHasMissingTypes(ManagedMissingTypeModelCategory.VariableDeclaration, indexToRemove);
                CurrentGraphChangeDescription.AddDeletedModel(variableDeclarationModel);
            }

            m_ExistingVariableNames.Remove(variableDeclarationModel.Title);

            var parent = variableDeclarationModel.ParentGroup;
            (parent as GroupModel)?.RemoveItem(variableDeclarationModel);
            return parent;
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
        /// Creates a new node in a graph.
        /// </summary>
        /// <param name="nodeName">The name of the node to create.</param>
        /// <param name="position">The position of the node to create.</param>
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the node is created.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <typeparam name="TNodeType">The type of the new node to create.</typeparam>
        /// <returns>The newly created node.</returns>
        public TNodeType CreateNode<TNodeType>(string nodeName = "", Vector2 position = default,
            Hash128 guid = default, Action<TNodeType> initializationCallback = null, SpawnFlags spawnFlags = SpawnFlags.Default)
            where TNodeType : AbstractNodeModel
        {
            Action<AbstractNodeModel> setupWrapper = null;
            if (initializationCallback != null)
            {
                setupWrapper = n => initializationCallback.Invoke(n as TNodeType);
            }

            return (TNodeType)CreateNode(typeof(TNodeType), nodeName, position, guid, setupWrapper, spawnFlags);
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
            using var dirtyScope = AssetDirtyScope();

            if (!AllowPortalCreation && typeof(WirePortalModel).IsAssignableFrom(nodeTypeToCreate))
            {
                throw new ArgumentException("Portal creation is disabled.", nameof(nodeTypeToCreate));
            }

            if (!AllowSubgraphCreation && typeof(SubgraphNodeModel).IsAssignableFrom(nodeTypeToCreate))
            {
                throw new ArgumentException("Subgraph creation is disabled.", nameof(nodeTypeToCreate));
            }

            var nodeModel = InstantiateNode(nodeTypeToCreate, nodeName, position, guid, initializationCallback, spawnFlags);

            if (!spawnFlags.IsOrphan() && ReferenceEquals(nodeModel.Container, this))
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

            if (!AllowPortalCreation && typeof(WirePortalModel).IsAssignableFrom(nodeTypeToCreate))
            {
                throw new ArgumentException("Portal creation is disabled.", nameof(nodeTypeToCreate));
            }

            if (!AllowSubgraphCreation && typeof(SubgraphNodeModel).IsAssignableFrom(nodeTypeToCreate))
            {
                throw new ArgumentException("Subgraph creation is disabled.", nameof(nodeTypeToCreate));
            }

            AbstractNodeModel nodeModel;
            if (typeof(Constant).IsAssignableFrom(nodeTypeToCreate))
            {
                var constant = (Constant)Activator.CreateInstance(nodeTypeToCreate);
                var constantNodeModel = (ConstantNodeModel)Activator.CreateInstance(ConstantNodeType);
                constantNodeModel.Value = constant;
                nodeModel = constantNodeModel;
            }
            else if (typeof(AbstractNodeModel).IsAssignableFrom(nodeTypeToCreate))
                nodeModel = (AbstractNodeModel)Activator.CreateInstance(nodeTypeToCreate);
            else
                throw new ArgumentOutOfRangeException(nameof(nodeTypeToCreate));

            nodeModel.GraphModel = this;
            if (nodeModel is IHasTitle titled)
                titled.Title = nodeName ?? nodeTypeToCreate.Name;

            nodeModel.SpawnFlags = spawnFlags;
            nodeModel.Position = position;
            if (guid.isValid)
                nodeModel.SetGuid(guid);
            initializationCallback?.Invoke(nodeModel);
            nodeModel.OnCreateNode();

            return nodeModel;
        }

        /// <summary>
        /// Indicates whether a variable is allowed in the graph or not.
        /// </summary>
        /// <param name="variable">The variable in the graph.</param>
        /// <param name="graphModel">The graph of the variable.</param>
        /// <returns><c>true</c> if the variable is allowed, <c>false</c> otherwise.</returns>
        public virtual bool CanCreateVariableNode(VariableDeclarationModelBase variable, GraphModel graphModel)
        {
            var allowMultipleDataOutputInstances = AllowMultipleDataOutputInstances != AllowMultipleDataOutputInstances.Disallow;
            return allowMultipleDataOutputInstances
                || variable.DataType == TypeHandle.ExecutionFlow
                || variable.Modifiers != ModifierFlags.Write
                || graphModel.FindReferencesInGraph<VariableNodeModel>(variable).Count == 0;
        }

        /// <summary>
        /// Creates a new variable node in the graph.
        /// </summary>
        /// <param name="declarationModel">The declaration for the variable.</param>
        /// <param name="position">The position of the node to create.</param>
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <returns>The newly created variable node.</returns>
        public virtual VariableNodeModel CreateVariableNode(VariableDeclarationModelBase declarationModel,
            Vector2 position, Hash128 guid = default, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var nodeType = VariableNodeType;
            Debug.Assert(typeof(VariableNodeModel).IsAssignableFrom(nodeType));

            var initializationCallback = new Action<AbstractNodeModel>(n =>
            {
                var variableNodeModel = n as VariableNodeModel;
                Debug.Assert(variableNodeModel != null);
                variableNodeModel.SetDeclarationModel(declarationModel);
            });

            return CreateNode(nodeType, declarationModel.Title, position, guid, initializationCallback, spawnFlags) as VariableNodeModel;
        }

        /// <summary>
        /// Gets the type handle for subgraphs.
        /// </summary>
        /// <returns>The type handle associated with subgraphs.</returns>
        public virtual TypeHandle GetSubgraphTypeHandle()
        {
            return TypeHandle.Subgraph;
        }

        /// <summary>
        /// Creates a new subgraph node in the graph.
        /// </summary>
        /// <param name="subgraphModel">The graph model to use as the subgraph.</param>
        /// <param name="position">The position of the node to create.</param>
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <returns>The newly created subgraph node.</returns>
        public virtual SubgraphNodeModel CreateSubgraphNode(GraphModel subgraphModel, Vector2 position, Hash128 guid = default, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            if (!AllowSubgraphCreation)
            {
                throw new InvalidOperationException("Subgraph creation is disabled.");
            }

            if (subgraphModel.IsContainerGraph())
            {
                Debug.LogWarning("Failed to create the subgraph node. Container graphs cannot be referenced by a subgraph node.");
                return null;
            }

            if (subgraphModel.IsLocalSubgraph && GetGraphModelByGuid(subgraphModel.Guid) != subgraphModel)
            {
                Debug.LogError("Failed to create the subgraph node. Local subgraph not found.");
                return null;
            }

            var nodeType = SubgraphNodeType;
            Debug.Assert(typeof(SubgraphNodeModel).IsAssignableFrom(nodeType));

            var initializationCallback = new Action<AbstractNodeModel>(n =>
            {
                var subgraphNodeModel = n as SubgraphNodeModel;
                Debug.Assert(subgraphNodeModel != null);
                subgraphNodeModel.SetSubgraphModel(subgraphModel.GetGraphReference(true));
            });

            return CreateNode(nodeType, subgraphModel.Name, position, guid, initializationCallback, spawnFlags) as SubgraphNodeModel;
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
            var constantType = GetConstantType(constantTypeHandle);
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
            if (!AllowPortalCreation && sourceNode is WirePortalModel)
            {
                throw new ArgumentException("Portal creation is disabled.", nameof(sourceNode));
            }

            if (!AllowSubgraphCreation && sourceNode is SubgraphNodeModel)
            {
                throw new ArgumentException("Subgraph creation is disabled.", nameof(sourceNode));
            }

            using var assetDirtyScope = AssetDirtyScope();

            var pastedNodeModel = sourceNode.Clone();

            // Set graph model BEFORE define node as it is commonly use during Define
            pastedNodeModel.GraphModel = this;
            pastedNodeModel.OnDuplicateNode(sourceNode);

            if (pastedNodeModel is BlockNodeModel pastedBlockNodeModel)
            {
                RegisterBlockNode(pastedBlockNodeModel);
            }
            else
            {
                AddNode(pastedNodeModel);
                pastedNodeModel.Position += delta;
            }

            return pastedNodeModel;
        }

        /// <summary>
        /// Indicates if a node can be pasted or duplicated in the graph.
        /// </summary>
        /// <param name="originalModel">The node model to copy.</param>
        /// <returns>True if the node can be pasted or duplicated.</returns>
        /// <remarks>Implementations must handle nodes that comes from other graph tools.</remarks>
        public abstract bool CanPasteNode(AbstractNodeModel originalModel);

        /// <summary>
        /// Duplicates a wire and adds it to the graph.
        /// </summary>
        /// <param name="sourceWire">The wire to duplicate.</param>
        /// <param name="targetInputNode">The new input node for the wire.</param>
        /// <param name="targetOutputNode">The new output node for the wire.</param>
        /// <param name="reuseExistingWire">If a wire already exists between the same nodes and ports as <paramref name="sourceWire"/>, returns the existing wire instead of creating a new one.</param>
        /// <returns>The newly created wire or an existing wire if <paramref name="reuseExistingWire"/> is true and a matching wire is found.</returns>
        /// <remarks>
        /// During a duplication operation, a wire is usually duplicated along with its input node, output node, or both.
        /// When both nodes are duplicated, the method connects the new wire between them.
        /// When only the input node is duplicated, the method connects the new wire to the original output node, and <paramref name="targetOutputNode"/> must be null.
        /// When only the output node is duplicated, the method connects the new wire to the original input node, and <paramref name="targetInputNode"/> must be null.
        /// The method duplicates the wire only if the <see cref="PortCapacity"/> constraints allow it. If duplicating the wire exceeds the allowed number of connections for a port, the method does not perform the duplication.
        /// </remarks>
        public virtual WireModel DuplicateWire(WireModel sourceWire, AbstractNodeModel targetInputNode, AbstractNodeModel targetOutputNode, bool reuseExistingWire = true)
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
                inputPortModel = targetInputNode switch
                {
                    InputOutputPortsNodeModel inputOutputPortsNodeModel => inputOutputPortsNodeModel.InputsById[sourceWire.ToPortId],
                    StateModel stateModel => stateModel.GetInPort(),
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    _ => (targetInputNode as PortNodeModel)?.GetPorts().FirstOrDefault(p => p.UniqueName == sourceWire.ToPortId)
#pragma warning restore UA2001
                };

                outputPortModel = targetOutputNode switch
                {
                    InputOutputPortsNodeModel outputPortsNodeModel => outputPortsNodeModel.OutputsById[sourceWire.FromPortId],
                    StateModel stateModel => stateModel.GetOutPort(),
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    _ => (targetOutputNode as PortNodeModel)?.GetPorts().FirstOrDefault(p => p.UniqueName == sourceWire.FromPortId)
#pragma warning restore UA2001
                };
            }

            if (inputPortModel != null && outputPortModel != null)
            {
                if (inputPortModel.Capacity == PortCapacity.Single && inputPortModel.GetConnectedWires().HasAny())
                    return null;
                if (outputPortModel.Capacity == PortCapacity.Single && outputPortModel.GetConnectedWires().HasAny())
                    return null;

                if (reuseExistingWire)
                {
                    var existing = GetAnyWireConnectedToPorts(inputPortModel, outputPortModel);
                    if (existing != null)
                        return existing;
                }

                var newWire = sourceWire.Clone();

                newWire.GraphModel = this;
                newWire.SetPorts(inputPortModel, outputPortModel);
                AddWire(newWire);
                ApplyReorderToGraph(outputPortModel);

                return newWire;
            }

            return null;
        }

        WireModel GetAnyWireConnectedToPorts(PortModel toPort, PortModel fromPort)
        {
            var wires = GetWiresForPort(toPort);
            foreach (var wire in wires)
            {
                if (wire.ToPort == toPort && wire.FromPort == fromPort)
                    return wire;
            }

            return null;
        }

        /// <summary>
        /// Duplicates variable or constant nodes connected to multiple ports, ensuring there is one node per wire.
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
        /// Deletes a node from the graph.
        /// </summary>
        /// <param name="nodeToDelete">The node to delete.</param>
        /// <param name="deleteConnections">Whether to delete the wires connected to the nodes.</param>
        /// <param name="deleteUnrefPortalDeclarations">Whether to delete unreferenced portal declarations.</param>
        public void DeleteNode(AbstractNodeModel nodeToDelete, bool deleteConnections, bool deleteUnrefPortalDeclarations = true)
        {
            DeleteNodes(new[] { nodeToDelete }, deleteConnections, deleteUnrefPortalDeclarations);
        }

        /// <summary>
        /// Deletes a collection of nodes from the graph.
        /// </summary>
        /// <param name="nodeModels">The nodes to delete.</param>
        /// <param name="deleteConnections">Whether to delete the wires connected to the nodes.</param>
        ///         /// <param name="deleteUnrefPortalDeclarations">Whether to delete unreferenced portal declarations.</param>

        public virtual void DeleteNodes(IReadOnlyCollection<AbstractNodeModel> nodeModels, bool deleteConnections, bool deleteUnrefPortalDeclarations = true)
        {
            using var dirtyScope = AssetDirtyScope();
            var portalRefs = new List<WirePortalModel>();
            var deletedElementsByContainer = new Dictionary<IGraphElementContainer, List<GraphElementModel>>();

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var nodeModel in nodeModels.Where(n => n.IsDeletable()))
#pragma warning restore UA2001
            {
                if (!deletedElementsByContainer.TryGetValue(nodeModel.Container, out var deletedElements))
                {
                    deletedElements = new List<GraphElementModel>();
                    deletedElementsByContainer.Add(nodeModel.Container, deletedElements);
                }

                deletedElements.Add(nodeModel);

                if (deleteConnections)
                {
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    var connectedWires = nodeModel.GetConnectedWires().ToList();
#pragma warning restore UA2001
                    DeleteWires(connectedWires);
                }

                // If all the portals with the given declaration are deleted, delete the declaration.
                if (deleteUnrefPortalDeclarations && nodeModel is WirePortalModel { DeclarationModel: not null } wirePortalModel)
                {
                    portalRefs.Clear();
                    FindReferencesInGraph(wirePortalModel.DeclarationModel, portalRefs);
                    portalRefs.RemoveAll(nodeModels.Contains);

                    if (portalRefs.Count == 0)
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
                }

                if (nodeModel is SubgraphNodeModel { IsReferencingLocalSubgraph: true } subgraphNodeModel)
                {
                    RemoveLocalSubgraph(subgraphNodeModel.GetSubgraphModel());
                }

                nodeModel.OnDeleteNode();
            }

            foreach (var (container, deletedElements) in deletedElementsByContainer)
            {
                if (container is GraphModel graphModel && graphModel.Guid == Guid)
                    RemoveElements(deletedElements);
                else
                    container.RemoveContainerElements(deletedElements);
            }
        }

        /// <summary>
        /// Returns the type of wire to instantiate between two ports.
        /// </summary>
        /// <param name="toPort">The destination port.</param>
        /// <param name="fromPort">The origin port.</param>
        /// <returns>The wire model type.</returns>
        public virtual Type GetWireType(PortModel toPort, PortModel fromPort)
        {
            return typeof(WireModel);
        }

        /// <summary>
        /// Gets the type of transition to instantiate between two states in a state machine.
        /// </summary>
        /// <param name="toPort">The destination port.</param>
        /// <param name="fromPort">The origin port.</param>
        /// <param name="transitionSupportKind">The kind of <see cref="TransitionSupportModel"/>.</param>
        /// <returns>The transition support model type.</returns>
        /// <remarks>Override this method to return a custom transition type. The custom transition type must derive from <see cref="TransitionSupportModel"/>.</remarks>
        public virtual Type GetTransitionType(PortModel toPort, PortModel fromPort, TransitionSupportKind transitionSupportKind)
        {
            return typeof(TransitionSupportModel);
        }

        /// <summary>
        /// Creates a transition support on a state (for initial, global and self transitions) and adds it to the graph.
        /// </summary>
        /// <param name="stateModel">The state on which to add the transition.</param>
        /// <param name="transitionSupportKind">The kind of transition to create.</param>
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <returns>The newly created wire</returns>
        public TransitionSupportModel CreateSingleStateTransitionSupport(StateModel stateModel,
            TransitionSupportKind transitionSupportKind,
            Hash128 guid = default)
        {
            var anchorPos = stateModel.GetInPort().ComputeOffsetForNewSingleStateTransition();
            return CreateTransitionSupport(stateModel.GetInPort(), AnchorSide.Top, anchorPos,
                stateModel.GetOutPort(), AnchorSide.None, 0, transitionSupportKind, guid);
        }

        /// <summary>
        /// Creates a wire and adds it to the graph.
        /// </summary>
        /// <param name="toPort">The port that the wire connects to.</param>
        /// <param name="fromPort">The port from which the wire originates.</param>
        /// <param name="guid">The unique identifier (GUID) to assign to the newly created item.</param>
        /// <returns>The newly created wire.</returns>
        /// <remarks>
        /// This method creates a wire that connects two nodes, originating from an output port and going to an input port. A unique identifier (GUID) is assigned to the newly
        /// created wire, so the <see cref="GraphModel"/> can track and retrieve it easily using methods such as <see cref="TryGetModelFromGuid"/> or <see cref="GetModel"/>.
        /// </remarks>
        public WireModel CreateWire(PortModel toPort, PortModel fromPort, Hash128 guid = default)
        {
            return CreateWire(GetWireType(toPort, fromPort), toPort, fromPort, true, guid);
        }

        /// <summary>
        /// Creates a wire and adds it to the graph.
        /// </summary>
        /// <param name="wireType">The type of wire to create. Must be or derive from <see cref="WireModel"/>.</param>
        /// <param name="toPort">The port from which the wire originates.</param>
        /// <param name="fromPort">The port to which the wire goes.</param>
        /// <param name="reuseExisting">If true, return an already existing wire between the two ports, if any, instead of creating a new wire.</param>
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <returns>The newly created wire</returns>
        public virtual WireModel CreateWire(Type wireType, PortModel toPort, PortModel fromPort, bool reuseExisting = true, Hash128 guid = default)
        {
            if (reuseExisting)
            {
                var existing = GetAnyWireConnectedToPorts(toPort, fromPort);
                if (existing != null)
                    return existing;
            }

            var wireModel = InstantiateWire(wireType, toPort, fromPort, guid);
            AddWire(wireModel);

            return wireModel;
        }

        /// <summary>
        /// Instantiates a wire.
        /// </summary>
        /// <param name="wireType">The type of wire to create. Must be or derive from <see cref="WireModel"/>.</param>
        /// <param name="toPort">The port from which the wire originates.</param>
        /// <param name="fromPort">The port to which the wire goes.</param>
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <returns>The newly created wire</returns>
        protected virtual WireModel InstantiateWire(Type wireType, PortModel toPort, PortModel fromPort, Hash128 guid = default)
        {
            var wireModel = ModelHelpers.Instantiate<WireModel>(wireType);
            wireModel.GraphModel = this;
            if (guid.isValid)
                wireModel.SetGuid(guid);
            wireModel.SetPorts(toPort, fromPort);
            return wireModel;
        }

        /// <summary>
        /// Deletes a wire from the graph.
        /// </summary>
        /// <param name="wireToDelete">The list of wires to delete.</param>
        public virtual void DeleteWire(WireModel wireToDelete)
        {
            using var dirtyScope = AssetDirtyScope();

            if (wireToDelete != null && wireToDelete.IsDeletable())
            {
                if (wireToDelete is WirePlaceholder placeholder)
                {
                    RemovePlaceholder(placeholder);
                }
                else
                {
                    wireToDelete.ToPort?.NodeModel?.OnDisconnection(wireToDelete.ToPort, wireToDelete.FromPort);
                    wireToDelete.FromPort?.NodeModel?.OnDisconnection(wireToDelete.FromPort, wireToDelete.ToPort);

                    CurrentGraphChangeDescription.AddChangedModel(wireToDelete.ToPort, ChangeHint.GraphTopology);
                    CurrentGraphChangeDescription.AddChangedModel(wireToDelete.FromPort, ChangeHint.GraphTopology);

                    RemoveWire(wireToDelete);
                }
            }
        }

        /// <summary>
        /// Deletes wires from the graph.
        /// </summary>
        /// <param name="wireModels">The list of wires to delete.</param>
        public void DeleteWires(IReadOnlyCollection<WireModel> wireModels)
        {
            using var dirtyScope = AssetDirtyScope();

            // Call ToList on the collection to prevent iteration over the PortWireIndex.m_WiresByPort
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var wireModel in wireModels.ToList())
#pragma warning restore UA2001
            {
                DeleteWire(wireModel);
            }
        }

        /// <summary>
        /// Returns the type of sticky note to instantiate.
        /// </summary>
        protected virtual Type StickyNoteType => typeof(StickyNoteModel);

        /// <summary>
        /// Creates a new sticky note and optionally adds it to the graph.
        /// </summary>
        /// <param name="position">The position of the sticky note to create.</param>
        /// <param name="spawnFlags">The flags specifying how the sticky note is to be spawned.</param>
        /// <returns>The newly created sticky note</returns>
        /// <remarks>
        /// 'CreateStickyNote' creates a new sticky note at the specified position and optionally adds it to the graph, depending on the provided spawnFlags.
        /// Use this method to create sticky notes for annotations, notes, or comments within a graph.
        /// The 'position' parameter defines where the sticky note appears, and 'spawnFlags' specifies the spawning behavior.
        /// </remarks>
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
            var stickyNoteModelType = StickyNoteType;
            var stickyNoteModel = ModelHelpers.Instantiate<StickyNoteModel>(stickyNoteModelType);
            stickyNoteModel.PositionAndSize = position;
            stickyNoteModel.GraphModel = this;
            return stickyNoteModel;
        }

        /// <summary>
        /// Removes and destroys a sticky note from the graph.
        /// </summary>
        /// <param name="stickyNoteToDelete">The sticky note to remove.</param>
        public virtual void DeleteStickyNote(StickyNoteModel stickyNoteToDelete)
        {
            if (stickyNoteToDelete.IsDeletable())
            {
                RemoveStickyNote(stickyNoteToDelete);
                stickyNoteToDelete.Destroy();
            }
        }

        /// <summary>
        /// Removes and destroys sticky notes from the graph.
        /// </summary>
        /// <param name="stickyNoteModels">The sticky notes to remove.</param>
        public void DeleteStickyNotes(IReadOnlyCollection<StickyNoteModel> stickyNoteModels)
        {
            using var dirtyScope = AssetDirtyScope();

            foreach (var stickyNoteModel in stickyNoteModels)
            {
                DeleteStickyNote(stickyNoteModel);
            }
        }

        /// <summary>
        /// The type of placemat to instantiate.
        /// </summary>
        protected virtual Type PlacematType => typeof(PlacematModel);

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
            var placematModelType = PlacematType;
            var placematModel = ModelHelpers.Instantiate<PlacematModel>(placematModelType);
            placematModel.TitleFontSize = 18;
            placematModel.TitleAlignment = TextAlignment.Left;
            placematModel.PositionAndSize = position;
            placematModel.GraphModel = this;
            if (guid.isValid)
                placematModel.SetGuid(guid);
            return placematModel;
        }

        /// <summary>
        /// Deletes a placemat from the graph.
        /// </summary>
        /// <param name="placematToDelete">The placemat to delete.</param>
        public virtual void DeletePlacemat(PlacematModel placematToDelete)
        {
            if (placematToDelete.IsDeletable())
            {
                RemovePlacemat(placematToDelete);
                placematToDelete.Destroy();
            }
        }

        /// <summary>
        /// Deletes placemats from the graph.
        /// </summary>
        /// <param name="placematModels">The list of placemats to delete.</param>
        public void DeletePlacemats(IReadOnlyCollection<PlacematModel> placematModels)
        {
            using var dirtyScope = AssetDirtyScope();

            foreach (var placematModel in placematModels)
            {
                DeletePlacemat(placematModel);
            }
        }

        /// <summary>
        /// The default <see cref="VariableDeclarationModelBase"/> type when creating new variables.
        /// </summary>
        public virtual Type VariableDeclarationType => typeof(VariableDeclarationModel);

        /// <summary>
        /// The default <see cref="VariableDeclarationModelBase"/> type when referencing external variables.
        /// </summary>
        protected virtual Type ExternalVariableDeclarationType => typeof(ExternalVariableDeclarationModel);

        /// <summary>
        /// The type of variable node to instantiate.
        /// </summary>
        protected virtual Type VariableNodeType => typeof(VariableNodeModel);

        /// <summary>
        /// The type of subgraph node to instantiate.
        /// </summary>
        protected virtual Type SubgraphNodeType => typeof(SubgraphNodeModel);

        /// <summary>
        /// The type of constant node to instantiate.
        /// </summary>
        protected virtual Type ConstantNodeType => typeof(ConstantNodeModel);

        /// <summary>
        /// Creates a new variable declaration in the graph.
        /// </summary>
        /// <param name="variableDataType">The type of data the new variable declaration to create represents.</param>
        /// <param name="variableName">The name of the new variable declaration to create.</param>
        /// <param name="modifierFlags">The modifier flags of the new variable declaration to create.</param>
        /// <param name="scope">The scope of the variable.</param>
        /// <param name="group">The group in which the variable is added. If null, it will go to the root group.</param>
        /// <param name="indexInGroup">The index of the variable in the group. For indexInGroup &lt;= 0, The item will be added at the beginning. For indexInGroup &gt;= Items.Count, items will be added at the end.</param>
        /// <param name="initializationModel">The initialization model of the new variable declaration to create. Can be <c>null</c>.</param>
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <param name="spawnFlags">The flags specifying how the variable declaration is to be spawned.</param>
        /// <returns>The newly created variable declaration.</returns>
        public virtual VariableDeclarationModel CreateGraphVariableDeclaration(TypeHandle variableDataType, string variableName,
            ModifierFlags modifierFlags, VariableScope scope, GroupModel group = null, int indexInGroup = int.MaxValue, Constant initializationModel = null, Hash128 guid = default,
            SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            if (IsContainerGraph() && (modifierFlags == ModifierFlags.Read || modifierFlags == ModifierFlags.Write))
            {
                Debug.LogWarning("Cannot create an input or an output variable declaration in a container graph.");
                return null;
            }

            if (!AllowExposedVariableCreation && scope == VariableScope.Exposed)
            {
                Debug.LogWarning("This graph doesn't allow the creation of a variable declaration with an exposed scope. A variable declaration with a local scope is created instead.");
                scope = VariableScope.Local;
            }

            return CreateGraphVariableDeclaration(VariableDeclarationType, variableDataType, variableName,
                modifierFlags, scope, group, indexInGroup, initializationModel, guid, InitCallback, spawnFlags);

            void InitCallback(VariableDeclarationModelBase variableDeclaration, Constant initModel)
            {
                if (variableDeclaration != null)
                {
                    variableDeclaration.VariableFlags = VariableFlags.None;

                    if (initModel != null) variableDeclaration.InitializationModel = initModel;
                }
            }
        }

        /// <summary>
        /// Renames an existing local subgraph node in the graph.
        /// Does not rename asset subgraph nodes.
        /// </summary>
        /// <param name="subgraphNodeModel">The local subgraph node to rename.</param>
        /// <param name="expectedNewName">The new name we want to give to the subgraph node.</param>
        public virtual void RenameSubgraphNode(SubgraphNodeModel subgraphNodeModel, string expectedNewName)
        {
            if (!subgraphNodeModel.IsReferencingLocalSubgraph)
                return;

            subgraphNodeModel.GetSubgraphModel().Name = expectedNewName;
            subgraphNodeModel.GetSubgraphModel().SetGraphObjectDirty();
            CurrentGraphChangeDescription.AddChangedModel(subgraphNodeModel, ChangeHint.Data);
        }

        /// <summary>
        /// Finds all node models that refer to a given declaration model.
        /// </summary>
        /// <param name="declarationModel">The declaration model to look for.</param>
        /// <typeparam name="T">The type of references to look for.</typeparam>
        /// <returns>A list of nodes that refer to the given declaration model.</returns>
        public IReadOnlyList<T> FindReferencesInGraph<T>(DeclarationModel declarationModel)
        {
            var results = new List<T>();
            FindReferencesInGraph(declarationModel, results);
            return results;
        }

        /// <summary>
        /// Finds all node models that refer to a given declaration model.
        /// </summary>
        /// <param name="declarationModel">The declaration model to lok for.</param>
        /// <param name="outReferences">A list to hold the nodes that refer to the given declaration model.</param>
        /// <typeparam name="T">The type of references to look for.</typeparam>
        /// <remarks><paramref name="outReferences"/> is not cleared before elements are appended to it.</remarks>
        public void FindReferencesInGraph<T>(DeclarationModel declarationModel, List<T> outReferences)
        {
            if (declarationModel == null || outReferences == null)
                return;

            foreach (var nodeModel in NodeModels)
            {
                if (nodeModel is IHasDeclarationModel hasDeclarationModel && hasDeclarationModel.DeclarationModel != null)
                {
                    if (hasDeclarationModel.DeclarationModel.Guid == declarationModel.Guid && hasDeclarationModel is T tElement)
                    {
                        outReferences.Add(tElement);
                    }
                }
            }
        }


        /// <summary>
        /// Finds all entry portals that refer to a given declaration model.
        /// </summary>
        /// <param name="declarationModel">The declaration model to look for.</param>
        /// <returns>A list of entry portals that refer to the given declaration model.</returns>
        public IReadOnlyList<WirePortalModel> GetEntryPortals(DeclarationModel declarationModel)
        {
            var entryPortals = new List<WirePortalModel>();
            var allReferences = FindReferencesInGraph<WirePortalModel>(declarationModel);
            for (var i = 0; i < allReferences.Count; i++)
            {
                switch (allReferences[i])
                {
                    case ISingleInputPortNodeModel:
                        entryPortals.Add(allReferences[i]);
                        break;
                }
            }

            return entryPortals;
        }

        /// <summary>
        /// Finds all exit portals that refer to a given declaration model.
        /// </summary>
        /// <param name="declarationModel">The declaration model to look for.</param>
        /// <returns>A list of exit portals that refer to the given declaration model.</returns>
        public IReadOnlyList<WirePortalModel> GetExitPortals(DeclarationModel declarationModel)
        {
            var exitPortals = new List<WirePortalModel>();
            var allReferences = FindReferencesInGraph<WirePortalModel>(declarationModel);
            for (var i = 0; i < allReferences.Count; i++)
            {
                switch (allReferences[i])
                {
                    case ISingleOutputPortNodeModel:
                        exitPortals.Add(allReferences[i]);
                        break;
                }
            }

            return exitPortals;
        }

        /// <summary>
        /// Creates a new variable declaration in the graph.
        /// </summary>
        /// <param name="variableDataType">The type of data the new variable declaration to create represents.</param>
        /// <param name="variableName">The name of the new variable declaration to create.</param>
        /// <param name="modifierFlags">The modifier flags of the new variable declaration to create.</param>
        /// <param name="scope">The scope of the variable.</param>
        /// <param name="group">The group in which the variable is added. If null, it will go to the root group.</param>
        /// <param name="indexInGroup">THe index of the variable in the group. For indexInGroup &lt;= 0, The item will be added at the beginning. For indexInGroup &gt;= Items.Count, items will be added at the end.</param>
        /// <param name="initializationModel">The initialization model of the new variable declaration to create. Can be <c>null</c>.</param>
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the variable declaration is created.</param>
        /// <param name="spawnFlags">The flags specifying how the variable declaration is to be spawned.</param>
        /// <typeparam name="TDeclType">The type of variable declaration to create.</typeparam>
        /// <returns>The newly created variable declaration.</returns>
        public TDeclType CreateGraphVariableDeclaration<TDeclType>(TypeHandle variableDataType,
            string variableName, ModifierFlags modifierFlags, VariableScope scope, GroupModel group = null, int indexInGroup = int.MaxValue, Constant initializationModel = null,
            Hash128 guid = default, Action<TDeclType, Constant> initializationCallback = null,
            SpawnFlags spawnFlags = SpawnFlags.Default)
            where TDeclType : VariableDeclarationModel
        {
            return (TDeclType)CreateGraphVariableDeclaration(typeof(TDeclType), variableDataType, variableName,
                modifierFlags, scope, group, indexInGroup, initializationModel, guid,
                (d, c) => initializationCallback?.Invoke((TDeclType)d, c),
                spawnFlags);
        }

        /// <summary>
        /// Creates a new variable declaration in the graph.
        /// </summary>
        /// <param name="variableTypeToCreate">The type of variable declaration to create.</param>
        /// <param name="variableDataType">The type of data the new variable declaration to create represents.</param>
        /// <param name="variableName">The name of the new variable declaration to create.</param>
        /// <param name="modifierFlags">The modifier flags of the new variable declaration to create.</param>
        /// <param name="scope">The scope of the variable.</param>
        /// <param name="group">The group in which the variable is added. If null, it will go to the root group.</param>
        /// <param name="indexInGroup">The index of the variable in the group. For indexInGroup &lt;= 0, The item will be added at the beginning. For indexInGroup &gt;= Items.Count, items will be added at the end.</param>
        /// <param name="initializationModel">The initialization model of the new variable declaration to create. Can be <c>null</c>.</param>
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the variable declaration is created.</param>
        /// <param name="spawnFlags">The flags specifying how the variable declaration is to be spawned.</param>
        /// <returns>The newly created variable declaration.</returns>
        public virtual VariableDeclarationModel CreateGraphVariableDeclaration(Type variableTypeToCreate,
            TypeHandle variableDataType, string variableName, ModifierFlags modifierFlags, VariableScope scope,
            GroupModel group = null, int indexInGroup = int.MaxValue, Constant initializationModel = null,
            Hash128 guid = default, Action<VariableDeclarationModelBase, Constant> initializationCallback = null,
            SpawnFlags spawnFlags = SpawnFlags.None)
        {
            if (IsContainerGraph() && (modifierFlags == ModifierFlags.Read || modifierFlags == ModifierFlags.Write))
            {
                Debug.LogWarning("Cannot create an input or an output variable declaration in a container graph.");
                return null;
            }

            using var assetDirtyScope = AssetDirtyScope();

            var variableDeclaration = InstantiateVariableDeclaration(variableTypeToCreate, variableDataType,
                variableName, modifierFlags, scope, initializationModel, guid, initializationCallback);

            if (variableDeclaration == null)
                return null;

            if (!spawnFlags.IsOrphan())
                AddVariableDeclaration(variableDeclaration);

            if (group != null)
                group.InsertItem(variableDeclaration, indexInGroup);
            else
            {
                var section = variableDeclaration.GraphModel.GetSectionModel(variableDeclaration.GraphModel.GetVariableSection(variableDeclaration));
                section.InsertItem(variableDeclaration, indexInGroup);
            }

            m_PlaceholderData[guid] = new PlaceholderData { GroupTitle = variableDeclaration.ParentGroup.Title };

            if (modifierFlags != ModifierFlags.None)
                RedefineSubgraphNodeModels();

            return variableDeclaration;
        }

        /// <summary>
        /// Creates a new external variable declaration in the graph.
        /// </summary>
        /// <param name="initializationCallback">A callback to complete the variable initialization.</param>
        /// <param name="group">The group in which the variable is added. If null, it will go to the root group.</param>
        /// <param name="indexInGroup">The index of the variable in the group. For indexInGroup &lt;= 0, The item will be added at the beginning. For indexInGroup &gt;= Items.Count, items will be added at the end.</param>
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <param name="spawnFlags">The flags specifying how the variable declaration is to be spawned.</param>
        /// <typeparam name="TDeclType">The type of variable declaration to create.</typeparam>
        /// <returns>The newly created external variable declaration.</returns>
        public TDeclType CreateExternalVariableReferenceDeclaration<TDeclType>(
            Action<TDeclType> initializationCallback,
            GroupModel group = null, int indexInGroup = int.MaxValue, Hash128 guid = default,
            SpawnFlags spawnFlags = SpawnFlags.None)
            where TDeclType : ExternalVariableDeclarationModelBase
        {
            return (TDeclType)CreateExternalVariableReferenceDeclaration(typeof(TDeclType),
                v => initializationCallback?.Invoke((TDeclType)v),
                group, indexInGroup, guid, spawnFlags);
        }

        /// <summary>
        /// Indicates whether a <see cref="VariableDeclarationModelBase"/> requires initialization.
        /// </summary>
        /// <param name="decl">The variable declaration model to query.</param>
        /// <returns>True if the variable declaration model requires initialization, false otherwise.</returns>
        public virtual bool VariableDeclarationRequiresInitialization(VariableDeclarationModelBase decl)
        {
            return decl.RequiresInitialization();
        }

        /// <summary>
        /// Gets a list of external sources that contains <see cref="VariableDeclarationModelBase"/> to import in the graph.
        /// </summary>
        /// <returns>A list of asset paths.</returns>
        public virtual IReadOnlyList<ExternalVariableSource> GetExternalVariableDeclarationModelSources()
        {
            return Array.Empty<ExternalVariableSource>();
        }

        /// <summary>
        /// Creates a new external variable declaration reference in the graph.
        /// </summary>
        /// <param name="variableTypeToCreate">The type of variable declaration to create.</param>
        /// <param name="initializationCallback">A callback to complete the variable initialization.</param>
        /// <param name="group">The group in which the variable is added. If null, it will go to the root group.</param>
        /// <param name="indexInGroup">The index of the variable in the group. For indexInGroup &lt;= 0, The item will be added at the beginning. For indexInGroup &gt;= Items.Count, items will be added at the end.</param>
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <param name="spawnFlags">The flags specifying how the variable declaration is to be spawned.</param>
        /// <returns>The newly created external variable declaration.</returns>
        public virtual ExternalVariableDeclarationModelBase CreateExternalVariableReferenceDeclaration(Type variableTypeToCreate,
            Action<ExternalVariableDeclarationModelBase> initializationCallback,
            GroupModel group = null, int indexInGroup = int.MaxValue, Hash128 guid = default,
            SpawnFlags spawnFlags = SpawnFlags.None)
        {
            using var assetDirtyScope = AssetDirtyScope();

            variableTypeToCreate ??= ExternalVariableDeclarationType;
            var variableDeclaration = ModelHelpers.Instantiate<ExternalVariableDeclarationModelBase>(variableTypeToCreate);

            initializationCallback?.Invoke(variableDeclaration);

            if (guid.isValid)
                variableDeclaration.SetGuid(guid);
            variableDeclaration.GraphModel = this;

            if (!spawnFlags.IsOrphan())
                AddVariableDeclaration(variableDeclaration);

            if (group != null)
                group.InsertItem(variableDeclaration, indexInGroup);
            else
            {
                var section = variableDeclaration.GraphModel.GetSectionModel(variableDeclaration.GraphModel.GetVariableSection(variableDeclaration));
                section.InsertItem(variableDeclaration, indexInGroup);
            }

            m_PlaceholderData[guid] = new PlaceholderData { GroupTitle = variableDeclaration.ParentGroup.Title };

            return variableDeclaration;
        }

        /// <summary>
        /// Adds and removes external variable declaration references by consulting the external sources given by <see cref="GetExternalVariableDeclarationModelSources"/>.
        /// </summary>
        public void UpdateExternalVariableDeclarationReferences()
        {
            // The automated changes done here should not make the asset dirty.
            using var dirtyScope = BlockAssetDirtyScope();

            // First, create all missing external variable references and gather all valid external variable references in validVariables (new and existing).
            var validVariables = new List<ExternalVariableDeclarationModelBase>();
            var globalVariableSourceAssets = GetExternalVariableDeclarationModelSources();
            if (globalVariableSourceAssets != null)
            {
                foreach (var globalVariableSourceAsset in globalVariableSourceAssets)
                {
                    CreateExternalVariableReferencesForAsset(globalVariableSourceAsset, validVariables);
                }
            }

            // Then, remove all external variable references that are not valid anymore.
            var toDelete = new List<VariableDeclarationModelBase>();
            foreach (var variableDeclaration in VariableDeclarations)
            {
                if (variableDeclaration is ExternalVariableDeclarationModelBase)
                {
                    var found = false;
                    foreach (var validVariable in validVariables)
                    {
                        if (validVariable.Equals(variableDeclaration))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        variableDeclaration.SetCapability(Capabilities.Deletable, true);
                        toDelete.Add(variableDeclaration);
                    }
                }
            }

            DeleteVariableDeclarations(toDelete);
        }

        /// <summary>
        /// Creates external variable references for a given source.
        /// </summary>
        /// <param name="externalVariableSource">The source of variable declarations.</param>
        /// <param name="validVariables">The list of all variables that would be created, if none were already defined.</param>
        protected virtual void CreateExternalVariableReferencesForAsset(ExternalVariableSource externalVariableSource , List<ExternalVariableDeclarationModelBase> validVariables)
        {
            using var dirtyScope = AssetDirtyScope();

            var externalVariableDeclarations = new List<VariableDeclarationModelBase>();
            externalVariableSource.GetVariableDeclarations(externalVariableDeclarations);
            foreach (var externalVariableDeclarationModel in externalVariableDeclarations)
            {
                var externalReferenceFound = false;
                foreach (var localVariableDeclarationModel in VariableDeclarations)
                {
                    if (localVariableDeclarationModel is ExternalVariableDeclarationModel localExternDecl)
                    {
                        if (localExternDecl.IsReferringTo(externalVariableSource, externalVariableDeclarationModel.Guid))
                        {
                            externalReferenceFound = true;
                            // Since we don't really know what happened, we need to assume that the external definition changed.
                            CurrentGraphChangeDescription.AddChangedModel(localVariableDeclarationModel, ChangeHint.Data);
                            validVariables.Add(localExternDecl);
                            break;
                        }
                    }
                }

                if (!externalReferenceFound)
                {
                    var guid = externalVariableDeclarationModel.Guid;

                    var newVariable = CreateExternalVariableReferenceDeclaration(typeof(ExternalVariableDeclarationModel),
                        v =>
                        {
                            (v as ExternalVariableDeclarationModel)?.SetReference(externalVariableSource, guid);
                        });
                    validVariables.Add(newVariable);
                    CurrentGraphChangeDescription.AddNewModel(newVariable);
                }
            }
        }

        /// <summary>
        /// Instantiates a new variable declaration.
        /// </summary>
        /// <param name="variableTypeToCreate">The type of variable to create.</param>
        /// <param name="variableDataType">The type of data the new variable declaration to create represents.</param>
        /// <param name="variableName">The name of the new variable declaration to create.</param>
        /// <param name="modifierFlags">The modifier flags of the new variable declaration to create.</param>
        /// <param name="scope">The scope of the variable.</param>
        /// <param name="initializationModel">The initialization model of the new variable declaration to create. Can be <c>null</c>.</param>
        /// <param name="guid">The guid to assign to the newly created item. If none is provided, a new
        /// guid will be generated for it.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the variable declaration is created.</param>
        /// <returns>The newly created variable declaration.</returns>
        protected virtual VariableDeclarationModel InstantiateVariableDeclaration(Type variableTypeToCreate,
            TypeHandle variableDataType, string variableName, ModifierFlags modifierFlags, VariableScope scope,
            Constant initializationModel, Hash128 guid, Action<VariableDeclarationModelBase, Constant> initializationCallback = null)
        {
            var variableDeclaration = ModelHelpers.Instantiate<VariableDeclarationModel>(variableTypeToCreate);

            if (guid.isValid)
                variableDeclaration.SetGuid(guid);
            variableDeclaration.GraphModel = this;
            variableDeclaration.DataType = variableDataType;
            if (initializationModel != null)
                variableDeclaration.InitializationModel = initializationModel;
            variableDeclaration.Title = GenerateGraphVariableDeclarationUniqueName(variableName);
            variableDeclaration.Scope = scope;
            variableDeclaration.Modifiers = modifierFlags;

            initializationCallback?.Invoke(variableDeclaration, variableDeclaration.InitializationModel);

            return variableDeclaration;
        }

        VariableDeclarationPlaceholder InstantiateVariableDeclarationPlaceholder(TypeHandle variableDataType, string variableName, Hash128 guid)
        {
            var variableDeclaration = ModelHelpers.Instantiate<VariableDeclarationPlaceholder>(typeof(VariableDeclarationPlaceholder));

            if (guid.isValid)
                variableDeclaration.SetGuid(guid);
            variableDeclaration.GraphModel = this;
            variableDeclaration.DataType = variableDataType;
            variableDeclaration.Title = GenerateGraphVariableDeclarationUniqueName(variableName);
            return variableDeclaration;
        }

        /// <summary>
        /// Renames an existing variable declaration in the graph.
        /// </summary>
        /// <param name="variable">The variable to rename.</param>
        /// <param name="expectedNewName">The new name we want to give to the variable.</param>
        /// <remarks>Duplicates of variable names are allowed, only if the user manually enter it themselves. Automatic functions discourage duplicates.</remarks>
        public virtual void RenameVariable(VariableDeclarationModelBase variable, string expectedNewName)
        {
            m_ExistingVariableNames.Remove(variable.Title);
            m_ExistingVariableNames.Add(expectedNewName);
            variable.Title = expectedNewName;
        }

        /// <summary>
        /// Generates a unique name for a variable declaration in the graph.
        /// </summary>
        /// <param name="originalName">The name of the variable declaration.</param>
        /// <returns>The unique name for the variable declaration.</returns>
        protected virtual string GenerateGraphVariableDeclarationUniqueName(string originalName)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var names = m_ExistingVariableNames.ToArray();
#pragma warning restore UA2001
            var name = ObjectNames.GetUniqueName(names, originalName);
            return name;
        }

        /// <summary>
        /// Creates a constant of the type represented by <paramref name="constantTypeHandle"/>
        /// </summary>
        /// <param name="constantTypeHandle">The type of the constant that will be created.</param>
        /// <returns>A new constant.</returns>
        public virtual Constant CreateConstantValue(TypeHandle constantTypeHandle)
        {
            var constantType = GetConstantType(constantTypeHandle);
            if (constantType == null)
                return null;

            var instance = (Constant)Activator.CreateInstance(constantType);
            instance.Initialize(constantTypeHandle);
            return instance;
        }

        /// <summary>
        /// Gets the constant type associated with the given <see cref="TypeHandle"/>.
        /// </summary>
        /// <param name="typeHandle">The handle for which to retrieve the type.</param>
        /// <returns>The type associated with <paramref name="typeHandle"/></returns>
        public virtual Type GetConstantType(TypeHandle typeHandle)
        {
            if (typeHandle.IsCustomTypeHandle())
                return null;

            Type t = typeHandle.Resolve();
            if (t.IsEnum && t != typeof(Enum))
                return typeof(EnumConstant);

            if (t == typeof(void))
                return null;

            return typeof(Constant<>).MakeGenericType(t);
        }

        /// <summary>
        /// Indicates whether a given type handle can be assigned to another type handle.
        /// </summary>
        /// <param name="destination">The destination type handle.</param>
        /// <param name="source">The source type handle.</param>
        /// <returns>Whether a given type handle can be assigned to another type handle.</returns>
        public virtual bool CanAssignTo(TypeHandle destination, TypeHandle source)
        {
            return destination == TypeHandle.Unknown || source.IsAssignableFrom(destination);
        }

        /// <summary>
        /// The type of group model to instantiate.
        /// </summary>
        public virtual Type GroupModelType => typeof(GroupModel);

        /// <summary>
        /// Indicates whether a given type handle from a port can be assigned to another type handle from a port.
        /// </summary>
        /// <param name="destination">The destination port to which we want to assign type handle.</param>
        /// <param name="source">The source port from which we want to assign type handle.</param>
        /// <returns>Whether a given port's data handle can be assigned to another port's type handle.</returns>
        public virtual bool CanAssignTo(PortModel destination, PortModel source)
        {
            return destination.CanConnectPort(source);
        }

        /// <summary>
        /// Instantiates a group model.
        /// </summary>
        /// <param name="title">The title of the group model</param>
        /// <returns>The created group model.</returns>
        protected virtual GroupModel InstantiateGroup(string title)
        {
            var groupType = GroupModelType;
            var group = ModelHelpers.Instantiate<GroupModel>(groupType);
            group.Title = title;
            group.GraphModel = this;
            return group;
        }

        /// <summary>
        /// Registers a group to the graph.
        /// </summary>
        /// <param name="group">The group.</param>
        protected virtual void AddGroup(GroupModelBase group)
        {
            // Group is not added to the graph: it will be added to a section.
            RegisterElement(group);
            CurrentGraphChangeDescription.AddNewModel(group);
        }

        /// <summary>
        /// Unregisters a group from the graph.
        /// </summary>
        /// <param name="group">The group.</param>
        protected virtual void RemoveGroup(GroupModel group)
        {
            UnregisterElement(group);
            CurrentGraphChangeDescription.AddDeletedModel(group);
        }

        /// <summary>
        /// Creates a new group.
        /// </summary>
        /// <param name="title">The title of the new group.</param>
        /// <param name="items">An optional list of items that will be added to the group.</param>
        /// <returns>A new group.</returns>
        public virtual GroupModel CreateGroup(string title, IReadOnlyCollection<IGroupItemModel> items = null)
        {
            using var assetDirtyScope = AssetDirtyScope();

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
        /// <param name="indexInGroup">The index of the duplicated variable in its group. By default, the index is -1.</param>
        /// <returns>The duplicated variable declaration.</returns>
        public virtual TDeclType DuplicateGraphVariableDeclaration<TDeclType>(TDeclType sourceModel, bool keepGuid = false, int indexInGroup = -1) where TDeclType : VariableDeclarationModelBase
        {
            if (sourceModel.GraphModel == this && !sourceModel.IsCopiable())
                return null;

            using var assetDirtyScope = AssetDirtyScope();

            var uniqueName = sourceModel.Title;
            var copy = sourceModel.Clone();
            copy.GraphModel = this;
            if (keepGuid)
                copy.SetGuid(sourceModel.Guid);
            copy.Title = GenerateGraphVariableDeclarationUniqueName(uniqueName);

            AddVariableDeclaration(copy);

            if (sourceModel.ParentGroup != null && sourceModel.ParentGroup.GraphModel == this && sourceModel.ParentGroup is GroupModel parentGroup)
                parentGroup.InsertItem(copy, indexInGroup);
            else
            {
                var section = GetSectionModel(GetVariableSection(copy));

                if (section is null)
                {
                    section = new SectionModel()
                    {
                        Title = GetVariableSection(copy)
                    };
                }

                section.InsertItem(copy, indexInGroup);
            }

            return copy;
        }

        /// <summary>
        /// Indicates if a variable declaration can be pasted or duplicated.
        /// </summary>
        /// <param name="originalModel">The variable declaration model to copy.</param>
        /// <returns>True if the variable declaration can be pasted or duplicated.</returns>
        public abstract bool CanPasteVariable(VariableDeclarationModelBase originalModel);

        /// <summary>
        /// Deletes the given variable declaration model, with the option of also deleting the corresponding variable models.
        /// </summary>
        /// <remarks>If <paramref name="deleteUsages"/> is <c>false</c>, the user has to take care of deleting the corresponding variable models prior to this call.</remarks>
        /// <param name="variableModel">The variable declaration model to delete.</param>
        /// <param name="deleteUsages">Whether to delete the corresponding variable models.</param>
        public virtual void DeleteVariableDeclaration(VariableDeclarationModelBase variableModel, bool deleteUsages = true)
        {
            using var dirtyScope = AssetDirtyScope();

            if (!variableModel.IsDeletable())
                return;

            if (variableModel is VariableDeclarationPlaceholder placeholderModel)
                RemovePlaceholder(placeholderModel);

            RemoveVariableDeclaration(variableModel);

            if (deleteUsages)
            {
                var nodesToDelete = FindReferencesInGraph<AbstractNodeModel>(variableModel);
                DeleteNodes(nodesToDelete, deleteConnections: true);
            }
        }

        /// <summary>
        /// Deletes the given variable declaration models, with the option of also deleting the corresponding variable models.
        /// </summary>
        /// <remarks>If <paramref name="deleteUsages"/> is <c>false</c>, the user has to take care of deleting the corresponding variable models prior to this call.</remarks>
        /// <param name="variableModels">The variable declaration models to delete.</param>
        /// <param name="deleteUsages">Whether to delete the corresponding variable models.</param>
        public void DeleteVariableDeclarations(IReadOnlyCollection<VariableDeclarationModelBase> variableModels, bool deleteUsages = true)
        {
            using var dirtyScope = AssetDirtyScope();

            foreach (var variableModel in variableModels)
            {
                DeleteVariableDeclaration(variableModel, deleteUsages);
            }
        }

        /// <summary>
        /// Deletes the given group models.
        /// </summary>
        /// <param name="groupModels">The group models to delete.</param>
        public void DeleteGroups(IReadOnlyCollection<GroupModel> groupModels)
        {
            using var dirtyScope = AssetDirtyScope();

            var deletedModels = new List<GraphElementModel>();
            var deletedVariables = new List<VariableDeclarationModelBase>();

            void RecurseRemoveGroup(GroupModel groupModel)
            {
                RemoveGroup(groupModel);
                foreach (var item in groupModel.Items)
                {
                    if (item is VariableDeclarationModelBase variable)
                        deletedVariables.Add(variable);
                    else if (item is GroupModel group)
                        RecurseRemoveGroup(group);
                    else
                        deletedModels.Add(item as GraphElementModel);
                }
            }

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var groupModel in groupModels.Where(v => v.IsDeletable()))
#pragma warning restore UA2001
            {
                (groupModel.ParentGroup as GroupModel)?.RemoveItem(groupModel);
                RecurseRemoveGroup(groupModel);
            }

            DeleteVariableDeclarations(deletedVariables);
            CurrentGraphChangeDescription.AddDeletedModels(deletedModels);
        }

        /// <summary>
        /// Returns the type of portal to instantiate.
        /// </summary>
        protected virtual Type PortalType => typeof(DeclarationModel);

        /// <summary>
        /// The type of wire portal entry nodes to instantiate.
        /// </summary>
        protected virtual Type WirePortalEntryNodeType => typeof(WirePortalEntryModel);

        /// <summary>
        /// The type of wire portal exit nodes to instantiate.
        /// </summary>
        protected virtual Type WirePortalExitNodeType => typeof(WirePortalExitModel);


        /// <summary>
        /// The <see cref="GraphModel"/> that contains this <see cref="GraphModel"/>, if any.
        /// </summary>
        public GraphModel ParentGraph
        {
            get;
            internal set;
        }

        /// <summary>
        /// Check if this <see cref="GraphModel"/> is used as a local subgraph.
        /// </summary>
        public bool IsLocalSubgraph
        {
            get => ParentGraph != null;
        }


        // For tests
        /// <summary>
        /// The local subgraphs of this <see cref="GraphModel"/>.
        /// </summary>
        internal IReadOnlyList<GraphModel> LocalSubgraphs => m_LocalSubgraphs;

        /// <summary>
        /// Creates a new declaration model representing a portal and optionally add it to the graph.
        /// </summary>
        /// <param name="portalName">The name of the portal</param>
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <param name="spawnFlags">The flags specifying how the portal is to be spawned.</param>
        /// <returns>The newly created declaration model</returns>
        public virtual DeclarationModel CreateGraphPortalDeclaration(string portalName, Hash128 guid = default, SpawnFlags spawnFlags = SpawnFlags.None)
        {
            if (!AllowPortalCreation)
            {
                throw new InvalidOperationException("Portal creation is disabled.");
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
            if (!AllowPortalCreation)
            {
                throw new InvalidOperationException("Portal creation is disabled.");
            }

            var portalModelType = PortalType;
            var portalModel = ModelHelpers.Instantiate<DeclarationModel>(portalModelType);
            portalModel.Title = portalName;
            if (guid.isValid)
                portalModel.SetGuid(guid);
            portalModel.GraphModel = this;
            return portalModel;
        }

        /// <summary>
        /// Creates a portal opposite to <paramref name="wirePortalModel"/>, positioned just beside it.
        /// </summary>
        /// <param name="wirePortalModel">The portal for which an opposite portal should be created.</param>
        /// <param name="spawnFlags">Creation flags for the new portal.</param>
        /// <returns>The new portal.</returns>
        public AbstractNodeModel CreateOppositePortal(WirePortalModel wirePortalModel, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            var portalOffset = Vector2.right * 150;
            var offset = wirePortalModel switch
            {
                ISingleInputPortNodeModel _ => portalOffset,
                ISingleOutputPortNodeModel _ => -portalOffset,
                _ => Vector2.zero
            };
            var currentPos = wirePortalModel?.Position ?? Vector2.zero;
            return CreateOppositePortal(wirePortalModel, currentPos + offset, spawnFlags);
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
            if (!AllowPortalCreation)
            {
                throw new InvalidOperationException("Portal creation is disabled.");
            }

            using var dirtyScope = AssetDirtyScope();

            WirePortalModel createdPortal = null;
            Type oppositeType = null;
            switch (wirePortalModel)
            {
                case WirePortalEntryModel _:
                    oppositeType = WirePortalExitNodeType;
                    break;
                case WirePortalExitModel _:
                    oppositeType = WirePortalEntryNodeType;
                    break;
            }

            if (oppositeType != null)
                createdPortal = CreateNode(oppositeType, wirePortalModel.Title, position, spawnFlags: spawnFlags, initializationCallback: n => ((WirePortalModel)n).SetPortDataTypeHandle(wirePortalModel.GetPortDataTypeHandle())) as WirePortalModel;

            if (createdPortal != null)
                createdPortal.SetDeclarationModel(wirePortalModel.DeclarationModel);

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
            if (!AllowPortalCreation)
            {
                throw new InvalidOperationException("Portal creation is disabled.");
            }

            using var dirtyScope = AssetDirtyScope();

            var inputPortModel = wireModel.ToPort;
            var outputPortModel = wireModel.FromPort;

            // Only a single portal per output port. Don't recreate if we already created one.
            WirePortalModel portalEntry = null;

            if (outputPortModel != null && !existingPortalEntries.TryGetValue(wireModel.FromPort, out portalEntry))
            {
                portalEntry = CreateEntryPortalFromPort(outputPortModel, entryPortalPosition, portalHeight);
                wireModel.SetPort(WireSide.To, (portalEntry as ISingleInputPortNodeModel)?.InputPort);
                existingPortalEntries[outputPortModel] = portalEntry;
                CurrentGraphChangeDescription.AddChangedModel(wireModel, ChangeHint.Layout);
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

            var portalExit = CreateExitPortalToPort(inputPortModel, exitPortalPosition, portalHeight, portalEntry.DeclarationModel);
            portalExits.Add(portalExit);

            CreateWire(inputPortModel, (portalExit as ISingleOutputPortNodeModel)?.OutputPort);
        }

        /// <summary>
        /// Reverts portals to wires.
        /// </summary>
        /// <param name="portalModel">The selected portal to revert.</param>
        /// <param name="shouldRevertAll">Whether all portals with the same declaration as the selected portal should be reverted, else only the pairs containing the selected portal will be reverted.</param>
        /// <returns>The list of <see cref="WireModel"/>s created or <c>null</c> if none were created.</returns>
        public virtual List<WireModel> RevertPortalsToWires(WirePortalModel portalModel, bool shouldRevertAll)
        {
            if (portalModel == null || !TryGetModelFromGuid(portalModel.Guid, out _))
                return null;

            var exitPortals = GetExitPortals(portalModel.DeclarationModel);
            var entryPortals = GetEntryPortals(portalModel.DeclarationModel);

            // If there is no entry portals (or vice-versa), wires cannot be created.
            if (entryPortals.Count == 0 || exitPortals.Count == 0)
                return null;

            var fromPorts = new List<PortModel>();
            var toPorts = new List<PortModel>();

            if (shouldRevertAll)
            {
                foreach (var exitPortal in exitPortals)
                {
                    var connectedPorts = (exitPortal as ISingleOutputPortNodeModel)?.OutputPort.GetConnectedPorts();
                    if (connectedPorts != null)
                        toPorts.AddRange(connectedPorts);
                }
                foreach (var entryPortal in entryPortals)
                {
                    var connectedPorts = (entryPortal as ISingleInputPortNodeModel)?.InputPort.GetConnectedPorts();
                    if (connectedPorts != null)
                        fromPorts.AddRange(connectedPorts);
                }

                DeleteNodes(entryPortals, true);
                DeleteNodes(exitPortals, true);
            }
            else if (portalModel is ISingleInputPortNodeModel selectedEntryPortal)
            {
                foreach (var exitPortal in exitPortals)
                {
                    var connectedPorts = (exitPortal as ISingleOutputPortNodeModel)?.OutputPort.GetConnectedPorts();
                    if (connectedPorts != null)
                        toPorts.AddRange(connectedPorts);
                }

                fromPorts = selectedEntryPortal.InputPort.GetConnectedPorts() as List<PortModel>;

                DeleteNode(portalModel, true);

                // Only if there were only entry portal, delete the exit portals
                if (entryPortals.Count == 1)
                    DeleteNodes(exitPortals, true);
            }
            else if (portalModel is ISingleOutputPortNodeModel selectedExitPortal)
            {
                foreach (var entryPortal in entryPortals)
                {
                    var connectedPorts = (entryPortal as ISingleInputPortNodeModel)?.InputPort.GetConnectedPorts();
                    if (connectedPorts != null)
                        fromPorts.AddRange(connectedPorts);
                }

                toPorts = selectedExitPortal.OutputPort.GetConnectedPorts() as List<PortModel>;

                DeleteNode(portalModel, true);

                // If there were only one exit portal, delete the entry portals
                if (exitPortals.Count == 1)
                    DeleteNodes(entryPortals, true);
            }

            if (toPorts == null || fromPorts == null)
                return null;

            // Create wires replacing the portals.
            var wireModels = new List<WireModel>();
            foreach (var toPort in toPorts)
            {
                foreach (var fromPort in fromPorts)
                {
                    wireModels.Add(CreateWire(toPort, fromPort));
                }
            }

            return wireModels;
        }

        /// <summary>
        /// Creates an exit portal matching a port.
        /// </summary>
        /// <param name="outputPortModel">The output port model to which the portal will be connected.</param>
        /// <param name="position">The desired position of the entry portal.</param>
        /// <param name="height">The desired height of the entry portal.</param>
        /// <param name="declarationModel">The declaration of the portal. If null, a new one will be created.</param>
        /// <param name="offset">The offset to apply to the portal.</param>
        /// <returns>The created entry portal.</returns>
        public virtual WirePortalModel CreateEntryPortalFromPort(PortModel outputPortModel, Vector2 position, int height, DeclarationModel declarationModel = null, float offset = 16)
        {
            if (!AllowPortalCreation)
            {
                throw new InvalidOperationException("Portal creation is disabled.");
            }

            using var dirtyScope = AssetDirtyScope();

            if (outputPortModel.NodeModel is not InputOutputPortsNodeModel nodeModel)
                return null;

            string portalName;
            if (nodeModel is ConstantNodeModel constantNodeModel)
                portalName = TypeHelpers.GetFriendlyName(constantNodeModel.Type);
            else
            {
                portalName = nodeModel.Title ?? "";
                var portName = outputPortModel.Title ?? "";
                if (!string.IsNullOrEmpty(portName))
                    portalName += " - " + portName;
            }

            var portalEntry = CreateWirePortalNode(WirePortalEntryNodeType, declarationModel ?? CreateGraphPortalDeclaration(portalName), outputPortModel.DataTypeHandle, position);

            // y offset based on port order. hurgh.
            var idx = nodeModel.OutputsByDisplayOrder.IndexOf(outputPortModel);
            portalEntry.Position += Vector2.down * (height * idx + offset); // Fudgy.

            return portalEntry;
        }

        /// <summary>
        /// Creates an exit portal matching a port.
        /// </summary>
        /// <param name="inputPortModel">The input port model to which the portal will be connected.</param>
        /// <param name="position">The desired position of the exit portal.</param>
        /// <param name="height">The desired height of the exit portal.</param>
        /// <param name="declarationModel">The declaration of the portal</param>
        /// <param name="offset">The offset of the portal.</param>
        /// <returns>The created exit portal.</returns>
        public virtual WirePortalModel CreateExitPortalToPort(PortModel inputPortModel, Vector2 position, int height, DeclarationModel declarationModel, float offset = 16)
        {
            if (!AllowPortalCreation)
            {
                throw new InvalidOperationException("Portal creation is disabled.");
            }

            using var dirtyScope = AssetDirtyScope();

            var portalExit = CreateWirePortalNode(WirePortalExitNodeType, declarationModel, inputPortModel.DataTypeHandle, position);

            portalExit.Position = position;
            {
                if (inputPortModel.NodeModel is InputOutputPortsNodeModel nodeModel)
                {
                    // y offset based on port order. hurgh.
                    var idx = nodeModel.InputsByDisplayOrder.IndexOf(inputPortModel);
                    portalExit.Position += Vector2.down * (height * idx + offset); // Fudgy.
                }
            }

            return portalExit;
        }

        /// <summary>
        /// Creates a new wire portal node in the graph.
        /// </summary>
        /// <param name="portalType">The type of the portal.</param>
        /// <param name="declarationModel">The declaration for the wire portal node.</param>
        /// <param name="portDataTypeHandle">The data type of the port.</param>
        /// <param name="position">The position of the node to create.</param>
        /// <param name="name">The name of the portal node.</param>
        /// <param name="guid">The guid to assign to the newly created item.</param>
        /// <param name="initializationCallback">An initialization method to be called right after the node is created.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <returns>The newly created variable node.</returns>
        public virtual WirePortalModel CreateWirePortalNode(Type portalType, DeclarationModel declarationModel, TypeHandle portDataTypeHandle,
            Vector2 position, string name = "", Hash128 guid = default, Action<AbstractNodeModel> initializationCallback = null, SpawnFlags spawnFlags = SpawnFlags.Default)
        {
            return CreateNode(portalType, name, position, guid, initializationCallback: n =>
            {
                if (n is WirePortalModel wirePortalModel)
                {
                    wirePortalModel.SetPortDataTypeHandle(portDataTypeHandle);
                    wirePortalModel.SetDeclarationModel(declarationModel);
                }
                initializationCallback?.Invoke(n);
            }, spawnFlags: spawnFlags) as WirePortalModel;
        }

        /// <summary>
        /// Retrieves all the portals linked to the given <paramref name="portalModel"/> (if any).
        /// </summary>
        /// <remarks>
        /// For an entry portal, all the linked exit portals are returned.<br/>
        /// For an exit portal, all the linked entry portals are returned.
        /// </remarks>
        /// <param name="portalModel">The portal to retrieve the linked portals from.</param>
        /// <returns>The portals linked to the given one.</returns>
        public virtual IEnumerable<WirePortalModel> GetLinkedPortals(WirePortalModel portalModel)
        {
            if (portalModel != null)
            {
                return this.FindReferencesInGraph<WirePortalModel>(portalModel.DeclarationModel);
            }

            return Array.Empty<WirePortalModel>();
        }

        /// <summary>
        /// Retrieves the portals models dependent on the given <paramref name="portalModel"/> (if any).
        /// </summary>
        /// <remarks>
        /// In a pull model, an exit portal's dependency are the entry portals linked to it and entry portals have no dependencies.<br/>
        /// In a push model, an entry portal's dependency are the exit portals linked to it and exit portals have no dependencies.
        /// </remarks>
        /// <param name="portalModel">The portal to retrieve the dependent portals from.</param>
        /// <returns>The portals dependent on the given one.</returns>
        public virtual IEnumerable<WirePortalModel> GetPortalDependencies(WirePortalModel portalModel)
        {
            if (portalModel is ISingleInputPortNodeModel)
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                return this.FindReferencesInGraph<WirePortalModel>(portalModel.DeclarationModel).Where(n => n is ISingleOutputPortNodeModel);
#pragma warning restore UA2001
            }

            return Array.Empty<WirePortalModel>();
        }

        /// <summary>
        /// Executes tasks to perform when the <see cref="GraphToolkit.Editor.GraphObject"/> is enabled.
        /// </summary>
        public virtual void OnEnable()
        {
            MigrateAssetSubgraphsAndGraphReferences();

            // Called when asset is saved. Make sure m_DirtyScopes reflects the reality.
            if ((GraphObject == null || !GraphObject.Dirty) && m_DirtyScopes != null)
            {
                foreach (var dirtyScope in m_DirtyScopes)
                {
                    dirtyScope.Dirty = false;
                }
            }

            foreach (var(_, model) in GetElementsByGuid())
            {
                if (model is null)
                    continue;

                model.GraphModel = this;
            }

            m_LocalSubgraphs ??= new List<GraphModel>();
            foreach (var subGraphModel in m_LocalSubgraphs)
            {
                subGraphModel.OnEnable();
            }

            // This will create the default section if it does not exist.
            CheckGroupConsistency();

            foreach (var nodeModel in NodeModels)
            {
                RecurseDefineNode(nodeModel);
            }

            // Remove the null references that were added to keep the topology of graph element lists while missing types were present.
            RemoveNullReferencesAddedWhileHasMissingTypes();

            GetGraphProcessorContainer().OnGraphModelEnabled();
        }

        /// <summary>
        /// Executes tasks to perform when the <see cref="GraphToolkit.Editor.GraphObject"/> is disabled.
        /// </summary>
        public virtual void OnDisable()
        {
            if (m_LocalSubgraphs != null)
            {
                foreach (var subgraph in m_LocalSubgraphs)
                {
                    subgraph.OnDisable();
                }
            }

            GetGraphProcessorContainer().OnGraphModelDisabled();

            foreach (var node in NodeModels)
                UIDependencies.RemoveModel(node);
            foreach (var wire in WireModels)
                UIDependencies.RemoveModel(wire);
            foreach (var stickyNote in m_GraphStickyNoteModels)
                UIDependencies.RemoveModel(stickyNote);
            foreach (var placemat in m_GraphPlacematModels)
                UIDependencies.RemoveModel(placemat);
            foreach (var variable in m_GraphVariableModels)
                UIDependencies.RemoveModel(variable);
            foreach (var portal in m_GraphPortalModels)
                UIDependencies.RemoveModel(portal);
            foreach (var section in m_SectionModels)
                UIDependencies.RemoveModel(section);
        }

        void RecurseDefineNode(AbstractNodeModel nodeModel)
        {
            nodeModel?.SyncNodePreview();
            (nodeModel as NodeModel)?.DefineNode();
            if (nodeModel is IGraphElementContainer container)
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                foreach (var subNodeModel in container.GetGraphElementModels().OfType<AbstractNodeModel>())
#pragma warning restore UA2001
                {
                    RecurseDefineNode(subNodeModel);
                }
            }
        }

        /// <summary>
        /// Migrates subgraphs saved as subassets to subgraphs stored in the graph model.
        /// Upgrades all graph references from <see cref="GraphAssetReference"/> and <see cref="SubgraphAssetReference"/>
        /// to <see cref="GraphReference"/>.
        /// </summary>
        protected void MigrateAssetSubgraphsAndGraphReferences()
        {
            // If there already are local subgraphs, this is not an graph that needs to be migrated.
            if (m_LocalSubgraphs is { Count: > 0 })
                return;
            UpgradeGraphReferences();
        }

        /// <summary>
        /// Migrates subgraphs saved as subassets to subgraphs stored in the graph model.
        /// Upgrades all graph references from <see cref="GraphAssetReference"/> and <see cref="SubgraphAssetReference"/>
        /// to <see cref="GraphReference"/>.
        /// </summary>
        /// <remarks>Extend this method if your graph model stores graph references (other than in <see cref="SubgraphNodeModel"/>).</remarks>
        public virtual void UpgradeGraphReferences()
        {
            foreach (var nodeModel in NodeModels)
            {
                if (nodeModel is SubgraphNodeModel subgraphNodeModel)
                {
                    subgraphNodeModel.UpgradeGraphReference();
                }
            }
        }

        /// <summary>
        /// Gets the graph processor container.
        /// </summary>
        /// <returns>The graph processor container.</returns>
        public GraphProcessorContainer GetGraphProcessorContainer()
        {
            if (m_GraphProcessorContainer == null)
            {
                m_GraphProcessorContainer = new GraphProcessorContainer();
                CreateGraphProcessors();
            }

            return m_GraphProcessorContainer;
        }

        /// <summary>
        /// Creates the graph processors.
        /// </summary>
        protected virtual void CreateGraphProcessors()
        {
            if (AllowMultipleDataOutputInstances == AllowMultipleDataOutputInstances.AllowWithWarning)
                GetGraphProcessorContainer().AddGraphProcessor(new VariableNodeGraphProcessor(this));
        }

        /// <summary>
        /// Converts a <see cref="GraphProcessingError"/> to a <see cref="GraphProcessingErrorModel"/>.
        /// </summary>
        /// <param name="error">The error to convert.</param>
        /// <returns>The converted error.</returns>
        public virtual GraphProcessingErrorModel CreateProcessingErrorModel(GraphProcessingError error)
        {
            return new GraphProcessingErrorModel(error);
        }

        /// <summary>
        /// Executes tasks to perform when an undo or redo operation occurs.
        /// </summary>
        public virtual void UndoRedoPerformed()
        {
            using var assetDirtyScope = AssetDirtyScope();

            foreach (var nodeModel in NodeModels)
            {
                RecurseDefineNode(nodeModel);
            }

            SetGraphObjectDirty();
        }

        /// <summary>
        /// Updates the graph model when loading the graph.
        /// </summary>
        public virtual void OnLoadGraph()
        {
            using var assetDirtyScope = BlockAssetDirtyScope();

            AddGraphPlaceholders();

            // This is necessary because we can load a graph in the tool without OnEnable(),
            // which calls OnDefineNode(), being called (yet).
            // Also, PortModel.OnAfterDeserialized(), which resets port caches, is not necessarily called,
            // since the graph may already have been loaded by the AssetDatabase a long time ago.

            // The goal of this is to create the missing ports when subgraph variables get deleted.
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var nodeModel in NodeModels.OfType<NodeModel>())
#pragma warning restore UA2001
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
            Assert.IsTrue((Object)GraphObject, "graph object is invalid");
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
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var nodesAndBlocks = NodeAndBlockModels.ToList();
#pragma warning restore UA2001
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
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    var originalDeclarations = VariableDeclarations.Where(d => d.Guid == variableNode.DeclarationModel.Guid).ToList();
#pragma warning restore UA2001
                    Assert.IsTrue(originalDeclarations.Count <= 1);
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    var originalDeclaration = originalDeclarations.SingleOrDefault();
#pragma warning restore UA2001
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

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var groupItemModel in groupModel.Items.OfType<GroupModel>())
#pragma warning restore UA2001
            {
                CheckSectionsAndGroupsRecursive(groupItemModel);
            }
        }

        /// <inheritdoc />
        public override void OnAfterDeserialize()
        {
            using var dirtyScope = BlockAssetDirtyScope();

            base.OnAfterDeserialize();

            if (m_GraphWireModels == null)
                m_GraphWireModels = new List<WireModel>();

            if (m_GraphStickyNoteModels == null)
                m_GraphStickyNoteModels = new List<StickyNoteModel>();

            if (m_GraphPlacematModels == null)
                m_GraphPlacematModels = new List<PlacematModel>();

            if (m_GraphNodeModels == null)
                m_GraphNodeModels = new List<AbstractNodeModel>();

            if (m_LocalSubgraphs == null)
                m_LocalSubgraphs = new List<GraphModel>();

            foreach (var localSubgraph in m_LocalSubgraphs)
            {
                localSubgraph.ParentGraph = this;
            }

            // Set the graph model on all elements.
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var model in m_GraphNodeModels.Where(m => m != null))
#pragma warning restore UA2001
            {
                model.GraphModel = this;
            }

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var model in m_GraphWireModels.Where(m => m != null))
#pragma warning restore UA2001
            {
                model.GraphModel = this;
            }

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var model in m_GraphStickyNoteModels.Where(m => m != null))
#pragma warning restore UA2001
            {
                model.GraphModel = this;
            }

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var model in m_GraphPlacematModels.Where(m => m != null))
#pragma warning restore UA2001
            {
                model.GraphModel = this;
            }

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var model in m_GraphVariableModels.Where(m => m != null))
#pragma warning restore UA2001
            {
                model.GraphModel = this;
            }

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var model in m_GraphPortalModels.Where(m => m != null))
#pragma warning restore UA2001
            {
                model.GraphModel = this;
            }

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var model in m_SectionModels.Where(m => m != null))
#pragma warning restore UA2001
            {
                model.GraphModel = this;
            }

            m_ExistingVariableNames = new HashSet<string>(VariableDeclarations.Count);
            foreach (var declarationModel in VariableDeclarations)
            {
                if (declarationModel != null && declarationModel is not ExternalVariableDeclarationModelBase) // in case of bad serialized graph - breaks a test if not tested
                {
                    m_ExistingVariableNames.Add(declarationModel.Title);
                }
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
        /// Cleans the stored sections based on the given section names.
        /// </summary>
        public virtual void CleanupSections()
        {
            using var dirtyScope = AssetDirtyScope();

            var sectionNames = AdditionalSectionNames;

            if (m_SectionModels == null)
            {
                m_SectionModels = new List<SectionModel>();
                SetGraphObjectDirty();
            }

            var nullSections = 0;
            for (var i = 0; i < m_SectionModels.Count; i++)
            {
                if (m_SectionModels[i] == null)
                    nullSections++;
            }

            if (nullSections > 0)
            {
                var newList = new List<SectionModel>(m_SectionModels.Count - nullSections);
                for (var i = 0; i < m_SectionModels.Count; i++)
                {
                    var sectionModel = m_SectionModels[i];
                    if (sectionModel != null)
                    {
                        newList.Add(sectionModel);
                    }
                }

                m_SectionModels = newList;
                SetGraphObjectDirty();
            }

            // For migration of old graphs, if it is not found, the first section is considered the default section and everything is added to it.
            if (m_SectionModels.Count > 0 && m_SectionModels.Find(t => t.Title == DefaultSectionName) == null)
            {
                m_SectionModels[0].Rename(DefaultSectionName);

                for (int i = 1; i < m_SectionModels.Count; ++i)
                {
                    var items = new List<IGroupItemModel>(m_SectionModels[i].Items);
                    for (int j = 0; j < items.Count; ++j)
                        m_SectionModels[0].InsertItem(items[j]);
                }

                while (m_SectionModels.Count > 1)
                {
                    m_SectionModels.RemoveAt(m_SectionModels.Count - 1); // Remove elements from the end of the list
                }

                SetGraphObjectDirty();
            }

            var sectionHash = new HashSet<string>(sectionNames);
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var section in m_SectionModels.ToList())
#pragma warning restore UA2001
            {
                if (!sectionHash.Contains(section.Title) && section.Title != DefaultSectionName)
                {
                    RemoveSection(section);
                }
            }

            var oneDefault = EnsureUniqueness(DefaultSectionName);

            if (!oneDefault)
                CreateSection(DefaultSectionName);

            foreach (var sectionName in sectionNames)
            {
                bool one = EnsureUniqueness(sectionName);
                if (!one)
                {
                    CreateSection(sectionName);
                }
            }

            return;

            bool EnsureUniqueness(string name)
            {
                SectionModel foundSection = null;

                // look for multiple Default Sections
                for (int i = 0; i < m_SectionModels.Count; ++i)
                {
                    if (m_SectionModels[i].Title == name)
                    {
                        if (foundSection == null)
                            foundSection = m_SectionModels[i];
                        else
                        {
                            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                            foreach (var item in m_SectionModels[i].Items.ToList())
#pragma warning restore UA2001
                            {
                                foundSection.InsertItem(item);
                            }

                            RemoveSection(m_SectionModels[i]);
                            --i;
                        }
                    }
                }

                return foundSection != null;
            }
        }

        /// <summary>
        /// Makes this graph a clone of <paramref name="sourceGraphModel"/>.
        /// </summary>
        /// <param name="sourceGraphModel">The source graph.</param>
        /// <param name="keepVariableDeclarationGuids">Whether duplicated variable declarations should keep the same guids as the source models'.</param>
        public virtual void CloneGraph(GraphModel sourceGraphModel, bool keepVariableDeclarationGuids = false)
        {
            ResetCaches();

            using var dirtyScope = AssetDirtyScope();

            LastKnownBounds = sourceGraphModel.LastKnownBounds;

            m_GraphNodeModels = new List<AbstractNodeModel>();
            m_GraphWireModels = new List<WireModel>();
            m_PortWireIndex = new PortWireIndex<WireModel>(m_GraphWireModels);
            m_GraphStickyNoteModels = new List<StickyNoteModel>();
            m_GraphPlacematModels = new List<PlacematModel>();
            m_GraphVariableModels = new List<VariableDeclarationModelBase>();
            m_GraphPortalModels = new List<DeclarationModel>();
            m_SectionModels = new List<SectionModel>();
            m_LocalSubgraphs = new List<GraphModel>(sourceGraphModel.m_LocalSubgraphs?.Count ?? 0);

            var elementMapping = new Dictionary<string, GraphElementModel>();
            var nodeMapping = new Dictionary<AbstractNodeModel, AbstractNodeModel>();
            var variableMapping = new Dictionary<VariableDeclarationModelBase, VariableDeclarationModelBase>();

            if (sourceGraphModel.VariableDeclarations.HasAny())
            {
                var variableDeclarationModels = sourceGraphModel.VariableDeclarations;
                for (var i = 0; i < variableDeclarationModels.Count; i++)
                {
                    var copy = DuplicateGraphVariableDeclaration(variableDeclarationModels[i], keepVariableDeclarationGuids, i);
                    variableMapping.Add(variableDeclarationModels[i], copy);
                }
            }

            foreach (var sourceNode in sourceGraphModel.NodeModels)
            {
                var pastedNode = DuplicateNode(sourceNode, Vector2.zero);
                nodeMapping[sourceNode] = pastedNode;

                if (sourceGraphModel.EntryPoint == sourceNode)
                    EntryPoint = pastedNode;


                if (sourceNode is ContextNodeModel sourceContextNode && pastedNode is ContextNodeModel pastedContextNode)
                {
                    for (var i = 0; i < sourceContextNode.BlockCount; i++)
                        nodeMapping[sourceContextNode.GetBlock(i)] = pastedContextNode.GetBlock(i);
                }
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
            }

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var sourceVariableNode in sourceGraphModel.NodeModels.Where(model => model is VariableNodeModel))
#pragma warning restore UA2001
            {
                elementMapping.TryGetValue(sourceVariableNode.Guid.ToString(), out var newNode);

                if (newNode != null)
                    ((VariableNodeModel)newNode).SetDeclarationModel(variableMapping[((VariableNodeModel)sourceVariableNode).VariableDeclarationModel]);
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
                PlacematModel.CopyPlacematParameters(placemat, pastedPlacemat);
            }

            foreach (var section in sourceGraphModel.SectionModels)
            {
                var newSection = GetSectionModel(section.Title);
                if (newSection == null)
                {
                    newSection = CreateSection(section.Title);
                }
                else
                {
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    newSection.RemoveContainerElements(newSection.Items.Cast<GraphElementModel>().ToList());
#pragma warning restore UA2001
                }

                newSection.CopyFrom(section, variableMapping);
            }

            SetGraphObjectDirty();
        }

        void AddGraphPlaceholders()
        {
            RemoveUnmanagedNullElements();

            // Get the indexes of null models (used to create placeholders for models which data was not serialized properly).
            var remainingNullModelIndexes = new List<(ManagedMissingTypeModelCategory, int)>();
            var contextWithNullBlocks = new List<ContextNodeModel>();
            for (var i = 0; i < NodeModels.Count; i++)
            {
                var node = NodeModels[i];
                if (node == null)
                {
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    var metadata = m_GraphElementMetaData.Where(m => m.Category is ManagedMissingTypeModelCategory.Node or ManagedMissingTypeModelCategory.ContextNode).FirstOrDefault(m => m.Index == i);
#pragma warning restore UA2001
                    if (metadata != null)
                        remainingNullModelIndexes.Add((metadata.Category, i));
                }
                else if (node is ContextNodeModel contextNodeModel && contextNodeModel.GetGraphElementModels().HasAny(ge => ge == null))
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
            foreach (var referenceWithMissingType in SerializationUtility.GetManagedReferencesWithMissingTypes(GraphObject))
            {
                if (YamlParsingHelper.TryParseGUID(referenceWithMissingType.serializedData, hashGuidFieldName, obsoleteGuidFieldName, 0, out var guid))
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
                        PlaceholderModelHelper.TryCreatePlaceholder(this, ManagedMissingTypeModelCategory.BlockNode, referenceWithMissingType, guid, out _);
                    }
                    else if (PlaceholderModelHelper.TryCreatePlaceholder(this, metadata.Category, referenceWithMissingType, guid, out var createdPlaceholder))
                    {
                        SaveNodePositionForMetadata(createdPlaceholder);
                        remainingNullModelIndexes.Remove((metadata.Category, metadata.Index));
                    }
                }
            }

            // Create placeholders for null models for which the data is not serialized anymore.
            foreach (var(category, index) in remainingNullModelIndexes)
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var metadata = m_GraphElementMetaData.FirstOrDefault(m => m.Category == category && m.Index == index);
#pragma warning restore UA2001
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
                                if (m_PlaceholderData.ContainsKey(metadata.Guid))
                                    CreateNodePlaceholder(PlaceholderModelHelper.missingTypeWontBeRestored, m_PlaceholderData[metadata.Guid].Position, metadata.Guid);
                                else
                                    metadata.ToRemove = true;
                                break;
                            case ManagedMissingTypeModelCategory.VariableDeclaration:
                                CreateVariableDeclarationPlaceholder(PlaceholderModelHelper.missingTypeWontBeRestored, metadata.Guid);
                                break;
                            case ManagedMissingTypeModelCategory.Wire:
                                RemoveWire(WireModels[index]); // We don't have the data for the ports.
                                break;
                            case ManagedMissingTypeModelCategory.PortalDeclaration:
                                CreatePortalDeclarationPlaceholder(PlaceholderModelHelper.missingTypeWontBeRestored, metadata.Guid);
                                break;
                            case ManagedMissingTypeModelCategory.ContextNode:
                                if (m_PlaceholderData.ContainsKey(metadata.Guid))
                                    CreateContextNodePlaceholder(PlaceholderModelHelper.missingTypeWontBeRestored, m_PlaceholderData[metadata.Guid].Position, metadata.Guid);
                                else
                                    metadata.ToRemove = true;
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
                    #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    if (block == null && context.BlockPlaceholders.All(t => t.Guid != context.BlockGuids[i]))
#pragma warning restore UA2001
                        CreateBlockNodePlaceholder(PlaceholderModelHelper.missingTypeWontBeRestored, context.BlockGuids[i], context);
                }
            }
        }

        void SaveNodePositionForMetadata(IPlaceholder createdPlaceholder)
        {
            // The node position needs to be kept in the metadata to be able to recreate the placeholder at the right position.
            if (createdPlaceholder is not NodePlaceholder nodeModel)
                return;

            if (m_PlaceholderData.TryGetValue(nodeModel.Guid, out var placeholderData))
                placeholderData.Position = nodeModel.Position;
            else
                m_PlaceholderData.Add(nodeModel.Guid, new PlaceholderData { Position = nodeModel.Position });
        }

        void RemoveUnmanagedNullElements()
        {
            var dirty = false;
            dirty |= m_GraphStickyNoteModels.RemoveAll(t => t == null) != 0;
            dirty |= m_GraphPlacematModels.RemoveAll(t => t == null) != 0;
            dirty |= m_SectionModels.RemoveAll(t => t == null) != 0;
            m_SectionModels.ForEach(t => dirty |= t.Repair());

            if (dirty)
                SetGraphObjectDirty();
        }

        void RemovePlaceholder(IPlaceholder placeholder)
        {
            if (TryGetModelFromGuid(placeholder.Guid, out var model))
            {
                UnregisterElement(model);
                CurrentGraphChangeDescription.AddDeletedModel(model);
            }

            // Clear the serialized data related to the null object the user wants to remove.
            SerializationUtility.ClearManagedReferenceWithMissingType(GraphObject, placeholder.ReferenceId);

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var metadata = m_GraphElementMetaData.FirstOrDefault(m => m.Guid == placeholder.Guid);
#pragma warning restore UA2001

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
                    SetGraphObjectDirty();
                    break;
                case ManagedMissingTypeModelCategory.VariableDeclaration:
                    m_GraphVariableModels.RemoveAt(metadata.Index);
                    SetGraphObjectDirty();
                    break;
                case ManagedMissingTypeModelCategory.Wire:
                    m_GraphWireModels.RemoveAt(metadata.Index);
                    SetGraphObjectDirty();
                    break;
                case ManagedMissingTypeModelCategory.PortalDeclaration:
                    m_GraphPortalModels.RemoveAt(metadata.Index);
                    SetGraphObjectDirty();
                    break;
            }

            // Remove the associated metadata
            RemoveFromMetadata(metadata.Index, metadata.Category);
        }

        internal NodePlaceholder CreateNodePlaceholder(string nodeName, Vector2 position, Hash128 guid, long referenceId = -1)
        {
            var node = InstantiateNode(typeof(NodePlaceholder), nodeName, position, guid) as NodePlaceholder;
            RegisterElement(node);
            CurrentGraphChangeDescription.AddNewModel(node);

            if (node != null && referenceId != -1)
                node.ReferenceId = referenceId;

            m_Placeholders.Add(node);

            return node;
        }

        internal ContextNodePlaceholder CreateContextNodePlaceholder(string nodeName, Vector2 position, Hash128 guid, IEnumerable<BlockNodeModel> blocks = null, long referenceId = -1)
        {
            var contextNode = InstantiateNode(typeof(ContextNodePlaceholder), nodeName, position, guid) as ContextNodePlaceholder;
            RegisterElement(contextNode);
            CurrentGraphChangeDescription.AddNewModel(contextNode);

            if (contextNode != null && referenceId != -1)
                contextNode.ReferenceId = referenceId;

            m_Placeholders.Add(contextNode);

            if (contextNode is ContextNodeModel contextNodeModel)
            {
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var blockGuids = blocks?.Select(b => b.Guid).ToList();
#pragma warning restore UA2001
                if (blockGuids == null)
                {
                    if (m_PlaceholderData.TryGetValue(guid, out var data))
                        blockGuids = data.BlockGuids;
                }
                else
                {
                    // Keep the blocks' guids to be able to recreate the placeholder in case the missing type serialized data is lost.
                    m_PlaceholderData ??= new SerializedValueDictionary<Hash128, PlaceholderData>();
                    if (m_PlaceholderData.TryGetValue(guid, out var placeholderData))
                        placeholderData.BlockGuids = blockGuids;
                    else
                        m_PlaceholderData.Add(guid, new PlaceholderData { BlockGuids = blockGuids });
                }

                if (blockGuids != null)
                {
                    foreach (var blockGuid in blockGuids)
                        CreateBlockNodePlaceholder("! Missing ! The context node has a missing type.", blockGuid, contextNodeModel);
                }
            }

            return contextNode;
        }

        internal BlockNodePlaceholder CreateBlockNodePlaceholder(string nodeName, Hash128 guid, ContextNodeModel contextNodeModel, long referenceId = -1)
        {
            var node = InstantiateNode(typeof(BlockNodePlaceholder), nodeName, Vector2.zero, guid) as BlockNodePlaceholder;
            RegisterElement(node);
            CurrentGraphChangeDescription.AddNewModel(node);

            if (node != null && referenceId != -1)
                node.ReferenceId = referenceId;

            if (node is BlockNodeModel blockNodeModel && contextNodeModel != null)
                contextNodeModel.InsertBlock(blockNodeModel, spawnFlags: SpawnFlags.Orphan);

            return node;
        }

        internal VariableDeclarationPlaceholder CreateVariableDeclarationPlaceholder(string variableName, Hash128 guid, long referenceId = -1)
        {
            var variableDeclaration = InstantiateVariableDeclarationPlaceholder(TypeHandle.MissingType, variableName, guid);

            RegisterElement(variableDeclaration);
            CurrentGraphChangeDescription.AddNewModel(variableDeclaration);

            if (variableDeclaration != null && referenceId != -1)
                variableDeclaration.ReferenceId = referenceId;

            var group = m_PlaceholderData.TryGetValue(guid, out var data) ? GetSectionModel(data.GroupTitle) : GetSectionModel(GetVariableSection(variableDeclaration));
            group.InsertItem(variableDeclaration);

            m_Placeholders.Add(variableDeclaration);

            return variableDeclaration;
        }

        internal PortalDeclarationPlaceholder CreatePortalDeclarationPlaceholder(string portalName, Hash128 guid, long referenceId = -1)
        {
            var portalModel = ModelHelpers.Instantiate<PortalDeclarationPlaceholder>(typeof(PortalDeclarationPlaceholder));
            portalModel.Title = portalName;

            if (guid.isValid)
                portalModel.SetGuid(guid);

            portalModel.GraphModel = this;

            RegisterElement(portalModel);
            CurrentGraphChangeDescription.AddNewModel(portalModel);

            if (referenceId != -1)
                portalModel.ReferenceId = referenceId;

            m_Placeholders.Add(portalModel);

            return portalModel;
        }

        internal WirePlaceholder CreateWirePlaceholder(PortModel toPort, PortModel fromPort, Hash128 guid, long referenceId = -1)
        {
            var existing = GetAnyWireConnectedToPorts(toPort, fromPort);
            if (existing != null)
                return existing as WirePlaceholder;

            var wireModel = ModelHelpers.Instantiate<WirePlaceholder>(typeof(WirePlaceholder));
            wireModel.GraphModel = this;

            if (guid.isValid)
                wireModel.SetGuid(guid);

            wireModel.SetPorts(toPort, fromPort);

            RegisterElement(wireModel);
            CurrentGraphChangeDescription.AddNewModel(wireModel);

            if (referenceId != -1)
                wireModel.ReferenceId = referenceId;

            m_Placeholders.Add(wireModel);
            m_PortWireIndex?.WireAdded(wireModel);

            return wireModel;
        }

        void InsertNullReferencesWhileHasMissingTypes(ManagedMissingTypeModelCategory category, int index)
        {
            if (!SerializationUtility.HasManagedReferencesWithMissingTypes(GraphObject))
                return;

            // While there are references with missing types, null should be added whenever an element is removed from a list to keep its topology.
            // This is needed because the missing type registry will keep information about instances and where they were referenced from on load.
            // If the property path from which they were referenced from is either no longer containing a null or no longer available and the instance is no longer referenced from anywhere else it will be pruned out.
            switch (category)
            {
                case ManagedMissingTypeModelCategory.Node:
                case ManagedMissingTypeModelCategory.ContextNode:
                    m_GraphNodeModels.Insert(index, null);
                    break;
                case ManagedMissingTypeModelCategory.VariableDeclaration:
                    m_GraphVariableModels.Insert(index, null);
                    break;
                case ManagedMissingTypeModelCategory.Wire:
                    m_GraphWireModels.Insert(index, null);
                    break;
                case ManagedMissingTypeModelCategory.PortalDeclaration:
                    m_GraphPortalModels.Insert(index, null);
                    break;
            }
        }

        void RemoveNullReferencesAddedWhileHasMissingTypes()
        {
            // Null references were added to keep the graph element lists' topology while there were missing types.
            // They need to be removed when the missing types were resolved.
            if (SerializationUtility.HasManagedReferencesWithMissingTypes(GraphObject))
                return;

            using var dirtyScope = AssetDirtyScope();

            RemoveNullReferences(ManagedMissingTypeModelCategory.Node, m_GraphNodeModels);
            RemoveNullReferences(ManagedMissingTypeModelCategory.ContextNode, m_GraphNodeModels);
            RemoveNullReferences(ManagedMissingTypeModelCategory.VariableDeclaration, m_GraphVariableModels);
            RemoveNullReferences(ManagedMissingTypeModelCategory.Wire, m_GraphWireModels);
            RemoveNullReferences(ManagedMissingTypeModelCategory.PortalDeclaration, m_GraphPortalModels);
            return;

            void RemoveNullReferences(ManagedMissingTypeModelCategory category, IReadOnlyList<GraphElementModel> graphElementModels)
            {
                for (var i = graphElementModels.Count - 1; i >= 0; i--)
                {
                    var wireModel = graphElementModels[i];
                    if (wireModel is null)
                    {
                        #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                        var metadata = m_GraphElementMetaData.FirstOrDefault(m => m.Category == category && m.Index == i);
#pragma warning restore UA2001
                        if (metadata != null)
                            RemoveMetaDataModel(metadata);
                    }
                }
            }
        }

        /// <summary>
        /// "If this GraphModel is a subgraph, any subgraph nodes that reference it in the parent graph must redefine its ports whenever an input or output variable declaration is added."
        /// </summary>
        private protected virtual void RedefineSubgraphNodeModels()
        {
            if (ParentGraph == null)
                return;

            foreach (var node in ParentGraph.NodeModels)
            {
                if (node is not SubgraphNodeModel subgraphNodeModel)
                    continue;

                if (subgraphNodeModel.GetSubgraphModel() == this)
                {
                    ParentGraph.RecurseDefineNode(node);
                }
            }
        }

        /// <inheritdoc />
        public virtual bool Repair()
        {
            var dirty = false;

            dirty |= m_GraphNodeModels.RemoveAll(t => t is null or IPlaceholder) != 0;
            dirty |= m_GraphNodeModels.RemoveAll(t => t is VariableNodeModel variable && variable.DeclarationModel == null) != 0;

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var container in m_GraphNodeModels.OfType<IGraphElementContainer>())
#pragma warning restore UA2001
            {
                dirty |= container.Repair();
            }

            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var validGuids = new HashSet<Hash128>(m_GraphNodeModels.Select(t => t.Guid));
#pragma warning restore UA2001

           dirty |= m_GraphWireModels.RemoveAll(t => t is null or IPlaceholder) != 0;
           dirty |= m_GraphWireModels.RemoveAll(t => !validGuids.Contains(t.FromNodeGuid) || !validGuids.Contains(t.ToNodeGuid)) != 0;
           dirty |= m_GraphStickyNoteModels.RemoveAll(t => t == null) != 0;
           dirty |= m_GraphPlacematModels.RemoveAll(t => t == null) != 0;
           dirty |= m_GraphVariableModels.RemoveAll(t => t is null) != 0;
           dirty |= m_GraphPortalModels.RemoveAll(t => t is null) != 0;
            m_SectionModels.ForEach(t => dirty |= t.Repair());

            return dirty;
        }

        /// <summary>
        /// Checks whether the graph is a Container Graph or not. If it is not a Container Graph, it is an Asset Graph.
        /// </summary>
        /// <remarks>
        /// A Container Graph is a graph that cannot be nested inside another graph, and can be referenced by a game object or scene.
        /// An Asset Graph is a graph that can have exposed inputs/outputs, making it so that it can be nested inside another graph, and can be referenced by a game object or scene.
        /// </remarks>
        /// <returns>True if the graph is a container graph, false otherwise.</returns>
        public virtual bool IsContainerGraph() => false;

        /// <summary>
        /// Checks the conditions to specify whether the Asset Graph can be a subgraph or not.
        /// </summary>
        /// <remarks>
        /// A subgraph is an Asset Graph that is nested inside another graph, and can be referenced by a game object or scene.
        /// </remarks>
        /// <returns>True if the Asset Graph can be a subgraph, false otherwise.</returns>
        public virtual bool CanBeSubgraph() => !IsContainerGraph();

        /// <summary>
        /// Checks the conditions to specify whether the graph can be dropped in another graph.
        /// </summary>
        public virtual bool CanBeDroppedInOtherGraph(GraphModel otherGraph)
        {
            return GetType().Name == otherGraph.GetType().Name;
        }

        /// <summary>
        /// The list of subgraph templates that this graph supports.
        /// </summary>
        public virtual List<GraphTemplate> SubgraphTemplates { get; }

        /// <summary>
        /// Creates a subgraph node from a selection of graph elements.
        /// </summary>
        /// <param name="subgraphModel">An empty graph model that will be used as the subgraph.</param>
        /// <param name="selection">The selection of graph elements to add to the subgraph.</param>
        /// <param name="nodePosition">The desired position of the subgraph node.</param>
        /// <param name="nodeGuid">The desired guid of the subgraph node.</param>
        /// <param name="elementsToDelete">Additional graph elements to delete, if any.</param>
        /// <param name="portIdsToAlign">Ids of ports on the subgraph node that need their connections to be aligned.</param>
        public virtual SubgraphNodeModel CreateSubgraphNodeFromSelection(GraphModel subgraphModel, List<GraphElementModel> selection, Vector2 nodePosition, Hash128 nodeGuid, List<GraphElementModel> elementsToDelete = null, List<string> portIdsToAlign = null)
        {
            if (!AllowSubgraphCreation)
            {
                throw new InvalidOperationException("Subgraph creation is disabled.");
            }

            using var dirtyScope = AssetDirtyScope();

            // Create an empty subgraph node.
            var subgraphNodeModel = CreateSubgraphNode(subgraphModel, nodePosition, nodeGuid);

            // Populate the subgraph and create the connections to the subgraph node in the main graph.
            CreateSubgraphCreationHelper().HandleSubgraphNodeCreation(subgraphNodeModel, selection, elementsToDelete, portIdsToAlign);

            return subgraphNodeModel;
        }

        /// <summary>
        /// Retrieves a graph by its guid. The graphs searched are the current graph plus the local subgraphs of the current graph.
        /// </summary>
        /// <param name="guid">The guid of the graph to retrieve.</param>
        /// <param name="recursive">Should the search be recursive in the local subgraphs of the current graph.</param>
        /// <returns>The graph model with the given guid, or null if not found.</returns>
        public virtual GraphModel GetGraphModelByGuid(Hash128 guid, bool recursive = true)
        {
            if (Guid == guid)
                return this;

            if (m_LocalSubgraphs != null)
            {
                foreach (var subgraphModel in m_LocalSubgraphs)
                {
                    if (subgraphModel.Guid == guid)
                        return subgraphModel;
                }

                if (recursive)
                {
                    foreach (var subgraphModel in m_LocalSubgraphs)
                    {
                        var foundSubgraph = subgraphModel.GetGraphModelByGuid(guid);
                        if (foundSubgraph != null)
                            return foundSubgraph;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets all the nested sub graphs assets contained in the graph.
        /// </summary>
        /// <returns>A list of <see cref="GraphToolkit.Editor.GraphObject"/> nested in this subgraph.</returns>
        public List<GraphObject> GetNestedSubgraphAssetsRecursive()
        {
            // Don't include this graph.
            var visitedAssets = new HashSet<Hash128> { Guid };
            var nestedSubgraphAssets = new List<GraphObject>();
            GetNestedSubgraphAssetsRecursive(nestedSubgraphAssets, visitedAssets);
            return nestedSubgraphAssets;
        }

        /// <summary>
        /// Gets all the nested sub graphs contained in the graph.
        /// </summary>
        /// <param name="subgraphAssets">The nested subgraph assets.</param>
        /// <param name="alreadyVisitedAssets">The already visited assets.</param>
        protected virtual void GetNestedSubgraphAssetsRecursive(List<GraphObject> subgraphAssets, HashSet<Hash128> alreadyVisitedAssets)
        {
            for (var i = 0; i < NodeModels.Count; i++)
            {
                if (NodeModels[i] is not SubgraphNodeModel subgraphNodeModel || subgraphNodeModel.IsReferencingLocalSubgraph)
                    continue;

                var otherSubgraphModel = subgraphNodeModel.GetSubgraphModel();
                if (otherSubgraphModel is null)
                    continue;

                if (!alreadyVisitedAssets.Add(otherSubgraphModel.Guid))
                {
                    continue;
                }

                subgraphAssets.Add(otherSubgraphModel.GraphObject);

                otherSubgraphModel.GetNestedSubgraphAssetsRecursive(subgraphAssets, alreadyVisitedAssets);
            }
        }

        internal GraphChangeDescription PushNewGraphChangeDescription()
        {
            var changes = new GraphChangeDescription();
            m_GraphChangeDescriptionStack.Push(changes);
            return changes;
        }

        internal void PopGraphChangeDescription()
        {
            if (!m_GraphChangeDescriptionStack.TryPop(out var currentScopeChange))
                return;
            if (!m_GraphChangeDescriptionStack.TryPeek(out var outerScopeChanges))
                return;
            outerScopeChanges.Union(currentScopeChange);
        }

        /// <inheritdoc />
        public virtual void CloneAssets(List<Object> clones, Dictionary<Object, Object> originalToCloneMap)
        {
            foreach (var node in NodeAndBlockModels)
            {
                if (node is IObjectClonedCallbackReceiver assetClonedCallbackReceiver)
                {
                    assetClonedCallbackReceiver.CloneAssets(clones, originalToCloneMap);
                }
            }

            foreach (var variableDeclaration in VariableDeclarations)
            {
                if (variableDeclaration is IObjectClonedCallbackReceiver assetClonedCallbackReceiver)
                {
                    assetClonedCallbackReceiver.CloneAssets(clones, originalToCloneMap);
                }
            }
        }

        /// <inheritdoc />
        public virtual void OnAfterAssetClone(IReadOnlyDictionary<Object, Object> originalToCloneMap)
        {
            if (GraphObject != null && originalToCloneMap.TryGetValue(GraphObject, out var clonedAsset))
            {
                GraphObject = clonedAsset as GraphObject;
            }

            foreach (var node in NodeAndBlockModels)
            {
                if (node is IObjectClonedCallbackReceiver assetClonedCallbackReceiver)
                {
                    assetClonedCallbackReceiver.OnAfterAssetClone(originalToCloneMap);
                }
            }

            foreach (var variableDeclaration in VariableDeclarations)
            {
                if (variableDeclaration is IObjectClonedCallbackReceiver assetClonedCallbackReceiver)
                {
                    assetClonedCallbackReceiver.OnAfterAssetClone(originalToCloneMap);
                }
            }
        }

        /// <inheritdoc />
        public virtual void OnBeforeCopy()
        {
            foreach (var node in NodeAndBlockModels)
            {
                if (node is ICopyPasteCallbackReceiver copyPasteCallbackReceiver)
                {
                    copyPasteCallbackReceiver.OnBeforeCopy();
                }
            }
        }

        /// <inheritdoc />
        public virtual void OnAfterCopy()
        {
            foreach (var node in NodeAndBlockModels)
            {
                if (node is ICopyPasteCallbackReceiver copyPasteCallbackReceiver)
                {
                    copyPasteCallbackReceiver.OnAfterCopy();
                }
            }
        }

        /// <inheritdoc />
        public virtual void OnAfterPaste()
        {
            foreach (var node in NodeAndBlockModels)
            {
                if (node is ICopyPasteCallbackReceiver copyPasteCallbackReceiver)
                {
                    copyPasteCallbackReceiver.OnAfterPaste();
                }
            }
        }

        /// <summary>
        /// Creates an <see cref="InspectorModel"/>
        /// </summary>
        /// <param name="inspectedModels">The models that are inspected.</param>
        /// <returns>The new inspector model.</returns>
        public virtual InspectorModel CreateInspectorModel(IEnumerable<Model> inspectedModels)
        {
            // PF: This should probably live in ModelInspectorViewModel
            return new InspectorModel { InspectedModels = inspectedModels };
        }

        /// <summary>
        /// Gets the models passed as parameters that can be displayed in the inspector.
        /// </summary>
        /// <param name="models">The models.</param>
        /// <returns>The models passed as parameters that can be displayed in the inspector.</returns>
        public virtual IEnumerable<GraphElementModel> GetModelsDisplayableInInspector(IEnumerable<GraphElementModel> models)
        {
            // PF: This should probably live in ModelInspectorViewModel
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return models.Where(t => t is AbstractNodeModel or VariableDeclarationModelBase or PlacematModel or WireModel or GroupModel or StickyNoteModel);
#pragma warning restore UA2001
        }

        /// <summary>
        /// Indicates whether a given port can be expanded and have sub ports.
        /// </summary>
        /// <param name="port">The port.</param>
        /// <returns>Whether a given port can be expanded and have sub ports.</returns>
        public virtual bool CanExpandPort(PortModel port)
        {
            return false;
        }

        /// <summary>
        /// Defines the sub ports of a given port, if <see cref="CanExpandPort"/> returns true.
        /// </summary>
        /// <param name="subPortsDefinition">The scope used to add sub ports.</param>
        /// <param name="port">The port.</param>
        public virtual void OnDefineSubPorts(ISubPortDefinition subPortsDefinition, PortModel port)
        {
            if (port.NodeModel is not ConstantNodeModel constantNode
                || port.ParentPort != null)
            {
                return;
            }

            constantNode.OnDefineSubPorts(subPortsDefinition, port);
        }
        /// <summary>
        /// Resolves a <see cref="GraphModel"/> from a <see cref="GraphReference"/>. Relative to this graph.
        /// </summary>
        /// <param name="reference">The <see cref="GraphReference"/></param>
        /// <returns>The resolved <see cref="GraphModel"/>.</returns>
        public virtual GraphModel ResolveGraphModelFromReference(in GraphReference reference)
        {
            if ( ! reference.HasAssetReference)
                return GetGraphModelByGuid(reference.GraphModelGuid);

            return GraphReference.ResolveGraphModel(reference);
        }

        /// <summary>
        /// Returns a <see cref="GraphReference"/> to the graphModel given, relative to this graph if <paramref name="allowLocalReference"/> is true.
        /// </summary>
        /// <param name="graphModel">The graphModel.</param>
        /// <param name="allowLocalReference">If true, a local <see cref="GraphReference"/> will be returned, if possible.</param>
        /// <returns>A <see cref="GraphReference"/> to the graphModel given, relative to this graph if <paramref name="allowLocalReference"/> is true. </returns>
        public virtual GraphReference GetGraphModelReference(GraphModel graphModel, bool allowLocalReference)
        {
            if (graphModel.GraphObject == GraphObject && allowLocalReference)
            {
                return new GraphReference(graphModel.Guid, default, EntityId.None);
            }

            return new GraphReference(graphModel);
        }

        /// <summary>
        /// Retrieves the style (icon and color) associated with a data type in this graph model.
        /// </summary>
        /// <param name="dataType">The data type.</param>
        /// <returns>The icon and color associated with the provided data type. Null if not available.</returns>
        public virtual (Texture2D icon, Color color)? GetDataTypeStyle(Type dataType)
        {
            return BaseDataTypeStyleMapper.GetDataTypeStyle(dataType, GetType());
        }

        public class TestAccess
        {
            readonly GraphModel m_SubgraphModel;
            public TestAccess(GraphModel subgraphModel)
            {
                m_SubgraphModel = subgraphModel;
            }

            public void RedefineSubgraphNodeModel() => m_SubgraphModel.RedefineSubgraphNodeModels();
            public void RecurseDefineNode(AbstractNodeModel nodeModel) => m_SubgraphModel.RecurseDefineNode(nodeModel);
        }
    }
}
