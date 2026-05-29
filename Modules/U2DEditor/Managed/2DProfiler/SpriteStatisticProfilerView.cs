// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.U2D.Profiling
{
    class SpriteStatisticProfilerView : VisualElement
    {
        const string k_UXML = "U2DEditor/SpriteAtlasProfiler/SpriteStatisticProfilerView/SpriteStatisticProfilerView.uxml";

        Label m_SpriteCountLabel;
        Label m_SpriteAtlasCountLabel;
        Label m_SpritesRenderedLabel;
        Label m_SpriteAtlasesRenderedLabel;
        Label m_SpriteRenderingTimeLabel;
        Label m_SortingGroupTimeLabel;
        Toggle m_LiveUpdate;
        public SpriteStatisticProfilerView()
        {
            var visualTree = EditorGUIUtility.Load(k_UXML) as VisualTreeAsset;
            visualTree.CloneTree(this);

            m_SpriteCountLabel = this.Q<Label>("SpriteCountLabel");
            m_SpriteAtlasCountLabel = this.Q<Label>("SpriteAtlasCountLabel");
            m_SpritesRenderedLabel = this.Q<Label>("SpritesRenderedLabel");
            m_SpriteAtlasesRenderedLabel = this.Q<Label>("SpriteAtlasesRenderedLabel");
            m_SpriteRenderingTimeLabel = this.Q<Label>("SpriteRenderingTimeLabel");
            m_SortingGroupTimeLabel = this.Q<Label>("SortingGroupTimeLabel");
            m_LiveUpdate = this.Q<Toggle>("EnableStatisticsToggle");
        }

        public void SetStatistic(long spriteCount, long spriteAtlasCount, long spritesRendered, long spriteAtlasesRendered, float spriteRenderingTime, float sortingGroupTime)
        {
            m_SpriteCountLabel.text = spriteCount.ToString();
            m_SpriteAtlasCountLabel.text = spriteAtlasCount.ToString();
            m_SpritesRenderedLabel.text = spritesRendered.ToString();
            m_SpriteAtlasesRenderedLabel.text = spriteAtlasesRendered.ToString();
            m_SpriteRenderingTimeLabel.text = $"{spriteRenderingTime:F2}ms";
            m_SortingGroupTimeLabel.text = $"{sortingGroupTime:F2}ms";
        }

        public bool IsLiveUpdateEnabled()
        {
            return m_LiveUpdate.value;
        }
    }
}
