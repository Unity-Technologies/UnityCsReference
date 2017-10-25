// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [RejectDragAndDropMaterial]
    [NativeType(Header = "Modules/SpriteMask/Public/SpriteMask.h")]
    public sealed partial class SpriteMask : Renderer
    {
        extern public int frontSortingLayerID { get; set; }
        extern public int frontSortingOrder { get; set; }
        extern public int backSortingLayerID { get; set; }
        extern public int backSortingOrder { get; set; }
        extern public float alphaCutoff { get; set; }
        extern public Sprite sprite { get; set; }
        extern public bool isCustomRangeActive {[NativeMethod("IsCustomRangeActive")] get; [NativeMethod("SetCustomRangeActive")] set; }

        internal extern Bounds GetSpriteBounds();
    }
}
