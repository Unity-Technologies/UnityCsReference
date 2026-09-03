// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Declares that a component needs another component type on the same element. When the declaring
    /// component is added to an element, from code or from UXML, the required components are added
    /// first, in declaration order. A required component that is already attached keeps its values.
    /// A required component that is missing is added with its default values.
    /// </summary>
    /// <remarks>
    /// The required type must be a struct decorated with <see cref="VisualElementComponentAttribute"/>.
    /// Requirements must not form a cycle: a component cannot require itself, directly or through
    /// other components. Both cases are compile-time errors. Removing a component that another
    /// attached component still requires is allowed; the editor logs a warning, player builds do
    /// no check.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
    public sealed class RequiresComponentOfTypeAttribute : Attribute
    {
        /// <summary>The component type that must exist on the same element.</summary>
        public Type ComponentType { get; }

        /// <summary>Initializes the attribute with the required component type.</summary>
        /// <param name="componentType">A struct decorated with <see cref="VisualElementComponentAttribute"/>.</param>
        public RequiresComponentOfTypeAttribute(Type componentType) { ComponentType = componentType; }
    }
}
