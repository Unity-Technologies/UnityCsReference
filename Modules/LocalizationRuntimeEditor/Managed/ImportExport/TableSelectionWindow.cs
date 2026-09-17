// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

// Base for the export windows that work across collections: options, a table selection, and an Export button.
abstract class TableSelectionWindow : EditorWindow
{
    protected CollectionTableSelector Selector { get; private set; }

    ResourceTableCollection m_Preselect;

    protected static T Open<T>(string title, ResourceTableCollection selected) where T : TableSelectionWindow
    {
        var window = GetWindow<T>(true);
        window.titleContent = new GUIContent(title, LocIcons.Tex(LocIcons.Table));
        window.minSize = new Vector2(420, 360);
        window.Preselect(selected);
        return window;
    }

    void Preselect(ResourceTableCollection collection)
    {
        m_Preselect = collection;
        if (collection != null && Selector != null)
            Selector.SetSelection(collection);
    }

    void CreateGUI()
    {
        BuildOptions(rootVisualElement);

        Selector = new CollectionTableSelector();
        Selector.AddToClassList(LocClasses.LsGrow);
        if (m_Preselect != null)
            Selector.SetSelection(m_Preselect);
        rootVisualElement.Add(Selector);

        rootVisualElement.Add(new Button(Export) { text = LocLabels.Export });
    }

    protected abstract void BuildOptions(VisualElement root);

    protected abstract void Export();

    protected bool WarnIfNothingSelected(string title)
    {
        if (Selector.AnySelected)
            return false;
        EditorUtility.DisplayDialog(title, L10n.Tr("Select at least one table to export.", null), L10n.Tr("OK", null));
        return true;
    }
}
