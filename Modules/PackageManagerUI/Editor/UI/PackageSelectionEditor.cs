// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [CustomEditor(typeof(PackageSelectionObject)), CanEditMultipleObjects]
    internal sealed class PackageEditor : Editor
    {
        private const float kMinHeightForAssetStore = 192f;
        private const float kMinHeightForOther = 96f;
        private const float kLabelMinWidth = 64f;

        private static readonly string k_PackageNotAccessibleMessage = L10n.Tr("This package is not accessible anymore.");
        private static readonly string k_MultiPackagesSelectionMessage = L10n.Tr("Multi-object editing not supported.");
        internal override string targetTitle
        {
            get
            {
                if (packageSelectionObject == null)
                    return base.targetTitle;

                return string.Format(L10n.Tr("{0} '{1}' Manifest"), m_Version?.GetDescriptor(true), m_Version != null ?
                    string.IsNullOrEmpty(m_Version.displayName) ? m_Version.name : m_Version.displayName :
                    packageSelectionObject.displayName);
            }
        }

        private static class Styles
        {
            public static readonly GUIContent information = EditorGUIUtility.TrTextContent("Information");
            public static readonly GUIContent name = EditorGUIUtility.TrTextContent("Name");
            public static readonly GUIContent displayName = EditorGUIUtility.TrTextContent("Display name");
            public static readonly GUIContent version = EditorGUIUtility.TrTextContent("Version");
            public static readonly GUIContent category = EditorGUIUtility.TrTextContent("Category");
            public static readonly GUIContent description = EditorGUIUtility.TrTextContent("Description");
            public static readonly GUIContent package = EditorGUIUtility.TrTextContent("Package name");
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
        private IPackageVersion m_Version;

        private ISelectionProxy m_Selection;
        private IAssetDatabaseProxy m_AssetDatabase;
        private IUpmCache m_UpmCache;
        private IPackageDatabase m_PackageDatabase;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_Selection = container.Resolve<ISelectionProxy>();
            m_AssetDatabase = container.Resolve<IAssetDatabaseProxy>();
            m_UpmCache = container.Resolve<IUpmCache>();
            m_PackageDatabase = container.Resolve<IPackageDatabase>();
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

        private void GetPackageAndVersion(PackageSelectionObject packageSelectionObject)
        {
            m_Package = m_PackageDatabase.GetPackage(packageSelectionObject.packageUniqueId);
            m_Version = m_Package?.versions.primary;
        }

        private void OnPackagesChanged(PackagesChangeArgs args)
        {
            var selectedPackageUniqueId = packageSelectionObject?.packageUniqueId;
            if (string.IsNullOrEmpty(selectedPackageUniqueId))
                return;
            if (args.added.Concat(args.removed).Concat(args.updated).Any(p => p.uniqueId == selectedPackageUniqueId))
            {
                GetPackageAndVersion(packageSelectionObject);
                isInspectorDirty = true;
            }
        }

        public override void OnInspectorGUI()
        {
            if (targets.Length > 1)
            {
                GUILayout.Label(k_MultiPackagesSelectionMessage, EditorStyles.helpBox);
                return;
            }

            if (packageSelectionObject == null)
            {
                EditorGUILayout.HelpBox(k_PackageNotAccessibleMessage, MessageType.Error);
                return;
            }

            if (m_Package == null || m_Version == null)
            {
                GetPackageAndVersion(packageSelectionObject);
                if (m_Package == null || m_Version == null)
                {
                    EditorGUILayout.HelpBox(k_PackageNotAccessibleMessage, MessageType.Error);
                    return;
                }
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
            GUI.enabled = true;

            // Package information
            GUILayout.Label(Styles.information, EditorStyles.boldLabel);
            DoPackageInformationLayout();

            // Package description
            GUILayout.Label(Styles.description, EditorStyles.boldLabel);
            DoPackageDescriptionLayout();

            // Dependencies or Packages included section
            var dependenciesTitleText = EditorGUIUtility.TrTextContent(
                m_Version.HasTag(PackageTag.Feature) ? "Packages included" : "Dependencies");
            GUILayout.Label(dependenciesTitleText, EditorStyles.boldLabel);
            m_List.DoLayoutList();

            GUI.enabled = previousEnabled;
        }

        internal override void OnHeaderTitleGUI(Rect titleRect, string header)
        {
            if (targets.Length > 1)
                header = string.Format(L10n.Tr("{0} Packages"), targets.Length);

            base.OnHeaderTitleGUI(titleRect, header);
        }

        internal override void OnHeaderControlsGUI()
        {
            base.OnHeaderControlsGUI();

            if (targets.Length > 1)
                return;

            var previousEnabled = GUI.enabled;

            PackageManifest manifest = null;
            if (m_Version?.HasTag(PackageTag.Custom) ?? false)
            {
                var packageInfo = m_UpmCache.GetBestMatchPackageInfo(m_Version.name, m_Version.isInstalled, m_Version.versionString);
                manifest = m_AssetDatabase.LoadAssetAtPath<PackageManifest>($"{packageInfo.assetPath}/package.json");
            }
            GUI.enabled =  manifest != null;
            if (GUILayout.Button(Styles.editPackage, EditorStyles.miniButton))
                m_Selection.activeObject = manifest;

            GUI.enabled = m_Package != null && m_Version != null;
            if (GUILayout.Button(Styles.viewInPackageManager, EditorStyles.miniButton))
            {
                PackageManagerWindow.SelectPackageAndPageStatic(m_Version.HasTag(PackageTag.LegacyFormat) ? m_Version.package.uniqueId : m_Version.uniqueId);
            }
            GUI.enabled = previousEnabled;
        }

        internal override void OnForceReloadInspector()
        {
            base.OnForceReloadInspector();

            var packageDatabase = ServicesContainer.instance.Resolve<IPackageDatabase>();
            if (packageSelectionObject != null && (m_Package == null || m_Version == null))
            {
                m_Package = packageDatabase.GetPackage(packageSelectionObject.packageUniqueId);
                m_Version = m_Package?.versions.primary;
            }
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
            var versionString = dependency.version;
            var package = m_PackageDatabase.GetPackage(dependency.name);
            if (package != null)
            {
                packageName = string.IsNullOrEmpty(package.displayName) ? package.name : package.displayName;
                var versionToUse = package.versions.lifecycleVersion ?? package.versions.primary;
                if (versionString == "default")
                    versionString = versionToUse?.versionString ?? dependency.version;

                if (versionToUse?.HasTag(PackageTag.BuiltIn) == true)
                    versionString = string.Empty;
            }

            var w = rect.width;
            rect.x += 4;
            rect.width = w / 3 * 2 - 2;
            rect.height -= EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.SelectableLabel(rect, packageName);

            rect.x += w / 3 * 2;
            rect.width = w / 3 - 4;
            EditorGUI.SelectableLabel(rect, versionString);
        }

        private void DoPackageInformationLayout()
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true)))
            {
                var labels = new List<GUIContent>();
                if (!string.IsNullOrEmpty(m_Version.name))
                    labels.Add(Styles.name);
                labels.Add(Styles.displayName);
                if (!m_Version.HasTag(PackageTag.Feature))
                    labels.Add(Styles.version);
                labels.Add(Styles.category);

                var contents = new List<string>();
                if (!string.IsNullOrEmpty(m_Version.name))
                    contents.Add(m_Version.name);
                contents.Add(m_Version.displayName);
                if (!m_Version.HasTag(PackageTag.Feature))
                    contents.Add(m_Version.version.ToString());
                contents.Add(m_Version.category);

                SelectableLabelFields(labels, contents);
            }
        }

        private void DoPackageDescriptionLabel()
        {
            var descriptionStyle = EditorStyles.textArea;
            var description = !string.IsNullOrEmpty(m_Package.product?.description) ? m_Package.product.description : m_Version.description;
            var descriptionRect = GUILayoutUtility.GetRect(EditorGUIUtility.TempContent(description), descriptionStyle, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            EditorGUI.SelectableLabel(descriptionRect, description, descriptionStyle);
        }

        private void DoPackageDescriptionLayout()
        {
            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true)))
            {
                using (var scrollView = new EditorGUILayout.VerticalScrollViewScope(m_ScrollPosition,
                    GUILayout.MinHeight(m_Version.HasTag(PackageTag.LegacyFormat) ? kMinHeightForAssetStore : kMinHeightForOther)))
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
