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
        delegate void ArtifactDifferenceMessageFormatter(ArtifactInfoDifference diff, List<string> msgsList);

        internal enum DiffType
        {
            Modified, Added, Removed
        }

        internal struct ArtifactInfoDifference
        {
            public readonly string key;
            public readonly DiffType diffType;
            public readonly ArtifactInfo oldArtifactInfo;
            public readonly ArtifactInfo newArtifactInfo;

            internal ArtifactInfoDifference(string key, DiffType diffType, ArtifactInfo oldArtifactInfo, ArtifactInfo newArtifactInto)
            {
                this.key = key;
                this.diffType = diffType;
                this.oldArtifactInfo = oldArtifactInfo;
                this.newArtifactInfo = newArtifactInto;
            }
        }

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
        public static string kArtifact_FileIdOfMainObject = "Artifact/FileIdOfMainObject"; //Manual check
        public static string kImportParameter_Platform = "ImportParameter/Platform";
        public static string kEnvironment_TextureImportCompression = "Environment/TextureImportCompression";
        public static string kEnvironment_ColorSpace = "Environment/ColorSpace";
        public static string kEnvironment_GraphicsAPIMask = "Environment/GraphicsAPIMask";
        public static string kEnvironment_ScriptingRuntimeVersion = "Environment/ScriptingRuntimeVersion"; //Manual check
        public static string kEnvironment_CustomDependency = "Environment/CustomDependency";


        internal static IEnumerable<string> GatherDifferences(ArtifactInfo oldInfo, ArtifactInfo newInfo)
        {
            var allDiffs = newInfo.dependencies.Except(oldInfo.dependencies)
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
                });

            var msgs = new List<string>();
            foreach (var artifactInfoDiff in allDiffs)
            {
                foreach (var formatter in getMessageFormatters())
                {
                    // we're invoking all of the formatters if the condition is true
                    // formatter methods should probably return the value instead of adding it to the list
                    // in that case we could yield it here
                    if (artifactInfoDiff.key.StartsWith(formatter.startsWith, StringComparison.Ordinal))
                        formatter.formatterMethod.Invoke(artifactInfoDiff, msgs);
                }
            }

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
        }

        private static void ArtifactFileIdOfMainObjectModified(ArtifactInfoDifference diff, List<string> msgsList)
        {
            if (diff.diffType == DiffType.Modified)
            {
                var propertyName = diff.newArtifactInfo.dependencies[diff.key].value;
                var assetName = GetAssetName(diff.newArtifactInfo);
                msgsList.Add($"the property '{propertyName}' was changed, which is registered as a dependency of '{assetName}'");
            }
            else if (diff.diffType == DiffType.Added)
            {
                var propertyName = diff.newArtifactInfo.dependencies[diff.key].value;
                msgsList.Add($"a dependency on the property '{propertyName}' was added");
            }
            else if (diff.diffType == DiffType.Removed)
            {
                var propertyName = diff.newArtifactInfo.dependencies[diff.key].value;
                msgsList.Add($"a dependency on the property '{propertyName}' was removed");
            }
        }

        private static void GlobalAllImporterVersionModified(ArtifactInfoDifference diff, List<string> msgsList)
        {
            if (diff.diffType == DiffType.Modified)
            {
                var oldVersion = diff.oldArtifactInfo.dependencies[diff.key].value;
                var newVersion = diff.newArtifactInfo.dependencies[diff.key].value;
                msgsList.Add($"the Global Importer version value was changed from '{oldVersion}' to '{newVersion}'");
            }
            else
            {
                Debug.LogError($"{kGlobal_allImporterVersion} cannot be added or removed as a dependency");
            }
        }

        private static void GlobalArtifactFormatVersionModified(ArtifactInfoDifference diff, List<string> msgsList)
        {
            if (diff.diffType == DiffType.Modified)
            {
                var oldVersion = diff.oldArtifactInfo.dependencies[diff.key].value;
                var newVersion = diff.newArtifactInfo.dependencies[diff.key].value;
                msgsList.Add($"the global artifact format version changed from '{oldVersion}' to '{newVersion}'");
            }
            else
            {
                Debug.LogError($"{kGlobal_artifactFormatVersion} cannot be added or removed as a dependency");
            }
        }

        private static void ScriptingRuntimeVersionModified(ArtifactInfoDifference diff, List<string> msgsList)
        {
            if (diff.diffType == DiffType.Modified)
            {
                var oldVersion = diff.oldArtifactInfo.dependencies[diff.key].value;
                var newVersion = diff.newArtifactInfo.dependencies[diff.key].value;
                msgsList.Add($"the project's ScriptingRuntimeVersion changed from '{oldVersion}' to '{newVersion}");
            }
            else if (diff.diffType == DiffType.Added)
            {
                var addedVersion = diff.newArtifactInfo.dependencies[diff.key].value;
                msgsList.Add($"a dependency on the selected Scripting Runtime Version '{addedVersion}' was added");
            }
            else if (diff.diffType == DiffType.Removed)
            {
                var removedVersion = diff.newArtifactInfo.dependencies[diff.key].value;
                msgsList.Add($"a dependency on the selected Scripting Runtime Version '{removedVersion}' was removed");
            }
        }

        private static void GraphicsAPIMaskModified(ArtifactInfoDifference diff, List<string> msgsList)
        {
            //UnityEngine.Rendering.GraphicsDeviceType
            if (diff.diffType == DiffType.Modified)
            {
                var assetName = GetAssetName(diff.newArtifactInfo);
                var oldGraphicsAPIValue = diff.oldArtifactInfo.dependencies[diff.key].value;
                var newGraphicsAPIValue = diff.newArtifactInfo.dependencies[diff.key].value;

                msgsList.Add($"the project's GraphicsAPIMask changed from '{oldGraphicsAPIValue}' to '{newGraphicsAPIValue}'");
            }
            else if (diff.diffType == DiffType.Added)
            {
                var assetName = GetAssetName(diff.newArtifactInfo);
                var newGraphicsAPIValue = diff.newArtifactInfo.dependencies[diff.key].value;

                msgsList.Add($"a dependency on the Graphics API '{newGraphicsAPIValue}' was added to '{assetName}'");
            }
            else if (diff.diffType == DiffType.Removed)
            {
                var assetName = GetAssetName(diff.newArtifactInfo);
                var newGraphicsAPIValue = diff.newArtifactInfo.dependencies[diff.key].value;

                msgsList.Add($"the dependency on the Graphics API '{newGraphicsAPIValue}' was removed from '{assetName}'");
            }
        }

        private static void PostProcessorVersionHashModified(ArtifactInfoDifference diff, List<string> msgsList)
        {
            var oldPostProcessorKey = diff.oldArtifactInfo.dependencies.Keys.FirstOrDefault(key => key.StartsWith(kImporterRegistry_PostProcessorVersionHash, StringComparison.Ordinal));

            var newPostProcessorKey = diff.newArtifactInfo.dependencies.Keys.FirstOrDefault(key => key.StartsWith(kImporterRegistry_PostProcessorVersionHash, StringComparison.Ordinal));

            if (!string.IsNullOrEmpty(oldPostProcessorKey) && !string.IsNullOrEmpty(newPostProcessorKey))
            {
                var assetName = GetAssetName(diff.newArtifactInfo);
                msgsList.Add($"a PostProcessor associated with '{assetName}' changed from '{oldPostProcessorKey}' to '{newPostProcessorKey}'");
            }
            else
            {
                if (diff.diffType == DiffType.Removed)
                {
                    var assetName = GetAssetName(diff.newArtifactInfo);
                    var key = diff.key;
                    var start = key.LastIndexOf("/") + 1;

                    var postProcessor = key.Substring(start, key.Length - start);
                    msgsList.Add($"the PostProcessor '{postProcessor}' associated with '{assetName}' was removed as a dependency");
                }
                else if (diff.diffType == DiffType.Added)
                {
                    var assetName = GetAssetName(diff.newArtifactInfo);
                    var key = diff.key;
                    var start = key.LastIndexOf("/") + 1;

                    var postProcessor = key.Substring(start, key.Length - start);
                    msgsList.Add($"the PostProcessor '{postProcessor}' was added as a dependency for '{assetName}'");
                }
            }
        }

        private static void GuidOfPathLocationModified(ArtifactInfoDifference diff, List<string> msgsList)
        {
            if (diff.diffType == DiffType.Added)
            {
                var entry = diff.newArtifactInfo.dependencies[diff.key];
                string guid = entry.value.ToString();

                var pathOfDependency = AssetDatabase.GUIDToAssetPath(guid);

                var assetName = GetAssetName(diff.newArtifactInfo);
                msgsList.Add($"a dependency on '{pathOfDependency}' was added to '{assetName}'");
            }
            else if (diff.diffType == DiffType.Removed)
            {
                var entry = diff.oldArtifactInfo.dependencies[diff.key];
                string guid = entry.value.ToString();

                var pathOfDependency = AssetDatabase.GUIDToAssetPath(guid);

                var assetName = GetAssetName(diff.newArtifactInfo);
                msgsList.Add($"a dependency on '{pathOfDependency}' was removed from '{assetName}'");
            }
        }

        private static void GuidOfPathLocationModifiedViaHashOfSourceAsset(ArtifactInfoDifference diff, List<string> msgsList, string pathOfDependency)
        {
            if (diff.diffType == DiffType.Modified)
            {
                var assetName = GetAssetName(diff.newArtifactInfo);
                msgsList.Add($"the asset '{pathOfDependency}' was changed, which is registered as a dependency of '{assetName}'");
            }
            else if (diff.diffType == DiffType.Added)
            {
                var assetName = GetAssetName(diff.newArtifactInfo);
                msgsList.Add($"a dependency on '{pathOfDependency}' was added to '{assetName}'");
            }
            else if (diff.diffType == DiffType.Removed)
            {
                var assetName = diff.oldArtifactInfo.dependencies[kImportParameter_NameOfAsset].value;
                msgsList.Add($"a dependency on '{pathOfDependency}' was removed from '{assetName}'");
            }
        }

        private static void PlatformDependencyModified(ArtifactInfoDifference diff, List<string> msgsList)
        {
            if (diff.diffType == DiffType.Removed)
            {
                var assetName = GetAssetName(diff.newArtifactInfo);
                msgsList.Add($"the dynamic dependency for the current build target was removed for '{assetName}'");
            }
            else if (diff.diffType == DiffType.Added)
            {
                var assetName = GetAssetName(diff.newArtifactInfo);
                msgsList.Add($"a dynamic dependency which makes '{assetName}' dependent on the current build target was added");
            }
        }

        private static void ImporterVersionModified(ArtifactInfoDifference diff, List<string> msgsList)
        {
            if (diff.diffType == DiffType.Modified)
            {
                var oldImporterVersion = diff.oldArtifactInfo.dependencies[diff.key].value;
                var newImporterVersion = diff.newArtifactInfo.dependencies[diff.key].value;
                var importerName = diff.newArtifactInfo.dependencies[kImportParameter_ImporterType].value;
                msgsList.Add($"The version of the importer '{importerName}' changed from '{oldImporterVersion}' to '{newImporterVersion}'");
            }
        }

        private static void ImporterTypeModified(ArtifactInfoDifference diff, List<string> msgsList)
        {
            var newImporterTypeName = diff.newArtifactInfo.dependencies[diff.key].value;
            var oldImporterTypeName = diff.oldArtifactInfo.dependencies[diff.key].value;
            var assetName = GetAssetName(diff.newArtifactInfo);

            msgsList.Add($"the importer associated with '{assetName}' changed from '{oldImporterTypeName}' now it is '{newImporterTypeName}'");
        }

        // formatter methods below
        private static void EnvironmentCustomDependencyDifference(ArtifactInfoDifference diff, List<string> msgs)
        {
            if (diff.diffType == DiffType.Added)
            {
                var newCustomDependencyValue = diff.newArtifactInfo.dependencies[diff.key].value;
                var customDependencyName = diff.key.Substring(kEnvironment_CustomDependency.Length + 1);
                msgs.Add(
                    $"the custom dependency '{customDependencyName}' was added with value '{newCustomDependencyValue}'");
            }
            else if (diff.diffType == DiffType.Removed)
            {
                var customDependencyName = diff.key.Substring(kEnvironment_CustomDependency.Length + 1);
                msgs.Add(
                    $"the custom dependency '{customDependencyName}' was removed");
            }
            else if (diff.diffType == DiffType.Modified)
            {
                var oldCustomDependencyValue = diff.oldArtifactInfo.dependencies[diff.key].value;
                var newCustomDependencyValue = diff.newArtifactInfo.dependencies[diff.key].value;
                var customDependencyName = diff.key.Substring(kEnvironment_CustomDependency.Length + 1);
                msgs.Add(
                    $"the value of the custom dependency '{customDependencyName}' was changed from '{oldCustomDependencyValue}' to '{newCustomDependencyValue}'");
            }
        }

        private static void ArtifactHashOfContentDifference(ArtifactInfoDifference diff, List<string> msgs)
        {
            var startIndex = diff.key.IndexOf('(') + 1;
            var guid = diff.key.Substring(startIndex, 32);
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var assetName = GetAssetName(diff.newArtifactInfo);

            if (diff.diffType == DiffType.Added)
            {
                msgs.Add($"the asset at '{path}' was added as a dependency of '{assetName}'");
            }
            else if (diff.diffType == DiffType.Removed)
            {
                msgs.Add($"the asset at '{path}' was removed as a dependency of '{assetName}'");
            }
            else if (diff.diffType == DiffType.Modified)
            {
                msgs.Add($"the asset at '{path}' changed, which is registered as a dependency of '{assetName}'");
            }
        }

        private static void TextureCompressionModified(ArtifactInfoDifference diff, List<string> msgs)
        {
            if (diff.diffType == DiffType.Removed)
            {
                var assetName = GetAssetName(diff.newArtifactInfo);
                msgs.Add($"the asset '{assetName}' no longer depends on texture compression");
            }
            else if (diff.diffType == DiffType.Added)
            {
                msgs.Add($"a dependency on the project's texture compression was added");
            }
            else if (diff.diffType == DiffType.Modified)
            {
                var oldTextureCompressionSetting = diff.oldArtifactInfo.dependencies[diff.key].value;
                var newTextureCompressionSetting = diff.newArtifactInfo.dependencies[diff.key].value;
                msgs.Add($"the project's texture compression setting changed from {oldTextureCompressionSetting} to {newTextureCompressionSetting}");
            }
        }

        private static void ColorSpaceModified(ArtifactInfoDifference diff, List<string> msgs)
        {
            if (diff.diffType == DiffType.Removed)
            {
                var assetName = GetAssetName(diff.newArtifactInfo);
                msgs.Add($"the asset '{assetName}' no longer depends color space");
            }
            else if (diff.diffType == DiffType.Modified)
            {
                var oldColorSpace = diff.oldArtifactInfo.dependencies[diff.key].value;
                var newColorSpace = diff.newArtifactInfo.dependencies[diff.key].value;
                msgs.Add($"the project's color space changed from {oldColorSpace} to {newColorSpace}");
            }
        }

        private static void NameOfAssetModified(ArtifactInfoDifference diff, List<string> msgs)
        {
            var oldName = diff.oldArtifactInfo.dependencies[diff.key].value;
            var newName = diff.newArtifactInfo.dependencies[diff.key].value;
            msgs.Add($"the name of the asset was changed from '{oldName}' to '{newName}'");
        }

        private static void HashOfSourceAssetModified(ArtifactInfoDifference diff, List<string> msgs)
        {
            //This means that a source asset that this asset depends on was modified, so we need to do some extra work
            //to get the name out
            if (ContainsGuidOfPathLocationChange(diff, out var pathOfDependency))
            {
                GuidOfPathLocationModifiedViaHashOfSourceAsset(diff, msgs, pathOfDependency);
                return;
            }

            var startIndex = diff.key.IndexOf('(') + 1;
            var guid = diff.key.Substring(startIndex, 32);
            msgs.Add($"the source asset was changed");
        }

        private static bool ContainsGuidOfPathLocationChange(ArtifactInfoDifference diff, out string pathOfDependency)
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

        private static void MetaFileHashModified(ArtifactInfoDifference diff, List<string> msgs)
        {
            var startIndex = diff.key.LastIndexOf('/') + 1;
            var guid = diff.key.Substring(startIndex, 32);
            var path = AssetDatabase.GUIDToAssetPath(guid);
            msgs.Add($"the .meta file '{path}.meta' was changed");
        }

        private static string GetAssetName(ArtifactInfo artifactInfo)
        {
            var assetName = artifactInfo.dependencies[kImportParameter_NameOfAsset].value.ToString();
            return assetName;
        }
    }
}
