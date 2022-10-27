// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    [AttributeUsage(AttributeTargets.Field)]
    class InspectorUseSetterMethodAttribute : Attribute
    {
        /// <summary>
        /// The name of the method to use.
        /// </summary>
        public string MethodName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InspectorUseSetterMethodAttribute"/> class.
        /// </summary>
        /// <param name="methodName">The name of the method to use.</param>
        public InspectorUseSetterMethodAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}
