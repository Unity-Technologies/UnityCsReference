// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    public abstract class RenderPipelineAsset : ScriptableObject
    {
        private readonly List<RenderPipeline> m_Pipelines = new List<RenderPipeline>();

        internal void DestroyInstances()
        {
            foreach (var pipeline in m_Pipelines)
                pipeline.Dispose();
            m_Pipelines.Clear();
        }

        internal RenderPipeline InternalCreatePipeline()
        {
            var pipeline = CreatePipeline();
            if (pipeline != null)
                m_Pipelines.Add(pipeline);
            return pipeline;
        }

        public virtual int terrainBrushPassIndex => (int)UnityEngine.Rendering.RenderQueue.GeometryLast;

        public virtual string[] renderingLayerMaskNames => null;

        public virtual Material defaultMaterial => null;

        public virtual Shader autodeskInteractiveShader => null;

        public virtual Shader autodeskInteractiveTransparentShader => null;

        public virtual Shader autodeskInteractiveMaskedShader => null;

        public virtual Material defaultParticleMaterial => null;

        public virtual Material defaultLineMaterial => null;

        public virtual Material defaultTerrainMaterial => null;

        public virtual Material defaultUIMaterial => null;

        public virtual Material defaultUIOverdrawMaterial => null;

        public virtual Material defaultUIETC1SupportedMaterial => null;

        public virtual Material default2DMaterial => null;

        public virtual Shader defaultShader => null;

        protected abstract RenderPipeline CreatePipeline();

        protected IEnumerable<RenderPipeline> instances => m_Pipelines;

        protected virtual void OnValidate()
        {
            DestroyInstances();
        }

        protected virtual void OnDisable()
        {
            DestroyInstances();
        }
    }
}
