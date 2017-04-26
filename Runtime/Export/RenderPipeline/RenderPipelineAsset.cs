// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering
{
    public abstract class RenderPipelineAsset : ScriptableObject, IRenderPipelineAsset
    {
        private readonly List<IRenderPipeline> m_CreatedPipelines = new List<IRenderPipeline>();

        public void DestroyCreatedInstances()
        {
            foreach (var pipeline in m_CreatedPipelines)
                pipeline.Dispose();

            m_CreatedPipelines.Clear();
        }

        public IRenderPipeline CreatePipeline()
        {
            var pipe = InternalCreatePipeline();
            if (pipe != null)
                m_CreatedPipelines.Add(pipe);

            return pipe;
        }

        protected abstract IRenderPipeline InternalCreatePipeline();

        protected IEnumerable<IRenderPipeline> CreatedInstances()
        {
            return m_CreatedPipelines;
        }

        private void OnValidate()
        {
            DestroyCreatedInstances();
        }

        private void OnDisable()
        {
            DestroyCreatedInstances();
        }
    }
}
