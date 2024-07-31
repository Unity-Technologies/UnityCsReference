// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static class UxmlSerializedDataRegistry
    {
        private static JobHandle? k_UxmlRegistryRegistrationHandle;

        static bool s_Registered;

        static readonly Dictionary<string, Type> s_MovedTypes = new();
        static readonly Dictionary<string, UxmlSerializedDataDescription> s_DescriptionsCache = new();

        public static Dictionary<string, Type> SerializedDataTypes { get; } = new();

        [UsedImplicitly]
        internal static void GenerateUxmlRegistries()
        {
            k_UxmlRegistryRegistrationHandle = new InitializeUxmlRegistryDescriptions().Schedule();
        }

        [UsedImplicitly]
        internal static void RegisterCustomDependencies()
        {
            // No need to register custom dependencies when going to play mode
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (k_UxmlRegistryRegistrationHandle is { IsCompleted: false })
            {
                // Force the initialization to complete, calling all the "initialize on load" should
                // give us enough time to process all the registry.
                k_UxmlRegistryRegistrationHandle?.Complete();
            }

            UxmlCodeDependencies.instance.RegisterUxmlSerializedDataDependencies(SerializedDataTypes);
        }

        struct InitializeUxmlRegistryDescriptions : IJob
        {
            public void Execute()
            {
                Register();
            }
        }

        // Used for testing
        public static void ClearCache()
        {
            SerializedDataTypes.Clear();
            s_MovedTypes.Clear();

            s_Registered = false;

            ClearDescriptionCache();
        }

        // Used for testing
        public static void ClearDescriptionCache()
        {
            s_DescriptionsCache.Clear();
            UxmlDescriptionRegistry.Clear();
        }

        public static UxmlSerializedDataDescription GetDescription(string typeName)
        {
            ForceRegistrationCompletion();

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
            ForceRegistrationCompletion();

            if (!SerializedDataTypes.TryGetValue(typeName, out var type) && !s_MovedTypes.TryGetValue(typeName, out type))
                return null;

            return type;
        }

        public static void Register()
        {
            if (s_Registered)
                return;

            var types = TypeCache.GetTypesDerivedFrom<UxmlSerializedData>();
            foreach (var serializedDataType in types)
            {
                var attributes = serializedDataType.Attributes;
                if ((attributes & TypeAttributes.Abstract) != 0)
                    continue;

                var declaringType = serializedDataType.DeclaringType;

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

                    if (string.IsNullOrEmpty(declaringType.Namespace))
                        RegisterType(uxmlElementAttribute.name, serializedDataType);
                    else
                        RegisterType($"{declaringType.Namespace}.{uxmlElementAttribute.name}", serializedDataType);
                }

                RegisterType(declaringType.FullName, serializedDataType);
            }

            s_Registered = true;
            k_UxmlRegistryRegistrationHandle = null;
        }

        static void RegisterType(string typeName, Type serializedDataType)
        {
            if (SerializedDataTypes.TryGetValue(typeName, out var desc))
            {
                if (serializedDataType == desc)
                {
                    Debug.LogWarning($"UxmlElement Registration: The UxmlElement of type '{typeName}' in the assembly '{serializedDataType.Assembly.GetName().Name}' has already been registered.");
                    return;
                }

                if (serializedDataType.Assembly != desc.Assembly)
                    Debug.LogError($"UxmlElement Registration Error: A UxmlElement of type '{typeName}' in the assembly '{serializedDataType.Assembly.GetName().Name}' has already been registered by a different assembly '{desc.Assembly.GetName().Name}.");
                else
                    Debug.LogError($"UxmlElement Registration Error: A UxmlElement of type '{typeName}' is already registered with '{desc.Name}'. It cannot be registered again with '{serializedDataType.Name}'.");

                return;
            }

            SerializedDataTypes[typeName] = serializedDataType;
            // Force the generation of the uxml description so that it happens on the background thread.
            UxmlDescriptionRegistry.GetDescription(serializedDataType);

            // Check for MovedFromAttribute
            var elementType = serializedDataType.DeclaringType;
            var movedFromAttribute = elementType.GetCustomAttribute<MovedFromAttribute>();
            if (movedFromAttribute != null)
            {
                var fullOldName = VisualElementFactoryRegistry.GetMovedUIControlTypeName(elementType, movedFromAttribute);
                if (s_MovedTypes.TryGetValue(fullOldName, out var conflict))
                {
                    Debug.LogError($"The UxmlElement for the type {typeName} contains a MovedFromAttribute with the old name {fullOldName} which is already registered to {conflict.DeclaringType.FullName}.");
                    return;
                }

                s_MovedTypes[fullOldName] = serializedDataType;
            }
        }

        private static void ForceRegistrationCompletion()
        {
            if (s_Registered)
                return;

            // If this was called before the job was even started, run the registration in a synced manner, otherwise force
            // the job to complete.
            if (k_UxmlRegistryRegistrationHandle.HasValue)
                k_UxmlRegistryRegistrationHandle?.Complete();
            else
                Register();
        }
    }
}
