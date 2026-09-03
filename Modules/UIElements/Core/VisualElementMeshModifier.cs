// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    public partial class VisualElement
    {
        internal List<MeshModifierRegistration> m_MeshModifiers;

        /// <summary>
        /// Registers a modifier that runs after this element's mesh generation completes, before
        /// its vertex data is copied to the GPU.
        /// </summary>
        /// <param name="callback">Callback invoked on the main thread.</param>
        /// <param name="recursive">If <c>true</c>, the callback fires for this element plus every dirty
        /// descendant. Panel-wide post-processing is achieved by registering recursively on the panel
        /// root element.</param>
        /// <param name="priority">Dominant ordering axis for callback invocation. Lower values run first.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="callback"/> is null.</exception>
        public void AddMeshModifier(MeshModificationCallback callback, bool recursive = false, int priority = 0)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            m_MeshModifiers ??= new List<MeshModifierRegistration>();
            long id = UIRUtility.GetNextMeshModifierId();
            m_MeshModifiers.Add(new MeshModifierRegistration(callback, recursive, priority, id));

            DirtyForMeshModifierChange(recursive);
        }

        /// <summary>
        /// Removes every modifier previously registered on this element. No-op if none are registered.
        /// </summary>
        /// <remarks>
        /// Equivalent to calling <see cref="RemoveMeshModifier"/> for each registered callback, but
        /// dispatches a single dirty event for the whole batch.
        /// </remarks>
        public void ClearMeshModifiers()
        {
            if (m_MeshModifiers == null || m_MeshModifiers.Count == 0)
                return;

            bool anyRecursive = false;
            for (int i = 0; i < m_MeshModifiers.Count; ++i)
            {
                if (m_MeshModifiers[i].recursive)
                {
                    anyRecursive = true;
                    break;
                }
            }

            m_MeshModifiers.Clear();
            DirtyForMeshModifierChange(anyRecursive);
        }

        /// <summary>
        /// Removes a previously-registered modifier. The registration is identified by delegate
        /// equality; pass the same callback reference that was registered. No-op if the callback is not
        /// registered. If the callback was registered more than once, only the first matching registration
        /// is removed.
        /// </summary>
        /// <param name="callback">The callback reference passed to <see cref="AddMeshModifier"/>.</param>
        public void RemoveMeshModifier(MeshModificationCallback callback)
        {
            if (callback == null || m_MeshModifiers == null)
                return;

            for (int i = 0; i < m_MeshModifiers.Count; ++i)
            {
                if (m_MeshModifiers[i].callback == callback)
                {
                    bool wasRecursive = m_MeshModifiers[i].recursive;
                    m_MeshModifiers.RemoveAt(i);
                    DirtyForMeshModifierChange(wasRecursive);
                    return;
                }
            }
        }

        void DirtyForMeshModifierChange(bool recursive)
        {
            var updater = elementPanel?.GetUpdater(VisualTreeUpdatePhase.Repaint) as UIRRepaintUpdater;
            updater?.renderTreeManager?.UIEOnVisualsChanged(this, hierarchical: recursive);
        }
    }
}
