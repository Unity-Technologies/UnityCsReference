// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// Finds the <see cref="ExternalBuildDrawer"/> implementations, keyed by the package each one declares.
    /// </summary>
    internal static class ExternalBuildDrawers
    {
        internal static Dictionary<string, ExternalBuildDrawer> Create()
        {
            var drawers = new Dictionary<string, ExternalBuildDrawer>();

            foreach (var declared in GetPackageToDrawerTypes(GetDrawerTypes()))
            {
                try
                {
                    drawers.Add(declared.Key, (ExternalBuildDrawer)Activator.CreateInstance(declared.Value));
                }
                catch (Exception e)
                {
                    Debug.LogError($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Could not create the external build drawer '{declared.Value.FullName}': {e.Message}");
                }
            }

            return drawers;
        }

        internal static Dictionary<string, Type> GetPackageToDrawerTypes(IReadOnlyList<Type> types)
        {
            var declared = new Dictionary<string, Type>();
            foreach (var type in types)
            {
                var attribute = (ExternalBuildDrawerAttribute)Attribute.GetCustomAttribute(type, typeof(ExternalBuildDrawerAttribute));
                if (attribute == null || string.IsNullOrEmpty(attribute.ProducerPackage))
                {
                    Debug.LogError($"{BuildAnalysisConstants.k_ConsoleLogPrefix} External build drawer '{type.FullName}' does not name the package it draws with [{nameof(ExternalBuildDrawer)}], so it was not created.");
                    continue;
                }

                if (type.GetConstructor(Type.EmptyTypes) == null)
                {
                    Debug.LogError($"{BuildAnalysisConstants.k_ConsoleLogPrefix} External build drawer '{type.FullName}' needs a public parameterless constructor, so it was not created.");
                    continue;
                }

                if (declared.TryGetValue(attribute.ProducerPackage, out var kept))
                {
                    Debug.LogError($"{BuildAnalysisConstants.k_ConsoleLogPrefix} External build drawers '{kept.FullName}' and '{type.FullName}' both draw builds from '{attribute.ProducerPackage}'. Only '{kept.FullName}' is used.");
                    continue;
                }

                declared.Add(attribute.ProducerPackage, type);
            }

            return declared;
        }

        private static List<Type> GetDrawerTypes()
        {
            var types = new List<Type>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<ExternalBuildDrawer>())
            {
                if (!type.IsAbstract && !type.ContainsGenericParameters)
                    types.Add(type);
            }

            types.Sort((left, right) => string.CompareOrdinal(left.FullName, right.FullName));
            return types;
        }
    }
}
