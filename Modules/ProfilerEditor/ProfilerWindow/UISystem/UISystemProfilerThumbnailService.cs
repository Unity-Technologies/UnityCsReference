// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    [InitializeOnLoad]
    internal class UISystemProfilerRenderService : IDisposable
    {
        private bool m_Disposed;

        public UISystemProfilerRenderService()
        {}

        public void Dispose()
        {
            m_Disposed = true;
        }

        private Texture2D Generate(int frameIndex, int renderDataIndex, int renderDataCount, bool overdraw)
        {
            return m_Disposed ? null : ProfilerProperty.UISystemProfilerRender(frameIndex, renderDataIndex, renderDataCount, overdraw);
        }

        public Texture2D GetThumbnail(int frameIndex, int renderDataIndex, int infoRenderDataCount, bool overdraw)
        {
            return Generate(frameIndex, renderDataIndex, infoRenderDataCount, overdraw);
        }
    }
}
