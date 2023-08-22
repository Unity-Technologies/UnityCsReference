// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    abstract class MultiTypeField : BaseField<Object>
    {
        static readonly string k_UssPath = BuilderConstants.UtilitiesPath + "/MultiTypeField/MultiTypeField.uss";
        static readonly string k_UxmlPath = BuilderConstants.UtilitiesPath + "/MultiTypeField/MultiTypeField.uxml";
        static readonly string acceptDropVariantUssClassName = "unity-object-field-display--accept-drop";
        static readonly PropertyName serializedPropertyKey = new PropertyName("--unity-object-field-serialized-property");

        const string k_OptionsPopupContainerName = "unity-multi-type-options-popup-container";

        readonly ObjectField m_ObjectField;
        readonly VisualElement m_ObjectFieldInput;
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

            // To be able to support drag and dropping assets of all the supported types, we need to override the default behaviour of the ObjectField.
            // to do this, we register a trickle down callback on the (private) element that handles that on the `ObjectField`.
            m_ObjectFieldInput = m_ObjectField.Q(className:"unity-object-field-display");
            m_ObjectFieldInput.RegisterCallback<DragPerformEvent>(OnDragPerformed, TrickleDown.TrickleDown);
            m_ObjectFieldInput.RegisterCallback<DragUpdatedEvent>(OnDragUpdated, TrickleDown.TrickleDown);

            var popupContainer = this.Q(k_OptionsPopupContainerName);
            m_TypePopup = new PopupField<string> { formatSelectedValueCallback = OnFormatSelectedValue };
            popupContainer.Add(m_TypePopup);
        }

        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            Object validatedObject = DNDValidateObject();
            if (validatedObject != null)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                m_ObjectFieldInput.EnableInClassList(acceptDropVariantUssClassName, true);

                evt.StopImmediatePropagation();
            }
        }

        private void OnDragPerformed(DragPerformEvent evt)
        {
            Object validatedObject = DNDValidateObject();
            if (validatedObject != null)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                m_ObjectField.value = validatedObject;

                DragAndDrop.AcceptDrag();
                m_ObjectFieldInput.RemoveFromClassList(acceptDropVariantUssClassName);

                evt.StopImmediatePropagation();
            }
        }

        private Object DNDValidateObject()
        {
            var references = DragAndDrop.objectReferences;
            var property = m_ObjectField.GetProperty(serializedPropertyKey) as SerializedProperty;

            foreach (var type in m_TypeOptions.Values)
            {
                var validatedObject = EditorGUI.ValidateObjectFieldAssignment(references, type, property, EditorGUI.ObjectFieldValidatorOptions.None);

                if (validatedObject != null)
                {
                    // If scene objects are not allowed and object is a scene object then clear
                    if (!m_ObjectField.allowSceneObjects && !EditorUtility.IsPersistent(validatedObject))
                        validatedObject = null;
                }

                if (validatedObject)
                    return validatedObject;
            }

            return null;
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
                if (!m_ObjectField.value) return formatValue;
                if (!m_ObjectField.objectType.IsInstanceOfType(m_ObjectField.value))
                    m_ObjectField.value = null;
            }

            return formatValue;
        }

        public void SetTypePopupValueWithoutNotify(Type type)
        {
            var typeDisplayName = m_TypeOptions.FirstOrDefault(pair => pair.Value.IsAssignableFrom(type)).Key;
            m_TypePopup.SetValueWithoutNotify(typeDisplayName);
        }

        public override void SetValueWithoutNotify(Object newValue)
        {
            m_ObjectField.SetValueWithoutNotify(newValue);
            if (newValue)
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
