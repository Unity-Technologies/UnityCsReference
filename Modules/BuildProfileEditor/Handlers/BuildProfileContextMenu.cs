// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using System.Collections.Generic;
using UnityEditor.Build.Profile.Elements;
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
        static readonly string k_DeleteActiveProfileTitle = L10n.Tr("Delete Active Build Profile");
        static readonly string k_DeleteActiveProfileMessage = L10n.Tr("This will delete your active build profile and activate the respective platform build profile. This cannot be undone.");
        static readonly string k_DeleteProfileTitle = L10n.Tr("Delete Selected Build Profile");
        static readonly string k_DeleteProfileMessage = L10n.Tr("This will delete the selected build profile. This cannot be undone.");
        static readonly string k_DeleteMultipleProfilesTitle = L10n.Tr("Delete Selected Build Profiles");
        static readonly string k_DeleteActiveAndOtherProfilesMessage = L10n.Tr("This will delete the selected build profiles, including your active build profile, and activate the respective platform build profile. This cannot be undone.");
        static readonly string k_DeleteMultipleProfilesMessage = L10n.Tr("This will delete the selected build profiles. This cannot be undone.");

        readonly BuildProfileWindowSelection m_ProfileSelection;
        readonly BuildProfileDataSource m_ProfileDataSource;
        readonly BuildProfileWindow m_ProfileWindow;

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

            string newName = buildProfileLabelName;
            m_ProfileDataSource.RenameAsset(buildProfile, newName);
            SelectBuildProfileInView(buildProfile, isClassic: false, shouldAppend: false);
            return true;
        }

        internal void HandleDuplicateSelectedProfiles(bool duplicateClassic)
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

        void SelectBuildProfileInView(BuildProfile buildProfile, bool isClassic, bool shouldAppend)
        {
            var targetProfiles = isClassic ? m_ProfileDataSource.classicPlatforms : m_ProfileDataSource.customBuildProfiles;
            var index = targetProfiles.IndexOf(buildProfile);
            if (index < 0)
                return;

            if (isClassic)
            {
                m_ProfileSelection.visualElement.SelectInstalledPlatform(index);
                return;
            }

            if (shouldAppend)
                m_ProfileSelection.visualElement.AppendBuildProfileSelection(index);
            else
                m_ProfileSelection.visualElement.SelectBuildProfile(index);
        }

        /// <summary>
        /// Given a list of build profiles, prompts for user confirmation for deletion.
        /// </summary>
        /// <returns>true, if user approves deletion.</returns>
        bool ShowDeleteSelectedProfilesDialog(List<BuildProfile> selectedProfiles)
        {
            var maxPathsToShow = 3;
            var paths = new StringBuilder();
            var containsActiveProfile = false;
            for (int i = selectedProfiles.Count - 1; i >= 0; --i)
            {
                if (i < selectedProfiles.Count - maxPathsToShow)
                {
                    paths.Append("...\n");
                    break;
                }

                var profile = selectedProfiles[i];
                if (BuildProfileContext.activeProfile == profile)
                    containsActiveProfile = true;

                var path = AssetDatabase.GetAssetPath(profile);
                paths.Append(path + "\n");
            }

            var multipleProfiles = selectedProfiles.Count > 1;
            var title = multipleProfiles
                ? k_DeleteMultipleProfilesTitle
                : containsActiveProfile ? k_DeleteActiveProfileTitle : k_DeleteProfileTitle;
            var message = paths + "\n" + (multipleProfiles
                ? (containsActiveProfile ? k_DeleteActiveAndOtherProfilesMessage : k_DeleteMultipleProfilesMessage)
                : (containsActiveProfile ? k_DeleteActiveProfileMessage : k_DeleteProfileMessage));

            return EditorUtility.DisplayDialog(title, message, k_DeleteContinue, k_DeleteCancel);
        }

        void HandleDeleteSelectedProfiles()
        {
            var selectedProfiles = m_ProfileSelection.GetAll();
            var isDeleteConfirmed = ShowDeleteSelectedProfilesDialog(selectedProfiles);
            if (!isDeleteConfirmed)
                return;

            var profilesDeleted = false;
            for (int i = selectedProfiles.Count - 1; i >= 0; --i)
            {
                var profile = selectedProfiles[i];
                if (BuildProfileContext.activeProfile == profile)
                {
                    // if we're deleting an active profile, we want to compare the value of its settings that require a restart
                    // to the value of the settings for the platform we'll be activating after we delete the current platform
                    // and show a restart editor prompt if they're different so the settings take effect
                    var isSuccess = BuildProfileModuleUtil.HandlePlayerSettingsChanged(profile, null,
                        profile.buildTarget, EditorUserBuildSettings.activeBuildTarget);
                    if (!isSuccess)
                    {
                        continue;
                    }
                }

                m_ProfileDataSource.DeleteAsset(profile);
                profilesDeleted = true;
            }

            // No need to select a profile after deletion, since the method below
            // selects the active profile after repaint
            if (profilesDeleted)
                m_ProfileWindow.RepaintAndClearSelection();
        }
    }
}
