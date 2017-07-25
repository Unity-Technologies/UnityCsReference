// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Reflection;
using UnityEngine;

namespace UnityEditor
{
    // Base class to derive custom property drawers from. Use this to create custom drawers
    // for your own [[Serializable]] classes or for script variables with custom [[PropertyAttribute]]s.
    public abstract class PropertyDrawer : GUIDrawer
    {
        internal PropertyAttribute m_Attribute;
        internal FieldInfo m_FieldInfo;

        // The [[PropertyAttribute]] for the property. Not applicable for custom class drawers. (RO)
        public PropertyAttribute attribute { get { return m_Attribute; } }

        // The reflection FieldInfo for the member this property represents. (RO)
        public FieldInfo fieldInfo { get { return m_FieldInfo; } }

        internal void OnGUISafe(Rect position, SerializedProperty property, GUIContent label)
        {
            ScriptAttributeUtility.s_DrawerStack.Push(this);
            OnGUI(position, property, label);
            ScriptAttributeUtility.s_DrawerStack.Pop();
        }

        // Override this method to make your own GUI for the property.
        public virtual void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.DefaultPropertyField(position, property, label);
            EditorGUI.LabelField(position, label, EditorGUIUtility.TempContent("No GUI Implemented"));
        }

        internal float GetPropertyHeightSafe(SerializedProperty property, GUIContent label)
        {
            ScriptAttributeUtility.s_DrawerStack.Push(this);
            float height = GetPropertyHeight(property, label);
            ScriptAttributeUtility.s_DrawerStack.Pop();
            return height;
        }

        // Override this method to specify how tall the GUI for this field is in pixels.
        public virtual float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.kSingleLineHeight;
        }

        internal bool CanCacheInspectorGUISafe(SerializedProperty property)
        {
            ScriptAttributeUtility.s_DrawerStack.Push(this);
            bool canCache = CanCacheInspectorGUI(property);
            ScriptAttributeUtility.s_DrawerStack.Pop();
            return canCache;
        }

        public virtual bool CanCacheInspectorGUI(SerializedProperty property)
        {
            return true;
        }
    }
}
