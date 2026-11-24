// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class VisualState : IEquatable<VisualState>
    {
        public string itemUniqueId;
        public string groupName;
        public bool visible;
        public bool lockedByDefault;
        public bool userUnlocked;

        public bool isLocked => lockedByDefault && !userUnlocked;

        public VisualState(string itemUniqueId, string groupName = "", bool lockedByDefault = false)
        {
            this.itemUniqueId = itemUniqueId;
            this.groupName = groupName;
            this.lockedByDefault = lockedByDefault;
            visible = true;
            userUnlocked = false;
        }

        public bool Equals(VisualState other)
        {
            return other != null
                   && (itemUniqueId ?? string.Empty) == (other.itemUniqueId ?? string.Empty)
                   && (groupName ?? string.Empty) == (other.groupName ?? string.Empty)
                   && visible == other.visible
                   && lockedByDefault == other.lockedByDefault
                   && userUnlocked == other.userUnlocked;
        }

        public VisualState Clone()
        {
            return (VisualState)MemberwiseClone();
        }
    }
}
