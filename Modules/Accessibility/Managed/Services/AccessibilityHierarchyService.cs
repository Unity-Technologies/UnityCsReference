// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Accessibility
{
    internal class AccessibilityHierarchyService : IService
    {
        AccessibilityHierarchy m_Hierarchy;

        internal AccessibilityHierarchy hierarchy
        {
            set
            {
                if (value == null)
                {
                    RemoveActiveHierarchy(notifyScreenChanged: true);
                    return;
                }

                RemoveActiveHierarchy(notifyScreenChanged: false);
                m_Hierarchy = value;
                m_Hierarchy.AllocateNative();
                AssistiveSupport.notificationDispatcher.SendScreenChanged();
            }
            get => m_Hierarchy;
        }

        public void Start() { }

        public void Stop()
        {
            if (m_Hierarchy == null)
            {
                return;
            }

            RemoveActiveHierarchy(notifyScreenChanged: true);
        }

        private void RemoveActiveHierarchy(bool notifyScreenChanged)
        {
            if (m_Hierarchy == null)
            {
                return;
            }

            m_Hierarchy.FreeNative();
            m_Hierarchy = null;

            if (notifyScreenChanged)
            {
                AssistiveSupport.notificationDispatcher.SendScreenChanged();
            }
        }

        internal bool TryGetNode(int id, out AccessibilityNode node)
        {
            node = null;
            return m_Hierarchy != null && m_Hierarchy.TryGetNode(id, out node);
        }

        internal List<AccessibilityNode> GetRootNodes()
        {
            return m_Hierarchy?.m_RootNodes;
        }

        internal bool TryGetNodeAt(float x, float y, out AccessibilityNode node)
        {
            node = null;
            return m_Hierarchy != null && m_Hierarchy.TryGetNodeAt(x, y, out node);
        }
    }
}
