// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

[CustomPropertyDrawer(typeof(FileCollectionExtension), true)]
class FileCollectionExtensionDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var root = new VisualElement();
        var extension = property.managedReferenceValue as FileCollectionExtension;
        if (extension == null)
            return root;

        var collection = property.serializedObject.targetObject as ResourceTableCollection;
        var file = property.FindPropertyRelative("m_ConnectedFile");

        root.Add(ExtensionUI.Header(extension.GetType(), extension.DisplayName));
        root.Add(new PropertyField(file, LocLabels.ConnectedFile));
        root.Add(new PropertyField(property.FindPropertyRelative("m_RemoveMissingPulledKeys"), LocLabels.RemoveMissingPulledKeys));

        var buttons = ExtensionUI.Row();
        var push = new Button(() => Push(extension, collection, file)) { text = LocLabels.Push };
        var pull = new Button(() => Pull(extension, collection, file)) { text = LocLabels.Pull };
        push.tooltip = L10n.Tr("Writes this collection to the connected file.", null);
        pull.tooltip = L10n.Tr("Reads the connected file into this collection.", null);
        push.AddToClassList(LocClasses.LsGrow);
        pull.AddToClassList(LocClasses.LsGrow);
        buttons.Add(push);
        buttons.Add(pull);
        root.Add(buttons);
        return root;
    }

    static void Push(FileCollectionExtension extension, ResourceTableCollection collection, SerializedProperty file)
    {
        if (collection == null)
            return;
        var path = file.stringValue;
        if (string.IsNullOrEmpty(path))
        {
            var chosen = EditorUtility.SaveFilePanel(L10n.Tr("Push to File", null), Directory(path), collection.TableCollectionName, extension.Exporter.FileExtension);
            if (string.IsNullOrEmpty(chosen))
                return;
            path = Connect(file, chosen);
        }

        var reporter = new EditorProgressBarReporter();
        try
        {
            if (TableFileIO.Write(path, writer => extension.Exporter.Export(writer, collection, reporter)))
                EditorUtility.RevealInFinder(path);
        }
        finally
        {
            reporter.Clear();
        }
    }

    static void Pull(FileCollectionExtension extension, ResourceTableCollection collection, SerializedProperty file)
    {
        if (collection == null)
            return;
        var path = file.stringValue;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            var chosen = EditorUtility.OpenFilePanel(L10n.Tr("Pull from File", null), Directory(path), extension.Importer.FileExtension);
            if (string.IsNullOrEmpty(chosen))
                return;
            path = Connect(file, chosen);
        }

        var options = new TableImportOptions
        {
            CreateUndo = true,
            RemoveMissingEntries = extension.RemoveMissingPulledKeys,
            CreateMissingKeys = true,
            CreateMissingLocaleTables = true
        };
        var reporter = new EditorProgressBarReporter();
        try
        {
            TableFileIO.Read(path, reader => extension.Importer.ImportInto(reader, collection, options, reporter));
        }
        finally
        {
            reporter.Clear();
            collection.SharedData?.InvalidateCache();
            LocalizationEditorSettings.RaiseCollectionsChanged();
        }
    }

    // A path inside the project is stored relative so the collection asset travels between machines.
    static string Connect(SerializedProperty file, string path)
    {
        var relative = FileUtil.GetProjectRelativePath(path.Replace('\\', '/'));
        var stored = string.IsNullOrEmpty(relative) ? path : relative;
        file.stringValue = stored;
        file.serializedObject.ApplyModifiedProperties();
        return stored;
    }

    static string Directory(string path)
        => string.IsNullOrEmpty(path) ? "Assets" : Path.GetDirectoryName(path);
}
