// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Attribute to mark a field as being a basic setting, one that appear in the Basic Settings section
    /// of the model inspector.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    class ModelSettingAttribute : Attribute
    {
    }
}
