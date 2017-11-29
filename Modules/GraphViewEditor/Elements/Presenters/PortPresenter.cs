// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    [Serializable]
    public abstract class PortPresenter : GraphElementPresenter
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
        private Type m_PortType;
        public virtual Type portType
        {
            get { return m_PortType; }
            set { m_PortType = value; }
        }

        [SerializeField]
        private bool m_Highlight = true;
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
        // the port is collapsed explicitly : it must be hidden even if the node is not collapsed
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
                throw new ArgumentException("The value passed to PortPresenter.Connect is null");
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
                throw new ArgumentException("The value passed to PortPresenter.Disconnect is null");
            }

            m_Connections.Remove(edgePresenter);
        }

        public bool IsConnectable()
        {
            return true;
        }

        protected virtual void SetCapabilities()
        {
            capabilities = 0;
        }

        protected new void OnEnable()
        {
            base.OnEnable();
            m_PortType = typeof(object);
            m_Connections = new List<EdgePresenter>();

            SetCapabilities();
        }
    }
}
