// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Properties;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    sealed class PositionFoldout : VisualElement, INotifyCompositeStylePropertyChanged<StyleLength>
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new PositionFoldout();
        }

        static readonly BindingId topProperty = nameof(top);
        static readonly BindingId rightProperty = nameof(right);
        static readonly BindingId bottomProperty = nameof(bottom);
        static readonly BindingId leftProperty = nameof(left);

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
                var previousValue = m_Top;
                m_Top = value;
                Refresh();
                NotifyStylePropertyChanged(topProperty, previousValue, m_Top);
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
                var previousValue = m_Right;
                m_Right = value;
                Refresh();
                NotifyStylePropertyChanged(rightProperty, previousValue, m_Right);
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
                var previousValue = m_Bottom;
                m_Bottom = value;
                Refresh();
                NotifyStylePropertyChanged(bottomProperty, previousValue, m_Bottom);
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
                var previousValue = m_Left;
                m_Left = value;
                Refresh();
                NotifyStylePropertyChanged(leftProperty, previousValue, m_Left);
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

        private void Refresh()
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

        public void SetValue(BindingId id, StyleLength v, bool notify)
        {
            if (id == topProperty)
            {
                if (notify)
                    top = v;
                else
                    m_Top = v;
            }
            else if (id == rightProperty)
            {
                if (notify)
                    right = v;
                else
                    m_Right = v;
            }
            else if (id == bottomProperty)
            {
                if (notify)
                    bottom = v;
                else
                    m_Bottom = v;
            }
            else if (id == leftProperty)
            {
                if (notify)
                    left = v;
                else
                    m_Left = v;
            }

            if (!notify)
                Refresh();
        }

        public void NotifyStylePropertyChanged(BindingId id, StyleLength previousValue, StyleLength newValue)
        {
            this.NotifyStylePropertyChanged(this, id, previousValue, newValue);
            NotifyPropertyChanged(id);
        }
    }
}
