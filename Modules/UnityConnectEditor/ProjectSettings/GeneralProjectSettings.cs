// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace UnityEditor.Connect
{
    /// <summary>
    /// The general section of the CloudServices project settings (actually simply called Services in the ProjectSettings)
    /// Uses a simple state machine to keep track of current state.
    /// </summary>
    internal class GeneralProjectSettings : ServicesProjectSettings
    {
        const string k_ProjectSettingsPath = "Project/Services";
        const string k_TemplatePath = "UXML/ServicesWindow/GeneralProjectSettingsStateBound.uxml";

        const string k_ScrollContainerClassName = "scroll-container";

        const string k_KeywordOrganization = "organization";
        const string k_KeywordProject = "project";
        const string k_KeywordUnlink = "unlink";
        const string k_KeywordMembers = "members";
        const string k_KeywordDashboard = "dashboard";
        const string k_KeywordId = "id";
        const string k_GeneralLabel = "General";

        const string k_ProjectNameBlockName = "ProjectName";
        const string k_OrganizationBlockName = "Organization";
        const string k_ProjectIdBlockName = "ProjectId";
        const string k_DashboardBlockName = "Dashboard";
        const string k_ProjectIdSubmitButtonName = "ProjectIdSubmit";

        const string k_EditButtonClassName = "edit-button";
        const string k_CancelButtonClassName = "cancel-button";
        const string k_FieldValueClassName = "field-value";
        const string k_ReadModeClassName = "read-mode";
        const string k_EditModeClassName = "edit-mode";

        const string k_UnlinkProjectDialogTitle = "Unlink Project";
        const string k_UnlinkProjectDialogMessage = "Are you sure you want to unlink this project?";
        const string k_ProjectUnlinkSuccessMessage = "Project was unlinked successfully.";
        const string k_Yes = "Yes";
        const string k_No = "No";

        public static string generalProjectSettingsPath => k_ProjectSettingsPath;

        protected override bool sendNotificationForNonStandardStates => false;

        CoppaManager m_CoppaManager;

        [SettingsProvider]
        public static SettingsProvider CreateServicesProvider()
        {
            return new GeneralProjectSettings(k_ProjectSettingsPath, SettingsScope.Project, new List<string>()
            {
                L10n.Tr(k_KeywordOrganization),
                L10n.Tr(k_KeywordProject),
                L10n.Tr(k_KeywordUnlink),
                L10n.Tr(k_KeywordMembers),
                L10n.Tr(k_KeywordDashboard),
                L10n.Tr(k_KeywordId)
            });
        }

        GeneralProjectSettings(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, k_GeneralLabel, keywords) {}

        protected override Notification.Topic[] notificationTopicsToSubscribe => new[]
        {
            Notification.Topic.AdsService,
            Notification.Topic.AnalyticsService,
            Notification.Topic.BuildService,
            Notification.Topic.CollabService,
            Notification.Topic.CoppaCompliance,
            Notification.Topic.CrashService,
            Notification.Topic.ProjectBind,
            Notification.Topic.PurchasingService
        };

        protected override SingleService serviceInstance => null;
        protected override string serviceUssClassName => "general";

        protected override void ToggleRestrictedVisualElementsAvailability(bool enable)
        {
        }

        protected override void ActivateAction(string searchContext)
        {
            // Must reset properties every time this is activated
            rootVisualElement.Add(m_GeneralTemplate.CloneTree().contentContainer);

            //If we haven't received new bound info, fetch them
            var projectInfoOnBind = new ProjectInfoData(UnityConnect.instance.projectInfo.organizationId,
                UnityConnect.instance.projectInfo.organizationName,
                UnityConnect.instance.projectInfo.projectName,
                UnityConnect.instance.projectInfo.projectGUID,
                UnityConnect.instance.projectInfo.projectId);
            var generalTemplate = EditorGUIUtility.Load(k_TemplatePath) as VisualTreeAsset;
            var scrollContainer = rootVisualElement.Q(className: k_ScrollContainerClassName);
            scrollContainer.Clear();
            scrollContainer.Add(generalTemplate.CloneTree().contentContainer);
            SetupCoppaManager(scrollContainer);

            //Collect Field Blocks entry points and initialize them
            var projectNameFieldBlock = rootVisualElement.Q(k_ProjectNameBlockName);
            var organizationFieldBlock = rootVisualElement.Q(k_OrganizationBlockName);
            var projectIdFieldBlock = rootVisualElement.Q(k_ProjectIdBlockName);
            var dashboardFieldBlock = rootVisualElement.Q(k_DashboardBlockName);

            InitializeFieldBlock(projectNameFieldBlock, projectInfoOnBind.name);
            InitializeFieldBlock(organizationFieldBlock, projectInfoOnBind.organizationName);
            InitializeFieldBlock(projectIdFieldBlock, projectInfoOnBind.guid);
            InitializeFieldBlock(dashboardFieldBlock);

            //Setup dashboard link
            var dashboardClickable = new Clickable(() =>
            {
                ServicesConfiguration.instance.RequestBaseDashboardUrl(OpenDashboardOrgAndProjectIds);
            });
            rootVisualElement.Q(k_DashboardBlockName).AddManipulator(dashboardClickable);

            projectIdFieldBlock.Q<Button>(k_ProjectIdSubmitButtonName).clicked += UnbindProject;
            HandlePermissionRestrictedControls();
        }

        void SetupCoppaManager(VisualElement parentContainer)
        {
            //remove old version if any
            parentContainer.Q(CoppaManager.coppaContainerName)?.parent?.RemoveFromHierarchy();
            m_CoppaManager = new CoppaManager(parentContainer)
            {
                exceptionCallback = (compliance, exception) =>
                {
                    NotificationManager.instance.Publish(Notification.Topic.CoppaCompliance, Notification.Severity.Error,
                        L10n.Tr(exception.Message));
                }
            };
            var coppaContainer = rootVisualElement.Q(CoppaManager.coppaContainerName);
            var editModeContainer = coppaContainer?.Q(className: k_ClassNameEditMode);
            editModeContainer?.SetEnabled(false);
        }

        protected override void DeactivateAction()
        {
        }

        static void InitializeFieldBlock(VisualElement fieldBlock, string fieldValue = null)
        {
            if (fieldBlock != null)
            {
                ToggleModeVisibility(fieldBlock, true);
                var editButton = fieldBlock.Q<Button>(className: k_EditButtonClassName);
                if (editButton != null)
                {
                    editButton.clicked += () => { ToggleModeVisibility(fieldBlock, false); };
                }
                var cancelButton = fieldBlock.Q<Button>(className: k_CancelButtonClassName);
                if (cancelButton != null)
                {
                    cancelButton.clicked += () => { ToggleModeVisibility(fieldBlock, true); };
                }

                if (fieldValue != null)
                {
                    var valueLabel = fieldBlock.Q<Label>(className: k_FieldValueClassName);
                    if (valueLabel != null)
                    {
                        valueLabel.text = fieldValue;
                    }
                    var valueTextField = fieldBlock.Q<TextField>(className: k_FieldValueClassName);
                    valueTextField?.SetValueWithoutNotify(fieldValue);
                }
            }
        }

        static void ToggleModeVisibility(VisualElement fieldBlock, bool showRead)
        {
            var readMode = fieldBlock.Q(className: k_ReadModeClassName);
            if (readMode != null)
            {
                readMode.style.display = showRead ? DisplayStyle.Flex : DisplayStyle.None;
            }

            var editMode = fieldBlock.Q(className: k_EditModeClassName);
            if (editMode != null)
            {
                editMode.style.display = showRead ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        void UnbindProject()
        {
            if (EditorUtility.DisplayDialog(L10n.Tr(k_UnlinkProjectDialogTitle),
                L10n.Tr(k_UnlinkProjectDialogMessage),
                L10n.Tr(k_Yes),
                L10n.Tr(k_No)))
            {
                ServicesRepository.DisableAllServices(shouldUpdateApiFlag: false);

                string cachedProjectName = UnityConnect.instance.projectInfo.projectName;
                UnityConnect.instance.UnbindProject();
                EditorAnalytics.SendProjectServiceBindingEvent(new ProjectBindManager.ProjectBindState() { bound = false, projectName = cachedProjectName });
                NotificationManager.instance.Publish(Notification.Topic.ProjectBind, Notification.Severity.Info, L10n.Tr(k_ProjectUnlinkSuccessMessage));
                ReinitializeSettings();
            }
        }
    }
}
