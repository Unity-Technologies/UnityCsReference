// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Rendering;

namespace UnityEngine
{
    ///<summary>A component for masking Sprites and Particles.</summary>
    ///<remarks>By default it will mask all Sorting Layers. A custom range of Sorting Layers can be set. If a SortingGroup is present, it will act local to the SortingGroup.</remarks>
    [RejectDragAndDropMaterial]
    [global::UnityEngine.NativeClass("SpriteMask", PersistentTypeId = 331)]
    [NativeHeader("Modules/SpriteMask/Public/SpriteMask.h")]
    public sealed partial class SpriteMask : Renderer
    {
        internal SpriteMask(global::UnityEngine.EntityId id) : base(id) {}
        public SpriteMask() {}
        ///<summary>Sets which source to use when generating the mask.</summary>
        public enum MaskSource
        {
            ///<summary>Use a <see cref="Sprite" /> as a source for the mask.</summary>
            Sprite = 0,
            ///<summary>Use a supported <see cref="Renderer" /> as a source of the mask.</summary>
            ///<remarks>Supported Renderers are: SpriteRenderer, SpriteShapeRenderer and TilemapRenderer.</remarks>
            SupportedRenderers = 1,
        }

        ///<summary>Unique ID of the sorting layer defining the start of the custom range.</summary>
        ///<remarks>Sprites sorted before this sorting layer will not be masked.</remarks>
        extern public int frontSortingLayerID { get; set; }
        ///<summary>Order within the front sorting layer defining the start of the custom range.</summary>
        extern public int frontSortingOrder { get; set; }
        ///<summary>Unique ID of the sorting layer defining the end of the custom range.</summary>
        ///<remarks>Sprites sorted after this sorting layer will not be masked.</remarks>
        extern public int backSortingLayerID { get; set; }
        ///<summary>Order within the back sorting layer defining the end of the custom range.</summary>
        extern public int backSortingOrder { get; set; }
        ///<summary>The minimum alpha value used by the mask to select the area of influence defined over the mask's sprite.</summary>
        extern public float alphaCutoff { get; set; }
        ///<summary>The Sprite used to define the mask.</summary>
        extern public Sprite sprite { get; set; }
        ///<summary>Mask sprites from front to back sorting values only.</summary>
        extern public bool isCustomRangeActive {[NativeMethod("IsCustomRangeActive")] get; [NativeMethod("SetCustomRangeActive")] set; }

        ///<summary>Determines the position of the <see cref="Sprite" /> used for sorting the <see cref="SpriteMask" />.</summary>
        public extern SpriteSortPoint spriteSortPoint { get; set; }

        ///<summary>The source used for generating the mask for this <see cref="SpriteMask" />.</summary>
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
