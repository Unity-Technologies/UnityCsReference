// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

[CustomPropertyDrawer(typeof(StartupSelectorsAttribute))]
class StartupSelectorsDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var root = new VisualElement();
        root.Add(new Label(property.displayName));
        root.Add(StartupSelectorList.Create(property));
        return root;
    }
}
