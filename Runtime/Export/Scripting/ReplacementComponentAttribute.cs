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

        internal string AlternativeMessage { get; set; }

        public ReplacementComponentAttribute(string typeName, string displayName = null)
        {
            TypeName = typeName;
            DisplayName = displayName;
            AlternativeMessage = string.Empty;
        }
    }

    internal class SRPReplacementComponentAttribute : ReplacementComponentAttribute
    {
        public SRPReplacementComponentAttribute(string typeName, string displayName = null)
            : base(typeName, displayName)
        {
            AlternativeMessage = $"This component is not supported in SRP projects and will be ignored. Use the {DisplayName} component instead.";
        }
    }
}
