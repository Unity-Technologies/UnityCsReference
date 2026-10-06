// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Modules
{
    internal class AssetsModuleLightmapperAnalyzer : AssetsModuleAnalyzer
    {
        internal const string PAA8000 = nameof(PAA8000);

        internal static readonly Descriptor k_RemovedLightmapperDescriptor = new Descriptor
            (
            PAA8000,
            "Lighting Settings: Removed lightmapper",
            Areas.Quality | Areas.Upgrade,
            "These Lighting Settings were saved with a lightmapper that has been removed. Unity bakes them with the Progressive GPU Lightmapper instead, or with the Unity Compute Light Baker if it's the <b>Default Light Baker</b> in <b>Project Settings > Graphics</b>. Baked lighting results can differ, and a large scene that baked on the CPU might not fit in GPU memory.",
            "Rebake the lighting in the scenes that use these Lighting Settings. If the bake runs out of memory, set <b>GPU Baking Profile</b> in the <b>Lighting</b> window to <b>Low Memory Usage</b> or <b>Lowest Memory Usage</b>, reduce <b>Max Lightmap Size</b>, or set <b>Default Light Baker</b> in <b>Project Settings > Graphics</b> to <b>Unity Compute Light Baker</b>."
            )
        {
            MessageFormat = "'{0}' uses the removed {1} lightmapper",
            DocumentationUrl = "https://docs.unity3d.com/Manual/progressive-lightmapper.html",
            Fixer = (issue, analysisParams) =>
            {
                if (EditorApplication.isPlaying)
                {
                    Debug.LogWarning($"Cannot fix asset at '{issue.RelativePath}' in Play mode.");
                    return false;
                }

                if (InternalEditorUtility.IsReadOnlyAsset(issue.RelativePath, out _))
                {
                    Debug.LogWarning($"Cannot fix asset at '{issue.RelativePath}' because it's readonly.");
                    return false;
                }

                // Unity upgrades the lightmapper on load without marking the asset dirty, so writing the upgrade to
                // disk needs a forced reserialization.
                AssetDatabase.ForceReserializeAssets(new[] { issue.RelativePath }, ForceReserializeAssetsOptions.ReserializeAssets);
                return true;
            }
        };

        const string k_UnityDirective = "%UNITY ";
        const string k_YamlDirective = "%YAML ";

        // Documents in a text-serialized file start with the class ID of the object they contain
        const string k_DocumentHeaderPrefix = "--- ";
        const string k_LightingSettingsDocumentHeader = "--- !u!850595691 ";
        const string k_LightmapSettingsDocumentHeader = "--- !u!157 ";
        const string k_LightingSettingsReferenceKey = "  m_LightingSettings: ";
        const string k_SerializedVersionKey = "  serializedVersion: ";
        const string k_BakeBackendKey = "  m_BakeBackend: ";
        const string k_EnableBakedLightmapsKey = "  m_EnableBakedLightmaps: ";

        // LightmapSettings from before Lighting Settings existed keep the lightmapper in m_LightmapEditorSettings, and
        // LightmapEditorSettings from before multiple lightmappers existed always use Enlighten
        const int k_LastLegacyLightmapSettingsVersion = 11;
        const int k_LastEnlightenOnlyLightmapEditorSettingsVersion = 6;
        const string k_NestedPrefix = "    ";
        const string k_GISettingsKey = "  m_GISettings:";
        const string k_LightmapEditorSettingsKey = "  m_LightmapEditorSettings:";
        const string k_NestedSerializedVersionKey = "    serializedVersion: ";
        const string k_NestedBakeBackendKey = "    m_BakeBackend: ";
        const string k_NestedEnableBakedLightmapsKey = "    m_EnableBakedLightmaps: ";

        // Serialized values of LightingSettings.Lightmapper for the removed backends
        const int k_BakeBackendEnlighten = 0;
        const int k_BakeBackendProgressiveCPU = 1;

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_RemovedLightmapperDescriptor);
        }

        public override IEnumerable<ReportItem> Analyze(AssetAnalysisContext context)
        {
            var assetPath = context.AssetPath;
            if (!assetPath.EndsWith(".lighting", StringComparison.OrdinalIgnoreCase) &&
                !assetPath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                yield break;

            if (!TryReadSerializedBakeBackend(assetPath, out var bakeBackend))
                yield break;

            string lightmapperName;
            switch (bakeBackend)
            {
                case k_BakeBackendEnlighten:
                    lightmapperName = "Enlighten Baked Global Illumination";
                    break;
                case k_BakeBackendProgressiveCPU:
                    lightmapperName = "Progressive CPU";
                    break;
                default:
                    yield break;
            }

            yield return context.CreateIssue
            (
                IssueCategory.AssetIssue,
                k_RemovedLightmapperDescriptor.Id,
                Path.GetFileName(assetPath),
                lightmapperName
            )
            .WithLocation(assetPath)
            .WithUpgradeProperties("7000.0", null, null);
        }

        enum SerializedDocument
        {
            Other,
            LightmapSettings,
            LightingSettings,
        }

        // Reads the lightmapper a text-serialized scene or Lighting Settings asset is saved with: from its
        // LightingSettings object, or from the LightmapSettings of a scene from before Lighting Settings existed.
        // Returns false if the file is binary-serialized, has no Lighting Settings of its own, or has Baked Global
        // Illumination disabled, because the lightmapper is not used then.
        static bool TryReadSerializedBakeBackend(string assetPath, out int bakeBackend)
        {
            bakeBackend = -1;

            if (!File.Exists(assetPath))
                return false;

            var bakedLightmapsEnabled = true;
            var foundBakeBackend = false;
            var lightmapSettingsVersion = int.MaxValue;
            var lightmapEditorSettingsVersion = int.MaxValue;

            using (var reader = new StreamReader(assetPath))
            {
                // Text-serialized files start with the %YAML directive, which Unity 7.0 and newer precede with a
                // %UNITY directive
                var line = reader.ReadLine();
                if (line != null && line.StartsWith(k_UnityDirective, StringComparison.Ordinal))
                    line = reader.ReadLine();
                if (line == null || !line.StartsWith(k_YamlDirective, StringComparison.Ordinal))
                    return false;

                var document = SerializedDocument.Other;
                var section = string.Empty;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith(k_DocumentHeaderPrefix, StringComparison.Ordinal))
                    {
                        if (document == SerializedDocument.LightingSettings)
                            break;
                        if (document == SerializedDocument.LightmapSettings && lightmapSettingsVersion <= k_LastLegacyLightmapSettingsVersion)
                            break;

                        if (line.StartsWith(k_LightingSettingsDocumentHeader, StringComparison.Ordinal))
                            document = SerializedDocument.LightingSettings;
                        else if (line.StartsWith(k_LightmapSettingsDocumentHeader, StringComparison.Ordinal))
                            document = SerializedDocument.LightmapSettings;
                        else
                            document = SerializedDocument.Other;
                        continue;
                    }

                    if (document == SerializedDocument.LightingSettings)
                    {
                        if (TryParseValue(line, k_BakeBackendKey, out var value))
                        {
                            bakeBackend = value;
                            foundBakeBackend = true;
                        }
                        else if (TryParseValue(line, k_EnableBakedLightmapsKey, out value))
                            bakedLightmapsEnabled = value != 0;
                    }
                    else if (document == SerializedDocument.LightmapSettings)
                    {
                        if (!line.StartsWith(k_NestedPrefix, StringComparison.Ordinal))
                            section = line;

                        // A scene which references a Lighting Settings asset, or none, has no embedded Lighting
                        // Settings, so there's no need to read the rest of the scene.
                        if (line.StartsWith(k_LightingSettingsReferenceKey, StringComparison.Ordinal) &&
                            (line.Contains("guid:") || line.Contains("{fileID: 0}")))
                            return false;

                        if (TryParseValue(line, k_SerializedVersionKey, out var value))
                            lightmapSettingsVersion = value;
                        else if (lightmapSettingsVersion > k_LastLegacyLightmapSettingsVersion)
                            continue;
                        else if (section == k_GISettingsKey && TryParseValue(line, k_NestedEnableBakedLightmapsKey, out value))
                            bakedLightmapsEnabled = value != 0;
                        else if (section == k_LightmapEditorSettingsKey && TryParseValue(line, k_NestedSerializedVersionKey, out value))
                            lightmapEditorSettingsVersion = value;
                        else if (section == k_LightmapEditorSettingsKey && TryParseValue(line, k_NestedBakeBackendKey, out value))
                        {
                            bakeBackend = value;
                            foundBakeBackend = true;
                        }
                    }
                }
            }

            if (lightmapSettingsVersion <= k_LastLegacyLightmapSettingsVersion &&
                lightmapEditorSettingsVersion <= k_LastEnlightenOnlyLightmapEditorSettingsVersion)
            {
                bakeBackend = k_BakeBackendEnlighten;
                foundBakeBackend = true;
            }

            return foundBakeBackend && bakedLightmapsEnabled;
        }

        static bool TryParseValue(string line, string key, out int value)
        {
            value = 0;
            return line.StartsWith(key, StringComparison.Ordinal) && int.TryParse(line.Substring(key.Length), out value);
        }
    }
}
