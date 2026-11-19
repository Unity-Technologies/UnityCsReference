// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Build.Rendering
{
    class EnsureSinglePipelineOnBuild : IPreprocessBuildWithContext
    {
        public int callbackOrder => (int) ExecutionOrder.EnsureSinglePipeline;

        public void OnPreprocessBuild(BuildCallbackContext ctx)
        {
            var buildTargetGroupName = BuildPipeline.GetBuildTargetGroupName(ctx.Report.summary.platform);

            var missingReferences = GetMissingRenderPipelineAssetReferences(buildTargetGroupName);
            if (missingReferences.Count > 0)
                throw new BuildFailedException($"The following settings reference a Render Pipeline Asset that is missing: {string.Join(", ", missingReferences)}. Assign a valid Render Pipeline Asset or clear the reference.");

            if (!QualitySettings.SamePipelineAssetsForPlatform(buildTargetGroupName))
                throw new BuildFailedException($"The current build target has assets in its associated Quality levels and Graphics Settings that belong to different render pipelines. Please check your settings.");
        }

        // In C#, an assigned-but-missing asset and an empty slot are both marshaled to "null", so we
        // read the serialized reference to check if an entityId is assigned.
        internal static List<string> GetMissingRenderPipelineAssetReferences(string buildTargetGroupName)
        {
            var missingReferences = new List<string>();

            var graphicsSettings = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            if (IsMissingReference(graphicsSettings.FindProperty("m_CustomRenderPipeline")))
                missingReferences.Add("Graphics Settings > Default Render Pipeline");

            var qualitySettings = new SerializedObject(QualitySettings.GetQualitySettings());
            var qualityLevels = qualitySettings.FindProperty("m_QualitySettings");
            foreach (var level in QualitySettings.GetActiveQualityLevelsForPlatform(buildTargetGroupName))
            {
                var qualityLevel = qualityLevels.GetArrayElementAtIndex(level);
                if (IsMissingReference(qualityLevel.FindPropertyRelative("customRenderPipeline")))
                    missingReferences.Add($"Quality Settings > {qualityLevel.FindPropertyRelative("name").stringValue}");
            }

            return missingReferences;
        }

        static bool IsMissingReference(SerializedProperty property)
        {
            return property != null && property.objectReferenceValue == null && property.objectReferenceEntityIdValue != EntityId.None;
        }
    }
}
