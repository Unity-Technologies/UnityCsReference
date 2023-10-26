// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    // Base class to derive custom decorator drawers from.
    public abstract class DecoratorDrawer : GUIDrawer
    {
        internal PropertyAttribute m_Attribute;

        // The [[PropertyAttribute]] for the property.
        public PropertyAttribute attribute { get { return m_Attribute; } }

        // Override this method to make your own GUI for the property.
        public virtual void OnGUI(Rect position)
        {
        }

        /// <summary>
        /// Override this method to make your own GUI for the property based on UIElements.
        /// </summary>
        public virtual VisualElement CreatePropertyGUI()
        {
            return null;
        }

        // Override this method to specify how tall the GUI for this field is in pixels.
        public virtual float GetHeight()
        {
            return EditorGUI.kSingleLineHeight;
        }

        [Obsolete("CanCacheInspectorGUI has been deprecated and is no longer used.", false)]
        public virtual bool CanCacheInspectorGUI()
        {
            return true;
        }
    }
}
