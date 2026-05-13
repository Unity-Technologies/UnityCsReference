// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [UxmlElement]
    sealed partial class PositionFoldout : VisualElement
    {
        static readonly string k_FieldClassName = "unity-position-section";

        private const string k_UxmlPath = "UIToolkitAuthoring/Inspector/Controls/PositionFoldout.uxml";
        static readonly string k_UssPathNoExt = "UIToolkitAuthoring/Inspector/Controls/PositionSection";

        internal static readonly string k_PositionAnchorsFieldName = "anchors";

        Foldout m_Foldout;
        StyleLengthField m_TopField;
        StyleLengthField m_BottomField;
        StyleLengthField m_LeftField;
        StyleLengthField m_RightField;
        PositionAnchors m_Anchors;

        public PositionFoldout()
        {
            AddToClassList(k_FieldClassName);

            var vta = EditorGUIUtility.Load(k_UxmlPath) as VisualTreeAsset;
            vta.CloneTree(this);

            styleSheets.Add(EditorGUIUtility.Load(k_UssPathNoExt + ".uss") as StyleSheet);
            styleSheets.Add(EditorGUIUtility.Load(k_UssPathNoExt + (EditorGUIUtility.isProSkin ? "Dark" : "Light") + ".uss") as StyleSheet);

            m_Anchors = this.Q<PositionAnchors>(k_PositionAnchorsFieldName);

            m_TopField = this.Q<StyleLengthField>("top");
            m_BottomField = this.Q<StyleLengthField>("bottom");
            m_LeftField = this.Q<StyleLengthField>("left");
            m_RightField = this.Q<StyleLengthField>("right");

            m_TopField.RegisterCallback<PropertyChangedEvent>(e =>
            {
                if (e.property == BaseField<StyleLength>.valueProperty)
                    UpdateAnchors();
            });
            m_BottomField.RegisterCallback<PropertyChangedEvent>(e =>
            {
                if (e.property == BaseField<StyleLength>.valueProperty)
                    UpdateAnchors();
            });
            m_LeftField.RegisterCallback<PropertyChangedEvent>(e =>
            {
                if (e.property == BaseField<StyleLength>.valueProperty)
                    UpdateAnchors();
            });
            m_RightField.RegisterCallback<PropertyChangedEvent>(e =>
            {
                if (e.property == BaseField<StyleLength>.valueProperty)
                    UpdateAnchors();
            });

            m_Anchors.topPoint.RegisterValueChangedCallback(e => m_TopField.value = e.newValue ? 0 : Length.Auto());
            m_Anchors.bottomPoint.RegisterValueChangedCallback(e => m_BottomField.value = e.newValue ? 0 : Length.Auto());
            m_Anchors.leftPoint.RegisterValueChangedCallback(e => m_LeftField.value = e.newValue ? 0 : Length.Auto());
            m_Anchors.rightPoint.RegisterValueChangedCallback(e => m_RightField.value = e.newValue ? 0 : Length.Auto());

            m_Foldout = this.Q<Foldout>("anchors-foldout");

            var positionField = this.Q<PositionStyleEnumField>();
            UpdatePositionAnchorsFoldoutState(positionField.value);
            positionField.RegisterValueChangedCallback(e => UpdatePositionAnchorsFoldoutState(e.newValue));

            UpdateAnchors();
        }

        void UpdateAnchors()
        {
            m_Anchors.topPoint.SetValueWithoutNotify(m_TopField.value != Length.Auto());
            m_Anchors.rightPoint.SetValueWithoutNotify(m_RightField.value != Length.Auto());
            m_Anchors.bottomPoint.SetValueWithoutNotify(m_BottomField.value != Length.Auto());
            m_Anchors.leftPoint.SetValueWithoutNotify(m_LeftField.value != Length.Auto());
        }

        void UpdatePositionAnchorsFoldoutState(StyleEnum<Position> value)
        {
            m_Foldout.text = (value == Position.Absolute) ? "Anchors" : "Offsets";
            m_Foldout.EnableInClassList("relative", value == Position.Relative);
        }
    }
}
