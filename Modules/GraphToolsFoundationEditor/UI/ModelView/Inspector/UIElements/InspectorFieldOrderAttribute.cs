// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Field attribute to control the order of fields in the inspector.
    /// Fields without InspectorFieldOrderAttribute will always be displayed first.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    class InspectorFieldOrderAttribute : PropertyAttribute
    {
        public int Order { get; }

        /// <summary>
        /// Create an instance of the <see cref="InspectorFieldOrderAttribute"/> class.
        /// </summary>
        /// <param name="order">The order in the inspector.</param>
        public InspectorFieldOrderAttribute(int order = 0)
        {
            Assert.IsTrue(order >= 0, "The order can't be a negative number");
            Order = order;
        }
    }
}
