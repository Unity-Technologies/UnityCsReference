// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderUxmlIntAttributeFieldFactory : BuilderTypedUxmlAttributeFieldFactoryBase<int, BaseField<int>>
    {
        protected override BaseField<int> InstantiateField(object attributeOwner, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute)
        {
            if (attribute.name.Equals("value") && attributeOwner is LayerField)
            {
                return new LayerField();
            }

            if (attribute.name.Equals("value") && attributeOwner is LayerMaskField)
            {
                return new LayerMaskField();
            }

            if (attribute.name.Equals("fixed-item-height") &&
                     attributeOwner is BaseVerticalCollectionView)
            {
                var uiField = new IntegerField(BuilderNameUtilities.ConvertDashToHuman(attribute.name));
                uiField.isDelayed = true;
                uiField.RegisterCallback<InputEvent>(OnFixedHeightValueChangedImmediately);
                uiField.labelElement.RegisterCallback<PointerMoveEvent>(OnFixedHeightValueChangedImmediately);
                return uiField;
            }

            return new IntegerField();
        }

        public override void SetFieldValue(VisualElement field, object attributeOwner, VisualTreeAsset uxmlDocument, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute, object value)
        {
            if (field is IntegerField && attribute.name.Equals("fixed-item-height") && attributeOwner is BaseVerticalCollectionView)
            {
                var styleRow = field.GetProperty(BuilderConstants.InspectorLinkedStyleRowVEPropertyName) as BuilderStyleRow;
                styleRow?.contentContainer.AddToClassList(BuilderConstants.InspectorFixedItemHeightFieldClassName);
            }

            if (field is LayerField layerField)
            {
                var layerFieldAttributeOwner = attributeOwner as LayerField;

                layerField.SetValueWithoutNotify(layerFieldAttributeOwner.value);
            }
            else if (field is LayerMaskField layerMaskField)
            {
                var layerMaskFieldAttributeOwner = attributeOwner as LayerMaskField;

                layerMaskField.SetValueWithoutNotify(layerMaskFieldAttributeOwner.value);
            }
            else
            {
                if (value is float)
                    value = Convert.ToInt32(value);
                base.SetFieldValue(field, attributeOwner, uxmlDocument, attributeUxmlOwner, attribute, value);
            }
        }

        void OnFixedItemHeightValueChanged(ChangeEvent<int> evt, UxmlAttributeDescription attribute, Action<VisualElement, UxmlAttributeDescription, object, string> onValueChange)
        {
            var field = evt.currentTarget as IntegerField;
            if (evt.newValue < 1)
            {
                SetNegativeFixedItemHeightHelpBoxEnabled(true,field);
                field.SetValueWithoutNotify(1);
                onValueChange?.Invoke(field, attribute, field.value, ValueToUxml.Convert(field.value));
                return;
            }

            onValueChange?.Invoke(field, attribute, evt.newValue, ValueToUxml.Convert(evt.newValue));
        }

        void OnFixedHeightValueChangedImmediately(InputEvent evt)
        {
            var field = evt.currentTarget as BaseField<int>;
            if (field == null)
                return;

            var newValue = evt.newData;
            var valueResolved = UINumericFieldsUtils.TryConvertStringToLong(newValue, out var v);
            var resolvedValue = valueResolved ? Mathf.ClampToInt(v) : field.value;

            SetNegativeFixedItemHeightHelpBoxEnabled((newValue.Length != 0 && (resolvedValue < 1 || newValue.Equals("-"))), field);
        }

        void OnFixedHeightValueChangedImmediately(PointerMoveEvent evt)
        {
            if (evt.target is not Label labelElement)
                return;

            var field = labelElement.parent as TextInputBaseField<int>;
            if (field == null)
                return;
            var valueResolved = UINumericFieldsUtils.TryConvertStringToLong(field.text, out var v);
            var resolvedValue = valueResolved ? Mathf.ClampToInt(v) : field.value;

            SetNegativeFixedItemHeightHelpBoxEnabled((resolvedValue < 1 || field.text.ToCharArray()[0].Equals('-')), field);
        }

        void SetNegativeFixedItemHeightHelpBoxEnabled(bool enabled, BaseField<int> field)
        {
            var negativeWarningHelpBox = field.parent.Q<UnityEngine.UIElements.HelpBox>();
            if (enabled)
            {
                if (negativeWarningHelpBox == null)
                {
                    negativeWarningHelpBox = new UnityEngine.UIElements.HelpBox(
                        L10n.Tr(BuilderConstants.HeightIntFieldValueCannotBeNegativeMessage), HelpBoxMessageType.Warning);
                    field.parent.Add(negativeWarningHelpBox);
                    negativeWarningHelpBox.AddToClassList(BuilderConstants.InspectorShownWarningMessageClassName);
                }
                else
                {
                    negativeWarningHelpBox.AddToClassList(BuilderConstants.InspectorShownWarningMessageClassName);
                    negativeWarningHelpBox.RemoveFromClassList(BuilderConstants.InspectorHiddenWarningMessageClassName);
                }
                return;
            }

            if (negativeWarningHelpBox == null)
                return;
            negativeWarningHelpBox.AddToClassList(BuilderConstants.InspectorHiddenWarningMessageClassName);
            negativeWarningHelpBox.RemoveFromClassList(BuilderConstants.InspectorShownWarningMessageClassName);
        }

        protected override void NotifyValueChanged(ChangeEvent<int> evt, BaseField<int> field
            , object attributeOwner
            , UxmlAsset attributeUxmlOwner
            , UxmlAttributeDescription attribute
            , string uxmlValue
            , Action<VisualElement, UxmlAttributeDescription, object, string> onValueChange)
        {
            if (attribute.name.Equals("fixed-item-height") &&
                attributeOwner is BaseVerticalCollectionView)
            {
                OnFixedItemHeightValueChanged(evt, attribute, onValueChange);
                return;
            }
            onValueChange?.Invoke(field, attribute, evt.newValue, uxmlValue);
        }
    }
}
