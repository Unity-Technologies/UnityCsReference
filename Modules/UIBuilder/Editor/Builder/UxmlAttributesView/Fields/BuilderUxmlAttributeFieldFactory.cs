// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    /// <summary>
    /// Creates a ui field based on uxml attributes.
    /// </summary>
    internal interface IBuilderUxmlAttributeFieldFactory
    {
        /// <summary>
        /// Indicates whether this factory can create a field for the specified uxml attribute.
        /// </summary>
        /// <param name="attributeOwner">An instance created from the uxml element that owns the related xml attribute.</param>
        /// <param name="attributeUxmlOwner">The uxml element that owns the uxml attribute related to evaluate.</param>
        /// <param name="attribute">The uxml attribute to evaluate.</param>
        /// <returns>Return true if the factory can create field for the specified attribute.</returns>
        bool CanCreateField(object attributeOwner, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute);

        /// <summary>
        /// Creates a ui field based on uxml attributes.
        /// </summary>
        /// <param name="attributeOwner">An instance created from the uxml element that owns the related xml attribute.</param>
        /// <param name="attributeUxmlOwner">The uxml element that owns the uxml attribute related to field to create.</param>
        /// <param name="attribute">The uxml attribute.</param>
        /// <param name="onValueChange">The callback to invoke whenever the value of the create field changes.</param>
        /// <returns>The field created for the specified uxml attribute.</returns>
        VisualElement CreateField(object attributeOwner, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute, Action<VisualElement, UxmlAttributeDescription, object, string> onValueChange);

        /// <summary>
        /// Sets the value of the specified field created from the specified uxml attribute.
        /// </summary>
        /// <param name="field">The field to update</param>
        /// <param name="attributeOwner">An instance created from the uxml element that owns the related xml attribute.</param>
        /// <param name="uxmlDocument">The uxml document that contains the uxml attribute owner.</param>
        /// <param name="attributeUxmlOwner">The uxml element that owns the uxml attribute related to field to update.</param>
        /// <param name="attribute">The uxml attribute related to the field to update.</param>
        /// <param name="value">The new value to set</param>
        void SetFieldValue(VisualElement field, object attributeOwner, VisualTreeAsset uxmlDocument, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute, object value);

        /// <summary>
        /// Resets the value of the specified field created from the specified uxml attribute to its default value.
        /// </summary>
        /// <param name="field">The field to reset.</param>
        /// <param name="attributeOwner">An instance created from the uxml element that owns the related xml attribute.</param>
        /// <param name="uxmlDocument">The uxml document that contains the uxml attribute owner.</param>
        /// <param name="attributeUxmlOwner">The uxml element that owns the uxml attribute related to field to reset.</param>
        /// <param name="attribute">The uxml attribute related to the field to reset.</param>
        void ResetFieldValue(VisualElement field, object attributeOwner, VisualTreeAsset uxmlDocument, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute);

        void ResetFieldValueToInline(VisualElement field, object attributeOwner, VisualTreeAsset uxmlDocument, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute);
    }

    /// <summary>
    /// Base factory to create a ui field based on a TypedUxmlAttributeDescription .
    /// </summary>
    /// <typeparam name="T">The value type of TypedUxmlAttributeDescription for which this factory can create fields</typeparam>
    /// <typeparam name="TFieldType">The class of fields created by this factory</typeparam>
    internal abstract class BuilderTypedUxmlAttributeFieldFactoryBase<T, TFieldType> : IBuilderUxmlAttributeFieldFactory where TFieldType : BaseField<T>
    {
        public virtual bool CanCreateField(object attributeOwner, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute)
        {
            return attribute is TypedUxmlAttributeDescription<T>;
        }

        /// <summary>
        /// Instantiates a ui field based on the specified uxml attribute.
        /// </summary>
        /// <param name="attributeOwner">An instance created from the uxml element that owns the related xml attribute.</param>
        /// <param name="attributeUxmlOwner">The uxml element that owns the uxml attribute related to field to instantiate.</param>
        /// <param name="attribute">The uxml attribute.</param>
        /// <returns></returns>
        protected abstract TFieldType InstantiateField(object attributeOwner, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute);

        public VisualElement CreateField(object attributeOwner, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute, Action<VisualElement, UxmlAttributeDescription, object, string> onValueChange)
        {
            var fieldLabel = BuilderNameUtilities.ConvertDashToHuman(attribute.name);
            var field = InstantiateField(attributeOwner, attributeUxmlOwner, attribute);

            field.label = fieldLabel;

            if (attribute.name.Equals("multiline") && attributeOwner is TextField)
            {
                field.RegisterValueChangedCallback(evt =>
                {
                    OnMultilineToggleValueChange(evt, attributeUxmlOwner);
                    NotifyValueChanged(evt, field, attributeOwner, attributeUxmlOwner, attribute, ValueToUxml.Convert(evt.newValue), onValueChange);
                });

                return field;
            }

            field.RegisterValueChangedCallback((evt) =>
            {
                NotifyValueChanged(evt, field, attributeOwner, attributeUxmlOwner, attribute, ValueToUxml.Convert(evt.newValue), onValueChange);
            });
            return field;
        }

        public virtual void SetFieldValue(VisualElement field, object attributeOwner, VisualTreeAsset uxmlDocument, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute, object value)
        {
            (field as TFieldType).SetValueWithoutNotify((T)value);
        }

        public virtual void ResetFieldValue(VisualElement field, object attributeOwner, VisualTreeAsset uxmlDocument, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute)
        {
            var typedAttribute = attribute as TypedUxmlAttributeDescription<T>;
            (field as TFieldType).SetValueWithoutNotify(typedAttribute.defaultValue);
        }

        public virtual void ResetFieldValueToInline(VisualElement field, object attributeOwner, VisualTreeAsset uxmlDocument,
            UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute)
        {
            var typedAttribute = attribute as TypedUxmlAttributeDescription<T>;
            var value = typedAttribute.GetValueFromBag(attributeUxmlOwner, CreationContext.Default);

            SetFieldValue(field, attributeOwner, uxmlDocument, attributeUxmlOwner, attribute, value);
        }

        /// <summary>
        /// Notifies that the value of the specified field has changed.
        /// </summary>
        /// <param name="evt">The event emitted when the value of the field changed.</param>
        /// <param name="field">The field created by this factory of which value has changed.</param>
        /// <param name="attributeOwner">An instance created from the uxml element that owns the related xml attribute.</param>
        /// <param name="attributeUxmlOwner">The uxml element that owns the uxml attribute related to the field.</param>
        /// <param name="attribute">The uxml attribute edited by the target field.</param>
        /// <param name="uxmlValue">The new value formatted to uxml.</param>
        /// <param name="onValueChange">The callback to invoke.</param>
        protected virtual void NotifyValueChanged(ChangeEvent<T> evt, TFieldType field
            , object attributeOwner
            , UxmlAsset attributeUxmlOwner
            , UxmlAttributeDescription attribute
            , string uxmlValue
            , Action<VisualElement, UxmlAttributeDescription, object, string> onValueChange)
        {
            onValueChange?.Invoke(field, attribute, evt.newValue, uxmlValue);
        }

        void OnMultilineToggleValueChange(ChangeEvent<T> evt, UxmlAsset attributeUxmlOwner)
        {
            if (evt.target is not Toggle target)
                return;
            if (evt is not ChangeEvent<bool> boolEvt)
                return;

            var valueFieldInInspector = target?.GetFirstAncestorOfType<BuilderInspector>().Query<TextField>().Where(x => x.bindingPath is "value").First();
            if (valueFieldInInspector == null)
                return;

            valueFieldInInspector.multiline = boolEvt.newValue;
            valueFieldInInspector.EnableInClassList(BuilderConstants.InspectorMultiLineTextFieldClassName, boolEvt.newValue);
            if (!boolEvt.newValue)
                return;

            // when multiline set, inspector field does not have \n, but its value attribute does
            // set inspector field value to the value attribute
            var valueAttributeString = attributeUxmlOwner.GetAttributeValue("value");
            valueFieldInInspector.SetValueWithoutNotify(valueAttributeString);
        }
    }

    /// <summary>
    /// Creates a ui field based on a TypedUxmlAttributeDescription.
    /// </summary>
    /// <typeparam name="T">The value type of TypedUxmlAttributeDescription for which this factory can create fields.</typeparam>
    /// <typeparam name="TFieldType">The class of fields created by this factory.</typeparam>
    internal class BuilderTypedUxmlAttributeFieldFactory<T, TFieldType> : BuilderTypedUxmlAttributeFieldFactoryBase<T, TFieldType> where TFieldType : BaseField<T>, new ()
    {
        protected override TFieldType InstantiateField(object attributeOwner, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute)
        {
            return new TFieldType();
        }
    }

    internal sealed class BuilderDefaultUxmlAttributeFieldFactory : BuilderTypedUxmlAttributeFieldFactory<string, TextField>
    {
        public override bool CanCreateField(object attributeOwner, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute)
        {
            return true;
        }

        public override void ResetFieldValue(VisualElement field, object attributeOwner, VisualTreeAsset uxmlDocument, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute)
        {
            (field as TextField).SetValueWithoutNotify(string.Empty);
        }
    }
}
