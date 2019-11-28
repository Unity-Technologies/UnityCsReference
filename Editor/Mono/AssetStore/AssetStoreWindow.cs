// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Connect;
using UnityEditor.PackageManager.UI;

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
            AssetStoreWindow window = GetWindow<AssetStoreWindow>(typeof(SceneView));
            window.SetMinMaxSizes();
            window.Show();
            return window;
        }

        public void OnEnable()
        {
            this.SetAntiAliasing(4);
            titleContent = GetLocalizedTitleContent();
            var windowResource = EditorGUIUtility.Load("UXML/AssetStore/AssetStoreWindow.uxml") as VisualTreeAsset;
            if (windowResource != null)
            {
                var root = windowResource.CloneTree();

                var styleSheet = EditorGUIUtility.Load("StyleSheets/AssetStore/AssetStoreWindow.uss") as StyleSheet;
                styleSheet.isUnityStyleSheet = true;
                root.styleSheets.Add(styleSheet);

                rootVisualElement.Add(root);
                root.StretchToParentSize();

                visitWebsiteButton.clickable.clicked += OnVisitWebsiteButtonClicked;
                launchPackageManagerButton.clickable.clicked += OnLaunchPackageManagerButtonClicked;
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
            string assetStoreUrl = UnityConnect.instance.GetConfigurationURL(CloudConfigUrl.CloudAssetStoreUrl);
            if (UnityEditor.Connect.UnityConnect.instance.loggedIn)
                UnityEditor.Connect.UnityConnect.instance.OpenAuthorizedURLInWebBrowser(assetStoreUrl);
            else Application.OpenURL(assetStoreUrl);
        }

        private void OnLaunchPackageManagerButtonClicked()
        {
            PackageManagerWindow.OpenPackageManager(null);
        }

        private void SetMinMaxSizes()
        {
            this.minSize = new Vector2(455, 354);
            this.maxSize = new Vector2(4000, 4000);
        }

        private Button visitWebsiteButton { get { return rootVisualElement.Q<Button>("visitWebsiteButton"); } }
        private Button launchPackageManagerButton { get { return rootVisualElement.Q<Button>("launchPackageManagerButton"); } }
    }
}
