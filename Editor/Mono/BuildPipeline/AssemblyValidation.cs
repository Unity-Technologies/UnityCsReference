// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Scripting.Compilers;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
internal class AssemblyValidationRule : Attribute
{
    public int Priority;

    private readonly RuntimePlatform _platform;

    public AssemblyValidationRule(RuntimePlatform platform)
    {
        _platform = platform;
        Priority = 0;
    }

    public RuntimePlatform Platform
    {
        get { return _platform; }
    }
}

internal struct ValidationResult
{
    public bool Success;
    public IValidationRule Rule;
    public IEnumerable<CompilerMessage> CompilerMessages;
}

internal interface IValidationRule
{
    ValidationResult Validate(IEnumerable<string> userAssemblies, params object[] options);
}

internal class AssemblyValidation
{
    private static Dictionary<RuntimePlatform, List<Type>> _rulesByPlatform;

    public static ValidationResult Validate(RuntimePlatform platform, IEnumerable<string> userAssemblies, params object[] options)
    {
        WarmUpRulesCache();

        var assemblies = userAssemblies as string[] ?? userAssemblies.ToArray();
        if (assemblies.Length != 0)
        {
            foreach (var validationRule in ValidationRulesFor(platform, options))
            {
                var result = validationRule.Validate(assemblies, options);
                if (!result.Success)
                    return result;
            }
        }

        return new ValidationResult { Success = true };
    }

    private static void WarmUpRulesCache()
    {
        if (_rulesByPlatform != null)
            return;

        _rulesByPlatform = new Dictionary<RuntimePlatform, List<Type>>();

        var assembly = typeof(AssemblyValidation).Assembly;
        foreach (var type in assembly.GetTypes().Where(IsValidationRule))
            RegisterValidationRule(type);
    }

    private static bool IsValidationRule(Type type)
    {
        return ValidationRuleAttributesFor(type).Any();
    }

    private static IEnumerable<IValidationRule> ValidationRulesFor(RuntimePlatform platform, params object[] options)
    {
        return ValidationRuleTypesFor(platform).Select(t => CreateValidationRuleWithOptions(t, options)).Where(v => v != null);
    }

    private static IEnumerable<Type> ValidationRuleTypesFor(RuntimePlatform platform)
    {
        if (!_rulesByPlatform.ContainsKey(platform))
            yield break;

        foreach (var validationType in _rulesByPlatform[platform])
            yield return validationType;
    }

    private static IValidationRule CreateValidationRuleWithOptions(Type type, params object[] options)
    {
        var constructorOptions = new List<object>(options);
        while (true)
        {
            var currentOptions = constructorOptions.ToArray();
            var constructor = ConstructorFor(type, currentOptions);
            if (constructor != null)
                return (IValidationRule)constructor.Invoke(currentOptions);

            if (constructorOptions.Count == 0)
                return null;

            constructorOptions.RemoveAt(constructorOptions.Count - 1);
        }
    }

    private static ConstructorInfo ConstructorFor(Type type, IEnumerable<object> options)
    {
        var constructorArguments = options.Select(o => o.GetType()).ToArray();
        return type.GetConstructor(constructorArguments);
    }

    internal static void RegisterValidationRule(Type type)
    {
        foreach (var attribute in ValidationRuleAttributesFor(type))
            RegisterValidationRuleForPlatform(attribute.Platform, type);
    }

    internal static void RegisterValidationRuleForPlatform(RuntimePlatform platform, Type type)
    {
        if (!_rulesByPlatform.ContainsKey(platform))
            _rulesByPlatform[platform] = new List<Type>();

        if (_rulesByPlatform[platform].IndexOf(type) == -1)
            _rulesByPlatform[platform].Add(type);

        _rulesByPlatform[platform].Sort((a, b) => CompareValidationRulesByPriority(a, b, platform));
    }

    internal static int CompareValidationRulesByPriority(Type a, Type b, RuntimePlatform platform)
    {
        var aPriority = PriorityFor(a, platform);
        var bPriority = PriorityFor(b, platform);

        if (aPriority == bPriority)
            return 0;

        return (aPriority < bPriority ? -1 : 1);
    }

    private static int PriorityFor(Type type, RuntimePlatform platform)
    {
        return
            ValidationRuleAttributesFor(type).
            Where(attr => attr.Platform == platform).
            Select(attr => attr.Priority).
            FirstOrDefault();
    }

    private static IEnumerable<AssemblyValidationRule> ValidationRuleAttributesFor(Type type)
    {
        return type.GetCustomAttributes(true).OfType<AssemblyValidationRule>();
    }
}
