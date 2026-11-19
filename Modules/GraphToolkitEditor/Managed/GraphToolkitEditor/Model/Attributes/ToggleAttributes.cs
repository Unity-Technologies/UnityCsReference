// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Attribute for boolean field, which allow to use the opposite value of the field. The Toggle will be checked when the field is false.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    [UnityRestricted]
    internal class InvertToggleAttribute : Attribute
    {
    }

    /// <summary>
    /// Attribute for boolean field, which displays the field as a true/False dropdown instead of a toggle.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    [UnityRestricted]
    internal class BoolDropDownAttribute : Attribute
    {
    }
}
