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
    abstract class GraphElementPresenter : ScriptableObject
    {
        [SerializeField]
        private bool m_Selected;

        [SerializeField]
        private Rect m_Position;

        [SerializeField]
        private Capabilities m_Capabilities;

        public virtual Rect position
        {
            get { return m_Position; }
            set { m_Position = value; }
        }

        public Capabilities capabilities
        {
            get { return m_Capabilities; }
            set { m_Capabilities = value; }
        }

        public bool selected
        {
            get { return m_Selected; }
            set
            {
                // Set new value (toggle old value)
                if ((capabilities & Capabilities.Selectable) == Capabilities.Selectable)
                {
                    m_Selected = value;
                }
            }
        }

        public virtual UnityEngine.Object[] GetObjectsToWatch()
        {
            return new UnityEngine.Object[] { this };
        }

        protected virtual void OnEnable()
        {
            capabilities = Capabilities.Normal | Capabilities.Movable | Capabilities.Selectable;
        }

        public virtual void OnRemoveFromGraph()
        {
        }

        public virtual void CommitChanges()
        {
        }

        public virtual IEnumerable<GraphElementPresenter> allChildren
        {
            get { return Enumerable.Empty<GraphElementPresenter>(); }
        }

        public virtual IEnumerable<GraphElementPresenter> allElements
        {
            get { yield return this; }
        }
    }
}
