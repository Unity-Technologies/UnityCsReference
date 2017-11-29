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
    public abstract class GraphViewPresenter : ScriptableObject
    {
        [SerializeField]
        // TODO TEMP protected while upgrading MaterialGraph. Needs to go back private
        protected List<GraphElementPresenter> m_Elements;

        [SerializeField]
        private List<GraphElementPresenter> m_TempElements;

        [SerializeField]
        private Vector3 m_Position;
        public virtual Vector3 position { get { return m_Position; } set { m_Position = value; } }

        [SerializeField]
        private Vector3 m_Scale;
        public virtual Vector3 scale { get { return m_Scale; } set { m_Scale = value; } }

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
            if (m_Elements == null)
                m_Elements = new List<GraphElementPresenter>();
            if (m_TempElements == null)
                m_TempElements = new List<GraphElementPresenter>();

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

        public virtual List<PortPresenter> GetCompatiblePorts(PortPresenter startPort, NodeAdapter nodeAdapter)
        {
            return allChildren.OfType<PortPresenter>()
                .Where(nap => nap.IsConnectable() &&
                nap.orientation == startPort.orientation &&
                nap.direction != startPort.direction &&
                nodeAdapter.GetAdapter(nap.source, startPort.source) != null)
                .ToList();
        }
    }
}
