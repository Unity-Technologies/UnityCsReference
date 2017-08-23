// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    [Serializable]
    internal
    abstract class GraphViewPresenter : ScriptableObject
    {
        [SerializeField]
        // TODO TEMP protected while upgrading MaterialGraph. Needs to go back private
        protected List<GraphElementPresenter> m_Elements = new List<GraphElementPresenter>();

        [SerializeField]
        private List<GraphElementPresenter> m_TempElements = new List<GraphElementPresenter>();

        [SerializeField]
        public Vector3 position;
        [SerializeField]
        public Vector3 scale;

        public IEnumerable<GraphElementPresenter> allChildren
        {
            get { return m_Elements.SelectMany(e => e.allElements); }
        }

        public virtual void AddElement(GraphElementPresenter element)
        {
            m_Elements.Add(element);
        }

        // Some usage require a separate handler for edges.
        public virtual void AddElement(EdgePresenter edge)
        {
            AddElement((GraphElementPresenter)edge);
        }

        public virtual void RemoveElement(GraphElementPresenter element)
        {
            element.OnRemoveFromGraph();
            m_Elements.Remove(element);
        }

        protected void OnEnable()
        {
            m_Elements.Clear();
            m_TempElements.Clear();
        }

        public IEnumerable<GraphElementPresenter> elements
        {
            get { return m_Elements.Union(m_TempElements); }
        }

        public void AddTempElement(GraphElementPresenter element)
        {
            m_TempElements.Add(element);
        }

        public void RemoveTempElement(GraphElementPresenter element)
        {
            element.OnRemoveFromGraph();
            m_TempElements.Remove(element);
        }

        public void ClearTempElements()
        {
            m_TempElements.Clear();
        }

        public virtual List<NodeAnchorPresenter> GetCompatibleAnchors(NodeAnchorPresenter startAnchor, NodeAdapter nodeAdapter)
        {
            return allChildren.OfType<NodeAnchorPresenter>()
                .Where(nap => nap.IsConnectable() &&
                nap.orientation == startAnchor.orientation &&
                nap.direction != startAnchor.direction &&
                nodeAdapter.GetAdapter(nap.source, startAnchor.source) != null)
                .ToList();
        }
    }
}
