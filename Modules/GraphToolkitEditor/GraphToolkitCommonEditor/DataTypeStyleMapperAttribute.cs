// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Attribute to specify which graph types a <see cref="DataTypeStyleMapper"/> applies to.
    /// </summary>
    /// Must be applied to classes derived from <see cref="DataTypeStyleMapper"/>.
    /// When applied to a <see cref="DataTypeStyleMapper"/>, the styles registered within will only apply to the specified graph types.
    /// When the attribute is not present, the styles apply to all graph types.
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DataTypeStyleMapperAttribute : Attribute
    {
        internal Type[] GraphTypes { get; }

        /// <summary>
        /// Instantiates the attribute with the specified graph types.
        /// </summary>
        /// <param name="graphType"></param>
        public DataTypeStyleMapperAttribute(params Type[] graphType)
        {
            GraphTypes = graphType;
        }
    }
}
