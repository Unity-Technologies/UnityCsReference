// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IPackageManagerWindowProxy: IService
    {
        public void OpenAndSelectPackage(string packageName, string pageId);
    }

    internal class PackageManagerWindowProxy: BaseService<IPackageManagerWindowProxy>, IPackageManagerWindowProxy
    {
        public void OpenAndSelectPackage(string packageName, string pageId)
        {
            PackageManagerWindow.OpenAndSelectPackage(packageName, pageId);
        }
    }
}
