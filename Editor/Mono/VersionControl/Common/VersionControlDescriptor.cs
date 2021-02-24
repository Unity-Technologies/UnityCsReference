// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.VersionControl
{
    public class VersionControlDescriptor
    {
        public string name { get; }
        public string displayName { get; }
        internal Type type { get; }

        internal VersionControlDescriptor(string name, string displayName, Type type)
        {
            this.name = name;
            this.displayName = displayName;
            this.type = type;
        }
    }
}
