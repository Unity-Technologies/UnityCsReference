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
        private const string k_AddNewScopedRegistryText = "New Scoped Registry";
        private const string k_SelectedRegistryClass = "selectedRegistry";
        private const string k_NewRegistryClass = "newRegistry";
        private const string k_SelectedScopeClass = "selectedScope";

        internal new class UxmlFactory : UxmlFactory<ScopedRegistriesSettings> {}

        private Dictionary<string, Label> m_RegistryLabels = new Dictionary<string, Label>();
        private Label m_NewScopedRegistryLabel;

        private RegistryInfoDraft draft => m_SettingsProxy.registryInfoDraft;

        private ResourceLoader m_ResourceLoader;
        private PackageManagerProjectSettingsProxy m_SettingsProxy;
        private ApplicationProxy m_ApplicationProxy;
        private UpmCache m_UpmCache;
        private UpmRegistryClient m_UpmRegistryClient;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_SettingsProxy = container.Resolve<PackageManagerProjectSettingsProxy>();
            m_ApplicationProxy = container.Resolve<ApplicationProxy>();
            m_UpmCache = container.Resolve<UpmCache>();
            m_UpmRegistryClient = container.Resolve<UpmRegistryClient>();
        }

        public ScopedRegistriesSettings()
        {
            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("ScopedRegistriesSettings.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            scopedRegistriesInfoBox.Q<Button>().clickable.clicked += () =>
            {
                m_ApplicationProxy.OpenURL($"https://docs.unity3d.com/{m_ApplicationProxy.shortUnityVersion}/Documentation/Manual/upm-scoped.html");
            };
            applyRegistriesButton.clickable.clicked += ApplyChanges;
            revertRegistriesButton.clickable.clicked += RevertChanges;
            registryNameTextField.RegisterValueChangedCallback(OnRegistryNameChanged);
            registryUrlTextField.RegisterValueChangedCallback(OnRegistryUrlChanged);

            m_NewScopedRegistryLabel = new Label();
            m_NewScopedRegistryLabel.AddToClassList(k_NewRegistryClass);
            m_NewScopedRegistryLabel.OnLeftClick(() => OnRegistryLabelClicked(null));

            addRegistryButton.clickable.clicked += AddRegistryClicked;
            removeRegistryButton.clickable.clicked += RemoveRegistryClicked;

            addScopeButton.clickable.clicked += AddScopeClicked;
            removeScopeButton.clickable.clicked += RemoveScopeClicked;

            m_UpmRegistryClient.onRegistriesModified += OnRegistriesModified;
            m_UpmRegistryClient.onRegistryOperationError += OnRegistryOperationError;

            UpdateRegistryList();
            UpdateRegistryDetails();
        }

        private void AddRegistryClicked()
        {
            if (draft.original == null)
                return;

            if (!ShowUnsavedChangesDialog())
                return;

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
            if (draft.original != null)
            {
                if (AnyPackageInstalledFromRegistry(draft.original.name))
                {
                    EditorUtility.DisplayDialog(L10n.Tr("Cannot delete scoped registry"),
                        L10n.Tr("There are packages in your project that are from this scoped registry, please remove them before removing the scoped registry."), L10n.Tr("Ok"));
                    return;
                }

                var deleteRegistry = EditorUtility.DisplayDialog(L10n.Tr("Deleting a scoped registry"),
                    L10n.Tr("You are about to delete a scoped registry, are you sure you want to continue?"),
                    L10n.Tr("Ok"), L10n.Tr("Cancel"));

                if (deleteRegistry)
                    m_UpmRegistryClient.RemoveRegistry(draft.original.name);
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
            if (draft.isUrlOrScopesUpdated && AnyPackageInstalledFromRegistry(draft.original.name) &&
                !EditorUtility.DisplayDialog(L10n.Tr("Updating a scoped registry"),
                    L10n.Tr("There are packages in your project that are from this scoped registry, updating the URL or the scopes could result in errors in your project. Are you sure you want to continue?"),
                    L10n.Tr("Ok"), L10n.Tr("Cancel")))
                return;

            if (draft.Validate())
            {
                var scopes = draft.sanitizedScopes.ToArray();
                if (draft.original != null)
                    m_UpmRegistryClient.UpdateRegistry(draft.original.name, draft.name, draft.url, scopes);
                else
                    m_UpmRegistryClient.AddRegistry(draft.name, draft.url, scopes);
            }
            else
            {
                RefreshErrorBox();
            }
        }

        private void RevertChanges()
        {
            draft.RevertChanges();
            if (draft.original != null)
                GetRegistryLabel(draft.original.name).text = draft.original.name;
            else
            {
                m_NewScopedRegistryLabel.text = L10n.Tr(k_AddNewScopedRegistryText);
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

        private Label GetRegistryLabel(string registryName)
        {
            if (string.IsNullOrEmpty(registryName))
                return m_NewScopedRegistryLabel;
            return m_RegistryLabels.TryGetValue(registryName, out var label) ? label : null;
        }

        private void OnRegistryNameChanged(ChangeEvent<string> evt)
        {
            draft.name = evt.newValue;
            RefreshButtonState(draft.original == null, draft.hasUnsavedChanges);
            RefreshSelectedLabelText();
        }

        private void OnRegistryUrlChanged(ChangeEvent<string> evt)
        {
            draft.url = evt.newValue;
            RefreshButtonState(draft.original == null, draft.hasUnsavedChanges);
            RefreshSelectedLabelText();
        }

        private void OnRegistryScopesChanged(ChangeEvent<string> evt = null)
        {
            draft.SetScopes(scopesList.Children().Cast<TextField>().Select(textField => textField.value));
            RefreshButtonState(draft.original == null, draft.hasUnsavedChanges);
            RefreshSelectedLabelText();
        }

        private void OnRegistryLabelClicked(string registryName)
        {
            if (draft.original?.name == registryName)
                return;

            if (!ShowUnsavedChangesDialog())
                return;

            GetRegistryLabel(draft.original?.name)?.EnableInClassList(k_SelectedRegistryClass, false);
            m_SettingsProxy.SelectRegistry(registryName);
            GetRegistryLabel(registryName).EnableInClassList(k_SelectedRegistryClass, true);
            UpdateRegistryDetails();
        }

        private void OnRegistriesModified()
        {
            UpdateRegistryList();
            UpdateRegistryDetails();
        }

        private void OnRegistryOperationError(string name, UIError error)
        {
            if ((draft.original?.name ?? draft.name) == name)
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

            var discardChanges = EditorUtility.DisplayDialog(L10n.Tr("Discard unsaved changes"),
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

        private void UpdateRegistryList()
        {
            registriesList.Clear();
            m_RegistryLabels.Clear();

            foreach (var registryInfo in m_SettingsProxy.scopedRegistries)
            {
                var label = new Label(registryInfo.name);
                label.OnLeftClick(() => OnRegistryLabelClicked(registryInfo.name));

                var isSelected = draft.original?.name == registryInfo.name;
                if (isSelected)
                {
                    label.AddToClassList(k_SelectedRegistryClass);
                    label.text = GetLabelText(draft);
                }
                m_RegistryLabels.Add(registryInfo.name, label);
                registriesList.Add(label);
            }

            // draft.original == null indicates the new scoped registry is selected no matter what reason is it
            // isUserAddingNewScopedRegistry: the user specifically added the `adding new scoped registry` label. The value would be false
            // when you open the settings page with 0 scoped registry in the manifest.json
            var showAddNewScopedRegistryLabel = draft.original == null || m_SettingsProxy.isUserAddingNewScopedRegistry;
            if (showAddNewScopedRegistryLabel)
            {
                m_NewScopedRegistryLabel.EnableInClassList(k_SelectedRegistryClass, draft.original == null);
                m_NewScopedRegistryLabel.text = GetLabelText(draft, true);
                registriesList.Add(m_NewScopedRegistryLabel);
            }

            addRegistryButton.SetEnabled(!showAddNewScopedRegistryLabel);
            removeRegistryButton.SetEnabled(registriesList.childCount > 0 && !(showAddNewScopedRegistryLabel && registriesList.childCount == 1));
        }

        private void UpdateRegistryDetails()
        {
            registryNameTextField.SetValueWithoutNotify(draft.name ?? string.Empty);
            registryUrlTextField.SetValueWithoutNotify(draft.url ?? string.Empty);

            scopesList.Clear();
            foreach (var scope in draft.scopes)
                scopesList.Add(CreateScopeTextField(scope));
            if (scopesList.childCount > 0)
                RefreshScopeSelection();
            removeScopeButton.SetEnabled(scopesList.childCount > 1);

            RefreshButtonText(draft.original == null);
            RefreshButtonState(draft.original == null, draft.hasUnsavedChanges);
            RefreshErrorBox();
        }

        private void RefreshButtonText(bool isAddNewRegistry)
        {
            revertRegistriesButton.text = isAddNewRegistry ? L10n.Tr("Cancel") : L10n.Tr("Revert");
            applyRegistriesButton.text = isAddNewRegistry ? L10n.Tr("Save") : L10n.Tr("Apply");
        }

        private void RefreshButtonState(bool isAddNewRegistry, bool hasUnsavedChanges)
        {
            revertRegistriesButton.SetEnabled(isAddNewRegistry || hasUnsavedChanges);
            applyRegistriesButton.SetEnabled(hasUnsavedChanges);
        }

        private void RefreshSelectedLabelText()
        {
            var label = draft.original != null ? GetRegistryLabel(draft.original.name) : m_NewScopedRegistryLabel;
            if (label != null)
                label.text = GetLabelText(draft);
        }

        private void RefreshErrorBox()
        {
            scopedRegistryErrorBox.text = draft.errorMessage;
            UIUtils.SetElementDisplay(scopedRegistryErrorBox, !string.IsNullOrEmpty(scopedRegistryErrorBox.text));
        }

        private static string GetLabelText(RegistryInfoDraft draft, bool newScopedRegistry = false)
        {
            if (newScopedRegistry || draft.original == null)
                return (draft.original != null || string.IsNullOrEmpty(draft.name)) ? L10n.Tr(k_AddNewScopedRegistryText) : $"* {draft.name}";
            else
                return draft.hasUnsavedChanges ? $"* {draft.name}" : draft.name;
        }

        private VisualElementCache cache { get; set; }

        private HelpBox scopedRegistriesInfoBox => cache.Get<HelpBox>("scopedRegistriesInfoBox");
        private HelpBox scopedRegistryErrorBox => cache.Get<HelpBox>("scopedRegistryErrorBox");
        private VisualElement registriesList => cache.Get<VisualElement>("registriesList");
        private TextField registryNameTextField => cache.Get<TextField>("registryNameTextField");
        private TextField registryUrlTextField => cache.Get<TextField>("registryUrlTextField");
        private VisualElement scopesList => cache.Get<VisualElement>("scopesList");
        private Button addRegistryButton => cache.Get<Button>("addRegistryButton");
        private Button removeRegistryButton => cache.Get<Button>("removeRegistryButton");
        private Button addScopeButton => cache.Get<Button>("addScopeButton");
        private Button removeScopeButton => cache.Get<Button>("removeScopeButton");
        private Button revertRegistriesButton => cache.Get<Button>("revertRegistriesButton");
        private Button applyRegistriesButton => cache.Get<Button>("applyRegistriesButton");
    }
}
