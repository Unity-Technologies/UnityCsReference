// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Rendering
{
    public abstract partial class RenderPipelineAsset : ScriptableObject
    {
        internal RenderPipeline InternalCreatePipeline()
        {
            RenderPipeline pipeline = null;
            try
            {
                EnsureGlobalSettings();
                pipeline = CreatePipeline();
            }
            catch (InvalidImportException)
            {
                // This can be called at a time where AssetDatabase is not available for loading.
                // When this happens, the GUID can be get but the resource loaded will be null.
                // In SRP using the ResourceReloader mechanism in CoreRP, it checks this and add InvalidImport data when this occurs.
            }

            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (pipelineType != null && pipeline != null && pipelineType != pipeline.GetType())
                Debug.LogWarning($"{GetType()}.{nameof(CreatePipeline)}. Type error: {pipelineType} is different from {pipeline.GetType()}. The property {nameof(pipelineType)} must return the same type");
            return pipeline;
        }

        public virtual int terrainBrushPassIndex => (int)UnityEngine.Rendering.RenderQueue.GeometryLast;

        public virtual Material defaultMaterial => null;

        public virtual Shader autodeskInteractiveShader => null;

        public virtual Shader autodeskInteractiveTransparentShader => null;

        public virtual Shader autodeskInteractiveMaskedShader => null;

        public virtual Shader terrainDetailLitShader => null;

        public virtual Shader terrainDetailGrassShader => null;

        public virtual Shader terrainDetailGrassBillboardShader => null;

        public virtual Material defaultParticleMaterial => null;

        public virtual Material defaultLineMaterial => null;

        public virtual Material defaultTerrainMaterial => null;

        public virtual Material defaultUIMaterial => null;

        public virtual Material defaultUIOverdrawMaterial => null;

        public virtual Material defaultUIETC1SupportedMaterial => null;

        public virtual Material default2DMaterial => null;

        public virtual Material default2DMaskMaterial => null;

        public virtual Shader defaultShader => null;

        public virtual Shader defaultSpeedTree7Shader => null;

        public virtual Shader defaultSpeedTree8Shader => null;

        public virtual Shader defaultSpeedTree9Shader => null;

        public virtual string renderPipelineShaderTag
        {
            get
            {
                Debug.LogWarning($"The property {nameof(renderPipelineShaderTag)} has not been overridden. At build time, any shader variants that use any RenderPipeline tag will be stripped.");
                return string.Empty;
            }
        }

        protected abstract RenderPipeline CreatePipeline();

        public virtual Type pipelineType
        {
            get
            {
                Debug.LogWarning($"You must either inherit from {nameof(RenderPipelineAsset)}<TRenderPipeline> or override {nameof(pipelineType)} property.");
                return null;
            }
        }

        internal string pipelineTypeFullName => pipelineType?.FullName ?? string.Empty;

        protected virtual void EnsureGlobalSettings()
        {

        }

        protected virtual void OnValidate()
        {
            RenderPipelineManager.RecreateCurrentPipeline(this);
        }

        protected virtual void OnDisable()
        {
            RenderPipelineManager.CleanupRenderPipeline();
        }
    }

    public abstract partial class RenderPipelineAsset<TRenderPipeline> : RenderPipelineAsset
        where TRenderPipeline : RenderPipeline
    {
        public sealed override Type pipelineType => typeof(TRenderPipeline);

        public override string renderPipelineShaderTag => typeof(TRenderPipeline).Name;
    }
}
