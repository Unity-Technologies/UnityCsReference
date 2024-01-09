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

namespace UnityEditor.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static class UxmlSerializedDataRegistry
    {
        const string k_DefaultDependencyPrefix = "UxmlSerializedData/";

        static bool s_Registered;

        static readonly Dictionary<string, Type> s_MovedTypes = new();
        static readonly Dictionary<string, UxmlSerializedDataDescription> s_DescriptionsCache = new();

        public static Dictionary<string, Type> SerializedDataTypes { get; } = new();

        [UsedImplicitly, InitializeOnLoadMethod]
        internal static void RegisterDependencies()
        {
            // No need to register custom dependencies when going to play mode
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Register();

            AssetDatabase.UnregisterCustomDependencyPrefixFilter(k_DefaultDependencyPrefix);
            foreach (var typeName in SerializedDataTypes.Keys)
            {
                var keyName = GetDependencyKeyName(typeName);
                AssetDatabase.RegisterCustomDependency(keyName, Hash128.Compute(typeName));
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
        }

        public static UxmlSerializedDataDescription GetDescription(string typeName)
        {
            if (!s_Registered)
                Register();

            if (s_DescriptionsCache.TryGetValue(typeName, out var desc))
                return desc;

            if (!SerializedDataTypes.TryGetValue(typeName, out var type) && !s_MovedTypes.TryGetValue(typeName, out type))
                return null;

            desc = UxmlSerializedDataDescription.Create(type);
            s_DescriptionsCache.Add(typeName, desc);
            return desc;
        }

        public static string GetDependencyKeyName(string typeName) => k_DefaultDependencyPrefix + typeName;

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
                if (uxmlElementAttribute != null && !string.IsNullOrEmpty(uxmlElementAttribute.name))
                {
                    if (string.IsNullOrEmpty(declaringType.Namespace))
                        RegisterType(uxmlElementAttribute.name, serializedDataType);
                    else
                        RegisterType($"{declaringType.Namespace}.{uxmlElementAttribute.name}", serializedDataType);
                }

                RegisterType(declaringType.FullName, serializedDataType);
            }

            s_Registered = true;
        }

        static void RegisterType(string typeName, Type serializedDataType)
        {
            if (SerializedDataTypes.TryGetValue(typeName, out var desc))
            {
                Debug.LogError($"A UxmlElement for the type {typeName} in the assembly {serializedDataType.Assembly.GetName().Name} was already registered from another assembly {desc.Assembly.GetName().Name}.");
                return;
            }

            SerializedDataTypes[typeName] = serializedDataType;

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
    }
}
