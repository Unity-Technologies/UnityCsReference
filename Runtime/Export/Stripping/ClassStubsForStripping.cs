// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // These classes are only used by native code, and only here to prevent them from being stripped.
    [NativeClass("LowerResBlitTexture", PersistentTypeId = 0x583d8c3f)]
    internal class LowerResBlitTexture : Object
    {
        internal LowerResBlitTexture(global::UnityEngine.EntityId id) : base(id) {}
        [RequiredByNativeCode]
        internal void LowerResBlitTextureDontStripMe() {}
    }

    [global::UnityEngine.NativeClass("PreloadData", PersistentTypeId = 150)]
    internal class PreloadData : Object
    {
        internal PreloadData(global::UnityEngine.EntityId id) : base(id) {}
        [RequiredByNativeCode]
        internal void PreloadDataDontStripMe() {}
    }
}
