// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.EditorTools
{
    public interface IDrawSelectedHandles
    {
        void OnDrawHandles();
    }

    public abstract class EditorTool : ScriptableObject, IEditor
    {
        bool m_Active;

        [HideInInspector]
        [SerializeField]
        internal UnityObject[] m_Targets;

        [HideInInspector]
        [SerializeField]
        internal UnityObject m_Target;

        public IEnumerable<UnityObject> targets
        {
            get
            {
                // CustomEditor tools get m_Targets populated on instantiation. Persistent tools always reflect the
                // current selection, and therefore do not have m_Targets set. When a tool is deserialized, the m_Targets
                // array is initialized but not populated, hence the `> 0` check (valid because it is not possible to
                // create a custom editor tool instance with no targets)
                if (m_Targets != null && m_Targets.Length > 0)
                    return m_Targets;

                return Selection.objects;
            }
        }

        public UnityObject target
        {
            get { return m_Target == null ? Selection.activeObject : m_Target; }
        }

        public virtual GUIContent toolbarIcon
        {
            get { return null; }
        }

        public virtual bool gridSnapEnabled
        {
            get { return false; }
        }

        internal void Activate()
        {
            if(m_Active
                // Prevent to reenable the tool if this is not the active one anymore
                // Can happen when entering playmode due to the delayCall in EditorToolManager.OnEnable
                || this != EditorToolManager.activeTool)
                return;

            OnActivated();
            m_Active = true;
        }

        internal void Deactivate()
        {
            if(!m_Active)
                return;

            OnWillBeDeactivated();
            m_Active = false;
        }

        public virtual void OnActivated() {}

        public virtual void OnWillBeDeactivated() {}

        public virtual void OnToolGUI(EditorWindow window) {}

        public virtual void PopulateMenu(DropdownMenu menu) {}

        public virtual bool IsAvailable()
        {
            return true;
        }

        void IEditor.SetTarget(UnityObject value)
        {
            m_Target = value;
        }

        void IEditor.SetTargets(UnityObject[] value)
        {
            m_Targets = value;
        }
    }
}
