// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.PackageManager.UI
{
    [CustomEditor(typeof(PackageManifestImporter))]
    [CanEditMultipleObjects]
    internal class PackageManifestImporterEditor : AssetImporterEditor
    {
        enum PackageVisibility
        {
            DependsOnType,
            AlwaysHidden,
            AlwaysVisible
        }

        private static readonly string s_LocalizedTitle = L10n.Tr("Package '{0}' Manifest");
        private static readonly string s_LocalizedMultipleTitle = L10n.Tr("{0} Package Manifests");
        private static readonly string s_LocalizedInvalidPackageManifest = L10n.Tr("Invalid Package Manifest");
        private static readonly string s_LocalizedPackageManagerUINotInstalledWarning = L10n.Tr("Package Manager UI package is required to see selected packaged detail.");

        private const float kMinHeightDescriptionScrollView = 96f;
        private const int kMinimalUnityMajorVersionSupported = 2017;

        private static List<string> s_MajorUnityVersions;
        private static List<string> MajorUnityVersions
        {
            get
            {
                if (s_MajorUnityVersions != null)
                    return s_MajorUnityVersions;

                var version = InternalEditorUtility.GetUnityVersion();
                s_MajorUnityVersions = new List<string>();
                for (var majorVersion = kMinimalUnityMajorVersionSupported; majorVersion <= version.Major; majorVersion++)
                    s_MajorUnityVersions.Add(majorVersion.ToString());

                return s_MajorUnityVersions;
            }
        }

        private static readonly List<string> MinorUnityVersions = new List<string> { "1", "2", "3", "4" };

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
            public bool isEnable;
            public string major;
            public string minor;
            public string release;
        }

        [Serializable]
        class PackageName
        {
            public string completeName;
            public string name;
            public string organizationName;
            public string domain;
        }

        [Serializable]
        class PackageInformation
        {
            public PackageName packageName = new PackageName();
            public string displayName;
            public string version;
            public string description;
            public string type;
            public AdvancedSettings settings = new AdvancedSettings();
            public PackageUnityVersion unity = new PackageUnityVersion();
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
            public static readonly GUIContent name = EditorGUIUtility.TrTextContent("Name", "Package name. Must be lowercase");
            public static readonly GUIContent organizationName = EditorGUIUtility.TrTextContent("Organization name", "Package organization name. Must be lowercase and not include dots '.'");
            public static readonly GUIContent displayName = EditorGUIUtility.TrTextContent("Display name", "Display name used in UI.");
            public static readonly GUIContent version = EditorGUIUtility.TrTextContent("Version", "Package Version, much follow SemVer (ex: 1.0.0-preview.1).");
            public static readonly GUIContent type = EditorGUIUtility.TrTextContent("Type", "Package Type (optional).");

            public static readonly GUIContent showAdvanced = EditorGUIUtility.TrTextContent("Advanced", "Show advanced settings.");

            public static readonly GUIContent visibility = EditorGUIUtility.TrTextContent("Visibility in Editor", "Package visibility in Editor.");

            public static readonly GUIContent unity = EditorGUIUtility.TrTextContent("Minimal Unity Version");
            public static readonly GUIContent unityMajor = EditorGUIUtility.TrTextContent("Major", "Major version of Unity");
            public static readonly GUIContent unityMinor = EditorGUIUtility.TrTextContent("Minor", "Minor version of Unity");
            public static readonly GUIContent unityRelease = EditorGUIUtility.TrTextContent("Release", "Specific release (ex: 0a9)");

            public static readonly GUIContent description = EditorGUIUtility.TrTextContent("Brief Description");

            public static readonly GUIContent dependencies = EditorGUIUtility.TrTextContent("Dependencies");
            public static readonly GUIContent package = EditorGUIUtility.TrTextContent("Package name", "Package name. Must be lowercase");

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
        private SerializedProperty m_Name;
        private SerializedProperty m_OrganizationName;
        private SerializedProperty m_DisplayName;
        private SerializedProperty m_Version;
        private SerializedProperty m_UnityVersionEnabled;
        private SerializedProperty m_UnityMajor;
        private SerializedProperty m_UnityMinor;
        private SerializedProperty m_UnityRelease;
        private SerializedProperty m_Description;
        private SerializedProperty m_Type;
        private SerializedProperty m_Advanced;
        private SerializedProperty m_Visibility;

        internal override string targetTitle
        {
            get
            {
                if (targets.Length > 1)
                {
                    return string.Format(s_LocalizedMultipleTitle, targets.Length);
                }
                return string.Format(s_LocalizedTitle, packageState != null && packageState.isValidFile ?
                    !IsNullOrEmptyTrim(packageState.info.displayName) ? packageState.info.displayName.Trim() : packageState.info.packageName.completeName :
                    s_LocalizedInvalidPackageManifest);
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
                PackageManagerWindow.SelectPackageAndFilterStatic(packageState.info.packageName.completeName);
            }
            GUI.enabled = previousEnabled;
        }

        public override void OnEnable()
        {
            base.OnEnable();

            errorMessages = new List<string>();
            warningMessages = new List<string>();

            m_IsValidFile = extraDataSerializedObject.FindProperty("isValidFile");
            m_Name = extraDataSerializedObject.FindProperty("info.packageName.name");
            m_OrganizationName = extraDataSerializedObject.FindProperty("info.packageName.organizationName");
            m_DisplayName = extraDataSerializedObject.FindProperty("info.displayName");
            m_Version = extraDataSerializedObject.FindProperty("info.version");
            m_UnityVersionEnabled = extraDataSerializedObject.FindProperty("info.unity.isEnable");
            m_UnityMajor = extraDataSerializedObject.FindProperty("info.unity.major");
            m_UnityMinor = extraDataSerializedObject.FindProperty("info.unity.minor");
            m_UnityRelease = extraDataSerializedObject.FindProperty("info.unity.release");
            m_Description = extraDataSerializedObject.FindProperty("info.description");
            m_Type = extraDataSerializedObject.FindProperty("info.type");
            m_Advanced = extraDataSerializedObject.FindProperty("info.settings");
            m_Visibility = extraDataSerializedObject.FindProperty("info.settings.visibility");

            m_DependenciesList = new ReorderableList(extraDataSerializedObject,
                extraDataSerializedObject.FindProperty("dependencies"), true, false, true, true)
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

        private static void DrawDependencyHeaderElement(Rect rect)
        {
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
            var packageName = dependency.FindPropertyRelative("packageName");
            var version = dependency.FindPropertyRelative("version");

            var w = rect.width;
            rect.x += 4;
            rect.width = w / 3 * 2 - 2;
            rect.height -= EditorGUIUtility.standardVerticalSpacing;
            packageName.stringValue = EditorGUI.DelayedTextField(rect, packageName.stringValue);

            if (!IsNullOrEmptyTrim(packageName.stringValue) && !PackageValidation.ValidateName(packageName.stringValue))
                errorMessages.Add($"Invalid Dependency Package Name '{packageName.stringValue}'");

            using (new EditorGUI.DisabledScope(IsNullOrEmptyTrim(packageName.stringValue)))
            {
                rect.x += w / 3 * 2;
                rect.width = w / 3 - 4;
                version.stringValue = EditorGUI.DelayedTextField(rect, version.stringValue);

                if (!IsNullOrEmptyTrim(version.stringValue) && !PackageValidation.ValidateVersion(version.stringValue))
                    errorMessages.Add(
                        $"Invalid Dependency Version '{version.stringValue}' for '{packageName.stringValue}'");
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
                EditorGUILayout.DelayedTextField(m_Name, Styles.name);
                m_OrganizationName.stringValue = EditorGUILayout.DelayedTextFieldDropDown(Styles.organizationName, m_OrganizationName.stringValue.ToLower(), Connect.UnityConnect.instance.userInfo.organizationNames);
                EditorGUILayout.DelayedTextField(m_DisplayName, Styles.displayName);
                EditorGUILayout.DelayedTextField(m_Version, Styles.version);
                m_Type.stringValue = EditorGUILayout.DelayedTextFieldDropDown(Styles.type, m_Type.stringValue, PackageInfo.GetPredefinedPackageTypes());

                EditorGUILayout.PropertyField(m_UnityVersionEnabled, Styles.unity);
                if (m_UnityVersionEnabled.boolValue)
                {
                    EditorGUI.showMixedValue = m_UnityMajor.hasMultipleDifferentValues;
                    m_UnityMajor.stringValue = EditorGUILayout.DelayedTextFieldDropDown(Styles.unityMajor,
                        m_UnityMajor.stringValue, MajorUnityVersions.ToArray());

                    EditorGUI.showMixedValue = m_UnityMinor.hasMultipleDifferentValues;
                    m_UnityMinor.stringValue = EditorGUILayout.DelayedTextFieldDropDown(Styles.unityMinor,
                        m_UnityMinor.stringValue, MinorUnityVersions.ToArray());

                    EditorGUI.showMixedValue = m_UnityRelease.hasMultipleDifferentValues;
                    m_UnityRelease.stringValue =
                        EditorGUILayout.DelayedTextField(Styles.unityRelease, m_UnityRelease.stringValue);
                    EditorGUI.showMixedValue = false;
                }
            }
        }

        private void DoPackageDescriptionLayout()
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true)))
            {
                using (var scrollView = new EditorGUILayout.VerticalScrollViewScope(descriptionScrollViewPosition,
                    GUILayout.MinHeight(kMinHeightDescriptionScrollView)))
                {
                    descriptionScrollViewPosition = scrollView.scrollPosition;
                    m_Description.stringValue = EditorGUILayout.TextArea(m_Description.stringValue ?? "" ,
                        GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                }
            }
        }

        private void PerformValidation()
        {
            if (!PackageValidation.ValidateOrganizationName(m_OrganizationName.stringValue) && !IsNullOrEmptyTrim(m_OrganizationName.stringValue))
                errorMessages.Add($"Invalid Package Organization Name '{m_OrganizationName.stringValue}'");

            if (!PackageValidation.ValidateName(m_Name.stringValue))
                errorMessages.Add($"Invalid Package Name '{m_Name.stringValue}'");

            if (!PackageValidation.ValidateVersion(m_Version.stringValue))
                errorMessages.Add($"Invalid Version '{m_Version.stringValue}'");

            if (m_UnityVersionEnabled.boolValue)
            {
                if (!PackageValidation.ValidateUnityVersion(m_UnityMajor.stringValue, m_UnityMinor.stringValue,
                    m_UnityRelease.stringValue))
                {
                    var unityVersion = string.Join(".", new[] {m_UnityMajor.stringValue, m_UnityMinor.stringValue});
                    if (!IsNullOrEmptyTrim(m_UnityRelease.stringValue))
                        unityVersion += "." + m_UnityRelease.stringValue.Trim();

                    errorMessages.Add($"Invalid Unity Version '{unityVersion}'");
                }
            }

            if (IsNullOrEmptyTrim(m_DisplayName.stringValue) || m_DisplayName.stringValue.Trim().Length == 0)
            {
                warningMessages.Add("Display name should be provided.");
            }

            if (IsNullOrEmptyTrim(m_Description.stringValue) || m_Description.stringValue.Trim().Length == 0)
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
            else
            {
                if (IsNullOrEmptyTrim(packageState.info.type))
                    warningMessages.Add("This package and all its assets will be hidden by default in Editor because its type is empty");
                else if (PackageInfo.GetPredefinedHiddenByDefaultPackageTypes().Contains(packageState.info.type))
                    warningMessages.Add($"This package and all its assets will be hidden by default in Editor because its type is '{packageState.info.type}'");
                else
                    warningMessages.Add($"This package and all its assets will be visible by default in Editor because its type is '{packageState.info.type}'");
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
            GUILayout.Label(Styles.dependencies, EditorStyles.boldLabel);
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

            if (errorMessages.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(string.Join("\n", errorMessages.ToArray()), MessageType.Error);
            }

            if (warningMessages.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(string.Join("\n", warningMessages.ToArray()), MessageType.Warning);
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
                var jsonString = File.ReadAllText(assetPath);
                var info = Json.Deserialize(jsonString) as Dictionary<string, object>;

                packageState.dependencies = new List<PackageDependency>();
                packageState.info = new PackageInformation();
                packageState.isValidFile = info != null &&
                    info.ContainsKey("name") && info["name"] is string &&
                    info.ContainsKey("version") && info["version"] is string;


                if (packageState.isValidFile)
                {
                    if (info.ContainsKey("displayName") && info["displayName"] is string)
                        packageState.info.displayName = (string)info["displayName"];

                    packageState.info.packageName.completeName = info["name"] as string;

                    var packageNameSplit = packageState.info.packageName.completeName.Split('.');
                    var packageNameSplitCount = packageNameSplit.Count();

                    if (packageNameSplitCount > 2)
                    {
                        packageState.info.packageName.domain = packageNameSplit[0];
                        packageState.info.packageName.organizationName = packageNameSplit[1];
                        string domainAndOrganizationName = packageState.info.packageName.domain + "." + packageState.info.packageName.organizationName + ".";
                        packageState.name = packageState.info.packageName.completeName.Replace(domainAndOrganizationName, "");
                        packageState.info.packageName.name = packageState.name;
                    }
                    else if (packageNameSplitCount == 2)
                    {
                        packageState.info.packageName.organizationName = packageNameSplit[0];
                        packageState.info.packageName.name = packageNameSplit[1];
                    }
                    else
                        packageState.info.packageName.name = packageState.info.packageName.completeName;


                    packageState.info.version = info["version"] as string;

                    if (info.ContainsKey("description") && info["description"] is string)
                        packageState.info.description = (string)info["description"];

                    if (info.ContainsKey("type") && info["type"] is string)
                        packageState.info.type = (string)info["type"];

                    if (info.ContainsKey("hideInEditor") && info["hideInEditor"] is bool)
                        packageState.info.settings.visibility = (bool)info["hideInEditor"] ? PackageVisibility.AlwaysHidden : PackageVisibility.AlwaysVisible;
                    else
                        packageState.info.settings.visibility = PackageVisibility.DependsOnType;

                    if (info.ContainsKey("unity") && info["unity"] is string)
                    {
                        var splitVersions = ((string)info["unity"]).Split('.');
                        packageState.info.unity = new PackageUnityVersion
                        {
                            isEnable = true, major = splitVersions[0], minor = splitVersions.Length > 1 ? splitVersions[1] : "", release = ""
                        };

                        if (info.ContainsKey("unityRelease") && info["unityRelease"] is string)
                            packageState.info.unity.release = (string)info["unityRelease"];
                    }
                    else
                    {
                        var unityVersion = InternalEditorUtility.GetUnityVersion();
                        packageState.info.unity = new PackageUnityVersion
                        {
                            isEnable = false, major = unityVersion.Major.ToString(), minor = unityVersion.Minor.ToString(), release = ""
                        };
                    }

                    if (info.ContainsKey("dependencies"))
                    {
                        if (!(info["dependencies"] is IDictionary))
                        {
                            packageState.isValidFile = false;
                            return;
                        }
                        var dependenciesDictionary = (IDictionary)info["dependencies"];
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
                }
            }
            catch (IOException)
            {
                Debug.Log($"Couldn't open package manifest file {assetPath}.");
                packageState.isValidFile = false;
            }
        }

        private static void WritePackageManifest(Object target, PackageManifestState packageState)
        {
            var importer = target as PackageManifestImporter;
            if (importer == null)
                return;

            var assetPath = importer.assetPath;
            Dictionary<string, object> json = null;

            try
            {
                var jsonString = File.ReadAllText(assetPath);
                json = Json.Deserialize(jsonString) as Dictionary<string, object>;
            }
            catch (IOException)
            {
                Debug.Log($"Couldn't open package manifest file {assetPath}.");
            }

            if (json == null)
                return;

            var domainTrimmed = packageState.info.packageName.domain.Trim();
            var organizationNameTrimmed = packageState.info.packageName.organizationName.Trim();
            var nameTrimmed = packageState.info.packageName.name.Trim();

            if (string.IsNullOrEmpty(domainTrimmed) && string.IsNullOrEmpty(organizationNameTrimmed))
            {
                json["name"] = nameTrimmed;
            }
            else if (string.IsNullOrEmpty(domainTrimmed) && !string.IsNullOrEmpty(organizationNameTrimmed))
            {
                json["name"] = string.Join(".",
                    new[] { organizationNameTrimmed,
                            nameTrimmed });
            }
            else if (!string.IsNullOrEmpty(domainTrimmed) && !string.IsNullOrEmpty(organizationNameTrimmed))
            {
                json["name"] = string.Join(".",
                    new[] { domainTrimmed,
                            organizationNameTrimmed,
                            nameTrimmed });
            }

            if (!IsNullOrEmptyTrim(packageState.info.displayName))
                json["displayName"] = packageState.info.displayName.Trim();
            else
                json.Remove("displayName");

            json["version"] = packageState.info.version;

            if (!IsNullOrEmptyTrim(packageState.info.description))
                json["description"] = packageState.info.description.Trim();
            else
                json.Remove("description");

            if (!IsNullOrEmptyTrim(packageState.info.type))
                json["type"] = packageState.info.type.Trim();
            else
                json.Remove("type");

            if (packageState.info.settings.visibility == PackageVisibility.DependsOnType)
                json.Remove("hideInEditor");
            else
                json["hideInEditor"] = packageState.info.settings.visibility == PackageVisibility.AlwaysHidden;

            if (packageState.info.unity.isEnable)
            {
                if (!IsNullOrEmptyTrim(packageState.info.unity.major) &&
                    !IsNullOrEmptyTrim(packageState.info.unity.minor))
                {
                    json["unity"] = string.Join(".",
                        new[] {packageState.info.unity.major.Trim(), packageState.info.unity.minor.Trim()});

                    if (!IsNullOrEmptyTrim(packageState.info.unity.release))
                        json["unityRelease"] = packageState.info.unity.release.Trim();
                    else
                        json.Remove("unityRelease");
                }
            }
            else
            {
                json.Remove("unity");
                json.Remove("unityRelease");
            }

            if (packageState.dependencies.Count > 0)
            {
                var dependencies = new Dictionary<string, string>();
                foreach (var dependency in packageState.dependencies)
                {
                    if (!IsNullOrEmptyTrim(dependency.packageName))
                        dependencies.Add(dependency.packageName.Trim(), dependency.version);
                }

                json["dependencies"] = dependencies;
            }
            else
                json.Remove("dependencies");

            try
            {
                File.WriteAllText(assetPath, Json.Serialize(json, true));
                Client.Resolve();
            }
            catch (IOException)
            {
                Debug.Log($"Couldn't write package manifest file {assetPath}.");
            }
        }

        private static bool IsNullOrEmptyTrim(string str)
        {
            return str == null || str.Trim().Length == 0;
        }
    }
}
