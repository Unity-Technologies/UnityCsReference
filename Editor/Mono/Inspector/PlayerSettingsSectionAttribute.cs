// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEditor.Build;

namespace UnityEditor
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    internal class PlayerSettingsSectionAttribute: System.Attribute
    {
        private string targetName;
        public string title;
        public int order;

        public string TargetName => targetName;
        public string Title => title;
        public int Order => order;

        public PlayerSettingsSectionAttribute(string targetName)
        {
            this.targetName = targetName;
            this.title = String.Empty;
            this.order = 0;
        }
    }
}
