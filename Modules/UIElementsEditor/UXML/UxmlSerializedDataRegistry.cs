// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;
using HelpBox = UnityEngine.UIElements.HelpBox;

namespace UnityEditor.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static class UxmlSerializedDataRegistry
    {
        private static bool s_Initialized = false;
        static readonly Dictionary<string, Type> s_MovedTypes = new();
        static readonly Dictionary<string, UxmlSerializedDataDescription> s_DescriptionsCache = new();

        public static Dictionary<string, Type> SerializedDataTypes { get; } = new();

        [UsedImplicitly]
        internal static void RegisterCustomDependencies()
        {
            // No need to register custom dependencies when going to play mode
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            UxmlCodeDependencies.instance.RegisterUxmlSerializedDataDependencies(SerializedDataTypes);
        }

        // Used for testing
        public static void ResetCache()
        {
            ClearCache();
            RegisterUxmlSerializedDataTypes();
        }

        // Used for testing
        public static void ClearCache()
        {
            SerializedDataTypes.Clear();
            s_MovedTypes.Clear();
            ClearDescriptionCache();
        }

        // Used for testing
        public static void ClearDescriptionCache()
        {
            s_DescriptionsCache.Clear();
            UxmlDescriptionRegistry.Clear();
            s_Initialized = false;
        }

        public static UxmlSerializedDataDescription GetDescription(string typeName)
        {
            if (s_DescriptionsCache.TryGetValue(typeName, out var desc))
                return desc;

            if (!SerializedDataTypes.TryGetValue(typeName, out var type) && !s_MovedTypes.TryGetValue(typeName, out type))
                return null;

            desc = UxmlSerializedDataDescription.Create(type);
            s_DescriptionsCache.Add(typeName, desc);
            return desc;
        }

        public static Type GetDataType(string typeName)
        {
            if (!SerializedDataTypes.TryGetValue(typeName, out var type) && !s_MovedTypes.TryGetValue(typeName, out type))
                return null;

            return type;
        }

        public static void RegisterUxmlSerializedDataTypes()
        {
            if (s_Initialized)
                return;

            RegisterBuiltInTypes();

            var registrationMethods = TypeCache.GetMethodsWithAttribute<RegisterUxmlCacheAttribute>();
            foreach (var registrationMethod in registrationMethods)
            {
                try
                {
                    if (registrationMethod.ContainsGenericParameters)
                        continue;
                    registrationMethod.Invoke(null, null);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            var uxmlSerializedDataTypes = TypeCache.GetTypesDerivedFrom<UxmlSerializedData>();

            foreach (var uxmlSerializedDataType in uxmlSerializedDataTypes)
            {
                var attributes = uxmlSerializedDataType.Attributes;
                if ((attributes & TypeAttributes.Abstract) != 0)
                    continue;

                var declaringType = uxmlSerializedDataType.DeclaringType;
                // All "valid" UxmlSerializedData types need to be nested under a UxmlElement or UxmlObject type.
                if (null == declaringType || declaringType.ContainsGenericParameters)
                    continue;

                var uxmlElementAttribute = declaringType.GetCustomAttribute<UxmlElementAttribute>();
                if (uxmlElementAttribute != null && !string.IsNullOrEmpty(uxmlElementAttribute.name) &&
                    uxmlElementAttribute.name != declaringType.Name) // Ignore the default name (UUM-73716)
                {
                    var nameValidationError = UxmlUtility.ValidateUxmlName(uxmlElementAttribute.name);
                    if (nameValidationError != null)
                    {
                        Debug.LogError($"Invalid UXML element name '{uxmlElementAttribute.name}' for type '{declaringType.FullName}'. {nameValidationError}");
                        continue;
                    }

                    RegisterSerializedType(
                        string.IsNullOrEmpty(declaringType.Namespace)
                            ? uxmlElementAttribute.name
                            : $"{declaringType.Namespace}.{uxmlElementAttribute.name}", uxmlSerializedDataType, SerializedDataTypes);
                }

                RegisterSerializedType(declaringType.FullName, uxmlSerializedDataType, SerializedDataTypes);
                RegisterMovedFromType(declaringType, uxmlSerializedDataType, s_MovedTypes);
            }

            s_Initialized = true;
            return;

            [Pure]
            static void RegisterSerializedType(string typeName, Type serializedDataType, Dictionary<string, Type> cache)
            {
                if (string.IsNullOrEmpty(typeName))
                    return;

                if (cache.TryGetValue(typeName, out var cachedSerializedDataType))
                {
                    if (serializedDataType == cachedSerializedDataType)
                    {
                        Debug.LogWarning($"UxmlElement Registration: The UxmlElement of type '{typeName}' in the assembly '{serializedDataType.Assembly.GetName().Name}' has already been registered.");
                        return;
                    }

                    if (serializedDataType.Assembly != cachedSerializedDataType.Assembly)
                        Debug.LogError($"UxmlElement Registration Error: A UxmlElement of type '{typeName}' in the assembly '{serializedDataType.Assembly.GetName().Name}' has already been registered by a different assembly '{cachedSerializedDataType.Assembly.GetName().Name}.");
                    else
                        Debug.LogError($"UxmlElement Registration Error: A UxmlElement of type '{typeName}' is already registered with '{cachedSerializedDataType.Name}'. It cannot be registered again with '{serializedDataType.Name}'.");
                }
                cache[typeName] = serializedDataType;
            }

            [Pure]
            static void RegisterMovedFromType(Type declaringType, Type serializedDataType, Dictionary<string, Type> cache)
            {
                // Check for MovedFromAttribute
                var movedFromAttribute = declaringType.GetCustomAttribute<MovedFromAttribute>();
                if (movedFromAttribute != null)
                {
                    var fullOldName = VisualElementFactoryRegistry.GetMovedUIControlTypeName(declaringType, movedFromAttribute);
                    if (cache.TryGetValue(fullOldName, out var conflict))
                    {
                        Debug.LogError($"The UxmlElement for the type {declaringType.FullName} contains a MovedFromAttribute with the old name {fullOldName} which is already registered to {conflict.DeclaringType.FullName}.");
                        return;
                    }

                    cache[fullOldName] = serializedDataType;
                }
            }
        }

        private static void RegisterBuiltInTypes()
        {
            AbstractProgressBar.UxmlSerializedData.Register();
            BaseBoolField.UxmlSerializedData.Register();
            BaseListView.UxmlSerializedData.Register();
            BaseTreeView.UxmlSerializedData.Register();
            BaseVerticalCollectionView.UxmlSerializedData.Register();
            BindableElement.UxmlSerializedData.Register();
            Binding.UxmlSerializedData.Register();
            BoundsField.UxmlSerializedData.Register();
            BoundsIntField.UxmlSerializedData.Register();
            Button.UxmlSerializedData.Register();
            ButtonStripField.UxmlSerializedData.Register();
            ColorField.UxmlSerializedData.Register();
            Column.UxmlSerializedData.Register();
            Columns.UxmlSerializedData.Register();
            CurveField.UxmlSerializedData.Register();
            DataBinding.UxmlSerializedData.Register();
            DoubleField.UxmlSerializedData.Register();
            DropdownField.UxmlSerializedData.Register();
            EnumField.UxmlSerializedData.Register();
            EnumFlagsField.UxmlSerializedData.Register();
            FloatField.UxmlSerializedData.Register();
            Foldout.UxmlSerializedData.Register();
            GradientField.UxmlSerializedData.Register();
            GroupBox.UxmlSerializedData.Register();
            Hash128Field.UxmlSerializedData.Register();
            HelpBox.UxmlSerializedData.Register();
            IntegerField.UxmlSerializedData.Register();
            LayerField.UxmlSerializedData.Register();
            LayerMaskField.UxmlSerializedData.Register();
            ListView.UxmlSerializedData.Register();
            LongField.UxmlSerializedData.Register();
            MaskField.UxmlSerializedData.Register();
            MinMaxSlider.UxmlSerializedData.Register();
            MultiColumnListView.UxmlSerializedData.Register();
            MultiColumnTreeView.UxmlSerializedData.Register();
            ObjectField.UxmlSerializedData.Register();
            PropertyField.UxmlSerializedData.Register();
            RadioButton.UxmlSerializedData.Register();
            RadioButtonGroup.UxmlSerializedData.Register();
            RectField.UxmlSerializedData.Register();
            RectIntField.UxmlSerializedData.Register();
            RepeatButton.UxmlSerializedData.Register();
            ScrollView.UxmlSerializedData.Register();
            Scroller.UxmlSerializedData.Register();
            Slider.UxmlSerializedData.Register();
            SliderInt.UxmlSerializedData.Register();
            SortColumnDescription.UxmlSerializedData.Register();
            SortColumnDescriptions.UxmlSerializedData.Register();
            Tab.UxmlSerializedData.Register();
            TabView.UxmlSerializedData.Register();
            TagField.UxmlSerializedData.Register();
            TemplateContainer.UxmlSerializedData.Register();
            TextElement.UxmlSerializedData.Register();
            TextField.UxmlSerializedData.Register();
            Toggle.UxmlSerializedData.Register();
            ToggleButtonGroup.UxmlSerializedData.Register();
            ToolbarSearchField.UxmlSerializedData.Register();
            TreeView.UxmlSerializedData.Register();
            TwoPaneSplitView.UxmlSerializedData.Register();
            UnsignedIntegerField.UxmlSerializedData.Register();
            UnsignedLongField.UxmlSerializedData.Register();
            Vector2Field.UxmlSerializedData.Register();
            Vector2IntField.UxmlSerializedData.Register();
            Vector3Field.UxmlSerializedData.Register();
            Vector3IntField.UxmlSerializedData.Register();
            Vector4Field.UxmlSerializedData.Register();
            VisualElement.UxmlSerializedData.Register();
        }
    }
}
