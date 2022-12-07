// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.ItemLibrary.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// UI side base implementation for the Stencil, which represents the capabilities of a graph and its UI.
    /// </summary>
    abstract class Stencil : StencilBase
    {
        static readonly IReadOnlyDictionary<string, string> k_NoCategoryStyle = new Dictionary<string, string>();

        protected IItemDatabaseProvider m_DatabaseProvider;

        GraphProcessorContainer m_GraphProcessorContainer;

        protected virtual IReadOnlyDictionary<string, string> CategoryPathStyleNames => k_NoCategoryStyle;

        /// <summary>
        /// Extra stylesheet(s) to load when displaying the item library.
        /// </summary>
        /// <remarks>(FILENAME)_dark.uss and (FILENAME)_light.uss will be loaded as well if existing.</remarks>
        protected virtual string CustomItemLibraryStylesheetPath => null;

        /// <summary>
        /// The list of valid section names for the graph.
        /// </summary>
        public override IEnumerable<string> SectionNames { get; } = new List<string> { "Graph Variables" };

        public override BlackboardGraphModel CreateBlackboardGraphModel(GraphModel graphModel)
        {
            return new BlackboardGraphModel(){GraphModel = graphModel};
        }

        /// <inheritdoc />
        public override void OnGraphModelEnabled()
        {
            base.OnGraphModelEnabled();
            GetGraphProcessorContainer().OnGraphModelEnabled();
        }

        /// <inheritdoc />
        public override void OnGraphModelDisabled()
        {
            GetGraphProcessorContainer().OnGraphModelDisabled();
            base.OnGraphModelDisabled();
        }

        public override InspectorModel CreateInspectorModel(IEnumerable<Model> inspectedModels)
        {
            return new InspectorModel(){InspectedModels = inspectedModels};
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

        protected virtual void CreateGraphProcessors()
        {
            if (AllowMultipleDataOutputInstances == AllowMultipleDataOutputInstances.AllowWithWarning)
                GetGraphProcessorContainer().AddGraphProcessor(new VariableNodeGraphProcessor_Internal());
        }

        [CanBeNull]
        public virtual ILibraryFilterProvider GetLibraryFilterProvider()
        {
            return null;
        }

        /// <summary>
        /// Gets the <see cref="ItemLibraryAdapter"/> used to search for elements.
        /// </summary>
        /// <param name="graphModel">The graph where to search for elements.</param>
        /// <param name="title">The title to display when searching.</param>
        /// <param name="toolName">The name of the tool requesting the item library, for display purposes.</param>
        /// <param name="contextPortModel">The ports used for the search, if any.</param>
        /// <returns></returns>
        [CanBeNull]
        public virtual IItemLibraryAdapter GetItemLibraryAdapter(GraphModel graphModel, string title, string toolName, IEnumerable<PortModel> contextPortModel = null)
        {
            var adapter = new GraphNodeLibraryAdapter(graphModel, title, toolName);
            adapter.CategoryPathStyleNames = CategoryPathStyleNames;
            adapter.CustomStyleSheetPath = CustomItemLibraryStylesheetPath;
            return adapter;
        }

        public virtual IItemDatabaseProvider GetItemDatabaseProvider()
        {
            return m_DatabaseProvider ??= new DefaultDatabaseProvider(this);
        }

        public virtual void OnGraphProcessingStarted(GraphModel graphModel) {}
        public virtual void OnGraphProcessingSucceeded(GraphModel graphModel, GraphProcessingResult results) {}
        public virtual void OnGraphProcessingFailed(GraphModel graphModel, GraphProcessingResult results) {}

        // PF: To preference
        public virtual bool MoveNodeDependenciesByDefault => false;

        /// <summary>
        /// Prompt the Item Library to create nodes to connect
        /// </summary>
        /// <param name="view">The view.</param>
        /// <param name="wires">The wires to connect.</param>
        /// <param name="worldPosition">The position on which the nodes will be created.</param>
        /// <param name="wiresToDelete">The wires that need to be deleted, if any.</param>
        public void CreateNodesFromWires(GraphView view, IEnumerable<(WireModel model, WireSide side)> wires, Vector2 worldPosition, IEnumerable<WireModel> wiresToDelete = null)
        {
            var localPosition = view.ContentViewContainer.WorldToLocal(worldPosition);
            Action<ItemLibraryItem> createNode = item =>
            {
                if (item is GraphNodeModelLibraryItem nodeItem)
                    view.Dispatch(CreateNodeCommand.OnWireSide(nodeItem, wires, localPosition));

                var allWiresToDelete = wires.Where(w => w.model is IGhostWire).Select(w => w.model).ToList();
                if (wiresToDelete != null)
                    allWiresToDelete.AddRange(wiresToDelete);

                if (allWiresToDelete.Any())
                {
                    foreach (var modelView in allWiresToDelete.Select(wireModel => wireModel.GetView_Internal(view)))
                    {
                        if (modelView != null && modelView is GraphElement element)
                            view.RemoveElement(element);
                    }
                }
            };
            var portModels = wires.Select(e => e.model.GetOtherPort(e.side)).ToList();
            switch (portModels.First().Direction)
            {
                case PortDirection.Output:
                    ItemLibraryService.ShowOutputToGraphNodes(this, view, portModels, worldPosition, createNode);
                    break;

                case PortDirection.Input:
                    ItemLibraryService.ShowInputToGraphNodes(this, view, portModels, worldPosition, createNode);
                    break;
            }
        }

        public virtual void PreProcessGraph(GraphModel graphModel)
        {
        }

        public virtual void OnInspectorGUI()
        {}

        /// <summary>
        /// Converts a <see cref="GraphProcessingError"/> to a <see cref="GraphProcessingErrorModel"/>.
        /// </summary>
        /// <param name="error">The error to convert.</param>
        /// <returns>The converted error.</returns>
        public virtual GraphProcessingErrorModel CreateProcessingErrorModel(GraphProcessingError error)
        {
            if (error.SourceNode != null && !error.SourceNode.Destroyed)
                return new GraphProcessingErrorModel(error);

            return null;
        }

        public class MenuItem
        {
            public string name;
            public Action action;
        }

        /// <summary>
        /// Populates the given <paramref name="menuItems"/> given a section, to create variable declaration models for a blackboard.
        /// </summary>
        /// <param name="sectionName">The name of the section in which the menu is added.</param>
        /// <param name="menuItems">An array of <see cref="MenuItem"/> to fill.</param>
        /// <param name="view">The view.</param>
        /// <param name="selectedGroup">The currently selected group model.</param>
        public virtual void PopulateBlackboardCreateMenu(string sectionName, List<MenuItem> menuItems, RootView view, GroupModel selectedGroup = null)
        {
            menuItems.Add(new MenuItem{name ="Create Variable",action = () =>
            {
                view.Dispatch(new CreateGraphVariableDeclarationCommand("variable", true, TypeHandle.Float, selectedGroup ?? GraphModel.GetSectionModel(sectionName)));
            }});
        }

        /// <inheritdoc />
        public override Type GetConstantType(TypeHandle typeHandle)
        {
            return TypeToConstantMapper.GetConstantType(typeHandle);
        }
    }
}
