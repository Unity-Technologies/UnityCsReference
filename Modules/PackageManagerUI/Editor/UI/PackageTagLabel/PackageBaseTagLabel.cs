// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal abstract class PackageBaseTagLabel : Label
    {
        public new static readonly string ussClassName = "package-tag-label";

        public PackageBaseTagLabel()
        {
            AddToClassList(ussClassName);
        }

        public abstract void Refresh(IPackageVersion version);
    }
}
