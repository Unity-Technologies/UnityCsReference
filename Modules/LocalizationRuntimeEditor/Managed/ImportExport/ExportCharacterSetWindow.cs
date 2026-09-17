// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
using System.IO;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

class ExportCharacterSetWindow : TableSelectionWindow
{
    const string k_DirectoryPref = "LocalizationRuntime.CharacterSet.Directory";

    public static void ShowWindow(ResourceTableCollection selected)
        => Open<ExportCharacterSetWindow>(LocLabels.ExportCharacterSet, selected);

    protected override void BuildOptions(VisualElement root)
    {
        root.Add(new HelpBox(L10n.Tr("Writes every distinct character the selected tables use, for building a font asset.", null), HelpBoxMessageType.Info));
    }

    protected override void Export()
    {
        var title = LocLabels.ExportCharacterSet;
        if (WarnIfNothingSelected(title))
            return;

        var path = EditorUtility.SaveFilePanel(title, EditorPrefs.GetString(k_DirectoryPref, string.Empty), LocLabels.CharacterSet, "txt");
        if (string.IsNullOrEmpty(path))
            return;
        EditorPrefs.SetString(k_DirectoryPref, Path.GetDirectoryName(path));

        var characters = CharacterSetCollectionFormat.NewCharacterSet();
        foreach (var (_, table) in Selector.SelectedTables())
            CharacterSetCollectionFormat.CollectCharacters(table, characters);

        if (TableFileIO.Write(path, writer => CharacterSetCollectionFormat.WriteCharacters(writer, characters)))
            EditorUtility.RevealInFinder(path);
    }
}
