// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Rendering
{
    public abstract class RenderPipelineAsset : ScriptableObject
    {
        internal RenderPipeline InternalCreatePipeline()
        {
            try
            {
                EnsureGlobalSettings();
                return CreatePipeline();
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

            return null;
        }

        public virtual int terrainBrushPassIndex => (int)UnityEngine.Rendering.RenderQueue.GeometryLast;

        public virtual string[] renderingLayerMaskNames => null;
        public virtual string[] prefixedRenderingLayerMaskNames => null;

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

        public virtual string renderPipelineShaderTag => string.Empty;

        protected abstract RenderPipeline CreatePipeline();

        protected virtual void EnsureGlobalSettings()
        {

        }

        protected virtual void OnValidate()
        {
            if (RenderPipelineManager.s_CurrentPipelineAsset == this)
            {
                RenderPipelineManager.CleanupRenderPipeline();
                RenderPipelineManager.PrepareRenderPipeline(this);
            }
        }

        protected virtual void OnDisable()
        {
            RenderPipelineManager.CleanupRenderPipeline();
        }
    }
}
