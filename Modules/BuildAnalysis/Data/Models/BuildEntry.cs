// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    [Serializable]
    internal class BuildEntry : IEquatable<BuildEntry>
    {
        public GUID BuildSessionGUID;
        public string BuildName = string.Empty;
        public BuildTarget Platform;
        public DateTime BuildStartedAt;
        public BuildResult BuildResult;
        public BuildType BuildType;
        public long TotalSizeBytes;
        public long TotalTimeMs;
        public string FolderPath = string.Empty;  // Library/BuildHistory/{guid}

        public bool Equals(BuildEntry other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return BuildSessionGUID == other.BuildSessionGUID;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;

            return Equals((BuildEntry)obj);
        }

        public override int GetHashCode()
        {
            return BuildSessionGUID.GetHashCode();
        }

        public static bool operator ==(BuildEntry left, BuildEntry right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(BuildEntry left, BuildEntry right)
        {
            return !Equals(left, right);
        }
    }
}
