// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    partial class RenderTexture
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use RenderTexture.autoGenerateMips instead (UnityUpgradable) -> autoGenerateMips", false)]
        public bool generateMips
        {
            get { return autoGenerateMips; }
            set { autoGenerateMips = value; }
        }
    }
}
