using UnityEditor.UIElements.Debugger;

namespace Unity.UI.Builder
{
    internal class BuilderInspectorMatchingSelectors
    {
        BuilderInspector m_Inspector;
        MatchedRulesExtractor m_MatchedRulesExtractor;

        public MatchedRulesExtractor matchedRulesExtractor => m_MatchedRulesExtractor;

        public BuilderInspectorMatchingSelectors(BuilderInspector inspector)
        {
            m_Inspector = inspector;

            m_MatchedRulesExtractor = new MatchedRulesExtractor();
        }

        public void GetElementMatchers()
        {
            if (m_Inspector.currentVisualElement == null || m_Inspector.currentVisualElement.elementPanel == null)
                return;

            m_MatchedRulesExtractor.selectedElementRules.Clear();
            m_MatchedRulesExtractor.selectedElementStylesheets.Clear();
            m_MatchedRulesExtractor.FindMatchingRules(m_Inspector.currentVisualElement);
        }
    }
}
