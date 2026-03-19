// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    public class ReplacementComponentAttribute : Attribute
    {
        public string TypeName { get; }

        public string DisplayName { get; }

        public ReplacementComponentAttribute(string typeName, string displayName = null)
        {
            TypeName = typeName;
            DisplayName = displayName;
        }
    }
}
