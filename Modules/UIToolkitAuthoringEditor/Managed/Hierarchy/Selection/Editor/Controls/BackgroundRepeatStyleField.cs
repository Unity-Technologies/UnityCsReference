// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;
using UnityEditor;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [UxmlElement]
    [UsedImplicitly]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal partial class BackgroundRepeatStyleField : BaseField<BackgroundRepeat>
    {
        static readonly string s_FieldClassName = "unity-background-repeat-style-field";
        static readonly string s_UxmlPath = "UIToolkitAuthoring/Inspector/Controls/BackgroundRepeatStyleField.uxml";
        static readonly string s_UssPath = "UIToolkitAuthoring/Inspector/Controls/BackgroundRepeatStyleField.uss";
        static readonly string s_IconPath = "UIToolkit/Icons";

        public static readonly string s_BackgroundRepeatXFieldName = "repeatx";
        public static readonly string s_BackgroundRepeatYFieldName = "repeaty";

        static readonly string NoRepeatTooltip = "value: no-repeat\n\nBackground image will not be repeated and will be displayed once in the background.";
        static readonly string RepeatTooltip = "value: repeat\n\nBackground image will only repeat horizontally, creating a tiled effect from left to right.";
        static readonly string RoundTooltip = "value: round\n\nBackground image will be repeated as needed to fill the available space, while also resizing the image to ensure that there is no remaining space at the edges.";
        static readonly string SpaceTooltip ="value: space\n\nBackground image is repeated as much as possible without clipping. The first and last images are pinned to either side of the element, and whitespace is distributed evenly between the images.";

        ToggleButtonGroup m_BackgroundRepeatXField;
        ToggleButtonGroup m_BackgroundRepeatYField;

        static readonly int[] k_RepeatButtonMapping = { 0, 3, 2, 1 };
        static readonly int[] k_ButtonRepeatMapping = { 0, 1, 2, 3 };

        public BackgroundRepeatStyleField() : this(null) { }

        public BackgroundRepeatStyleField(string label) : base(label)
        {
            AddToClassList(s_FieldClassName);

            styleSheets.Add(EditorGUIUtility.Load(s_UssPath) as StyleSheet);

            var template = EditorGUIUtility.Load(s_UxmlPath) as VisualTreeAsset;
            template.CloneTree(this);

            m_BackgroundRepeatXField = this.Q<ToggleButtonGroup>(s_BackgroundRepeatXFieldName);
            m_BackgroundRepeatYField = this.Q<ToggleButtonGroup>(s_BackgroundRepeatYFieldName);

            m_BackgroundRepeatXField.RegisterValueChangedCallback(e =>
            {
                UpdateBackgroundRepeatField();
                e.StopPropagation();
            });
            m_BackgroundRepeatYField.RegisterValueChangedCallback(e =>
            {
                UpdateBackgroundRepeatField();
                e.StopPropagation();
            });

            var fieldTooltip = "Controls how a background image is repeated within an element.";
            m_BackgroundRepeatXField.labelElement.tooltip = fieldTooltip;
            m_BackgroundRepeatYField.labelElement.tooltip = fieldTooltip;

            var iconImageNoRepeat = UIResources.LoadIcon($"{s_IconPath}/repeatBG_OFF.png");
            var iconImageRepeat = UIResources.LoadIcon($"{s_IconPath}/repeatBG_ON.png");
            var iconImageRound = UIResources.LoadIcon($"{s_IconPath}/repeatBG_ROUND.png");
            var iconImageSpace = UIResources.LoadIcon($"{s_IconPath}/repeatBG_SPACE.png");

            m_BackgroundRepeatXField.Add(new Button() { iconImage = iconImageNoRepeat, tooltip = NoRepeatTooltip });
            m_BackgroundRepeatXField.Add(new Button() { iconImage = iconImageSpace, tooltip = SpaceTooltip });
            m_BackgroundRepeatXField.Add(new Button() { iconImage = iconImageRound, tooltip = RoundTooltip });
            m_BackgroundRepeatXField.Add(new Button() { iconImage = iconImageRepeat, tooltip = RepeatTooltip });

            m_BackgroundRepeatYField.Add(new Button() { iconImage = iconImageNoRepeat, tooltip = NoRepeatTooltip });
            m_BackgroundRepeatYField.Add(new Button() { iconImage = iconImageSpace, tooltip = SpaceTooltip });
            m_BackgroundRepeatYField.Add(new Button() { iconImage = iconImageRound, tooltip = RoundTooltip });
            m_BackgroundRepeatYField.Add(new Button() { iconImage = iconImageRepeat, tooltip = RepeatTooltip });

            value = new BackgroundRepeat();
        }

        public override void SetValueWithoutNotify(BackgroundRepeat newValue)
        {
            base.SetValueWithoutNotify(newValue);
            RefreshSubFields();
        }

        void RefreshSubFields()
        {
            var toggleButtonGroupState = new ToggleButtonGroupState(1u << (int)value.x, 4);
            m_BackgroundRepeatXField.SetValueWithoutNotify(toggleButtonGroupState);

            toggleButtonGroupState = new ToggleButtonGroupState(1u << (int)value.y, 4);
            m_BackgroundRepeatYField.SetValueWithoutNotify(toggleButtonGroupState);
        }

        void UpdateBackgroundRepeatField()
        {
            var valueX = m_BackgroundRepeatXField.value;
            var selectedX = valueX.GetActiveOptions(stackalloc int[valueX.length]);

            var valueY = m_BackgroundRepeatYField.value;
            var selectedY = valueY.GetActiveOptions(stackalloc int[valueY.length]);

            int intX = selectedX[0];
            int intY = selectedY[0];

            value = new BackgroundRepeat((Repeat)k_ButtonRepeatMapping[intX], (Repeat)k_ButtonRepeatMapping[intY]);
        }
    }
}
