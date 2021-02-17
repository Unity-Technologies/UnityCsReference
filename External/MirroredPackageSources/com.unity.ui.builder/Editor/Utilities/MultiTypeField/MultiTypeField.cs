using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    abstract class MultiTypeField : BaseField<Object>
    {
        static readonly string k_UssPath = BuilderConstants.UtilitiesPath + "/MultiTypeField/MultiTypeField.uss";
        static readonly string k_UxmlPath = BuilderConstants.UtilitiesPath + "/MultiTypeField/MultiTypeField.uxml";

        const string k_OptionsPopupContainerName = "unity-multi-type-options-popup-container";

        readonly ObjectField m_ObjectField;
        readonly Dictionary<string, Type> m_TypeOptions;
        readonly PopupField<string> m_TypePopup;

        protected MultiTypeField() : this(null) {}

        protected MultiTypeField(string label) : base(label)
        {
            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(k_UssPath));

            var template = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(k_UxmlPath);
            template.CloneTree(this);

            m_TypeOptions = new Dictionary<string, Type>();
            m_ObjectField = this.Q<ObjectField>();
            m_ObjectField.RegisterValueChangedCallback(OnObjectValueChange);

            var popupContainer = this.Q(k_OptionsPopupContainerName);
            m_TypePopup = new PopupField<string> { formatSelectedValueCallback = OnFormatSelectedValue };
            popupContainer.Add(m_TypePopup);
        }

        void OnObjectValueChange(ChangeEvent<Object> evt)
        {
            value = evt.newValue;
            evt.StopImmediatePropagation();
            evt.PreventDefault();
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
                if (m_ObjectField.value != null && m_ObjectField.objectType != m_ObjectField.value.GetType())
                    m_ObjectField.value = null;
            }

            return formatValue;
        }

        public void SetTypePopupValueWithoutNotify(Type type)
        {
            var typeDisplayName = m_TypeOptions.FirstOrDefault(pair => pair.Value == type).Key;
            m_TypePopup.SetValueWithoutNotify(typeDisplayName);
        }

        public override void SetValueWithoutNotify(Object newValue)
        {
            m_ObjectField.SetValueWithoutNotify(newValue);
            if (newValue != null)
            {
                foreach (var pair in m_TypeOptions)
                {
                    if (pair.Value == newValue.GetType())
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
