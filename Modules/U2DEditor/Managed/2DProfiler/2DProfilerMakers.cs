// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.U2D.Profiling
{
    static class U2DProfilerMakers
    {
        public const string k_SpriteCountMarkerName = "Sprite Count";
        public const string k_SpriteAtlasCountMakerName = "SpriteAtlas Count";
        public const string k_SpritesRenderedMakerName = "Sprites rendered";
        public const string k_SpriteAtlasesRenderedMakerName = "SpriteAtlases rendered";

        public readonly static string[] s_SpriteRendererSampleNames = new string[]
        {
            "SpriteRenderer.PrepareNode",
            "SpriteRenderer.Render",
            "SpriteRenderer.RenderMultiple",
        };


        public readonly static string[] s_SortingGroupSampleNames = new string[]
        {
            "SortingGroup.SortChildren",
            "SortingGroupManager.Update",
            "SortingGroup.UpdateRenderer",
            "SortingGroup.UpdateSortingGroup"
        };
    }
}
