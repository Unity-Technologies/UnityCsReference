// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

// The binding window edits the generated UxmlSerializedData, not a serialized LocalizedString, so these hand the
// reference picker the layout that data uses.
[CustomPropertyDrawer(typeof(LocalizedString.UxmlSerializedData), true)]
class LocalizedStringBindingDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
        => new LocalizedStringBindingElement(property, L10n.Tr("Localized String", null), 0);
}

sealed class LocalizedStringBindingElement : LocalizedStringElement
{
    public LocalizedStringBindingElement(SerializedProperty property, string label, int depth)
        : base(property, label, depth, Layout.UxmlData)
    {
    }
}

[CustomPropertyDrawer(typeof(LocalizedAsset<>.UxmlSerializedData), true)]
class LocalizedAssetBindingDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        // The data class is nested in the closed generic, so the asset type comes from its declaring type.
        var assetType = AssetTypeOf(fieldInfo?.FieldType) ?? typeof(UnityEngine.Object);
        return new LocalizedAssetBindingElement(property, L10n.Tr("Localized Asset", null), assetType);
    }

    static Type AssetTypeOf(Type dataType)
    {
        for (var type = dataType?.DeclaringType; type != null && type != typeof(object); type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(LocalizedAsset<>))
                return type.GetGenericArguments()[0];
        }
        return null;
    }
}

sealed class LocalizedAssetBindingElement : LocalizedAssetElement
{
    public LocalizedAssetBindingElement(SerializedProperty property, string label, Type assetType)
        : base(property, label, assetType, Layout.UxmlData)
    {
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
