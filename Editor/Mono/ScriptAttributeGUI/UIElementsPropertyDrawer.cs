// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental
{
    // Base class to derive custom property drawers from. Use this to create custom drawers using UIElements
    // for your own [[Serializable]] classes or for script variables with custom [[PropertyAttribute]]s.
    public abstract class UIElementsPropertyDrawer : PropertyDrawer
    {
        public virtual VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return null;
        }
    }
}
