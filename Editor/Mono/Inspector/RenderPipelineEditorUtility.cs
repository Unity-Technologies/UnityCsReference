// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Reflection;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    public static class RenderPipelineEditorUtility
    {
        public static Type[] GetDerivedTypesSupportedOnCurrentPipeline<T>()
        {
            return TypeCache.GetTypesDerivedFrom<T>()
                .Where(t => t.GetCustomAttribute<SupportedOnRenderPipelineAttribute>() is { isSupportedOnCurrentPipeline: true })
                .ToArray();
        }

        [Obsolete($"{nameof(FetchFirstCompatibleTypeUsingScriptableRenderPipelineExtension)} is deprecated. Use {nameof(GetDerivedTypesSupportedOnCurrentPipeline)} instead. #from(2023.1)", false)]
        public static Type FetchFirstCompatibleTypeUsingScriptableRenderPipelineExtension<TBaseClass>()
        {
            var extensionTypes = TypeCache.GetTypesDerivedFrom<TBaseClass>();

            foreach (Type extensionType in extensionTypes)
            {
#pragma warning disable CS0618
                if (Attribute.GetCustomAttribute(extensionType, typeof(ScriptableRenderPipelineExtensionAttribute)) is ScriptableRenderPipelineExtensionAttribute { inUse: true })
                    return extensionType;
#pragma warning restore CS0618

            }

            return null;
        }
    }
}
