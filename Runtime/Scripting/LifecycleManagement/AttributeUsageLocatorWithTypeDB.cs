// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using UnityEngine.Scripting;


namespace UnityEngine
{
    [NativeHeader("Runtime/Misc/LifecycleManager.h")]
    [RequiredByNativeCode]
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    internal class LifecycleAssemblyData
    {
        public string assemblyName;
        public MethodInfo[] methodInfos;
    }

    [NativeHeader("Runtime/Misc/LifecycleManager.h")]
    [RequiredByNativeCode]
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    //[NativeType(CodegenOptions.Auto, "ManagedLifecycleAttributeData")]
    internal class LifecycleAttributeData
    {
        public Type AttributeType;
        public LifecycleAssemblyData[] assemblyData;
    }

#pragma warning disable RS0030 // This [Preserve] usage will be addressed by future work https://jira.unity3d.com/browse/SCP-1622
    [Preserve]
#pragma warning restore RS0030
    internal class AttributeUsageLocatorWithTypeDB : IAttributeUsageLocator
    {
        private static Dictionary<Type, PerAssemblyMethodCatalog> hookCaches = new Dictionary<Type, PerAssemblyMethodCatalog>();

        public AttributeUsageLocatorWithTypeDB()
        {
            if (hookCaches.Count == 0)
                ExtractData();
        }

        public PerAssemblyMethodCatalog FindStaticMethodsWithAttribute(Type attributeType)
        {
            if (!hookCaches.TryGetValue(attributeType, out var result))
            {
                return new PerAssemblyMethodCatalog();
            }

            return result;
        }

        public IEnumerable<MethodInfo> FindStaticMethodsWithAttribute(Type attributeType, Assembly assembly)
        {
            if (!hookCaches.TryGetValue(attributeType, out var result))
            {
                return new List<MethodInfo>();
            }

            if (!result.TryGetValue(assembly.GetName().Name, out var methodInfos))
                return new List<MethodInfo>();

            return methodInfos;
        }

        private void ExtractData()
        {
            var attributeData = Internal_GetTypeDBExtracteLifeCycleHooks();
            foreach (var data in attributeData)
            {
                if (!hookCaches.TryGetValue(data.AttributeType, out var catalog))
                {
                    catalog = new PerAssemblyMethodCatalog();
                    hookCaches.Add(data.AttributeType, catalog);
                }

                foreach (var assemblyData in data.assemblyData)
                {
                    catalog.Add(assemblyData.assemblyName, assemblyData.methodInfos.ToList());
                }
            }
        }

        [FreeFunction("Internal_GetTypeDBExtracteLifeCycleHooks")]
        public static extern LifecycleAttributeData[] Internal_GetTypeDBExtracteLifeCycleHooks();
    }
}

