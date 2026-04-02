// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlayMode.Editor
{
    /// <summary>
    /// Used in the PlayModeListView as ListItem
    /// </summary>
    class LabelWithIcon : VisualElement
    {
        static readonly string k_BaseClassName = "unity-scenarios-label-with-icon__base--scenario-window";

        static string s_VisualTreePath = "PlayMode/UI/LabelWithIcon.uxml";
        static string s_StylePath = "PlayMode/UI/Framework.uss";

        /// <summary>
        /// Invoked if the edit was successful and no InputWarning was present.
        /// </summary>
        public event Action<string> OnFinishEdit;

        /// <summary>
        /// Invoked each time the TextField value changes while editing.
        /// </summary>
        public event Action<string> OnEdit;

        /// <summary>
        /// Invoked when editing was canceled (ESC) or an InputWarning was present.
        /// </summary>
        public event Action OnCancel;

        public string Text { get => Label.text; set => Label.text = value; }

        Label Label => this.Q<Label>();
        TextField TextField => this.Q<TextField>();
        VisualElement Icon => this.Q<VisualElement>("icon");

        bool m_InputIsValid;
        readonly VisualElement m_WarnIcon;

        public bool InputIsValid
        {
            get => m_InputIsValid;
            set
            {
                m_InputIsValid = value;
                if (m_InputIsValid)
                    RemoveFromClassList("unity-scenarios-label-with-icon__base--input-warning");
                else
                    AddToClassList("unity-scenarios-label-with-icon__base--input-warning");
            }
        }

        internal void ShowWarningIcon(bool show, string tooltipText)
        {
            m_WarnIcon.tooltip = tooltipText;
            m_WarnIcon.style.display = show ? new StyleEnum<DisplayStyle>(DisplayStyle.Flex) : new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }

        public LabelWithIcon(string text, string styleClass = "", string iconName = "")
        {
            VisualElement item = new VisualElement();

            (EditorGUIUtility.LoadRequired(s_VisualTreePath) as VisualTreeAsset).CloneTree(item);
            styleSheets.Add(EditorGUIUtility.LoadRequired(s_StylePath) as StyleSheet);
            var element = item.Q("label-with-icon");
            name = k_BaseClassName;
            AddToClassList(k_BaseClassName);
            AddToClassList(styleClass);

            var icon = element.Q<VisualElement>("icon");
            if (string.IsNullOrEmpty(iconName))
                element.Q<VisualElement>("icon-container").style.display = DisplayStyle.None;
            else
            {
                var iconTexture = EditorGUIUtility.IconContent(iconName).image as Texture2D;
                icon.style.backgroundImage = iconTexture;
            }

            m_WarnIcon = element.Q<VisualElement>("warn-icon");
            m_WarnIcon.AddToClassList("unity-scenarios-label-with-icon__warn-icon");
            m_WarnIcon.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            m_WarnIcon.style.backgroundImage = EditorGUIUtility.FindTexture("console.warnicon"); ;

            var label = element.Q<Label>("label");
            label.text = text;

            // copy just the content of the uxml and not the template container etc. to prevent extra nesting
            while (element.childCount > 0)
            {
                Add(element.ElementAt(0));
            }
        }

        public void EnableEditMode()
        {
            TextField.SetValueWithoutNotify(Label.text);
            TextField.maxLength = 100;
            TextField.UnregisterCallback<ChangeEvent<string>>(OnTextFieldChange);
            TextField.RegisterCallback<ChangeEvent<string>>(OnTextFieldChange);

            TextField.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

            TextField.UnregisterCallback<BlurEvent>(OnTextFieldFocusOut);
            TextField.RegisterCallback<BlurEvent>(OnTextFieldFocusOut);

            AddToClassList("unity-scenarios-label-with-icon__base--editable");

            // use scheduler again, because we have to wait until the textfield has display=flex
            // before we can focus it.
            schedule.Execute(() =>
            {
                TextField.Focus();
                OnEdit?.Invoke(TextField.text);
            }).ExecuteLater(50);
        }

        void DisableEditMode(bool cancel = false)
        {
            RemoveFromClassList("unity-scenarios-label-with-icon__base--input-warning");
            RemoveFromClassList("unity-scenarios-label-with-icon__base--editable");

            TextField.UnregisterCallback<ChangeEvent<string>>(OnTextFieldChange);
            TextField.UnregisterCallback<BlurEvent>(OnTextFieldFocusOut);

            if (cancel || !InputIsValid)
            {
                OnCancel?.Invoke();
                return;
            }

            OnFinishEdit?.Invoke(TextField.text);
        }

        void OnTextFieldFocusOut(BlurEvent evt)
        {
            DisableEditMode();
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                DisableEditMode(true);
                evt.StopPropagation();
            }
        }

        void OnTextFieldChange(ChangeEvent<string> evt)
        {
            OnEdit?.Invoke(evt.newValue);
        }

        public static LabelWithIcon Create(string text, string styleClass = "", string iconClass = "")
        {
            return new LabelWithIcon(text, styleClass, iconClass);
        }

        public void SetIcon(Texture2D iconTexture)
        {
            Icon.style.backgroundImage = iconTexture;
        }
    }
}
