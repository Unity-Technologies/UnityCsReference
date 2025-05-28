// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal abstract class PackageBaseHelpBox : HelpBoxWithOptionalReadMore
    {
        public abstract void Refresh(IPackageVersion version);
    }
}
