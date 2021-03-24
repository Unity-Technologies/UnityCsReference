// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI
{
    internal interface IWindow
    {
        PackageSelectionArgs activeSelection { get; }

        IMenu addMenu { get; }
        IMenu advancedMenu { get; }

        void Select(string identifier);

        IDetailsExtension AddDetailsExtension();

        IPackageActionMenu AddPackageActionMenu();

        IPackageActionButton AddPackageActionButton();
    }
}
