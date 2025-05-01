// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class ScopedRegistriesSettings : VisualElement
    {
        private const string k_NewRegistryClass = "newRegistry";
        private const string k_SelectedScopeClass = "selectedScope";

        private string k_EditRegistryName = L10n.Tr("Edit Registry Name");
        private string k_EditRegistryUrl = L10n.Tr("Edit Registry URL");
        private string k_EditRegistryScopes = L10n.Tr("Edit Registry Scopes");
        private string k_AddNewRegistryDraft = L10n.Tr("Add New Registry Draft");
        private string k_RemoveRegistry = L10n.Tr("Remove registry");
        private string k_RegistrySelectionChange = L10n.Tr("Registry Selection Change");

        [Serializable]
        internal new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new ScopedRegistriesSettings();
        }
        private Dictionary<string, RegistryItem> m_ExistingRegistryItems = new Dictionary<string, RegistryItem>();
        internal IReadOnlyDictionary<string, RegistryItem> registryItems => m_ExistingRegistryItems;

        private readonly RegistryItem m_NewScopedRegistryItem;

        internal RegistryInfoDraft draft => m_SettingsProxy.registryInfoDraft;

        private IResourceLoader m_ResourceLoader;
        private IProjectSettingsProxy m_SettingsProxy;
        private IApplicationProxy m_ApplicationProxy;
        private IUpmCache m_UpmCache;
        private IUpmRegistryClient m_UpmRegistryClient;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<IResourceLoader>();
            m_SettingsProxy = container.Resolve<IProjectSettingsProxy>();
            m_ApplicationProxy = container.Resolve<IApplicationProxy>();
            m_UpmCache = container.Resolve<IUpmCache>();
            m_UpmRegistryClient = container.Resolve<IUpmRegistryClient>();
        }

        public ScopedRegistriesSettings()
        {
            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("ScopedRegistriesSettings.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            scopedRegistriesInfoBox.readMoreUrl =
                $"https://docs.unity3d.com/{m_ApplicationProxy.shortUnityVersion}/Documentation/Manual/upm-scoped.html#security";

            applyRegistriesButton.clickable.clicked += ApplyChanges;
            revertRegistriesButton.clickable.clicked += RevertChanges;
            registryNameTextField.RegisterValueChangedCallback(OnRegistryNameChanged);
            registryUrlTextField.RegisterValueChangedCallback(OnRegistryUrlChanged);

            // We pre-create the new scoped registry item, as otherwise it would be re-created each time we refresh the list
            m_NewScopedRegistryItem = new RegistryItem(null).SetLabelClick(() => OnRegistryItemClicked(null));
            m_NewScopedRegistryItem.AddToClassList(k_NewRegistryClass);

            addRegistryButton.clickable.clicked += AddRegistryClicked;
            removeRegistryButton.clickable.clicked += RemoveRegistryClicked;

            addScopeButton.clickable.clicked += AddScopeClicked;
            removeScopeButton.clickable.clicked += RemoveScopeClicked;

            m_UpmRegistryClient.onRegistriesModified += OnRegistriesModified;
            m_UpmRegistryClient.onRegistryOperationError += OnRegistryOperationError;
            Undo.undoRedoEvent -= OnUndoRedoPerformed;
            Undo.undoRedoEvent += OnUndoRedoPerformed;

            // on domain reload, it's not guaranteed that the settings have
            //  reloaded the draft object yet- need to wait and do this when
            //  initialization has finished
            if (draft.IsReady())
            {
                UpdateRegistryList();
                UpdateRegistryDetails();
            }
            else
            {
                m_SettingsProxy.onInitializationFinished += OnSettingsInitialized;
            }
        }

        private void OnUndoRedoPerformed(in UndoRedoInfo info)
        {
            if (EditorWindow.HasOpenInstances<ProjectSettingsWindow>())
            {
                draft.SetModifiedAfterUndo();
                // check if the old state makes sense- does it still exist in the list of registries
                //  if not, put it into a new draft, since it was deleted at some point prior
                if (!string.IsNullOrEmpty(draft.original?.name) && !m_SettingsProxy.scopedRegistries.Any(a => a.name == draft.original?.name))
                {
                    draft.SetOriginalRegistryInfo(null, true);
                    m_SettingsProxy.isUserAddingNewScopedRegistry = true;
                }

                UpdateRegistryList();
                UpdateRegistryDetails();
                RefreshScopeSelection();
                RefreshApplyAndRevertButtons();
            }
        }

        private void OnSettingsInitialized()
        {
            UpdateRegistryList();
            UpdateRegistryDetails();
        }

        private void AddRegistryClicked()
        {
            if (draft.original is null)
                return;

            if (!ShowUnsavedChangesDialog())
                return;

            draft.RegisterWithOriginalOnUndo(k_AddNewRegistryDraft);

            m_SettingsProxy.isUserAddingNewScopedRegistry = true;
            draft.SetOriginalRegistryInfo(null);
            UpdateRegistryList();
            UpdateRegistryDetails();
            registryNameTextField.Q("unity-text-input").Focus();
        }

        private bool AnyPackageInstalledFromRegistry(string registryName)
        {
            if (string.IsNullOrEmpty(registryName))
                return false;
            if (!m_UpmCache.installedPackageInfos.Any())
                m_UpmCache.SetInstalledPackageInfos(PackageInfo.GetAllRegisteredPackages());
            return m_UpmCache.installedPackageInfos.Any(p => p.registry?.name == registryName);
        }

        private void RemoveRegistryClicked()
        {
            if (draft.original is not null)
            {
                bool deleteRegistry;
                if (AnyPackageInstalledFromRegistry(draft.original.name))
                {
                    var message = L10n.Tr("There are packages in your project that are from this scoped registry, deleting the scoped registry might break these packages, are you sure you want to continue?");
                    deleteRegistry = m_ApplicationProxy.isBatchMode || m_ApplicationProxy.DisplayDialog("deleteScopedRegistryWithInstalledPackages", L10n.Tr("Deleting a scoped registry"), message, L10n.Tr("Delete anyway"), L10n.Tr("Cancel"));
                }
                else
                {
                    var message = L10n.Tr("You are about to delete a scoped registry, are you sure you want to continue?");
                    deleteRegistry = m_ApplicationProxy.isBatchMode || m_ApplicationProxy.DisplayDialog("deleteScopedRegistry", L10n.Tr("Deleting a scoped registry"), message, L10n.Tr("OK"), L10n.Tr("Cancel"));
                }


                if (deleteRegistry)
                {
                    draft.RegisterWithOriginalOnUndo(k_RemoveRegistry);
                    m_UpmRegistryClient.RemoveRegistry(draft.original.name);
                }
            }
            else
            {
                if (!ShowUnsavedChangesDialog())
                    return;

                m_SettingsProxy.isUserAddingNewScopedRegistry = false;
                RevertChanges();
            }
        }

        private void AddScopeClicked()
        {
            var textField = CreateScopeTextField();
            scopesList.Add(textField);
            textField.Q("unity-text-input").Focus();
            removeScopeButton.SetEnabled(scopesList.childCount > 1);
            OnRegistryScopesChanged();
            RefreshScopeSelection();
        }

        private void RemoveScopeClicked()
        {
            if (scopesList.childCount <= 1)
                return;

            if (draft.selectedScopeIndex >= 0 && draft.selectedScopeIndex < scopesList.childCount)
            {
                scopesList.RemoveAt(draft.selectedScopeIndex);
                removeScopeButton.SetEnabled(scopesList.childCount > 1);
                OnRegistryScopesChanged();
                RefreshScopeSelection();
            }
        }

        private void ApplyChanges()
        {
            CheckRegistryDraftCompliance(
               successCallback: registryInfo =>
               {
                   if (registryInfo.compliance.status == RegistryComplianceStatus.NonCompliant)
                   {
                       var violation = registryInfo.compliance.violations[0];
                       if (m_ApplicationProxy.DisplayDialog("nonCompliantRegistry",
                           L10n.Tr("Restricted registry"),
                           string.Format(L10n.Tr("The provider must revise this registry to comply with Unity's Terms of Service. Contact the provider for further assistance. {0}"), violation.message),
                           L10n.Tr("Read More"), L10n.Tr("Close")))
                           m_ApplicationProxy.OpenURL(violation.readMoreLink);

                       return;
                   }

                   if (draft.isUrlOrScopesUpdated && AnyPackageInstalledFromRegistry(draft.original.name) &&
                       !m_ApplicationProxy.DisplayDialog("updateScopedRegistry",
                           L10n.Tr("Updating a scoped registry"),
                           L10n.Tr("There are packages in your project that are from this scoped registry, updating the URL or the scopes could result in errors in your project. Are you sure you want to continue?"),
                           L10n.Tr("OK"), L10n.Tr("Cancel")))
                       return;

                   if (draft.Validate())
                   {
                       if (draft.original is not null)
                           m_UpmRegistryClient.UpdateRegistry(draft.original.name, draft.name, draft.url, draft.sanitizedScopes.ToArray());
                       else
                           m_UpmRegistryClient.AddRegistry(draft.name, draft.url, draft.sanitizedScopes.ToArray());
                   }
                   else
                   {
                       RefreshErrorBox();
                   }
               },
               errorCallback: error =>
               {
                   draft.errorMessage = error.message;
                   RefreshErrorBox();
               }
            );
        }

        private void CheckRegistryDraftCompliance(Action<RegistryInfo> successCallback = null, Action<UIError> errorCallback = null)
        {
            if (draft.original is not null)
                m_UpmRegistryClient.UpdateRegistryDryRun(draft.original.name, draft.name, draft.url, draft.sanitizedScopes.ToArray(), successCallback, errorCallback);
            else
                m_UpmRegistryClient.AddRegistryDryRun(draft.name, draft.url, draft.sanitizedScopes.ToArray(), successCallback, errorCallback);
        }

        private void RevertChanges()
        {
            draft.RevertChanges();
            RefreshDraftRegistryItem();
            if (draft.original is null)
            {
                var lastScopedRegistry = m_SettingsProxy.scopedRegistries.LastOrDefault();
                if (lastScopedRegistry != null)
                {
                    m_SettingsProxy.SelectRegistry(lastScopedRegistry.name);
                    m_SettingsProxy.isUserAddingNewScopedRegistry = false;
                }
            }
            UpdateRegistryList();
            UpdateRegistryDetails();
        }

        private void OnRegistryNameChanged(ChangeEvent<string> evt)
        {
            draft.RegisterOnUndo(k_EditRegistryName);
            draft.name = evt.newValue;
            RefreshApplyAndRevertButtons();
            RefreshDraftRegistryItem();
        }

        private void OnRegistryUrlChanged(ChangeEvent<string> evt)
        {
            draft.RegisterOnUndo(k_EditRegistryUrl);
            draft.url = evt.newValue;
            RefreshApplyAndRevertButtons();
            RefreshDraftRegistryItem();
        }

        private void OnRegistryScopesChanged(ChangeEvent<string> evt = null)
        {
            draft.RegisterOnUndo(k_EditRegistryScopes);
            draft.SetScopes(scopesList.Children().Cast<TextField>().Select(textField => textField.value));
            RefreshApplyAndRevertButtons();
            RefreshDraftRegistryItem();
        }

        private void OnRegistryItemClicked(string registryName)
        {
            if (draft.original?.name == registryName)
                return;

            if (!ShowUnsavedChangesDialog())
                return;

            draft.RegisterWithOriginalOnUndo(k_RegistrySelectionChange);
            m_SettingsProxy.SelectRegistry(registryName);

            RefreshDraftRegistryItem();
            RefreshRegistryItemSelections();
            RefreshAddAndRemoveRegistryButtons();
            UpdateRegistryDetails();
        }

        private void OnRegistriesModified()
        {
            UpdateRegistryList();
            UpdateRegistryDetails();
            RefreshScopeSelection();
        }

        private void OnRegistryOperationError(string registryName, UIError error)
        {
            if ((draft.original?.name ?? draft.name) == registryName)
            {
                draft.errorMessage = error.message;
                RefreshErrorBox();
            }
        }

        // Returns false if there are unsaved changes and the user choose to cancel the current operation
        // Otherwise returns true
        private bool ShowUnsavedChangesDialog()
        {
            if (!draft.hasUnsavedChanges)
                return true;

            var discardChanges = m_ApplicationProxy.isBatchMode || m_ApplicationProxy.DisplayDialog("discardUnsavedRegistryChanges", L10n.Tr("Discard unsaved changes"),
                L10n.Tr("You have unsaved changes which would be lost if you continue this operation. Do you want to continue and discard unsaved changes?"),
                L10n.Tr("Continue"), L10n.Tr("Cancel"));

            if (discardChanges)
                RevertChanges();

            return discardChanges;
        }

        private TextField CreateScopeTextField(string scope = null)
        {
            var scopeTextField = new TextField();
            if (!string.IsNullOrEmpty(scope))
                scopeTextField.value = scope;
            scopeTextField.RegisterValueChangedCallback(OnRegistryScopesChanged);
            scopeTextField.RegisterCallback<FocusEvent>(OnScopeTextFieldFocus);
            return scopeTextField;
        }

        private void OnScopeTextFieldFocus(FocusEvent e)
        {
            var textField = e.target as TextField;
            if (textField != null)
            {
                var previousIndex = draft.selectedScopeIndex;
                var scopeIndex = textField.parent.IndexOf(textField);
                if (previousIndex == scopeIndex)
                    return;
                draft.selectedScopeIndex = scopeIndex;
                RefreshScopeSelection();
            }
        }

        private void RefreshScopeSelection()
        {
            var selectedIndex = draft.selectedScopeIndex;
            selectedIndex = Math.Min(Math.Max(0, selectedIndex), scopesList.childCount - 1);
            draft.selectedScopeIndex = selectedIndex;
            for (var i = 0; i < scopesList.childCount; i++)
                scopesList[i].EnableInClassList(k_SelectedScopeClass, i == selectedIndex);
        }

        internal void UpdateRegistryList()
        {
            registriesList.Clear();
            m_ExistingRegistryItems.Clear();

            foreach (var registryInfo in m_SettingsProxy.scopedRegistries)
            {
                if (m_ExistingRegistryItems.ContainsKey(registryInfo.name))
                {
                    // Workaround because registries are keyed by registryInfo.name rather than registryInfo.id.
                    // Without this, the UI just fails to render and there's an uncaught
                    // ArgumentException error logged in the console due to a duplicate key error
                    // thrown by m_RegistryLabels.Add below.
                    UnityEngine.Debug.LogWarning(
                        string.Format(
                            L10n.Tr("Unable to display a scoped registry named {0} defined in a UPM configuration file: an existing scoped registry has the same name in your project manifest. Rename one of the conflicting scoped registries if you want to see them all in the Scoped Registry list."),
                            registryInfo.name)
                    );
                    continue;
                }

                var registryItem = new RegistryItem(registryInfo).SetLabelClick(() => OnRegistryItemClicked(registryInfo.name));
                m_ExistingRegistryItems.Add(registryInfo.name, registryItem);
                registriesList.Add(registryItem);
            }
            registriesList.Add(m_NewScopedRegistryItem);

            RefreshDraftRegistryItem();
            RefreshRegistryItemSelections();
            RefreshAddAndRemoveRegistryButtons();
        }

        private void UpdateRegistryDetails()
        {
            registriesRightContainer.SetEnabled(canEditSelectedRegistry);

            registryNameTextField.SetValueWithoutNotify(draft.name ?? string.Empty);
            registryUrlTextField.SetValueWithoutNotify(draft.url ?? string.Empty);

            scopesList.Clear();
            foreach (var scope in draft.scopes)
                scopesList.Add(CreateScopeTextField(scope));
            if (scopesList.childCount > 0)
                RefreshScopeSelection();
            removeScopeButton.SetEnabled(scopesList.childCount > 1);

            RefreshApplyAndRevertButtons();
            RefreshErrorBox();
            RefreshNonCompliantStatus();
        }

        private void RefreshApplyAndRevertButtons()
        {
            var isAddNewRegistry = draft.original is null;
            var hasUnsavedChanges = draft.hasUnsavedChanges;

            revertRegistriesButton.text = isAddNewRegistry ? L10n.Tr("Cancel") : L10n.Tr("Revert");
            revertRegistriesButton.SetEnabled(isAddNewRegistry || hasUnsavedChanges);

            applyRegistriesButton.text = isAddNewRegistry ? L10n.Tr("Save") : L10n.Tr("Apply");
            applyRegistriesButton.SetEnabled(hasUnsavedChanges);
        }

        private void RefreshDraftRegistryItem()
        {
            if (draft.original is null)
                m_NewScopedRegistryItem.RefreshDraft(draft);
            else
                m_ExistingRegistryItems.GetValueOrDefault(draft.original.name)?.RefreshDraft(draft);
            UIUtils.SetElementDisplay(m_NewScopedRegistryItem, draft.original is null || m_SettingsProxy.isUserAddingNewScopedRegistry);
        }

        private void RefreshRegistryItemSelections()
        {
            var selectedRegistryName = draft.original?.name;
            foreach (var (registryName, registryItem) in m_ExistingRegistryItems)
                registryItem.SetSelected(registryName == selectedRegistryName);
            m_NewScopedRegistryItem.SetSelected(draft.original is null);
        }

        private void RefreshAddAndRemoveRegistryButtons()
        {
            addRegistryButton.SetEnabled(draft.original is not null);
            removeRegistryButton.SetEnabled(canEditSelectedRegistry && m_ExistingRegistryItems.Count > 0);
        }

        private void RefreshErrorBox()
        {
            scopedRegistryErrorBox.text = draft.errorMessage;
            UIUtils.SetElementDisplay(scopedRegistryErrorBox, !string.IsNullOrEmpty(scopedRegistryErrorBox.text));
        }

        private void RefreshNonCompliantStatus()
        {
            var isDraftNonCompliant = draft.original?.compliance.status == RegistryComplianceStatus.NonCompliant;
            UIUtils.SetElementDisplay(scopedRegistryNonCompliantErrorBox, isDraftNonCompliant);
            if (!isDraftNonCompliant)
                return;

            var violation = draft.original.compliance.violations[0];
            scopedRegistryNonCompliantErrorBox.text = string.Format(
                L10n.Tr("The provider must revise this registry to comply with Unity's Terms of Service. Contact the provider for further assistance. {0}"),
                violation.message);
            scopedRegistryNonCompliantErrorBox.readMoreUrl = violation.readMoreLink;
        }

        private VisualElementCache cache { get; }

        // Disallow editing existing registries defined in User or Global UPM configuration files for now
        private bool canEditSelectedRegistry =>  draft.original is null || draft.original.configSource == ConfigSource.Project;

        private HelpBoxWithOptionalReadMore scopedRegistriesInfoBox => cache.Get<HelpBoxWithOptionalReadMore>("scopedRegistriesInfoBox");
        private HelpBox scopedRegistryErrorBox => cache.Get<HelpBox>("scopedRegistryErrorBox");
        internal HelpBoxWithOptionalReadMore scopedRegistryNonCompliantErrorBox => cache.Get<HelpBoxWithOptionalReadMore>("scopedRegistryNonCompliantErrorBox");
        internal VisualElement registriesList => cache.Get<VisualElement>("registriesList");
        internal VisualElement registriesRightContainer => cache.Get<VisualElement>("registriesRightContainer");
        internal TextField registryNameTextField => cache.Get<TextField>("registryNameTextField");
        internal TextField registryUrlTextField => cache.Get<TextField>("registryUrlTextField");
        internal VisualElement scopesList => cache.Get<VisualElement>("scopesList");
        internal Button addRegistryButton => cache.Get<Button>("addRegistryButton");
        internal Button removeRegistryButton => cache.Get<Button>("removeRegistryButton");
        internal Button addScopeButton => cache.Get<Button>("addScopeButton");
        internal Button removeScopeButton => cache.Get<Button>("removeScopeButton");
        internal Button revertRegistriesButton => cache.Get<Button>("revertRegistriesButton");
        internal Button applyRegistriesButton => cache.Get<Button>("applyRegistriesButton");
    }
}
