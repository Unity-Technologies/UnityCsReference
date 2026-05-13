// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    [UxmlElement]
    partial class ButtonStripField : BaseField<int>
    {
        public const string className = "unity-button-strip-field";
        internal static readonly UniqueStyleString classNameUnique = new(className);

        const string buttonClass = className + "__button";
        static readonly UniqueStyleString k_ButtonClass = new(buttonClass);
        static readonly UniqueStyleString k_IconClass = new(buttonClass + "-icon");
        static readonly UniqueStyleString k_ButtonLeftClass = new(buttonClass + "--left");
        static readonly UniqueStyleString k_ButtonMiddleClass = new(buttonClass + "--middle");
        static readonly UniqueStyleString k_ButtonRightClass = new(buttonClass + "--right");
        static readonly UniqueStyleString k_ButtonAloneClass = new(buttonClass + "--alone");

        readonly List<Button> m_Buttons = new List<Button>();

        public void AddButton(string text, string name = "")
        {
            var button = CreateButton(name);
            button.text = text;
            Add(button);
        }

        /// <summary>
        /// Add a button to the button strip.
        /// </summary>
        /// <param name="icon">Icon used for the button</param>
        /// <param name="name"></param>
        public void AddButton(Background icon, string name = "")
        {
            var button = CreateButton(name);
            var iconElement = new VisualElement();
            iconElement.AddToClassList(k_IconClass);
            iconElement.style.backgroundImage = icon;
            button.Add(iconElement);
            Add(button);
        }

        Button CreateButton(string name)
        {
            var button = new Button { name = name, };

            button.AddToClassList(k_ButtonClass);
            button.RegisterCallback<DetachFromPanelEvent>(OnButtonDetachFromPanel);
            button.clicked += () => { value = m_Buttons.IndexOf(button); };

            m_Buttons.Add(button);
            Add(button);

            RefreshButtonsStyling();
            return button;
        }

        void OnButtonDetachFromPanel(DetachFromPanelEvent evt)
        {
            if (evt.currentTarget is VisualElement element
                && element.parent is ButtonStripField buttonStrip)
            {
                buttonStrip.RefreshButtonsStyling();
                buttonStrip.EnsureValueIsValid();
            }
        }

        void RefreshButtonsStyling()
        {
            for (var i = 0; i < m_Buttons.Count; ++i)
            {
                var button = m_Buttons[i];
                bool alone = m_Buttons.Count == 1;
                bool left = i == 0;
                bool right = i == m_Buttons.Count - 1;

                button.EnableInClassList(k_ButtonAloneClass, alone);
                button.EnableInClassList(k_ButtonLeftClass, !alone && left);
                button.EnableInClassList(k_ButtonRightClass, !alone && right);
                button.EnableInClassList(k_ButtonMiddleClass, !alone && !left && !right);
            }
        }

        /// <summary>
        /// Creates a <see cref="ButtonStripField"/> with all default properties. The <see cref="itemsSource"/>,
        /// </summary>
        public ButtonStripField() : base(null)
        {
        }

        /// <summary>
        /// Constructs a <see cref="ButtonStripField"/>, with all required properties provided.
        /// </summary>
        /// <param name="label">The list of items to use as a data source.</param>
        public ButtonStripField(string label) : base(label)
        {
            AddToClassList(classNameUnique);
        }

        public override void SetValueWithoutNotify(int newValue)
        {
            newValue = Mathf.Clamp(newValue, 0, m_Buttons.Count - 1);
            base.SetValueWithoutNotify(newValue);
            RefreshButtonsState();
        }

        void EnsureValueIsValid()
        {
            SetValueWithoutNotify(Mathf.Clamp(value, 0, m_Buttons.Count - 1));
        }

        void RefreshButtonsState()
        {
            for (int i = 0; i < m_Buttons.Count; ++i)
            {
                m_Buttons[i].SetCheckedPseudoState(i == value);
            }
        }
    }
}
