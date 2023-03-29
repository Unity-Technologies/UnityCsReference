// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// <see cref="VariableDeclarationModel"/> extension methods.
    /// </summary>
    static class VariableDeclarationModelExtensions
    {
        /// <summary>
        /// Indicates whether a <see cref="VariableDeclarationModel"/> requires initialization.
        /// </summary>
        /// <param name="self">The variable declaration model to query.</param>
        /// <returns>True if the variable declaration model requires initialization, false otherwise.</returns>
        public static bool RequiresInitialization(this VariableDeclarationModel self)
        {
            if (self == null)
                return false;

            Type dataType = TypeHandleHelpers.ResolveType_Internal(self.DataType);

            return dataType.IsValueType || dataType == typeof(string);
        }

        /// <summary>
        /// Check if the variable declaration model is an input or output.
        /// </summary>
        /// <param name="self">The variable declaration model to query.</param>
        /// <returns>True if the variable declaration model is an input or an output, false otherwise .</returns>
        public static bool IsInputOrOutput(this VariableDeclarationModel self) => self.IsOutput() || self.IsInput();

        /// <summary>
        /// Check if the variable declaration model is an input or output trigger.
        /// </summary>
        /// <param name="self">The variable declaration model to query.</param>
        /// <returns>True if the variable declaration model is an input or an output trigger, false otherwise .</returns>
        public static bool IsInputOrOutputTrigger(this VariableDeclarationModel self)
        {
            return IsInputOrOutput(self) && self.DataType == TypeHandle.ExecutionFlow;
        }

        static bool IsOutput(this VariableDeclarationModel self) => self.Modifiers == ModifierFlags.Write;
        static bool IsInput(this VariableDeclarationModel self) => self.Modifiers == ModifierFlags.Read;
    }
}
