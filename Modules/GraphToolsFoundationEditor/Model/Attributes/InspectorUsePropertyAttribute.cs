// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Attribute used to tell the model inspector to use a property to get and set the field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    class InspectorUsePropertyAttribute : Attribute
    {
        /// <summary>
        /// The name of the property to use.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InspectorUsePropertyAttribute"/> class.
        /// </summary>
        /// <param name="propertyName">The name of the property to use.</param>
        public InspectorUsePropertyAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }
    }
}
