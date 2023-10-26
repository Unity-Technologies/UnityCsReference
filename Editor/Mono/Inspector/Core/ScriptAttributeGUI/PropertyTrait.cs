// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    // Base class to derive property drawers from that represent traits and support stacking.
    // Use this to create custom traits for script variables with custom [[PropertyAttribute]]s.
    // This class itself just displays a property unmodified. The virtual methods can be overridden
    // to modify various aspect of how the property is displayed. Usually the overridden methods
    // should call this base class implementation in order to support stacking, but do additional work.
    //
    // Examples of things that can be done as property traits:
    //  - Indent the property
    //  - Disable the property depending on some criteria
    //  - Display a helpbox below the property
    //  - Display the property without the label so it gets the full Inspector width
    internal class PropertyTrait : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, true);
        }
    }
}
