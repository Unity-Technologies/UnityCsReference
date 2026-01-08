// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    abstract class AutoPlacementHelper
    {
        protected GraphView m_GraphView;

        HashSet<Model> m_SelectedElementModels = new HashSet<Model>();
        HashSet<Model> m_LeftOverElementModels;

        protected void SendPlacementCommand(List<Model> updatedModels, List<Vector2> updatedDeltas)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var models = updatedModels.OfType<IMovable>();
#pragma warning restore RS0030
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_GraphView.Dispatch(new AutoPlaceElementsCommand(updatedDeltas, models.ToList()));
#pragma warning restore RS0030
        }

        protected Dictionary<Model, Vector2> GetElementDeltaResults()
        {
            GetSelectedElementModels();

            // Elements will be moved by a delta depending on their bounding rect
            List<(Rect, List<Model>)> boundingRects = GetBoundingRects();

            return GetDeltas(boundingRects);
        }

        protected abstract Dictionary<Model, Vector2> GetDeltas(List<(Rect, List<Model>)> boundingRects);

        protected abstract void UpdateReferencePosition(ref float referencePosition, Rect currentElementRect);

        protected abstract Vector2 GetDelta(Rect elementPosition, float referencePosition);

        void GetSelectedElementModels()
        {
            m_SelectedElementModels.Clear();
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_SelectedElementModels.UnionWith(m_GraphView.GetSelection().Where(element => !(element is WireModel) && element.IsMovable()));
#pragma warning restore RS0030
            m_LeftOverElementModels = new HashSet<Model>(m_SelectedElementModels);
        }

        List<(Rect, List<Model>)> GetBoundingRects()
        {
            List<(Rect, List<Model>)> boundingRects = new List<(Rect, List<Model>)>();

            GetPlacematsBoundingRects(ref boundingRects);
            GetLeftOversBoundingRects(ref boundingRects);

            return boundingRects;
        }

        void GetPlacematsBoundingRects(ref List<(Rect, List<Model>)> boundingRects)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            List<PlacematModel> selectedPlacemats = m_SelectedElementModels.OfType<PlacematModel>().ToList();
#pragma warning restore RS0030
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var placemat in selectedPlacemats.Where(placemat => m_LeftOverElementModels.Contains(placemat)))
#pragma warning restore RS0030
            {
                Placemat placematUI = placemat.GetView<Placemat>(m_GraphView);
                if (placematUI != null)
                {
                    var boundingRect = GetPlacematBoundingRect(ref boundingRects, placematUI, selectedPlacemats);
                    boundingRects.Add(new(boundingRect.Key, boundingRect.Value));
                }
            }
        }

        void GetLeftOversBoundingRects(ref List<(Rect, List<Model>)> boundingRects)
        {
            foreach (Model element in m_LeftOverElementModels)
            {
                GraphElement elementUI = element.GetView<GraphElement>(m_GraphView);
                if (elementUI != null)
                {
                    boundingRects.Add(new(elementUI.layout, new List<Model> { element }));
                }
            }
        }

        KeyValuePair<Rect, List<Model>> GetPlacematBoundingRect(ref List<(Rect, List<Model>)> boundingRects, Placemat placematUI, List<PlacematModel> selectedPlacemats)
        {
            Rect boundingRect = placematUI.layout;
            List<Model> elementsOnBoundingRect = new List<Model>();
            List<Placemat> placematsOnBoundingRect = GetPlacematsOnBoundingRect(ref boundingRect, ref elementsOnBoundingRect, selectedPlacemats);

            // Adjust the bounding rect with elements overlapping any of the placemats on the bounding rect
            AdjustPlacematBoundingRect(ref boundingRect, ref elementsOnBoundingRect, placematsOnBoundingRect);

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var otherRect in boundingRects.ToList())
#pragma warning restore RS0030
            {
                Rect otherBoundingRect = otherRect.Item1;
                List<Model> otherBoundingRectElements = otherRect.Item2;
                if (otherBoundingRectElements.HasAny(element => IsOnPlacemats(element.GetView<GraphElement>(m_GraphView), placematsOnBoundingRect)))
                {
                    AdjustBoundingRect(ref boundingRect, otherBoundingRect);
                    elementsOnBoundingRect.AddRange(otherBoundingRectElements);
                    boundingRects.Remove(otherRect);
                }
            }

            return new KeyValuePair<Rect, List<Model>>(boundingRect, elementsOnBoundingRect);
        }

        protected Dictionary<Model, Vector2> ComputeDeltas(IEnumerable<(Rect, List<Model>)> boundingRects, float referencePosition)
        {
            var deltas = new Dictionary<Model, Vector2>();

            foreach (var tuple in boundingRects)
            {
                var boundingRect = tuple.Item1;
                var elements = tuple.Item2;

                Vector2 delta = GetDelta(boundingRect, referencePosition);
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                foreach (var element in elements.Where(element => !deltas.ContainsKey(element)))
#pragma warning restore RS0030
                {
                    deltas[element] = delta;
                }
                UpdateReferencePosition(ref referencePosition, boundingRect);
            }

            return deltas;
        }

        List<Placemat> GetPlacematsOnBoundingRect(ref Rect boundingRect, ref List<Model> elementsOnBoundingRect, List<PlacematModel> selectedPlacemats)
        {
            List<Placemat> placematsOnBoundingRect = new List<Placemat>();

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (PlacematModel placemat in selectedPlacemats.Where(placemat => m_LeftOverElementModels.Contains(placemat)))
#pragma warning restore RS0030
            {
                Placemat placematUI = placemat.GetView<Placemat>(m_GraphView);
                if (placematUI != null && placematUI.layout.Overlaps(boundingRect))
                {
                    AdjustBoundingRect(ref boundingRect, placematUI.layout);

                    placematsOnBoundingRect.Add(placematUI);
                    elementsOnBoundingRect.Add(placemat);
                    m_LeftOverElementModels.Remove(placemat);
                }
            }

            return placematsOnBoundingRect;
        }

        static readonly List<ChildView> k_AdjustPlacematBoundingRectAllUIs = new();
        void AdjustPlacematBoundingRect(ref Rect boundingRect, ref List<Model> elementsOnBoundingRect, List<Placemat> placematsOnBoundingRect)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_GraphView.GraphModel.GetGraphElementModels()
#pragma warning restore RS0030
                .Where(e => e != null && !(e is PlacematModel))
                .GetAllViews(m_GraphView, null, k_AdjustPlacematBoundingRectAllUIs);
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            foreach (var elementUI in k_AdjustPlacematBoundingRectAllUIs.OfType<GraphElement>())
#pragma warning restore RS0030
            {
                if (IsOnPlacemats(elementUI, placematsOnBoundingRect))
                {
                    AdjustBoundingRect(ref boundingRect, elementUI.layout);
                    elementsOnBoundingRect.Add(elementUI.Model);
                    m_LeftOverElementModels.Remove(elementUI.Model);
                }
            }
            k_AdjustPlacematBoundingRectAllUIs.Clear();
        }

        static void AdjustBoundingRect(ref Rect boundingRect, Rect otherRect)
        {
            if (otherRect.yMin < boundingRect.yMin)
            {
                boundingRect.yMin = otherRect.yMin;
            }
            if (otherRect.xMin < boundingRect.xMin)
            {
                boundingRect.xMin = otherRect.xMin;
            }
            if (otherRect.yMax > boundingRect.yMax)
            {
                boundingRect.yMax = otherRect.yMax;
            }
            if (otherRect.xMax > boundingRect.xMax)
            {
                boundingRect.xMax = otherRect.xMax;
            }
        }

        static bool IsOnPlacemats(GraphElement element, List<Placemat> placemats)
        {
            return placemats.HasAny(placemat => !element.Equals(placemat) && element.layout.Overlaps(placemat.layout));
        }
    }
}
