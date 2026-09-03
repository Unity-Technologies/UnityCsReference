// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;

namespace Unity.Localization.Editor;

interface ITableCollectionFormat
{
    string DisplayName { get; }

    // The file extension without the leading dot, for example "csv".
    string FileExtension { get; }
}

interface ITableCollectionExporter : ITableCollectionFormat
{
    void Export(TextWriter writer, ResourceTableCollection collection, ITableImportExportReporter reporter = null);
}

interface ITableCollectionImporter : ITableCollectionFormat
{
    void ImportInto(TextReader reader, ResourceTableCollection collection, TableImportOptions options, ITableImportExportReporter reporter = null);
}

sealed class TableImportOptions
{
    public bool CreateUndo = true;

    public bool RemoveMissingEntries;

    public bool CreateMissingKeys = true;

    // Add per-locale tables for locales in the data but not the collection (only for project locales).
    public bool CreateMissingLocaleTables;
}

interface ITableImportExportReporter
{
    void Start(string title, string description);
    void ReportProgress(string description, float progress);
    void Completed(string message);
    void Fail(string message);
}

sealed class EditorProgressBarReporter : ITableImportExportReporter
{
    string m_Title = "Localization";

    public void Start(string title, string description)
    {
        m_Title = title;
        EditorUtility.DisplayProgressBar(title, description, 0f);
    }

    public void ReportProgress(string description, float progress) => EditorUtility.DisplayProgressBar(m_Title, description, progress);

    public void Completed(string message) => Clear();

    public void Fail(string message) => Clear();

    public void Clear() => EditorUtility.ClearProgressBar();
}

static partial class TableFormatRegistry
{
    [AutoStaticsCleanup] // holds instances discovered by reflection
    static List<ITableCollectionExporter> s_Exporters;
    [AutoStaticsCleanup] // holds instances discovered by reflection
    static List<ITableCollectionImporter> s_Importers;

    public static IReadOnlyList<ITableCollectionExporter> Exporters => s_Exporters ??= Build<ITableCollectionExporter>();

    public static IReadOnlyList<ITableCollectionImporter> Importers => s_Importers ??= Build<ITableCollectionImporter>();

    static List<T> Build<T>() where T : class, ITableCollectionFormat
    {
        var result = new List<T>();
        foreach (var type in TypeCache.GetTypesDerivedFrom<T>())
        {
            // Skip a custom format with no parameterless constructor rather than throw during discovery.
            if (type.IsAbstract || type.IsInterface || type.GetConstructor(System.Type.EmptyTypes) == null)
                continue;
            if (System.Activator.CreateInstance(type) is T instance)
                result.Add(instance);
        }
        return result;
    }
}
