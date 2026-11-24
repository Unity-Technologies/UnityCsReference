// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Modules;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.UI
{
    internal static partial class ConversionOutcomeState
    {
        [AutoStaticsCleanupOnCodeReload]
        static bool s_Running;

        internal static bool Running => s_Running;

        internal static void Reset()
        {
            s_Running = false;
            EditorApplication.update -= RepaintWhileRunning;
        }

        internal static void Run(string targetPipeline)
        {
            if (Running)
                return;

            s_Running = true;
            EditorApplication.update += RepaintWhileRunning;

            EditorApplication.delayCall += () =>
                MigrationToURPUtilities.RunRenderPipelineConverterInBatchMode(targetPipeline, OnFinished);
        }

        internal static void DrawOutcome(Report report)
        {
            if (Running || report == null || !report.SessionInfo.URPConverterRan)
                return;

            GUILayout.Space(8);

            var summary = MigrationWorkflowView.GetConversionOutcomeSummary(report);
            var messageType = summary == MigrationWorkflowView.Contents.ConversionProducedNoResults ? MessageType.Warning : MessageType.Info;
            EditorGUILayout.HelpBox(summary, messageType);
        }

        static void OnFinished(string output)
        {
            if (!EditorWindow.HasOpenInstances<ProjectAuditorWindow>())
            {
                Reset();
                return;
            }

            var window = ProjectAuditorWindow.Instance;
            var report = window.m_Report;

            var (manualFixIssues, replacedIssues) = MigrationWorkflowView.ApplyConversionResults(report, output);

            report.SessionInfo.URPConverterRan = true;
            report.NeedsSaving = true;
            EditorApplication.delayCall += () => ProjectAuditorWindow.AutosaveReport(report);

            window.MarkSummaryViewsDirty();
            var changedIssues = new List<ReportItem>(manualFixIssues);
            changedIssues.AddRange(replacedIssues);
            window.m_ViewManager.ReloadIssues(report, changedIssues);

            var failedCount = 0;
            foreach (var issue in manualFixIssues)
            {
                if (issue.Id == MigrationToURPModule.k_ConverterItemFailedDescriptor.Id)
                    failedCount++;
            }

            if (failedCount > 0)
                Debug.Log($"{failedCount} item(s) failed to convert. Check the console for more information.");

            s_Running = false;
            EditorApplication.update -= RepaintWhileRunning;
            window.Repaint();
        }

        static void RepaintWhileRunning()
        {
            if (!EditorWindow.HasOpenInstances<ProjectAuditorWindow>())
            {
                Reset();
                return;
            }

            ProjectAuditorWindow.Instance.Repaint();
        }
    }
}
