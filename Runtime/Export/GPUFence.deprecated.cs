// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using ShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode;
using UnityEngine.Scripting;
using uei = UnityEngine.Internal;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


namespace UnityEngine.Rendering
{
    // Old SynchronisationStage enum, stored here for backwards compatibility
    public enum SynchronisationStage
    {
        VertexProcessing = 0,
        PixelProcessing = 1,
    }

    [Obsolete("GPUFence has been deprecated. Use GraphicsFence instead (UnityUpgradable) -> GraphicsFence", false)]
    public struct GPUFence
    {
        public bool passed
        {
            get
            {
                return true;
            }
        }
    }
}
