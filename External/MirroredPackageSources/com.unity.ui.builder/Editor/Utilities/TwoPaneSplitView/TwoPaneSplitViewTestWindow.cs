using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class TwoPaneSplitViewTestWindow : EditorWindow
    {
        //[MenuItem("Tests/UI Builder/TwoPaneSplitViewTest")]
        static void ShowWindow()
        {
            var window = GetWindow<TwoPaneSplitViewTestWindow>();
            window.titleContent = new GUIContent("TwoPaneSplitViewTest");
            window.Show();
        }

        void OnEnable()
        {
            var root = rootVisualElement;

            root.styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UtilitiesPath + "/TwoPaneSplitView/TwoPaneSplitViewTestWindow.uss"));

            var xmlAsset = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.UtilitiesPath + "/TwoPaneSplitView/TwoPaneSplitViewTestWindow.uxml");
            xmlAsset.CloneTree(root);
        }
    }
}
