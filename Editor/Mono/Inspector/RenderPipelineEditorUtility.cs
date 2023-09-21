// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
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

        private static Dictionary<Type, Type> s_RenderPipelineAssetToRenderPipelineType = new ();

        internal static Type GetPipelineTypeFromPipelineAssetType(Type pipelineAssetType)
        {
            if (!typeof(RenderPipelineAsset).IsAssignableFrom(pipelineAssetType))
                return null;

            if (s_RenderPipelineAssetToRenderPipelineType.TryGetValue(pipelineAssetType, out var pipelineType))
                return pipelineType;

            Type baseGenericType = pipelineAssetType;
            while (baseGenericType != null)
            {
                if (!baseGenericType.IsGenericType || baseGenericType.GetGenericTypeDefinition() != typeof(RenderPipelineAsset<>))
                {
                    baseGenericType = baseGenericType.BaseType;
                    continue;
                }

                pipelineType = baseGenericType.GetGenericArguments()[0];
                s_RenderPipelineAssetToRenderPipelineType[pipelineAssetType] = pipelineType;
                return pipelineType;
            }

            var pipelineAsset = ScriptableObject.CreateInstance(pipelineAssetType) as RenderPipelineAsset;
            pipelineType = pipelineAsset.pipelineType;
            UnityEngine.Object.DestroyImmediate(pipelineAsset);
            s_RenderPipelineAssetToRenderPipelineType[pipelineAssetType] = pipelineType;
            return pipelineType;
        }
    }
}
