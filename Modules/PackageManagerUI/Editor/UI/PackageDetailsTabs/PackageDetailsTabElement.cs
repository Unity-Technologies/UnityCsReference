// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal abstract class PackageDetailsTabElement : BaseTabElement
    {
        // used to determine if the tab should be shown at all
        public virtual bool IsValid(IPackageVersion version) => true;

        public PackageDetailsTabElement()
        {
            style.display = DisplayStyle.Flex;
            style.flexGrow = 1;
            style.flexShrink = 1;
        }

        public abstract void Refresh(IPackageVersion version);
    }
}
