// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    public static class RenderPipelineGraphicsSettingsEditorUtility
    {
        public static void ForEachFieldOfType<T>(this IRenderPipelineResources resource, Action<T> callback, BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
        {
            if (resource == null)
                return;

            var fields = resource.GetType().GetFields(flags);
            foreach (var fieldInfo in fields)
                if (fieldInfo.GetValue(resource) is T fieldValue)
                    callback(fieldValue);
        }

        public static void RemoveRenderPipelineGraphicsSettingsWithMissingScript()
        {
            EditorGraphicsSettings.ForEachPipelineSettings((settings) => settings.CleanNullSettings());
        }
    }
}
