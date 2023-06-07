// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    internal class BuilderUxmlImageAttributeFieldFactory : BuilderTypedUxmlAttributeFieldFactoryBase<Object, BaseField<Object>>
    {
        public override bool CanCreateField(object attributeOwner, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute)
        {
            return attribute.GetType() == typeof(UxmlImageAttributeDescription);
        }

        protected override BaseField<Object> InstantiateField(object attributeOwner, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute)
        {
            return new ImageStyleField();
        }

        public override void SetFieldValue(VisualElement field, object attributeOwner, VisualTreeAsset uxmlDocument, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute, object value)
        {
            (field as ImageStyleField).SetValueWithoutNotify((value as Background?).Value.GetSelectedImage() ?? value as Object);
        }

        public override void ResetFieldValue(VisualElement field, object attributeOwner, VisualTreeAsset uxmlDocument, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute)
        {
            (field as ImageStyleField).SetValueWithoutNotify(null);
        }

        protected override void NotifyValueChanged(ChangeEvent<Object> evt,
            BaseField<Object> field,
            object attributeOwner,
            UxmlAsset attributeUxmlOwner,
            UxmlAttributeDescription attribute,
            string uxmlValue,
            Action<VisualElement, UxmlAttributeDescription, object, string> onValueChange)
        {
            var assetPath = AssetDatabase.GetAssetPath(evt.newValue);
            if (BuilderAssetUtilities.IsBuiltinPath(assetPath))
            {
                Builder.ShowWarning(BuilderConstants.BuiltInAssetPathsNotSupportedMessageUxml);

                // Revert the change.
                field.SetValueWithoutNotify(evt.previousValue);
                return;
            }

            base.NotifyValueChanged(evt, field, attributeOwner, attributeUxmlOwner, attribute, uxmlValue, onValueChange);
        }
    }
}
