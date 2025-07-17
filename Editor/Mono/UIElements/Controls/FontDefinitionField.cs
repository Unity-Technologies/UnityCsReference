// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using Cursor = UnityEngine.WSA.Cursor;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Makes a field for entering FontDefinition.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class FontDefinitionField : BaseField<FontDefinition>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<FontDefinition>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<FontDefinition>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), Array.Empty<UxmlAttributeNames>(), true);
            }

            public override object CreateInstance() => new FontDefinitionField();
        }

        /// <summary>
        /// USS class name of the object field in elements of this type.
        /// </summary>
        public static readonly string objectFieldUssClassName = "unity-multi-type-field__object-field";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = "unity-multi-type-field__visual-input";

        static readonly string k_OptionsPopupContainerName = "unity-multi-type-field__options-popup-container";

        readonly ObjectField m_ObjectField;
        readonly Dictionary<string, Type> m_TypeOptions;
        readonly PopupField<string> m_TypePopup;

        public ObjectField objectField => m_ObjectField;
        public PopupField<string> typePopup => m_TypePopup;

        /// <summary>
        /// Constructor.
        /// </summary>
        public FontDefinitionField() : this(null) {}

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        public FontDefinitionField(string label) : base(label, null)
        {
            m_TypeOptions = new Dictionary<string, Type>();
            m_ObjectField = new ObjectField() { classList = { objectFieldUssClassName }};
            m_ObjectField.RegisterValueChangedCallback(OnObjectValueChange);

            m_ObjectField.objectFieldDisplay.RegisterDefaultDragAndDrop(new List<Type>() { typeof(Font), typeof(FontAsset) });

            var popupContainer = new VisualElement() {name = k_OptionsPopupContainerName, classList = { k_OptionsPopupContainerName }};
            m_TypePopup = new PopupField<string> { formatSelectedValueCallback = OnFormatSelectedValue };
            popupContainer.Add(m_TypePopup);

            visualInput.AddToClassList(inputUssClassName);
            visualInput.Add(m_ObjectField);
            visualInput.Add(popupContainer);

            AddType(typeof(FontAsset), "Font Asset");
            AddType(typeof(Font), "Font");
        }

        void OnObjectValueChange(ChangeEvent<Object> evt)
        {
            if (evt.newValue is Font font)
                value = FontDefinition.FromFont(font);
            else if (evt.newValue is FontAsset fontAsset)
                value = FontDefinition.FromSDFFont(fontAsset);

            evt.StopImmediatePropagation();
        }

        protected void AddType(Type type)
        {
            AddType(type, type.Name);
        }

        protected void AddType(Type type, string displayName)
        {
            if (m_TypeOptions.ContainsKey(displayName))
                throw new ArgumentException($"Item with the name: {displayName} already exists.", nameof(displayName));

            m_TypeOptions.Add(displayName, type);
            m_TypePopup.choices.Add(displayName);

            m_TypePopup.style.display = m_TypeOptions.Count <= 1
                ? DisplayStyle.None
                : DisplayStyle.Flex;

            if (string.IsNullOrEmpty(m_TypePopup.value))
                m_TypePopup.value = displayName;
        }

        string OnFormatSelectedValue(string formatValue)
        {
            if (m_TypeOptions.Count > 0)
            {
                m_ObjectField.objectType = m_TypeOptions[formatValue];
                if (!m_ObjectField.value) return formatValue;
                if (!m_ObjectField.objectType.IsInstanceOfType(m_ObjectField.value))
                    m_ObjectField.value = null;
            }

            return formatValue;
        }

        public override void SetValueWithoutNotify(FontDefinition newValue)
        {
            m_ObjectField.SetValueWithoutNotify(newValue.font ? newValue.font : newValue.fontAsset);
            if (newValue.font)
            {
                m_TypePopup.SetValueWithoutNotify("Font");
            }
            else if (newValue.fontAsset)
            {
                m_TypePopup.SetValueWithoutNotify("Font Asset");
            }

            base.SetValueWithoutNotify(newValue);
        }
    }
}
