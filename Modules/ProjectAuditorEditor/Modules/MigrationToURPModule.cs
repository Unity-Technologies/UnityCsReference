// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEngine;
using Module = Unity.ProjectAuditor.Editor.Core.Module;

namespace Unity.ProjectAuditor.Editor.Modules
{
    // Run the URP Render Pipeline Converter and report its issues.
    internal class MigrationToURPModule : Module
    {
        const string k_ConvertersTypeName = "UnityEditor.Rendering.Universal.Converters, Unity.RenderPipelines.Universal.Editor";
        const string k_ContainerName = "BuiltInToURP";
        const string k_ScanFileName = "ProjectAuditorURPMigrationScan.json";

        // The asset converters scan through the Search service, which indexes the project over several Editor updates,
        // so a scan can take a while to finish.
        const double k_ScanTimeoutSeconds = 600.0;

        internal const string PAA7000 = nameof(PAA7000);

        internal static readonly Descriptor k_ConverterItemDescriptor = new Descriptor
            (
            PAA7000,
            "Universal Render Pipeline: Object requires conversion",
            Areas.MigrationToURP,
            "The Render Pipeline Converter found an object which relies on the Built-in Render Pipeline. The object will not function correctly until it has been converted to the Universal Render Pipeline.",
            "Convert the object via Window > Rendering > Render Pipeline Converter, using the Built-in to URP converters."
            )
        {
            MessageFormat = "{0}: '{1}' requires conversion to URP",
            FixerLabel = "Migrate to URP",
            Fixer = MigrationToURPUtilities.OpenRenderPipelineConverter,
            DocumentationUrl = MigrationToURPUtilities.DocumentationUrl
        };

#pragma warning disable CS0649

        // Mirrors the json layout written by the converter's scan-to-file command
        [Serializable]
        class ScanResultItem
        {
            public string name;
            public string info;
            public string type;
        }

        [Serializable]
        class ScanResultConverter
        {
            public string container;
            public string converterType;
            public List<ScanResultItem> items;
        }

        [Serializable]
        class ScanResult
        {
            public string status;
            public int convertersCompleted;
            public int convertersFailed;
            public List<ScanResultConverter> converters;
        }

#pragma warning restore CS0649

        const BindingFlags k_ConverterApiBindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        public override string Name => "Migration To URP";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => [AssetsModule.k_IssueLayout, SettingsModule.k_IssueLayout];

        public override void Initialize()
        {
            base.Initialize();
            RegisterDescriptor(k_ConverterItemDescriptor);
        }

        public override IEnumerator Audit(AnalysisParams analysisParams, IProgress progress)
        {
            var convertersType = Type.GetType(k_ConvertersTypeName);

            // Nothing to do if the migration checks aren't needed (eg using custom SRP), or if URP isn't installed
            if (!k_ConverterItemDescriptor.IsSupported(analysisParams) || convertersType == null)
            {
                analysisParams.OnModuleCompleted?.Invoke(Name, AnalysisResult.Success, 0);
                yield break;
            }

            // Cancelling a scan is how we stop it when analysis is cancelled, so it's part of the API we need
            var cancelScanMethod = convertersType.GetMethod("CancelScan", k_ConverterApiBindingFlags, null,
                Type.EmptyTypes, null);
            if (cancelScanMethod == null)
            {
                Debug.LogWarning($"[{ProjectAuditor.DisplayName}] Could not find the Render Pipeline Converter scan API on {convertersType.FullName}.");
                analysisParams.OnModuleCompleted?.Invoke(Name, AnalysisResult.Failure, 0);
                yield break;
            }

            AsyncProgressState progressState = progress?.Start("Scanning for URP conversion issues", 1);

            yield return null;

            // The converters report the status the scan finished with, which is how we know the results are complete
            string scanStatus = null;
            var scanFilePath = ScanWithRenderPipelineConverter(convertersType, status => scanStatus = status);
            var result = AnalysisResult.Failure;

            if (!string.IsNullOrEmpty(scanFilePath))
            {
                // Converters can report asynchronously, so the scan is usually still running at this point. The
                // timeout only guards against a converter which never reports its results at all.
                var timeoutTime = EditorApplication.timeSinceStartup + k_ScanTimeoutSeconds;
                while (scanStatus == null)
                {
                    if (progress is { IsCancelled: true })
                    {
                        CancelScan(cancelScanMethod);
                        break;
                    }

                    if (EditorApplication.timeSinceStartup > timeoutTime)
                    {
                        Debug.LogWarning($"[{ProjectAuditor.DisplayName}] The Render Pipeline Converter did not finish scanning within {k_ScanTimeoutSeconds} seconds.");
                        CancelScan(cancelScanMethod);
                        break;
                    }

                    yield return new WaitForEndOfFrame();
                }

                if (progress is { IsCancelled: true })
                {
                    result = AnalysisResult.Cancelled;
                }
                else if (scanStatus == "Cancelled")
                {
                    // We cancelled the scan ourselves because it timed out, which is already reported
                }
                else
                {
                    var context = new AnalysisContext { Params = analysisParams };
                    result = ReportScanResults(context, analysisParams, scanFilePath);
                }
            }

            AdvanceAsyncProgress(progress, progressState);
            progress?.Clear(progressState);

            analysisParams.OnModuleCompleted?.Invoke(Name, result, 0);
        }

        // Abandons the scan, so that the converters stop reporting and the next analysis can scan again
        static void CancelScan(MethodInfo cancelScanMethod)
        {
            try
            {
                cancelScanMethod.Invoke(null, null);
            }
            catch (Exception e)
            {
                var reason = e is TargetInvocationException invocationException ? invocationException.InnerException : e;
                Debug.LogError($"[{ProjectAuditor.DisplayName}] Unable to cancel the Render Pipeline Converter scan: {reason.Message}\n{reason}");
            }
        }

        static string ScanWithRenderPipelineConverter(Type convertersType, Action<string> onScanFinished)
        {
            var filterConvertersMethod = convertersType.GetMethod("FilterConverters", k_ConverterApiBindingFlags, null,
                [typeof(string), typeof(List<string>), typeof(bool)], null);
            var scanToFileMethod = convertersType.GetMethod("ScanToFile", k_ConverterApiBindingFlags, null,
                [typeof(List<Type>), typeof(string), typeof(Action<string>)], null);

            if (filterConvertersMethod == null || scanToFileMethod == null)
            {
                Debug.LogWarning($"[{ProjectAuditor.DisplayName}] Could not find the Render Pipeline Converter scan API on {convertersType.FullName}.");
                return null;
            }

            try
            {
                var converterTypes = filterConvertersMethod.Invoke(null, [k_ContainerName, new List<string>(), false]);

                if (converterTypes is not ICollection { Count: > 0 })
                {
                    Debug.LogWarning($"[{ProjectAuditor.DisplayName}] The Render Pipeline Converter has no converters for the {k_ContainerName} container.");
                    return null;
                }

                return scanToFileMethod.Invoke(null, [converterTypes, k_ScanFileName, onScanFinished]) as string;
            }
            catch (Exception e)
            {
                var reason = e is TargetInvocationException invocationException ? invocationException.InnerException : e;
                Debug.LogError($"[{ProjectAuditor.DisplayName}] The Render Pipeline Converter failed to scan the project: {reason.Message}\n{reason}");
                return null;
            }
        }

        static AnalysisResult ReportScanResults(AnalysisContext context, AnalysisParams analysisParams, string scanFilePath)
        {
            ScanResult scanResult;

            try
            {
                scanResult = JsonUtility.FromJson<ScanResult>(File.ReadAllText(scanFilePath));
            }
            catch (Exception e)
            {
                Debug.LogError($"[{ProjectAuditor.DisplayName}] Unable to read the Render Pipeline Converter scan results from {scanFilePath}: {e.Message}");
                return AnalysisResult.Failure;
            }

            if (scanResult == null)
                return AnalysisResult.Failure;

            // Individual converters can fail without invalidating the results of the others, they log their own errors
            if (scanResult.status != "Completed")
            {
                Debug.LogError($"[{ProjectAuditor.DisplayName}] The Render Pipeline Converter scan ended with status '{scanResult.status}'.");
                return AnalysisResult.Failure;
            }

            analysisParams.OnIncomingIssues(CreateIssues(context, scanResult));

            return AnalysisResult.Success;
        }

        static IEnumerable<ReportItem> CreateIssues(AnalysisContext context, ScanResult scanResult)
        {
            var issues = new List<ReportItem>();

            if (scanResult.converters == null)
                return issues;

            foreach (var converter in scanResult.converters)
            {
                if (converter.items == null)
                    continue;

                foreach (var item in converter.items)
                {
                    var category = (converter.converterType == "RenderSettings") ? IssueCategory.ProjectSetting : IssueCategory.AssetIssue;
                    var issue = context.CreateIssue(category, k_ConverterItemDescriptor.Id,
                        converter.converterType, item.name);

                    // Most converters report the path of the asset or scene the item belongs to as its info
                    if (!string.IsNullOrEmpty(item.info) && File.Exists(Path.Combine(ProjectAuditor.ProjectPath, item.info)))
                        issue = issue.WithLocation(item.info);

                    issues.Add(issue);
                }
            }

            return issues;
        }
    }

    /// <summary>
    /// Utilities to help with migration advice to the Universal Render Pipeline.
    /// </summary>
    public static class MigrationToURPUtilities
    {
        /// <summary>
        /// Link to the documentation.
        /// </summary>
        public static string DocumentationUrl => "https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html?subfolder=/manual/upgrade-guides.html";

        /// <summary>
        /// Shows where to install the URP package, and opens the Render Pipeline Converter tool.
        /// </summary>
        /// <param name="issue">ReportItem triggering this Quick Fix helper.</param>
        /// <param name="analysisParams">AnalysisParams for the current analysis.</param>
        /// <returns>Whether the method fixed the issue. Always false for this fixer.</returns>
        public static bool OpenRenderPipelineConverter(ReportItem issue, AnalysisParams analysisParams)
        {
            // When an SRP package is installed, the Render Pipeline Converter is the migration entry point.
            if (EditorApplication.ExecuteMenuItem("Window/Rendering/Render Pipeline Converter"))
                return false;

            // No SRP package is installed yet: open Package Manager so the user can add URP first.
            UnityEditor.PackageManager.UI.Window.Open("com.unity.render-pipelines.universal");

            // Opening a window doesn't migrate the project, so the issue remains until BiRP is no longer in use.
            return false;
        }
    }
}
