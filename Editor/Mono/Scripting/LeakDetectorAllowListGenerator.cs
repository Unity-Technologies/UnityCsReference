// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.Scripting.CodeReload
{
    internal enum AllowListGenerationMode
    {
        Merge,
        Replace,
    }

    internal static class LeakDetectorAllowListGenerator
    {
        internal const string k_GeneratedFilePath = "Editor/Mono/Scripting/LeakDetectorAllowList.gen.cs";
        internal const string k_ReportsArgument = "-alc-leak-reports";
        internal const string k_ReplaceArgument = "-alc-leak-allowlist-replace";
        internal const string k_ReportSearchPattern = "ALCLeaks*.json";

        internal const string k_UnknownOwner = "TODO: owning team";
        internal const string k_UnknownTicket = "TODO: tracking ticket";

        internal static void Regenerate()
        {
            var arguments = Environment.GetCommandLineArgs();
            var mode = HasArgument(arguments, k_ReplaceArgument) ? AllowListGenerationMode.Replace : AllowListGenerationMode.Merge;

            List<LeakIdentity> observed;
            if (HasArgument(arguments, k_ReportsArgument))
            {
                var errors = new List<string>();
                var reportPaths = CollectReportPaths(GetArgumentValues(arguments, k_ReportsArgument), errors);
                if (reportPaths.Count == 0 && errors.Count == 0)
                    errors.Add($"{k_ReportsArgument} matched no {k_ReportSearchPattern} report.");

                observed = ReadIdentitiesFromReports(reportPaths, errors);
                if (errors.Count > 0)
                    throw new InvalidDataException($"Not writing {k_GeneratedFilePath}, {errors.Count} requested report input(s) are invalid or unreadable:\n{string.Join("\n", errors)}");

                Debug.Log($"Read {observed.Count} leaks from {reportPaths.Count} report(s).");
            }
            else
            {
                observed = LeakDetectorAllowList.GetLeakIdentities(AssemblyLoadContextLeakDetector.RunLeakDetectionWithFullReport());
                Debug.LogWarning($"No {k_ReportsArgument} given, so the allow list is generated from a detection run on this project only. Known leaks that this project does not reproduce will be reported as unused, and with {k_ReplaceArgument} they would be dropped.");
            }

            var existing = LeakDetectorAllowListEntries.Entries;
            File.WriteAllText(k_GeneratedFilePath, Generate(observed, existing, mode));
            Debug.Log($"Wrote {k_GeneratedFilePath} in {mode} mode. Fill in the owning team and tracking ticket for any '{k_UnknownTicket}' entry before committing.");
        }

        internal static string Generate(IEnumerable<LeakIdentity> observed, IReadOnlyList<LeakAllowListEntry> existing, AllowListGenerationMode mode)
        {
            var attribution = new Dictionary<LeakIdentity, LeakAllowListEntry>();
            foreach (var entry in existing)
                attribution[entry.Identity] = entry;

            var sorted = new SortedSet<LeakIdentity>();
            foreach (var identity in observed)
            {
                if (identity.IsValid)
                    sorted.Add(identity);
            }

            if (mode == AllowListGenerationMode.Merge)
            {
                foreach (var entry in existing)
                {
                    if (entry.Identity.IsValid)
                        sorted.Add(entry.Identity);
                }
            }

            var source = new StringBuilder();
            source.Append(k_Header);

            foreach (var identity in sorted)
            {
                if (!attribution.TryGetValue(identity, out var entry))
                    entry = new LeakAllowListEntry(identity.AssemblyName, identity.TypeName, k_UnknownOwner, k_UnknownTicket);

                source.Append("            new LeakAllowListEntry(");
                source.Append(Quote(identity.AssemblyName)).Append(", ");
                source.Append(Quote(identity.TypeName)).Append(", ");
                source.Append(Quote(entry.OwningTeam)).Append(", ");
                source.Append(Quote(entry.TrackingTicket)).Append("),\n");
            }

            source.Append(k_Footer);
            return source.ToString();
        }

        internal static List<string> CollectReportPaths(IEnumerable<string> inputs, List<string> errors)
        {
            var paths = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var input in inputs)
            {
                if (Directory.Exists(input))
                {
                    var files = Directory.GetFiles(input, k_ReportSearchPattern, SearchOption.AllDirectories);
                    if (files.Length == 0)
                        errors.Add($"{k_ReportsArgument} directory '{input}' contains no {k_ReportSearchPattern} report.");

                    foreach (var file in files)
                        paths.Add(Path.GetFullPath(file));
                }
                else if (File.Exists(input))
                {
                    paths.Add(Path.GetFullPath(input));
                }
                else
                {
                    errors.Add($"{k_ReportsArgument} '{input}' is neither a file nor a directory.");
                }
            }

            return new List<string>(paths);
        }

        internal static List<LeakIdentity> ReadIdentitiesFromReports(IEnumerable<string> reportPaths, List<string> errors)
        {
            var identities = new List<LeakIdentity>();
            foreach (var path in reportPaths)
            {
                try
                {
                    identities.AddRange(ParseReport(File.ReadAllText(path)));
                }
                catch (Exception exception)
                {
                    errors.Add($"Could not read the ALC leak report '{path}': {exception.Message}");
                }
            }

            return identities;
        }

        internal static List<LeakIdentity> ParseReport(string json)
        {
            var leaks = new JSONParser(json).Parse().Get("ALCLeaks.Leaks");
            if (!leaks.IsList())
                throw new InvalidDataException("'ALCLeaks.Leaks' is missing or is not an array.");

            var identities = new List<LeakIdentity>();
            foreach (var leak in leaks.AsList())
            {
                var pathToRoot = leak.Get("PathToRoot");
                if (!pathToRoot.IsList())
                    throw new InvalidDataException("A leak has no 'PathToRoot' array.");

                if (pathToRoot.AsList().Count == 0)
                    continue;

                var leaked = pathToRoot.AsList()[0];
                identities.Add(new LeakIdentity(leaked.Get("AssemblyName").AsString(true), leaked.Get("TypeName").AsString(true)));
            }

            return identities;
        }

        static IEnumerable<string> GetArgumentValues(string[] arguments, string name)
        {
            for (int i = 0; i < arguments.Length - 1; ++i)
            {
                if (!string.Equals(arguments[i], name, StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var value in arguments[i + 1].Split(';', StringSplitOptions.RemoveEmptyEntries))
                    yield return value.Trim();
            }
        }

        static bool HasArgument(string[] arguments, string name)
        {
            foreach (var argument in arguments)
            {
                if (string.Equals(argument, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        static string Quote(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

        const string k_Header =
            "// Generated file. Entries are produced by LeakDetectorAllowListGenerator from the\n" +
            "// Logs/ALCLeaks*.json reports a run writes, normally a set downloaded from CI job artifacts:\n" +
            "// most of these leaks do not reproduce on a single machine in a single project.\n" +
            "//\n" +
            "//   Unity.exe -projectPath <any project> -batchmode -quit \\\n" +
            "//     -executeMethod UnityEditor.Scripting.CodeReload.LeakDetectorAllowListGenerator.Regenerate \\\n" +
            "//     -alc-leak-reports <folder of downloaded reports>\n" +
            "//\n" +
            "// That unions what the reports observed with what is already here. Pass\n" +
            "// -alc-leak-allowlist-replace to rebaseline from a complete artifact set instead. New entries\n" +
            "// are emitted with TODO placeholders - fill in the owning team and tracking ticket.\n" +
            "//\n" +
            "// Deleting entries by hand is the expected way to retire them: when a leak is fixed, remove its\n" +
            "// entries as part of that fix rather than regenerating the whole file. Only use the generator to\n" +
            "// add entries, so their identities come from a measured run rather than from guesswork.\n" +
            "//\n" +
            "// TEMPORARY TECHNICAL DEBT. Every entry below is a known, pre-existing AssemblyLoadContext leak\n" +
            "// (COPT-3929) that the ALC leak gate is told to tolerate so the gate can be enabled at all. An\n" +
            "// allow-listed leak still leaks - the ALC does not unload.\n" +
            "//\n" +
            "// This whole file and the filter that reads it are deleted by TASK-24 of the UnityALC\n" +
            "// series (epic SCP-1987) once the list is empty. Do not build anything on top of it.\n" +
            "//\n" +
            "// Adding an entry requires a named reviewer: add @christianb to the pull request. Removing an entry\n" +
            "// needs no review - it is what we are working towards.\n" +
            "\n" +
            "#if ENABLE_CORECLR\n" +
            "\n" +
            "using System.Collections.Generic;\n" +
            "using Unity.Scripting.LifecycleManagement;\n" +
            "\n" +
            "namespace UnityEditor.Scripting.CodeReload\n" +
            "{\n" +
            "    internal static class LeakDetectorAllowListEntries\n" +
            "    {\n" +
            "        [NoAutoStaticsCleanup]\n" +
            "        internal static readonly IReadOnlyList<LeakAllowListEntry> Entries = new LeakAllowListEntry[]\n" +
            "        {\n";

        const string k_Footer =
            "        };\n" +
            "    }\n" +
            "}\n" +
            "\n" +
            "#endif // ENABLE_CORECLR\n";
    }
}

