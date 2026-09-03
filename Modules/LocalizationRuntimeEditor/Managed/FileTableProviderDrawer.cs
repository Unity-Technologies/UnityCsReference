// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

// Titles a file table source by its display name rather than the array's "Element N".
[CustomPropertyDrawer(typeof(Providers.FileTables.FileTableProvider), useForChildren: true)]
class FileTableProviderDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var value = property.managedReferenceValue;
        if (value == null)
            return new Label(L10n.Tr("Unresolved", null));

        return new PropertyField(property, AssetProviderEditors.ProviderTitle(value.GetType()));
    }
}
