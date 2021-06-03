// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace UnityEditor
{
    internal class ArtifactDifferenceReporter
    {
        delegate void ArtifactDifferenceMessageFormatter(ref ArtifactInfoDifference diff, List<string> msgsList);

        internal enum DiffType
        {
            None, Modified, Added, Removed
        }

        internal class ArtifactInfoDifference
        {
            public readonly string key;
            public readonly DiffType diffType;
            public readonly ArtifactInfo oldArtifactInfo;
            public readonly ArtifactInfo newArtifactInfo;
            public string message;
            public string categoryKey;

            internal ArtifactInfoDifference(string key, DiffType diffType, ArtifactInfo oldArtifactInfo, ArtifactInfo newArtifactInfo)
            {
                this.key = key;
                this.diffType = diffType;
                this.oldArtifactInfo = oldArtifactInfo;
                this.newArtifactInfo = newArtifactInfo;
                this.message = "";
            }

            internal ArtifactInfoDifference(ArtifactInfoDifference other)
            {
                this.key = other.key;
                this.diffType = other.diffType;
                this.oldArtifactInfo = other.oldArtifactInfo;
                this.newArtifactInfo = other.newArtifactInfo;
                this.message = other.message;
            }
        }

        private List<ArtifactInfoDifference> m_AllDiffs;

        public static string kGlobal_artifactFormatVersion = "Global/artifactFormatVersion"; //Manual check
        public static string kGlobal_allImporterVersion = "Global/allImporterVersion"; //Manual check
        public static string kImportParameter_ImporterType = "ImportParameter/ImporterType";
        public static string kImporterRegister_ImporterVersion = "ImporterRegistry/ImporterVersion";
        public static string kImporterRegistry_PostProcessorVersionHash = "ImporterRegistry/PostProcessorVersionHash";
        public static string kImportParameter_NameOfAsset = "ImportParameter/NameOfAsset";
        public static string kSourceAsset_GuidOfPathLocation = "SourceAsset/GuidOfPathLocation";
        public static string kSourceAsset_HashOfSourceAssetByGUID = "SourceAsset/HashOfSourceAssetByGUID";
        public static string kSourceAsset_MetaFileHash = "SourceAsset/MetaFileHash";
        public static string kArtifact_HashOfContent = "Artifact/HashOfContent";
        public static string kArtifact_FileIdOfMainObject = "Artifact/Property"; //Manual check
        public static string kImportParameter_Platform = "ImportParameter/Platform";
        public static string kEnvironment_TextureImportCompression = "Environment/TextureImportCompression";
        public static string kEnvironment_ColorSpace = "Environment/ColorSpace";
        public static string kEnvironment_GraphicsAPIMask = "Environment/GraphicsAPIMask";
        public static string kEnvironment_ScriptingRuntimeVersion = "Environment/ScriptingRuntimeVersion"; //Manual check
        public static string kEnvironment_CustomDependency = "Environment/CustomDependency";
        public static string kImportParameter_PlatformGroup = "ImportParameter/BuildTargetPlatformGroup";
        public static string kIndeterministicImporter = "ImporterRegistry/IndeterministicImporter";

        internal IEnumerable<ArtifactInfoDifference> GetAllDifferences()
        {
            return m_AllDiffs;
        }

        internal IEnumerable<string> GatherDifferences(ArtifactInfo oldInfo, ArtifactInfo newInfo)
        {
            IEnumerable<ArtifactInfoDifference> ignoreDifferences = null;
            var messages = GatherDifferences(oldInfo, newInfo, ref ignoreDifferences);
            return messages;
        }

        internal IEnumerable<string> GatherDifferences(ArtifactInfo oldInfo, ArtifactInfo newInfo, ref IEnumerable<ArtifactInfoDifference> differences)
        {
            m_AllDiffs = newInfo.dependencies.Except(oldInfo.dependencies)
                .Concat(oldInfo.dependencies.Except(newInfo.dependencies))
                .Select(e => e.Key)
                .Distinct()
                .Select(key =>
                {
                    // Default is modified, since all non-changed dependencies are filtered out
                    var diffType = DiffType.Modified;

                    // check if it is a new dependency
                    if (!oldInfo.dependencies.ContainsKey(key))
                        diffType = DiffType.Added;

                    // check if dependency was removed from the asset
                    if (!newInfo.dependencies.ContainsKey(key))
                        diffType = DiffType.Removed;

                    return new ArtifactInfoDifference(key, diffType, oldInfo, newInfo);
                }).ToList();

            var msgs = new List<string>();
            for (int i = 0; i < m_AllDiffs.Count(); ++i)
            {
                var artifactInfoDiff = m_AllDiffs[i];
                foreach (var formatter in getMessageFormatters())
                {
                    // we're invoking all of the formatters if the condition is true
                    // formatter methods should probably return the value instead of adding it to the list
                    // in that case we could yield it here
                    if (artifactInfoDiff.key.StartsWith(formatter.startsWith, StringComparison.Ordinal))
                    {
                        formatter.formatterMethod.Invoke(ref artifactInfoDiff, msgs);
                        artifactInfoDiff.categoryKey = formatter.startsWith;
                    }
                }
            }

            if (m_AllDiffs.Count() == 0 && newInfo.artifactID != oldInfo.artifactID)
            {
                var indeterministicImporterDifference = new ArtifactInfoDifference(kIndeterministicImporter, DiffType.Modified, oldInfo, newInfo);
                indeterministicImporterDifference.message = "the importer used is non-deterministic, as it has produced a different result even though all the dependencies are the same";
                msgs.Add(indeterministicImporterDifference.message);
            }

            differences = m_AllDiffs;

            return msgs;
        }

        private static IEnumerable<(string startsWith, ArtifactDifferenceMessageFormatter formatterMethod)> getMessageFormatters()
        {
            yield return (kEnvironment_CustomDependency, EnvironmentCustomDependencyDifference);
            yield return (kEnvironment_TextureImportCompression, TextureCompressionModified);
            yield return (kEnvironment_ColorSpace, ColorSpaceModified);
            yield return (kImportParameter_ImporterType, ImporterTypeModified);
            yield return (kImporterRegister_ImporterVersion, ImporterVersionModified);
            yield return (kImportParameter_NameOfAsset, NameOfAssetModified);
            yield return (kSourceAsset_HashOfSourceAssetByGUID, HashOfSourceAssetModified);
            yield return (kSourceAsset_MetaFileHash, MetaFileHashModified);
            yield return (kArtifact_HashOfContent, ArtifactHashOfContentDifference);
            yield return (kImportParameter_Platform, PlatformDependencyModified);
            yield return (kSourceAsset_GuidOfPathLocation, GuidOfPathLocationModified);
            yield return (kImporterRegistry_PostProcessorVersionHash, PostProcessorVersionHashModified);
            yield return (kEnvironment_GraphicsAPIMask, GraphicsAPIMaskModified);
            yield return (kEnvironment_ScriptingRuntimeVersion, ScriptingRuntimeVersionModified);
            yield return (kGlobal_artifactFormatVersion, GlobalArtifactFormatVersionModified);
            yield return (kGlobal_allImporterVersion, GlobalAllImporterVersionModified);
            yield return (kArtifact_FileIdOfMainObject, ArtifactFileIdOfMainObjectModified);
            yield return (kImportParameter_PlatformGroup, PlatformGroupModified);
        }

        private static void PlatformGroupModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            if (diff.diffType == DiffType.Modified)
            {
                var oldPlatformGroup = diff.oldArtifactInfo.dependencies[diff.key].value;
                var newPlatformGroup = diff.newArtifactInfo.dependencies[diff.key].value;
                diff.message = $"the PlatformGroup value was changed from '{oldPlatformGroup}' to '{newPlatformGroup}'";
                msgsList.Add(diff.message);
            }
            else if (diff.diffType == DiffType.Added)
            {
                var platformGroup = diff.newArtifactInfo.dependencies[diff.key].value;
                var assetName = GetAssetName(diff.newArtifactInfo);
                diff.message = $"a dependency on the PlatformGroup '{platformGroup}' was added";
                msgsList.Add(diff.message);
            }
            else if (diff.diffType == DiffType.Removed)
            {
                var platformGroup = diff.oldArtifactInfo.dependencies[diff.key].value;
                var assetName = GetAssetName(diff.newArtifactInfo);
                diff.message = $"a dependency on the PlatformGroup '{platformGroup}' was removed";
                msgsList.Add(diff.message);
            }
        }

        private static void ArtifactFileIdOfMainObjectModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            var propertyName = diff.key.Split('/')[2];
            if (diff.diffType == DiffType.Modified)
            {
                var oldVersion = diff.oldArtifactInfo.dependencies[diff.key].value;
                var newVersion = diff.newArtifactInfo.dependencies[diff.key].value;
                var assetName = GetAssetName(diff.newArtifactInfo);
                diff.message = $"the property '{propertyName}' was changed from '{oldVersion}' to '{newVersion}', which is registered as a dependency of '{assetName}'";
                msgsList.Add(diff.message);
            }
            else if (diff.diffType == DiffType.Added)
            {
                diff.message = $"a dependency on the property '{propertyName}' was added";
                msgsList.Add(diff.message);
            }
            else if (diff.diffType == DiffType.Removed)
            {
                diff.message = $"a dependency on the property '{propertyName}' was removed";
                msgsList.Add(diff.message);
            }
        }

        private static void GlobalAllImporterVersionModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            if (diff.diffType == DiffType.Modified)
            {
                var oldVersion = diff.oldArtifactInfo.dependencies[diff.key].value;
                var newVersion = diff.newArtifactInfo.dependencies[diff.key].value;
                diff.message = $"the Global Importer version value was changed from '{oldVersion}' to '{newVersion}'";
                msgsList.Add(diff.message);
            }
            else
            {
                Debug.LogError($"{kGlobal_allImporterVersion} cannot be added or removed as a dependency");
            }
        }

        private static void GlobalArtifactFormatVersionModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            if (diff.diffType == DiffType.Modified)
            {
                var oldVersion = diff.oldArtifactInfo.dependencies[diff.key].value;
                var newVersion = diff.newArtifactInfo.dependencies[diff.key].value;
                diff.message = $"the global artifact format version changed from '{oldVersion}' to '{newVersion}'";
                msgsList.Add(diff.message);
            }
            else
            {
                Debug.LogError($"{kGlobal_artifactFormatVersion} cannot be added or removed as a dependency");
            }
        }

        private static void ScriptingRuntimeVersionModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            if (diff.diffType == DiffType.Modified)
            {
                var oldVersion = diff.oldArtifactInfo.dependencies[diff.key].value;
                var newVersion = diff.newArtifactInfo.dependencies[diff.key].value;
                diff.message = $"the project's ScriptingRuntimeVersion changed from '{oldVersion}' to '{newVersion}";
                msgsList.Add(diff.message);
            }
            else if (diff.diffType == DiffType.Added)
            {
                var addedVersion = diff.newArtifactInfo.dependencies[diff.key].value;
                diff.message = $"a dependency on the selected Scripting Runtime Version '{addedVersion}' was added";
                msgsList.Add(diff.message);
            }
            else if (diff.diffType == DiffType.Removed)
            {
                var removedVersion = diff.newArtifactInfo.dependencies[diff.key].value;
                diff.message = $"a dependency on the selected Scripting Runtime Version '{removedVersion}' was removed";
                msgsList.Add(diff.message);
            }
        }

        private static void GraphicsAPIMaskModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            //UnityEngine.Rendering.GraphicsDeviceType
            if (diff.diffType == DiffType.Modified)
            {
                var assetName = GetAssetName(diff.newArtifactInfo);
                var oldGraphicsAPIValue = diff.oldArtifactInfo.dependencies[diff.key].value;
                var newGraphicsAPIValue = diff.newArtifactInfo.dependencies[diff.key].value;
                diff.message = $"the project's GraphicsAPIMask changed from '{oldGraphicsAPIValue}' to '{newGraphicsAPIValue}'";
                msgsList.Add(diff.message);
            }
            else if (diff.diffType == DiffType.Added)
            {
                var assetName = GetAssetName(diff.newArtifactInfo);
                var newGraphicsAPIValue = diff.newArtifactInfo.dependencies[diff.key].value;
                diff.message = $"a dependency on the Graphics API '{newGraphicsAPIValue}' was added to '{assetName}'";
                msgsList.Add(diff.message);
            }
            else if (diff.diffType == DiffType.Removed)
            {
                var assetName = GetAssetName(diff.newArtifactInfo);
                var newGraphicsAPIValue = diff.newArtifactInfo.dependencies[diff.key].value;
                diff.message = $"the dependency on the Graphics API '{newGraphicsAPIValue}' was removed from '{assetName}'";
                msgsList.Add(diff.message);
            }
        }

        private static void PostProcessorVersionHashModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            var oldPostProcessorKey = diff.oldArtifactInfo.dependencies.Keys.FirstOrDefault(key => key.StartsWith(kImporterRegistry_PostProcessorVersionHash, StringComparison.Ordinal));

            var newPostProcessorKey = diff.newArtifactInfo.dependencies.Keys.FirstOrDefault(key => key.StartsWith(kImporterRegistry_PostProcessorVersionHash, StringComparison.Ordinal));

            if (!string.IsNullOrEmpty(oldPostProcessorKey) && !string.IsNullOrEmpty(newPostProcessorKey))
            {
                var assetName = GetAssetName(diff.newArtifactInfo);
                diff.message = $"a PostProcessor associated with '{assetName}' changed from '{oldPostProcessorKey}' to '{newPostProcessorKey}'";
                msgsList.Add(diff.message);
            }
            else
            {
                if (diff.diffType == DiffType.Removed)
                {
                    var assetName = GetAssetName(diff.newArtifactInfo);
                    var key = diff.key;
                    var start = key.LastIndexOf("/") + 1;

                    var postProcessor = key.Substring(start, key.Length - start);

                    diff.message = $"the PostProcessor '{postProcessor}' associated with '{assetName}' was removed as a dependency";
                    msgsList.Add(diff.message);
                }
                else if (diff.diffType == DiffType.Added)
                {
                    var assetName = GetAssetName(diff.newArtifactInfo);
                    var key = diff.key;
                    var start = key.LastIndexOf("/") + 1;

                    var postProcessor = key.Substring(start, key.Length - start);
                    diff.message = $"the PostProcessor '{postProcessor}' was added as a dependency for '{assetName}'";
                    msgsList.Add(diff.message);
                }
            }
        }

        private static void GuidOfPathLocationModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            if (diff.diffType == DiffType.Added)
            {
                var entry = diff.newArtifactInfo.dependencies[diff.key];
                string guid = entry.value.ToString();

                var pathOfDependency = AssetDatabase.GUIDToAssetPath(guid);

                var assetName = GetAssetName(diff.newArtifactInfo);

                diff.message = $"a dependency on '{pathOfDependency}' was added to '{assetName}'";
                msgsList.Add(diff.message);
            }
            else if (diff.diffType == DiffType.Removed)
            {
                var entry = diff.oldArtifactInfo.dependencies[diff.key];
                string guid = entry.value.ToString();

                var pathOfDependency = AssetDatabase.GUIDToAssetPath(guid);

                var assetName = GetAssetName(diff.newArtifactInfo);
                diff.message = $"a dependency on '{pathOfDependency}' was removed from '{assetName}'";
                msgsList.Add(diff.message);
            }
        }

        private static void GuidOfPathLocationModifiedViaHashOfSourceAsset(ref ArtifactInfoDifference diff, List<string> msgsList, string pathOfDependency)
        {
            if (diff.diffType == DiffType.Modified)
            {
                var assetName = GetAssetName(diff.newArtifactInfo);
                diff.message = $"the asset '{pathOfDependency}' was changed, which is registered as a dependency of '{assetName}'";
                msgsList.Add(diff.message);
            }
            else if (diff.diffType == DiffType.Added)
            {
                var assetName = GetAssetName(diff.newArtifactInfo);
                diff.message = $"a dependency on '{pathOfDependency}' was added to '{assetName}'";
                msgsList.Add(diff.message);
            }
            else if (diff.diffType == DiffType.Removed)
            {
                var assetName = diff.oldArtifactInfo.dependencies[kImportParameter_NameOfAsset].value;
                diff.message = $"a dependency on '{pathOfDependency}' was removed from '{assetName}'";
                msgsList.Add(diff.message);
            }
        }

        private static void PlatformDependencyModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            if (diff.diffType == DiffType.Removed)
            {
                var assetName = GetAssetName(diff.newArtifactInfo);
                diff.message = $"the dynamic dependency for the current build target was removed for '{assetName}'";
                msgsList.Add(diff.message);
            }
            else if (diff.diffType == DiffType.Added)
            {
                var assetName = GetAssetName(diff.newArtifactInfo);
                diff.message = $"a dynamic dependency which makes '{assetName}' dependent on the current build target was added";
                msgsList.Add(diff.message);
            }
            else if (diff.diffType == DiffType.Modified)
            {
                var oldPlatformValue = diff.oldArtifactInfo.dependencies[diff.key].value;
                var newPlatformValue = diff.newArtifactInfo.dependencies[diff.key].value;
                diff.message = $"the project's build target was changed from '{oldPlatformValue}' to '{newPlatformValue}'";
                msgsList.Add(diff.message);
            }
        }

        private static void ImporterVersionModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            if (diff.diffType == DiffType.Modified)
            {
                var oldImporterVersion = diff.oldArtifactInfo.dependencies[diff.key].value;
                var newImporterVersion = diff.newArtifactInfo.dependencies[diff.key].value;
                var importerName = diff.newArtifactInfo.dependencies[kImportParameter_ImporterType].value;
                diff.message = $"The version of the importer '{importerName}' changed from '{oldImporterVersion}' to '{newImporterVersion}'";
                msgsList.Add(diff.message);
            }
        }

        private static void ImporterTypeModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            var newImporterTypeName = diff.newArtifactInfo.dependencies[diff.key].value;
            var oldImporterTypeName = diff.oldArtifactInfo.dependencies[diff.key].value;
            var assetName = GetAssetName(diff.newArtifactInfo);
            diff.message = $"the importer associated with '{assetName}' changed from '{oldImporterTypeName}' now it is '{newImporterTypeName}'";
            msgsList.Add(diff.message);
        }

        // formatter methods below
        private static void EnvironmentCustomDependencyDifference(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            if (diff.diffType == DiffType.Added)
            {
                var newCustomDependencyValue = diff.newArtifactInfo.dependencies[diff.key].value;
                var customDependencyName = diff.key.Substring(kEnvironment_CustomDependency.Length + 1);
                diff.message = $"the custom dependency '{customDependencyName}' was added with value '{newCustomDependencyValue}'";
                msgsList.Add(diff.message);
            }
            else if (diff.diffType == DiffType.Removed)
            {
                var customDependencyName = diff.key.Substring(kEnvironment_CustomDependency.Length + 1);
                diff.message = $"the custom dependency '{customDependencyName}' was removed";
                msgsList.Add(diff.message);
            }
            else if (diff.diffType == DiffType.Modified)
            {
                var oldCustomDependencyValue = diff.oldArtifactInfo.dependencies[diff.key].value;
                var newCustomDependencyValue = diff.newArtifactInfo.dependencies[diff.key].value;
                var customDependencyName = diff.key.Substring(kEnvironment_CustomDependency.Length + 1);
                diff.message = $"the value of the custom dependency '{customDependencyName}' was changed from '{oldCustomDependencyValue}' to '{newCustomDependencyValue}'";
                msgsList.Add(diff.message);
            }
        }

        private static void ArtifactHashOfContentDifference(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            var startIndex = diff.key.IndexOf('(') + 1;
            var guid = diff.key.Substring(startIndex, 32);
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var assetName = GetAssetName(diff.newArtifactInfo);

            if (diff.diffType == DiffType.Added)
            {
                diff.message = $"the asset at '{path}' was added as a dependency of '{assetName}'";
                msgsList.Add(diff.message);
            }
            else if (diff.diffType == DiffType.Removed)
            {
                diff.message = $"the asset at '{path}' was removed as a dependency of '{assetName}'";
                msgsList.Add(diff.message);
            }
            else if (diff.diffType == DiffType.Modified)
            {
                diff.message = $"the asset at '{path}' changed, which is registered as a dependency of '{assetName}'";
                msgsList.Add(diff.message);
            }
        }

        private static void TextureCompressionModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            if (diff.diffType == DiffType.Removed)
            {
                var assetName = GetAssetName(diff.newArtifactInfo);

                diff.message = $"the asset '{assetName}' no longer depends on texture compression";
                msgsList.Add(diff.message);
            }
            else if (diff.diffType == DiffType.Added)
            {
                diff.message = $"a dependency on the project's texture compression was added";
                msgsList.Add(diff.message);
            }
            else if (diff.diffType == DiffType.Modified)
            {
                var oldTextureCompressionSetting = diff.oldArtifactInfo.dependencies[diff.key].value;
                var newTextureCompressionSetting = diff.newArtifactInfo.dependencies[diff.key].value;

                diff.message = $"the project's texture compression setting changed from {oldTextureCompressionSetting} to {newTextureCompressionSetting}";
                msgsList.Add(diff.message);
            }
        }

        private static void ColorSpaceModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            if (diff.diffType == DiffType.Removed)
            {
                var assetName = GetAssetName(diff.newArtifactInfo);
                diff.message = $"the asset '{assetName}' no longer depends color space";
                msgsList.Add(diff.message);
            }
            else if (diff.diffType == DiffType.Modified)
            {
                var oldColorSpace = diff.oldArtifactInfo.dependencies[diff.key].value;
                var newColorSpace = diff.newArtifactInfo.dependencies[diff.key].value;
                diff.message = $"the project's color space changed from {oldColorSpace} to {newColorSpace}";
                msgsList.Add(diff.message);
            }
        }

        private static void NameOfAssetModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            var oldName = diff.oldArtifactInfo.dependencies[diff.key].value;
            var newName = diff.newArtifactInfo.dependencies[diff.key].value;
            diff.message = $"the name of the asset was changed from '{oldName}' to '{newName}'";
            msgsList.Add(diff.message);
        }

        private static void HashOfSourceAssetModified(ref ArtifactInfoDifference diff, List<string> msgs)
        {
            //This means that a source asset that this asset depends on was modified, so we need to do some extra work
            //to get the name out
            if (ContainsGuidOfPathLocationChange(ref diff, out var pathOfDependency))
            {
                GuidOfPathLocationModifiedViaHashOfSourceAsset(ref diff, msgs, pathOfDependency);
                return;
            }

            var startIndex = diff.key.IndexOf('(') + 1;
            var guid = diff.key.Substring(startIndex, 32);

            diff.message = $"the source asset was changed";
            msgs.Add(diff.message);
        }

        private static bool ContainsGuidOfPathLocationChange(ref ArtifactInfoDifference diff, out string pathOfDependency)
        {
            pathOfDependency = string.Empty;

            var startIndex = diff.key.LastIndexOf('/') + 1;
            if (startIndex < 0)
                return false;

            var guid = diff.key.Substring(startIndex, 32);
            pathOfDependency = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(pathOfDependency))
                return false;

            //lowercase and replace slashes
            var goodPath = pathOfDependency.ToLower(CultureInfo.InvariantCulture);
            goodPath = goodPath.Replace("\\", "/");
            goodPath = goodPath.Replace("//", "/");

            var guidOfPathLocationKey = $"{kSourceAsset_GuidOfPathLocation}/{goodPath}";
            return diff.oldArtifactInfo.dependencies.ContainsKey(guidOfPathLocationKey);
        }

        private static void MetaFileHashModified(ref ArtifactInfoDifference diff, List<string> msgsList)
        {
            var startIndex = diff.key.LastIndexOf('/') + 1;
            var guid = diff.key.Substring(startIndex, 32);
            var path = AssetDatabase.GUIDToAssetPath(guid);

            if (diff.diffType == DiffType.Added)
            {
                diff.message = $"a dependency on the .meta file '{path}.meta' was added";
                msgsList.Add(diff.message);
            }
            else if (diff.diffType == DiffType.Removed)
            {
                diff.message = $"a dependency on the .meta file '{path}.meta' was removed";
                msgsList.Add(diff.message);
            }
            else if (diff.diffType == DiffType.Modified)
            {
                diff.message = $"the .meta file '{path}.meta' was changed";
                msgsList.Add(diff.message);
            }
        }

        private static string GetAssetName(ArtifactInfo artifactInfo)
        {
            var assetName = artifactInfo.dependencies[kImportParameter_NameOfAsset].value.ToString();
            return assetName;
        }
    }
}
