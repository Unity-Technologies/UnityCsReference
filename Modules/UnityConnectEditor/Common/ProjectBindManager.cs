// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEditorInternal;
using Button = UnityEngine.UIElements.Button;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace UnityEditor.Connect
{
    /// <summary>
    /// A common system to handle Project Bind configuration.
    /// </summary>
    internal class ProjectBindManager : IDisposable
    {
        const string k_ProjectBindTemplatePath = "UXML/ServicesWindow/ProjectBind.uxml";
        const string k_ProjectBindCommonStyleSheetPath = "StyleSheets/ServicesWindow/ProjectBindCommon.uss";
        const string k_ProjectBindDarkStyleSheetPath = "StyleSheets/ServicesWindow/ProjectBindDark.uss";
        const string k_ProjectBindLightStyleSheetPath = "StyleSheets/ServicesWindow/ProjectBindLight.uss";
        const string k_SelectOrganizationText = "Select organization";
        const string k_SelectProjectText = "Select project";
        const string k_Yes = "Yes";
        const string k_No = "No";
        const string k_LinkProjectWindowTitle = "Link Project";
        const string k_DialogConfirmationMessage = "Are you sure you want to link to the project '{0}' in organization '{1}'?";
        const string k_ProjectLinkSuccessMessage = "Project was linked successfully.";
        const string k_RootDataKey = "rootKey";
        const string k_BindContainerDataKey = "bindContainerKey";
        const string k_RoleOwner = "owner";
        const string k_RoleManager = "manager";

        static readonly List<string> k_AtLeastManagerFilter;

        Dictionary<string, ProjectRequestResponse> m_ProjectInfoByName;
        internal ProjectNameSlashReplacer m_LastReuseBlockProject = new ProjectNameSlashReplacer();

        private VisualElement m_LinkOrCreateBlock;
        private VisualElement m_LinkCloudProjectTab;
        private VisualElement m_CreateCloudProjectTab;

        // uxml elements
        private DropdownField m_ProjectsDropdown;
        private DropdownField m_OrganizationsDropdown;
        private RadioButtonGroup m_RadioButtonGroup;
        private HelpBox m_CreatePermissionsHelpBox;
        private Button m_LinkCloudProjectButton;
        private Button m_CreateProjectButton;
        private Button m_RefreshButton;

        private Task m_CurrentTask;
        private readonly object m_CurrentTaskLock = new();

        List<OrganizationRequestResponse> m_OrgIdByName = new ();

        public CreateButtonCallback createButtonCallback { private get; set; }
        public LinkButtonCallback linkButtonCallback { private get; set; }
        public ExceptionCallback exceptionCallback { private get; set; }

        public VisualElement projectBindContainer { get; private set; }

        internal struct ProjectBindState
        {
            public bool bound;
            public string projectName;
        }

        static ProjectBindManager()
        {
            k_AtLeastManagerFilter = new List<string>(new[] { k_RoleOwner, k_RoleManager });
        }

        /// <summary>
        /// Configures a new Project Bind manager to this within an existing EditorWindow
        /// </summary>
        /// <param name="rootVisualElement">visual element where the project bind content must be added</param>
        public ProjectBindManager(VisualElement rootVisualElement)
            => InitializeProjectBindManager(rootVisualElement);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_CurrentTask?.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        ~ProjectBindManager()
        {
            Unsubscribe();
            Dispose(false);
        }

        void Subscribe()
        {
            m_CreateProjectButton.clicked += OnCreateProjectButtonClicked;
            m_LinkCloudProjectButton.clicked += OnLinkProjectButtonClicked;
            m_RefreshButton.clicked += OnRefreshButtonClicked;
        }

        void Unsubscribe()
        {
            UnityConnect.instance.ProjectStateChanged -= OnProjectStateChangedAfterRebind;
            UnityConnect.instance.ProjectStateChanged -= OnProjectStateChangedAfterCreation;
            m_CreateProjectButton.clicked -= OnCreateProjectButtonClicked;
            m_LinkCloudProjectButton.clicked -= OnLinkProjectButtonClicked;
            m_RefreshButton.clicked -= OnRefreshButtonClicked;
        }

        void InitializeProjectBindManager(VisualElement rootVisualElement)
        {
            m_LastReuseBlockProject.LastProjectName = L10n.Tr(k_SelectProjectText);
            rootVisualElement.AddStyleSheetPath(k_ProjectBindCommonStyleSheetPath);
            rootVisualElement.viewDataKey = k_RootDataKey;
            rootVisualElement.AddStyleSheetPath(EditorGUIUtility.isProSkin ? k_ProjectBindDarkStyleSheetPath : k_ProjectBindLightStyleSheetPath);
            var projectBindTemplate = EditorGUIUtility.Load(k_ProjectBindTemplatePath) as VisualTreeAsset;
            rootVisualElement.Add(projectBindTemplate.CloneTree().contentContainer);

            QueryAndPrepareUxmlElements(rootVisualElement);

            Subscribe();

            InitializeOrganizationsDropdown();
            InitializeProjectsDropdown();

            _ = RunUITask(FetchOrganizations());

            EditorGameServicesAnalytics.SendProjectBindDisplayEvent();
        }

        void InitializeOrganizationsDropdown()
        {
            m_OrganizationsDropdown.choices = new List<string>{L10n.Tr(k_SelectOrganizationText)};
            m_OrganizationsDropdown.SetValueWithoutNotify(L10n.Tr(k_SelectOrganizationText));

            m_OrganizationsDropdown.RegisterValueChangedCallback(
                str => _ = RunUITask(OnOrganizationsDropdownSelectedValueChanged(str)));
        }

        void InitializeProjectsDropdown()
        {
            m_ProjectsDropdown.choices = new List<string>{L10n.Tr(k_SelectProjectText)};
            m_ProjectsDropdown.SetValueWithoutNotify(L10n.Tr(k_SelectProjectText));

            m_ProjectsDropdown.RegisterValueChangedCallback(
                evt => _ = RunUITask(OnProjectsDropdownSelectedValueChanged(evt)));
        }

        void QueryAndPrepareUxmlElements(VisualElement rootVisualElement)
        {
            projectBindContainer = rootVisualElement.Q("ProjectBindContainer");
            projectBindContainer.viewDataKey = k_BindContainerDataKey;

            var organizationSelectionBlock = projectBindContainer.Q("OrganizationSelectionBlock");
            VisualElement m_ChooseActionBlock = projectBindContainer.Q("ChooseActionBlock");
            m_RadioButtonGroup = m_ChooseActionBlock.Q<RadioButtonGroup>("RadioButtonGroup");

            var footer = projectBindContainer.Q("RefreshButtonBlock");

            m_LinkOrCreateBlock = projectBindContainer.Q("LinkOrCreateBlock");

            m_LinkCloudProjectTab = m_LinkOrCreateBlock.Q("LinkCloudProjectBlock");
            m_CreateCloudProjectTab = m_LinkOrCreateBlock.Q("CreateCloudProjectBlock");

            m_ProjectsDropdown = m_LinkCloudProjectTab.Q<DropdownField>("ProjectDropdown");
            m_OrganizationsDropdown = organizationSelectionBlock.Q<DropdownField>("OrganizationDropdown");

            var radioButtonUse = m_RadioButtonGroup.Q<RadioButton>("RadioButtonUse");
            var radioButtonCreate = m_RadioButtonGroup.Q<RadioButton>("RadioButtonCreate");

            radioButtonUse.RegisterValueChangedCallback(OnRadioButtonUseValueChanged);
            radioButtonCreate.RegisterValueChangedCallback(OnRadioButtonCreateValueChanged);

            m_LinkCloudProjectButton = m_LinkCloudProjectTab.Q<Button>("LinkBtn");
            m_CreateProjectButton = m_CreateCloudProjectTab.Q<Button>("CreateProjectIdBtn");
            m_CreatePermissionsHelpBox = m_CreateCloudProjectTab.Q<HelpBox>("CreatePermissionsHelpBox");

            m_CreateCloudProjectTab.Remove(m_CreatePermissionsHelpBox);

            m_RefreshButton = footer.Q<Button>("Refresh");

            m_LinkOrCreateBlock.Remove(m_CreateCloudProjectTab);
        }

        async Task OnOrganizationsFetched(List<string> organizationNames)
            => await UpdateOrganizationsDropdown(organizationNames);

        async Task OnProjectsFetched(List<string> projectNames)
            => await UpdateProjectsDropdown(projectNames);

        void OnRefreshButtonClicked()
            => _ = RunUITask(FetchOrganizations());

        void OnCreateProjectButtonClicked()
            => _ = RunUITask(CreateProject());

        void OnLinkProjectButtonClicked()
            => _ = RunUITask(LinkProject());

        /// <summary>
        /// Deactivate all interactive elements
        /// </summary>
        async Task DeactivateDropdownsAndButtons()
        {
            await AsyncUtils.RunNextActionOnMainThread(() =>
            {
                m_OrganizationsDropdown.SetEnabled(false);
                m_CreateProjectButton.SetEnabled(false);
                m_ProjectsDropdown.SetEnabled(false);
                m_LinkCloudProjectButton.SetEnabled(false);
                m_RefreshButton.SetEnabled(false);
            });
        }

        async Task RefreshDropdownAndButtonStates()
        {
            await AsyncUtils.RunNextActionOnMainThread(() =>
            {
                m_OrganizationsDropdown.SetEnabled(m_OrganizationsDropdown.choices !=
                                                   new List<string>() {L10n.Tr(k_SelectOrganizationText)});

                var isManager = false;

                foreach (var org in m_OrgIdByName)
                {
                    if (org.Name == m_OrganizationsDropdown.value)
                    {
                        isManager = k_AtLeastManagerFilter.Contains(org.Role);
                    }
                }

                m_CreateProjectButton.SetEnabled(
                    m_OrganizationsDropdown.value != L10n.Tr(k_SelectOrganizationText) &&
                    isManager);

                m_ProjectsDropdown.SetEnabled(m_OrganizationsDropdown.value != L10n.Tr(k_SelectOrganizationText));

                m_LinkCloudProjectButton.SetEnabled(m_ProjectsDropdown.value != L10n.Tr(k_SelectProjectText));

                m_RefreshButton.SetEnabled(true);

                if (m_OrganizationsDropdown.value != L10n.Tr(k_SelectOrganizationText) &&
                    !isManager)
                {
                    if (!m_CreateCloudProjectTab.Contains(m_CreatePermissionsHelpBox))
                        m_CreateCloudProjectTab.Add(m_CreatePermissionsHelpBox);
                }
                else
                {
                    if (m_CreateCloudProjectTab.Contains(m_CreatePermissionsHelpBox))
                        m_CreateCloudProjectTab.Remove(m_CreatePermissionsHelpBox);
                }
            });
        }

        async Task UpdateOrganizationsDropdown(List<string> organizationNames)
        {
            await AsyncUtils.RunNextActionOnMainThread(() =>
            {
                m_OrganizationsDropdown.choices = organizationNames;
                var savedOrg = m_OrganizationsDropdown.value;
                m_OrganizationsDropdown.SetValueWithoutNotify("");

                if (m_OrganizationsDropdown.choices.Contains(savedOrg) &&
                    savedOrg != L10n.Tr(k_SelectOrganizationText))
                {
                    m_OrganizationsDropdown.value = savedOrg;
                }
                else
                {
                    m_OrganizationsDropdown.value = L10n.Tr(k_SelectOrganizationText);
                }
            });
        }

        async Task UpdateProjectsDropdown(List<string> projectNames)
        {
            await AsyncUtils.RunNextActionOnMainThread(() =>
            {
                m_ProjectsDropdown.choices = projectNames;
                m_ProjectsDropdown.SetValueWithoutNotify("");

                if (m_ProjectsDropdown.choices.Contains(m_LastReuseBlockProject.LastProjectName))
                {
                    m_ProjectsDropdown.value = m_LastReuseBlockProject.LastProjectName;
                }
                else
                {
                    m_ProjectsDropdown.value = L10n.Tr(k_SelectProjectText);
                }
            });
        }

        void OnRadioButtonUseValueChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                if (m_LinkOrCreateBlock.Contains(m_CreateCloudProjectTab))
                {
                    m_LinkOrCreateBlock.Remove(m_CreateCloudProjectTab);
                }
                m_LinkOrCreateBlock.Add(m_LinkCloudProjectTab);
            }
        }

        void OnRadioButtonCreateValueChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                if (m_LinkOrCreateBlock.Contains(m_LinkCloudProjectTab))
                {
                    m_LinkOrCreateBlock.Remove(m_LinkCloudProjectTab);
                }

                m_LinkOrCreateBlock.Add(m_CreateCloudProjectTab);
            }
        }

        async Task OnOrganizationsDropdownSelectedValueChanged(ChangeEvent<string> evt)
        {
            if (m_OrganizationsDropdown.value == L10n.Tr(k_SelectOrganizationText))
            {
                m_ProjectsDropdown.choices = new List<string>() { L10n.Tr(k_SelectProjectText) };
                m_ProjectsDropdown.value = L10n.Tr(k_SelectProjectText);
            }
            else
            {
                await FetchProjects();
            }
        }

        Task OnProjectsDropdownSelectedValueChanged(ChangeEvent<string> evt)
        {
            m_LastReuseBlockProject.LastProjectName = evt.newValue;
            return Task.CompletedTask;
        }

        Task LinkProject()
        {
            var abort = false;
            var projectInfo = m_ProjectInfoByName[m_LastReuseBlockProject.LastProjectName];
            if (EditorUtility.DisplayDialog(L10n.Tr(k_LinkProjectWindowTitle),
                    string.Format(L10n.Tr(k_DialogConfirmationMessage), projectInfo.Name, projectInfo.OrganizationName),
                    L10n.Tr(k_Yes), L10n.Tr(k_No)))
            {
                try
                {
                    //Only register before creation. Remove first in case it was already added.
                    //TODO: Review to avoid dependency on project refreshed
                    UnityConnect.instance.ProjectStateChanged -= OnProjectStateChangedAfterRebind;
                    UnityConnect.instance.ProjectStateChanged += OnProjectStateChangedAfterRebind;
                    BindProject(projectInfo);
                    Unsubscribe();
                }
                catch (Exception ex)
                {
                    if (exceptionCallback != null)
                    {
                        exceptionCallback.Invoke(ex);
                        abort = true;
                    }
                    else
                    {
                        //If there is no exception callback, we have to at least log it
                        Debug.LogException(ex);
                    }
                }
                if (!abort)
                {
                    linkButtonCallback?.Invoke(projectInfo);
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Get a list of all the organizations a user has access to
        /// </summary>
        async Task FetchOrganizations()
        {
            try
            {
                m_OrgIdByName.Clear();
                var sortedOrganizationNames = new List<string>();

                m_OrgIdByName = await UnityConnectRequests.GetOrganizationsAsync();

                foreach (var org in m_OrgIdByName)
                {
                    sortedOrganizationNames.Add(org.Name);
                }

                sortedOrganizationNames.Sort();

                var popUpChoices = new List<string> {L10n.Tr(k_SelectOrganizationText)};
                popUpChoices.AddRange(sortedOrganizationNames);

                await OnOrganizationsFetched(popUpChoices);
            }
            catch (Exception ex)
            {
                if (exceptionCallback != null)
                {
                    exceptionCallback.Invoke(ex);
                }
                else
                {
                    //If there is no exception callback, we have to at least log it
                    Debug.LogException(ex);
                }
            }
        }

        /// <summary>
        /// Get a list of all the projects a user has access to within an organization
        /// </summary>
        async Task FetchProjects()
        {
            try
            {
                var selectedOrg = GetSelectedOrganization();

                var projects =
                    await UnityConnectRequests.GetOrganizationProjectsAsync(selectedOrg.GenesisId);

                m_ProjectInfoByName = new ();

                foreach (var project in projects)
                {
                    project.OrganizationName = selectedOrg.Name;
                    m_ProjectInfoByName.Add(project.Name, project);
                }

                var sortedProjectNames = new List<string>(m_ProjectInfoByName.Keys);

                // To work around the UI feature that creates sub-menus on slash characters, we
                // replaces slashes in project names by a fake stylized slash. This hack could be
                // removed in the future when we get a UI feature for it, or when dashboard will prevent
                // users from inputting slashes in their project names.
                sortedProjectNames =
                    m_LastReuseBlockProject.ReplaceSlashForFakeSlash(sortedProjectNames);
                sortedProjectNames.Sort();
                sortedProjectNames.Insert(0, L10n.Tr(k_SelectProjectText));

                await OnProjectsFetched(sortedProjectNames);
            }
            catch (Exception ex)
            {
                if (exceptionCallback != null)
                {
                    exceptionCallback.Invoke(ex);
                }
                else
                {
                    //If there is no exception callback, we have to at least log it
                    Debug.LogException(ex);
                }
            }
        }

        async Task CreateProject()
        {
            try
            {
                var selectedOrg = GetSelectedOrganization();

                var createdProject = await UnityConnectRequests.CreateNewProjectInOrganizationAsync(selectedOrg.GenesisId);

                ServicesRepository.DisableAllServices(shouldUpdateApiFlag: false);
                UnityConnect.instance.ProjectStateChanged -= OnProjectStateChangedAfterCreation;
                UnityConnect.instance.ProjectStateChanged += OnProjectStateChangedAfterCreation;
                BindProject(createdProject);
            }
            catch (Exception ex)
            {
                if (exceptionCallback != null)
                {
                    exceptionCallback.Invoke(ex);
                }
                else
                {
                    //If there is no exception callback, we have to at least log it
                    Debug.LogException(ex);
                }
            }
        }

        static void BindProject(ProjectRequestResponse projectInfo)
        {
            UnityConnect.instance.BindProject(projectInfo.Id, projectInfo.Name, projectInfo.OrganizationLegacyId);
            EditorAnalytics.SendProjectServiceBindingEvent(new ProjectBindState() { bound = true, projectName = projectInfo.Name });
            NotificationManager.instance.Publish(Notification.Topic.ProjectBind, Notification.Severity.Info, L10n.Tr(k_ProjectLinkSuccessMessage));
        }

        /// <summary>
        /// Using this method to simulate a callback after binding a project for creation and making sure the
        /// project info are fully loaded in UnityConnect.instance.projectInfo
        /// </summary>
        /// <param name="projectInfo"></param>
        private void OnProjectStateChangedAfterCreation(ProjectInfo projectInfo)
        {
            if (UnityConnect.instance.projectInfo.valid)
            {
                UnityConnect.instance.ProjectStateChanged -= OnProjectStateChangedAfterCreation;
                ServicesRepository.EnableServicesOnProjectCreation();
            }
        }

        /// <summary>
        /// Using this method to simulate a callback after binding a project for rebinding an existing project
        /// and making sure the project info are fully loaded in UnityConnect.instance.projectInfo
        /// </summary>
        /// <param name="projectInfo"></param>
        private void OnProjectStateChangedAfterRebind(ProjectInfo projectInfo)
        {
            if (UnityConnect.instance.projectInfo.valid)
            {
                UnityConnect.instance.ProjectStateChanged -= OnProjectStateChangedAfterRebind;
            }
        }

        /// <summary>
        /// Return the information of the currently selected organization
        /// </summary>
        OrganizationRequestResponse GetSelectedOrganization()
        {
            foreach (var org in m_OrgIdByName)
            {
                if (org.Name == m_OrganizationsDropdown.value)
                {
                    return org;
                }
            }

            throw new KeyNotFoundException($"Could not find organization {m_OrganizationsDropdown.value}.");
        }

        /// <summary>
        /// Prevents users from triggering multiple different major operations at the same time from the UI
        /// </summary>
        /// <param name="task">Task to run if no other major task is running</param>
        async Task RunUITask(Task task)
        {
            lock (m_CurrentTaskLock)
            {
                if (m_CurrentTask is {IsCompleted: false})
                {
                    return;
                }

                m_CurrentTask = task;
            }

            await DeactivateDropdownsAndButtons();
            await task;
            await RefreshDropdownAndButtonStates();
        }

        public delegate void CreateButtonCallback(ProjectRequestResponse projectInfoData);
        public delegate void LinkButtonCallback(ProjectRequestResponse projectInfoData);
        public delegate void ExceptionCallback(Exception exception);
    }
}
