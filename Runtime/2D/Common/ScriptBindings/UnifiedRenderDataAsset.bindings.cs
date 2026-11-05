// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.U2D
{
    // A UnifiedRenderData saved as an asset; the handle itself has no identity on disk.
    // Texture bindings and GPU buffers are not serialized; both are rebuilt on the next draw.
    [NativeHeader("Runtime/2D/Common/UnifiedRenderDataAsset.h")]
    [NativeClass("UnifiedRenderDataAsset", PersistentTypeId = 0x1B6D3F5A)]
    internal sealed class UnifiedRenderDataAsset : Object
    {
        internal UnifiedRenderDataAsset(global::UnityEngine.EntityId id) : base(id) {}

        public UnifiedRenderDataAsset()
        {
            SetEntityIdFromConstructor(Internal_Create());
        }

        // Returns a handle onto the render data this asset holds, not a copy; the caller
        // must Dispose() it. Null once the asset is destroyed. Main thread only.
        public UnifiedRenderData GetRenderData()
        {
            IntPtr renderData = Internal_GetRenderData();
            return IntPtr.Zero == renderData ? null : new UnifiedRenderData(renderData);
        }

        [NativeName("CreateFromScript")]
        extern private static EntityId Internal_Create();

        [NativeHeader("Runtime/2D/Common/ScriptBindings/UnifiedRenderData.bindings.h")]
        [FreeFunction(Name = "UnifiedRenderData_Bindings::ExtractFromAsset", HasExplicitThis = true)]
        extern private IntPtr Internal_GetRenderData();
    }
}
