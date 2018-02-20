// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed partial class PreferenceItem : Attribute
    {
        public PreferenceItem(string name) { this.name = name; }
        public string name;
        [RequiredSignature]
        private static void signature() {}
    }
}
