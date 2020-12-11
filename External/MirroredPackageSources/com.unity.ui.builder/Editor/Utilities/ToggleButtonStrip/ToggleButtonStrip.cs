using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal interface IToggleButtonStrip : INotifyValueChanged<string>
    {
        IEnumerable<string> choices { get; set; }

        IEnumerable<string> labels { get; set; }

        Type enumType { get; set; }
    }

    internal class ToggleButtonStrip : BaseField<string>, IToggleButtonStrip
    {
        static readonly string s_UssPath = BuilderConstants.UtilitiesPath + "/ToggleButtonStrip/ToggleButtonStrip.uss";

        static readonly string s_UssClassName = "unity-toggle-button-strip";

        ButtonStrip m_ButtonStrip;

        public new class UxmlFactory : UxmlFactory<ToggleButtonStrip, UxmlTraits> {}

        public new class UxmlTraits : BaseField<string>.UxmlTraits
        {
            public UxmlTraits()
            {
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
            }
        }

        public IEnumerable<string> choices
        {
            get { return m_ButtonStrip.choices; }
            set { m_ButtonStrip.choices = value; }
        }

        public IEnumerable<string> labels
        {
            get { return m_ButtonStrip.labels; }
            set { m_ButtonStrip.labels = value; }
        }

        public Type enumType { get; set; }

        public ToggleButtonStrip() : this(null, null) {}

        public ToggleButtonStrip(string label, IList<string> choices) : base(label)
        {
            AddToClassList(s_UssClassName);

            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(s_UssPath));

            m_ButtonStrip = new ButtonStrip();
            m_ButtonStrip.onButtonClick = OnOptionChange;
            visualInput = m_ButtonStrip;

            this.choices = choices;
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            var button = m_ButtonStrip.Q<Button>(newValue);
            if (button == null)
                return;

            base.SetValueWithoutNotify(newValue);

            ToggleButtonStates(button);
        }

        void OnOptionChange(EventBase evt)
        {
            var button = evt.target as Button;
            var newValue = button.name;
            value = newValue;

            ToggleButtonStates(button);
        }

        void ToggleButtonStates(Button button)
        {
            m_ButtonStrip.Query<Button>().ForEach((b) =>
            {
                b.pseudoStates &= ~(PseudoStates.Checked);
            });
            button.pseudoStates |= PseudoStates.Checked;
            button.IncrementVersion(VersionChangeType.Styles);
        }
    }
}
