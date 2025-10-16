// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;
using Debug = UnityEngine.Debug;

namespace UnityEditor
{
    internal class AttributeUsageLocatorWithTypeCache : IAttributeUsageLocator
    {
        public PerAssemblyMethodCatalog FindStaticMethodsWithAttribute(Type attributeType)
        {
            var results = new PerAssemblyMethodCatalog();
            var methods = UnityEditor.TypeCache.GetMethodsWithAttribute(attributeType);
            if (methods.Count == 0)
            {
                return results;
            }
            foreach (MethodInfo method in methods)
            {
                if (!method.IsStatic)
                {
                    Debug.LogError("Method " + method.Name + " has to be static.");
                    continue;
                }
                Assembly assembly = method.DeclaringType.Assembly;
                string assemblyName = assembly.GetName().Name;
                if (assemblyName == null)
                {
                    continue;
                }

                if (!results.ContainsKey(assemblyName))
                {
                    results.Add(assemblyName, new List<MethodInfo>());
                }
                results[assemblyName].Add(method);
            }
            return results;
        }

        public IEnumerable<MethodInfo> FindStaticMethodsWithAttribute(Type attributeType, Assembly assembly)
        {
            string assemblyName = assembly.GetName().Name;
            if (assemblyName != null)
            {
                var methods = UnityEditor.TypeCache.GetMethodsWithAttribute(attributeType, assemblyName);
                foreach (var method in methods)
                {
                    if (!method.IsStatic)
                    {
                        Debug.LogError("Method " + method.Name + " has to be static.");
                        continue;
                    }
                    yield return method;
                }
            }
        }
    }
}
