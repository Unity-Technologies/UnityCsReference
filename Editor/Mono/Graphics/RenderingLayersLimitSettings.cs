// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    public abstract class RenderingLayersLimitSettings : IRenderPipelineGraphicsSettings
    {
        #region Version

        internal enum Version
        {
            Initial = 0,
        }

        [SerializeField] [HideInInspector] Version m_Version = Version.Initial;

        /// <summary>Current version.</summary>
        public int version => (int)m_Version;

        #endregion

        protected abstract int maxRenderingLayersForPipeline { get; }

        public int maxSupportedRenderingLayers => maxRenderingLayersForPipeline is > 1 and <= 32 ? maxRenderingLayersForPipeline : 32;
    }
}
