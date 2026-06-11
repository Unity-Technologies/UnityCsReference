// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine.Bindings;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UIToolkit.Editor
{
    enum BackgroundPositionMode
    {
        Invalid,
        Horizontal,
        Vertical
    };

    [UxmlElement]
    [UsedImplicitly]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal partial class BackgroundPositionStyleField : BaseField<BackgroundPosition>
    {
        static readonly string FieldClassName = "unity-background-position-style-field";
        static readonly string UxmlPath = "UIToolkitAuthoring/Inspector/Controls/BackgroundPositionStyleField.uxml";
        static readonly string UssPathDark = "UIToolkitAuthoring/Inspector/Controls/BackgroundPositionStyleFieldDark.uss";
        static readonly string UssPathLight = "UIToolkitAuthoring/Inspector/Controls/BackgroundPositionStyleFieldLight.uss";
        public static readonly string BackgroundPositionFieldName = "position";
        public static readonly string BackgroundPositionAlignFieldName = "align";
        public static readonly string InspectorContainerClassName = "unity-ui-inspector__container";

        static readonly string BackgroundPositionXLeftTooltip = "Aligns the background image to the left side.";
        static readonly string BackgroundPositionXCenterTooltip = "Centers the background image horizontally.";
        static readonly string BackgroundPositionXRightTooltip = "Aligns the background image to the right side.";
        static readonly string BackgroundPositionYTopTooltip = "Aligns the background image to the top edge.";
        static readonly string BackgroundPositionYCenterTooltip = "Centers the background image vertically.";
        static readonly string BackgroundPositionYBottomTooltip = "Aligns the background image to the bottom edge";

        LengthField m_BackgroundPositionField;
        ToggleButtonGroup m_BackgroundPositionAlign;

        BackgroundPosition.Axis m_Mode;

        [UxmlAttribute]
        public BackgroundPosition.Axis mode
        {
            get => m_Mode;
            set
            {
                if (m_Mode == value)
                    return;
                m_Mode = value;

                OnModeChanged();
            }
        }

        public BackgroundPositionStyleField() : base(null)
        {
            AddToClassList(InspectorContainerClassName);
            AddToClassList(FieldClassName);

            var ussPath = EditorGUIUtility.isProSkin ? UssPathDark : UssPathLight;
            styleSheets.Add(EditorGUIUtility.Load(ussPath) as StyleSheet);

            var template = EditorGUIUtility.Load(UxmlPath) as VisualTreeAsset;
            template.CloneTree(this);

            m_BackgroundPositionField = this.Q<LengthField>(BackgroundPositionFieldName);
            m_BackgroundPositionAlign = this.Q<ToggleButtonGroup>(BackgroundPositionAlignFieldName);

            m_BackgroundPositionField.RegisterValueChangedCallback(e =>
            {
                UpdateBackgroundPositionField();
                e.StopPropagation();
            });

            m_BackgroundPositionAlign.RegisterValueChangedCallback(e =>
            {
                UpdateBackgroundPositionField();
                e.StopPropagation();
            });

            value = new BackgroundPosition(BackgroundPositionKeyword.Center);
            OnModeChanged();
        }

        public override void SetValueWithoutNotify(BackgroundPosition newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshSubFields();
        }

        void RefreshSubFields()
        {
            var toggleButtonGroupState = new ToggleButtonGroupState(0u, 3);

            int index = 0;

            switch (value.keyword)
            {
                case BackgroundPositionKeyword.Left:
                case BackgroundPositionKeyword.Top:
                    index = 0;
                    break;
                case BackgroundPositionKeyword.Center:
                    index = 1;
                    break;
                case BackgroundPositionKeyword.Right:
                case BackgroundPositionKeyword.Bottom:
                    index = 2;
                    break;
            }

            toggleButtonGroupState[index] = true;
            m_BackgroundPositionAlign.SetValueWithoutNotify(toggleButtonGroupState);

            m_BackgroundPositionField.SetEnabled(index != 1);
            m_BackgroundPositionField.SetValueWithoutNotify(value.offset);
        }

        void UpdateBackgroundPositionField()
        {
            // Rebuild value from sub fields
            var newPosition = new BackgroundPosition();

            var valueAlign = m_BackgroundPositionAlign.value;
            var selectedAlign = valueAlign.GetActiveOptions(stackalloc int[valueAlign.length]);
            int indexAlign = selectedAlign[0];


            if (indexAlign == 0) newPosition.keyword = (m_Mode == BackgroundPosition.Axis.Horizontal) ? BackgroundPositionKeyword.Left : BackgroundPositionKeyword.Top;
            else if (indexAlign == 1) newPosition.keyword = BackgroundPositionKeyword.Center;
            else newPosition.keyword = (m_Mode == BackgroundPosition.Axis.Horizontal) ? BackgroundPositionKeyword.Right : BackgroundPositionKeyword.Bottom;

            newPosition.offset = m_BackgroundPositionField.value;

            value = newPosition;
        }

        void OnModeChanged()
        {
            string[] classNames;
            string[] tooltips;

            if (m_Mode == BackgroundPosition.Axis.Horizontal)
            {
                classNames = new[] { "background-position-x-left", "background-position-x-center", "background-position-x-right" };
                tooltips = new[] { BackgroundPositionXLeftTooltip, BackgroundPositionXCenterTooltip, BackgroundPositionXRightTooltip };
            }
            else
            {
                classNames = new[] { "background-position-y-top", "background-position-y-center", "background-position-y-bottom" };
                tooltips = new[] { BackgroundPositionYTopTooltip, BackgroundPositionYCenterTooltip, BackgroundPositionYBottomTooltip };
            }

            m_BackgroundPositionAlign.Clear();

            for (var i = 0; i < classNames.Length; ++i)
            {
                var button = new Button { tooltip = tooltips[i] };
                button.AddToClassList(classNames[i]);
                m_BackgroundPositionAlign.Add(button);
            }

            var toggleButtonGroupState = new ToggleButtonGroupState(2u, 3);
            m_BackgroundPositionAlign.SetValueWithoutNotify(toggleButtonGroupState);
        }
    }
}
