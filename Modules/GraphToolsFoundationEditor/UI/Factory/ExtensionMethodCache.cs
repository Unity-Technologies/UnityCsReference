// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Use this attribute to tag classes containing static extension methods you want to cache in
    /// an <see cref="ExtensionMethodCache{TExtendedType}"/>.
    /// </summary>
    [MeansImplicitUse(ImplicitUseTargetFlags.WithMembers)]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    class GraphElementsExtensionMethodsCacheAttribute : Attribute
    {
        internal const int lowestPriority_Internal = 0;

        /// <summary>
        /// Default extension method priority for methods provided by tools.
        /// </summary>
        public const int toolDefaultPriority = 1000;

        /// <summary>
        /// The type of view to which the extension methods apply.
        /// </summary>
        public Type ViewDomain { get; }

        /// <summary>
        /// The priority of the extension methods.
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphElementsExtensionMethodsCacheAttribute"/> class.
        /// </summary>
        /// <param name="viewDomain">The type of view to which the extension methods apply.</param>
        /// <param name="priority">The priority of the extension methods.</param>
        public GraphElementsExtensionMethodsCacheAttribute(Type viewDomain, int priority = toolDefaultPriority)
        {
            Assert.IsTrue(typeof(RootView).IsAssignableFrom(viewDomain));
            Assert.IsFalse(viewDomain.IsInterface);

            ViewDomain = viewDomain;
            Priority = priority;
        }
    }

    enum ExtensionMethodCacheVisitMode_Internal
    {
        OnlyClassesWithAttribute,
        EveryMethod
    }

    /// <summary>
    /// A cache that holds extension methods on <typeparamref name="TExtendedType"/>.
    /// </summary>
    /// <typeparam name="TExtendedType">The type extended by the extension methods.</typeparam>
    static class ExtensionMethodCache<TExtendedType>
    {
        // ReSharper disable once StaticMemberInGenericType
        static Dictionary<(Type, Type), MethodInfo> s_FactoryMethodCache;

        // ReSharper disable once StaticMemberInGenericType
        static Queue<Type> s_CandidateTypes = new Queue<Type>();

        /// <summary>
        /// Gets an extension method that satisfies the constraints given by the parameters.
        /// </summary>
        /// <param name="viewDomain">The type of <see cref="RootView"/> the extension method should apply to.</param>
        /// <param name="targetType">The type of model the extension method should apply to. The type tree is used to find the most appropriate method.</param>
        /// <param name="filterMethods">A filter function for the methods.</param>
        /// <param name="keySelector">A method that selects a parameter of the extension method to use as a key by the <paramref name="filterMethods"/>.</param>
        /// <returns>A method that satisfies the constraints given by the parameters.</returns>
        public static MethodInfo GetExtensionMethod(
            Type viewDomain,
            Type targetType,
            Func<MethodInfo, bool> filterMethods,
            Func<MethodInfo, Type> keySelector)
        {
            Assert.AreEqual(0, s_CandidateTypes.Count);

            MethodInfo extension = null;
            var currentDomain = viewDomain;

            while (extension == null && currentDomain != null)
            {
                s_CandidateTypes.Enqueue(targetType);
                do
                {
                    extension = GetExtensionMethodOf(currentDomain, s_CandidateTypes, filterMethods, keySelector);
                }
                while (extension == null && s_CandidateTypes.Any());

                currentDomain = currentDomain.BaseType;
            }

            var key = (viewDomain, targetType);
            if (!s_FactoryMethodCache.ContainsKey(key))
                s_FactoryMethodCache[key] = extension;

            s_CandidateTypes.Clear();
            return extension;
        }

        static MethodInfo GetExtensionMethodOf(
            Type viewDomain,
            Queue<Type> candidateTypes,
            Func<MethodInfo, bool> filterMethods,
            Func<MethodInfo, Type> keySelector)
        {
            if (candidateTypes == null || !candidateTypes.Any())
                return null;

            var targetType = candidateTypes.Dequeue();

            if (targetType == typeof(ScriptableObject))
                return null;

            s_FactoryMethodCache ??= BuildFactoryMethodCache_Internal(filterMethods, keySelector);

            var key = (viewDomain, targetType);
            if (s_FactoryMethodCache.ContainsKey(key))
                return s_FactoryMethodCache[key];

            if (!targetType.IsInterface)
            {
                foreach (var type in GetInterfaces(targetType))
                {
                    candidateTypes.Enqueue(type);
                }

                if (targetType.BaseType != null)
                {
                    candidateTypes.Enqueue(targetType.BaseType);
                }
            }
            if( targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var realTargetType = targetType.GenericTypeArguments[0];
                if (!realTargetType.IsInterface)
                {
                    foreach (var type in GetInterfaces(realTargetType))
                    {
                        candidateTypes.Enqueue(typeof(IEnumerable<>).MakeGenericType(type));
                    }
                }

                if (realTargetType.BaseType != null)
                {
                    candidateTypes.Enqueue(typeof(IEnumerable<>).MakeGenericType(realTargetType.BaseType));
                }
            }

            return null;
        }

        internal static Dictionary<(Type, Type), MethodInfo> BuildFactoryMethodCache_Internal(
            Func<MethodInfo, bool> filterMethods,
            Func<MethodInfo, Type> keySelector,
            ExtensionMethodCacheVisitMode_Internal mode = ExtensionMethodCacheVisitMode_Internal.OnlyClassesWithAttribute)
        {
            return mode == ExtensionMethodCacheVisitMode_Internal.OnlyClassesWithAttribute ?
                // only goes through methods inside a class with GraphElementsExtensionMethodsCacheAttribute
                BuildFactoryMethodCache<GraphElementsExtensionMethodsCacheAttribute>(filterMethods, keySelector) :
                // goes through every method. Super slow. Kept for test purposes.
                BuildFactoryMethodCache<ExtensionAttribute>(filterMethods, keySelector);
        }

        static Dictionary<(Type, Type), MethodInfo> BuildFactoryMethodCache<TAttribute>(
            Func<MethodInfo, bool> filterMethods,
            Func<MethodInfo, Type> keySelector)
            where TAttribute : Attribute
        {
            var factoryMethods = new Dictionary<(Type, Type), MethodInfo>();

            var assemblies = AssemblyCache_Internal.CachedAssemblies_Internal;
            var extensionMethods = AssemblyCache_Internal.GetExtensionMethods_Internal<TAttribute>(assemblies);
            Type extendedType = typeof(TExtendedType);
            if (extensionMethods.TryGetValue(extendedType, out var allMethodInfos))
            {
                foreach (var methodInfo in allMethodInfos.Where(filterMethods))
                {
                    if (methodInfo.DeclaringType == null)
                        continue;

                    var methodAttributes = methodInfo.DeclaringType.GetCustomAttributes<GraphElementsExtensionMethodsCacheAttribute>();

                    foreach (var methodAttr in methodAttributes)
                    {
                        var key = (methodAttr.ViewDomain, keySelector(methodInfo));
                        if (!factoryMethods.TryGetValue(key, out var currentValue))
                        {
                            factoryMethods[key] = methodInfo;
                        }
                        else
                        {
                            var cacheAttr = currentValue.DeclaringType?.GetCustomAttribute<GraphElementsExtensionMethodsCacheAttribute>();
                            int currentPriority = cacheAttr?.Priority ?? 0;

                            if (methodAttr.Priority == currentPriority)
                            {
                                Debug.LogError($"Duplicate extension methods for domain and type {key} have the same priority" +
                                    $"as a previously discovered extension method. It will be ignored." +
                                    $" Previous value: {currentValue}, new value: {methodInfo}, extended type: {extendedType.FullName}");
                            }
                            else if (methodAttr.Priority < currentPriority)
                            {
                                var gtfAssembly = typeof(GraphElementsExtensionMethodsCacheAttribute).Assembly;
                                var newMethodAssembly = methodInfo.DeclaringType?.Assembly;
                                if (newMethodAssembly != gtfAssembly)
                                {
                                    Debug.LogError($"Extension methods for domain and type {key} has lower priority than an" +
                                        $"extension method declared by GraphToolsFoundation. It will be ignored." +
                                        $" Previous value: {currentValue}, new value: {methodInfo}, extended type: {extendedType.FullName}");
                                }
                            }
                            else if (methodAttr.Priority > currentPriority)
                            {
                                factoryMethods[key] = methodInfo;
                            }
                        }
                    }
                }
            }

            return factoryMethods;
        }

        static IEnumerable<Type> GetInterfaces(Type type)
        {
            if (type.BaseType == null)
                return type.GetInterfaces();

            return type.GetInterfaces().Except(type.BaseType.GetInterfaces());
        }
    }
}
