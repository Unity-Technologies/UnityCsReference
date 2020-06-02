// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Connect;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Asset Store", icon = "Asset Store")]
    internal class AssetStoreWindow : EditorWindow
    {
        // Use this for initialization
        // Index at 1499 because "Package Manager" is 1500, pairing tools for user to get external content
        [MenuItem("Window/Asset Store", false, 1499)]
        public static AssetStoreWindow Init()
        {
            if (EditorPrefs.GetBool("AlwaysOpenAssetStoreInBrowser", false))
            {
                OpenAssetStoreInBrowser();
                return null;
            }
            else
            {
                AssetStoreWindow window = GetWindow<AssetStoreWindow>(typeof(SceneView));
                window.SetMinMaxSizes();
                window.Show();
                return window;
            }
        }

        private static void OpenAssetStoreInBrowser()
        {
            string assetStoreUrl = UnityConnect.instance.GetConfigurationURL(CloudConfigUrl.CloudAssetStoreUrl);
            if (UnityEditor.Connect.UnityConnect.instance.loggedIn)
                UnityEditor.Connect.UnityConnect.instance.OpenAuthorizedURLInWebBrowser(assetStoreUrl);
            else Application.OpenURL(assetStoreUrl);
        }

        public void OnEnable()
        {
            this.antiAliasing = 4;
            titleContent = GetLocalizedTitleContent();
            var windowResource = EditorGUIUtility.Load("UXML/AssetStore/AssetStoreWindow.uxml") as VisualTreeAsset;
            if (windowResource != null)
            {
                var root = windowResource.CloneTree();

                var lightStyleSheet = EditorGUIUtility.Load(EditorUIService.instance.GetUIToolkitDefaultCommonLightStyleSheetPath()) as StyleSheet;
                var assetStoreStyleSheet = EditorGUIUtility.Load("StyleSheets/AssetStore/AssetStoreWindow.uss") as StyleSheet;
                var styleSheet = CreateInstance<StyleSheet>();
                styleSheet.isUnityStyleSheet = true;

                var resolver = new StyleSheets.StyleSheetResolver();
                resolver.AddStyleSheets(lightStyleSheet, assetStoreStyleSheet);
                resolver.ResolveTo(styleSheet);
                root.styleSheets.Add(styleSheet);

                rootVisualElement.Add(root);
                root.StretchToParentSize();

                visitWebsiteButton.clickable.clicked += OnVisitWebsiteButtonClicked;
                launchPackageManagerButton.clickable.clicked += OnLaunchPackageManagerButtonClicked;

                alwaysOpenInBrowserToggle.SetValueWithoutNotify(EditorPrefs.GetBool("AlwaysOpenAssetStoreInBrowser", false));

                alwaysOpenInBrowserToggle.RegisterValueChangedCallback(changeEvent =>
                {
                    EditorPrefs.SetBool("AlwaysOpenAssetStoreInBrowser", changeEvent.newValue);
                });
            }
        }

        public void OnDisable()
        {
            visitWebsiteButton.clickable.clicked -= OnVisitWebsiteButtonClicked;
            launchPackageManagerButton.clickable.clicked -= OnLaunchPackageManagerButtonClicked;
        }

        public static void OpenURL(string url)
        {
            string assetStoreUrl = $"{UnityConnect.instance.GetConfigurationURL(CloudConfigUrl.CloudAssetStoreUrl)}/packages/{url}";
            if (UnityEditor.Connect.UnityConnect.instance.loggedIn)
                UnityEditor.Connect.UnityConnect.instance.OpenAuthorizedURLInWebBrowser(assetStoreUrl);
            else Application.OpenURL(assetStoreUrl);
        }

        private void OnVisitWebsiteButtonClicked()
        {
            OpenAssetStoreInBrowser();
        }

        private void OnLaunchPackageManagerButtonClicked()
        {
            EditorUIService.instance.PackageManagerOpen();
        }

        private void SetMinMaxSizes()
        {
            this.minSize = new Vector2(455, 354);
            this.maxSize = new Vector2(4000, 4000);
        }

        private Button visitWebsiteButton { get { return rootVisualElement.Q<Button>("visitWebsiteButton"); } }
        private Button launchPackageManagerButton { get { return rootVisualElement.Q<Button>("launchPackageManagerButton"); } }
        private Toggle alwaysOpenInBrowserToggle { get { return rootVisualElement.Q<Toggle>("alwaysOpenInBrowserToggle"); } }
    }
}
