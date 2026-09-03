// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The generated view for a <see cref="Condition"/> in the transition inspector.
    /// </summary>
    /// <remarks>
    /// Access it through <see cref="ConditionView{T}.View"/> and add custom UI to <see cref="Root"/>.
    /// </remarks>
    public interface IConditionView
    {
        /// <summary>
        /// The container element that hosts the condition's fields.
        /// </summary>
        /// <remarks>
        /// It contains the built-in condition UI — the value field, plus a title label and a comparison operator
        /// dropdown when the condition overrides <see cref="Condition{T}.DisplayComparisonDropdown"/> — and any custom
        /// UI added by a <see cref="ConditionView{T}"/>.
        /// </remarks>
        public VisualElement Root { get; }
    }
}
