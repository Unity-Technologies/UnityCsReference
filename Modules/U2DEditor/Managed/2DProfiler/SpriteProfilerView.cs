// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.U2D.Profiling
{
    class SpriteProfilerView : VisualElement
    {
        const string k_Uss = "U2DEditor/SpriteAtlasProfiler/SpriteProfilerStyle.uss";

        TwoPaneSplitView m_SplitView;
        SpriteAtlasProfilerInfoView m_SpriteAtlasProfilerInfoView;
        SpriteStatisticProfilerView m_SpriteStatisticProfilerView;

        public SpriteProfilerView(SpriteAtlasProfilerInfoTreeViewState state)
        {
            m_SpriteAtlasProfilerInfoView = new SpriteAtlasProfilerInfoView(state);
            var styleSheet = EditorGUIUtility.Load(k_Uss) as StyleSheet;
            this.styleSheets.Add(styleSheet);
            m_SplitView = new TwoPaneSplitView(0, 300, TwoPaneSplitViewOrientation.Horizontal);
            m_SpriteStatisticProfilerView = new SpriteStatisticProfilerView();
            m_SplitView.Add(m_SpriteStatisticProfilerView);
            m_SplitView.Add(m_SpriteAtlasProfilerInfoView.CreateGUI());
            Add(m_SplitView);
        }

        public SpriteAtlasProfilerInfoView GetSpriteAtlasProfilerInfoView()
        {
            return m_SpriteAtlasProfilerInfoView;
        }

        public SpriteStatisticProfilerView GetSpriteStatisticProfilerView()
        {
            return m_SpriteStatisticProfilerView;
        }

        public void Init(SpriteAtlasProfilerInfoBackend backend)
        {
            m_SpriteAtlasProfilerInfoView.Init(backend);
        }
    }
}
