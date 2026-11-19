// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.GraphToolkit.Editor
{
    interface IOptionsDefinition
    {
        INodeOption AddNodeOption(string optionName, Type dataType, string optionDisplayName = null, string tooltip = null,
            bool showInInspectorOnly = false, int order = 0, Attribute[] attributes = null, object defaultValue = null);
    }
}
