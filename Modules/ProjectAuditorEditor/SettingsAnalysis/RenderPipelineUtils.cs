// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Unity.ProjectAuditor.Editor.SettingsAnalysis
{
    internal static class RenderPipelineUtils
    {
        internal static List<T> GetAllComponents<T>()
        {
            var allComponents = new List<T>();
            for (int n = 0; n < SceneManager.sceneCount; ++n)
            {
                var scene = SceneManager.GetSceneAt(n);
                var roots = scene.GetRootGameObjects();
                foreach (var go in roots)
                {
                    GetComponents(go, ref allComponents);
                }
            }

            return allComponents;
        }

        private static void GetComponents<T>(GameObject go, ref List<T> components)
        {
            bool result = go.TryGetComponent(out T comp);
            if (result)
            {
                components.Add(comp);
            }

            for (int i = 0; i < go.transform.childCount; i++)
            {
                GetComponents(go.transform.GetChild(i).gameObject, ref components);
            }
        }

        internal static IEnumerable<ReportItem> AnalyzeAssets(
            SettingsAnalysisContext context,
            Func<SettingsAnalysisContext, RenderPipelineAsset, int, IEnumerable<ReportItem>> analyze)
        {
            var issues = analyze(context, GraphicsSettings.defaultRenderPipeline, -1);
            foreach (var issue in issues)
            {
                yield return issue;
            }

            var initialQualityLevel = QualitySettings.GetQualityLevel();
            for (var i = 0; i < QualitySettings.names.Length; ++i)
            {
                QualitySettings.SetQualityLevel(i);

                issues = analyze(context, QualitySettings.renderPipeline, i);
                foreach (var issue in issues)
                {
                    yield return issue;
                }
            }

            QualitySettings.SetQualityLevel(initialQualityLevel);
        }

        internal static ReportItem CreateAssetSettingIssue(AnalysisContext context, int qualityLevel, string name, string id)
        {
            string assetLocation = qualityLevel == -1
                ? "Default Rendering Pipeline Asset"
                : $"Rendering Pipeline Asset on Quality Level: '{QualitySettings.names[qualityLevel]}'";
            return context.CreateIssue(IssueCategory.ProjectSetting, id,
                name, assetLocation)
                .WithCustomProperties(new object[] { qualityLevel })
                .WithLocation(qualityLevel == -1 ? "Project/Graphics" : "Project/Quality");
        }

        internal static void FixAssetSetting(ReportItem issue, Action<RenderPipelineAsset> setter)
        {
            int qualityLevel = issue.GetCustomPropertyInt32(0);
            if (qualityLevel == -1)
            {
                setter(GraphicsSettings.defaultRenderPipeline);
                return;
            }

            var initialQualityLevel = QualitySettings.GetQualityLevel();
            QualitySettings.SetQualityLevel(qualityLevel);
            setter(QualitySettings.renderPipeline);
            QualitySettings.SetQualityLevel(initialQualityLevel);
        }
    }
}
