// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Service for applying UXML upgrades to <see cref="VisualTreeAsset"/>.
    /// </summary>
    public class UxmlUpgradeService
    {
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal readonly List<IUxmlUpgrader> m_Upgraders = new List<IUxmlUpgrader>();
        readonly List<bool> m_UpgradersEnabled = new List<bool>();
        readonly VisualTreeAssetExporter m_Exporter = new VisualTreeAssetExporter();

        static readonly VisualTreeAssetExporter.ExportOptions s_ExportOptions = new VisualTreeAssetExporter.ExportOptions
        {
            ignoreAttributeList = new string[] {"__unity-builder-selected-element"},
            styleExporterOptions = new StyleSheetExporter.UssExportOptions { ignorePropertyList = new string[] {"__unity_ui_builder_selected_stylesheet"} }
        };

        /// <summary>
        /// Creates a new instance of the UxmlUpgradeService and initializes the list of upgraders by finding all types that implement the <see cref="IUxmlUpgrader"/> interface.
        /// The upgraders are sorted by name for consistent execution order.
        /// </summary>
        public UxmlUpgradeService()
        {
            // Initialize upgraders list
            foreach(var t in TypeCache.GetTypesDerivedFrom<IUxmlUpgrader>())
            {
                if (t.IsAbstract || t.IsInterface)
                    continue;

                try
                {
                    var upgrader = (IUxmlUpgrader)Activator.CreateInstance(t);
                    m_Upgraders.Add(upgrader);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to create UXML upgrader instance for type {t.FullName}: {e.Message}");
                }
            }

            // Sort by name
            m_Upgraders.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            // All upgraders are enabled by default
            for (int i = 0; i < m_Upgraders.Count; i++)
                m_UpgradersEnabled.Add(true);
        }

        /// <summary>
        /// Get the list of all registered upgraders.
        /// </summary>
        public IReadOnlyList<IUxmlUpgrader> upgraders => m_Upgraders;

        /// <summary>
        /// Get an upgrader by its name.
        /// </summary>
        /// <param name="name">The name of the upgrader to find.</param>
        /// <returns>The upgrader with the specified name, or null if not found.</returns>
        public IUxmlUpgrader GetUpgraderByName(string name)
        {
            for (int i = 0; i < m_Upgraders.Count; i++)
            {
                if (m_Upgraders[i].name == name)
                    return m_Upgraders[i];
            }
            return null;
        }

        /// <summary>
        /// Enable or disable a specific upgrader.
        /// </summary>
        public void SetUpgraderEnabled(IUxmlUpgrader upgrader, bool enabled)
        {
            if (upgrader == null)
                throw new ArgumentNullException(nameof(upgrader));
            for (int i = 0; i < m_Upgraders.Count; i++)
            {
                if (m_Upgraders[i] == upgrader)
                {
                    m_UpgradersEnabled[i] = enabled;
                    break;
                }
            }
        }

        /// <summary>
        /// Check if an upgrader is enabled.
        /// </summary>
        public bool IsUpgraderEnabled(IUxmlUpgrader upgrader)
        {
            if (upgrader == null)
                throw new ArgumentNullException(nameof(upgrader));
            for (int i = 0; i < m_Upgraders.Count; i++)
            {
                if (m_Upgraders[i] == upgrader)
                    return m_UpgradersEnabled[i];
            }
            return true; // Default to enabled if not found
        }

        /// <summary>
        /// Apply all enabled upgrades to the specified assets.
        /// </summary>
        /// <param name="assets">The assets to upgrade.</param>
        public void ApplyUpgrades(List<VisualTreeAsset> assets)
        {
            // Collect enabled upgraders
            using var _ = ListPool<IUxmlUpgrader>.Get(out var enabledUpgraders);
            for (int i = 0; i < m_Upgraders.Count; i++)
            {
                if (m_UpgradersEnabled[i])
                    enabledUpgraders.Add(m_Upgraders[i]);
            }

            ApplyUpgrades(assets, enabledUpgraders);
        }

        /// <summary>
        /// Apply specific upgrades to the specified assets.
        /// </summary>
        /// <param name="assets">The assets to upgrade.</param>
        /// <param name="upgradersToRun">List of specific upgraders to run.</param>
        public void ApplyUpgrades(List<VisualTreeAsset> assets, List<IUxmlUpgrader> upgradersToRun)
        {
            if (assets == null)
                throw new ArgumentNullException(nameof(assets));
            if (upgradersToRun == null)
                throw new ArgumentNullException(nameof(upgradersToRun));

            if (assets.Count == 0)
            {
                Debug.Log("No assets to process.");
                return;
            }
            ApplyUpgradesInternal(assets, upgradersToRun);
        }

        internal virtual void ApplyUpgradesInternal(List<VisualTreeAsset> assets, List<IUxmlUpgrader> upgradersToRun)
        {
            var reporter = new UxmlUpgradeReporter();

            // Check if we're running automatic upgraders manually
            bool runningUrlUpgrader = false;

            foreach (var upgrader in upgradersToRun)
            {
                if (upgrader is UrlUpgrader)
                    runningUrlUpgrader = true;
            }

            // We need to reimport if we're NOT running these automatic upgraders
            // This ensures automatic upgrades remain enabled for upgraders we're not manually running
            bool needsReimport = !runningUrlUpgrader;

            int totalModified = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                for (int i = 0; i < assets.Count; i++)
                {
                    var vta = assets[i];
                    if (vta == null)
                        continue;

                    try
                    {
                        // Reimport if needed - disable automatic upgrades only for upgraders we're running manually
                        // This prevents double-applying upgrades
                        if (needsReimport)
                        {
                            try
                            {
                                var reimportedVta = ReimportAsset(vta, runningUrlUpgrader);

                                // Replace the asset in the list with the reimported one
                                vta = reimportedVta;
                                assets[i] = vta;
                            }
                            catch (Exception e)
                            {
                                reporter.AddError($"Failed to reimport '{vta.name}': {e.Message}");
                                continue;
                            }
                        }

                        bool assetModified = false;

                        // Apply the provided upgraders
                        foreach (var upgrader in upgradersToRun)
                        {
                            try
                            {
                                if (upgrader.Upgrade(vta))
                                {
                                    assetModified = true;
                                    reporter.AddUpgradeResult(vta, upgrader.name);
                                }
                            }
                            catch (Exception e)
                            {
                                reporter.AddError($"Upgrader '{upgrader.name}' failed for '{vta.name}': {e.Message}");
                            }
                        }

                        if (assetModified)
                        {
                            var uxml = m_Exporter.ToUxmlString(vta, s_ExportOptions);
                            WriteUxmlToFile(vta, uxml);
                            totalModified++;
                        }
                    }
                    catch (Exception e)
                    {
                        reporter.AddError($"Failed to upgrade '{vta.name}': {e.Message}");
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            reporter.LogReport(totalModified);
        }

        internal virtual VisualTreeAsset ReimportAsset(VisualTreeAsset vta, bool enableUrlUpgrades)
        {
            var importer = new UXMLImporterImpl
            {
                enableAutomaticUrlUpgrades = enableUrlUpgrades,
            };

            var uxmlContent = ReadUxmlFromFile(vta);
            importer.ImportXmlFromString(uxmlContent, out var reimportedVta);

            return reimportedVta;
        }

        internal virtual string ReadUxmlFromFile(VisualTreeAsset asset)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            if (!string.IsNullOrEmpty(assetPath) && File.Exists(assetPath))
            {
                return File.ReadAllText(assetPath);
            }
            throw new FileNotFoundException($"Could not find UXML file for asset '{asset.name}' at path '{assetPath}'.");
        }

        /// <summary>
        /// Write the given UXML string to the file corresponding to the given asset. This method is virtual to allow mocking in tests.
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="uxml"></param>
        internal virtual void WriteUxmlToFile(VisualTreeAsset asset, string uxml)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            if (!string.IsNullOrEmpty(assetPath))
            {
                File.WriteAllText(assetPath, uxml);
                AssetDatabase.ImportAsset(assetPath);
            }
        }
    }
}
