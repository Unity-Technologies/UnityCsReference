using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class FontStyleStrip : BaseField<string>, IToggleButtonStrip
    {
        static readonly string s_UssPath = BuilderConstants.UtilitiesPath + "/FontStyleStrip/FontStyleStrip.uss";

        static readonly string s_UssClassName = "unity-font-style-strip";

        static readonly string s_BoldChoice = "bold";
        static readonly string s_ItalicChocie = "italic";
        static readonly List<string> s_VisibleChoices = new List<string>() { s_BoldChoice, s_ItalicChocie };

        public new class UxmlFactory : UxmlFactory<FontStyleStrip, UxmlTraits> {}

        [Flags]
        enum FontStyleFlag
        {
            Bold = 1 << 0,
            Italic = 1 << 1,
            Both = Bold | Italic,
            None = 0
        }

        FontStyleFlag m_InternalState;

        ButtonStrip m_ButtonStrip;

        List<string> m_Choices = new List<string>();
        List<string> m_Labels = new List<string>();

        public IEnumerable<string> choices
        {
            get { return m_Choices; }
            set
            {
                m_Choices.Clear();

                if (value == null)
                    return;

                m_Choices.AddRange(value);
            }
        }

        public IEnumerable<string> labels
        {
            get { return m_Labels; }
            set
            {
                m_Labels.Clear();

                if (value == null)
                    return;

                m_Labels.AddRange(value);
            }
        }

        public Type enumType { get; set; }

        public FontStyleStrip() : this(null) {}

        public FontStyleStrip(string label) : base(label)
        {
            AddToClassList(s_UssClassName);

            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(s_UssPath));

            m_ButtonStrip = new ButtonStrip();
            m_ButtonStrip.onButtonClick = OnOptionChange;
            visualInput = m_ButtonStrip;

            m_ButtonStrip.choices = s_VisibleChoices;
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            base.SetValueWithoutNotify(newValue);
            SetInternalStatesFromExternalChoice(newValue);
        }

        void SetInternalStatesFromExternalChoice(string newValue)
        {
            m_InternalState = FontStyleFlag.None;
            if (newValue == "normal")
                m_InternalState = FontStyleFlag.None;
            else if (newValue == "bold")
                m_InternalState = FontStyleFlag.Bold;
            else if (newValue == "italic")
                m_InternalState = FontStyleFlag.Italic;
            else if (newValue == "bold-and-italic")
                m_InternalState = FontStyleFlag.Both;

            ToggleButtonStates();
        }

        void OnOptionChange(EventBase evt)
        {
            var button = evt.target as Button;
            var choiceName = button.name;

            if (choiceName == s_BoldChoice)
                m_InternalState ^= FontStyleFlag.Bold;
            else if (choiceName == s_ItalicChocie)
                m_InternalState ^= FontStyleFlag.Italic;

            if (m_InternalState == FontStyleFlag.None)
                value = "normal";
            else if (m_InternalState == FontStyleFlag.Bold)
                value = "bold";
            else if (m_InternalState == FontStyleFlag.Italic)
                value = "italic";
            else
                value = "bold-and-italic";
        }

        void ToggleButtonStates()
        {
            m_ButtonStrip.Query<Button>().ForEach((b) =>
            {
                b.pseudoStates &= ~(PseudoStates.Checked);

                if ((b.name == s_BoldChoice && m_InternalState.HasFlag(FontStyleFlag.Bold))
                    || (b.name == s_ItalicChocie && m_InternalState.HasFlag(FontStyleFlag.Italic)))
                    b.pseudoStates |= PseudoStates.Checked;
            });
        }
    }
}
