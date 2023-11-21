// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [InitializeOnLoad]
    internal class UXMLEditorFactories : VisualElementFactoryRegistry
    {
        private static readonly bool k_Registered;
        static readonly string k_UIECoreModule = "UnityEngine.UIElementsModule";

        static UXMLEditorFactories()
        {
            if (k_Registered)
                return;

            k_Registered = true;

            #pragma warning disable CS0618 // Type or member is obsolete
            // Discover all factories thanks to the type cache!
            var types = TypeCache.GetTypesDerivedFrom<IUxmlFactory>();
            foreach (var type in types)
            {
                var attributes = type.Attributes;
                if (type.Assembly.GetName().Name == k_UIECoreModule // Exclude core UIElements factories which are registered manually
                    || (attributes & (TypeAttributes.Abstract | TypeAttributes.Interface)) != 0
                    || type.IsGenericType)
                    continue;
                var factory = (IUxmlFactory)Activator.CreateInstance(type);
                RegisterFactory(factory);
            }
            #pragma warning restore CS0618 // Type or member is obsolete

            foreach (var factoryList in factories.Values)
            {
                foreach (var factory in factoryList)
                {
                    UxmlCodeDependencies.instance.RegisterAssetAttributeDependencies(factory);
                }
            }
        }
    }

    [InitializeOnLoad]
    [Obsolete("UxmlObjectEditorFactories is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
    internal class UxmlObjectEditorFactories : UxmlObjectFactoryRegistry
    {
        private static readonly bool k_Registered;
        static readonly string k_UIECoreModule = "UnityEngine.UIElementsModule";

        static UxmlObjectEditorFactories()
        {
            if (k_Registered)
                return;

            k_Registered = true;

            // Discover all factories thanks to the type cache!
            var types = TypeCache.GetTypesDerivedFrom<IBaseUxmlObjectFactory>();
            foreach (var type in types)
            {
                var attributes = type.Attributes;
                if (type.Assembly.GetName().Name == k_UIECoreModule // Exclude core UIElements factories which are registered manually
                    || (attributes & (TypeAttributes.Abstract | TypeAttributes.Interface)) != 0
                    || type.IsGenericType)
                    continue;
                var factory = (IBaseUxmlObjectFactory)Activator.CreateInstance(type);
                RegisterFactory(factory);
            }

            foreach (var factoryList in factories.Values)
            {
                foreach (var factory in factoryList)
                {
                    UxmlCodeDependencies.instance.RegisterAssetAttributeDependencies(factory);
                }
            }
        }
    }
}
