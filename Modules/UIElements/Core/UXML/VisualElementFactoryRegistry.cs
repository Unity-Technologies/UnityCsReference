// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class VisualElementFactoryRegistry
    {
#pragma warning disable CS0618 // Type or member is obsolete

        private static Dictionary<string, List<IUxmlFactory>> s_Factories;
        private static Dictionary<string, List<IUxmlFactory>> s_MovedTypesFactories;

        internal static string GetMovedUIControlTypeName(Type type, MovedFromAttribute attr)
        {
            if (type == null)
                return string.Empty;

            var data = attr.data;
            var namespaceName = data.nameSpaceHasChanged ? data.nameSpace : type.Namespace;
            var typeName = data.classHasChanged ? data.className : type.Name;
            var fullOldName = namespaceName + "." + typeName;
            return fullOldName;
        }

        internal static Dictionary<string, List<IUxmlFactory>> factories
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get
            {
                if (s_Factories == null)
                {
                    s_Factories = new Dictionary<string, List<IUxmlFactory>>();
                    s_MovedTypesFactories = new Dictionary<string, List<IUxmlFactory>>(50);
                    RegisterUserFactories();
                }

                return s_Factories;
            }
        }

        protected static void RegisterFactory(IUxmlFactory factory)
        {
            if (factories.TryGetValue(factory.uxmlQualifiedName, out var factoryList))
            {
                foreach (var f in factoryList)
                {
                    if (f.GetType() == factory.GetType())
                    {
                        throw new ArgumentException($"A factory for the type {factory.GetType().FullName} was already registered");
                    }
                }
                factoryList.Add(factory);
            }
            else
            {
                factoryList = new List<IUxmlFactory>();
                factoryList.Add(factory);
                s_Factories.Add(factory.uxmlQualifiedName, factoryList);
                var uxmlType = factory.uxmlType;
                var attr = uxmlType?.GetCustomAttribute<MovedFromAttribute>(false);
                if (attr != null && typeof(VisualElement).IsAssignableFrom(uxmlType))
                {
                    var movedTypeName = GetMovedUIControlTypeName(uxmlType, attr);
                    if (string.IsNullOrEmpty(movedTypeName) == false)
                        s_MovedTypesFactories.Add(movedTypeName, factoryList);
                }
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static bool TryGetValue(string fullTypeName, out List<IUxmlFactory> factoryList)
        {
            var ret = factories.TryGetValue(fullTypeName, out factoryList);
            if (ret == false)
                ret = s_MovedTypesFactories.TryGetValue(fullTypeName, out factoryList);
            return ret;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static bool TryGetValue(Type type, out List<IUxmlFactory> factoryList)
        {
            foreach (var fl in factories.Values)
            {
                if (fl[0].uxmlType == type)
                {
                    factoryList = fl;
                    return true;
                }
            }

            factoryList = null;
            return false;
        }

        internal static void RegisterUserFactories()
        {
            // In the Player, we filter assemblies to only introspect types of user assemblies
            // which will exclude Unity builtin assemblies (i.e. runtime modules).
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
