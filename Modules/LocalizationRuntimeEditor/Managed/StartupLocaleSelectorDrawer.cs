// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

[CustomPropertyDrawer(typeof(IStartupLocaleSelector), useForChildren: true)]
class StartupLocaleSelectorDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var value = property.managedReferenceValue;
        if (value == null)
            return new Label(L10n.Tr("None", null));

        // We want the type as the label
        return new PropertyField(property, ObjectNames.NicifyVariableName(value.GetType().Name));
    }
}
