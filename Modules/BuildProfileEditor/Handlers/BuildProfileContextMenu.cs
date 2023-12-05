// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEditor.Build.Profile.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Handlers
{
    internal class BuildProfileContextMenu
    {
        static readonly string k_Duplicate = L10n.Tr("Duplicate");
        static readonly string k_CopyToNewProfile = L10n.Tr("Copy To New Profile");
        static readonly string k_Rename = L10n.Tr("Rename");
        static readonly string k_Delete = L10n.Tr("Delete");

        static readonly string k_DeleteContinue = L10n.Tr("Continue");
        static readonly string k_DeleteCancel = L10n.Tr("Cancel");
        static readonly string k_DeleteTitle = L10n.Tr("Delete Active Build Profile");
        static readonly string k_DeleteMessage = L10n.Tr("This will delete your active build profile and activate the respective platform build profile. This cannot be undone.");

        BuildProfileWindowSelection m_ProfileSelection;
        BuildProfileDataSource m_ProfileDataSource;
        BuildProfileWindow m_ProfileWindow;

        internal BuildProfileContextMenu(BuildProfileWindow window, BuildProfileWindowSelection profileSelection, BuildProfileDataSource profileDataSource)
        {
            m_ProfileWindow = window;
            m_ProfileSelection = profileSelection;
            m_ProfileDataSource = profileDataSource;
        }

        internal ContextualMenuManipulator AddBuildProfileContextMenu()
        {
            return new ContextualMenuManipulator((evt) =>
            {
                BuildProfileListEditableLabel label = evt.target as BuildProfileListEditableLabel;
                BuildProfile targetProfile = label.dataSource as BuildProfile;
                if (targetProfile == null)
                {
                    return;
                }

                bool isMultipleSelection =  m_ProfileSelection.IsMultipleSelection();
                bool isClassic = BuildProfileContext.IsClassicPlatformProfile(targetProfile);
                if (!isMultipleSelection)
                {
                    SelectBuildProfileInView(targetProfile, isClassic, shouldAppend: false);
                }

                evt.menu.ClearItems();

                if (isClassic)
                {
                    evt.menu.AppendAction(
                        k_CopyToNewProfile,
                        action =>
                        {
                            HandleDuplicateSelectedProfiles(duplicateClassic: true);
                        });
                }
                else
                {
                    evt.menu.AppendAction(
                        k_Duplicate,
                        action =>
                        {
                            HandleDuplicateSelectedProfiles(duplicateClassic: false);
                        });

                    evt.menu.AppendAction(
                        k_Rename,
                        action =>
                        {
                            label.EditName();
                        }, isMultipleSelection ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

                    evt.menu.AppendAction(
                        k_Delete,
                        action =>
                        {
                            HandleDeleteSelectedProfiles();
                        });
                }
            });
        }

        internal bool UpdateBuildProfileLabelName(object buildProfileObject, string buildProfileLabelName)
        {
            var buildProfile = buildProfileObject as BuildProfile;
            if (buildProfile == null || string.IsNullOrEmpty(buildProfileLabelName))
                return false;

            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (var character in invalidChars)
            {
                if (buildProfileLabelName.Contains(character))
                {
                    Debug.LogWarning($"A file name can't contain the following character:\t{character}");
                    return false;
                }
            }

            string newName = buildProfileLabelName;
            m_ProfileDataSource.RenameAsset(buildProfile, newName);
            m_ProfileSelection.ClearListViewSelection(BuildProfileWindowSelection.ListViewSelectionType.ClassicAndCustom);
            SelectBuildProfileInView(buildProfile, isClassic: false, shouldAppend: false);
            return true;
        }

        internal void SelectClassicBuildProfileInViewForModule(string moduleName)
        {
            var targetIndex = -1;
            for (int i = 0; i < m_ProfileDataSource.classicPlatforms.Count; ++i)
            {
                var platform = m_ProfileDataSource.classicPlatforms[i];
                if (platform.moduleName == moduleName)
                {
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex >= 0)
                m_ProfileSelection.SelectBuildProfileInViewByIndex(targetIndex, isClassic: true, shouldAppend: false);
        }

        void SelectBuildProfileInView(BuildProfile buildProfile, bool isClassic, bool shouldAppend)
        {
            var targetProfiles = isClassic ? m_ProfileDataSource.classicPlatforms : m_ProfileDataSource.customBuildProfiles;
            var index = targetProfiles.IndexOf(buildProfile);
            if (index >= 0)
                m_ProfileSelection.SelectBuildProfileInViewByIndex(index, isClassic, shouldAppend);
        }

        void HandleDuplicateSelectedProfiles(bool duplicateClassic)
        {
            var selectedProfiles = m_ProfileSelection.GetAll();
            var duplicatedProfiles = m_ProfileDataSource.DuplicateProfiles(selectedProfiles, duplicateClassic);

            m_ProfileWindow.RepaintAndClearSelection();

            for (int i = 0; i < duplicatedProfiles.Count; ++i)
            {
                var duplicated = duplicatedProfiles[i];
                bool shouldAppend = i > 0;
                SelectBuildProfileInView(duplicated, isClassic: false, shouldAppend);
            }
        }

        void HandleDeleteSelectedProfiles()
        {
            var selectedProfiles = m_ProfileSelection.GetAll();
            for (int i = selectedProfiles.Count - 1; i >= 0; --i)
            {
                var profile = selectedProfiles[i];
                if (BuildProfileContext.instance.activeProfile == profile)
                {
                    string path = AssetDatabase.GetAssetPath(profile);
                    string finalMessage = $"{path}\n\n{k_DeleteMessage}";
                    if (!EditorUtility.DisplayDialog(k_DeleteTitle, finalMessage, k_DeleteContinue, k_DeleteCancel))
                    {
                        continue;
                    }
                }

                m_ProfileDataSource.DeleteAsset(profile);
            }

            // No need to select a profile after deletion, since the method below
            // selects the active profile after repaint
            m_ProfileWindow.RepaintAndClearSelection();
        }
    }
}
