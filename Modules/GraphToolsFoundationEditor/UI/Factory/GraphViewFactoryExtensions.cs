// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Extension methods to create UI for graph element models for the <see cref="GraphView"/>.
    /// </summary>
    /// <remarks>
    /// Extension methods in this class are selected by matching the type of their third parameter to the type
    /// of the graph element model for which we need to instantiate a <see cref="ModelView"/>. You can change the UI for a
    /// model by defining new extension methods for <see cref="ElementBuilder"/> in a class having
    /// the <see cref="GraphElementsExtensionMethodsCacheAttribute"/>.
    /// </remarks>
    [GraphElementsExtensionMethodsCache(typeof(GraphView), GraphElementsExtensionMethodsCacheAttribute.lowestPriority_Internal)]
    static class GraphViewFactoryExtensions
    {
        /// <summary>
        /// Creates a context node from its model.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="nodeModel">The ContextNodeModel this <see cref="ModelView"/> will display.</param>
        /// <returns>A setup <see cref="ModelView"/></returns>
        public static ModelView CreateContext(this ElementBuilder elementBuilder, ContextNodeModel nodeModel)
        {
            ModelView ui = new ContextNode();

            ui.SetupBuildAndUpdate(nodeModel, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        /// <summary>
        /// Creates a block node from its model.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The BlockNodeModel this <see cref="ModelView"/> will display.</param>
        /// <returns>A setup <see cref="ModelView"/></returns>
        public static ModelView CreateBlock(this ElementBuilder elementBuilder, BlockNodeModel model)
        {
            ModelView ui = new BlockNode();

            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static ModelView CreateNode(this ElementBuilder elementBuilder, AbstractNodeModel model)
        {
            ModelView ui;

            if (model is ISingleInputPortNodeModel || model is ISingleOutputPortNodeModel)
                ui = new TokenNode();
            else if (model is PortNodeModel)
                ui = new CollapsibleInOutNode();
            else
                ui = new Node();

            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static ModelView CreatePort(this ElementBuilder elementBuilder, PortModel model)
        {
            var ui = new Port();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static ModelView CreateWire(this ElementBuilder elementBuilder, WireModel model)
        {
            var ui = new Wire();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static ModelView CreateStickyNote(this ElementBuilder elementBuilder, StickyNoteModel model)
        {
            var ui = new StickyNote();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static ModelView CreatePlacemat(this ElementBuilder elementBuilder, PlacematModel model)
        {
            var ui = new Placemat();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static ModelView CreateWirePortal(this ElementBuilder elementBuilder, WirePortalModel model)
        {
            var ui = new TokenNode();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static ModelView CreateErrorBadgeModelView(this ElementBuilder elementBuilder, ErrorBadgeModel model)
        {
            var badge = new ErrorBadge();
            badge.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return badge;
        }

        public static ModelView CreateGraphProcessingErrorBadgeModelView(this ElementBuilder elementBuilder, GraphProcessingErrorModel model)
        {
            var badge = new ErrorBadge();
            badge.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);

            Assert.IsNotNull(badge);
            if (model.Fix != null)
            {
                var contextualMenuManipulator = new ContextualMenuManipulator(e =>
                {
                    e.menu.AppendAction("Fix Error/" + model.Fix.Description,
                        _ => model.Fix.QuickFixAction(elementBuilder.View));
                });
                badge.AddManipulator(contextualMenuManipulator);
            }
            return badge;
        }

        /// <summary>
        /// Creates a subgraph node from its model.
        /// </summary>
        /// <param name="elementBuilder">The element builder.</param>
        /// <param name="model">The SubgraphNodeModel this <see cref="ModelView"/> will display.</param>
        /// <returns>A setup <see cref="ModelView"/></returns>
        public static ModelView CreateSubgraphNodeUI(this ElementBuilder elementBuilder, SubgraphNodeModel model)
        {
            var ui = new SubgraphNode();

            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }
    }
}
