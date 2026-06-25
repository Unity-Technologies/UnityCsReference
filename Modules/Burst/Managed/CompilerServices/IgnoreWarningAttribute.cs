// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Burst.CompilerServices
{
    /// <summary>
    /// Can be used to specify that a warning produced by Burst for a given
    /// method should be ignored.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class IgnoreWarningAttribute : Attribute
    {
        /// <summary>
        /// Ignore a single warning produced by Burst.
        /// </summary>
        /// <param name="warning">The warning to ignore.</param>
        public IgnoreWarningAttribute(int warning) { }
    }
}
