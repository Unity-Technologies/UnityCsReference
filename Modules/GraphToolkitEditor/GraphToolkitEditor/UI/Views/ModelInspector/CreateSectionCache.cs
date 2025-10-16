// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.GraphToolkit.Editor
{
    static class CreateSectionCache
    {
        public static MultipleModelsView CallCreateSection(ElementBuilder eb, Model model)
        {
            Type rootViewType = eb.View?.GetType();

            if (s_SingleFunctions == null || !CanFindType(rootViewType, s_SingleFunctions))
            {
                BuildCache(rootViewType);
            }

            Type currentType = model.GetType();
            while (currentType != typeof(object) && currentType != null)
            {
                if (s_SingleFunctions.TryGetValue((rootViewType, currentType), out var func) && func != null)
                    return func(eb, model);
                foreach (var interfaceType in currentType.GetDirectInterfaces())
                {
                    if (s_SingleFunctions.TryGetValue((rootViewType, interfaceType), out var funcInterface) && funcInterface != null)
                        return funcInterface(eb, model);
                }

                currentType = currentType.BaseType;
            }

            return null;
        }

        static bool CanFindType<T>(Type rootViewType, Dictionary<(Type, Type), T> dictionary)
        {
            foreach (var key in dictionary.Keys)
            {
                if (key.Item1 == rootViewType)
                {
                    return true;
                }
            }

            return false;
        }

        public static MultipleModelsView CallCreateSection(ElementBuilder eb, IReadOnlyList<Model> models)
        {
            if (models.Count == 0)
                return null;

            Type rootViewType = eb.View?.GetType();

            if (s_EnumFunctions == null || !CanFindType(rootViewType, s_EnumFunctions))
            {
                BuildCache(rootViewType);
            }

            if (models.Count == 1)
            {
                MultipleModelsView singleResult = CallCreateSection(eb, models[0]);
                if (singleResult != null)
                    return singleResult;
            }

            Type currentType = ModelHelpers.GetCommonBaseType(models);
            while (currentType != typeof(object) && currentType != null)
            {
                if (s_EnumFunctions.TryGetValue((rootViewType, currentType), out var func) && func != null)
                    return func(eb, models);
                foreach (var interfaceType in currentType.GetDirectInterfaces())
                {
                    if (s_EnumFunctions.TryGetValue((rootViewType, interfaceType), out var funcInterface) && funcInterface != null)
                        return funcInterface(eb, models);
                }

                currentType = currentType.BaseType;
            }

            return null;
        }

        static void BuildCache(Type viewType)
        {
            var thisAssembly = typeof(GraphElement).Assembly;

            s_SingleFunctions ??= new Dictionary<(Type, Type), Func<ElementBuilder, Model, MultipleModelsView>>();
            s_EnumFunctions ??= new Dictionary<(Type, Type), Func<ElementBuilder, IReadOnlyList<Model>, MultipleModelsView>>();

            var typesWithAttribute = TypeCache.GetTypesWithAttribute<ModelInspectorCreateSectionMethodsCacheAttribute>();

            var sortedMatchingTypes = new SortedDictionary<int, Type>();

            foreach (var matchingType in typesWithAttribute)
            {
                var attr = matchingType.GetCustomAttribute<ModelInspectorCreateSectionMethodsCacheAttribute>();
                if (!attr.ViewDomain.IsAssignableFrom(viewType))
                    continue;
                sortedMatchingTypes.Add(-attr.Priority, matchingType); // minus because we want the reverse order.
            }

            foreach (var type in sortedMatchingTypes.Values)
            {
                var meths = type.GetMethods(BindingFlags.Static | BindingFlags.Public);

                foreach (var meth in meths)
                {
                    if (meth.ReturnType == typeof(MultipleModelsView) &&
                        meth.GetParameters().Length == 2 &&
                        meth.GetParameters()[0].ParameterType == typeof(ElementBuilder) &&
                        (typeof(Model).IsAssignableFrom(meth.GetParameters()[1].ParameterType) || meth.GetParameters()[1].ParameterType.IsInterface))
                    {
                        if (meth.DeclaringType?.Assembly != thisAssembly && viewType.Assembly == thisAssembly && meth.GetParameters()[1].ParameterType.Assembly == thisAssembly)
                            Debug.LogWarning($"ModelInspectorCreateSectionMethodsCache method {meth.DeclaringType?.Name ?? "null"}.{meth.Name} is declared for both a view type and a model from {thisAssembly}. This will override the behaviour in other tools as well");

                        s_SingleFunctions[(viewType, meth.GetParameters()[1].ParameterType)] = (eb, model) => (MultipleModelsView)meth.Invoke(null, new object[] { eb, model });
                    }

                }

                foreach (var meth in meths)
                {
                    if (meth.ReturnType != typeof(MultipleModelsView) ||
                        meth.GetParameters().Length != 2 ||
                        meth.GetParameters()[0].ParameterType != typeof(ElementBuilder) ||
                        !(meth.GetParameters()[1].ParameterType.IsGenericType && meth.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
                        )
                        continue;

                    var modelType = meth.GetParameters()[1].ParameterType.GenericTypeArguments[0];

                    if (!typeof(Model).IsAssignableFrom(modelType) && !modelType.IsInterface)
                        continue;

                    if (!s_EnumFunctions.ContainsKey((viewType, modelType)))
                    {
                        var castMethod = typeof(CollectionExtensions).GetMethod(nameof(CollectionExtensions.Cast), BindingFlags.Public | BindingFlags.Static)?.MakeGenericMethod(new[] { typeof(Model), modelType });

                        Assert.IsNotNull(castMethod, "Cast Method for IReadOnlyList<" + typeof(Model).FullName + ", " + modelType.FullName + ">  not found");

                        s_EnumFunctions[(viewType, modelType)] = (eb, models) => (MultipleModelsView)meth.Invoke(null, new[] { eb, castMethod.Invoke(null, new object[] { models }) });
                    }
                }
            }
        }

        static Dictionary<(Type, Type), Func<ElementBuilder, IReadOnlyList<Model>, MultipleModelsView>> s_EnumFunctions;
        static Dictionary<(Type, Type), Func<ElementBuilder, Model, MultipleModelsView>> s_SingleFunctions;
    }
}
