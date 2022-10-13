using JetBrains.Annotations;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using UnityEditor;

namespace Unity.UI.Builder
{
    class PositionSection : VisualElement
    {
        [UsedImplicitly]
        public new class UxmlFactory : UxmlFactory<PositionSection, UxmlTraits> { }

        static readonly string k_FieldClassName = "unity-position-section";

        static readonly string k_UxmlPath = BuilderConstants.UtilitiesPath + "/StyleField/PositionSection.uxml";
        static readonly string k_UssPathNoExt = BuilderConstants.UtilitiesPath + "/StyleField/PositionSection";
        internal static readonly string k_PositionAnchorsFieldName = "anchors";

        PositionStyleField m_PositionTopField;
        PositionStyleField m_PositionBottomField;
        PositionStyleField m_PositionLeftField;
        PositionStyleField m_PositionRightField;
        PositionAnchors m_Anchors;

        public PositionSection()
        {
            AddToClassList(BuilderConstants.InspectorContainerClassName);
            AddToClassList(k_FieldClassName);
            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssPathNoExt + (EditorGUIUtility.isProSkin ? "Dark" : "Light") + ".uss"));
            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssPathNoExt + ".uss"));

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(k_UxmlPath);

            template.CloneTree(this);

            m_Anchors = this.Q<PositionAnchors>(k_PositionAnchorsFieldName);

            m_PositionTopField = this.Q<PositionStyleField>(StylePropertyId.Top.UssName());
            m_PositionTopField.point = m_Anchors.topPoint;
            m_PositionBottomField = this.Q<PositionStyleField>(StylePropertyId.Bottom.UssName());
            m_PositionBottomField.point = m_Anchors.bottomPoint;
            m_PositionLeftField = this.Q<PositionStyleField>(StylePropertyId.Left.UssName());
            m_PositionLeftField.point = m_Anchors.leftPoint;
            m_PositionRightField = this.Q<PositionStyleField>(StylePropertyId.Right.UssName());
            m_PositionRightField.point = m_Anchors.rightPoint;

            m_Anchors.topPoint.RegisterValueChangedCallback(e => OnPointClicked(e, m_PositionTopField));
            m_Anchors.bottomPoint.RegisterValueChangedCallback(e => OnPointClicked(e, m_PositionBottomField));
            m_Anchors.leftPoint.RegisterValueChangedCallback(e => OnPointClicked(e, m_PositionLeftField));
            m_Anchors.rightPoint.RegisterValueChangedCallback(e => OnPointClicked(e, m_PositionRightField));
        }

        void OnPointClicked(ChangeEvent<bool> evt, PositionStyleField styleField)
        {
            styleField.value = evt.newValue ? "0px" : StyleFieldConstants.KeywordAuto;
        }
    }
}
