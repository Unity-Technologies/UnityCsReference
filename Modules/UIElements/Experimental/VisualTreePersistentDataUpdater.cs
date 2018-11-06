// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    internal class VisualTreePersistentDataUpdater : BaseVisualTreeUpdater
    {
        private HashSet<VisualElement> m_UpdateList = new HashSet<VisualElement>();
        private HashSet<VisualElement> m_ParentList = new HashSet<VisualElement>();

        private const int kMaxValidatePersistentDataCount = 5;
        private uint m_Version = 0;
        private uint m_LastVersion = 0;

        public override string description
        {
            get { return "Update PersistentData"; }
        }

        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if ((versionChangeType & VersionChangeType.PersistentData) != VersionChangeType.PersistentData)
                return;

            ++m_Version;

            m_UpdateList.Add(ve);
            PropagateToParents(ve);
        }

        public override void Update()
        {
            if (m_Version == m_LastVersion)
                return;

            int validatePersistentDataCount = 0;
            while (m_LastVersion != m_Version)
            {
                m_LastVersion = m_Version;

                ValidatePersistentDataOnSubTree(visualTree, true);
                validatePersistentDataCount++;

                if (validatePersistentDataCount > kMaxValidatePersistentDataCount)
                {
                    Debug.LogError("UIElements: Too many children recursively added that rely on persistent data: " + visualTree);
                    break;
                }
            }

            m_UpdateList.Clear();
            m_ParentList.Clear();
        }

        private void ValidatePersistentDataOnSubTree(VisualElement ve, bool enablePersistence)
        {
            // We don't want to persist when there is a high chance that there will
            // be persistenceKey conflicts and data sharing. Generally, if an element
            // has no persistenceKey, we do not persist it and any of its children.
            // There are some exceptions, hence the use of IsPersitenceSupportedOnChildren().
            if (!ve.IsPersitenceSupportedOnChildren())
                enablePersistence = false;

            if (m_UpdateList.Contains(ve))
            {
                m_UpdateList.Remove(ve);
                ve.OnPersistentDataReady(enablePersistence);
            }

            if (m_ParentList.Contains(ve))
            {
                m_ParentList.Remove(ve);
                for (int i = 0; i < ve.shadow.childCount; ++i)
                {
                    ValidatePersistentDataOnSubTree(ve.shadow[i], enablePersistence);
                }
            }
        }

        private void PropagateToParents(VisualElement ve)
        {
            var parent = ve.shadow.parent;
            while (parent != null)
            {
                if (!m_ParentList.Add(parent))
                {
                    break;
                }

                parent = parent.shadow.parent;
            }
        }
    }
}
