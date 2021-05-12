// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    class ButtonStripField : BaseField<int>
    {
        public new class UxmlFactory : UxmlFactory<ButtonStripField, UxmlTraits> {}
        public new class UxmlTraits : BaseField<int>.UxmlTraits {}

        public const string className = "unity-button-strip-field";
        const string k_ButtonClass = className + "__button";
        const string k_IconClass = className + "__button-icon";
        const string k_ButtonLeftClass = k_ButtonClass + "--left";
        const string k_ButtonMiddleClass = k_ButtonClass + "--middle";
        const string k_ButtonRightClass = k_ButtonClass + "--right";
        const string k_ButtonAloneClass = k_ButtonClass + "--alone";

        static readonly List<Button> s_ButtonQueryResults = new List<Button>();

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
            this.Query<Button>().ToList(s_ButtonQueryResults);

            for (var i = 0; i < s_ButtonQueryResults.Count; ++i)
            {
                var button = s_ButtonQueryResults[i];
                bool alone = s_ButtonQueryResults.Count == 1;
                bool left = i == 0;
                bool right = i == s_ButtonQueryResults.Count - 1;

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
            AddToClassList(className);
        }

        public override void SetValueWithoutNotify(int newValue)
        {
            newValue = Mathf.Clamp(newValue, 0, s_ButtonQueryResults.Count - 1);
            base.SetValueWithoutNotify(newValue);

            RefreshButtonsState();
        }

        void EnsureValueIsValid()
        {
            this.Query<Button>().ToList(s_ButtonQueryResults);

            if (value >= s_ButtonQueryResults.Count)
                value = 0;
        }

        void RefreshButtonsState()
        {
            this.Query<Button>().ToList(s_ButtonQueryResults);

            for (int i = 0; i < s_ButtonQueryResults.Count; ++i)
            {
                if (i == value)
                    s_ButtonQueryResults[i].pseudoStates |= PseudoStates.Checked;
                else
                    s_ButtonQueryResults[i].pseudoStates &= ~PseudoStates.Checked;
            }
        }
    }
}
