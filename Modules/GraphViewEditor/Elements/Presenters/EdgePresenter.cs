// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    [Serializable]
    internal
    class EdgePresenter : GraphElementPresenter
    {
        [SerializeField]
        private NodeAnchorPresenter m_OutputPresenter;

        [SerializeField]
        private NodeAnchorPresenter m_InputPresenter;

        public virtual NodeAnchorPresenter output
        {
            get { return m_OutputPresenter; }
            set
            {
                if (m_OutputPresenter != null)
                {
                    m_OutputPresenter.Disconnect(this);
                }

                m_OutputPresenter = value;
                if (m_OutputPresenter != null)
                    m_OutputPresenter.Connect(this);
            }
        }

        public virtual NodeAnchorPresenter input
        {
            get { return m_InputPresenter; }
            set
            {
                if (m_InputPresenter != null)
                {
                    m_InputPresenter.Disconnect(this);
                }
                m_InputPresenter = value;
                if (m_InputPresenter != null)
                    m_InputPresenter.Connect(this);
            }
        }

        public Vector2 candidatePosition { get; set; }
        public bool candidate { get; set; }

        protected new void OnEnable()
        {
            base.OnEnable();
            capabilities = Capabilities.Deletable | Capabilities.Selectable;
        }

        protected EdgePresenter() {}
    }
}
