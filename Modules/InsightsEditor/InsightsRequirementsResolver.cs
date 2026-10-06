// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.EngineDiagnostics;
using UnityEditor.PackageManager;

namespace UnityEditor.InsightsEditor
{
    static class InsightsRequirementsResolver
    {
        const string k_StartupSyncDoneKey = "InsightsRequirementsResolver.StartupSyncDone";

        [InitializeOnLoadMethod]
        static void SyncOnStartupAndSubscribeToPackageRegistrationChanges()
        {
            // Resolved once per Editor session and kept current on package registration changes
            // rather than rescanned on every domain reload; builds re-resolve at serialization
            // time (see InsightsSettings::Transfer).
            if (!SessionState.GetBool(k_StartupSyncDoneKey, false))
            {
                SessionState.SetBool(k_StartupSyncDoneKey, true);
                SyncRequirements();
            }

            PackageManager.Events.registeredPackages += OnPackagesRegistered;
        }

        internal static void SyncRequirements()
        {
            EngineDiagnosticsSettings.SetCollectionRequirements(ResolveRequirements().ToArray());
            EngineDiagnosticsSettings.SetProjectPackages(ScanProjectPackages().ToArray());
        }

        static void OnPackagesRegistered(PackageRegistrationEventArgs args)
        {
            SyncRequirements();
        }

        internal static List<CollectionRequirement> ResolveRequirements()
        {
            var requirementsByPackage = new Dictionary<string, CollectionRequirement>();
            foreach (var assembly in UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies())
            {
                try
                {
                    if (assembly.IsDynamic || !assembly.IsDefined(typeof(RequiresInsightsAttribute), false))
                        continue;
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogWarning($"[Insights] Could not read RequiresInsights declarations from {assembly.GetName().Name}: {e.Message}");
                    continue;
                }

                // Identity comes from the package that owns the attributed assembly, so a package
                // cannot declare a requirement on behalf of another package.
                var package = PackageManager.PackageInfo.FindForAssembly(assembly);
                if (package == null)
                {
                    UnityEngine.Debug.LogWarning($"[Insights] Ignoring RequiresInsights declaration in {assembly.GetName().Name}: the assembly does not belong to a package.");
                    continue;
                }

                if (!IsValidRequirementValue(package.name) || !IsValidRequirementValue(package.version))
                {
                    UnityEngine.Debug.LogWarning($"[Insights] Ignoring RequiresInsights declaration from package '{package.name}': name or version '{package.version}' is empty or contains unsupported characters.");
                    continue;
                }

                if (!requirementsByPackage.ContainsKey(package.name))
                    requirementsByPackage.Add(package.name, new CollectionRequirement { packageName = package.name, semVer = package.version });
            }

            var requirements = new List<CollectionRequirement>(requirementsByPackage.Values);
            SortByPackageName(requirements);
            return requirements;
        }

        // Third-party package names are project-identifying data the payload must not carry.
        // The trailing dot enforces a namespace boundary so third-party names that merely start
        // with "com.unity" (e.g. "com.unityfoo.analytics") don't match.
        const string k_ReportedPackagePrefix = "com.unity.";

        internal static bool IsReportedPackageName(string packageName)
        {
            return packageName != null && packageName.StartsWith(k_ReportedPackagePrefix, StringComparison.Ordinal);
        }

        internal static List<CollectionRequirement> ScanProjectPackages()
        {
            var allPackages = PackageManager.PackageInfo.GetAllRegisteredPackages();
            var packages = new List<CollectionRequirement>(allPackages.Length);
            foreach (var package in allPackages)
            {
                if (!IsReportedPackageName(package.name))
                    continue;

                if (!IsValidRequirementValue(package.name) || !IsValidRequirementValue(package.version))
                    continue;

                packages.Add(new CollectionRequirement { packageName = package.name, semVer = package.version });
            }

            SortByPackageName(packages);
            return packages;
        }

        // Sort so serialized build data and request payloads are deterministic.
        internal static void SortByPackageName(List<CollectionRequirement> requirements)
        {
            requirements.Sort((a, b) => string.CompareOrdinal(a.packageName, b.packageName));
        }

        // Must accept exactly what ConfigurationRequest::IsSafeJsonValue accepts.
        static bool IsValidRequirementValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            foreach (var c in value)
            {
                var isAsciiAlphanumeric = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
                if (!isAsciiAlphanumeric && c != '.' && c != '-' && c != '_' && c != '+' && c != '/' && c != '@')
                    return false;
            }

            return true;
        }
    }
}
