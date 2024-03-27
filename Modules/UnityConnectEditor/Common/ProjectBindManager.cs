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
        const long k_HttpStatusCodeUnprocessableEntity = 422;
        const string k_Yes = "Yes";
        const string k_No = "No";

        static readonly List<string> k_AnyRoleFilter;
        static readonly List<string> k_AtLeastManagerFilter;

        const string k_LinkProjectWindowTitle = "Link Project";
        const string k_DialogConfirmationMessage = "Are you sure you want to link to the project '{0}' in organization '{1}'?";
        const string k_NetworkIssueWarningMessage = "This might be caused by network issues. Try using the refresh button.";
        const string k_CouldNotCreateProjectMessage = "Could not create project.";
        const string k_CouldNotObtainProjectMessage = "Could not obtain projects. " + k_NetworkIssueWarningMessage;
        const string k_CouldNotObtainOrganizationsMessage = "Could not obtain organizations. " + k_NetworkIssueWarningMessage;
        const string k_ProjectLinkSuccessMessage = "Project was linked successfully.";

        const string k_RootDataKey = "rootKey";
        const string k_BindContainerDataKey = "bindContainerKey";

        const string k_RoleOwner = "owner";
        const string k_RoleManager = "manager";
        const string k_RoleUser = "user";

        const string k_JsonProjectsNodeName = "projects";
        const string k_JsonArchivedNodeName = "archived";
        const string k_JsonOrgIdNodeName = "org_id";
        const string k_JsonIdNodeName = "id";
        const string k_JsonNameNodeName = "name";
        const string k_JsonGuidNodeName = "guid";
        const string k_JsonOrgsNodeName = "orgs";
        const string k_JsonRoleNodeName = "role";
        const string k_JsonOrgNameNodeName = "org_name";
        internal const string k_FakeSlashUnicode = "\uff0f";

        Dictionary<string, ProjectInfoData> m_ProjectInfoByName;
        internal ProjectNameSlashReplacer m_LastReuseBlockProject = new ProjectNameSlashReplacer();
        UnityWebRequest m_CurrentRequest;
        int m_CreateIteration;

        private object m_ButtonsLock = new object();

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

        private bool m_FetchingOrganizations;
        private bool m_FetchingProjects;

        public static event Action<List<string>> OrganizationsFetched;
        public static event Action<List<string>> ProjectsFetched;

        class OrgData
        {
            public string Name;
            public string Id;
            public bool IsManager;
        }

        List<OrgData> m_OrgIdByName = new ();

        public CreateButtonCallback createButtonCallback { private get; set; }
        public LinkButtonCallback linkButtonCallback { private get; set; }
        public ExceptionCallback exceptionCallback { private get; set; }

        public VisualElement projectBindContainer { get; private set; }

        /// <summary>
        /// This class provides a method that replaces slashes in project names, keeps track of modified
        /// project names, and encapsulates the last project name used.
        /// </summary>
        /// <remarks>
        /// This is a hack to prevent UI display issues for UGS projects that contain a slash (to prevent the creation
        /// of UI sub-menus). This hack could be removed in the future, if UGS dashboard prevents users from inputting
        /// slashes in project names, or when UI team develops a feature to deactivate sub-menu creation on slashes.
        /// </remarks>
        internal class ProjectNameSlashReplacer
        {
            string m_LastProjectName;
            internal List<string> m_ModifiedProjectNames = new List<string>();

            internal string LastProjectName
            {
                get
                {
                    // Replaces a fake slash character by a real slash if the project name was modified. Slashes in
                    // project names get replaced with fake slashes for the UI. Here, we revert that. This is because
                    // in the code we want the true project names.
                    if(m_ModifiedProjectNames.Contains(m_LastProjectName))
                    {
                        return m_LastProjectName.Replace(k_FakeSlashUnicode, "/");
                    }
                    else
                    {
                        return m_LastProjectName;
                    }
                }
                set => m_LastProjectName = value;
            }

            /// <summary>
            /// Replaces the regular slash character with a stylized slash for all strings in a list
            /// </summary>
            /// <param name="input">Strings to be modified</param>
            /// <returns>List of modified strings if they contained a slash, original strings otherwise</returns>
            internal List<string> ReplaceSlashForFakeSlash(List<string> input)
            {
                m_ModifiedProjectNames.Clear();

                if (input == null)
                {
                    return null;
                }

                for (int i = 0; i < input.Count; i++)
                {
                    if (input[i].Contains("/"))
                    {
                        input[i] = input[i].Replace("/", k_FakeSlashUnicode);
                        m_ModifiedProjectNames.Add(input[i]);
                    }
                }

                return input;
            }
        }

        internal struct ProjectBindState
        {
            public bool bound;
            public string projectName;
        }

        static ProjectBindManager()
        {
            k_AnyRoleFilter = new List<string>(new[] { k_RoleOwner, k_RoleManager, k_RoleUser });
            k_AtLeastManagerFilter = new List<string>(new[] { k_RoleOwner, k_RoleManager });
        }

        /// <summary>
        /// Configures a new Project Bind manager to this within an existing EditorWindow
        /// </summary>
        /// <param name="rootVisualElement">visual element where the project bind content must be added</param>
        public ProjectBindManager(VisualElement rootVisualElement)
        {
            InitializeProjectBindManager(rootVisualElement);
        }

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_CurrentRequest?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ProjectBindManager()
        {
            Unsubscribe();
            Dispose(false);
        }

        void InitializeProjectBindManager(VisualElement rootVisualElement)
        {
            m_LastReuseBlockProject.LastProjectName = L10n.Tr(k_SelectProjectText);
            rootVisualElement.AddStyleSheetPath(k_ProjectBindCommonStyleSheetPath);
            rootVisualElement.viewDataKey = k_RootDataKey;
            rootVisualElement.AddStyleSheetPath(EditorGUIUtility.isProSkin ? k_ProjectBindDarkStyleSheetPath : k_ProjectBindLightStyleSheetPath);
            var projectBindTemplate = EditorGUIUtility.Load(k_ProjectBindTemplatePath) as VisualTreeAsset;
            rootVisualElement.Add(projectBindTemplate.CloneTree().contentContainer);

            QueryAndPrepareUXMLElements(rootVisualElement);

            Subscribe();

            InitializeOrganizationsDropdown();
            InitializeProjectsDropdown();

            FetchOrganizations();

            EditorGameServicesAnalytics.SendProjectBindDisplayEvent();
        }

        void QueryAndPrepareUXMLElements(VisualElement rootVisualElement)
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

        void Subscribe()
        {
            ProjectsFetched += OnProjectsFetched;
            OrganizationsFetched += OnOrganizationsFetched;
            m_CreateProjectButton.clicked += OnCreateProjectButtonClicked;
            m_LinkCloudProjectButton.clicked += OnLinkProjectButtonClicked;
            m_RefreshButton.clicked += OnRefreshButtonClicked;
        }

        void Unsubscribe()
        {
            UnityConnect.instance.ProjectStateChanged -= OnProjectStateChangedAfterRebind;
            UnityConnect.instance.ProjectStateChanged -= OnProjectStateChangedAfterCreation;
            ProjectsFetched -= OnProjectsFetched;
            OrganizationsFetched -= OnOrganizationsFetched;
            m_CreateProjectButton.clicked -= OnCreateProjectButtonClicked;
            m_LinkCloudProjectButton.clicked -= OnLinkProjectButtonClicked;
            m_RefreshButton.clicked -= OnRefreshButtonClicked;
        }

        void InitializeOrganizationsDropdown()
        {
            m_OrganizationsDropdown.choices = new List<string>{L10n.Tr(k_SelectOrganizationText)};
            m_OrganizationsDropdown.SetValueWithoutNotify(L10n.Tr(k_SelectOrganizationText));

            m_OrganizationsDropdown.RegisterValueChangedCallback(OnOrganizationsDropdownSelectedValueChanged);
        }

        void InitializeProjectsDropdown()
        {
            m_ProjectsDropdown.choices = new List<string>{L10n.Tr(k_SelectProjectText)};
            m_ProjectsDropdown.SetValueWithoutNotify(L10n.Tr(k_SelectProjectText));

            m_ProjectsDropdown.RegisterValueChangedCallback(OnProjectsDropdownSelectedValueChanged);
        }

        void DeactivateDropdownsAndButtons()
        {
            m_OrganizationsDropdown.SetEnabled(false);
            m_CreateProjectButton.SetEnabled(false);
            m_ProjectsDropdown.SetEnabled(false);
            m_LinkCloudProjectButton.SetEnabled(false);
            m_RefreshButton.SetEnabled(false);
        }

        void RefreshDropdownAndButtonStates()
        {
            if (m_FetchingProjects || m_FetchingOrganizations)
                return;

            m_OrganizationsDropdown.SetEnabled(m_OrganizationsDropdown.choices !=
                                               new List<string>() {L10n.Tr(k_SelectOrganizationText)});

            var isManager = false;

            foreach (var org in m_OrgIdByName)
            {
                if (org.Name == m_OrganizationsDropdown.value)
                {
                    isManager = org.IsManager;
                }
            }

            m_CreateProjectButton.SetEnabled(m_OrganizationsDropdown.value != L10n.Tr(k_SelectOrganizationText) &&
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
        }

        void OnOrganizationsFetched(List<string> organizationNames)
            => UpdateOrganizationsDropdown(organizationNames);

        void OnProjectsFetched(List<string> projectNames)
            => UpdateProjectsDropdown(projectNames);

        void UpdateOrganizationsDropdown(List<string> organizationNames)
        {
            m_OrganizationsDropdown.choices = organizationNames;
            m_FetchingOrganizations = false;
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

            lock (m_ButtonsLock)
            {
                RefreshDropdownAndButtonStates();
            }
        }

        void UpdateProjectsDropdown(List<string> projectNames)
        {
            m_ProjectsDropdown.choices = projectNames;
            m_FetchingProjects = false;
            m_ProjectsDropdown.SetValueWithoutNotify("");

            if (m_ProjectsDropdown.choices.Contains(m_LastReuseBlockProject.LastProjectName))
            {
                m_ProjectsDropdown.value = m_LastReuseBlockProject.LastProjectName;
            }
            else
            {
                m_ProjectsDropdown.value = L10n.Tr(k_SelectProjectText);
            }

            lock (m_ButtonsLock)
            {
                RefreshDropdownAndButtonStates();
            }
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

        void OnOrganizationsDropdownSelectedValueChanged(ChangeEvent<string> evt)
        {
            if (m_OrganizationsDropdown.value == L10n.Tr(k_SelectOrganizationText))
            {
                m_ProjectsDropdown.choices = new List<string>() { L10n.Tr(k_SelectProjectText) };
                m_ProjectsDropdown.value = L10n.Tr(k_SelectProjectText);
            }
            else
            {
                FetchProjects();
            }

            lock (m_ButtonsLock)
            {
                RefreshDropdownAndButtonStates();
            }
        }

        void OnProjectsDropdownSelectedValueChanged(ChangeEvent<string> evt)
        {
            m_LastReuseBlockProject.LastProjectName = evt.newValue;

            lock (m_ButtonsLock)
            {
                RefreshDropdownAndButtonStates();
            }
        }

        void OnRefreshButtonClicked()
            => FetchOrganizations();

        void OnCreateProjectButtonClicked()
        {
            if (m_OrganizationsDropdown.value != L10n.Tr(k_SelectOrganizationText))
            {
                m_CreateIteration = 0;
                RequestCreateOperation();
            }
        }

        void OnLinkProjectButtonClicked()
        {
            if (L10n.Tr(k_SelectProjectText) != m_LastReuseBlockProject.LastProjectName)
            {
                DeactivateDropdownsAndButtons();
                var abort = false;
                var projectInfo = m_ProjectInfoByName[m_LastReuseBlockProject.LastProjectName];
                if (EditorUtility.DisplayDialog(L10n.Tr(k_LinkProjectWindowTitle),
                        string.Format(L10n.Tr(k_DialogConfirmationMessage), projectInfo.name, projectInfo.organizationName),
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
                RefreshDropdownAndButtonStates();
            }
        }

        /// <summary>
        /// To attach a project an existing project, we must collect all orgs the current user is a member of.
        /// In addition the current user may be a guest of a specific project, in which case we must also look at
        /// all projects to find organizations.
        /// </summary>
        void FetchOrganizations()
        {
            DeactivateDropdownsAndButtons();
            m_FetchingOrganizations = true;
            ServicesConfiguration.instance.RequestCurrentUserApiUrl(currentUserApiUrl =>
            {
                var getOrganizationsRequest = new UnityWebRequest(currentUserApiUrl + "?include=orgs,projects",
                    UnityWebRequest.kHttpVerbGET) { downloadHandler = new DownloadHandlerBuffer() };
                getOrganizationsRequest.suppressErrorsToConsole = true;
                getOrganizationsRequest.SetRequestHeader("AUTHORIZATION", $"Bearer {UnityConnect.instance.GetUserInfo().accessToken}");
                var operation = getOrganizationsRequest.SendWebRequest();
                operation.completed += op =>
                {
                    try
                    {
                        if (ServicesUtils.IsUnityWebRequestReadyForJsonExtract(getOrganizationsRequest))
                        {
                            var jsonParser = new JSONParser(getOrganizationsRequest.downloadHandler.text);
                            var json = jsonParser.Parse();
                            try
                            {
                                m_OrgIdByName.Clear();
                                var sortedOrganizationNames = new List<string>();
                                foreach (var rawOrg in json.AsDict()[k_JsonOrgsNodeName].AsList())
                                {
                                    var org = rawOrg.AsDict();
                                    if (k_AnyRoleFilter.Contains(org[k_JsonRoleNodeName].AsString()))
                                    {
                                        sortedOrganizationNames.Add(org[k_JsonNameNodeName].AsString());
                                        m_OrgIdByName.Add(new OrgData
                                        {
                                            Name = org[k_JsonNameNodeName].AsString(),
                                            Id = org[k_JsonIdNodeName].AsString(),
                                            IsManager = k_AtLeastManagerFilter.Contains(org[k_JsonRoleNodeName].AsString())
                                        });

                                    }
                                }

                                foreach (var rawProject in json.AsDict()[k_JsonProjectsNodeName].AsList())
                                {
                                    var project = rawProject.AsDict();
                                    if (!project[k_JsonArchivedNodeName].AsBool()
                                        && !sortedOrganizationNames.Contains(project[k_JsonOrgNameNodeName].AsString()))
                                    {
                                        sortedOrganizationNames.Add(project[k_JsonOrgNameNodeName].AsString());

                                        m_OrgIdByName.Add(new OrgData()
                                        {
                                            Name = project[k_JsonOrgNameNodeName].AsString(),
                                            Id = project[k_JsonOrgIdNodeName].AsString(),
                                            IsManager = false
                                        });
                                    }
                                }

                                sortedOrganizationNames.Sort();
                                var popUpChoices = new List<string> { L10n.Tr(k_SelectOrganizationText) };
                                popUpChoices.AddRange(sortedOrganizationNames);

                                OrganizationsFetched?.Invoke(popUpChoices);
                            }
                            catch (Exception ex)
                            {
                                m_FetchingOrganizations = false;
                                RefreshDropdownAndButtonStates();
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
                        else
                        {
                            var ex = new UnityConnectWebRequestException(L10n.Tr(k_CouldNotObtainOrganizationsMessage))
                            {
                                error = getOrganizationsRequest.error,
                                method = getOrganizationsRequest.method,
                                timeout = getOrganizationsRequest.timeout,
                                url = getOrganizationsRequest.url,
                                responseHeaders = getOrganizationsRequest.GetResponseHeaders(),
                                responseCode = getOrganizationsRequest.responseCode,
                                isHttpError = (getOrganizationsRequest.result == UnityWebRequest.Result.ProtocolError),
                                isNetworkError = (getOrganizationsRequest.result == UnityWebRequest.Result.ConnectionError),
                            };
                            if (exceptionCallback != null)
                            {
                                exceptionCallback.Invoke(ex);
                            }
                            else
                            {
                                //If there is no exception callback, we have to at least log it
                                Debug.LogException(ex);
                            }
                            m_FetchingOrganizations = false;
                            RefreshDropdownAndButtonStates();
                        }
                    }
                    finally
                    {
                        getOrganizationsRequest.Dispose();
                    }
                };
            });
        }

        void FetchProjects()
        {
            DeactivateDropdownsAndButtons();
            m_FetchingProjects = true;
            var orgId = "";

            foreach (var org in m_OrgIdByName)
            {
                if (org.Name == m_OrganizationsDropdown.value)
                {
                    orgId = org.Id;
                }
            }

            ServicesConfiguration.instance.RequestOrganizationProjectsApiUrl(orgId, organizationProjectsApiUrl =>
            {
                var getProjectsRequest = new UnityWebRequest(organizationProjectsApiUrl,
                    UnityWebRequest.kHttpVerbGET) { downloadHandler = new DownloadHandlerBuffer() };
                getProjectsRequest.suppressErrorsToConsole = true;
                getProjectsRequest.SetRequestHeader("AUTHORIZATION", $"Bearer {UnityConnect.instance.GetUserInfo().accessToken}");
                var operation = getProjectsRequest.SendWebRequest();
                operation.completed += op =>
                {
                    try
                    {
                        if (ServicesUtils.IsUnityWebRequestReadyForJsonExtract(getProjectsRequest))
                        {
                            var jsonParser = new JSONParser(getProjectsRequest.downloadHandler.text);
                            var json = jsonParser.Parse();
                            try
                            {
                                m_ProjectInfoByName = new Dictionary<string, ProjectInfoData>();

                                var jsonProjects = json.AsDict()[k_JsonProjectsNodeName].AsList();
                                foreach (var jsonProject in jsonProjects)
                                {
                                    if (!jsonProject.AsDict()[k_JsonArchivedNodeName].AsBool())
                                    {
                                        var projectInfo = ExtractProjectInfoFromJson(jsonProject);
                                        m_ProjectInfoByName.Add(projectInfo.name, projectInfo);
                                    }
                                }

                                var projectNames = new List<string> { L10n.Tr(k_SelectProjectText) };
                                var sortedProjectNames = new List<string>(m_ProjectInfoByName.Keys);

                                // To work around the UI feature that creates sub-menus on slash characters, we
                                // replaces slashes in project names by a fake stylized slash. This hack could be
                                // removed in the future when we get a UI feature for it, or when dashboard will prevent
                                // users from inputting slashes in their project names.
                                sortedProjectNames =
                                    m_LastReuseBlockProject.ReplaceSlashForFakeSlash(sortedProjectNames);

                                sortedProjectNames.Sort();
                                projectNames.AddRange(sortedProjectNames);

                                ProjectsFetched?.Invoke(projectNames);
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
                                m_FetchingProjects = false;
                                RefreshDropdownAndButtonStates();
                            }
                        }
                        else
                        {
                            var ex = new UnityConnectWebRequestException(L10n.Tr(k_CouldNotObtainProjectMessage))
                            {
                                error = getProjectsRequest.error,
                                method = getProjectsRequest.method,
                                timeout = getProjectsRequest.timeout,
                                url = getProjectsRequest.url,
                                responseHeaders = getProjectsRequest.GetResponseHeaders(),
                                responseCode = getProjectsRequest.responseCode,
                                isHttpError = (getProjectsRequest.result == UnityWebRequest.Result.ProtocolError),
                                isNetworkError = (getProjectsRequest.result == UnityWebRequest.Result.ConnectionError),
                            };
                            if (exceptionCallback != null)
                            {
                                exceptionCallback.Invoke(ex);
                            }
                            else
                            {
                                //If there is no exception callback, we have to at least log it
                                Debug.LogException(ex);
                            }

                            m_FetchingProjects = false;
                            RefreshDropdownAndButtonStates();
                        }
                    }
                    finally
                    {
                        getProjectsRequest.Dispose();
                    }
                };
            });

        }

        void RequestCreateOperation()
        {
            DeactivateDropdownsAndButtons();

            var orgId = "";

            foreach (var org in m_OrgIdByName)
            {
                if (org.Name == m_OrganizationsDropdown.value)
                {
                    orgId = org.Id;
                }
            }

            ServicesConfiguration.instance.RequestOrganizationProjectsApiUrl(orgId, organizationProjectsApiUrl =>
            {
                var payload = $"{{\"name\":\"{Application.productName + GetProjectNameSuffix()}\", \"active\":true}}";
                var uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
                m_CurrentRequest = new UnityWebRequest(organizationProjectsApiUrl, UnityWebRequest.kHttpVerbPOST)
                { downloadHandler = new DownloadHandlerBuffer(), uploadHandler = uploadHandler};
                m_CurrentRequest.suppressErrorsToConsole = true;
                m_CurrentRequest.SetRequestHeader("AUTHORIZATION", $"Bearer {UnityConnect.instance.GetUserInfo().accessToken}");
                m_CurrentRequest.SetRequestHeader("Content-Type", "application/json;charset=UTF-8");
                var operation = m_CurrentRequest.SendWebRequest();
                operation.completed += CreateOperationOnCompleted;
            });
        }

        string GetProjectNameSuffix()
        {
            return m_CreateIteration > 0 ? $" ({m_CreateIteration})" : string.Empty;
        }

        void CreateOperationOnCompleted(AsyncOperation obj)
        {
            if (m_CurrentRequest == null)
            {
                RefreshDropdownAndButtonStates();
                //If we lost our m_CurrentRequest request reference, we can't risk doing anything.
                return;
            }

            if (ServicesUtils.IsUnityWebRequestReadyForJsonExtract(m_CurrentRequest))
            {
                var jsonParser = new JSONParser(m_CurrentRequest.downloadHandler.text);
                var json = jsonParser.Parse();
                var abort = false;
                try
                {
                    var projectInfo = ExtractProjectInfoFromJson(json);
                    try
                    {
                        ServicesRepository.DisableAllServices(shouldUpdateApiFlag: false);
                        //Only register before creation. Remove first in case it was already added.
                        //TODO: Review to avoid dependency on project refreshed
                        UnityConnect.instance.ProjectStateChanged -= OnProjectStateChangedAfterCreation;
                        UnityConnect.instance.ProjectStateChanged += OnProjectStateChangedAfterCreation;
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
                        createButtonCallback?.Invoke(projectInfo);
                    }
                }
                finally
                {
                    m_CurrentRequest?.Dispose();
                    m_CurrentRequest = null;
                }

                RefreshDropdownAndButtonStates();
            }
            else if (m_CurrentRequest.responseCode == k_HttpStatusCodeUnprocessableEntity)
            {
                m_CurrentRequest?.Dispose();
                m_CurrentRequest = null;
                m_CreateIteration++;
                RequestCreateOperation();
            }
            else
            {
                try
                {
                    var ex = new UnityConnectWebRequestException(L10n.Tr(k_CouldNotCreateProjectMessage))
                    {
                        error = m_CurrentRequest.error,
                        method = m_CurrentRequest.method,
                        timeout = m_CurrentRequest.timeout,
                        url = m_CurrentRequest.url,
                        responseHeaders = m_CurrentRequest.GetResponseHeaders(),
                        responseCode = m_CurrentRequest.responseCode,
                        isHttpError = (m_CurrentRequest.result == UnityWebRequest.Result.ProtocolError),
                        isNetworkError = (m_CurrentRequest.result == UnityWebRequest.Result.ConnectionError),
                    };
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
                finally
                {
                    m_CurrentRequest?.Dispose();
                    m_CurrentRequest = null;
                }

                RefreshDropdownAndButtonStates();
            }
        }

        void BindProject(ProjectInfoData projectInfo)
        {
            UnityConnect.instance.BindProject(projectInfo.guid, projectInfo.name, projectInfo.organizationId);
            EditorAnalytics.SendProjectServiceBindingEvent(new ProjectBindState() { bound = true, projectName = projectInfo.name });
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
                ServicesRepository.SyncServicesOnProjectRebind();
            }
        }

        static ProjectInfoData ExtractProjectInfoFromJson(JSONValue jsonProject)
        {
            return new ProjectInfoData(
                jsonProject.AsDict()[k_JsonOrgIdNodeName].AsString(),
                jsonProject.AsDict()[k_JsonOrgNameNodeName].AsString(),
                jsonProject.AsDict()[k_JsonNameNodeName].AsString(),
                jsonProject.AsDict()[k_JsonGuidNodeName].AsString(),
                jsonProject.AsDict()[k_JsonIdNodeName].AsString()
            );
        }

        public delegate void CreateButtonCallback(ProjectInfoData projectInfoData);

        public delegate void LinkButtonCallback(ProjectInfoData projectInfoData);

        public delegate void ExceptionCallback(Exception exception);
    }

    internal class ProjectInfoData
    {
        public string organizationId { get; }
        public string organizationName { get; }
        public string name { get; }
        public string guid { get; }

        public string projectId { get; }

        public ProjectInfoData(string organizationId, string organizationName, string name, string guid, string projectId)
        {
            this.guid = guid;
            this.name = name;
            this.organizationId = organizationId;
            this.organizationName = organizationName;
            this.projectId = projectId;
        }
    }
}
