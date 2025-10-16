// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Attribute to display a custom label for a field in the inspector.
    /// of the model inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    [UnityRestricted]
    internal class DisplayNameAttribute : Attribute
    {
        /// <summary>
        /// The displayed name of the field.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DisplayNameAttribute"/> class.
        /// </summary>
        /// <param name="displayName">The displayed name of the field.</param>
        public DisplayNameAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }
}
