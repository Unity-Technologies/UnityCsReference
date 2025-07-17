// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class FontStyleToggleField : BaseField<FontStyle>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<FontStyle>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<FontStyle>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new FontStyleToggleField();
        }

        static readonly string k_UssPathNoExt = BuilderConstants.UtilitiesPath + "/StyleField/EnumToggleField";
        static readonly string k_StyleLight = k_UssPathNoExt + "Light.uss";
        static readonly string k_StyleDark = k_UssPathNoExt + "Dark.uss";

        static readonly string s_UssClassName = "unity-toggle-button-group_font-style-field";

        [Flags]
        enum FontStyleFlag
        {
            Bold = 1 << 0,
            Italic = 1 << 1,
            Both = Bold | Italic,
            None = 0
        }

        ToggleButtonGroup m_ToggleButtonGroup;

        public ToggleButtonGroup toggleButtonGroup => m_ToggleButtonGroup;

        public FontStyleToggleField() : this(null) {}

        public FontStyleToggleField(string label) : base(label, new ToggleButtonGroup())
        {
            styleSheets.Add(EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? k_StyleDark : k_StyleLight) as StyleSheet);

            AddToClassList(s_UssClassName);

            m_ToggleButtonGroup = visualInput as ToggleButtonGroup;

            if (m_ToggleButtonGroup == null)
            {
                m_ToggleButtonGroup = new ToggleButtonGroup();
                visualInput = m_ToggleButtonGroup;
            }

            m_ToggleButtonGroup.isMultipleSelection = true;
            m_ToggleButtonGroup.allowEmptySelection = true;
            m_ToggleButtonGroup.Add(new Button() { name = "bold", iconImage = Background.FromTexture2D(new Texture2D(0, 0)), tooltip = "bold" });
            m_ToggleButtonGroup.Add(new Button() { name = "italic", iconImage = Background.FromTexture2D(new Texture2D(0, 0)), tooltip = "italic" });
            m_ToggleButtonGroup.RegisterValueChangedCallback(OnFontStyleChange);
            m_ToggleButtonGroup.value = ToggleButtonGroupState.FromEnumFlags(FontStyleFlag.None);
        }

        public override void SetValueWithoutNotify(FontStyle newValue)
        {
            base.SetValueWithoutNotify(newValue);

            var fontStyleByString = newValue switch
            {
                FontStyle.Normal => FontStyleFlag.None,
                FontStyle.Bold => FontStyleFlag.Bold,
                FontStyle.Italic => FontStyleFlag.Italic,
                FontStyle.BoldAndItalic => FontStyleFlag.Both,
                _ => FontStyleFlag.None
            };

            m_ToggleButtonGroup.SetValueWithoutNotify(ToggleButtonGroupState.FromEnumFlags(fontStyleByString));
        }

        void OnFontStyleChange(ChangeEvent<ToggleButtonGroupState> evt)
        {
            var state = ToggleButtonGroupState.ToEnumFlags<FontStyleFlag>(evt.newValue);

            var fontStyle = state switch
            {
                FontStyleFlag.None => FontStyle.Normal,
                FontStyleFlag.Bold => FontStyle.Bold,
                FontStyleFlag.Italic => FontStyle.Italic,
                _ => FontStyle.BoldAndItalic
            };

            base.value = fontStyle;
        }
    }
}
