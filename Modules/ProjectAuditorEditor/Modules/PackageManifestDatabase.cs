// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Core;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Modules
{
    /// <summary>
    /// Holds the relevant contents of the "combined manifest" JSON files (one per Major.Minor editor line, e.g. "6000.1").
    /// </summary>
    public class PackageManifestDatabase
    {
        // TEMP: combined manifests are hosted in the rules package, one file per Major.Minor editor
        // line, until Unity hosts and updates this data in a central location.
        const string k_CombinedManifestsFolder = "com.unity.editor-manifests.combined";

        internal struct PackageVersionInfo
        {
            public string Version;
            public string MinimumVersion;
            public string DeprecatedMessage;
            public bool RemovedOnProjectUpgrade;

            // Presence of a "deprecated" message marks the package as deprecated.
            public bool Deprecated => !string.IsNullOrEmpty(DeprecatedMessage);
        }

        internal class EditorVersionManifest
        {
            // Package name -> version info, taken from the latest patch (last entry in "versions").
            public Dictionary<string, PackageVersionInfo> Packages;

            // Union of every package name that appears in any patch's "removed" list.
            public List<string> RemovedPackages;
        }

        /// <summary>
        /// Package status at a given version.
        /// </summary>
        public enum PackageManifestStatus
        {
            /// <summary>
            /// The package is active.
            /// </summary>
            Active,
            /// <summary>
            /// The package has been deprecated.
            /// </summary>
            Deprecated,
            /// <summary>
            /// The package has been removed.
            /// </summary>
            Removed
        }

        /// <summary>
        /// Package info at a given version.
        /// </summary>
        public class PackageManifestInfo
        {
            /// <summary>
            /// Package status.
            /// </summary>
            public PackageManifestStatus Status { get; internal set; }

            /// <summary>
            /// Package version.
            /// </summary>
            public string Version { get; internal set; }

            /// <summary>
            /// Package minimum version.
            /// </summary>
            public string MinimumVersion { get; internal set; }

            /// <summary>
            /// The deprecation message, if the package is deprecated. Null otherwise.
            /// </summary>
            public string DeprecatedMessage { get; internal set; }

            /// <summary>
            /// True if the package will be removed from the project automatically when the project is upgraded to this editor version.
            /// </summary>
            public bool RemovedOnProjectUpgrade { get; internal set; }
        }

#pragma warning disable CS0649
        [Serializable]
        private sealed class SerializedPackage
        {
            public string name;
            public string version;
            public string minimumVersion;
            public string deprecated;
            public bool removeOnProjectUpgrade;
        }

        [Serializable]
        private sealed class SerializedManifest
        {
            public string version;
            public SerializedPackage[] packages;
            public string[] removed;
        }

        // Overrides apply to the whole editor version line (not to a specific patch), and can name
        // packages that never appear in any patch's "packages" list.
        [Serializable]
        private sealed class SerializedOverride
        {
            public string name;
            public string deprecated;
        }

        [Serializable]
        private sealed class SerializedFile
        {
            public string editorVersionPrefix;
            public SerializedManifest[] manifests;
            public SerializedOverride[] overrides;
        }
#pragma warning restore CS0649

        // keyed by Major.Minor ("6000.1"), matching ObsoleteLibrary.UnityVersions
        readonly Dictionary<string, EditorVersionManifest> m_Manifests = new Dictionary<string, EditorVersionManifest>();

        [NoAutoStaticsCleanup] // Lazy cache; the parsed data doesn't change during script-only reloads, so keep it across audits and code reloads.
        static PackageManifestDatabase s_Instance;

        /// <summary>
        /// Lazily-created, cached database of package version info.
        /// </summary>
        internal static PackageManifestDatabase Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new PackageManifestDatabase();
                    s_Instance.Load();
                }

                return s_Instance;
            }
        }
        
        internal PackageManifestDatabase()
        {
        }

        internal void Load()
        {
            if (!ProjectAuditorRulesPackage.IsInstalled)
                return;

            var manifestsFolder = Path.Combine(ProjectAuditor.s_RulesDataPath, k_CombinedManifestsFolder);

            // Only load manifests for versions we might report upgrade issues for; there's no need to
            // parse data for versions ObsoleteLibrary has already filtered out.
            foreach (var editorVersionPrefix in ObsoleteLibrary.UnityVersions)
            {
                var filename = Path.Combine(manifestsFolder, editorVersionPrefix + ".json");
                if (!File.Exists(filename))
                    continue;

                Parse(File.ReadAllText(filename));
            }
        }

        internal void Parse(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            var file = JsonUtility.FromJson<SerializedFile>(json);
            if (file == null || string.IsNullOrEmpty(file.editorVersionPrefix) || file.manifests == null)
                return;

            // Only store data for versions in ObsoleteLibrary.UnityVersions (already filtered to
            // versions newer than the running editor).
            if (Array.IndexOf(ObsoleteLibrary.UnityVersions, file.editorVersionPrefix) < 0)
                return;

            var manifest = new EditorVersionManifest
            {
                Packages = new Dictionary<string, PackageVersionInfo>(),
                RemovedPackages = new List<string>()
            };

            // Each patch manifest is a delta: the base patch lists the full package set and every later
            // patch lists only the packages that changed. Replay them in ascending patch order (later
            // patch wins) to reconstruct the effective latest state.
            var orderedManifests = new List<SerializedManifest>(file.manifests);
            orderedManifests.Sort((a, b) => CompareEditorVersions(a?.version, b?.version));

            foreach (var patchManifest in orderedManifests)
            {
                if (patchManifest == null)
                    continue;

                if (patchManifest.packages != null)
                {
                    foreach (var package in patchManifest.packages)
                    {
                        if (package == null || string.IsNullOrEmpty(package.name))
                            continue;

                        // Delta entries can be partial (only the changed field is present), so merge into the accumulated record.
                        manifest.Packages.TryGetValue(package.name, out var info);

                        if (!string.IsNullOrEmpty(package.version))
                            info.Version = package.version;
                        if (!string.IsNullOrEmpty(package.minimumVersion))
                            info.MinimumVersion = package.minimumVersion;
                        if (!string.IsNullOrEmpty(package.deprecated))
                            info.DeprecatedMessage = package.deprecated;
                        if (package.removeOnProjectUpgrade)
                            info.RemovedOnProjectUpgrade = true;

                        manifest.Packages[package.name] = info;
                    }
                }

                // "removed" entries appear only in the exact patch a package was removed in. Drop the
                // package from the effective set and record it in the per-version removed list.
                if (patchManifest.removed != null)
                {
                    foreach (var removedName in patchManifest.removed)
                    {
                        if (string.IsNullOrEmpty(removedName))
                            continue;

                        manifest.Packages.Remove(removedName);

                        if (!manifest.RemovedPackages.Contains(removedName))
                            manifest.RemovedPackages.Add(removedName);
                    }
                }
            }

            // Overrides apply on top of the merged patch data, and can deprecate packages that never
            // appear in any patch's "packages" list (e.g. packages removed from discovery entirely).
            if (file.overrides != null)
            {
                foreach (var over in file.overrides)
                {
                    if (over == null || string.IsNullOrEmpty(over.name) || string.IsNullOrEmpty(over.deprecated))
                        continue;

                    if (manifest.RemovedPackages.Contains(over.name))
                        continue;

                    manifest.Packages.TryGetValue(over.name, out var info);
                    info.DeprecatedMessage = over.deprecated;
                    manifest.Packages[over.name] = info;
                }
            }

            m_Manifests[file.editorVersionPrefix] = manifest;
        }

        // Compares Unity editor version strings, e.g. "6000.1.17f1" vs "6000.1.8f1".
        // Utility.CompareVersions targets package SemVer and doesn't understand the "17f1" patch suffix.
        static int CompareEditorVersions(string lhs, string rhs)
        {
            UnityVersion.TryParse(lhs, out var left);
            UnityVersion.TryParse(rhs, out var right);
            return (left ?? default).CompareTo(right ?? default);
        }

        /// <summary>
        /// Queries the package version database.
        /// </summary>
        /// <param name="editorVersionPrefix">The Unity version to query. Usually a future version, to detect what will change during an upgrade.</param>
        /// <param name="packageName">The package to query.</param>
        /// <param name="manifest">The information about the package in the desired version.</param>
        /// <returns>Returns true if the database contains information about the supplied package.</returns>
        public bool TryGetManifestInfo(string editorVersionPrefix, string packageName, out PackageManifestInfo manifest)
        {
            if (!m_Manifests.TryGetValue(editorVersionPrefix, out var versionManifest))
            {
                manifest = null;
                return false;
            }

            if (versionManifest.RemovedPackages.Contains(packageName))
            {
                manifest = new PackageManifestInfo { Status = PackageManifestStatus.Removed };
                return true;
            }
            else if (versionManifest.Packages.TryGetValue(packageName, out var info))
            {
                manifest = new PackageManifestInfo();

                if (info.Deprecated)
                    manifest.Status = PackageManifestStatus.Deprecated;
                else
                    manifest.Status = PackageManifestStatus.Active;

                manifest.Version = info.Version;
                manifest.MinimumVersion = info.MinimumVersion;
                manifest.DeprecatedMessage = info.DeprecatedMessage;
                manifest.RemovedOnProjectUpgrade = info.RemovedOnProjectUpgrade;
                return true;
            }
            else
            {
                manifest = null;
                return false;
            }
        }
    }
}
