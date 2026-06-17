// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor.AssetImporters;
using UnityEditor.Callbacks;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

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

        private const string k_DefaultDependencyVersion = "0.1.0";

        private static List<string> s_MajorUnityVersions;
        private static List<string> MajorUnityVersions
        {
            get
            {
                if (s_MajorUnityVersions != null)
                    return s_MajorUnityVersions;

                var version = InternalEditorUtility.GetUnityVersion();
                s_MajorUnityVersions = new List<string> { "2020", "2021", "2022" };
                for (var majorVersion = 6000; majorVersion <= version.Major; majorVersion += 1000)
                    s_MajorUnityVersions.Add(majorVersion.ToString());
                return s_MajorUnityVersions;
            }
        }

        private static readonly List<string> MinorUnityVersionsPrior6 = new List<string> { "0", "1", "2", "3" };
        private static List<string> s_MinorUnityVersions = null;
        private static List<string> MinorUnityVersions
        {
            get
            {
                if (s_MinorUnityVersions != null)
                    return s_MinorUnityVersions;

                var version = InternalEditorUtility.GetUnityVersion();
                var maxMinor = version.Minor;
                s_MinorUnityVersions = new List<string>();
                for (var minorVersion = 0; minorVersion <= maxMinor; minorVersion++)
                    s_MinorUnityVersions.Add(minorVersion.ToString());
                return s_MinorUnityVersions;
            }
        }

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

            [System.NonSerialized]
            public readonly Dictionary<string, PropertyErrorsAndWarnings> propertiesErrorsAndWarnings = new Dictionary<string, PropertyErrorsAndWarnings>();

            [System.NonSerialized]
            public bool inspectorHasErrors = false;
        }

        PackageManifestState packageState => extraDataTarget as PackageManifestState;

        private static class Styles
        {
            public static readonly GUIContent information = EditorGUIUtility.TrTextContent("Information");
            public static readonly GUIContent technicalName = EditorGUIUtility.TrTextContent("Technical Name", "A unique multi-part identifier in lowercase, using reverse domain name notation. Example: com.companyname.packagename");

            public static readonly GUIContent displayName = EditorGUIUtility.TrTextContent("Display Name", "A user-friendly name that appears in the Unity Editor, such as in the Project window and the Package Manager window.");
            public static readonly GUIContent version = EditorGUIUtility.TrTextContent("Version", "Must follow Semantic Versioning conventions. For example, 1.2.2 or 1.0.0-pre.2.");
            public static readonly GUIContent type = EditorGUIUtility.TrTextContent("Type", "Type (optional).");

            public static readonly GUIContent showAdvanced = EditorGUIUtility.TrTextContent("Advanced", "Show advanced settings.");

            public static readonly GUIContent visibility = EditorGUIUtility.TrTextContent("Visibility in Editor", "Determines the visibility of the package in the Project window and the object picker of the Inspector window.");

            public static readonly GUIContent author = EditorGUIUtility.TrTextContent("Author", "The author of the package.");
            public static readonly GUIContent authorName = EditorGUIUtility.TrTextContent("Name", "Author name.");
            public static readonly GUIContent authorUrl = EditorGUIUtility.TrTextContent("URL", "Author's website.");
            public static readonly GUIContent authorEmail = EditorGUIUtility.TrTextContent("Email", "Author's email address.");

            public static readonly GUIContent unityVersion = EditorGUIUtility.TrTextContent("Minimum Unity version", "The minimum Unity version this package supports.");
            public static readonly GUIContent unityMajor = EditorGUIUtility.TrTextContent("Major", "Major version of Unity");
            public static readonly GUIContent unityMinor = EditorGUIUtility.TrTextContent("Minor", "Minor version of Unity");
            public static readonly GUIContent unityRelease = EditorGUIUtility.TrTextContent("Release", "Specific release (ex: 0a9)");

            public static readonly GUIContent documentationUrl = EditorGUIUtility.TrTextContent("Documentation URL", "Custom location for this package’s documentation, specified as a URL.");
            public static readonly GUIContent licensesUrl = EditorGUIUtility.TrTextContent("Licenses URL", "Custom location for this package’s license information, specified as a URL.");
            public static readonly GUIContent changelogUrl = EditorGUIUtility.TrTextContent("Changelog URL", "Custom location for this package’s changelog, specified as a URL.");

            public static readonly GUIContent description = EditorGUIUtility.TrTextContent("Brief description", "Descriptive text that appears in the details panel of the Package Manager window. This field supports rich text formatting tags.");

            public static readonly GUIContent dependenciesAsPackage = EditorGUIUtility.TrTextContent("Dependencies", "Other packages that this package depends on.");
            public static readonly GUIContent dependenciesAsFeatureset = EditorGUIUtility.TrTextContent("Packages included", "Packages that are part of this feature set.");
            public static readonly GUIContent package = EditorGUIUtility.TrTextContent("Technical Name of package", "A unique multi-part identifier in lowercase, using reverse domain name notation. Example: com.companyname.packagename");

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

        internal class PropertyErrorsAndWarnings
        {
            public List<string> m_ErrorMessages = new List<string>();
            public List<string> m_WarningMessages = new List<string>();
        }

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

        private IUpmCache m_UpmCache;
        private IOProxy m_IOProxy;
        private string m_AssetPath;

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

            m_UpmCache = ServicesContainer.instance.Resolve<IUpmCache>();
            m_IOProxy = ServicesContainer.instance.Resolve<IOProxy>();
            m_AssetPath = (target as PackageManifestImporter).assetPath;

            //Ensure UIElements handles the IMGUI container with margins
            alwaysAllowExpansion = true;

            if (packageState != null)
            {
                packageState.inspectorHasErrors = false;
                packageState.propertiesErrorsAndWarnings.Clear();
            }

            Undo.undoRedoPerformed += OnUndoRedoPerformed;

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
                onAddCallback = AddDependencyElement,
                elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing
            };
        }

        public override void OnDisable()
        {
            base.OnDisable();
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        protected override void InitializeExtraDataInstance(Object extraData, int targetIndex)
        {
            var data = (PackageManifestState)extraData;
            ReadPackageManifest(targets[targetIndex], data);
            data.propertiesErrorsAndWarnings.Clear();
            data.inspectorHasErrors = false;
        }

        private void AddDependencyElement(ReorderableList list)
        {
            new ReorderableList.Defaults().DoAddButton(list);
            var dependency = list.serializedProperty.GetArrayElementAtIndex(list.count - 1);
            var packageName = dependency.FindPropertyRelative(k_ManifestFieldPackageName);
            var version = dependency.FindPropertyRelative(k_ManifestFieldVersion);
            packageName.stringValue = "";
            version.stringValue = k_DefaultDependencyVersion;
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

            if (isFeatureSet)
                return;

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(packageName.stringValue)))
            {
                rect.x += w / 3 * 2;
                rect.width = w / 3 - 4;
                version.stringValue = EditorGUI.TextField(rect, version.stringValue);
            }
        }

        protected override bool CanApply()
        {
            return !packageState.inspectorHasErrors;
        }

        protected override void Apply()
        {
            base.Apply();

            if (packageState.inspectorHasErrors)
            {
                var errors = new List<string>();
                foreach (var property in packageState.propertiesErrorsAndWarnings)
                    errors.AddRange(property.Value.m_ErrorMessages);
                throw new InvalidOperationException(string.Format(L10n.Tr("The Inspector window contains errors, information can't be saved.\n{0}"), String.Join("\n", errors)));
            }


            for (int i = 0; i < targets.Length; i++)
            {
                WritePackageManifest(targets[i], (PackageManifestState)extraDataTargets[i]);
            }
        }

        protected override bool OnApplyRevertGUI()
        {
            using (new EditorGUI.DisabledScope(!HasModified()))
            {
                using (var buttonState = new EditorGUI.ChangeCheckScope())
                {
                    RevertButton();

                    if (buttonState.changed)
                        packageState.propertiesErrorsAndWarnings.Clear();
                }
                using (new EditorGUI.DisabledScope(packageState.inspectorHasErrors))
                {
                    return ApplyButton();
                }
            }
        }

        protected void OnUndoRedoPerformed()
        {
            if (packageState != null)
            {
                packageState.inspectorHasErrors = false;
                packageState.propertiesErrorsAndWarnings.Clear();
            }
        }

        private void DoPropertyFieldLayoutErrors(List<string> errorMessages)
        {
            foreach (var err in errorMessages)
                EditorGUILayout.HelpBox(err, MessageType.Error);
            packageState.inspectorHasErrors = packageState.inspectorHasErrors || errorMessages.Count > 0;
        }
        private void DoPropertyFieldLayoutWarnings(List<string> warningMessages)
        {
            foreach (var err in warningMessages)
                EditorGUILayout.HelpBox(err, MessageType.Warning);
        }

        private void DoPropertyFieldLayoutErrorsAndWarnings(SerializedProperty property)
        {
            if (packageState.propertiesErrorsAndWarnings.TryGetValue(property.propertyPath, out var propertyErrorsAndWarnings))
            {
                DoPropertyFieldLayoutErrors(propertyErrorsAndWarnings.m_ErrorMessages);
                DoPropertyFieldLayoutWarnings(propertyErrorsAndWarnings.m_WarningMessages);
            }
        }

        private bool DoPropertyFieldLayout(SerializedProperty property, GUIContent style)
        {
            using (var propertyState = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(property, style);
                if (propertyState.changed || !packageState.propertiesErrorsAndWarnings.ContainsKey(property.propertyPath))
                    return true;
            }
            return false;
        }

        private void DoPackageInformationLayout()
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true)))
            {
                if (DoPropertyFieldLayout(m_TechnicalName, Styles.technicalName))
                    ValidateTechnicalName(m_TechnicalName, m_UpmCache.GetInstalledPackageInfo(m_TechnicalName.stringValue), m_AssetPath);
                DoPropertyFieldLayoutErrorsAndWarnings(m_TechnicalName);

                GUILayout.Space(10);

                if (DoPropertyFieldLayout(m_DisplayName, Styles.displayName))
                    ValidateDisplayName(m_DisplayName);
                DoPropertyFieldLayoutErrorsAndWarnings(m_DisplayName);

                if (DoPropertyFieldLayout(m_Version, Styles.version))
                    ValidateVersionProperty(m_Version);
                DoPropertyFieldLayoutErrorsAndWarnings(m_Version);

                if (DoPropertyFieldLayout(m_DocumentationUrl, Styles.documentationUrl))
                    ValidateUrl(m_DocumentationUrl);
                DoPropertyFieldLayoutErrorsAndWarnings(m_DocumentationUrl);

                if (DoPropertyFieldLayout(m_LicensesUrl, Styles.licensesUrl))
                    ValidateUrl(m_LicensesUrl);
                DoPropertyFieldLayoutErrorsAndWarnings(m_LicensesUrl);

                if (DoPropertyFieldLayout(m_ChangelogUrl, Styles.changelogUrl))
                    ValidateUrl(m_ChangelogUrl);
                DoPropertyFieldLayoutErrorsAndWarnings(m_ChangelogUrl);

                GUILayout.Space(10);
                EditorGUILayout.PropertyField(m_AuthorEnabled, Styles.author);
                if (m_AuthorEnabled.boolValue)
                {
                    if (DoPropertyFieldLayout(m_AuthorName, Styles.authorName))
                        ValidateAuthorName(m_AuthorName);
                    DoPropertyFieldLayoutErrorsAndWarnings(m_AuthorName);

                    if (DoPropertyFieldLayout(m_AuthorUrl, Styles.authorUrl))
                        ValidateUrl(m_AuthorUrl);
                    DoPropertyFieldLayoutErrorsAndWarnings(m_AuthorUrl);

                    if (DoPropertyFieldLayout(m_AuthorEmail, Styles.authorEmail))
                        ValidateEmail(m_AuthorEmail);
                    DoPropertyFieldLayoutErrorsAndWarnings(m_AuthorEmail);
                }
                if (isFeatureSet)
                    return;

                GUILayout.Space(10);
                if (DoPropertyFieldLayout(m_UnityVersionEnabled, Styles.unityVersion))
                    ValidateUnityVersionEnabled(m_UnityVersionEnabled);
                DoPropertyFieldLayoutErrorsAndWarnings(m_UnityVersionEnabled);
                if (m_UnityVersionEnabled.boolValue)
                {
                    using (var propertiesState = new EditorGUI.ChangeCheckScope())
                    {
                        EditorGUI.showMixedValue = m_UnityMajor.hasMultipleDifferentValues;
                        m_UnityMajor.stringValue = EditorGUILayout.TextFieldDropDown(Styles.unityMajor,
                            m_UnityMajor.stringValue, MajorUnityVersions.ToArray());

                        var minorVersionList = MinorUnityVersions;
                        if (!string.IsNullOrEmpty(m_UnityMajor.stringValue) && long.TryParse(m_UnityMajor.stringValue, out var major) &&
                            major < 6000)
                            minorVersionList = MinorUnityVersionsPrior6;
                        EditorGUI.showMixedValue = m_UnityMinor.hasMultipleDifferentValues;
                        m_UnityMinor.stringValue = EditorGUILayout.TextFieldDropDown(Styles.unityMinor,
                            m_UnityMinor.stringValue, minorVersionList.ToArray());

                        EditorGUI.showMixedValue = m_UnityRelease.hasMultipleDifferentValues;
                        m_UnityRelease.stringValue =
                            EditorGUILayout.TextField(Styles.unityRelease, m_UnityRelease.stringValue);
                        EditorGUI.showMixedValue = false;

                        if (propertiesState.changed || !packageState.propertiesErrorsAndWarnings.ContainsKey(m_UnityMajor.propertyPath))
                            ValidateUnityVersion(m_UnityMajor, m_UnityMinor, m_UnityRelease);
                    }
                    DoPropertyFieldLayoutErrorsAndWarnings(m_UnityMajor);
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
                List<string> warningMessages = new List<string>();

                using (var scrollView = new EditorGUILayout.VerticalScrollViewScope(descriptionScrollViewPosition, GUILayout.MinHeight(k_MinHeightDescriptionScrollView)))
                {
                    descriptionScrollViewPosition = scrollView.scrollPosition;

                    // We want to have text we can edit instead of selectable label when it's in Edit mode
                    if (previousEnabled == true)
                    {
                        using (var propertyState = new EditorGUI.ChangeCheckScope())
                        {
                            m_Description.stringValue = EditorGUILayout.TextArea(m_Description.stringValue ?? "",
                            GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

                            if (propertyState.changed || !packageState.propertiesErrorsAndWarnings.ContainsKey(m_Description.propertyPath))
                                ValidateDescription(m_Description);
                        }
                    }
                    else
                        DoPackageDescriptionLabel();
                }
                DoPropertyFieldLayoutErrorsAndWarnings(m_Description);

                GUILayout.Space(10);
            }

            GUI.enabled = previousEnabled;
        }

        public override void OnInspectorGUI()
        {
            extraDataSerializedObject.Update();

            if (!m_IsValidFile.boolValue)
            {
                EditorGUILayout.HelpBox(s_LocalizedInvalidPackageManifest, MessageType.Error);
                return;
            }

            packageState.inspectorHasErrors = false;

            // Package information
            GUILayout.Label(Styles.information, EditorStyles.boldLabel);
            DoPackageInformationLayout();

            // Package description
            GUILayout.Label(Styles.description, EditorStyles.boldLabel);
            DoPackageDescriptionLayout();

            // Package dependencies
            if (m_DependenciesList.index < 0 && m_DependenciesList.count > 0)
                m_DependenciesList.index = 0;

            var dependenciesTitle = isFeatureSet ? Styles.dependenciesAsFeatureset : Styles.dependenciesAsPackage;
            GUILayout.Label(dependenciesTitle, EditorStyles.boldLabel);

            using (var propertyState = new EditorGUI.ChangeCheckScope())
            {
                m_DependenciesList.DoLayoutList();
                if (propertyState.changed || !packageState.propertiesErrorsAndWarnings.ContainsKey(m_DependenciesList.serializedProperty.propertyPath))
                    ValidateDependenciesList(m_DependenciesList.serializedProperty, m_DependenciesList.count);
            }
            DoPropertyFieldLayoutErrorsAndWarnings(m_DependenciesList.serializedProperty);

            // Package advanced settings
            EditorGUILayout.PropertyField(m_Advanced, Styles.showAdvanced, false);
            if (m_Advanced.isExpanded)
            {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true)))
                {
                    if (DoPropertyFieldLayout(m_Visibility, Styles.visibility))
                        ValidatePackageVisibility(m_Visibility);
                    DoPropertyFieldLayoutErrorsAndWarnings(m_Visibility);
                }
            }

            extraDataSerializedObject.ApplyModifiedProperties();

            ApplyRevertGUI();

            if (isFeatureSet)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(L10n.Tr("Customization of a feature is not supported. Doing this may break your project. Use at your own risk."), MessageType.Warning);
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
                        var dependenciesDictionary = (IDictionary)dependencies;
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
                Debug.Log(string.Format(L10n.Tr("Couldn't open package manifest file {0}."), assetPath));
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
                Debug.Log(string.Format(L10n.Tr("Couldn't open package manifest file {0}."), assetPath));
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
                    var packageFolder = m_IOProxy.GetParentDirectory(assetPath);
                    var newPackageFolder = m_IOProxy.PathsCombine(m_IOProxy.GetParentDirectory(packageFolder), packageState.info.technicalName);
                    ioProxy.Move(packageFolder, newPackageFolder);
                }
            }
            catch (System.IO.IOException)
            {
                Debug.Log(string.Format(L10n.Tr("Couldn't write package manifest file {0}."), assetPath));
            }
            catch (UnauthorizedAccessException)
            {
                Debug.LogError(string.Format(L10n.Tr("Access denied when accessing package manifest file {0}. Please make sure the file is not read-only."), assetPath));
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

        internal void ValidateDependencyVersion(PropertyErrorsAndWarnings propertyErrorsAndWarnings, SerializedProperty version, string packageName)
        {
            if (string.IsNullOrEmpty(version.stringValue))
                propertyErrorsAndWarnings.m_ErrorMessages.Add(string.Format(L10n.Tr("Version is mandatory and missing for dependency '{0}'."), packageName));
            else if (!PackageValidator.ValidateVersion(version.stringValue, out var majorStr, out var minorStr, out var patchStr))
                propertyErrorsAndWarnings.m_ErrorMessages.Add(string.Format(L10n.Tr("Invalid version '{0}' for dependency '{1}'."), version.stringValue, packageName));
            else
            {
                if (!long.TryParse(majorStr, out var major) || major > k_MaxVersion ||
                    !long.TryParse(minorStr, out var minor) || minor > k_MaxVersion ||
                    !long.TryParse(patchStr, out var patch) || patch > k_MaxVersion)
                    propertyErrorsAndWarnings.m_ErrorMessages.Add(string.Format(L10n.Tr("Each component of version '{0}' for dependency '{1}' must be an integer less than or equal to {2}."), version.stringValue, packageName, k_MaxVersion));
                else if (major > k_RecommendedMaxVersion || minor > k_RecommendedMaxVersion || patch > k_RecommendedMaxVersion)
                    propertyErrorsAndWarnings.m_WarningMessages.Add(string.Format(L10n.Tr("Consider to use an integer less than or equal to {0} for each component of version '{1}' for dependency '{2}'."), k_RecommendedMaxVersion, version.stringValue, packageName));
            }
        }

        internal static void ValidateVersion(string version, out PropertyErrorsAndWarnings propertyErrorsAndWarnings)
        {
            propertyErrorsAndWarnings = new PropertyErrorsAndWarnings();

            if (string.IsNullOrEmpty(version))
                propertyErrorsAndWarnings.m_ErrorMessages.Add(L10n.Tr("Version is a required property."));
            else if (!PackageValidator.ValidateVersion(version, out var majorStr, out var minorStr, out var patchStr))
                propertyErrorsAndWarnings.m_ErrorMessages.Add(string.Format(L10n.Tr("Invalid version '{0}'."), version));
            else
            {
                if (!long.TryParse(majorStr, out var major) || major > k_MaxVersion ||
                    !long.TryParse(minorStr, out var minor) || minor > k_MaxVersion ||
                    !long.TryParse(patchStr, out var patch) || patch > k_MaxVersion)
                    propertyErrorsAndWarnings.m_ErrorMessages.Add(string.Format(L10n.Tr("Each component of version '{0}' must be an integer less than or equal to {1}."), version, k_MaxVersion));
                else if (major > k_RecommendedMaxVersion || minor > k_RecommendedMaxVersion || patch > k_RecommendedMaxVersion)
                    propertyErrorsAndWarnings.m_WarningMessages.Add(string.Format(L10n.Tr("Consider using an integer less than or equal to {0} for each component of version '{1}'."), k_RecommendedMaxVersion, version));
            }
        }

        private void ValidateVersionProperty(SerializedProperty version)
        {
            ValidateVersion(version.stringValue, out var propertyErrorsAndWarnings);
            packageState.propertiesErrorsAndWarnings[version.propertyPath] = propertyErrorsAndWarnings;
        }

        private void ValidateUnityVersionEnabled(SerializedProperty unityVersionEnabled)
        {
            var propertyErrorsAndWarnings = new PropertyErrorsAndWarnings();
            if (!unityVersionEnabled.boolValue)
                propertyErrorsAndWarnings.m_WarningMessages.Add(L10n.Tr("The recommended best practice is to include a Minimum Unity version."));
            packageState.propertiesErrorsAndWarnings[unityVersionEnabled.propertyPath] = propertyErrorsAndWarnings;
        }

        internal void ValidateUnityVersion(SerializedProperty unityMajor, SerializedProperty unityMinor, SerializedProperty unityRelease)
        {
            var propertyErrorsAndWarnings = new PropertyErrorsAndWarnings();
            if (string.IsNullOrEmpty(unityMajor.stringValue) && string.IsNullOrEmpty(unityMinor.stringValue) && string.IsNullOrEmpty(unityRelease.stringValue))
                propertyErrorsAndWarnings.m_ErrorMessages.Add(L10n.Tr("Version is a required property when the Minimum Unity version is checked."));
            else if (!PackageValidator.ValidateUnityVersion(unityMajor.stringValue, unityMinor.stringValue, unityRelease.stringValue))
            {
                var unityVersion = $"{unityMajor.stringValue}.{unityMinor.stringValue}";
                if (!string.IsNullOrWhiteSpace(unityRelease.stringValue))
                    unityVersion += "." + unityRelease.stringValue.Trim();

                propertyErrorsAndWarnings.m_ErrorMessages.Add(string.Format(L10n.Tr("Invalid Unity Version '{0}'."), unityVersion));
            }
            packageState.propertiesErrorsAndWarnings[unityMajor.propertyPath] = propertyErrorsAndWarnings;
        }

        internal void ValidateDependenciesList(SerializedProperty dependencies, int countDependencies)
        {
            var propertyErrorsAndWarnings = new PropertyErrorsAndWarnings();
            var distinctDependencies = new HashSet<string>();
            var currIndex = 0;
            while (currIndex < countDependencies)
            {
                var dependency = dependencies.GetArrayElementAtIndex(currIndex);
                var packageName = dependency.FindPropertyRelative(k_ManifestFieldPackageName);
                var version = dependency.FindPropertyRelative(k_ManifestFieldVersion);
                if (!string.IsNullOrEmpty(packageName.stringValue))
                {
                    if (distinctDependencies.Contains(packageName.stringValue))
                        propertyErrorsAndWarnings.m_ErrorMessages.Add(string.Format(L10n.Tr("Dependency {0} is already in the list."), packageName.stringValue));
                    else
                        distinctDependencies.Add(packageName.stringValue);
                    if (!PackageValidator.ValidateCompleteTechnicalName(packageName.stringValue))
                        propertyErrorsAndWarnings.m_ErrorMessages.Add(string.Format(L10n.Tr("Invalid Technical Name '{0}'."), packageName.stringValue));
                    ValidateDependencyVersion(propertyErrorsAndWarnings, version, packageName.stringValue);
                }
                currIndex++;
            }
            packageState.propertiesErrorsAndWarnings[dependencies.propertyPath] = propertyErrorsAndWarnings;
        }

        internal void ValidateTechnicalName(SerializedProperty technicalName, PackageInfo packageInfo, string assetPath)
        {
            var propertyErrorsAndWarnings = new PropertyErrorsAndWarnings();
            if (string.IsNullOrEmpty(technicalName.stringValue))
                propertyErrorsAndWarnings.m_ErrorMessages.Add(L10n.Tr("Technical Name is a required property."));
            else if (!PackageValidator.ValidateCompleteTechnicalName(technicalName.stringValue))
                propertyErrorsAndWarnings.m_ErrorMessages.Add(string.Format(L10n.Tr("Invalid Technical Name '{0}'."), technicalName.stringValue));
            if (packageInfo != null && m_IOProxy.GetParentDirectory(m_IOProxy.PathsCombine(packageInfo.assetPath, "package.json")) != m_IOProxy.GetParentDirectory(assetPath))
                propertyErrorsAndWarnings.m_ErrorMessages.Add(string.Format(L10n.Tr("Technical Name '{0}' is already used in this project."), technicalName.stringValue));
            packageState.propertiesErrorsAndWarnings[technicalName.propertyPath] = propertyErrorsAndWarnings;
        }

        internal void ValidateDisplayName(SerializedProperty displayName)
        {
            var propertyErrorsAndWarnings = new PropertyErrorsAndWarnings();
            if (string.IsNullOrWhiteSpace(displayName.stringValue) || displayName.stringValue.Trim().Length == 0)
                propertyErrorsAndWarnings.m_WarningMessages.Add(L10n.Tr("The recommended best practice is to include a Display Name."));
            packageState.propertiesErrorsAndWarnings[displayName.propertyPath] = propertyErrorsAndWarnings;
        }

        internal void ValidateDescription(SerializedProperty description)
        {
            var propertyErrorsAndWarnings = new PropertyErrorsAndWarnings();
            if (string.IsNullOrWhiteSpace(description.stringValue) || description.stringValue.Trim().Length == 0)
                propertyErrorsAndWarnings.m_WarningMessages.Add(L10n.Tr("The recommended best practice is to include a package description."));
            packageState.propertiesErrorsAndWarnings[description.propertyPath] = propertyErrorsAndWarnings;
        }

        internal void ValidateAuthorName(SerializedProperty authorName)
        {
            var propertyErrorsAndWarnings = new PropertyErrorsAndWarnings();
            if (string.IsNullOrWhiteSpace(authorName.stringValue) || authorName.stringValue.Trim().Length == 0)
                propertyErrorsAndWarnings.m_WarningMessages.Add(L10n.Tr("Package author name should be provided when author field is checked."));
            packageState.propertiesErrorsAndWarnings[authorName.propertyPath] = propertyErrorsAndWarnings;
        }

        internal void ValidatePackageVisibility(SerializedProperty visibility)
        {
            var propertyErrorsAndWarnings = new PropertyErrorsAndWarnings();
            var packageVisibility = (PackageVisibility)visibility.intValue;
            if (packageVisibility == PackageVisibility.AlwaysHidden)
                propertyErrorsAndWarnings.m_WarningMessages.Add(L10n.Tr("This package and all its assets will be hidden by default in the Editor because its visibility is set to 'Always Hidden'."));
            if (packageVisibility == PackageVisibility.AlwaysVisible)
                propertyErrorsAndWarnings.m_WarningMessages.Add(L10n.Tr("This package and all its assets will be visible by default in the Editor because its visibility is set to 'Always Visible'."));
            packageState.propertiesErrorsAndWarnings[visibility.propertyPath] = propertyErrorsAndWarnings;
        }

        internal void ValidateUrl(SerializedProperty url)
        {
            var propertyErrorsAndWarnings = new PropertyErrorsAndWarnings();
            if (!string.IsNullOrWhiteSpace(url.stringValue))
            {
                if (!(Uri.TryCreate(url.stringValue, UriKind.Absolute, out var uriResult) &&
                    (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)))
                    propertyErrorsAndWarnings.m_ErrorMessages.Add(string.Format(L10n.Tr("This URL is malformed or invalid '{0}'."), url.stringValue));
            }
            packageState.propertiesErrorsAndWarnings[url.propertyPath] = propertyErrorsAndWarnings;
        }

        internal void ValidateEmail(SerializedProperty email)
        {
            var propertyErrorsAndWarnings = new PropertyErrorsAndWarnings();
            if (!string.IsNullOrWhiteSpace(email.stringValue))
            {
                if (!Regex.IsMatch(email.stringValue, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
                    propertyErrorsAndWarnings.m_ErrorMessages.Add(string.Format(L10n.Tr("This email format is malformed or invalid '{0}'."), email.stringValue));
            }
            packageState.propertiesErrorsAndWarnings[email.propertyPath] = propertyErrorsAndWarnings;
        }
    }
}
