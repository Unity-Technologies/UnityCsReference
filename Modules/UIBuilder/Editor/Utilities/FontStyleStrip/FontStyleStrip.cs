// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    [UxmlElement]
    internal partial class FontStyleStrip : BaseField<string>
    {
        static readonly string s_UssPath = BuilderConstants.UtilitiesPath + "/FontStyleStrip/FontStyleStrip.uss";
        static readonly string s_UssClassName = "unity-font-style-strip";

        [Flags]
        enum FontStyleFlag
        {
            Bold = 1 << 0,
            Italic = 1 << 1,
            Both = Bold | Italic,
            None = 0
        }

        ToggleButtonGroup m_ToggleButtonGroup;

        public FontStyleStrip() : this(null) {}

        public FontStyleStrip(string label) : base(label)
        {
            AddToClassList(s_UssClassName);

            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(s_UssPath));

            m_ToggleButtonGroup = new ToggleButtonGroup { isMultipleSelection = true, allowEmptySelection = true };
            m_ToggleButtonGroup.Add(new Button() { name = "bold", tooltip = "Bold" });
            m_ToggleButtonGroup.Add(new Button() { name = "italic", tooltip = "Italic" });
            m_ToggleButtonGroup.RegisterValueChangedCallback(OnFontStyleChange);
            m_ToggleButtonGroup.value = ToggleButtonGroupState.FromEnumFlags(FontStyleFlag.None);

            visualInput = m_ToggleButtonGroup;
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            base.SetValueWithoutNotify(newValue);

            var fontStyleByString = newValue switch
            {
                "normal" => FontStyleFlag.None,
                "bold" => FontStyleFlag.Bold,
                "italic" => FontStyleFlag.Italic,
                "bold-and-italic" => FontStyleFlag.Both,
                _ => FontStyleFlag.None
            };

            m_ToggleButtonGroup.SetValueWithoutNotify(ToggleButtonGroupState.FromEnumFlags(fontStyleByString));
        }

        void OnFontStyleChange(ChangeEvent<ToggleButtonGroupState> evt)
        {
            var state = ToggleButtonGroupState.ToEnumFlags<FontStyleFlag>(evt.newValue);

            var fontStyle = state switch
            {
                FontStyleFlag.None => "normal",
                FontStyleFlag.Bold => "bold",
                FontStyleFlag.Italic => "italic",
                _ => "bold-and-italic"
            };

            base.value = fontStyle;
        }
    }
}
