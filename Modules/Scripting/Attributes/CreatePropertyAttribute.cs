// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace Unity.Properties
{
    /// <summary>
    /// Use this attribute to have a property generated for the member.
    /// </summary>
    /// <remarks>
    /// By default public fields will have properties generated.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    [RequireAttributeUsages]
    public class CreatePropertyAttribute : RequiredMemberAttribute
    {
        /// <summary>
        /// Indicates if the property should generate a setter.
        /// </summary>
        /// <remarks>
        /// Setting this to <see langword="false"/> will not generate a setter for a readonly field or
        /// a get only property.
        /// </remarks>
        public bool ReadOnly { get; set; } = false;
    }

    /// <summary>
    /// Use this attribute to prevent have a property from being automatically generated on a public field.
    /// </summary>
    /// <see cref="CreatePropertyAttribute"/>
    [AttributeUsage(AttributeTargets.Field)]
    public class DontCreatePropertyAttribute : Attribute
    {

    }
}
