using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.UIElements.Debugger;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [InitializeOnLoad]
    internal class UXMLEditorFactories : VisualElementFactoryRegistry
    {
        private static readonly bool k_Registered;

        static UXMLEditorFactories()
        {
            if (k_Registered)
                return;

            k_Registered = true;

            // Generic element types cannot be discovered through reflection.
            // Resort to manual registration for now (these classes are internal / duplicate PropertyField?)
            IUxmlFactory[] propertyControls =
            {
                new PropertyControl<int>.UxmlFactory(),
                new PropertyControl<long>.UxmlFactory(),
                new PropertyControl<float>.UxmlFactory(),
                new PropertyControl<double>.UxmlFactory(),
                new PropertyControl<string>.UxmlFactory(),
            };
            foreach (var uxmlFactory in propertyControls)
            {
                RegisterFactory(uxmlFactory);
            }

            // Discover all factories thanks to the type cache!
            var types = TypeCache.GetTypesDerivedFrom<IUxmlFactory>();
            foreach (var type in types)
            {
                TypeAttributes attributes = type.Attributes;
                if (type.Assembly.GetName().Name == "UnityEngine.UIElementsModule" // Exclude core UIElements factories which are registered manually
                    || (attributes & (TypeAttributes.Abstract | TypeAttributes.Interface)) != 0
                    || type.IsGenericType)
                    continue;
                var factory = (IUxmlFactory)Activator.CreateInstance(type);
                RegisterFactory(factory);
            }
        }
    }
}
