// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class VisualState : IEquatable<VisualState>
    {
        public string packageUniqueId;
        public string groupName;
        public bool visible;
        public bool expanded;
        public bool seeAllVersions;
        public string selectedVersionId;

        public VisualState(string packageUniqueId, string groupName)
        {
            this.packageUniqueId = packageUniqueId;
            this.groupName = groupName;
            visible = true;
            expanded = false;
            seeAllVersions = false;
            selectedVersionId = string.Empty;
        }

        public bool Equals(VisualState other)
        {
            return packageUniqueId == other.packageUniqueId
                && groupName == other.groupName
                && visible == other.visible
                && expanded == other.expanded
                && seeAllVersions == other.seeAllVersions
                && selectedVersionId == other.selectedVersionId;
        }

        public VisualState Clone()
        {
            return (VisualState)MemberwiseClone();
        }
    }
}
