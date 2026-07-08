// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Marks a static method as the default configurator for a control.
/// </summary>
/// <remarks>
/// When an element is created through the UI Library or through menu items, the tagged method is invoked so the control
/// can be configured properly instead of being created "naked".
/// For example a Button can set its <c>text</c>, a field can set its <c>label</c>, or a container can
/// add default children.
///
/// The tagged method must have the following signature: static void(<see cref="ElementConfigurationContext"/>).
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
sealed class UILibraryDefaultConfigurationAttribute : Attribute
{
    /// <summary>
    /// The control type this method configures. Must be assignable to <see cref="VisualElement"/>.
    /// </summary>
    public Type targetType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UILibraryDefaultConfigurationAttribute"/> class.
    /// </summary>
    /// <param name="targetType">The control type this method configures.</param>
    public UILibraryDefaultConfigurationAttribute(Type targetType)
    {
        this.targetType = targetType;
    }
}

/// <summary>
/// Marks a static method as a named variant configurator for a control.
/// </summary>
/// <remarks>
/// A variant is an alternate default configuration for a control. Each variant surfaces as an additional
/// entry in the UI Library, with its name appended to the control's name (for example "Button (Big)").
/// A control can declare several variants, each with a distinct <see cref="variantName"/>, either by tagging
/// multiple methods.
///
/// The tagged method must have the following signature: static void(<see cref="ElementConfigurationContext"/>).
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
sealed class UILibraryVariantConfigurationAttribute : Attribute
{
    /// <summary>
    /// The control type this method configures. Must be assignable to <see cref="VisualElement"/>.
    /// </summary>
    public Type targetType { get; }

    /// <summary>
    /// The distinct name of the variant, appended to the control name in the UI Library.
    /// </summary>
    public string variantName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UILibraryVariantConfigurationAttribute"/> class.
    /// </summary>
    /// <param name="targetType">The control type this method configures.</param>
    /// <param name="variantName">The distinct name of the variant.</param>
    public UILibraryVariantConfigurationAttribute(Type targetType, string variantName)
    {
        this.targetType = targetType;
        this.variantName = variantName;
    }
}
