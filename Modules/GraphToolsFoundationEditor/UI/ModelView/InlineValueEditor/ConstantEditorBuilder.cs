// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Holds information needed when building a constant editor.
    /// </summary>
    class ConstantEditorBuilder
    {
        /// <summary>
        /// The command dispatcher.
        /// </summary>
        public RootView CommandTarget { get; }

        /// <summary>
        /// Whether the constant is locked.
        /// </summary>
        public bool ConstantIsLocked { get; }

        /// <summary>
        /// The graph element model that owns the constant, if any.
        /// </summary>
        public IEnumerable<GraphElementModel> ConstantOwners { get; }

        /// <summary>
        /// The label to display in front of the field.
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantEditorBuilder"/> class.
        /// </summary>
        /// <param name="commandTarget">The view used to dispatch commands.</param>
        /// <param name="constantIsLocked">Whether the constant is locked.</param>
        /// <param name="constantOwners">The graph element models that owns the constant, if any.</param>
        /// <param name="label">The label to display in front of the field.</param>
        public ConstantEditorBuilder(RootView commandTarget,
            bool constantIsLocked, IEnumerable<GraphElementModel> constantOwners, string label)
        {
            CommandTarget = commandTarget;
            ConstantIsLocked = constantIsLocked;
            ConstantOwners = constantOwners;
            Label = label;
        }

        /// <summary>
        /// Filters candidate methods for the one that satisfy the signature VisualElement MyFunctionName(IConstantEditorBuilder builder, ...).
        /// </summary>
        /// <remarks>For use with <see cref="ExtensionMethodCache{IConstantEditorBuilder}.GetExtensionMethod"/>.</remarks>
        /// <param name="method">The method.</param>
        /// <returns>True if the method satisfies the signature, false otherwise.</returns>
        public static bool FilterMethods(MethodInfo method)
        {
            // Looking for methods like : BaseModelPropertyField MyFunctionName(IConstantEditorBuilder builder, <NodeTypeToBuild> node)
            var parameters = method.GetParameters();
            return method.ReturnType == typeof(BaseModelPropertyField)
                && parameters.Length == 2
                && parameters[0].ParameterType == typeof(ConstantEditorBuilder);
        }

        /// <summary>
        /// Selects the second parameter of the extension method as a key.
        /// </summary>
        /// <remarks>For use with <see cref="ExtensionMethodCache{IConstantEditorBuilder}.GetExtensionMethod"/>.</remarks>
        /// <param name="method">The method.</param>
        /// <returns>The second parameter of the method.</returns>
        public static Type KeySelector(MethodInfo method)
        {
            return method.GetParameters()[1].ParameterType;
        }
    }
}
