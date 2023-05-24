// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageLink
    {
        public IPackageVersion version { get; }

        public PackageLink(IPackageVersion version)
        {
            this.version = version;
        }

        public string displayName { get; set; }
        public string url { get; set; }
        public string analyticsEventName { get; set; }

        public string offlinePath { get; set; }

        public bool isEmpty => string.IsNullOrEmpty(url) && string.IsNullOrEmpty(offlinePath);

        public virtual bool isVisible => !isEmpty;
        public virtual bool isEnabled => true;

        public virtual string tooltip => url;

        internal enum ContextMenuAction
        {
            OpenInBrowser,
            OpenLocally,
        }

        public virtual ContextMenuAction[] contextMenuActions => Array.Empty<ContextMenuAction>();
    }
}
