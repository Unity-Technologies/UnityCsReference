// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Rendering
{
    public abstract class RenderPipelineAsset : ScriptableObject
    {
        internal RenderPipeline InternalCreatePipeline()
        {
            RenderPipeline pipeline = null;
            try
            {
                pipeline = CreatePipeline();
            }
            catch (System.Exception e)
            {
                // This can be called at a time where AssetDatabase is not available for loading.
                // When this happens, the GUID can be get but the resource loaded will be null.
                // In SRP using the ResourceReloader mechanism in CoreRP, it checks this and add InvalidImport data when this occurs.
                if (!(e.Data.Contains("InvalidImport") && e.Data["InvalidImport"] is int && (int)e.Data["InvalidImport"] == 1))
                    Debug.LogException(e);
            }
            return pipeline;
        }

        public virtual int terrainBrushPassIndex => (int)UnityEngine.Rendering.RenderQueue.GeometryLast;

        public virtual string[] renderingLayerMaskNames => null;

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

        public virtual Shader defaultShader => null;

        public virtual Shader defaultSpeedTree7Shader => null;

        public virtual Shader defaultSpeedTree8Shader => null;

        protected abstract RenderPipeline CreatePipeline();

        protected virtual void OnValidate()
        {
            RenderPipelineManager.CleanupRenderPipeline();
        }

        protected virtual void OnDisable()
        {
            RenderPipelineManager.CleanupRenderPipeline();
        }
    }
}
