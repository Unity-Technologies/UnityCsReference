// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.BuildService;
using UnityEditor.ScriptUpdater;
using UnityEditorInternal.APIUpdating;
using UnityEngine;

namespace UnityEditor.Scripting.ScriptCompilation.MsBuild;

// The MSBuild counterpart of BeeDriver's script updater loop. The Unity.Sdk Csc task runs
// ScriptUpdater on a compile with upgradable errors or warnings and writes the updated files aside; this applies them through
// ScriptUpdatesHandler (consent, version control, immutable packages) and rebuilds.
partial class MsBuildCompilation
{
    // Must match Csc.PendingUpdatesWarningCode in Unity.MSBuildScriptUpdater.
    internal const string k_PendingScriptUpdatesWarningCode = "UNITYSU001";

    // Mirrors PendingUpdates in Unity.MSBuildScriptUpdater. JsonUtility assigns the fields.
#pragma warning disable 649
    [Serializable]
    class PendingScriptUpdates
    {
        [Serializable]
        public class FileUpdate
        {
            public string original;
            public string updated;
        }

        [Serializable]
        public class Message
        {
            public string kind;
            public string text;
        }

        public string assemblyName;
        public string commandLine;
        public string output;
        public FileUpdate[] updates;
        public Message[] messages;
    }
#pragma warning restore 649

    // Like BeeDriver's attemptedScriptUpdaterOn: each assembly is updated at most once per compilation.
    readonly HashSet<string> m_ScriptUpdaterAttemptedAssemblies = new HashSet<string>();
    readonly List<LogMessage> m_ScriptUpdaterMessages = new List<LogMessage>();
    ScriptUpdatesHandler m_ScriptUpdatesHandler;
    bool m_RebuildingAfterScriptUpdates;

    void BeginScriptUpdatingForBuild()
    {
        if (m_RebuildingAfterScriptUpdates)
            m_RebuildingAfterScriptUpdates = false;
        else
        {
            m_ScriptUpdaterAttemptedAssemblies.Clear();
            m_ScriptUpdaterMessages.Clear();

            // Holds "Yes, for these and other files" consent, which lasts for one compilation.
            m_ScriptUpdatesHandler = null;
        }

        // The task skips these so a rebuild does not run the updater on them again.
        UnityEditorMSBuildPropsTargetsGeneration.UpdateScriptUpdaterConfig(APIUpdaterManager.ConfigurationSourcesFilter, m_ScriptUpdaterAttemptedAssemblies);
    }

    // Returns true when source files were updated and a rebuild has been requested.
    bool TryApplyScriptUpdates(BuildResultMessage buildResult)
    {
        var updates = new List<ScriptUpdate>();
        foreach (var manifestPath in FindPendingScriptUpdateManifests(buildResult))
        {
            var pending = ReadPendingScriptUpdates(manifestPath);

            // The task skips attempted assemblies, but a build served from the host's cache can still report one.
            if (pending == null || !m_ScriptUpdaterAttemptedAssemblies.Add(pending.assemblyName))
                continue;

            // stdout, so it reaches Editor.log without a Console entry.
            Console.WriteLine($"[API Updater] Ran script updater for {pending.assemblyName}: {pending.commandLine}");

            // As Bee's StdOutMode.LogStdOutOnFinish: Write then WriteLine, as one large WriteLine can fail when the log is piped.
            if (!string.IsNullOrEmpty(pending.output))
            {
                Console.Write(pending.output);
                Console.WriteLine();
            }

            foreach (var message in pending.messages ?? Array.Empty<PendingScriptUpdates.Message>())
            {
                m_ScriptUpdaterMessages.Add(new LogMessage
                {
                    Message = message.text,
                    MessageType = message.kind == "Error" ? LogMessageType.Error : LogMessageType.Warning,
                });
            }

            foreach (var update in pending.updates ?? Array.Empty<PendingScriptUpdates.FileUpdate>())
            {
                // A build served from the host's cache can point at a manifest from an earlier build.
                if (!File.Exists(update.updated))
                    continue;

                updates.Add(new ScriptUpdate(ToProjectRelativePath(update.original), update.updated));
            }
        }

        if (updates.Count == 0)
            return false;

        m_ScriptUpdatesHandler ??= new ScriptUpdatesHandler();
        if (!m_ScriptUpdatesHandler.Apply(updates))
            return false;

        m_RebuildingAfterScriptUpdates = true;
        RequestMsBuildScriptCompilation(restore: false, "Script updater updated source files");
        return true;
    }

    static IEnumerable<string> FindPendingScriptUpdateManifests(BuildResultMessage buildResult)
    {
        foreach (var projectResult in buildResult.GetProjectResults().Values)
        {
            foreach (var logMessage in projectResult.GetLogMessages())
            {
                if (logMessage.Code == k_PendingScriptUpdatesWarningCode)
                    yield return logMessage.File;
            }
        }

        // The host keeps a warning it cannot attribute to a project in the build-wide list.
        foreach (var logMessage in buildResult.GetLogMessages())
        {
            if (logMessage.Code == k_PendingScriptUpdatesWarningCode)
                yield return logMessage.File;
        }
    }

    static PendingScriptUpdates ReadPendingScriptUpdates(string manifestPath)
    {
        try
        {
            return JsonUtility.FromJson<PendingScriptUpdates>(File.ReadAllText(manifestPath));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Debug.LogWarning($"Could not read script updater results from {manifestPath}: {e.Message}");
            return null;
        }
    }

    // UnitySourceFileUpdatersResultHandler recognizes immutable packages by their Library/PackageCache/
    // path, and version control expects project-relative paths, as Bee's script updater reports them.
    static string ToProjectRelativePath(string path)
    {
        var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), path);
        if (Path.IsPathRooted(relativePath) || relativePath.StartsWith(".."))
            return path;

        return relativePath.Replace('\\', '/');
    }
}
