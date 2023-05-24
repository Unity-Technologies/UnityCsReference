// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageUpmVersionHistoryChangelogLink : PackageUpmChangelogLink
    {
        public PackageUpmVersionHistoryChangelogLink(IPackageVersion version) : base(version)
        {
        }

        public override bool isVisible => !isEmpty && base.isVisible;
    }
}
