// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Entities
{
    /// <summary>
    /// Attribute to mark a compiler generated method for method body copying.
    /// Intended only for use by internal Unity codegen.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class DOTSCompilerPatchedMethodAttribute : Attribute
    {
        /// <summary>
        /// Constructor for <see cref="DOTSCompilerPatchedMethodAttribute"/>.
        /// </summary>
        /// <param name="targetMethodName">Original method to copy method body into.</param>
        public DOTSCompilerPatchedMethodAttribute(string targetMethodName) { }
    }

    /// <summary>
    /// Attribute to mark a compiler generated method for property body copying.
    /// Intended only for use by internal Unity codegen.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DOTSCompilerPatchedPropertyAttribute : Attribute
    {
        /// <summary>
        /// Constructor for <see cref="DOTSCompilerPatchedMethodAttribute"/>.
        /// </summary>
        /// <param name="targetPropertyName">Original property to copy property body into.</param>
        public DOTSCompilerPatchedPropertyAttribute(string targetPropertyName) { }
    }
}
