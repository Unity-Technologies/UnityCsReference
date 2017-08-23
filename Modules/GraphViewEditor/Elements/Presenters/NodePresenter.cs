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
    class NodePresenter : SimpleElementPresenter
    {
        [SerializeField]
        protected List<NodeAnchorPresenter> m_InputAnchors;
        public List<NodeAnchorPresenter> inputAnchors
        {
            get { return m_InputAnchors ?? (m_InputAnchors = new List<NodeAnchorPresenter>()); }
        }

        [SerializeField]
        protected List<NodeAnchorPresenter> m_OutputAnchors;
        public List<NodeAnchorPresenter> outputAnchors
        {
            get { return m_OutputAnchors ?? (m_OutputAnchors = new List<NodeAnchorPresenter>()); }
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
            get { return inputAnchors.Concat(outputAnchors).Cast<GraphElementPresenter>(); }
        }

        public override IEnumerable<GraphElementPresenter> allElements
        {
            get
            {
                yield return this;
                foreach (var inpt in inputAnchors)
                {
                    yield return inpt;
                }
                foreach (var outpt in outputAnchors)
                {
                    yield return outpt;
                }
            }
        }
    }
}
