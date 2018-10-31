// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Rendering;
using System;

namespace UnityEngine.Rendering
{
    public struct ScopedRenderPass : IDisposable
    {
        ScriptableRenderContext m_Context;

        internal ScopedRenderPass(ScriptableRenderContext context)
        {
            m_Context = context;
        }

        public void Dispose()
        {
            try
            {
                m_Context.EndRenderPass();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"The {nameof(ScopedRenderPass)} instance is not valid. This can happen if it was constructed using the default constructor.", e);
            }
        }
    }

    public struct ScopedSubPass : IDisposable
    {
        ScriptableRenderContext m_Context;

        internal ScopedSubPass(ScriptableRenderContext context)
        {
            m_Context = context;
        }

        public void Dispose()
        {
            try
            {
                m_Context.EndSubPass();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"The {nameof(ScopedSubPass)} instance is not valid. This can happen if it was constructed using the default constructor.", e);
            }
        }
    }
}
