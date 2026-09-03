// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Restricts a <see cref="VisualElementComponentAttribute"/> component to a specific <see cref="VisualElement"/>
    /// subclass (or any class derived from it). When present, every <c>[RegisterCallback]</c> static method
    /// on the component may declare its <c>owner</c> parameter as the constrained type instead of plain
    /// <see cref="VisualElement"/>, and the component picker filters by the constraint.
    /// </summary>
    /// <remarks>
    /// The constraint is validated cheapest-first: a Roslyn analyzer on statically-known
    /// <c>AddComponent&lt;T&gt;()</c> calls, generator validation of handler signatures, an
    /// <c>AddComponent&lt;T&gt;()</c> runtime check, and a UXML import check. A struct may carry at most one
    /// <see cref="RequiresElementOfTypeAttribute"/>, and <see cref="ElementType"/> must be
    /// <see cref="VisualElement"/> or a subclass (anything else, including interfaces, is a compile-time
    /// error, UITKSG034).
    /// </remarks>
    [AttributeUsage(AttributeTargets.Struct, Inherited = false)]
    public sealed class RequiresElementOfTypeAttribute : Attribute
    {
        /// <summary>The <see cref="VisualElement"/> subclass a component of this type may attach to.</summary>
        public Type ElementType { get; }

        /// <summary>Initializes the attribute with the required owner element type.</summary>
        /// <param name="elementType">A <see cref="VisualElement"/> subclass.</param>
        public RequiresElementOfTypeAttribute(Type elementType) { ElementType = elementType; }
    }
}
