// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderUxmlTypeAttributeFieldFactory : IBuilderUxmlAttributeFieldFactory
    {
        static readonly Dictionary<Type, TypeInfo> s_CachedTypeInfos = new Dictionary<Type, TypeInfo>();

        static readonly UnityEngine.Pool.ObjectPool<BuilderAttributeTypeName> s_TypeNameItemPool = new(
                () => new BuilderAttributeTypeName(),
                null,
                c => c.ClearType());

        static TypeInfo GetTypeInfo(Type type)
        {
            if (!s_CachedTypeInfos.TryGetValue(type, out var typeInfo))
                s_CachedTypeInfos[type] = typeInfo = new TypeInfo(type);
            return typeInfo;
        }

        public readonly struct TypeInfo
        {
            public readonly Type type;
            public readonly string value;

            public TypeInfo(Type type)
            {
                this.type = type;
                value = type.GetFullNameWithAssembly();
            }
        }

        public bool CanCreateField(object attributeOwner, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute)
        {
            var attributeType = attribute.GetType();

            return (attributeType.IsGenericType && !attributeType.GetGenericArguments()[0].IsEnum && attributeType.GetGenericArguments()[0] is Type);
        }

        public VisualElement CreateField(object attributeOwner, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute, Action<VisualElement, UxmlAttributeDescription, object, string> onValueChange)
        {
            var attributeType = attribute.GetType();
            var desiredType = attributeType.GetGenericArguments()[0];
            var fieldLabel = BuilderNameUtilities.ConvertDashToHuman(attribute.name);
            return CreateField(desiredType, fieldLabel, attributeOwner, attributeUxmlOwner, attribute, onValueChange);
        }

        public VisualElement CreateField(Type desiredType, string label, object attributeOwner, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute, Action<VisualElement, UxmlAttributeDescription, object, string> onValueChange)
        {
            var uiField = new TextField(label) { isDelayed = true };
            var completer = new FieldSearchCompleter<TypeInfo>(uiField);

            completer.usesNativePopupWindow = true;
            // When possible, the popup should have the same width as the input field, so that the auto-complete
            // characters will try to match said input field.
            completer.matcherCallback += (str, info) => info.value.IndexOf(str, StringComparison.OrdinalIgnoreCase) >= 0;
            completer.itemHeight = 36;
            completer.dataSourceCallback += () =>
            {
                var desiredTypeInfo = new TypeInfo(desiredType);

                return TypeCache.GetTypesDerivedFrom(desiredType)
                    .Where(t => !t.IsGenericType)
                    // Remove UIBuilder types from the list
                    .Where(t => t.Assembly != GetType().Assembly)
                    .Select(GetTypeInfo)
                    .Append(desiredTypeInfo);
            };
            completer.getTextFromDataCallback += info => info.value;
            completer.makeItem = () => s_TypeNameItemPool.Get();
            completer.destroyItem = e =>
            {
                if (e is BuilderAttributeTypeName typeItem)
                    s_TypeNameItemPool.Release(typeItem);
            };
            completer.bindItem = (v, i) =>
            {
                if (v is BuilderAttributeTypeName l)
                    l.SetType(completer.results[i].type, completer.textField.text);
            };

            uiField.RegisterValueChangedCallback(e =>
            {
                // null and empty string are considered equal in this case
                if (string.IsNullOrEmpty(e.newValue) && string.IsNullOrEmpty(e.previousValue))
                    return;
                OnValidatedTypeAttributeChange(e, desiredType
                    , attributeOwner, attribute, onValueChange);
            });
            uiField.userData = completer;
            return uiField;
        }

        public void SetFieldValue(VisualElement field, object attributeOwner, VisualTreeAsset uxmlDocument, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute, object value)
        {
            var strValue = string.Empty;

            if (value is Type typeValue)
            {
                var fullTypeName = typeValue.AssemblyQualifiedName;
                var fullTypeNameSplit = fullTypeName.Split(',');
                strValue = $"{fullTypeNameSplit[0]},{fullTypeNameSplit[1]}";
            }
            else if (value is string str)
            {
                strValue = str;
            }

            (field as TextField).SetValueWithoutNotify(strValue);
        }

        public void ResetFieldValue(VisualElement field, object attributeOwner, VisualTreeAsset uxmlDocument, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute)
        {
            var a = attribute as TypedUxmlAttributeDescription<Type>;
            var f = field as TextField;

            if (a.defaultValue == null)
                f.SetValueWithoutNotify(string.Empty);
            else
                f.SetValueWithoutNotify(a.defaultValue.ToString());
        }

        public void ResetFieldValueToInline(VisualElement field, object attributeOwner, VisualTreeAsset uxmlDocument, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute)
        {
            var a = attribute as TypedUxmlAttributeDescription<Type>;
            var value = a.GetValueFromBag(attributeUxmlOwner, CreationContext.Default);
            var f = field as TextField;

            f.SetValueWithoutNotify(value.ToString());
        }

        void OnValidatedTypeAttributeChange(ChangeEvent<string> evt, Type desiredType
            , object attributeOwner, UxmlAttributeDescription attribute
            , Action<VisualElement, UxmlAttributeDescription, object, string> onValueChange)
        {
            var field = evt.elementTarget as TextField;
            var typeName = evt.newValue;
            var fullTypeName = typeName;

            if (field == null || evt.target == field.labelElement)
                return;

            Type type = null;
            if (!string.IsNullOrEmpty(typeName))
            {
                type = Type.GetType(fullTypeName, false);

                // Try some auto-fixes.
                if (type == null)
                {
                    fullTypeName = typeName + ", UnityEngine.CoreModule";
                    type = Type.GetType(fullTypeName, false);
                }

                if (type == null)
                {
                    fullTypeName = typeName + ", UnityEditor";
                    type = Type.GetType(fullTypeName, false);
                }

                if (type == null && typeName.Contains("."))
                {
                    var split = typeName.Split('.');
                    fullTypeName = typeName + $", {split[0]}.{split[1]}Module";
                    type = Type.GetType(fullTypeName, false);
                }

                if (type == null)
                {
                    Builder.ShowWarning(string.Format(BuilderConstants.TypeAttributeInvalidTypeMessage, field.label));
                    evt.StopPropagation();
                    return;
                }
                else if (!desiredType.IsAssignableFrom(type))
                {
                    Builder.ShowWarning(string.Format(BuilderConstants.TypeAttributeMustDeriveFromMessage, field.label,
                        desiredType.FullName));
                    evt.StopPropagation();
                    return;
                }
            }

            field.value = fullTypeName;
            onValueChange?.Invoke(field, attribute, fullTypeName, fullTypeName);
        }
    }
}
