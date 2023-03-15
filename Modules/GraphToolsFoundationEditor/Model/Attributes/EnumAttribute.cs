// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Attribute used to emulate an enum.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    class EnumAttribute : Attribute
    {
        /// <summary>
        /// The values of the enum.
        /// </summary>
        public readonly string[] Values;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumAttribute"/> class.
        /// </summary>
        public EnumAttribute(string[] values)
        {
            Values = values;
        }
    }
}
