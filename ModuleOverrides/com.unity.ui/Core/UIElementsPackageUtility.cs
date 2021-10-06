// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;

namespace UnityEngine.UIElements
{
    internal static class UIElementsPackageUtility
    {
        internal static bool IsUIEPackageLoaded { get; private set; }
        internal static string EditorResourcesBasePath { get; private set; }

        static UIElementsPackageUtility()
        {
            Refresh();
        }

        internal static void Refresh()
        {
            EditorResourcesBasePath = "";
            IsUIEPackageLoaded = false;
        }

    }
}
