// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal class BackgroundField : BaseField<Background>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<Background>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<Background>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), Array.Empty<UxmlAttributeNames>(), true);
            }

            public override object CreateInstance() => new BackgroundField();
        }

        /// <summary>
        /// USS class name of the object field in elements of this type.
        /// </summary>
        public static readonly string objectFieldUssClassName = "unity-multi-type-field__object-field";
        /// <summary>
        /// USS class name of the options popup in elements of this type.
        /// </summary>
        public static readonly string optionsPopupContainerName = "unity-multi-type-field__options-popup-container";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = "unity-multi-type-field__visual-input";

        readonly ObjectField m_ObjectField;
        readonly Dictionary<string, Type> m_TypeOptions;
        readonly PopupField<string> m_TypePopup;

        public ObjectField objectField => m_ObjectField;
        public PopupField<string> typePopup => m_TypePopup;

        public BackgroundField() : this(null) {}

        public BackgroundField(string label) : base(label, null)
        {
            m_TypeOptions = new Dictionary<string, Type>();
            m_ObjectField = new ObjectField().WithClassList(objectFieldUssClassName);
            m_ObjectField.RegisterValueChangedCallback(OnObjectValueChange);

            m_ObjectField.objectFieldDisplay.RegisterDefaultDragAndDrop(new List<Type>() { typeof(Texture2D), typeof(RenderTexture), typeof(Sprite), typeof(VectorImage) });

            var popupContainer = new VisualElement() {name = optionsPopupContainerName }.WithClassList(optionsPopupContainerName);
            m_TypePopup = new PopupField<string> { formatSelectedValueCallback = OnFormatSelectedValue };
            popupContainer.Add(m_TypePopup);

            visualInput.AddToClassList(inputUssClassName);
            visualInput.Add(m_ObjectField);
            visualInput.Add(popupContainer);

            AddType(typeof(Texture2D), "Texture");
            AddType(typeof(RenderTexture), "Render Texture");
            AddType(typeof(Sprite), "Sprite");
            AddType(typeof(VectorImage), "Vector");
        }

        void OnObjectValueChange(ChangeEvent<Object> evt)
        {
            value = Background.FromObject(evt.newValue);
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

        public override void SetValueWithoutNotify(Background newValue)
        {
            var obj = newValue.GetSelectedImage();
            m_ObjectField.SetValueWithoutNotify(obj);
            if (obj)
            {
                foreach (var pair in m_TypeOptions)
                {
                    if (pair.Value.IsInstanceOfType(newValue))
                    {
                        m_TypePopup.SetValueWithoutNotify(pair.Key);
                        break;
                    }
                }
            }

            base.SetValueWithoutNotify(newValue);
        }
    }
}
