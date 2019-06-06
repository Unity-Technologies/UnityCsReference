// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageDetails : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageDetails> {}

        public event Action<Package> OnCloseError = delegate {};
        public event Action<Package, Error> OnOperationError = delegate {};

        private readonly VisualElement root;
        private Package package;
        private const string emptyDescriptionClass = "empty";
        private PackageInfo DisplayPackage;
        private Selection Selection;
        private PackageCollection Collection;

        internal enum PackageAction
        {
            Add,
            Remove,
            Update,
            Downgrade,
            Enable,
            Disable,
            UpToDate,
            Current,
            Local,
            Git,
            Embedded
        }

        internal static readonly string[] PackageActionVerbs = { "Install", "Remove", "Update to", "Update to",  "Enable", "Disable", "Up to date", "Current", "Local", "Git", "Embedded" };
        private static readonly string[] PackageActionInProgressVerbs = { "Installing", "Removing", "Updating to", "Updating to", "Enabling", "Disabling", "Up to date", "Current", "Local", "Git", "Embedded" };

        // We limit the number of entries shown so as to not overload the dialog.
        // The relevance of results beyond this limit is also questionable.
        private const int MaxDependentList = 10;

        public PackageDetails()
        {
            root = Resources.GetTemplate("PackageDetails.uxml");
            Add(root);

            Cache = new VisualElementCache(root);

            foreach (var extension in PackageManagerExtensions.Extensions)
                CustomContainer.Add(extension.CreateExtensionUI());

            root.StretchToParentSize();

            SetContentVisibility(false);
            SetUpdateVisibility(false);
            DevelopButton.visible = false;
            RemoveButton.visible = false;
            UpdateBuiltIn.visible = false;

            DevelopButton.clickable.clicked += DevelopClick;
            UpdateButton.clickable.clicked += UpdateClick;
            UpdateBuiltIn.clickable.clicked += UpdateClick;
            RemoveButton.clickable.clicked += RemoveClick;
            ViewDocButton.clickable.clicked += ViewDocClick;
            ViewChangelogButton.clickable.clicked += ViewChangelogClick;
            ViewLicenses.clickable.clicked += ViewLicensesClick;

            var editManifestIconButton = new IconButton(Resources.GetIconPath("edit"));
            editManifestIconButton.clickable.clicked += EditPackageManifestClick;
            EditPackageManifestButton.Add(editManifestIconButton);

            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

            UnityEditor.Selection.selectionChanged += OnEditorSelectionChanged;
        }

        private void OnEditorSelectionChanged()
        {
            if (DisplayPackage == null || DisplayPackage.PackageManifestAsset == null)
                return;

            var onlyContainsCurrentPackageManifest = UnityEditor.Selection.count == 1 &&
                UnityEditor.Selection.Contains(DisplayPackage.PackageManifestAsset);
            EditPackageManifestButton.SetEnabled(!onlyContainsCurrentPackageManifest);
        }

        private void EditPackageManifestClick()
        {
            DisplayPackage.SelectPackageManifestAsset();
        }

        private void SetContentVisibility(bool visible)
        {
            UIUtils.SetElementDisplay(DetailContainer, visible);
            UIUtils.SetElementDisplay(PackageToolbarContainer, visible);
        }

        public void SetCollection(PackageCollection collection)
        {
            Collection = collection;
            Dependencies.SetCollection(collection);
        }

        public void SetSelection(Selection selection)
        {
            if (Selection != null)
                Selection.OnChanged -= OnSelectionChanged;

            Selection = selection;
            Selection.OnChanged += OnSelectionChanged;

            OnSelectionChanged(Selection.SelectedVersions);
        }

        private void OnSelectionChanged(IEnumerable<PackageVersion> selected)
        {
            if (!Selection.SelectedVersions.Any())
            {
                SetContentVisibility(false);
                return;
            }

            var main = selected.FirstOrDefault();
            if (main != null)
                SetPackage(main.Package, main.Version);
            else
                SetPackage(null);
        }

        private void OnEnterPanel(AttachToPanelEvent e)
        {
            PackageManagerToolbar.OnToggleDependenciesChange += ShowDependencies;
        }

        private void OnLeavePanel(DetachFromPanelEvent e)
        {
            PackageManagerToolbar.OnToggleDependenciesChange -= ShowDependencies;
        }

        private void ShowDependencies()
        {
            if (DisplayPackage == null || DisplayPackage.Info == null)
                return;

            if (PackageManagerPrefs.ShowPackageDependencies)
                Dependencies.SetDependencies(DisplayPackage.Info.dependencies);

            UIUtils.SetElementDisplay(Dependencies, PackageManagerPrefs.ShowPackageDependencies);
        }

        private void SetUpdateVisibility(bool value)
        {
            UIUtils.SetElementDisplay(UpdateButton, value);
        }

        void RefreshExtensions(PackageInfo packageInfo)
        {
            foreach (var extension in PackageManagerExtensions.Extensions)
                extension.OnPackageSelectionChange(packageInfo?.Info);

            foreach (var extension in PackageManagerExtensions.ToolbarExtensions)
                extension.OnPackageSelectionChange(packageInfo, PackageToolbarLeftArea);
        }

        private void SetDisplayPackage(PackageInfo packageInfo, Error packageError = null)
        {
            DisplayPackage = packageInfo;

            Error error = null;

            var detailVisible = package != null && DisplayPackage != null;
            if (!detailVisible)
            {
                RefreshExtensions(null);
            }
            else
            {
                SetUpdateVisibility(true);
                var isBuiltIn = DisplayPackage.IsBuiltIn;
                UIUtils.SetElementDisplay(ViewDocButton, true);
                UIUtils.SetElementDisplay(ViewLicensesContainer, !isBuiltIn);
                RemoveButton.visible = true;

                if (string.IsNullOrEmpty(DisplayPackage.Description))
                {
                    DetailDesc.text = "There is no description for this package.";
                    DetailDesc.AddToClassList(emptyDescriptionClass);
                }
                else
                {
                    DetailDesc.text = DisplayPackage.Description;
                    DetailDesc.RemoveFromClassList(emptyDescriptionClass);
                }

                root.Q<Label>("detailTitle").text = DisplayPackage.DisplayName;
                DetailVersion.text = "Version " + DisplayPackage.VersionWithoutTag;

                if (DisplayPackage.IsInDevelopment || DisplayPackage.HasVersionTag(PackageTag.preview))
                    UIUtils.SetElementDisplay(GetTag(PackageTag.verified), false);
                else
                {
                    var unityVersionParts = Application.unityVersion.Split('.');
                    var unityVersion = string.Format("{0}.{1}", unityVersionParts[0], unityVersionParts[1]);
                    VerifyLabel.text = unityVersion + " verified";
                    UIUtils.SetElementDisplay(GetTag(PackageTag.verified), DisplayPackage.IsVerified);
                }

                UIUtils.SetElementDisplay(GetTag(PackageTag.inDevelopment), DisplayPackage.IsInDevelopment);
                UIUtils.SetElementDisplay(GetTag(PackageTag.local), DisplayPackage.IsLocal);
                UIUtils.SetElementDisplay(GetTag(PackageTag.git), DisplayPackage.IsGit);
                UIUtils.SetElementDisplay(GetTag(PackageTag.preview), DisplayPackage.IsPreview);
                UIUtils.SetElementDisplay(EditPackageManifestButton, DisplayPackage.IsInstalled && !package.IsBuiltIn);

                UIUtils.SetElementDisplay(DocumentationContainer, true);
                UIUtils.SetElementDisplay(ChangelogContainer, DisplayPackage.HasChangelog);

                SampleList.SetPackage(DisplayPackage);

                root.Q<Label>("detailName").text = DisplayPackage.Name;
                DetailScrollView.scrollOffset = new Vector2(0, 0);

                DetailModuleReference.text = "";
                if (isBuiltIn)
                    DetailModuleReference.text = DisplayPackage.BuiltInDescription;

                DetailAuthor.text = "";
                if (!string.IsNullOrEmpty(DisplayPackage.Author))
                    DetailAuthor.text = string.Format("Author: {0}", DisplayPackage.Author);

                // If the package details is not enabled, don't update the date yet as we are fetching new information
                if (enabledSelf)
                {
                    DetailDate.text = "";

                    // In Development packages are not published, so we do not show any published date
                    if (DisplayPackage.Info != null && !DisplayPackage.IsInDevelopment)
                    {
                        if (DisplayPackage.Info.datePublished != null)
                            DetailDate.text = $"Published Date: {DisplayPackage.Info.datePublished.Value:MMMM dd,  yyyy}";
                        else if (DisplayPackage.IsCore || DisplayPackage.IsInstalled)
                        {
                            // For core packages, or installed packages that are bundled with Unity without being published, use Unity's build date
                            var unityBuildDate = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                            unityBuildDate = unityBuildDate.AddSeconds(InternalEditorUtility.GetUnityVersionDate());
                            DetailDate.text = $"Published Date: {unityBuildDate:MMMM dd, yyyy}";
                        }
                    }
                }

                UIUtils.SetElementDisplay(DetailDesc, !isBuiltIn);
                UIUtils.SetElementDisplay(DetailVersion, !isBuiltIn);
                UIUtils.SetElementDisplayNonEmpty(DetailModuleReference);
                UIUtils.SetElementDisplayNonEmpty(DetailAuthor);
                UIUtils.SetElementDisplayNonEmpty(DetailDate);

                if (DisplayPackage.Errors.Count > 0)
                    error = DisplayPackage.Errors.First();

                RefreshAddButton();
                RefreshRemoveButton();
                RefreshDevelopButton();

                UIUtils.SetElementDisplay(CustomContainer, true);

                package.AddSignal.OnOperation += OnAddOperation;
                package.RemoveSignal.OnOperation += OnRemoveOperation;
                package.EmbedSignal.OnOperation += OnDevelopOperation;
                RefreshExtensions(DisplayPackage);

                ShowDependencies();
            }
            OnEditorSelectionChanged();

            // Set visibility
            SetContentVisibility(detailVisible);

            if (packageError == null && package != null && package.Error != null)
                packageError = package.Error;
            if (null == error)
                error = packageError;

            if (error != null)
                SetError(error);
            else
                DetailError.ClearError();
        }

        public void SetPackage(Package package, PackageInfo displayPackage = null)
        {
            if (displayPackage == null && package != null)
                displayPackage = package.VersionToDisplay;

            if (package == this.package && displayPackage == DisplayPackage)
                return;

            if (this.package != null)
            {
                if (this.package.AddSignal.Operation != null)
                {
                    this.package.AddSignal.Operation.OnOperationError -= OnAddOperationError;
                    this.package.AddSignal.Operation.OnOperationSuccess -= OnAddOperationSuccess;
                }
                this.package.AddSignal.ResetEvents();

                if (this.package.RemoveSignal.Operation != null)
                {
                    this.package.RemoveSignal.Operation.OnOperationSuccess -= OnRemoveOperationSuccess;
                    this.package.RemoveSignal.Operation.OnOperationError -= OnRemoveOperationError;
                }
                this.package.RemoveSignal.ResetEvents();
            }

            SetContentVisibility(true);

            this.package = package;

            if (displayPackage == null)
                displayPackage = package != null ? package.VersionToDisplay : null;

            ShowDisplayPackage(displayPackage);
        }

        internal void OnLatestPackageInfoFetched(PackageInfo fetched, bool isDefaultVersion)
        {
            if (DisplayPackage == null || DisplayPackage.PackageId != fetched.PackageId)
                return;
            SetEnabled(true);
            SetDisplayPackage(fetched);
        }

        private void ShowDisplayPackage(PackageInfo displayPackage)
        {
            SetEnabled(true);

            if (Collection.NeedsFetchLatest(displayPackage))
            {
                SetEnabled(false);
                Collection.FetchLatestPackageInfo(displayPackage);
            }

            SetDisplayPackage(displayPackage);
        }

        private void SetError(Error error)
        {
            DetailError.AdjustSize(DetailScrollView.verticalScroller.visible);
            DetailError.SetError(error);
            DetailError.OnCloseError = () =>
            {
                OnCloseError(package);
            };
        }

        private void OnAddOperation(IAddOperation operation)
        {
            operation.OnOperationError += OnAddOperationError;
            operation.OnOperationSuccess += OnAddOperationSuccess;
        }

        private void OnAddOperationError(Error error)
        {
            if (package != null && package.AddSignal.Operation != null)
            {
                package.AddSignal.Operation.OnOperationSuccess -= OnAddOperationSuccess;
                package.AddSignal.Operation.OnOperationError -= OnAddOperationError;
                package.AddSignal.Operation = null;
            }

            SetError(error);
            OnOperationError(package, error);
        }

        private void OnAddOperationSuccess(PackageInfo packageInfo)
        {
            if (package != null && package.AddSignal.Operation != null)
            {
                package.AddSignal.Operation.OnOperationSuccess -= OnAddOperationSuccess;
                package.AddSignal.Operation.OnOperationError -= OnAddOperationError;
                package.AddSignal.Operation = null;
            }

            foreach (var extension in PackageManagerExtensions.Extensions)
                extension.OnPackageAddedOrUpdated(packageInfo.Info);

            Selection.SetSelection(packageInfo);
            foreach (var itemState in Selection.States.Where(p => p.PackageId == packageInfo.PackageId))
                itemState.Expanded = false;
        }

        private void OnRemoveOperation(IRemoveOperation operation)
        {
            // Make sure we are not already registered
            operation.OnOperationError -= OnRemoveOperationError;
            operation.OnOperationSuccess -= OnRemoveOperationSuccess;

            operation.OnOperationError += OnRemoveOperationError;
            operation.OnOperationSuccess += OnRemoveOperationSuccess;
        }

        private void OnRemoveOperationError(Error error)
        {
            if (package != null && package.RemoveSignal.Operation != null)
            {
                package.RemoveSignal.Operation.OnOperationSuccess -= OnRemoveOperationSuccess;
                package.RemoveSignal.Operation.OnOperationError -= OnRemoveOperationError;
                package.RemoveSignal.Operation = null;
            }

            SetError(error);
            OnOperationError(package, error);
        }

        private void OnRemoveOperationSuccess(PackageInfo packageInfo)
        {
            if (package != null && package.RemoveSignal.Operation != null)
            {
                package.RemoveSignal.Operation.OnOperationSuccess -= OnRemoveOperationSuccess;
                package.RemoveSignal.Operation.OnOperationError -= OnRemoveOperationError;
                package.RemoveSignal.Operation = null;
            }

            foreach (var extension in PackageManagerExtensions.Extensions)
                extension.OnPackageRemoved(packageInfo.Info);
        }

        private void OnDevelopOperation(IEmbedOperation operation)
        {
            // Make sure we are not already registered
            operation.OnOperationError -= OnDevelopOperationError;
            operation.OnOperationSuccess -= OnDevelopOperationSuccess;

            operation.OnOperationError += OnDevelopOperationError;
            operation.OnOperationSuccess += OnDevelopOperationSuccess;
        }

        private void OnDevelopOperationError(Error error)
        {
            if (package != null && package.EmbedSignal.Operation != null)
            {
                package.EmbedSignal.Operation.OnOperationSuccess -= OnDevelopOperationSuccess;
                package.EmbedSignal.Operation.OnOperationError -= OnDevelopOperationError;
                package.EmbedSignal.Operation = null;
            }

            SetError(error);
            OnOperationError(package, error);
        }

        private void OnDevelopOperationSuccess(PackageInfo packageInfo)
        {
            if (package != null && package.RemoveSignal.Operation != null)
            {
                package.EmbedSignal.Operation.OnOperationSuccess -= OnDevelopOperationSuccess;
                package.EmbedSignal.Operation.OnOperationError -= OnDevelopOperationError;
                package.EmbedSignal.Operation = null;
            }
        }

        private PackageInfo TargetVersion
        {
            get
            {
                var targetVersion = DisplayPackage;
                if (targetVersion == null)
                    return null;

                if (package.Current != null && package.Current.PackageId == targetVersion.PackageId)
                    targetVersion = package.LatestUpdate;

                return targetVersion;
            }
        }

        private void RefreshAddButton()
        {
            if (package.Current != null && package.Current.IsInDevelopment)
            {
                UIUtils.SetElementDisplay(UpdateBuiltIn, false);
                UIUtils.SetElementDisplay(UpdateButton, false);
                return;
            }

            var targetVersion = TargetVersion;
            if (targetVersion == null)
                return;

            var enableButton = !Package.PackageOperationInProgress;

            var action = PackageAction.Update;
            var addOperation = package.AddSignal.Operation;
            var inprogress = addOperation != null && addOperation.PackageInfo.PackageId == DisplayPackage.PackageId;
            var isBuiltIn = package.IsBuiltIn;

            if (addOperation != null)
            {
                if (isBuiltIn)
                {
                    action = PackageAction.Enable;
                    inprogress = true;
                    enableButton = false;
                }
                else
                {
                    var addOperationVersion = package.AddSignal.Operation.PackageInfo.Version;
                    if (package.Current == null)
                    {
                        action = PackageAction.Add;
                        inprogress = true;
                    }
                    else
                    {
                        action = addOperationVersion.CompareByPrecedence(package.Current.Version) >= 0
                            ? PackageAction.Update : PackageAction.Downgrade;
                        inprogress = true;
                    }

                    enableButton = false;
                }
            }
            else
            {
                if (package.Current != null)
                {
                    // Installed
                    if (package.Current.IsVersionLocked)
                    {
                        if (package.Current.Origin == PackageSource.Embedded)
                            action = PackageAction.Embedded;
                        else if (package.Current.Origin == PackageSource.Git)
                            action = PackageAction.Git;

                        enableButton = false;
                    }
                    else
                    {
                        if (targetVersion.IsInstalled)
                        {
                            if (targetVersion == package.LatestUpdate)
                                action = PackageAction.UpToDate;
                            else
                                action = PackageAction.Current;

                            enableButton = false;
                        }
                        else
                        {
                            action = targetVersion.Version.CompareByPrecedence(package.Current.Version) >= 0
                                ? PackageAction.Update : PackageAction.Downgrade;
                        }
                    }
                }
                else
                {
                    // Not Installed
                    if (package.Versions.Any())
                    {
                        if (isBuiltIn)
                            action = PackageAction.Enable;
                        else
                            action = PackageAction.Add;
                    }
                }
            }

            if (package.RemoveSignal.Operation != null)
                enableButton = false;

            if (EditorApplication.isCompiling)
            {
                enableButton = false;

                EditorApplication.update -= CheckCompilationStatus;
                EditorApplication.update += CheckCompilationStatus;
            }

            var version = action == PackageAction.Update || action == PackageAction.Downgrade ? targetVersion.Version : null;

            var button = isBuiltIn ? UpdateBuiltIn : UpdateButton;
            button.SetEnabled(enableButton);
            button.text = GetButtonText(action, inprogress, version);

            var visibleFlag = !(package.Current != null && package.Current.IsVersionLocked);
            UIUtils.SetElementDisplay(UpdateBuiltIn, isBuiltIn && visibleFlag);
            UIUtils.SetElementDisplay(UpdateButton, !isBuiltIn && visibleFlag);
        }

        private void RefreshRemoveButton()
        {
            var visibleFlag = false;

            var current = package.Current;

            // Show only if there is a current package installed
            if (current != null)
            {
                visibleFlag = current.CanBeRemoved && !package.IsPackageManagerUI;

                var action = current.IsBuiltIn ? PackageAction.Disable : PackageAction.Remove;
                var inprogress = package.RemoveSignal.Operation != null;

                var enableButton = visibleFlag && !EditorApplication.isCompiling && !inprogress && !Package.PackageOperationInProgress
                    && current == DisplayPackage;

                if (EditorApplication.isCompiling)
                {
                    EditorApplication.update -= CheckCompilationStatus;
                    EditorApplication.update += CheckCompilationStatus;
                }

                RemoveButton.SetEnabled(enableButton);
                RemoveButton.text = GetButtonText(action, inprogress);
            }

            UIUtils.SetElementDisplay(RemoveButton, visibleFlag);
        }

        private void RefreshDevelopButton()
        {
            if (package.AnyInDevelopment)
            {
                UIUtils.SetElementDisplay(DevelopButton, false);
                return;
            }

            var visibleFlag = DisplayPackage.IsInstalled && DisplayPackage.Origin == PackageSource.Registry;
            var inprogress = package.EmbedSignal.Operation != null;
            var enableButton = !EditorApplication.isCompiling && !inprogress && !Package.PackageOperationInProgress;

            if (EditorApplication.isCompiling)
            {
                EditorApplication.update -= CheckCompilationStatus;
                EditorApplication.update += CheckCompilationStatus;
            }

            DevelopButton.SetEnabled(enableButton);
            UIUtils.SetElementDisplay(DevelopButton, visibleFlag);
        }

        private void CheckCompilationStatus()
        {
            if (EditorApplication.isCompiling)
                return;

            RefreshAddButton();
            RefreshRemoveButton();
            RefreshDevelopButton();
            EditorApplication.update -= CheckCompilationStatus;
        }

        private string GetButtonText(PackageAction action, bool inProgress = false, SemVersion version = null)
        {
            var actionText = inProgress ? PackageActionInProgressVerbs[(int)action] : PackageActionVerbs[(int)action];
            return version == null ?
                string.Format("{0}",        actionText) :
                string.Format("{0} {1}", actionText, version);
        }

        private void DevelopClick()
        {
            // Embed
            DetailError.ClearError();
            package.Embed(info =>
            {
                RefreshAddButton();
                RefreshRemoveButton();
                RefreshDevelopButton();
            });

            UIUtils.SetElementDisplay(UpdateBuiltIn, false);
            UIUtils.SetElementDisplay(UpdateButton, false);
            UIUtils.SetElementDisplay(RemoveButton, false);
            RefreshDevelopButton();
        }

        private void UpdateClick()
        {
            if (package.IsPackageManagerUI)
            {
                // Let's not allow updating of the UI if there are build errors, as for now, that will prevent the UI from reloading properly.
                if (EditorUtility.scriptCompilationFailed)
                {
                    EditorUtility.DisplayDialog("Unity Package Manager", "The Package Manager UI cannot be updated while there are script compilation errors in your project.  Please fix the errors and try again.", "Ok");
                    return;
                }

                if (!EditorUtility.DisplayDialog("Unity Package Manager", "Updating this package will close the Package Manager window. You will have to re-open it after the update is done. Do you want to continue?", "Yes", "No"))
                    return;

                if (package.AddSignal.Operation != null)
                {
                    package.AddSignal.Operation.OnOperationSuccess -= OnAddOperationSuccess;
                    package.AddSignal.Operation.OnOperationError -= OnAddOperationError;
                    package.AddSignal.ResetEvents();
                    package.AddSignal.Operation = null;
                }

                DetailError.ClearError();
                EditorApplication.update += CloseAndUpdate;

                return;
            }

            DetailError.ClearError();
            package.Add(TargetVersion);
            RefreshAddButton();
            RefreshRemoveButton();
        }

        private void CloseAndUpdate()
        {
            EditorApplication.update -= CloseAndUpdate;
            package.Add(TargetVersion);

            var windows = UnityEngine.Resources.FindObjectsOfTypeAll<PackageManagerWindow>();
            if (windows.Length > 0)
            {
                windows[0].Close();
            }
        }

        /// <summary>
        /// Get a line-separated bullet list of package names
        /// </summary>
        private string GetPackageDashList(IEnumerable<PackageInfo> packageInfos, int maxListCount = MaxDependentList)
        {
            var shortListed = packageInfos.Take(maxListCount);
            return " - " + string.Join("\n - ", shortListed.Select(p => p.DisplayName).ToArray());
        }

        /// <summary>
        /// Get the full dependent message string
        /// </summary>
        private string GetDependentMessage(IEnumerable<PackageInfo> roots, int maxListCount = MaxDependentList)
        {
            var dependentPackages = roots.Where(p => !p.IsBuiltIn).ToList();
            var dependentModules = roots.Where(p => p.IsBuiltIn).ToList();

            var message = string.Format("{0}{1}:\n\n", "This built-in package is a dependency of the following ",  dependentPackages.Any() ? "packages" : "built-in packages");

            if (dependentPackages.Any())
                message += GetPackageDashList(dependentPackages, maxListCount);
            if (dependentPackages.Any() && dependentModules.Any())
                message += "\n\nand the following built-in packages:\n\n";
            if (dependentModules.Any())
                message += GetPackageDashList(dependentModules, maxListCount);

            if (roots.Count() > maxListCount)
                message += "\n\n   ... and more (see console for details) ...";

            message += "\n\nYou will need to remove or disable them before being able to disable this built-in package.";

            return message;
        }

        private void RemoveClick()
        {
            if (DisplayPackage.IsBuiltIn)
            {
                var roots = Collection.GetDependents(DisplayPackage).ToList();
                if (roots.Any())
                {
                    if (roots.Count > MaxDependentList)
                        Debug.Log(GetDependentMessage(roots, int.MaxValue));

                    var message = GetDependentMessage(roots);
                    EditorUtility.DisplayDialog("Cannot Disable Built-In Package", message, "Ok");

                    return;
                }
            }

            if (DisplayPackage.IsInDevelopment)
            {
                if (!EditorUtility.DisplayDialog("Unity Package Manager", "You will loose all your changes (if any) if you delete a package in development. Are you sure?", "Yes", "No"))
                    return;

                RefreshRemoveButton();
                RefreshAddButton();

                Directory.Delete(DisplayPackage.Info.resolvedPath, true);
                AssetDatabase.Refresh();
                EditorApplication.delayCall += () =>
                {
                    if (Collection.Filter == PackageFilter.InDevelopment)
                    {
                        var otherInDev = Collection.listPackages.Any(info => info.IsInDevelopment && info.Name != package.Name);
                        PackageManagerWindow.SelectPackageAndFilter(DisplayPackage.VersionId, otherInDev ? PackageFilter.InDevelopment : PackageFilter.Local, true);
                    }
                    else
                    {
                        PackageManagerWindow.SelectPackageAndFilter(DisplayPackage.VersionId, null, true);
                    }
                };

                return;
            }

            var result = 0;
            if (DisplayPackage.IsBuiltIn)
            {
                if (!PackageManagerPrefs.SkipDisableConfirmation)
                {
                    result = EditorUtility.DisplayDialogComplex("Disable Built-In Package",
                        "Are you sure you want to disable this built-in package?",
                        "Disable", "Cancel", "Never ask");
                }
            }
            else
            {
                if (!PackageManagerPrefs.SkipRemoveConfirmation)
                {
                    result = EditorUtility.DisplayDialogComplex("Removing Package",
                        "Are you sure you want to remove this package?",
                        "Remove", "Cancel", "Never ask");
                }
            }

            // Cancel
            if (result == 1)
                return;

            // Do not ask again
            if (result == 2)
            {
                if (DisplayPackage.IsBuiltIn)
                    PackageManagerPrefs.SkipDisableConfirmation = true;
                else
                    PackageManagerPrefs.SkipRemoveConfirmation = true;
            }

            // Remove
            DetailError.ClearError();
            package.Remove();
            RefreshRemoveButton();
            RefreshAddButton();
        }

        private static void ViewOfflineUrl(PackageInfo packageInfo, Func<bool, string> getUrl, string messageOnNotFound)
        {
            if (!packageInfo.IsAvailableOffline)
            {
                EditorUtility.DisplayDialog("Unity Package Manager", "This package is not available offline.", "Ok");
                return;
            }
            var offlineUrl = getUrl(true);
            if (!string.IsNullOrEmpty(offlineUrl))
                Application.OpenURL(offlineUrl);
            else
                EditorUtility.DisplayDialog("Unity Package Manager", messageOnNotFound, "Ok");
        }

        private static void ViewUrl(PackageInfo packageInfo, Func<bool, string> getUrl, string messageOnNotFound)
        {
            if (Application.internetReachability != NetworkReachability.NotReachable)
            {
                var onlineUrl = getUrl(false);
                var request = UnityWebRequest.Head(onlineUrl);
                var operation = request.SendWebRequest();
                operation.completed += (op) =>
                {
                    if (request.responseCode != 404)
                    {
                        Application.OpenURL(onlineUrl);
                    }
                    else
                    {
                        ViewOfflineUrl(packageInfo, getUrl, messageOnNotFound);
                    }
                };
            }
            else
            {
                ViewOfflineUrl(packageInfo, getUrl, messageOnNotFound);
            }
        }

        private void ViewDocClick()
        {
            ViewUrl(DisplayPackage, DisplayPackage.GetDocumentationUrl, "This package does not contain offline documentation.");
        }

        private void ViewChangelogClick()
        {
            ViewUrl(DisplayPackage, DisplayPackage.GetChangelogUrl, "This package does not contain offline changelog.");
        }

        private void ViewLicensesClick()
        {
            ViewUrl(DisplayPackage, DisplayPackage.GetLicensesUrl, "This package does not contain offline licenses.");
        }

        private VisualElementCache Cache { get; set; }

        private Label DetailDesc { get { return Cache.Get<Label>("detailDesc"); } }
        internal Button DevelopButton { get { return Cache.Get<Button>("develop"); } }
        internal Button UpdateButton { get { return Cache.Get<Button>("update"); } }
        internal Button RemoveButton { get { return Cache.Get<Button>("remove"); } }
        private Button ViewDocButton { get { return Cache.Get<Button>("viewDocumentation"); } }
        private VisualElement DocumentationContainer { get { return Cache.Get<VisualElement>("documentationContainer"); } }
        private Button ViewChangelogButton { get { return Cache.Get<Button>("viewChangelog"); } }
        private VisualElement ChangelogContainer { get { return Cache.Get<VisualElement>("changeLogContainer"); } }
        private Button ViewLicenses { get { return Cache.Get<Button>("viewLicenses"); } }
        private VisualElement ViewLicensesContainer { get { return Cache.Get<VisualElement>("viewLicensesContainer"); } }
        private Alert DetailError { get { return Cache.Get<Alert>("detailError"); } }
        private ScrollView DetailScrollView { get { return Cache.Get<ScrollView>("detailScrollView"); } }
        private VisualElement DetailContainer { get { return Cache.Get<VisualElement>("detail"); } }
        private Label DetailModuleReference { get { return Cache.Get<Label>("detailModuleReference"); } }
        private Label DetailVersion { get { return Cache.Get<Label>("detailVersion"); } }
        private Label DetailAuthor { get { return Cache.Get<Label>("detailAuthor"); } }
        private Label DetailDate { get { return Cache.Get<Label>("detailDate"); } }
        private Label VerifyLabel { get { return Cache.Get<Label>("verifiedTag"); } }
        private VisualElement CustomContainer { get { return Cache.Get<VisualElement>("detailCustomContainer"); } }
        private PackageSampleList SampleList { get { return Cache.Get<PackageSampleList>("detailSampleList"); } }
        internal VisualElement GetTag(PackageTag tag) {return Cache.Get<VisualElement>(tag + "Tag"); }
        private PackageDependencies Dependencies { get {return Cache.Get<PackageDependencies>("detailDependencies");} }
        private VisualElement PackageToolbarContainer { get {return Cache.Get<VisualElement>("toolbarContainer");} }
        private VisualElement PackageToolbarLeftArea { get {return Cache.Get<VisualElement>("leftItems");} }
        internal Button UpdateBuiltIn { get { return Cache.Get<Button>("updateBuiltIn"); } }
        private VisualElement EditPackageManifestButton { get { return Cache.Get<VisualElement>("editPackageManifest"); } }
    }
}
