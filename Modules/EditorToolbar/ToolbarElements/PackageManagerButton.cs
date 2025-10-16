// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.PackageManager.UI;
using UnityEditor.PackageManager.UI.Internal;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    [InitializeOnLoad]
    sealed class PackageManagerButton
    {
        const string k_ElementPath = "Package Management/Package Manager";

        [UnityOnlyMainToolbarPreset]
        [MainToolbarElement(k_ElementPath, defaultDockIndex = 11, defaultDockPosition = MainToolbarDockPosition.Left)]
        static MainToolbarElement Create()
        {
            var (icon, tooltip, clickAction) = GetContent(s_PackageDatabase);
            return new MainToolbarButton(new MainToolbarContent(icon, tooltip), clickAction);
        }

        static IPackageDatabase s_PackageDatabase;

        const string k_DefaultIconPath = "Icons/PackageManagerDefault.png";
        const string k_WarningIconPath = "Icons/PackageManagerWarning.png";
        const string k_ErrorIconPath = "Icons/PackageManagerError.png";

        static PackageManagerButton()
        {
            s_PackageDatabase = ServicesContainer.instance.Resolve<IPackageDatabase>();
            s_PackageDatabase.onPackagesChanged += OnPackageChanged;
        }

        static void OnPackageChanged(PackagesChangeArgs _) => MainToolbar.Refresh(k_ElementPath);

        internal static (Texture2D, string, Action) GetContent(IPackageDatabase packageDatabase)
        {
            var state = packageDatabase.GetPackagesInUseState();

            Texture2D icon = null;
            string tooltip = null;
            Action clickAction = null;

            switch (state)
            {
                case PackageInUseState.NonCompliant:
                    icon = EditorGUIUtility.LoadIcon(k_ErrorIconPath);
                    tooltip = L10n.Tr("Restricted Packages In Use");
                    clickAction = () => PackageManagerWindow.OpenAndSelectPage(InProjectNonCompliancePage.k_Id);
                    break;

                case PackageInUseState.Error:
                    icon = EditorGUIUtility.LoadIcon(k_ErrorIconPath);
                    tooltip = L10n.Tr("Project contains packages with errors");
                    clickAction = () => PackageManagerWindow.OpenAndSelectPage(InProjectErrorsAndWarningsPage.k_Id);
                    break;

                case PackageInUseState.Warning:
                    icon = EditorGUIUtility.LoadIcon(k_WarningIconPath);
                    tooltip = L10n.Tr("Project contains packages with warnings");
                    clickAction = () => PackageManagerWindow.OpenAndSelectPage(InProjectErrorsAndWarningsPage.k_Id);
                    break;

                case PackageInUseState.Experimental:
                    icon = EditorGUIUtility.LoadIcon(k_WarningIconPath);
                    tooltip = L10n.Tr("Experimental Packages In Use");
                    clickAction = () => PackageManagerWindow.OpenAndSelectPage(InProjectPage.k_Id, "experimental");
                    break;

                case PackageInUseState.None:
                default:
                    icon = EditorGUIUtility.LoadIcon(k_DefaultIconPath);
                    tooltip = L10n.Tr("Package Manager");
                    clickAction = () => PackageManagerWindow.OpenAndSelectPackage(null);
                    break;
            }
            return (icon, tooltip, clickAction);
        }
    }
}
