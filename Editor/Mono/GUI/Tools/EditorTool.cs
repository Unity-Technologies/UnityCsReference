// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace UnityEditor.EditorTools
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class EditorToolAttribute : Attribute
    {
        public string displayName { get; private set; }
        public Type targetType { get; set; }

        EditorToolAttribute() {}

        public EditorToolAttribute(string displayName, Type targetType = null)
        {
            this.targetType = targetType;
            this.displayName = displayName;
        }
    }

    public abstract class EditorTool : ScriptableObject
    {
        [HideInInspector]
        [SerializeField]
        internal UObject[] m_Targets;

        public IEnumerable<UObject> targets
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

        public abstract GUIContent toolbarIcon { get; }

        public virtual void OnToolGUI(EditorWindow window) {}

        public virtual void OnActivate() {}

        public virtual void OnDeactivate() {}

        public virtual bool IsAvailable()
        {
            return true;
        }
    }
}
