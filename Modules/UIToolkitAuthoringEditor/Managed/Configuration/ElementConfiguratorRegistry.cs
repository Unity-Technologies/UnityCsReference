// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Scans for configurator methods tagged with <see cref="UILibraryDefaultConfigurationAttribute"/> or
/// <see cref="UILibraryVariantConfigurationAttribute"/> and invokes the matching
/// configurator (through an <see cref="ElementConfigurationContext"/>) when an element is added.
/// </summary>
static partial class ElementConfiguratorRegistry
{
    static readonly Type[] k_ExpectedParameters = { typeof(ElementConfigurationContext) };

    readonly struct DefaultConfigurator(string source, Action<ElementConfigurationContext> configure)
    {
        // Stable, human-readable identifier. This can be used to provide a resolution when multiple
        // "default" configurations are registered.
        public readonly string source = source;
        public readonly Action<ElementConfigurationContext> configure = configure;
    }

    readonly struct Variant(string name, Action<ElementConfigurationContext> configure)
    {
        public readonly string name = name;
        public readonly Action<ElementConfigurationContext> configure = configure;
    }

    [AutoStaticsCleanup]
    static Dictionary<Type, List<DefaultConfigurator>> s_DefaultConfigurators;
    [AutoStaticsCleanup]
    static Dictionary<Type, List<Variant>> s_AttributeVariants;

    static void EnsureInitialized()
    {
        if (s_DefaultConfigurators != null)
            return;

        s_DefaultConfigurators = new Dictionary<Type, List<DefaultConfigurator>>();
        s_AttributeVariants = new Dictionary<Type, List<Variant>>();

        foreach (var method in TypeCache.GetMethodsWithAttribute<UILibraryDefaultConfigurationAttribute>())
        {
            var attribute = method.GetCustomAttribute<UILibraryDefaultConfigurationAttribute>();
            if (attribute != null && IsValidConfigurator(method, attribute.targetType, nameof(UILibraryDefaultConfigurationAttribute)))
                AddDefault(attribute.targetType, GetStableIdentifier(method), WrapMethod(method));
        }

        foreach (var method in TypeCache.GetMethodsWithAttribute<UILibraryVariantConfigurationAttribute>())
        {
            var attribute = method.GetCustomAttribute<UILibraryVariantConfigurationAttribute>();
            if (attribute != null && IsValidConfigurator(method, attribute.targetType, nameof(UILibraryVariantConfigurationAttribute)))
                AddVariant(attribute.targetType, attribute.variantName, WrapMethod(method), $"'{GetStableIdentifier(method)}'");
        }
    }

    static string GetStableIdentifier(MethodInfo method) => $"{method.DeclaringType?.FullName}.{method.Name}";

    static Action<ElementConfigurationContext> WrapMethod(MethodInfo method)
        => (Action<ElementConfigurationContext>)Delegate.CreateDelegate(typeof(Action<ElementConfigurationContext>), method);

    static void AddDefault(Type elementType, string source, Action<ElementConfigurationContext> configure)
    {
        if (!s_DefaultConfigurators.TryGetValue(elementType, out var configurators))
        {
            configurators = new List<DefaultConfigurator>();
            s_DefaultConfigurators.Add(elementType, configurators);
        }

        // Multiple defaults for the same control are allowed; the conflict is resolved in Project Settings.
        configurators.Add(new DefaultConfigurator(source, configure));
    }

    static bool AddVariant(Type elementType, string variantName,
        Action<ElementConfigurationContext> configure, string source)
    {
        if (string.IsNullOrEmpty(variantName))
        {
            Debug.LogError($"A variant for {source} must specify a non-empty variant name.");
            return false;
        }

        if (HasVariant(elementType, variantName))
        {
            Debug.LogError($"Variant '{variantName}' is declared more than once for '{elementType.FullName}'. Variant names must be distinct per control.");
            return false;
        }

        if (!s_AttributeVariants.TryGetValue(elementType, out var variants))
        {
            variants = new List<Variant>();
            s_AttributeVariants.Add(elementType, variants);
        }

        variants.Add(new Variant(variantName, configure));
        return true;
    }

    static bool HasVariant(Type elementType, string variantName)
    {
        return s_AttributeVariants.TryGetValue(elementType, out var variants) && IndexOf(variants, variantName) >= 0;
    }

    static int IndexOf(List<Variant> variants, string variantName)
    {
        for (var i = 0; i < variants.Count; i++)
        {
            if (string.Equals(variants[i].name, variantName, StringComparison.Ordinal))
                return i;
        }
        return -1;
    }

    static bool IsValidConfigurator(MethodInfo method, Type targetType, string attributeName)
    {
        if (targetType == null || !typeof(VisualElement).IsAssignableFrom(targetType))
        {
            Debug.LogError($"[{attributeName}] on '{method.DeclaringType?.FullName}.{method.Name}' must target a type assignable to {nameof(VisualElement)}.");
            return false;
        }

        if (!method.IsStatic || method.ReturnType != typeof(void) || !HasExpectedParameters(method))
        {
            Debug.LogError($"[{attributeName}] method '{method.DeclaringType?.FullName}.{method.Name}' must be a static method with the signature void({nameof(ElementConfigurationContext)}).");
            return false;
        }

        return true;
    }

    static bool HasExpectedParameters(MethodInfo method)
    {
        var parameters = method.GetParameters();
        if (parameters.Length != k_ExpectedParameters.Length)
            return false;

        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].ParameterType != k_ExpectedParameters[i])
                return false;
        }

        return true;
    }

    public static bool HasConfigurator(Type type, string variantName = null)
    {
        return TryGetConfigurator(type, variantName, out _);
    }

    public static IReadOnlyList<string> GetVariantNames(Type type)
    {
        EnsureInitialized();

        if (type == null || !s_AttributeVariants.TryGetValue(type, out var attributeVariants))
            return Array.Empty<string>();

        var result = new string[attributeVariants.Count];
        for (var i = 0; i < attributeVariants.Count; i++)
            result[i] = attributeVariants[i].name;
        return result;
    }

    public static bool Configure(Type type, string variantName, VisualTreeAsset visualTreeAsset, VisualElementAsset elementAsset)
    {
        if (!TryGetConfigurator(type, variantName, out var configure))
            return false;

        try
        {
            configure(new ElementConfigurationContext(visualTreeAsset, elementAsset));
        }
        catch (TargetInvocationException e)
        {
            Debug.LogException(e.InnerException ?? e);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        return true;
    }

    static bool TryGetConfigurator(Type type, string variantName, out Action<ElementConfigurationContext> configure)
    {
        EnsureInitialized();
        configure = null;

        if (type == null)
            return false;

        if (string.IsNullOrEmpty(variantName))
            return TryResolveDefault(type, out configure);

        if (s_AttributeVariants.TryGetValue(type, out var attributeVariants))
        {
            var index = IndexOf(attributeVariants, variantName);
            if (index >= 0)
            {
                configure = attributeVariants[index].configure;
                return true;
            }
        }
        return false;
    }

    static bool TryResolveDefault(Type type, out Action<ElementConfigurationContext> configure)
    {
        configure = null;

        if (!s_DefaultConfigurators.TryGetValue(type, out var configurators) || configurators.Count == 0)
            return false;

        if (configurators.Count == 1)
        {
            configure = configurators[0].configure;
            return true;
        }

        // Several default configurations target this type, pick the ordinal-first source so the resolution is deterministic.
        // Eventually, we can add a preference settings to resolve the conflict.
        var best = configurators[0];
        for (var i = 0; i < configurators.Count; i++)
        {
            var candidate = configurators[i];
            if (string.CompareOrdinal(candidate.source, best.source) < 0)
                best = candidate;
        }

        configure = best.configure;
        return true;
    }
}
