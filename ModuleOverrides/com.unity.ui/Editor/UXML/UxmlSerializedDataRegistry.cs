// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal static class UxmlSerializedDataRegistry
    {
        const string k_DefaultDependencyPrefix = "UxmlSerializedData/";

        private static bool s_Registered;

        // We use a sorted dictionary so that the UI Builder Library (ImportUxmlSerializedDataFromSource) can process the namespaces together.
        public static SortedDictionary<string, Type> SerializedDataTypes { get; } = new SortedDictionary<string, Type>();
        private static Dictionary<string, UxmlSerializedDataDescription> s_DescriptionsCache = new Dictionary<string, UxmlSerializedDataDescription>();

        [UsedImplicitly, InitializeOnLoadMethod]
        private static void RegisterDependencies()
        {
            // No need to register custom dependencies when going to play mode
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!s_Registered)
                Register();

            AssetDatabase.UnregisterCustomDependencyPrefixFilter(k_DefaultDependencyPrefix);
            foreach (var typeName in SerializedDataTypes.Keys)
            {
                var keyName = GetDependencyKeyName(typeName);
                AssetDatabase.RegisterCustomDependency(keyName, Hash128.Compute(typeName));
            }
        }

        public static UxmlSerializedDataDescription GetDescription(string typeName)
        {
            if (!s_Registered)
                Register();

            if (s_DescriptionsCache.TryGetValue(typeName, out var desc))
                return desc;

            if (!SerializedDataTypes.TryGetValue(typeName, out var type))
                return null;

            desc = UxmlSerializedDataDescription.Create(type);
            s_DescriptionsCache.Add(typeName, desc);
            return desc;
        }

        public static string GetDependencyKeyName(string typeName) => k_DefaultDependencyPrefix + typeName;

        private static void Register()
        {
            var types = TypeCache.GetTypesDerivedFrom<UxmlSerializedData>();
            foreach (var type in types)
            {
                var attributes = type.Attributes;
                if ((attributes & TypeAttributes.Abstract) != 0)
                    continue;

                var declaringType = type.DeclaringType;

                var uxmlElementAttribute = declaringType.GetCustomAttribute<UxmlElementAttribute>();
                if (uxmlElementAttribute != null && !string.IsNullOrEmpty(uxmlElementAttribute.name))
                {
                    if (string.IsNullOrEmpty(declaringType.Namespace))
                        RegisterType(uxmlElementAttribute.name, type);
                    else
                        RegisterType($"{declaringType.Namespace}.{uxmlElementAttribute.name}", type);
                }

                RegisterType(declaringType.FullName, type);
            }

            s_Registered = true;
        }

        static void RegisterType(string typeName, Type type)
        {
            if (SerializedDataTypes.ContainsKey(typeName))
            {
                Debug.LogError($"A UxmlElement for the type {typeName} was already registered");
                return;
            }

            SerializedDataTypes.Add(typeName, type);
        }
    }
}
