// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Rendering;

namespace UnityEngine
{
    [RejectDragAndDropMaterial]
    [NativeType(Header = "Modules/SpriteMask/Public/SpriteMask.h")]
    public sealed partial class SpriteMask : Renderer
    {
        public enum MaskSource
        {
            Sprite = 0,
            SupportedRenderers = 1,
        }

        extern public int frontSortingLayerID { get; set; }
        extern public int frontSortingOrder { get; set; }
        extern public int backSortingLayerID { get; set; }
        extern public int backSortingOrder { get; set; }
        extern public float alphaCutoff { get; set; }
        extern public Sprite sprite { get; set; }
        extern public bool isCustomRangeActive {[NativeMethod("IsCustomRangeActive")] get; [NativeMethod("SetCustomRangeActive")] set; }

        public extern SpriteSortPoint spriteSortPoint { get; set; }

        public extern MaskSource maskSource { get; set; }

        internal extern Renderer cachedSupportedRenderer { get; }

        internal extern Bounds GetSpriteBounds();
    }

    [NativeHeader("Modules/SpriteMask/Public/ScriptBindings/SpriteMask.bindings.h")]
    [StaticAccessor("SpriteUtilityBindings", StaticAccessorType.DoubleColon)]
    internal static class SpriteMaskUtility
    {
        extern internal static bool HasSpriteMaskInLayerRange(SortingLayerRange range);
    }
}
