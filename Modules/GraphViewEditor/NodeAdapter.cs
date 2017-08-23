// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    // types of port to adapt
    internal
    class PortSource<T>
    {
    }

    // attribute to declare and adapter
    internal
    class TypeAdapter : Attribute
    {
    }

    // TODO: This is a straight port from Canvas2D. I don't think that having to check for types in the assembly using reflection is the way we want to go.
    internal
    class NodeAdapter
    {
        private static List<MethodInfo> s_TypeAdapters;
        private static Dictionary<int, MethodInfo> s_NodeAdapterDictionary;

        public bool CanAdapt(object a, object b)
        {
            if (a == b)
                return false; // self connections are not permitted

            if (a == null || b == null)
                return false;

            MethodInfo mi = GetAdapter(a, b);
            if (mi == null)
            {
                Debug.Log("adapter node not found for: " + a.GetType() + " -> " + b.GetType());
            }
            return mi != null;
        }

        public bool Connect(object a, object b)
        {
            MethodInfo mi = GetAdapter(a, b);
            if (mi == null)
            {
                Debug.LogError("Attempt to connect 2 unadaptable types: " + a.GetType() + " -> " + b.GetType());
                return false;
            }
            object retVal = mi.Invoke(this, new[] { this, a, b });
            return (bool)retVal;
        }

        IEnumerable<MethodInfo> GetExtensionMethods(Assembly assembly, Type extendedType)
        {
            return assembly.GetTypes()
                .Where(t => t.IsSealed && !t.IsGenericType && !t.IsNested)
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(m => m.IsDefined(typeof(ExtensionAttribute), false) && m.GetParameters()[0].ParameterType == extendedType);
        }

        public MethodInfo GetAdapter(object a, object b)
        {
            if (a == null || b == null)
                return null;

            if (s_NodeAdapterDictionary == null)
            {
                s_NodeAdapterDictionary = new Dictionary<int, MethodInfo>();

                // add extension methods
                AppDomain currentDomain = AppDomain.CurrentDomain;
                foreach (Assembly assembly in currentDomain.GetAssemblies())
                {
                    foreach (MethodInfo method in GetExtensionMethods(assembly, typeof(NodeAdapter)))
                    {
                        ParameterInfo[] methodParams = method.GetParameters();
                        if (methodParams.Length == 3)
                        {
                            string pa = methodParams[1].ParameterType + methodParams[2].ParameterType.ToString();
                            s_NodeAdapterDictionary.Add(pa.GetHashCode(), method);
                        }
                    }
                }
            }

            string s = a.GetType().ToString() + b.GetType();

            MethodInfo methodInfo;
            return s_NodeAdapterDictionary.TryGetValue(s.GetHashCode(), out methodInfo) ? methodInfo : null;
        }

        public MethodInfo GetTypeAdapter(Type from, Type to)
        {
            if (s_TypeAdapters == null)
            {
                s_TypeAdapters = new List<MethodInfo>();
                AppDomain currentDomain = AppDomain.CurrentDomain;
                foreach (Assembly assembly in currentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (Type temptype in assembly.GetTypes())
                        {
                            MethodInfo[] methodInfos = temptype.GetMethods(BindingFlags.Public | BindingFlags.Static);
                            foreach (MethodInfo i in methodInfos)
                            {
                                object[] allAttrs = i.GetCustomAttributes(typeof(TypeAdapter), false);
                                if (allAttrs.Any())
                                {
                                    s_TypeAdapters.Add(i);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex);
                    }
                }
            }


            foreach (MethodInfo i in s_TypeAdapters)
            {
                if (i.ReturnType == to)
                {
                    ParameterInfo[] allParams = i.GetParameters();
                    if (allParams.Length == 1)
                    {
                        if (allParams[0].ParameterType == from)
                            return i;
                    }
                }
            }
            return null;
        }
    }
}
