// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    public class EditorToolbarSlider : Slider
    {
        public new const string ussClassName = "unity-editor-toolbar-slider";
        internal const string contentClassName = ussClassName + "__content";
        internal const string valueIndicatorClassName = ussClassName + "__value-indicator";
        internal const string editModeClassName = ussClassName + "--edit-mode";
        static readonly string k_MenuCopy = L10n.Tr("Copy");
        static readonly string k_MenuPaste = L10n.Tr("Paste");
        static readonly string k_MenuEdit = L10n.Tr("Edit");
        float m_PreviousValue;

        EditorToolbarContent m_Content;
        Label m_ValueLabel;
        bool m_InEditMode;

        internal EditorToolbarContent content => m_Content;

        public string text
        {
            get => m_Content.text;
            set => m_Content.text = value;
        }

        public Texture2D icon
        {
            get => m_Content.icon.textureIcon;
            set => m_Content.icon = new EditorToolbarIcon(value);
        }

        bool m_Rounded;
        internal bool rounded
        {
            get => m_Rounded;
            set
            {
                m_Rounded = value;
                this.value = this.value; // Ensure the value is rounded
            }
        }

        public EditorToolbarSlider() : this("", 0, 1) {}
        public EditorToolbarSlider(string text, float start, float end) : this(text, null, start, end) {}
        public EditorToolbarSlider(Texture2D icon, float start, float end) : this("", icon, start, end) { }
        public EditorToolbarSlider(string text, Texture2D icon, float start, float end) : this(text, new EditorToolbarIcon(icon), start, end) { }

        internal EditorToolbarSlider(string text, EditorToolbarIcon icon, float start, float end) : base(start, end)
        {
            AddToClassList(ussClassName);
            AddToClassList(EditorToolbar.elementClassName);

            showInputField = false;
            fill = true;

            var contentRoot = new VisualElement();
            contentRoot.pickingMode = PickingMode.Ignore;
            contentRoot.AddToClassList(contentClassName);
            m_Content = new EditorToolbarContent(contentRoot, text, icon);

            m_ValueLabel = new Label(start.ToString());
            m_ValueLabel.pickingMode = PickingMode.Ignore;
            m_ValueLabel.AddToClassList(valueIndicatorClassName);

            contentRoot.Add(m_ValueLabel);
            visualInput.Add(contentRoot);

            RegisterCallback<PropertyChangedEvent>(OnPropertyChanged);
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            this.AddManipulator(new ContextualMenuManipulator((evt) => PopulateContextMenu(evt.menu)));
            dragContainer.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button != 0)
                return;

            if (evt.clickCount == 1)
            {
                m_PreviousValue = value;
            }

            // Enter edit mode on double click
            else if (evt.clickCount == 2)
            {
                // The first click chances the value, we make it keep to the previous one for consistency
                EnterEditMode();
                value = m_PreviousValue;
                evt.StopPropagation();
            }
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateValueLabelWidth();
        }

        void OnPropertyChanged(PropertyChangedEvent evt)
        {
            if (evt.property == highValueProperty || evt.property == lowValueProperty)
                UpdateValueLabelWidth();
        }

        internal void PopulateContextMenu(DropdownMenu menu)
        {
            menu.AppendAction(k_MenuEdit, (action) => { EnterEditMode(); });
            menu.AppendAction(k_MenuCopy, (action) => { Copy(); });
            menu.AppendAction(k_MenuPaste, (action) => { Paste(); }, CanPaste() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        void UpdateValueLabelWidth()
        {
            // Ensure that the value indicator's width remains static
            // We check both low and high value to take into account negative numbers
            var lowValueWidth = m_ValueLabel.MeasureTextSize(GetValueAsText(lowValue), 200, MeasureMode.AtMost, rect.height, MeasureMode.AtMost).x;
            var highValueWidth = m_ValueLabel.MeasureTextSize(GetValueAsText(highValue), 200, MeasureMode.AtMost, rect.height, MeasureMode.AtMost).x;
            m_ValueLabel.style.width = Mathf.Max(lowValueWidth, highValueWidth);
        }

        internal void Copy()
        {
            Clipboard.floatValue = value;
        }

        bool CanPaste()
        {
            return Clipboard.hasFloat;
        }

        internal void Paste()
        {
            value = Clipboard.floatValue;
        }

        internal void EnterEditMode()
        {
            if (m_InEditMode)
                return;

            m_InEditMode = true;
            AddToClassList(editModeClassName);
            showInputField = true;
            inputTextField.isDelayed = true;
            inputTextField.RegisterCallback<FocusOutEvent>((evt) =>
            {
                if (inputTextField.hasFocus)
                    ExitEditMode();
            });
            inputTextField.RegisterCallback<KeyDownEvent>((evt) =>
            {
                if (!inputTextField.hasFocus)
                    return;

                if (evt.keyCode == KeyCode.Escape)
                    ExitEditMode();
            });

            inputTextField.RegisterValueChangedCallback((evt) => ExitEditMode());
            inputTextField.RegisterCallback<GeometryChangedEvent>(FocusOnLayoutDone);
        }

        void FocusOnLayoutDone(GeometryChangedEvent evt)
        {
            UnregisterCallback<GeometryChangedEvent>(FocusOnLayoutDone);

            inputTextField.Focus();
        }

        internal void ExitEditMode()
        {
            if (!m_InEditMode)
                return;

            m_InEditMode = false;
            RemoveFromClassList(editModeClassName);
            showInputField = false;
        }

        string GetValueAsText(float value)
        {
            if (rounded)
                return value.ToString("0");

            return value.ToString("0.00");
        }

        public override void SetValueWithoutNotify(float newValue)
        {
            if (rounded)
                newValue = Mathf.Round(newValue);

            base.SetValueWithoutNotify(newValue);

            if (m_ValueLabel != null)
                m_ValueLabel.text = GetValueAsText(value);
        }
    }
}
