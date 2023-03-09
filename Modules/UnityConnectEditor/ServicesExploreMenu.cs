// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.PackageManager.UI;

namespace UnityEditor.Connect
{
    static class ServicesExploreMenu
    {
        [MenuItem("Services/Explore", false, ServicesConstants.ExploreServicesTopMenuPriority, false)]
        static void OpenPackageManagerOnServicesFilter()
        {
            EditorGameServicesAnalytics.SendTopMenuExploreEvent();
            PackageManagerWindow.OpenPackageManagerOnPage(ServicesConstants.ExploreServicesPackageManagerPageId);
        }
    }
}
