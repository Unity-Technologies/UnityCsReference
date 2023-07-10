// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal class UxmlObjectFactoryRegistry
    {
        internal const string uieCoreModule = "UnityEngine.UIElementsModule";

        private static Dictionary<string, List<IBaseUxmlObjectFactory>> s_Factories;

        internal static Dictionary<string, List<IBaseUxmlObjectFactory>> factories
        {
            get
            {
                if (s_Factories == null)
                {
                    s_Factories = new Dictionary<string, List<IBaseUxmlObjectFactory>>();
                    RegisterEngineFactories();
                    RegisterUserFactories();
                }

                return s_Factories;
            }
        }

        protected static void RegisterFactory(IBaseUxmlObjectFactory factory)
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
                factoryList = new List<IBaseUxmlObjectFactory> { factory };
                s_Factories.Add(factory.uxmlQualifiedName, factoryList);
            }
        }

        // Core UI Toolkit uxml objects must be registered manually for both Editor and Player use cases.
        // For performance in the Player we want to avoid scanning any builtin Unity assembly with reflection.
        // Ideally a mechanism similar to the TypeCache in the Player would exist and remove the need for this.
        static void RegisterEngineFactories()
        {
            IBaseUxmlObjectFactory[] objectFactories =
            {
                new Columns.UxmlObjectFactory(),
                new Column.UxmlObjectFactory(),
                new SortColumnDescriptions.UxmlObjectFactory(),
                new SortColumnDescription.UxmlObjectFactory(),
            };

            foreach (var factory in objectFactories)
            {
                RegisterFactory(factory);
            }
        }

        static void RegisterUserFactories()
        {
            // In the Player, we filter assemblies to only introspect types of user assemblies
            // which will exclude Unity builtin assemblies (i.e. runtime modules).
        }
    }
}
