// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.Rendering
{
    public interface IRenderPipeline : IDisposable
    {
        bool disposed { get; }

        void Render(ScriptableRenderContext renderContext, Camera[] cameras);
    }
}
