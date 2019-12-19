// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.GraphView
{
    public class PlacematContainer : GraphView.Layer
    {
        public enum CycleDirection
        {
            Up,
            Down
        }

        LinkedList<Placemat> m_Placemats;
        public IEnumerable<Placemat> Placemats => m_Placemats;

        GraphView m_GraphView;

        public PlacematContainer(GraphView graphView)
        {
            m_Placemats = new LinkedList<Placemat>();
            m_GraphView = graphView;

            AddStyleSheetPath("PlacematContainer");
            AddToClassList("placematContainer");
            pickingMode = PickingMode.Ignore;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_GraphView.graphViewChanged += OnGraphViewChange;
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            // ReSharper disable once DelegateSubtraction
            m_GraphView.graphViewChanged -= OnGraphViewChange;
        }

        GraphViewChange OnGraphViewChange(GraphViewChange graphViewChange)
        {
            if (graphViewChange.elementsToRemove != null)
                foreach (Placemat placemat in graphViewChange.elementsToRemove.OfType<Placemat>())
                    RemovePlacemat(placemat);

            return graphViewChange;
        }

        public static int PlacematsLayer => Int32.MinValue;

        void AddPlacemat(Placemat placemat)
        {
            m_Placemats.AddLast(placemat);
            Add(placemat);
        }

        void RemovePlacemat(Placemat placemat)
        {
            placemat.RemoveFromHierarchy();
            m_Placemats.Remove(placemat);
        }

        public bool GetPortCenterOverride(Port port, out Vector2 overriddenPosition)
        {
            Node rootNode = port.node;
            if (rootNode != null)
            {
                Node currNode;
                while ((currNode = rootNode.GetFirstAncestorOfType<Node>()) != null)
                    rootNode = currNode;

                //Find the furthest placemat containing the rootNode and that is collapsed (if any)
                Placemat placemat = m_Placemats.FirstOrDefault(p => p.Collapsed && p.WillDragNode(rootNode));

                if (placemat != null)
                    return placemat.GetPortCenterOverride(port, out overriddenPosition);
            }

            overriddenPosition = Vector3.zero;
            return false;
        }

        public T CreatePlacemat<T>(Rect placematPosition, int zOrder, string placematTitle) where T : Placemat, new()
        {
            return InitAndAddPlacemat(new T(), placematPosition, zOrder, placematTitle);
        }

        public T CreatePlacemat<T>(Func<T> creator, Rect placematPosition, int zOrder, string placematTitle) where T : Placemat
        {
            return InitAndAddPlacemat(creator(), placematPosition, zOrder, placematTitle);
        }

        T InitAndAddPlacemat<T>(T placemat, Rect placematPosition, int zOrder, string placematTitle) where T : Placemat
        {
            placemat.Init(m_GraphView);
            placemat.title = placematTitle;
            placemat.SetPosition(placematPosition);
            placemat.ZOrder = zOrder;
            AddPlacemat(placemat);
            return placemat;
        }

        public void RemoveAllPlacemats()
        {
            Clear();
            m_Placemats.Clear();
        }

        public int GetTopZOrder()
        {
            return m_Placemats.Last?.Value.ZOrder + 1 ?? 1;
        }

        internal void CyclePlacemat(Placemat placemat, CycleDirection direction)
        {
            var node = m_Placemats.Find(placemat);
            if (node == null)
                return;

            var next = direction == CycleDirection.Up ? node.Next : node.Previous;
            if (next != null)
            {
                m_Placemats.Remove(placemat);
                if (direction == CycleDirection.Down)
                    m_Placemats.AddBefore(next, node);
                else
                    m_Placemats.AddAfter(next, node);
            }

            UpdateElementsOrder();
        }

        protected virtual void UpdateElementsOrder()
        {
            // Reset ZOrder from placemat order in array
            int idx = 1;
            foreach (var placemat in m_Placemats)
                placemat.ZOrder = idx++;

            Sort((a, b) => ((Placemat)a).ZOrder.CompareTo(((Placemat)b).ZOrder));
        }

        internal void SendToBack(Placemat placemat)
        {
            m_Placemats.Remove(placemat);
            m_Placemats.AddFirst(placemat);

            UpdateElementsOrder();
        }

        internal void BringToFront(Placemat placemat)
        {
            m_Placemats.Remove(placemat);
            m_Placemats.AddLast(placemat);

            UpdateElementsOrder();
        }

        public void HideCollapsedEdges()
        {
            // We need to hide edges in the reverse zOrder (topmost mats are collapsed first)
            foreach (var p in Placemats.OrderByDescending(p => p.ZOrder))
            {
                p.HideCollapsedEdges();
            }
        }
    }
}
