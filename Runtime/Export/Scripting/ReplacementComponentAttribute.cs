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

        internal bool ComponentDisabled { get; set; }

        internal string LearnMoreUrl { get; set; }

        public ReplacementComponentAttribute(string typeName, string displayName = null)
        {
            TypeName = typeName;
            DisplayName = displayName;
            AlternativeMessage = string.Empty;
            ComponentDisabled = false;
            LearnMoreUrl = string.Empty;
        }
    }

    internal class SRPReplacementComponentAttribute : ReplacementComponentAttribute
    {
        public SRPReplacementComponentAttribute(string typeName, string displayName = null)
            : base(typeName, displayName)
        {
            AlternativeMessage = $"This component is no longer supported and will be ignored. Consider replacing it with the {DisplayName} component.";
            ComponentDisabled = true;
            LearnMoreUrl = "https://unity.com/topics/the-path-to-a-single-render-pipeline-in-unity-7";
        }
    }
}
