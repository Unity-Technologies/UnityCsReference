// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Unity.Collections;

namespace UnityEngine.Rendering
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class SupportedOnRenderPipelineAttribute : Attribute
    {
        public enum SupportedMode
        {
            Unsupported,
            Supported,
            SupportedByBaseClass
        }

        static readonly Lazy<Type[]> k_DefaultRenderPipelineAsset = new(() => new[] { typeof(RenderPipelineAsset) });
        public Type[] renderPipelineTypes { get; }

        public SupportedOnRenderPipelineAttribute(Type renderPipeline)
            : this(new[] { renderPipeline }) {}

        public SupportedOnRenderPipelineAttribute(params Type[] renderPipeline)
        {
            if (renderPipeline == null)
            {
                Debug.LogError($"The {nameof(SupportedOnRenderPipelineAttribute)} parameters cannot be null.");
                return;
            }

            if (renderPipeline.Any(r => r == null || !typeof(RenderPipelineAsset).IsAssignableFrom(r)))
            {
                Debug.LogError(
                    $"The {nameof(SupportedOnRenderPipelineAttribute)} Attribute targets an invalid {nameof(RenderPipelineAsset)}. One of the types cannot be assigned from {nameof(RenderPipelineAsset)}: [{renderPipeline.SerializedView(t => t.Name)}].");
                return;
            }

            renderPipelineTypes = renderPipeline.Length == 0 ? k_DefaultRenderPipelineAsset.Value : renderPipeline;
        }

        public bool isSupportedOnCurrentPipeline => GetSupportedMode(renderPipelineTypes, GraphicsSettings.currentRenderPipelineAssetType) != SupportedMode.Unsupported;

        public SupportedMode GetSupportedMode(Type renderPipelineAssetType) => GetSupportedMode(renderPipelineTypes, renderPipelineAssetType);

        internal static SupportedMode GetSupportedMode(Type[] renderPipelineTypes, Type renderPipelineAssetType)
        {
            if (renderPipelineTypes == null)
                throw new ArgumentNullException($"Parameter {nameof(renderPipelineTypes)} cannot be null.");

            if (renderPipelineAssetType == null)
                return SupportedMode.Unsupported;

            if (renderPipelineTypes.Contains(renderPipelineAssetType))
                return SupportedMode.Supported;

            if (renderPipelineTypes.Any(t => t.IsAssignableFrom(renderPipelineAssetType)))
                return SupportedMode.SupportedByBaseClass;

            return SupportedMode.Unsupported;
        }
    }
}
