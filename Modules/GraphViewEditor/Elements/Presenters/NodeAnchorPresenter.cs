// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    [Serializable]
    internal
    abstract class NodeAnchorPresenter : GraphElementPresenter
    {
        protected object m_Source;
        public object source
        {
            get { return m_Source; }
            set
            {
                if (m_Source == value) return;
                m_Source = value;
            }
        }

        public abstract Direction direction { get; }

        [SerializeField]
        private Orientation m_Orientation;
        public virtual Orientation orientation
        {
            get { return m_Orientation; }
            set { m_Orientation = value; }
        }

        [SerializeField]
        private Type m_AnchorType;
        public virtual Type anchorType
        {
            get { return m_AnchorType; }
            set { m_AnchorType = value; }
        }

        [SerializeField]
        private bool m_Highlight;
        public virtual bool highlight
        {
            get { return m_Highlight; }
            set { m_Highlight = value; }
        }

        public virtual bool connected
        {
            get
            {
                return m_Connections.Count != 0;
            }
        }
        // the anchor is collapsed explicitly : it must be hidden even if the node is not collapsed
        public virtual bool collapsed
        {
            get { return false; }
        }

        public virtual IEnumerable<EdgePresenter> connections
        {
            get
            {
                return m_Connections;
            }
        }

        [SerializeField]
        private List<EdgePresenter> m_Connections;

        public virtual void Connect(EdgePresenter edgePresenter)
        {
            if (edgePresenter == null)
            {
                throw new ArgumentException("The value passed to NodeAnchorPresenter.Connect is null");
            }

            if (!m_Connections.Contains(edgePresenter))
            {
                m_Connections.Add(edgePresenter);
            }
        }

        public virtual void Disconnect(EdgePresenter edgePresenter)
        {
            if (edgePresenter == null)
            {
                throw new ArgumentException("The value passed to NodeAnchorPresenter.Disconnect is null");
            }

            m_Connections.Remove(edgePresenter);
        }

        public bool IsConnectable()
        {
            return true;
        }

        protected new void OnEnable()
        {
            base.OnEnable();
            m_AnchorType = typeof(object);
            m_Connections = new List<EdgePresenter>();

            capabilities = 0;
        }
    }
}
