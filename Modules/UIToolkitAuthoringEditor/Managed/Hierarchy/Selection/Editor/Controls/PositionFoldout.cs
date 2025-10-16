// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    class PositionFoldout : VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new PositionFoldout();
        }

        static readonly string k_FieldClassName = "unity-position-section";

        private const string k_UxmlPath = "UIToolkitAuthoring/Inspector/Controls/PositionFoldout.uxml";
        static readonly string k_UssPathNoExt = "UIToolkitAuthoring/Inspector/Controls/PositionSection";

        internal static readonly string k_PositionAnchorsFieldName = "anchors";

        StyleLength m_Top, m_Right, m_Bottom, m_Left;

        [CreateProperty]
        public StyleLength top
        {
            get => m_Top;
            set
            {
                if (m_Top == value)
                    return;
                m_Top = value;
                Refresh();
                NotifyPropertyChanged(nameof(top));
            }
        }

        [CreateProperty]
        public StyleLength right
        {
            get => m_Right;
            set
            {
                if (m_Right == value)
                    return;
                m_Right = value;
                Refresh();
                NotifyPropertyChanged(nameof(right));
            }
        }

        [CreateProperty]
        public StyleLength bottom
        {
            get => m_Bottom;
            set
            {
                if (m_Bottom == value)
                    return;
                m_Bottom = value;
                Refresh();
                NotifyPropertyChanged(nameof(bottom));
            }
        }

        [CreateProperty]
        public StyleLength left
        {
            get => m_Left;
            set
            {
                if (m_Left == value)
                    return;
                m_Left = value;
                Refresh();
                NotifyPropertyChanged(nameof(left));
            }
        }

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

            m_Top = new StyleLength(Length.Auto());
            m_Right = new StyleLength(Length.Auto());
            m_Bottom = new StyleLength(Length.Auto());
            m_Left = new StyleLength(Length.Auto());

            m_Anchors = this.Q<PositionAnchors>(k_PositionAnchorsFieldName);

            m_TopField = this.Q<StyleLengthField>("top");
            m_TopField.RegisterValueChangedCallback(e => top = e.newValue);
            m_BottomField = this.Q<StyleLengthField>("bottom");
            m_BottomField.RegisterValueChangedCallback(e => bottom = e.newValue);
            m_LeftField = this.Q<StyleLengthField>("left");
            m_LeftField.RegisterValueChangedCallback(e => left = e.newValue);
            m_RightField = this.Q<StyleLengthField>("right");
            m_RightField.RegisterValueChangedCallback(e => right = e.newValue);

            m_Anchors.topPoint.RegisterValueChangedCallback(e => top = e.newValue ? 0 : Length.Auto());
            m_Anchors.bottomPoint.RegisterValueChangedCallback(e => bottom = e.newValue ? 0 : Length.Auto());
            m_Anchors.leftPoint.RegisterValueChangedCallback(e => left = e.newValue ? 0 : Length.Auto());
            m_Anchors.rightPoint.RegisterValueChangedCallback(e => right = e.newValue ? 0 : Length.Auto());

            m_Foldout = this.Q<Foldout>("anchors-foldout");

            var positionField = this.Q<PositionStyleEnumField>();
            UpdatePositionAnchorsFoldoutState(positionField.value);
            positionField.RegisterValueChangedCallback(e => UpdatePositionAnchorsFoldoutState(e.newValue));

            Refresh();
        }

        protected void Refresh()
        {
            m_TopField.value = m_Top;
            m_RightField.value = m_Right;
            m_BottomField.value = m_Bottom;
            m_LeftField.value = m_Left;

            UpdateAnchors();
        }

        void UpdateAnchors()
        {
            m_Anchors.topPoint.SetValueWithoutNotify(top != Length.Auto());
            m_Anchors.rightPoint.SetValueWithoutNotify(right != Length.Auto());
            m_Anchors.bottomPoint.SetValueWithoutNotify(bottom != Length.Auto());
            m_Anchors.leftPoint.SetValueWithoutNotify(left != Length.Auto());
        }

        void UpdatePositionAnchorsFoldoutState(StyleEnum<Position> value)
        {
            m_Foldout.text = (value == Position.Absolute) ? "Anchors" : "Offsets";
            m_Foldout.EnableInClassList("relative", value == Position.Relative);
        }
    }
}
