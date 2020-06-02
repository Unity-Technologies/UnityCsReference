// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [CustomEditor(typeof(PackageSelectionObject))]
    internal sealed class PackageEditor : Editor
    {
        private const float kMinHeightForAssetStore = 192f;
        private const float kMinHeightForOther = 96f;
        private const float kLabelMinWidth = 64f;

        internal override string targetTitle
        {
            get
            {
                if (packageSelectionObject == null)
                    return base.targetTitle;

                return string.Format(L10n.Tr("Package '{0}' Manifest"), m_Version != null ?
                    string.IsNullOrEmpty(m_Version.displayName) ? m_Version.name : m_Version.displayName :
                    packageSelectionObject.displayName);
            }
        }

        private static class Styles
        {
            public static readonly GUIContent information = EditorGUIUtility.TrTextContent("Information");
            public static readonly GUIContent name = EditorGUIUtility.TrTextContent("Name", "Package name");
            public static readonly GUIContent displayName = EditorGUIUtility.TrTextContent("Display name", "Display name used in UI");
            public static readonly GUIContent version = EditorGUIUtility.TrTextContent("Version", "Package Version");
            public static readonly GUIContent category = EditorGUIUtility.TrTextContent("Category", "Package Category");
            public static readonly GUIContent description = EditorGUIUtility.TrTextContent("Description");
            public static readonly GUIContent dependencies = EditorGUIUtility.TrTextContent("Dependencies");
            public static readonly GUIContent package = EditorGUIUtility.TrTextContent("Package name", "Dependency package name");
            public static readonly GUIContent editPackage = EditorGUIUtility.TrTextContent("Edit");
            public static readonly GUIContent viewInPackageManager = EditorGUIUtility.TrTextContent("View in Package Manager");
        }

        private PackageSelectionObject packageSelectionObject => target as PackageSelectionObject;

        [HideInInspector]
        [SerializeField]
        private Vector2 m_ScrollPosition;

        [HideInInspector]
        [SerializeField]
        private ReorderableList m_List;

        [NonSerialized]
        private IPackage m_Package;

        [NonSerialized]
        private bool m_ShouldBeEnabled;

        [NonSerialized]
        private IPackageVersion m_Version;

        private SelectionProxy m_Selection;
        private AssetDatabaseProxy m_AssetDatabase;
        private PackageDatabase m_PackageDatabase;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_Selection = container.Resolve<SelectionProxy>();
            m_AssetDatabase = container.Resolve<AssetDatabaseProxy>();
            m_PackageDatabase = container.Resolve<PackageDatabase>();
        }

        void OnEnable()
        {
            ResolveDependencies();

            m_PackageDatabase.onPackagesChanged += OnPackagesChanged;
        }

        void OnDisable()
        {
            m_PackageDatabase.onPackagesChanged -= OnPackagesChanged;
        }

        private void OnPackagesChanged(IEnumerable<IPackage> added, IEnumerable<IPackage> removed, IEnumerable<IPackage> preUpdated, IEnumerable<IPackage> postUpdate)
        {
            var selectedPackageUniqueId = packageSelectionObject?.packageUniqueId;
            if (string.IsNullOrEmpty(selectedPackageUniqueId))
                return;
            if (added.Concat(removed).Concat(preUpdated).Any(p => p.uniqueId == selectedPackageUniqueId))
                m_PackageDatabase.GetPackageAndVersion(packageSelectionObject.packageUniqueId, packageSelectionObject.versionUniqueId, out m_Package, out m_Version);
        }

        public override void OnInspectorGUI()
        {
            if (packageSelectionObject == null)
            {
                EditorGUILayout.HelpBox(L10n.Tr("This package is not accessible anymore."), MessageType.Error);
                return;
            }

            if (m_Package == null || m_Version == null)
            {
                m_PackageDatabase.GetPackageAndVersion(packageSelectionObject.packageUniqueId, packageSelectionObject.versionUniqueId, out m_Package, out m_Version);
                if (m_Package == null || m_Version == null)
                {
                    EditorGUILayout.HelpBox(L10n.Tr("This package is not accessible anymore."), MessageType.Error);
                    return;
                }

                var immutable = true;
                m_ShouldBeEnabled = true;
                if (!m_Version.isInstalled || m_AssetDatabase.GetAssetFolderInfo("Packages/" + m_Package.name, out var rootFolder, out immutable))
                    m_ShouldBeEnabled = !immutable;
            }

            var dependencies = new List<DependencyInfo>();
            if (m_Version.dependencies != null)
                dependencies.AddRange(m_Version.dependencies);
            m_List = new ReorderableList(dependencies, typeof(DependencyInfo), false, true, false, false)
            {
                drawElementCallback = DrawDependencyListElement,
                drawHeaderCallback = DrawDependencyHeaderElement,
                elementHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing
            };

            var previousEnabled = GUI.enabled;
            GUI.enabled = m_ShouldBeEnabled;

            // Package information
            GUILayout.Label(Styles.information, EditorStyles.boldLabel);
            DoPackageInformationLayout();

            // Package description
            GUILayout.Label(Styles.description, EditorStyles.boldLabel);
            DoPackageDescriptionLayout();

            // Package dependencies
            GUILayout.Label(Styles.dependencies, EditorStyles.boldLabel);
            m_List.DoLayoutList();

            GUI.enabled = previousEnabled;
        }

        internal override void OnHeaderControlsGUI()
        {
            base.OnHeaderControlsGUI();

            var previousEnabled = GUI.enabled;
            GUI.enabled =  targets.Length == 1 && m_Package?.state == PackageState.InDevelopment && (m_Version?.isInstalled ?? false);
            if (GUILayout.Button(Styles.editPackage, EditorStyles.miniButton))
            {
                var path = m_Version.packageInfo.assetPath;
                var manifest = m_AssetDatabase.LoadAssetAtPath<PackageManifest>($"{path}/package.json");
                if (manifest != null)
                    m_Selection.activeObject = manifest;
            }
            GUI.enabled = targets.Length == 1 && m_Package != null && m_Version != null;
            if (GUILayout.Button(Styles.viewInPackageManager, EditorStyles.miniButton))
            {
                PackageManagerWindow.SelectPackageAndFilterStatic(m_Package.Is(PackageType.AssetStore) ? m_Version.packageUniqueId : m_Version.uniqueId);
            }
            GUI.enabled = previousEnabled;
        }

        internal override void OnForceReloadInspector()
        {
            base.OnForceReloadInspector();

            var packageDatabase = ServicesContainer.instance.Resolve<PackageDatabase>();
            if (packageSelectionObject != null && (m_Package == null || m_Version == null))
                packageDatabase.GetPackageAndVersion(packageSelectionObject.packageUniqueId, packageSelectionObject.versionUniqueId, out m_Package, out m_Version);
        }

        internal override bool HasLargeHeader()
        {
            return true;
        }

        internal override Rect DrawHeaderHelpAndSettingsGUI(Rect r)
        {
            return new Rect(r.width, 0, 0, 0);
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
            var list = m_List.list as IList<DependencyInfo>;
            var dependency = list[index];
            var packageName = dependency.name;
            var version = dependency.version;

            var w = rect.width;
            rect.x += 4;
            rect.width = w / 3 * 2 - 2;
            rect.height -= EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.SelectableLabel(rect, packageName);

            rect.x += w / 3 * 2;
            rect.width = w / 3 - 4;
            EditorGUI.SelectableLabel(rect, version);
        }

        private void DoPackageInformationLayout()
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true)))
            {
                var labels = new List<GUIContent>();
                if (!m_Package.Is(PackageType.AssetStore))
                    labels.Add(Styles.name);
                labels.AddRange(new[]
                {
                    Styles.displayName, Styles.version, Styles.category
                });

                var contents = new List<string>();
                if (!m_Package.Is(PackageType.AssetStore))
                    contents.Add(m_Version.name);
                contents.AddRange(new[]
                {
                    m_Version.displayName, m_Version.version.ToString(), m_Version.category
                });

                SelectableLabelFields(labels, contents);
            }
        }

        private void DoPackageDescriptionLabel()
        {
            var descriptionStyle = EditorStyles.textArea;
            var descriptionRect = GUILayoutUtility.GetRect(EditorGUIUtility.TempContent(m_Version.description), descriptionStyle, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            EditorGUI.SelectableLabel(descriptionRect, m_Version.description, descriptionStyle);
        }

        private void DoPackageDescriptionLayout()
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true)))
            {
                using (var scrollView = new EditorGUILayout.VerticalScrollViewScope(m_ScrollPosition,
                    GUILayout.MinHeight(m_Package.Is(PackageType.AssetStore) ? kMinHeightForAssetStore : kMinHeightForOther)))
                {
                    m_ScrollPosition = scrollView.scrollPosition;
                    DoPackageDescriptionLabel();
                }
            }
        }

        private static void SelectableLabelFields(IEnumerable<GUIContent> labels, IEnumerable<string> contents)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            foreach (var label in labels)
                GUILayout.Label(label, GUILayout.MinWidth(kLabelMinWidth), GUILayout.Height(EditorGUI.kSingleLineHeight));
            GUILayout.EndVertical();
            GUILayout.Space(EditorGUI.kSpacing);
            GUILayout.BeginVertical();
            foreach (var content in contents)
                EditorGUILayout.SelectableLabel(content, EditorStyles.textField, GUILayout.Height(EditorGUI.kSingleLineHeight));
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }
}
