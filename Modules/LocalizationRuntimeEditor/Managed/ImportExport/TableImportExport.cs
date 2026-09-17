// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;

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

// A format that gathers its options in a window instead of a single file dialog.
interface ITableCollectionWindowFormat : ITableCollectionFormat
{
    void Open(ResourceTableCollection collection);
}

// An online destination rather than a file, so it has no extension and the caller opens no file dialog.
interface ITableCollectionService
{
    string DisplayName { get; }

    // Hides the service until the collection carries whatever configuration it needs.
    bool IsAvailable(ResourceTableCollection collection);

    void Push(ResourceTableCollection collection, ITableImportExportReporter reporter = null);

    void Pull(ResourceTableCollection collection, TableImportOptions options, ITableImportExportReporter reporter = null);
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

// Shared by the tables window and the file extensions so both report a failed read or write the same way.
static class TableFileIO
{
    public static bool Write(string path, Action<TextWriter> write)
    {
        try
        {
            using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
            write(writer);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog(L10n.Tr("Export Failed", null), e.Message, L10n.Tr("OK", null));
            return false;
        }
    }

    public static bool Read(string path, Action<TextReader> read)
    {
        try
        {
            using var reader = new StreamReader(path);
            read(reader);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorUtility.DisplayDialog(L10n.Tr("Import Failed", null), e.Message, L10n.Tr("OK", null));
            return false;
        }
    }
}

static partial class TableFormatRegistry
{
    [AutoStaticsCleanup] // holds instances discovered by reflection
    static List<ITableCollectionExporter> s_Exporters;
    [AutoStaticsCleanup] // holds instances discovered by reflection
    static List<ITableCollectionImporter> s_Importers;
    [AutoStaticsCleanup] // holds instances discovered by reflection
    static List<ITableCollectionService> s_Services;
    [AutoStaticsCleanup] // holds instances discovered by reflection
    static List<ITableCollectionWindowFormat> s_Windows;
    [AutoStaticsCleanup] // holds instances discovered by reflection
    static List<object> s_Instances;

    public static IReadOnlyList<ITableCollectionExporter> Exporters => s_Exporters ??= Build<ITableCollectionExporter>();

    public static IReadOnlyList<ITableCollectionImporter> Importers => s_Importers ??= Build<ITableCollectionImporter>();

    public static IReadOnlyList<ITableCollectionService> Services => s_Services ??= Build<ITableCollectionService>();

    public static IReadOnlyList<ITableCollectionWindowFormat> Windows => s_Windows ??= Build<ITableCollectionWindowFormat>();

    static List<T> Build<T>() where T : class
    {
        var result = new List<T>();
        foreach (var instance in Instances())
        {
            if (instance is T typed)
                result.Add(typed);
        }
        return result;
    }

    // One instance per format type, shared by the lists, so a format that both exports and opens a window is built once.
    static List<object> Instances()
    {
        if (s_Instances != null)
            return s_Instances;

        s_Instances = new List<object>();
        var types = new HashSet<System.Type>();
        foreach (var type in TypeCache.GetTypesDerivedFrom<ITableCollectionFormat>())
            types.Add(type);
        foreach (var type in TypeCache.GetTypesDerivedFrom<ITableCollectionService>())
            types.Add(type);

        foreach (var type in types)
        {
            // Skip a custom format with no parameterless constructor rather than throw during discovery.
            if (type.IsAbstract || type.IsInterface || type.GetConstructor(System.Type.EmptyTypes) == null)
                continue;
            var instance = System.Activator.CreateInstance(type);
            if (instance != null)
                s_Instances.Add(instance);
        }
        return s_Instances;
    }
}
