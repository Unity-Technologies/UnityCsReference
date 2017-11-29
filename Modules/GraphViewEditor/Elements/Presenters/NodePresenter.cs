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
    public class NodePresenter : SimpleElementPresenter
    {
        [SerializeField]
        protected List<PortPresenter> m_InputPorts;
        public List<PortPresenter> inputPorts
        {
            get { return m_InputPorts ?? (m_InputPorts = new List<PortPresenter>()); }
        }

        [SerializeField]
        protected List<PortPresenter> m_OutputPorts;
        public List<PortPresenter> outputPorts
        {
            get { return m_OutputPorts ?? (m_OutputPorts = new List<PortPresenter>()); }
        }

        [SerializeField]
        private bool m_expanded;
        public virtual bool expanded
        {
            get { return m_expanded; }
            set { m_expanded = value; }
        }

        protected Orientation m_Orientation;
        public virtual Orientation orientation
        {
            get { return m_Orientation; }
        }

        // TODO make a simple creation function
        protected new void OnEnable()
        {
            base.OnEnable();

            capabilities |= Capabilities.Deletable;
        }

        protected NodePresenter()
        {
            m_expanded = true;
            m_Orientation = Orientation.Horizontal;
        }

        public override IEnumerable<GraphElementPresenter> allChildren
        {
            get { return inputPorts.Concat(outputPorts).Cast<GraphElementPresenter>(); }
        }

        public override IEnumerable<GraphElementPresenter> allElements
        {
            get
            {
                yield return this;
                foreach (var inpt in inputPorts)
                {
                    yield return inpt;
                }
                foreach (var outpt in outputPorts)
                {
                    yield return outpt;
                }
            }
        }
    }
}
