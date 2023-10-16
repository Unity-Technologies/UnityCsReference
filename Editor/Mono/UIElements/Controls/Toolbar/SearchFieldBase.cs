// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// The base class for a search field.
    /// </summary>
    public abstract class SearchFieldBase<TextInputType, T> : VisualElement, INotifyValueChanged<T>
        where TextInputType : TextInputBaseField<T>, new()
    {
        internal static readonly BindingId valueProperty = nameof(value);
        internal static readonly BindingId placeholderTextProperty = nameof(placeholderText);

        private readonly Button m_SearchButton;
        private readonly Button m_CancelButton;
        private readonly TextInputType m_TextField;
        /// <summary>
        /// The text field used by the search field to draw and modify the search string.
        /// </summary>
        internal protected TextInputType textInputField { get { return m_TextField; } }

        /// <summary>
        /// The search button.
        /// </summary>
        protected Button searchButton
        {
            get { return m_SearchButton; }
        }

        /// <summary>
        /// The object currently being exposed by the field.
        /// </summary>
        /// <remarks>
        /// If the new value is different from the current value, this method notifies registered callbacks with a <see cref="ChangeEvent{T}"/>.
        /// </remarks>
        [CreateProperty]
        public T value
        {
            get { return m_TextField.value; }
            set
            {
                var previous = m_TextField.value;
                m_TextField.value = value;
                if (!EqualityComparer<T>.Default.Equals(m_TextField.value, previous))
                    NotifyPropertyChanged(valueProperty);
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "unity-search-field-base";
        /// <summary>
        /// USS class name of text elements in elements of this type.
        /// </summary>
        public static readonly string textUssClassName = ussClassName + "__text-field";
        /// <summary>
        /// USS class name of text input elements in elements of this type.
        /// </summary>
        public static readonly string textInputUssClassName = textUssClassName + "__input";
        /// <summary>
        /// USS class name of search buttons in elements of this type.
        /// </summary>
        public static readonly string searchButtonUssClassName = ussClassName + "__search-button";
        /// <summary>
        /// USS class name of cancel buttons in elements of this type.
        /// </summary>
        public static readonly string cancelButtonUssClassName = ussClassName + "__cancel-button";
        /// <summary>
        /// USS class name of cancel buttons in elements of this type, when they are off.
        /// </summary>
        public static readonly string cancelButtonOffVariantUssClassName = cancelButtonUssClassName + "--off";
        /// <summary>
        /// USS class name of elements of this type, when they are using a popup menu.
        /// </summary>
        public static readonly string popupVariantUssClassName = ussClassName + "--popup";

        [Serializable, ExcludeFromDocs]
        public abstract new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField] private string placeholderText;
            #pragma warning restore 649

            [ExcludeFromDocs]
            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (SearchFieldBase<TextInputType, T>)obj;
                e.placeholderText = placeholderText;
            }
        }

        /// <summary>
        /// Defines <see cref="SearchFieldBase.UxmlTraits"/> for the <see cref="SearchFieldBase"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the properties of a SearchFieldBase element that you can
        /// use in a UXML asset.
        /// </remarks>
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            /// <summary>
            /// Constructor.
            /// </summary>
            public UxmlTraits()
            {
                focusable.defaultValue = true;
            }
        }

        /// <summary>
        /// The placeholder property represents a short hint intended to aid the users with data entry when the control has no value.
        /// </summary>
        [CreateProperty]
        public string placeholderText
        {
            get => m_TextField.textEdition.placeholder;
            set
            {
                if (value == m_TextField.textEdition.placeholder)
                    return;

                m_TextField.textEdition.placeholder = value;
                NotifyPropertyChanged(placeholderTextProperty);
            }
        }

        protected SearchFieldBase()
        {
            isCompositeRoot = true;
            focusable = true;
            tabIndex = 0;
            excludeFromFocusRing = true;
            delegatesFocus = true;

            AddToClassList(ussClassName);

            m_SearchButton = new Button(() => {}) { name = "unity-search" };
            m_SearchButton.AddToClassList(searchButtonUssClassName);
            m_SearchButton.focusable = false;
            hierarchy.Add(m_SearchButton);

            m_TextField = new TextInputType();
            m_TextField.AddToClassList(textUssClassName);
            hierarchy.Add(m_TextField);
            m_TextField.RegisterValueChangedCallback(OnValueChanged);

            var textInput = m_TextField.Q(TextField.textInputUssName);
            textInput.RegisterCallback<KeyDownEvent>(OnTextFieldKeyDown, TrickleDown.TrickleDown);
            textInput.AddToClassList(textInputUssClassName);

            m_CancelButton = new Button(() => {}) { name = "unity-cancel" };
            m_CancelButton.AddToClassList(cancelButtonUssClassName);
            m_CancelButton.AddToClassList(cancelButtonOffVariantUssClassName);
            hierarchy.Add(m_CancelButton);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanelEvent);
            m_CancelButton.clickable.clicked += OnCancelButtonClick;
        }

        private void OnAttachToPanelEvent(AttachToPanelEvent evt)
        {
            UpdateCancelButton();
        }

        private void OnValueChanged(ChangeEvent<T> change)
        {
            UpdateCancelButton(); // We need to update it on value changed because in most cases the TextField is modified directly
        }

        /// <summary>
        /// Method used when clearing the text field. You should usually clear the value when overriding the method.
        /// </summary>
        protected abstract void ClearTextField();

        private void OnTextFieldKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
                ClearTextField();
        }

        private void OnCancelButtonClick()
        {
            ClearTextField();
        }

        /// <summary>
        /// Sets the value for the toolbar search field without sending a change event.
        /// </summary>
        public virtual void SetValueWithoutNotify(T newValue)
        {
            m_TextField.SetValueWithoutNotify(newValue);
            UpdateCancelButton(); // We need to update it in that case because OnValueChanged will not be called
        }

        /// <summary>
        /// Tells if the field is empty. That meaning depends on the type of T.
        /// </summary>
        /// <param name="fieldValue">The value to check.</param>
        /// <returns>True if the parameter is empty. That meaning depends on the type of T.</returns>
        protected abstract bool FieldIsEmpty(T fieldValue);

        private void UpdateCancelButton()
        {
            m_CancelButton.EnableInClassList(cancelButtonOffVariantUssClassName, FieldIsEmpty(m_TextField.value));
        }
    }
}
