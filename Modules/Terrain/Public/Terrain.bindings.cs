// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;


namespace UnityEngine
{
    [NativeHeader("Modules/Terrain/Public/Terrain.h")]
    public sealed partial class Terrain : Behaviour
    {
        [NativeProperty("GarbageCollectRenderers")]
        extern public bool freeUnusedRenderingResources
        {
            get;
            set;
        }
    }
}

