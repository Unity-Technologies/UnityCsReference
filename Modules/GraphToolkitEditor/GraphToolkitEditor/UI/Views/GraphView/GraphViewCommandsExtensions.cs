// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Extension methods on <see cref="GraphView"/> to help dispatching commands.
    /// </summary>
    [UnityRestricted]
    internal static class GraphViewCommandsExtensions
    {
        /// <summary>
        /// Dispatches a <see cref="ReframeGraphViewCommand"/> to show all graph elements.
        /// </summary>
        /// <param name="self">The graph view.</param>
        public static void DispatchFrameAllCommand(this GraphView self)
        {
            var rectToFit = self.CalculateRectToFitAll();
            self.CalculateFrameTransform(rectToFit, self.layout, GraphView.frameBorder, out var frameTranslation, out var frameScaling);
            self.Dispatch(new ReframeGraphViewCommand(frameTranslation, frameScaling));
        }

        /// <summary>
        /// Dispatches a <see cref="ReframeGraphViewCommand"/> to show all selected graph elements.
        /// </summary>
        /// <param name="self">The graph view.</param>
        public static void DispatchFrameSelectionCommand(this GraphView self)
        {
            var graphElements = new List<GraphElement>();
            foreach (var model in self.GetSelection())
            {
                var ui = model.GetView<GraphElement>(self);
                if (ui != null)
                    graphElements.Add(ui);
            }

            if (graphElements.Count > 0)
                DispatchFrameAndSelectElementsCommand(self, false, graphElements);
            else
                DispatchFrameAllCommand(self);
        }

        static readonly List<GraphElement> k_DispatchFramePrevNextCommandAllUIs = new();

        static void DispatchFrameAndSelectElementCommand(GraphView graphView, IComparer<GraphElement> elementSortComparer, Func<GraphElementModel, bool> elementFilter = null)
        {
            elementFilter ??= DefaultElementFilter;

            var models = graphView.GraphModel.GetGraphElementModels();
            foreach (var model in models)
            {
                if (model.IsSelectable() && elementFilter(model))
                {
                    model.AppendAllViews(graphView, e => e != null, k_DispatchFramePrevNextCommandAllUIs);
                }
            }

            if (k_DispatchFramePrevNextCommandAllUIs.Count == 0)
                return;

            k_DispatchFramePrevNextCommandAllUIs.Sort(elementSortComparer);

            var selection = graphView.GetSelection();
            var selectedModel = selection.Count > 0 ? selection[0] : null;
            int idx = k_DispatchFramePrevNextCommandAllUIs.FindIndex(e => ReferenceEquals(e.Model, selectedModel));
            var graphElement = idx >= 0 && idx < k_DispatchFramePrevNextCommandAllUIs.Count - 1 ? k_DispatchFramePrevNextCommandAllUIs[idx + 1] : k_DispatchFramePrevNextCommandAllUIs[0];

            DispatchFrameAndSelectElementsCommand(graphView, true, graphElement);
            k_DispatchFramePrevNextCommandAllUIs.Clear();

            return;

            bool DefaultElementFilter(GraphElementModel model)
            {
                var acceptedTypes = new[] { typeof(AbstractNodeModel), typeof(PlacematModel), typeof(StickyNoteModel) };
                foreach (var acceptedType in acceptedTypes)
                {
                    if (acceptedType.IsInstanceOfType(model))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Computes the rectangle that encompasses all elements in <paramref name="graphElements"/>.
        /// </summary>
        /// <param name="self">The <see cref="GraphView"/> displaying the elements.</param>
        /// <param name="graphElements">The graph elements.</param>
        /// <returns>A rectangle that encompasses all elements in <paramref name="graphElements"/>.</returns>
        public static Rect CalculateRectToFitElements(this GraphView self, IReadOnlyList<GraphElement> graphElements)
        {
            Rect rectToFit = self.ContentViewContainer.layout;

            if (graphElements == null || graphElements.Count == 0)
            {
                return rectToFit;
            }

            VisualElement graphElement = graphElements[0].SizeElement;

            if (graphElement != null)
            {
                rectToFit = graphElement.parent.ChangeCoordinatesTo(self.ContentViewContainer, graphElement.layout);
            }

            rectToFit = graphElements.Aggregate(rectToFit, (current, currentGraphElement) =>
            {
                VisualElement currentElement = currentGraphElement.SizeElement;

                return RectUtils.Encompass(current, currentElement.parent.ChangeCoordinatesTo(self.ContentViewContainer, currentElement.layout));
            });

            return rectToFit;
        }

        /// <summary>
        /// Dispatch a command to frame and optionally select a list of graph elements.
        /// </summary>
        /// <param name="self">The GraphView.</param>
        /// <param name="graphElements">The list of elements to frame and optionally select.</param>
        /// <param name="select">True if the elements should be selected. False if the selection should not change.</param>
        public static void DispatchFrameAndSelectElementsCommand(this GraphView self, bool select, params GraphElement[] graphElements)
        {
            DispatchFrameAndSelectElementsCommand(self, select, (IReadOnlyList<GraphElement>)graphElements);
        }

        /// <summary>
        /// Dispatch a command to frame and optionally select a list of graph elements.
        /// </summary>
        /// <param name="self">The GraphView.</param>
        /// <param name="graphElements">The list of elements to frame and optionally select.</param>
        /// <param name="select">True if the elements should be selected. False if the selection should not change.</param>
        public static void DispatchFrameAndSelectElementsCommand(this GraphView self, bool select, IReadOnlyList<GraphElement> graphElements)
        {
            if (graphElements.Count == 0)
                return;

            self.CalculateFrameTransformToFitElements(graphElements, out var frameTranslation, out var frameScaling);

            self.Dispatch(new ReframeGraphViewCommand(frameTranslation, frameScaling,
                select ? graphElements.Select(e => e.GraphElementModel).ToList() : null));
        }

        /// <summary>
        /// Computes the frame transform needed to fit a list of graph elements.
        /// </summary>
        /// <param name="self">The GraphView.</param>
        /// <param name="graphElements">The list of elements to frame.</param>
        /// <param name="frameTranslation">The translation needed to frame the graph elements.</param>
        /// <param name="frameScaling">The scaling needed to frame the graph elements.</param>
        /// <param name="maxZoomLevel">The maximum zoom level permitted.</param>
        public static void CalculateFrameTransformToFitElements(this GraphView self, IReadOnlyList<GraphElement> graphElements, out Vector3 frameTranslation, out Vector3 frameScaling, float maxZoomLevel = -1.0f)
        {
            var rectToFit = CalculateRectToFitElements(self, graphElements);
            self.CalculateFrameTransform(rectToFit, self.layout, GraphView.frameBorder, out frameTranslation, out frameScaling, maxZoomLevel);
        }
    }
}
