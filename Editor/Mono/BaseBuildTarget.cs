// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal abstract class BaseBuildTarget : IBuildTarget
    {
        public virtual bool CanBuildOnCurrentHostPlatform => false;
        public virtual string DisplayName => TargetName;
        public abstract RuntimePlatform RuntimePlatform { get; }
        public abstract string TargetName { get; }

        public int CompareTo(IBuildTarget other)
        {
            return TargetName.CompareTo(other.TargetName);
        }

        public bool Equals(IBuildTarget other)
        {
            return TargetName.Equals(other.TargetName);
        }
    }
}
