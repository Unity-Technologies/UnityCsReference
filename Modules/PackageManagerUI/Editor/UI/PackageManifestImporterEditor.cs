// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEditor.AssetImporters;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEditor.Callbacks;
using System.Globalization;
using System.Text.RegularExpressions;

namespace UnityEditor.PackageManager.UI.Internal
{
    [CustomEditor(typeof(PackageManifestImporter))]
    [CanEditMultipleObjects]
    internal class PackageManifestImporterEditor : AssetImporterEditor
    {
        enum PackageVisibility
        {
            DefaultVisibility,
            AlwaysHidden,
            AlwaysVisible
        }

        private static readonly string s_LocalizedTitle = L10n.Tr("{0} '{1}' Manifest");
        private static readonly string s_LocalizedMultipleTitle = L10n.Tr("{0} Package Manifests");
        private static readonly string s_LocalizedInvalidPackageManifest = L10n.Tr("Invalid Package Manifest");

        private const float k_MinHeightDescriptionScrollView = 96f;
        private const long k_MaxVersion = 999999999L;
        private const long k_RecommendedMaxVersion = 999999L;

        private const string k_ManifestFieldName = "name";
        private const string k_ManifestFieldPackageName = "packageName";
        private const string k_ManifestFieldVersion = "version";
        private const string k_ManifestFieldDisplayName = "displayName";
        private const string k_ManifestFieldDescription = "description";
        private const string k_ManifestFieldType = "type";
        private const string k_ManifestFieldDependencies = "dependencies";
        private const string k_ManifestFieldHideEditor = "hideInEditor";
        private const string k_ManifestFieldUnity = "unity";
        private const string k_ManifestFieldUnityRelease = "unityRelease";
        private const string k_ManifestFieldDocumentationUrl = "documentationUrl";
        private const string k_ManifestFieldLicensesUrl = "licensesUrl";
        private const string k_ManifestFieldChangelogUrl = "changelogUrl";
        private const string k_ManifestFieldAuthor = "author";
        private const string k_ManifestFieldUrl = "url";
        private const string k_ManifestFieldEmail = "email";


        private static List<string> s_MajorUnityVersions;
        private static List<string> MajorUnityVersions
        {
            get
            {
                if (s_MajorUnityVersions != null)
                    return s_MajorUnityVersions;

                var version = InternalEditorUtility.GetUnityVersion();
                s_MajorUnityVersions = new List<string>{ "2018", "2019", "2020", "2021", "2022" };
                for (var majorVersion = 6000; majorVersion <= version.Major; majorVersion+= 1000)
                    s_MajorUnityVersions.Add(majorVersion.ToString());
                return s_MajorUnityVersions;
            }
        }

        private static readonly List<string> MinorUnityVersions = new List<string> { "0", "1", "2", "3", "4" };

        [Serializable]
        class AdvancedSettings
        {
            public PackageVisibility visibility;
        }

        [Serializable]
        class PackageDependency
        {
            public string packageName;
            public string version;
        }

        [Serializable]
        class PackageUnityVersion
        {
            public bool enabled;
            public string major;
            public string minor;
            public string release;
        }

        [Serializable]
        class PackageAuthor
        {
            public bool enabled;
            public string name;
            public string url;
            public string email;
        }

        [Serializable]
        class PackageInformation
        {
            public string technicalName;
            public string displayName;
            public string version;
            public string description;
            public string type;
            public AdvancedSettings settings = new AdvancedSettings();
            public PackageAuthor author = new PackageAuthor();
            public PackageUnityVersion unityVersion = new PackageUnityVersion();
            public string documentationUrl;
            public string licensesUrl;
            public string changelogUrl;
        }

        [Serializable]
        class PackageManifestState : ScriptableObject
        {
            public bool isValidFile;
            public PackageInformation info;
            public List<PackageDependency> dependencies;
        }

        PackageManifestState packageState => extraDataTarget as PackageManifestState;

        private static class Styles
        {
            public static readonly GUIContent information = EditorGUIUtility.TrTextContent("Information");
            public static readonly GUIContent technicalName = EditorGUIUtility.TrTextContent("Technical Name", "Must be lowercase and usually contains three parts (ex: com.companyname.packagename).");

            public static readonly GUIContent displayName = EditorGUIUtility.TrTextContent("Display name", "Display name used in UI.");
            public static readonly GUIContent version = EditorGUIUtility.TrTextContent("Version", "Must follow SemVer (ex: 1.0.0-preview.1).");
            public static readonly GUIContent type = EditorGUIUtility.TrTextContent("Type", "Type (optional).");

            public static readonly GUIContent showAdvanced = EditorGUIUtility.TrTextContent("Advanced", "Show advanced settings.");

            public static readonly GUIContent visibility = EditorGUIUtility.TrTextContent("Visibility in Editor");

            public static readonly GUIContent author = EditorGUIUtility.TrTextContent("Author", "Author information.");
            public static readonly GUIContent authorName = EditorGUIUtility.TrTextContent("Name", "Author name.");
            public static readonly GUIContent authorUrl = EditorGUIUtility.TrTextContent("URL", "Author's website.");
            public static readonly GUIContent authorEmail = EditorGUIUtility.TrTextContent("Email", "Author's email address.");

            public static readonly GUIContent unityVersion = EditorGUIUtility.TrTextContent("Minimum Unity version");
            public static readonly GUIContent unityMajor = EditorGUIUtility.TrTextContent("Major", "Major version of Unity");
            public static readonly GUIContent unityMinor = EditorGUIUtility.TrTextContent("Minor", "Minor version of Unity");
            public static readonly GUIContent unityRelease = EditorGUIUtility.TrTextContent("Release", "Specific release (ex: 0a9)");

            public static readonly GUIContent documentationUrl = EditorGUIUtility.TrTextContent("Documentation URL", "Documentation URL.");
            public static readonly GUIContent licensesUrl = EditorGUIUtility.TrTextContent("Licenses URL", "Licenses URL.");
            public static readonly GUIContent changelogUrl = EditorGUIUtility.TrTextContent("Changelog URL", "Changelog URL.");

            public static readonly GUIContent description = EditorGUIUtility.TrTextContent("Brief description");

            public static readonly GUIContent dependencies = EditorGUIUtility.TrTextContent("Dependencies");
            public static readonly GUIContent package = EditorGUIUtility.TrTextContent("Package name", "Must be lowercase");

            public static readonly GUIContent viewInPackageManager = EditorGUIUtility.TrTextContent("View in Package Manager");
        }

        public override bool showImportedObject => false;

        protected override bool useAssetDrawPreview => false;

        protected override Type extraDataType => typeof(PackageManifestState);

#pragma warning disable 0649
        [HideInInspector]
        [SerializeField]
        private Vector2 descriptionScrollViewPosition;
#pragma warning restore 0649

        private List<string> errorMessages;
        private List<string> warningMessages;

        ReorderableList m_DependenciesList;

        private SerializedProperty m_IsValidFile;
        private SerializedProperty m_TechnicalName;
        private SerializedProperty m_DisplayName;
        private SerializedProperty m_Version;
        private SerializedProperty m_AuthorEnabled;
        private SerializedProperty m_AuthorName;
        private SerializedProperty m_AuthorUrl;
        private SerializedProperty m_AuthorEmail;
        private SerializedProperty m_UnityVersionEnabled;
        private SerializedProperty m_UnityMajor;
        private SerializedProperty m_UnityMinor;
        private SerializedProperty m_UnityRelease;
        private SerializedProperty m_Description;
        private SerializedProperty m_Type;
        private SerializedProperty m_Advanced;
        private SerializedProperty m_Visibility;
        private SerializedProperty m_DocumentationUrl;
        private SerializedProperty m_LicensesUrl;
        private SerializedProperty m_ChangelogUrl;

        private bool isFeatureSet => m_Type?.stringValue == "feature";

        internal override string targetTitle
        {
            get
            {
                if (targets.Length > 1)
                {
                    return string.Format(s_LocalizedMultipleTitle, targets.Length);
                }

                var packageDescriptor = EditorGUIUtility.TrTextContent(isFeatureSet ? "Feature" : "Package");
                return string.Format(s_LocalizedTitle, packageDescriptor,
                    packageState != null && packageState.isValidFile ? !string.IsNullOrWhiteSpace(packageState.info.displayName) ? packageState.info.displayName.Trim() : packageState.info.technicalName : s_LocalizedInvalidPackageManifest);
            }
        }

        internal override void OnHeaderControlsGUI()
        {
            base.OnHeaderControlsGUI();

            // We want to have this button enabled even for immutable package
            var previousEnabled = GUI.enabled;
            GUI.enabled = packageState != null && packageState.isValidFile && targets.Length == 1;
            if (GUILayout.Button(Styles.viewInPackageManager, EditorStyles.miniButton))
            {
                PackageManagerWindow.OpenAndSelectPackage(packageState.info.technicalName);
            }
            GUI.enabled = previousEnabled;
        }

        public override void OnEnable()
        {
            base.OnEnable();

            //Ensure UIElements handles the IMGUI container with margins
            alwaysAllowExpansion = true;

            errorMessages = new List<string>();
            warningMessages = new List<string>();

            m_IsValidFile = extraDataSerializedObject.FindProperty("isValidFile");
            m_TechnicalName = extraDataSerializedObject.FindProperty("info.technicalName");
            m_DisplayName = extraDataSerializedObject.FindProperty("info.displayName");
            m_Version = extraDataSerializedObject.FindProperty("info.version");
            m_AuthorEnabled = extraDataSerializedObject.FindProperty("info.author.enabled");
            m_AuthorName = extraDataSerializedObject.FindProperty("info.author.name");
            m_AuthorUrl = extraDataSerializedObject.FindProperty("info.author.url");
            m_AuthorEmail = extraDataSerializedObject.FindProperty("info.author.email");
            m_UnityVersionEnabled = extraDataSerializedObject.FindProperty("info.unityVersion.enabled");
            m_UnityMajor = extraDataSerializedObject.FindProperty("info.unityVersion.major");
            m_UnityMinor = extraDataSerializedObject.FindProperty("info.unityVersion.minor");
            m_UnityRelease = extraDataSerializedObject.FindProperty("info.unityVersion.release");
            m_Description = extraDataSerializedObject.FindProperty("info.description");
            m_DocumentationUrl = extraDataSerializedObject.FindProperty("info.documentationUrl");
            m_LicensesUrl = extraDataSerializedObject.FindProperty("info.licensesUrl");
            m_ChangelogUrl = extraDataSerializedObject.FindProperty("info.changelogUrl");
            m_Type = extraDataSerializedObject.FindProperty("info.type");
            m_Advanced = extraDataSerializedObject.FindProperty("info.settings");
            m_Visibility = extraDataSerializedObject.FindProperty("info.settings.visibility");
            m_DependenciesList = new ReorderableList(extraDataSerializedObject,
                extraDataSerializedObject.FindProperty("dependencies"), true, true, true, true)
            {
                drawElementCallback = DrawDependencyListElement,
                drawHeaderCallback = DrawDependencyHeaderElement,
                elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing
            };
        }

        protected override void InitializeExtraDataInstance(Object extraData, int targetIndex)
        {
            ReadPackageManifest(targets[targetIndex], (PackageManifestState)extraData);
        }

        private void DrawDependencyHeaderElement(Rect rect)
        {
            if (isFeatureSet)
            {
                GUI.Label(rect, Styles.package, EditorStyles.label);
                return;
            }

            var w = rect.width;
            rect.x += 4;
            rect.width = w / 3 * 2 - 2;
            GUI.Label(rect, Styles.package, EditorStyles.label);

            rect.x += w / 3 * 2;
            rect.width = w / 3 - 4;
            GUI.Label(rect, Styles.version, EditorStyles.label);
        }

        private void DrawDependencyListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var list = m_DependenciesList.serializedProperty;
            var dependency = list.GetArrayElementAtIndex(index);
            var packageName = dependency.FindPropertyRelative(k_ManifestFieldPackageName);
            var version = dependency.FindPropertyRelative(k_ManifestFieldVersion);

            var w = rect.width;
            if (!isFeatureSet)
            {
                rect.x += 4;
                rect.width = w / 3 * 2 - 2;
            }
            rect.height -= EditorGUIUtility.standardVerticalSpacing;
            packageName.stringValue = EditorGUI.TextField(rect, packageName.stringValue);

            if (!string.IsNullOrWhiteSpace(packageName.stringValue) && !PackageValidator.ValidateCompleteTechnicalName(packageName.stringValue))
                errorMessages.Add($"Invalid Dependency Package Name '{packageName.stringValue}'");

            if (isFeatureSet)
                return;

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(packageName.stringValue)))
            {
                rect.x += w / 3 * 2;
                rect.width = w / 3 - 4;
                version.stringValue = EditorGUI.TextField(rect, version.stringValue);

                if (!string.IsNullOrWhiteSpace(version.stringValue))
                    ValidateVersion(packageName.stringValue, version.stringValue, errorMessages, warningMessages);
            }
        }

        protected override bool CanApply()
        {
            return errorMessages.Count == 0;
        }

        protected override void Apply()
        {
            base.Apply();

            for (int i = 0; i < targets.Length; i++)
            {
                WritePackageManifest(targets[i], (PackageManifestState)extraDataTargets[i]);
            }
        }

        protected override bool OnApplyRevertGUI()
        {
            using (new EditorGUI.DisabledScope(!HasModified()))
            {
                RevertButton();
                using (new EditorGUI.DisabledScope(errorMessages.Count > 0))
                {
                    return ApplyButton();
                }
            }
        }

        private void DoPackageInformationLayout()
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true)))
            {
                EditorGUILayout.PropertyField(m_TechnicalName, Styles.technicalName);

                GUILayout.Space(10);
                EditorGUILayout.PropertyField(m_DisplayName, Styles.displayName);
                EditorGUILayout.PropertyField(m_Version, Styles.version);
                EditorGUILayout.PropertyField(m_DocumentationUrl, Styles.documentationUrl);
                EditorGUILayout.PropertyField(m_LicensesUrl, Styles.licensesUrl);
                EditorGUILayout.PropertyField(m_ChangelogUrl, Styles.changelogUrl);

                GUILayout.Space(10);
                EditorGUILayout.PropertyField(m_AuthorEnabled, Styles.author);
                if (m_AuthorEnabled.boolValue)
                {
                    EditorGUILayout.PropertyField(m_AuthorName, Styles.authorName);
                    EditorGUILayout.PropertyField(m_AuthorUrl, Styles.authorUrl);
                    EditorGUILayout.PropertyField(m_AuthorEmail, Styles.authorEmail);
                }
                if (isFeatureSet)
                    return;

                GUILayout.Space(10);
                EditorGUILayout.PropertyField(m_UnityVersionEnabled, Styles.unityVersion);
                if (m_UnityVersionEnabled.boolValue)
                {
                    EditorGUI.showMixedValue = m_UnityMajor.hasMultipleDifferentValues;
                    m_UnityMajor.stringValue = EditorGUILayout.TextFieldDropDown(Styles.unityMajor,
                        m_UnityMajor.stringValue, MajorUnityVersions.ToArray());

                    EditorGUI.showMixedValue = m_UnityMinor.hasMultipleDifferentValues;
                    m_UnityMinor.stringValue = EditorGUILayout.TextFieldDropDown(Styles.unityMinor,
                        m_UnityMinor.stringValue, MinorUnityVersions.ToArray());

                    EditorGUI.showMixedValue = m_UnityRelease.hasMultipleDifferentValues;
                    m_UnityRelease.stringValue =
                        EditorGUILayout.TextField(Styles.unityRelease, m_UnityRelease.stringValue);
                    EditorGUI.showMixedValue = false;
                }
                GUILayout.Space(10);
            }
        }

        private void DoPackageDescriptionLabel()
        {
            var descriptionStyle = EditorStyles.textArea;
            var description = m_Description.stringValue ?? "";
            var descriptionRect = GUILayoutUtility.GetRect(EditorGUIUtility.TempContent(description), descriptionStyle, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            EditorGUI.SelectableLabel(descriptionRect, description, descriptionStyle);
        }

        private void DoPackageDescriptionLayout()
        {
            var previousEnabled = GUI.enabled;
            GUI.enabled = true;

            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true)))
            {
                using (var scrollView = new EditorGUILayout.VerticalScrollViewScope(descriptionScrollViewPosition, GUILayout.MinHeight(k_MinHeightDescriptionScrollView)))
                {
                    descriptionScrollViewPosition = scrollView.scrollPosition;

                    // We want to have text we can edit instead of selectable label when it's in Edit mode
                    if (previousEnabled == true)
                        m_Description.stringValue = EditorGUILayout.TextArea(m_Description.stringValue ?? "",
                        GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                    else
                        DoPackageDescriptionLabel();
                }

                GUILayout.Space(10);
            }

            GUI.enabled = previousEnabled;
        }

        private void PerformValidation()
        {
            if (!PackageValidator.ValidateCompleteTechnicalName(m_TechnicalName.stringValue))
                errorMessages.Add($"Invalid Complete Package Name '{m_TechnicalName.stringValue}'");

            ValidateVersion(null, m_Version.stringValue, errorMessages, warningMessages);

            if (m_UnityVersionEnabled.boolValue)
            {
                if (!PackageValidator.ValidateUnityVersion(m_UnityMajor.stringValue, m_UnityMinor.stringValue,
                    m_UnityRelease.stringValue))
                {
                    var unityVersion = $"{m_UnityMajor.stringValue}.{m_UnityMinor.stringValue}";
                    if (!string.IsNullOrWhiteSpace(m_UnityRelease.stringValue))
                        unityVersion += "." + m_UnityRelease.stringValue.Trim();

                    errorMessages.Add($"Invalid Unity Version '{unityVersion}'");
                }
            }

            if (string.IsNullOrWhiteSpace(m_DisplayName.stringValue) || m_DisplayName.stringValue.Trim().Length == 0)
            {
                warningMessages.Add("Display name should be provided.");
            }

            if (string.IsNullOrWhiteSpace(m_Description.stringValue) || m_Description.stringValue.Trim().Length == 0)
            {
                warningMessages.Add("Package description should be provided.");
            }

            if ((PackageVisibility)m_Visibility.intValue == PackageVisibility.AlwaysHidden)
            {
                warningMessages.Add("This package and all its assets will be hidden by default in Editor because its visibility is set to 'Always Hidden'");
            }
            if ((PackageVisibility)m_Visibility.intValue == PackageVisibility.AlwaysVisible)
            {
                warningMessages.Add("This package and all its assets will be visible by default in Editor because its visibility is set to 'Always Visible'");
            }
        }

        public override void OnInspectorGUI()
        {
            extraDataSerializedObject.Update();

            if (!m_IsValidFile.boolValue)
            {
                EditorGUILayout.HelpBox(s_LocalizedInvalidPackageManifest, MessageType.Error);
                return;
            }

            errorMessages.Clear();
            warningMessages.Clear();

            // Package information
            GUILayout.Label(Styles.information, EditorStyles.boldLabel);
            DoPackageInformationLayout();

            // Package description
            GUILayout.Label(Styles.description, EditorStyles.boldLabel);
            DoPackageDescriptionLayout();

            // Package dependencies
            if (m_DependenciesList.index < 0 && m_DependenciesList.count > 0)
                m_DependenciesList.index = 0;

            var dependenciesTitleText = EditorGUIUtility.TrTextContent(
                isFeatureSet ? "Packages included" : "Dependencies");
            GUILayout.Label(dependenciesTitleText, EditorStyles.boldLabel);
            m_DependenciesList.DoLayoutList();

            // Package advanced settings
            EditorGUILayout.PropertyField(m_Advanced, Styles.showAdvanced, false);
            if (m_Advanced.isExpanded)
            {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true)))
                {
                    EditorGUILayout.PropertyField(m_Visibility, Styles.visibility);
                }
            }

            // Validation
            PerformValidation();

            extraDataSerializedObject.ApplyModifiedProperties();

            ApplyRevertGUI();

            if (isFeatureSet)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Customization of a feature is not supported. Doing this may break your project. Use at your own risk.", MessageType.Warning);
            }

            if (errorMessages.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(string.Join("\n", errorMessages), MessageType.Error);
            }

            if (warningMessages.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(string.Join("\n", warningMessages), MessageType.Warning);
            }
        }

        internal override Rect DrawHeaderHelpAndSettingsGUI(Rect r)
        {
            return new Rect(r.width, 0, 0, 0);
        }

        private static void ReadPackageManifest(Object target, PackageManifestState packageState)
        {
            var importer = target as PackageManifestImporter;
            if (importer == null)
                return;

            var assetPath = importer.assetPath;

            try
            {
                var ioProxy = ServicesContainer.instance.Resolve<IIOProxy>();
                var jsonString = ioProxy.FileReadAllText(assetPath);
                var info = Json.Deserialize(jsonString) as Dictionary<string, object>;

                packageState.dependencies = new List<PackageDependency>();
                packageState.info = new PackageInformation();
                packageState.isValidFile = info != null &&
                    info.TryGetValue(k_ManifestFieldName, out var pkgName) && pkgName is string &&
                    info.TryGetValue(k_ManifestFieldVersion, out var version) && version is string;


                if (packageState.isValidFile)
                {
                    if (info.TryGetValue(k_ManifestFieldDisplayName, out var displayName) && displayName is string displayNameString)
                        packageState.info.displayName = displayNameString;

                    packageState.info.technicalName = info[k_ManifestFieldName] as string;
                    packageState.info.version = info[k_ManifestFieldVersion] as string;

                    if (info.TryGetValue(k_ManifestFieldDescription, out var description) && description is string descriptionString)
                        packageState.info.description = descriptionString;

                    if (info.TryGetValue(k_ManifestFieldType, out var type) && type is string typeString)
                        packageState.info.type = typeString;

                    if (info.TryGetValue(k_ManifestFieldHideEditor, out var hideInEditor) && hideInEditor is bool hideInEditorValue)
                        packageState.info.settings.visibility = hideInEditorValue ? PackageVisibility.AlwaysHidden : PackageVisibility.AlwaysVisible;
                    else
                        packageState.info.settings.visibility = PackageVisibility.DefaultVisibility;

                    if (info.TryGetValue(k_ManifestFieldAuthor, out var value))
                    {
                        if (value is Dictionary<string, object> authorInfo)
                            packageState.info.author = new PackageAuthor
                            {
                                enabled = true,
                                name = authorInfo.TryGetValue(k_ManifestFieldName, out var name) ? name as string : string.Empty,
                                url = authorInfo.TryGetValue(k_ManifestFieldUrl, out var url) ? url as string : string.Empty,
                                email = authorInfo.TryGetValue(k_ManifestFieldEmail, out var email) ? email as string : string.Empty
                            };
                        else
                            packageState.info.author = new PackageAuthor
                            {
                                enabled = true,
                                name = value as string ?? string.Empty,
                                url = string.Empty,
                                email = string.Empty
                            };
                    }
                    else
                    {
                        packageState.info.author = new PackageAuthor
                        {
                            enabled = false,
                            name = string.Empty,
                            url = string.Empty,
                            email = string.Empty
                        };
                    }

                    if (info.TryGetValue(k_ManifestFieldUnity, out var unity) && unity is string unityString)
                    {
                        var splitVersions = unityString.Split('.');
                        packageState.info.unityVersion = new PackageUnityVersion
                        {
                            enabled = true,
                            major = splitVersions[0],
                            minor = splitVersions.Length > 1 ? splitVersions[1] : "",
                            release = ""
                        };

                        if (info.TryGetValue(k_ManifestFieldUnityRelease, out var unityRelease) && unityRelease is string unityReleaseString)
                            packageState.info.unityVersion.release = unityReleaseString;
                    }
                    else
                    {
                        var unityVersion = InternalEditorUtility.GetUnityVersion();
                        packageState.info.unityVersion = new PackageUnityVersion
                        {
                            enabled = false,
                            major = unityVersion.Major.ToString(),
                            minor = unityVersion.Minor.ToString(),
                            release = ""
                        };
                    }

                    if (info.TryGetValue(k_ManifestFieldDependencies, out var dependencies))
                    {
                        if (dependencies is not IDictionary)
                        {
                            packageState.isValidFile = false;
                            return;
                        }
                        var dependenciesDictionary = (IDictionary) dependencies;
                        foreach (var packageName in dependenciesDictionary.Keys)
                        {
                            var dependency = new PackageDependency
                            {
                                packageName = packageName as string,
                                version = dependenciesDictionary[packageName] as string
                            };
                            packageState.dependencies.Add(dependency);
                        }
                    }

                    if (info.TryGetValue(k_ManifestFieldDocumentationUrl, out var docUrl) && docUrl is string docUrlString)
                        packageState.info.documentationUrl = docUrlString;

                    if (info.TryGetValue(k_ManifestFieldLicensesUrl, out var licenseUrl) && licenseUrl is string licenseUrlString)
                        packageState.info.licensesUrl = licenseUrlString;

                    if (info.TryGetValue(k_ManifestFieldChangelogUrl, out var changelogUrl) && changelogUrl is string changelogUrlString)
                        packageState.info.changelogUrl = changelogUrlString;

                }
            }
            catch (System.IO.IOException)
            {
                Debug.Log($"Couldn't open package manifest file {assetPath}.");
                packageState.isValidFile = false;
            }
        }

        private void WritePackageManifest(Object target, PackageManifestState packageState)
        {
            var importer = target as PackageManifestImporter;
            if (importer == null)
                return;

            var ioProxy = ServicesContainer.instance.Resolve<IIOProxy>();
            var assetPath = importer.assetPath;
            Dictionary<string, object> json = null;

            try
            {
                var jsonString = ioProxy.FileReadAllText(assetPath);
                json = Json.Deserialize(jsonString) as Dictionary<string, object>;
            }
            catch (System.IO.IOException)
            {
                Debug.Log($"Couldn't open package manifest file {assetPath}.");
            }

            if (json == null)
                return;

            var renameFolder = false;
            var newTechnicalName = packageState.info.technicalName?.Trim();
            if (!string.IsNullOrEmpty(newTechnicalName))
            {
                renameFolder = newTechnicalName != json[k_ManifestFieldName] as string
                               && ServicesContainer.instance.Resolve<IApplicationProxy>().DisplayDialog(
                                   "matchPackageFolderName",
                                   L10n.Tr("Update Folder Name to Match"),
                                   L10n.Tr("You changed the package’s technical name. Do you also want to update the package’s folder to match the technical name?"), "Update Name", "Keep Current");
                json[k_ManifestFieldName] = newTechnicalName;
            }

            if (!string.IsNullOrWhiteSpace(packageState.info.displayName))
                json[k_ManifestFieldDisplayName] = packageState.info.displayName.Trim();
            else
                json.Remove(k_ManifestFieldDisplayName);

            json[k_ManifestFieldVersion] = packageState.info.version;

            if (!string.IsNullOrWhiteSpace(packageState.info.description))
                json[k_ManifestFieldDescription] = packageState.info.description.Trim();
            else
                json.Remove(k_ManifestFieldDescription);

            if (!string.IsNullOrWhiteSpace(packageState.info.type))
                json[k_ManifestFieldType] = packageState.info.type.Trim();
            else
                json.Remove(k_ManifestFieldType);

            if (packageState.info.settings.visibility == PackageVisibility.DefaultVisibility)
                json.Remove(k_ManifestFieldHideEditor);
            else
                json[k_ManifestFieldHideEditor] = packageState.info.settings.visibility == PackageVisibility.AlwaysHidden;

            if (packageState.info.author.enabled)
            {
                var authorInfo = new Dictionary<string, object>();
                if (!string.IsNullOrWhiteSpace(packageState.info.author.name))
                    authorInfo[k_ManifestFieldName] = packageState.info.author.name.Trim();
                else
                    authorInfo.Remove(k_ManifestFieldName);
                if (!string.IsNullOrWhiteSpace(packageState.info.author.url))
                    authorInfo[k_ManifestFieldUrl] = packageState.info.author.url.Trim();
                else
                    authorInfo.Remove(k_ManifestFieldUrl);
                if (!string.IsNullOrWhiteSpace(packageState.info.author.email))
                    authorInfo[k_ManifestFieldEmail] = packageState.info.author.email.Trim();
                else
                    authorInfo.Remove(k_ManifestFieldEmail);
                json[k_ManifestFieldAuthor] = authorInfo;
            }
            else
            {
                json.Remove(k_ManifestFieldAuthor);
            }

            if (packageState.info.unityVersion.enabled)
            {
                if (!string.IsNullOrWhiteSpace(packageState.info.unityVersion.major) &&
                    !string.IsNullOrWhiteSpace(packageState.info.unityVersion.minor))
                {
                    json[k_ManifestFieldUnity] = $"{packageState.info.unityVersion.major.Trim()}.{packageState.info.unityVersion.minor.Trim()}";
                    if (!string.IsNullOrWhiteSpace(packageState.info.unityVersion.release))
                        json[k_ManifestFieldUnityRelease] = packageState.info.unityVersion.release.Trim();
                    else
                        json.Remove(k_ManifestFieldUnityRelease);
                }
            }
            else
            {
                json.Remove(k_ManifestFieldUnity);
                json.Remove(k_ManifestFieldUnityRelease);
            }

            if (packageState.dependencies.Count > 0)
            {
                var dependencies = new Dictionary<string, string>();
                foreach (var dependency in packageState.dependencies)
                {
                    if (!string.IsNullOrWhiteSpace(dependency.packageName))
                        dependencies.Add(dependency.packageName.Trim(), isFeatureSet ? "default" : dependency.version);
                }

                json[k_ManifestFieldDependencies] = dependencies;
            }
            else
                json.Remove(k_ManifestFieldDependencies);

            if (!string.IsNullOrWhiteSpace(packageState.info.documentationUrl))
                json[k_ManifestFieldDocumentationUrl] = packageState.info.documentationUrl.Trim();
            else
                json.Remove(k_ManifestFieldDocumentationUrl);

            if (!string.IsNullOrWhiteSpace(packageState.info.licensesUrl))
                json[k_ManifestFieldLicensesUrl] = packageState.info.licensesUrl.Trim();
            else
                json.Remove(k_ManifestFieldLicensesUrl);

            if (!string.IsNullOrWhiteSpace(packageState.info.changelogUrl))
                json[k_ManifestFieldChangelogUrl] = packageState.info.changelogUrl.Trim();
            else
                json.Remove(k_ManifestFieldChangelogUrl);

            try
            {
                ioProxy.FileWriteAllText(assetPath, Json.Serialize(json, true));

                if (renameFolder)
                {
                    var packageFolder = IOUtils.GetParentDirectory(assetPath);
                    var newPackageFolder = IOUtils.PathsCombine(IOUtils.GetParentDirectory(packageFolder), packageState.info.technicalName);
                    ioProxy.Move(packageFolder, newPackageFolder);
                }
            }
            catch (System.IO.IOException)
            {
                Debug.Log($"Couldn't write package manifest file {assetPath}.");
            }
            catch (UnauthorizedAccessException)
            {
                Debug.LogError($"Access denied when accessing package manifest file {assetPath}. Please make sure the file is not read-only.");
            }

            Client.Resolve();
        }

        [OnOpenAsset(OnOpenAssetAttributeMode.Validate)]
        private static bool OnOpenAsset(EntityId instanceID, int line, int column)
        {
            var selected = EditorUtility.EntityIdToObject(instanceID);
            var assetPath = AssetDatabase.GetAssetPath(selected);

            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            return assetPath.EndsWith("/package.json", StringComparison.OrdinalIgnoreCase);
        }

        internal static void ValidateVersion(string packageName, string version, List<string> errorMessages, List<string> warningMessages)
        {
            if (!PackageValidator.ValidateVersion(version, out var majorStr, out var minorStr, out var patchStr))
            {
                if (string.IsNullOrEmpty(packageName))
                    errorMessages.Add($"Invalid version '{version}'");
                else
                    errorMessages.Add($"Invalid version '{version}' for dependency '{packageName}'");
            }
            else
            {
                if (!long.TryParse(majorStr, out var major) || major > k_MaxVersion ||
                    !long.TryParse(minorStr, out var minor) || minor > k_MaxVersion ||
                    !long.TryParse(patchStr, out var patch) || patch > k_MaxVersion)
                {
                    if (string.IsNullOrEmpty(packageName))
                        errorMessages.Add($"Each component of version '{version}' must be an integer less than or equal to {k_MaxVersion}.");
                    else
                        errorMessages.Add($"Each component of version '{version}' for dependency '{packageName}' must be an integer less than or equal to {k_MaxVersion}.");
                }
                else if (major > k_RecommendedMaxVersion || minor > k_RecommendedMaxVersion || patch > k_RecommendedMaxVersion)
                {
                    if (string.IsNullOrEmpty(packageName))
                        warningMessages.Add($"Consider to use an integer less than or equal to {k_RecommendedMaxVersion} for each component of version '{version}'.");
                    else
                        warningMessages.Add($"Consider to use an integer less than or equal to {k_RecommendedMaxVersion} for each component of version '{version}' for dependency '{packageName}'.");
                }
            }
        }
    }
}
