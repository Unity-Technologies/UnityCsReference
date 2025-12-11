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
        private const float k_MinHeightForAssetStore = 192f;
        private const float k_MinHeightForOther = 96f;
        private const float k_LabelMinWidth = 64f;

        private static readonly string k_PackageNotAccessibleMessage = L10n.Tr("This package is not accessible anymore.");
        private static readonly string k_MultiPackagesSelectionMessage = L10n.Tr("Multi-object editing not supported.");
        internal override string targetTitle
        {
            get
            {
                if (packageSelectionObject is null)
                    return base.targetTitle;

                if (m_Version == null)
                    return string.Format(L10n.Tr("Package '{0}' Manifest"), packageSelectionObject.displayName);

                var descriptor = m_Version.GetDescriptor(true);
                var displayName = string.IsNullOrEmpty(m_Version.displayName) ? m_Version.name : m_Version.displayName;
                return string.Format(L10n.Tr("{0} '{1}' Manifest"), descriptor, displayName);
            }
        }

        private static class Styles
        {
            public static readonly GUIContent packageInformationTitle = EditorGUIUtility.TrTextContent("Package information", "Package information");
            public static readonly GUIContent name = EditorGUIUtility.TrTextContent("Name", "Name");
            public static readonly GUIContent displayName = EditorGUIUtility.TrTextContent("Display name", "Display name");
            public static readonly GUIContent version = EditorGUIUtility.TrTextContent("Version", "Version");
            public static readonly GUIContent category = EditorGUIUtility.TrTextContent("Category", "Category");
            public static readonly GUIContent documentationUrl = EditorGUIUtility.TrTextContent("Documentation URL", "Documentation URL");
            public static readonly GUIContent licensesUrl = EditorGUIUtility.TrTextContent("Licenses URL", "Licenses URL");
            public static readonly GUIContent changelogUrl = EditorGUIUtility.TrTextContent("Changelog URL", "Changelog URL");

            public static readonly GUIContent authorInformationTitle = EditorGUIUtility.TrTextContent("Author information", "Author information");
            public static readonly GUIContent authorName = EditorGUIUtility.TrTextContent("Name", "Name");
            public static readonly GUIContent authorUrl = EditorGUIUtility.TrTextContent("URL", "URL");
            public static readonly GUIContent authorEmail = EditorGUIUtility.TrTextContent("Email", "Email");

            public static readonly GUIContent minimumUnityVersionTitle = EditorGUIUtility.TrTextContent("Minimum Unity version", "Minimum Unity version");
            public static readonly GUIContent unityVersion = EditorGUIUtility.TrTextContent("Unity", "Unity version");
            public static readonly GUIContent unityReleaseVersion = EditorGUIUtility.TrTextContent("Unity release", "Unity release version");

            public static readonly GUIContent description = EditorGUIUtility.TrTextContent("Description", "Description");
            public static readonly GUIContent package = EditorGUIUtility.TrTextContent("Package name", "Package name");

            public static readonly GUIContent packagesIncluded = EditorGUIUtility.TrTextContent("Packages included", "Packages included");
            public static readonly GUIContent dependencies = EditorGUIUtility.TrTextContent("Dependencies", "Dependencies");

            public static readonly GUIContent editPackage = EditorGUIUtility.TrTextContent("Edit", "Edit");
            public static readonly GUIContent viewInPackageManager = EditorGUIUtility.TrTextContent("View in Package Manager", "View in Package Manager");
        }

        private PackageSelectionObject packageSelectionObject => target as PackageSelectionObject;

        [HideInInspector]
        [SerializeField]
        private Vector2 m_ScrollPosition;

        [HideInInspector]
        [SerializeField]
        private ReorderableList m_List;

        [NonSerialized]
        private IPackageVersion m_Version;

        [NonSerialized]
        private PackageInfo m_PackageInfo;

        private IPackageOperationDispatcher m_OperationDispatcher;
        private IUpmCache m_UpmCache;
        private IPackageDatabase m_PackageDatabase;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_OperationDispatcher = container.Resolve<IPackageOperationDispatcher>();
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

        private void GetPackageVersionFromSelection(string selectedPackageUniqueId)
        {
            m_Version = m_PackageDatabase.GetPackage(selectedPackageUniqueId)?.versions.primary;
            m_PackageInfo = m_UpmCache.GetBestMatchPackageInfo(m_Version?.name, m_Version?.package.product?.id ?? 0, m_Version?.isInstalled ?? false, m_Version?.versionString);
        }

        private void OnPackagesChanged(PackagesChangeArgs args)
        {
            var selectedPackageUniqueId = packageSelectionObject?.packageUniqueId;
            if (string.IsNullOrEmpty(selectedPackageUniqueId))
                return;

            if (args.added.Concat(args.removed).Concat(args.updated).AnyMatches(p => p.uniqueId == selectedPackageUniqueId))
            {
                GetPackageVersionFromSelection(selectedPackageUniqueId);
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

            var selectedPackageUniqueId = packageSelectionObject?.packageUniqueId;
            if (m_Version == null && !string.IsNullOrEmpty(selectedPackageUniqueId))
                GetPackageVersionFromSelection(selectedPackageUniqueId);

            if (m_Version == null)
            {
                EditorGUILayout.HelpBox(k_PackageNotAccessibleMessage, MessageType.Error);
                return;
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
            GUI.enabled = false;

            DoPackageInformationLayout();
            DoAuthorInformationLayout();
            DoMinimumUnityVersionLayout();
            DoPackageDescriptionLayout();
            DoPackageDependenciesLayout();

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

            GUI.enabled = m_Version != null && m_Version.HasTag(PackageTag.Custom | PackageTag.Local);
            if (GUILayout.Button(Styles.editPackage, EditorStyles.miniButton))
                m_OperationDispatcher.OpenManifest(m_Version);

            GUI.enabled = m_Version != null;
            if (GUILayout.Button(Styles.viewInPackageManager, EditorStyles.miniButton))
                PackageManagerWindow.OpenAndSelectPackage(m_Version.package.uniqueId);

            GUI.enabled = previousEnabled;
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
                var versionToUse = package.versions.recommended ?? package.versions.primary;
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
            GUILayout.Label(Styles.packageInformationTitle, EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true)))
            {
                var items = new List<(GUIContent label, string content)>();
                if (!string.IsNullOrEmpty(m_Version.name))
                    items.Add((Styles.name, m_Version.name));
                items.Add((Styles.displayName, m_Version.displayName));
                if (!m_Version.HasTag(PackageTag.Feature))
                    items.Add((Styles.version, m_Version.version.ToString()));
                if (!string.IsNullOrEmpty(m_Version.category))
                    items.Add((Styles.category, m_Version.category));
                if (!string.IsNullOrEmpty(m_PackageInfo?.documentationUrl))
                    items.Add((Styles.documentationUrl, m_PackageInfo.documentationUrl));
                if (!string.IsNullOrEmpty(m_PackageInfo?.licensesUrl))
                    items.Add((Styles.licensesUrl, m_PackageInfo.licensesUrl));
                if (!string.IsNullOrEmpty(m_PackageInfo?.changelogUrl))
                    items.Add((Styles.changelogUrl, m_PackageInfo.changelogUrl));
                SelectableLabelFields(items);
            }
        }

        private void DoAuthorInformationLayout()
        {
            if (m_Version.author == null)
                return;

            var items = new List<(GUIContent label, string content)>();
            if (!string.IsNullOrEmpty(m_Version.author.name))
                items.Add((Styles.authorName, m_Version.author.name));
            if (!string.IsNullOrEmpty(m_Version.author.url))
                items.Add((Styles.authorUrl, m_Version.author.url));
            if (!string.IsNullOrEmpty(m_Version.author.email))
                items.Add((Styles.authorEmail, m_Version.author.email));

            if (items.Count == 0)
                return;

            GUILayout.Label(Styles.authorInformationTitle, EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true)))
                SelectableLabelFields(items);
        }

        private void DoMinimumUnityVersionLayout()
        {
            if (m_Version.HasTag(PackageTag.Feature | PackageTag.BuiltIn) || string.IsNullOrEmpty(m_Version.minimumUnityVersion))
                return;

            var minimumUnityVersionSplit = m_Version.minimumUnityVersion.Split('.');
            var items = new List<(GUIContent label, string content)>();
            if (minimumUnityVersionSplit.Length >= 2)
                items.Add((Styles.unityVersion, minimumUnityVersionSplit[0] + "." + minimumUnityVersionSplit[1]));
            if (minimumUnityVersionSplit.Length == 3)
                items.Add((Styles.unityReleaseVersion, minimumUnityVersionSplit[2]));

            if (items.Count == 0)
                return;

            GUILayout.Label(Styles.minimumUnityVersionTitle, EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true)))
                SelectableLabelFields(items);
        }

        private void DoPackageDescriptionLayout()
        {
            GUILayout.Label(Styles.description, EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.ExpandWidth(true)))
                using (var scrollView = new EditorGUILayout.VerticalScrollViewScope(m_ScrollPosition,
                    GUILayout.MinHeight(m_Version.HasTag(PackageTag.LegacyFormat) ? k_MinHeightForAssetStore : k_MinHeightForOther)))
                {
                    m_ScrollPosition = scrollView.scrollPosition;
                    var descriptionStyle = EditorStyles.textArea;
                    var description = !string.IsNullOrEmpty(m_Version.package.product?.description) ? m_Version.package.product.description : m_Version.description;
                    var descriptionRect = GUILayoutUtility.GetRect(EditorGUIUtility.TempContent(description), descriptionStyle, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                    EditorGUI.SelectableLabel(descriptionRect, description, descriptionStyle);
                }
        }

        private void DoPackageDependenciesLayout()
        {
            var dependenciesTitleText = m_Version.HasTag(PackageTag.Feature) ? Styles.packagesIncluded : Styles.dependencies;
            GUILayout.Label(dependenciesTitleText, EditorStyles.boldLabel);
            m_List.DoLayoutList();
        }

        private static void SelectableLabelFields(IReadOnlyCollection<(GUIContent label, string content)> items)
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(150));
            foreach (var (label, _) in items)
                GUILayout.Label(label, GUILayout.MinWidth(k_LabelMinWidth), GUILayout.Height(EditorGUI.kSingleLineHeight));
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            foreach (var (_, content) in items)
                EditorGUILayout.SelectableLabel(content, EditorStyles.textField, GUILayout.Height(EditorGUI.kSingleLineHeight));
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }
}
