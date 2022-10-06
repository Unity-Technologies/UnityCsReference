// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Reflection;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ScriptableRenderPipelineExtensionAttribute : Attribute
    {
        internal Type renderPipelineType;

        public ScriptableRenderPipelineExtensionAttribute(Type renderPipelineAsset)
        {
            if (!(renderPipelineAsset?.IsSubclassOf(typeof(RenderPipelineAsset)) ?? false))
                throw new ArgumentException("Given renderPipelineAsset must derive from RenderPipelineAsset");
            renderPipelineType = renderPipelineAsset;
        }

        public bool inUse
            => GraphicsSettings.currentRenderPipeline?.GetType() == renderPipelineType;
    }

    public static class RenderPipelineEditorUtility
    {
        public static Type[] GetDerivedTypesSupportedOnCurrentPipeline<T>()
        {
            return TypeCache.GetTypesDerivedFrom<T>().Where(t =>
            {
                var attribute = t.GetCustomAttribute<SupportedOnRenderPipelineAttribute>();
                return attribute != null && attribute.isSupportedOnCurrentPipeline;

            }).ToArray();
        }

        public static Type FetchFirstCompatibleTypeUsingScriptableRenderPipelineExtension<TBaseClass>()
        {
            var extensionTypes = TypeCache.GetTypesDerivedFrom<TBaseClass>();

            foreach (Type extensionType in extensionTypes)
            {
                if (Attribute.GetCustomAttribute(extensionType, typeof(ScriptableRenderPipelineExtensionAttribute)) is ScriptableRenderPipelineExtensionAttribute { inUse: true })
                    return extensionType;
            }

            return null;
        }
    }
}
