// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    //Copied from UIBuilder
    interface IToggleButtonStrip : INotifyValueChanged<string>
    {
        IEnumerable<string> choices { get; set; }

        IEnumerable<string> labels { get; set; }

        Type enumType { get; set; }
    }

    class ToggleButtonStrip : BaseField<string>, IToggleButtonStrip
    {
        static readonly string s_UssPath = "ToggleButtonStrip/ToggleButtonStrip.uss";

        static readonly string s_UssClassName = "unity-toggle-button-strip";

        ButtonStrip m_ButtonStrip;

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

            this.AddStylesheet_Internal(s_UssPath);

            m_ButtonStrip = new ButtonStrip();
            m_ButtonStrip.onButtonClick = OnOptionChange;
            visualInput = m_ButtonStrip;

            this.choices = choices;
        }

        protected override void UpdateMixedValueContent()
        {
            if (showMixedValue)
            {
                m_ButtonStrip.Query<Button>().ForEach((b) =>
                {
                    b.pseudoStates &= ~(PseudoStates.Checked);
                });
            }
            else
            {
                SetValueWithoutNotify(value);
            }
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
