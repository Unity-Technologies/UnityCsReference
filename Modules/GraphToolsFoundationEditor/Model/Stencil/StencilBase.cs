// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Base class for the Stencil, which represents the capabilities of a graph.
    /// </summary>
    abstract class StencilBase
    {
        TypeMetadataResolver m_TypeMetadataResolver;

        /// <summary>
        /// The graph model to which this stencil is associated.
        /// </summary>
        public GraphModel GraphModel { get; }

        /// <summary>
        /// The metadata resolver for the graph.
        /// </summary>
        public virtual TypeMetadataResolver TypeMetadataResolver => m_TypeMetadataResolver ??= new TypeMetadataResolver();

        /// <summary>
        /// Whether it is allowed to have multiple instances of a data output variable.
        /// </summary>
        /// <returns>The answer to whether it is allowed to have multiple instances of a data output variable.</returns>
        public virtual AllowMultipleDataOutputInstances AllowMultipleDataOutputInstances => AllowMultipleDataOutputInstances.AllowWithWarning;

        /// <summary>
        /// Initializes a new instance of the <see cref="StencilBase"/> class.
        /// </summary>
        protected StencilBase(GraphModel graphModel)
        {
            GraphModel = graphModel;
        }

        /// <summary>
        /// Performs tasks that need to be done when the <see cref="GraphModel"/> is enabled.
        /// </summary>
        public virtual void OnGraphModelEnabled() { }

        /// <summary>
        /// Performs tasks that need to be done when the <see cref="GraphModel"/> is disabled.
        /// </summary>
        public virtual void OnGraphModelDisabled() { }

        /// <summary>
        /// Indicates whether a <see cref="VariableDeclarationModel"/> requires initialization.
        /// </summary>
        /// <param name="decl">The variable declaration model to query.</param>
        /// <returns>True if the variable declaration model requires initialization, false otherwise.</returns>
        public virtual bool RequiresInitialization(VariableDeclarationModel decl) => decl.RequiresInitialization();

        /// <summary>
        /// Gets the type for subgraph nodes.
        /// </summary>
        /// <returns>The type associated with subgraph nodes</returns>
        public virtual TypeHandle GetSubgraphNodeTypeHandle()
        {
            return TypeHandle.Subgraph;
        }

        /// <summary>
        /// Create a constant of the type represented by <paramref name="constantTypeHandle"/>
        /// </summary>
        /// <param name="constantTypeHandle">The type of the constant that will be created.</param>
        /// <returns>A new constant.</returns>
        public virtual Constant CreateConstantValue(TypeHandle constantTypeHandle)
        {
            var constantType = GetConstantType(constantTypeHandle);
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
            return null;
        }

        /// <summary>
        /// Get the entry points of the associated <see cref="GraphModel"/>.
        /// </summary>
        /// <returns>The entry points of the associated <see cref="GraphModel"/>.</returns>
        public virtual IEnumerable<AbstractNodeModel> GetEntryPoints()
        {
            return Enumerable.Empty<AbstractNodeModel>();
        }

        /// <summary>
        /// Creates a <see cref="LinkedNodesDependency"/> between the two nodes connected by the given <paramref name="wireModel"/>.
        /// </summary>
        /// <param name="wireModel">The wire model to create a dependency from.</param>
        /// <param name="linkedNodesDependency">The resulting dependency.</param>
        /// <param name="parentNodeModel">The node model considered as parent in the dependency.</param>
        /// <returns>True is a dependency was created, false otherwise.</returns>
        public virtual bool CreateDependencyFromWire(WireModel wireModel, out LinkedNodesDependency linkedNodesDependency,
            out AbstractNodeModel parentNodeModel)
        {
            linkedNodesDependency = new LinkedNodesDependency
            {
                DependentPort = wireModel.FromPort,
                ParentPort = wireModel.ToPort,
            };
            parentNodeModel = wireModel.ToPort.NodeModel;

            return true;
        }

        /// <summary>
        /// Retrieves the portals models dependant on the given <paramref name="portalModel"/> (if any).
        /// </summary>
        /// <remarks>
        /// <p>In a pull model, an exit portal's dependency are the entry portals linked to it and entry portals have no dependencies.</p>
        /// <p>In a push model, an entry portal's dependency are the exit portals linked to it and exit portals have no dependencies.</p>
        /// </remarks>
        /// <param name="portalModel">The portal to retrieve the dependent portals from.</param>
        /// <returns>The portals dependant on the given one.</returns>
        public virtual IEnumerable<WirePortalModel> GetPortalDependencies(WirePortalModel portalModel)
        {
            if (portalModel is ISingleInputPortNodeModel)
            {
                return portalModel.GraphModel.FindReferencesInGraph<WirePortalModel>(portalModel.DeclarationModel).Where(n => n is ISingleOutputPortNodeModel);
            }

            return Enumerable.Empty<WirePortalModel>();
        }

        /// <summary>
        /// Retrieves all the portals linked to the given <paramref name="portalModel"/> (if any).
        /// </summary>
        /// <remarks>
        /// <p>For an entry portal, all the linked exit portals are returned.</p>
        /// <p>For an exit portal, all the linked entry portals are returned.</p>
        /// </remarks>
        /// <param name="portalModel">The portal to retrieve the linked portals from.</param>
        /// <returns>The portals linked to the given one.</returns>
        public virtual IEnumerable<WirePortalModel> GetLinkedPortals(WirePortalModel portalModel)
        {
            if (portalModel != null)
            {
                return portalModel.GraphModel.FindReferencesInGraph<WirePortalModel>(portalModel.DeclarationModel);
            }

            return Enumerable.Empty<WirePortalModel>();
        }

        /// <summary>
        /// Indicates whether a variable is allowed in the graph or not.
        /// </summary>
        /// <param name="variable">The variable in the graph.</param>
        /// <param name="graphModel">The graph of the variable.</param>
        /// <returns><c>true</c> if the variable is allowed, <c>false</c> otherwise.</returns>
        public virtual bool CanCreateVariableNode(VariableDeclarationModel variable, GraphModel graphModel)
        {
            var allowMultipleDataOutputInstances = AllowMultipleDataOutputInstances != AllowMultipleDataOutputInstances.Disallow;
            return allowMultipleDataOutputInstances
                || variable.Modifiers != ModifierFlags.Write
                || variable.IsInputOrOutputTrigger()
                || !graphModel.FindReferencesInGraph<VariableNodeModel>(variable).Any();
        }

        /// <summary>
        /// Indicates if a node can be pasted or duplicated.
        /// </summary>
        /// <param name="originalModel">The node model to copy.</param>
        /// <param name="graph">The graph in which the action takes place.</param>
        /// <returns>True if the node can be pasted or duplicated.</returns>
        public abstract bool CanPasteNode(AbstractNodeModel originalModel, GraphModel graph);

        /// <summary>
        /// Indicates if a variable declaration can be pasted or duplicated.
        /// </summary>
        /// <param name="originalModel">The variable declaration model to copy.</param>
        /// <param name="graph">The graph in which the action takes place.</param>
        /// <returns>True if the variable declaration can be pasted or duplicated.</returns>
        public abstract bool CanPasteVariable(VariableDeclarationModel originalModel, GraphModel graph);

        /// <summary>
        /// Creates a <see cref="BlackboardGraphModel"/> for the <paramref name="graphModel"/>.
        /// </summary>
        /// <param name="graphModel">The graph to wrap in a <see cref="BlackboardGraphModel"/>.</param>
        /// <returns>A new <see cref="BlackboardGraphModel"/></returns>
        public abstract BlackboardGraphModel CreateBlackboardGraphModel(GraphModel graphModel);

        /// <summary>
        /// The list of valid section names for the graph.
        /// </summary>
        public abstract IEnumerable<string> SectionNames { get; }

        /// <summary>
        /// Returns a valid section for a given variable. Default is to return the first section.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <returns>A valid section for a given variable. Default is to return the first section.</returns>
        public virtual string GetVariableSection(VariableDeclarationModel variable)
        {
            return SectionNames.First();
        }

        /// <summary>
        /// Returns whether a given variable can be converted to go in the section named sectionName.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <param name="sectionName">The target section.</param>
        /// <returns>Whether a given variable can be converted to go in the section named sectionName.</returns>
        public virtual bool CanConvertVariable(VariableDeclarationModel variable, string sectionName)
        {
            return false;
        }

        /// <summary>
        /// Convert a variable to go to the section named sectionName. Either a new variable or the same variable can be returned.
        /// </summary>
        /// <param name="variable">The variable.</param>
        /// <param name="sectionName">The target section.</param>
        /// <returns>The converted variable.</returns>
        public virtual VariableDeclarationModel ConvertVariable(VariableDeclarationModel variable, string sectionName)
        {
            return null;
        }

        /// <summary>
        /// Creates a <see cref="InspectorModel"/> to inspect <see cref="inspectedModels"/>.
        /// </summary>
        /// <param name="inspectedModels">The models to inspect.</param>
        /// <returns>A view model for the inspector.</returns>
        public abstract InspectorModel CreateInspectorModel(IEnumerable<Model> inspectedModels);

        /// <summary>
        /// Returns whether a given type handle can be assigned to another type handle, in the context of the stencil.
        /// </summary>
        /// <param name="destination">The destination type handle.</param>
        /// <param name="source">The source type handle.</param>
        /// <returns>Whether a given type handle can be assigned to another type handle.</returns>
        public virtual bool CanAssignTo(TypeHandle destination, TypeHandle source)
        {
            return destination == TypeHandle.Unknown || source.IsAssignableFrom(destination, this);
        }

        public virtual IEnumerable<GraphElementModel> GetModelsDisplayableInInspector(IEnumerable<GraphElementModel> models)
        {
            return models.Where(t => t is AbstractNodeModel || t is VariableDeclarationModel || t is PlacematModel || t is WireModel);
        }
    }
}
