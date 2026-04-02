// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Handles reporting of UXML upgrade results and errors.
    /// </summary>
    class UxmlUpgradeReporter
    {
        readonly List<string> m_Errors = new List<string>();
        readonly Dictionary<VisualTreeAsset, List<string>> m_AssetResults = new Dictionary<VisualTreeAsset, List<string>>();

        /// <summary>
        /// Add an error message to the report.
        /// </summary>
        public void AddError(string error)
        {
            m_Errors.Add(error);
        }

        /// <summary>
        /// Record that an upgrader was applied to an asset.
        /// </summary>
        public void AddUpgradeResult(VisualTreeAsset asset, string result)
        {
            if (!m_AssetResults.TryGetValue(asset, out var results))
            {
                results = new List<string>();
                m_AssetResults[asset] = results;
            }
            results.Add(result);
        }

        /// <summary>
        /// Generate and log the final report.
        /// </summary>
        /// <param name="totalModified">The number of assets that were modified.</param>
        public void LogReport(int totalModified)
        {
            using var _ = StringBuilderPool.Get(out var report);

            // Report errors first
            if (m_Errors.Count > 0)
            {
                report.AppendLine("UXML Upgrade Errors:");
                foreach (var error in m_Errors)
                {
                    report.AppendLine($"  • {error}");
                }
                report.AppendLine();
            }

            // Report results
            if (totalModified > 0)
            {
                report.Append("UXML Upgrade Results - Modified ").Append(totalModified).Append(" asset(s):");
                report.AppendLine();
                report.AppendLine();

                foreach (var kvp in m_AssetResults)
                {
                    var vta = kvp.Key;
                    var upgraderNames = kvp.Value;

                    var assetPath = AssetDatabase.GetAssetPath(vta);
                    report.Append("Modified: ").AppendLine(vta.name);
                    if (!string.IsNullOrEmpty(assetPath))
                        report.AppendLine(assetPath);

                    foreach (var upgraderName in upgraderNames)
                    {
                        report.Append("  [").Append(upgraderName).AppendLine("]");
                    }

                    report.AppendLine();
                }
            }
            else if (m_Errors.Count == 0)
            {
                report.AppendLine("UXML Upgrade Results - No changes made.");
            }

            Debug.Log(report.ToString());
        }
    }
}
