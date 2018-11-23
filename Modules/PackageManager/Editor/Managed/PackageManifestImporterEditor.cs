// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.PackageManager
{
    [CustomEditor(typeof(PackageManifestImporter))]
    internal class PackageManifestImporterEditor : AssetImporterEditor
    {
        private static readonly string s_LocalizedTitle = L10n.Tr("Package '{0}' Manifest");
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
        private class PackageDependency
        {
            public string packageName;
            public string version;
        }

        [Serializable]
        private class PackageUnityVersion
        {
            public bool isEnable;
            public string major;
            public string minor;
            public string release;
        }

        [Serializable]
        private class PackageInformation
        {
            public string packageName;
            public string displayName;
            public string version;
            public string description;
            public PackageUnityVersion unity;
        }

        [Serializable]
        private class PackageManifestState : ScriptableObject
        {
            public bool isModified;
            public bool isValidFile;
            public PackageInformation info;
            public List<PackageDependency> dependencies;
        }

        private static class Styles
        {
            public static readonly GUIContent information = EditorGUIUtility.TrTextContent("Information");
            public static readonly GUIContent name = EditorGUIUtility.TrTextContent("Name", "Package name. Must be lowercase");
            public static readonly GUIContent displayName = EditorGUIUtility.TrTextContent("Display name", "Display name used in UI.");
            public static readonly GUIContent version = EditorGUIUtility.TrTextContent("Version", "Package Version, much follow SemVer (ex: 1.0.0-preview.1).");

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

        [SerializeField]
        private PackageManifestState packageState;

        [SerializeField]
        private SerializedObject packageSerializedObject;

        [HideInInspector]
        [SerializeField]
        private Vector2 descriptionScrollViewPosition;

        private List<string> errorMessages;
        private List<string> warningMessages;

        ReorderableList m_DependenciesList;

        private SerializedProperty m_IsModified;
        private SerializedProperty m_IsValidFile;
        private SerializedProperty m_Name;
        private SerializedProperty m_DisplayName;
        private SerializedProperty m_Version;
        private SerializedProperty m_UnityVersionEnabled;
        private SerializedProperty m_UnityMajor;
        private SerializedProperty m_UnityMinor;
        private SerializedProperty m_UnityRelease;
        private SerializedProperty m_Description;

        internal override string targetTitle
        {
            get
            {
                return string.Format(s_LocalizedTitle, packageState != null && packageState.isValidFile ?
                    !IsNullOrEmptyTrim(packageState.info.displayName) ? packageState.info.displayName : packageState.info.packageName :
                    s_LocalizedInvalidPackageManifest);
            }
        }

        internal override void OnHeaderControlsGUI()
        {
            base.OnHeaderControlsGUI();

            // We want to have this button enabled even for immutable package
            var previousEnabled = GUI.enabled;
            GUI.enabled = true;
            if (GUILayout.Button(Styles.viewInPackageManager, EditorStyles.miniButton))
            {
                if (!EditorApplication.ExecuteMenuItemWithTemporaryContext("Window/Package Manager", new Object[] { packageState }))
                    Debug.LogWarning(s_LocalizedPackageManagerUINotInstalledWarning);
            }
            GUI.enabled = previousEnabled;
        }

        public override void OnEnable()
        {
            if (targets == null)
                return;

            errorMessages = new List<string>();
            warningMessages = new List<string>();

            if (packageSerializedObject == null)
            {
                packageState = CreateInstance<PackageManifestState>();
                ReadPackageManifest(target, packageState);
                packageSerializedObject = new SerializedObject(packageState);
            }
            else
            {
                packageState = packageSerializedObject.targetObject as PackageManifestState;
            }

            m_IsValidFile = packageSerializedObject.FindProperty("isValidFile");
            m_IsModified = packageSerializedObject.FindProperty("isModified");
            m_Name = packageSerializedObject.FindProperty("info.packageName");
            m_DisplayName = packageSerializedObject.FindProperty("info.displayName");
            m_Version = packageSerializedObject.FindProperty("info.version");
            m_UnityVersionEnabled = packageSerializedObject.FindProperty("info.unity.isEnable");
            m_UnityMajor = packageSerializedObject.FindProperty("info.unity.major");
            m_UnityMinor = packageSerializedObject.FindProperty("info.unity.minor");
            m_UnityRelease = packageSerializedObject.FindProperty("info.unity.release");
            m_Description = packageSerializedObject.FindProperty("info.description");

            m_DependenciesList = new ReorderableList(packageSerializedObject,
                packageSerializedObject.FindProperty("dependencies"), false, false, true, true)
            {
                drawElementCallback = DrawDependencyListElement,
                drawHeaderCallback = DrawDependencyHeaderElement,
                elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing
            };
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

        public override bool HasModified()
        {
            return base.HasModified() || m_IsModified.boolValue;
        }

        protected override void Apply()
        {
            base.Apply();

            if (packageState == null)
                return;

            WritePackageManifest(target, packageState);

            GUI.FocusControl(null);
        }

        protected override void ResetValues()
        {
            base.ResetValues();

            if (packageState == null)
                return;

            ReadPackageManifest(target, packageState);
            packageSerializedObject.Update();

            GUI.FocusControl(null);
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
                EditorGUILayout.DelayedTextField(m_DisplayName, Styles.displayName);
                EditorGUILayout.DelayedTextField(m_Version, Styles.version);

                EditorGUILayout.PropertyField(m_UnityVersionEnabled, Styles.unity);
                if (m_UnityVersionEnabled.boolValue)
                {
                    m_UnityMajor.stringValue = EditorGUILayout.DelayedTextFieldDropDown(Styles.unityMajor,
                        m_UnityMajor.stringValue, MajorUnityVersions.ToArray());

                    m_UnityMinor.stringValue = EditorGUILayout.DelayedTextFieldDropDown(Styles.unityMinor,
                        m_UnityMinor.stringValue, MinorUnityVersions.ToArray());

                    m_UnityRelease.stringValue =
                        EditorGUILayout.DelayedTextField(Styles.unityRelease, m_UnityRelease.stringValue);
                }
            }
        }

        private void DoPackageDescriptionLayout()
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true)))
            {
                using (new EditorGUILayout.VerticalScrollViewScope(descriptionScrollViewPosition,
                    GUILayout.MinHeight(kMinHeightDescriptionScrollView)))
                {
                    m_Description.stringValue = EditorGUILayout.TextArea(m_Description.stringValue ?? "" ,
                        GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                }
            }
        }

        private void PerformValidation()
        {
            if (!PackageValidation.ValidateName(m_Name.stringValue))
                errorMessages.Add($"Invalid Package Name '{m_Name.stringValue}'");

            if (!PackageValidation.ValidateVersion(m_Version.stringValue))
                errorMessages.Add($"Invalid Version '{m_Version.stringValue}'");

            if (packageState.info.unity.isEnable)
            {
                if (!PackageValidation.ValidateUnityVersion(m_UnityMajor.stringValue, m_UnityMinor.stringValue,
                    m_UnityRelease.stringValue))
                {
                    var unityVersion = string.Join(".", new[] {m_UnityMajor.stringValue, m_UnityMinor.stringValue});
                    if (!IsNullOrEmptyTrim(m_UnityRelease.stringValue))
                        unityVersion += "." + m_UnityRelease.stringValue;

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
        }

        public override void OnInspectorGUI()
        {
            if (packageState == null)
                return;

            packageSerializedObject.Update();

            if (!m_IsValidFile.boolValue)
            {
                EditorGUILayout.HelpBox(s_LocalizedInvalidPackageManifest, MessageType.Error);
                return;
            }

            errorMessages.Clear();
            warningMessages.Clear();

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                // Package information
                GUILayout.Label(Styles.information, EditorStyles.boldLabel);
                DoPackageInformationLayout();

                // Package description
                GUILayout.Label(Styles.description, EditorStyles.boldLabel);
                DoPackageDescriptionLayout();

                // Package dependencies
                GUILayout.Label(Styles.dependencies, EditorStyles.boldLabel);
                m_DependenciesList.DoLayoutList();

                // Validation
                PerformValidation();

                if (change.changed)
                    m_IsModified.boolValue = true;
            }

            packageSerializedObject.ApplyModifiedProperties();

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
                    packageState.info.packageName = info["name"] as string;

                    if (info.ContainsKey("displayName") && info["displayName"] is string)
                        packageState.info.displayName = (string)info["displayName"];

                    packageState.info.version = info["version"] as string;

                    if (info.ContainsKey("description") && info["description"] is string)
                        packageState.info.description = (string)info["description"];

                    if (info.ContainsKey("unity") && info["unity"] is string)
                    {
                        var splitVersions = ((string)info["unity"]).Split('.');
                        packageState.info.unity = new PackageUnityVersion
                        {
                            isEnable = true, major = splitVersions[0], minor = splitVersions[1], release = ""
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

                    if (info.ContainsKey("dependencies") && info["dependencies"] is IDictionary)
                    {
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

                packageState.isModified = false;
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

            json["name"] = packageState.info.packageName;

            if (!IsNullOrEmptyTrim(packageState.info.displayName))
                json["displayName"] = packageState.info.displayName;
            else
                json.Remove("displayName");

            json["version"] = packageState.info.version;

            if (!IsNullOrEmptyTrim(packageState.info.description))
                json["description"] = packageState.info.description;
            else
                json.Remove("description");

            if (packageState.info.unity.isEnable)
            {
                if (!IsNullOrEmptyTrim(packageState.info.unity.major) &&
                    !IsNullOrEmptyTrim(packageState.info.unity.minor))
                {
                    json["unity"] = string.Join(".",
                        new[] {packageState.info.unity.major, packageState.info.unity.minor});

                    if (!IsNullOrEmptyTrim(packageState.info.unity.release))
                        json["unityRelease"] = packageState.info.unity.release;
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
                        dependencies.Add(dependency.packageName, dependency.version);
                }

                json["dependencies"] = dependencies;
            }
            else
                json.Remove("dependencies");

            try
            {
                File.WriteAllText(assetPath, Json.Serialize(json, true));
                packageState.isModified = false;
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
