// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    internal class BuilderUxmlImageAttributeFieldFactory : IBuilderUxmlAttributeFieldFactory
    {
        public bool CanCreateField(object attributeOwner, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute)
        {
            return attribute.GetType() == typeof(UxmlImageAttributeDescription);
        }

        public VisualElement CreateField(object attributeOwner, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute, Action<VisualElement, UxmlAttributeDescription, object, string> onValueChange)
        {
            var fieldLabel = BuilderNameUtilities.ConvertDashToHuman(attribute.name);
            var imageTypeField = new ImageStyleField(fieldLabel);

            imageTypeField.RegisterValueChangedCallback((evt) =>
            {
                NotifyValueChanged(evt, imageTypeField, attribute, ValueToUxml.Convert(evt.newValue), onValueChange);
            });

            return imageTypeField;
        }

        public void SetFieldValue(VisualElement field, object attributeOwner, VisualTreeAsset uxmlDocument, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute, object value)
        {
            (field as ImageStyleField).SetValueWithoutNotify((value as Background?).Value.GetSelectedImage() ?? value as Object);
        }

        public void ResetFieldValue(VisualElement field, object attributeOwner, VisualTreeAsset uxmlDocument, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute)
        {
            (field as ImageStyleField).SetValueWithoutNotify(null);
        }

        protected void NotifyValueChanged(ChangeEvent<UnityEngine.Object> evt
            , ImageStyleField field
            , UxmlAttributeDescription attribute
            , string uxmlValue
            , Action<VisualElement, UxmlAttributeDescription, object, string> onValueChange)
        {
            var assetPath = AssetDatabase.GetAssetPath(evt.newValue);
            if (BuilderAssetUtilities.IsBuiltinPath(assetPath))
            {
                Builder.ShowWarning(BuilderConstants.BuiltInAssetPathsNotSupportedMessageUxml);

                // Revert the change.
                field.SetValueWithoutNotify(evt.previousValue);
                return;
            }

            onValueChange?.Invoke(field, attribute, evt.newValue, uxmlValue);
        }
    }
}
