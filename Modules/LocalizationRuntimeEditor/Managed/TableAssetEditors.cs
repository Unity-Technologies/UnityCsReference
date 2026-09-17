// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

[CustomEditor(typeof(SharedTableData))]
class SharedTableDataEditor : UnityEditor.Editor
{
    const string k_CollectionNameField = "m_TableCollectionName";

    public override VisualElement CreateInspectorGUI()
        => TableAssetInspector.Build(LocLabels.Collection, serializedObject.FindProperty(k_CollectionNameField),
            AssetProviderEditors.OwnerOf((SharedTableData)target));
}

[CustomEditor(typeof(ResourceTable))]
class ResourceTableEditor : UnityEditor.Editor
{
    const string k_LocaleCodeField = "m_LocaleCode";

    public override VisualElement CreateInspectorGUI()
        => TableAssetInspector.Build(LocLabels.Locale, serializedObject.FindProperty(k_LocaleCodeField),
            AssetProviderEditors.OwnerOf((ResourceTable)target));
}

// The Resource Tables window owns both table assets, so their Inspector names the owning collection and offers the
// window rather than drawing fields that would break the collection's key ids.
static class TableAssetInspector
{
    internal const string OpenButtonName = "open-in-tables-window";

    internal static VisualElement Build(string label, SerializedProperty owner, ResourceTableCollection collection)
    {
        var root = new VisualElement();

        var ownerField = new TextField(label);
        ownerField.SetEnabled(false);
        if (owner != null)
            ownerField.BindProperty(owner);
        root.Add(ownerField);

        root.Add(new HelpBox(collection != null ? LocLabels.EditInTablesWindowHelp : LocLabels.NoOwningCollectionHelp,
            HelpBoxMessageType.Info));

        var open = new Button(() => ResourceTablesWindow.ShowWindow(collection))
        {
            name = OpenButtonName,
            text = LocLabels.OpenInTablesWindow
        };
        open.SetEnabled(collection != null);
        root.Add(open);
        return root;
    }
}
