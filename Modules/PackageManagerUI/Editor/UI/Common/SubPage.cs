// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class SubPage
    {
        public PackageFilterTab tab { get; private set; }

        public string name { get; private set; }
        public string displayName { get; private set; }
        public int priority { get; private set; }

        public string contentType { get; private set; }

        public Func<IPackage, bool> filter { get; private set; }

        public Func<IPackage, string> getGroupName { get; private set; }

        public Func<string, string, int> compareGroup { get; private set; }

        public SubPage(PackageFilterTab tab, string name, string displayName, string contentType, int priority, Func<IPackage, bool> filter = null, Func<IPackage, string> getGroupName = null, Func<string, string, int> compareGroup = null)
        {
            this.tab = tab;
            this.name = name;
            this.displayName = displayName;
            this.contentType = contentType;
            this.priority = priority;
            this.filter = filter;
            this.getGroupName = getGroupName;
            this.compareGroup = compareGroup;
        }
    }
}
