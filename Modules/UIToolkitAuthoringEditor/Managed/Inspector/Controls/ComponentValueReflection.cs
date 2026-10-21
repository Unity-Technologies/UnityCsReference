// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Reads a live component as a boxed copy when the component type is only known at runtime
/// (a cold path, once per selection / sync). The generic method is closed with
/// <c>MakeGenericMethod</c>; it must return by value because Mono cannot reflection-invoke the
/// ref-returning <see cref="VisualElement.GetComponent{T}"/>.
/// </summary>
static class ComponentValueReflection
{
    [NoAutoStaticsCleanup]
    internal static readonly MethodInfo GetComponentValueMethod =
        typeof(ComponentValueReflection).GetMethod(nameof(GetComponentValue), BindingFlags.Static | BindingFlags.NonPublic);

    [NoAutoStaticsCleanup]
    static readonly MethodInfo k_HasComponentMethod = typeof(VisualElement).GetMethod(nameof(VisualElement.HasComponent));

    /// <summary>
    /// Indicates whether the component of the specified type is attached to the element.
    /// </summary>
    internal static bool HasComponent(VisualElement element, Type componentType)
    {
        if (element == null || componentType == null)
            return false;

        return (bool)k_HasComponentMethod.MakeGenericMethod(componentType).Invoke(element, null);
    }

    [NoAutoStaticsCleanup]
    internal static readonly MethodInfo SetComponentValueMethod =
        typeof(ComponentValueReflection).GetMethod(nameof(SetComponentValue), BindingFlags.Static | BindingFlags.NonPublic);

    [NoAutoStaticsCleanup]
    internal static readonly MethodInfo CreateComponentValueMethod =
        typeof(ComponentValueReflection).GetMethod(nameof(CreateComponentValue), BindingFlags.Static | BindingFlags.NonPublic);

    [NoAutoStaticsCleanup]
    internal static readonly MethodInfo MarkComponentDirtyMethod =
        typeof(ComponentValueReflection).GetMethod(nameof(MarkComponentDirty), BindingFlags.Static | BindingFlags.NonPublic);

    static T GetComponentValue<T>(VisualElement element) where T : struct, IVisualElementComponent
        => element.GetComponent<T>();

    static void SetComponentValue<T>(VisualElement element, T value) where T : struct, IVisualElementComponent
        => element.GetComponent<T>() = value;

    // Same construction GetOrAddComponent uses, so a [UxmlCreateInstanceMethod] factory is honored.
    static T CreateComponentValue<T>() where T : struct, IVisualElementComponent
    {
        return default(T).__GetComponentCreateInstanceFactory() is Func<T> factory ? factory() : default;
    }

    static void MarkComponentDirty<T>(VisualElement element) where T : struct, IVisualElementComponent
        => element.MarkComponentDirty<T>();
}
