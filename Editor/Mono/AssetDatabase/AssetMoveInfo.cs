// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Experimental
{
    public struct AssetMoveInfo : IEquatable<AssetMoveInfo>
    {
        public AssetMoveInfo(string sourceAssetPath, string destinationAssetPath)
        {
            this.sourceAssetPath = sourceAssetPath;
            this.destinationAssetPath = destinationAssetPath;
        }

        public string sourceAssetPath { get; }
        public string destinationAssetPath { get; }

        public bool Equals(AssetMoveInfo other)
        {
            return string.Equals(sourceAssetPath, other.sourceAssetPath) && string.Equals(destinationAssetPath, other.destinationAssetPath);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is AssetMoveInfo && Equals((AssetMoveInfo)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((sourceAssetPath != null ? sourceAssetPath.GetHashCode() : 0) * 397) ^ (destinationAssetPath != null ? destinationAssetPath.GetHashCode() : 0);
            }
        }

        public static bool operator==(AssetMoveInfo left, AssetMoveInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(AssetMoveInfo left, AssetMoveInfo right)
        {
            return !left.Equals(right);
        }
    }
}
