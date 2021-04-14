// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    // Main dialog for the UI Toolkit Package Asset conversion. Has some textual information and 2 buttons to trigger
    // conversion from either package asset guids to trunk asset guids, or from trunk asset guids to package asset guids.
    internal class GUIDConverterMainDialog : EditorWindow
    {
        private const string k_DialogVisualTreeAssetPath = "UIPackageResources/UXML/Converter/GUIDConverterMainDialog.uxml";
        private const string k_DialogStylePath = "UIPackageResources/StyleSheets/Converter/GUIDConverterMainDialog.uss";

        public static void OpenDialog(string title, Action convertToTrunkAssetsAction, Action convertToPackageAssetsAction)
        {
            GUIDConverterMainDialog wnd = GetWindow<GUIDConverterMainDialog>(title);
            wnd.Init(convertToTrunkAssetsAction, convertToPackageAssetsAction);
            wnd.Show();
        }

        private const string k_ConvertToTrunkAssetsButton = "convert-to-trunk-assets";
        private const string k_ConvertToPackageAssetsButton = "convert-to-package-assets";

        private Button m_ConvertToTrunkAssetsButton;
        private Button m_ConvertToPackageAssetsButton;

        public void CreateGUI()
        {
            SetEditorWindowSize();

            var visualTree = EditorGUIUtility.Load(k_DialogVisualTreeAssetPath) as VisualTreeAsset;
            var contents = visualTree.Instantiate();
            contents.style.flexGrow = 1;

            m_ConvertToTrunkAssetsButton = contents.MandatoryQ<Button>(k_ConvertToTrunkAssetsButton);
            m_ConvertToPackageAssetsButton = contents.MandatoryQ<Button>(k_ConvertToPackageAssetsButton);

            rootVisualElement.Add(contents);

            var styleSheet = EditorGUIUtility.Load(k_DialogStylePath) as StyleSheet;
            rootVisualElement.styleSheets.Add(styleSheet);
        }

        private void SetEditorWindowSize()
        {
            EditorWindow editorWindow = this;

            Vector2 currentWindowSize = editorWindow.minSize;

            editorWindow.minSize = new Vector2(Mathf.Max(600, currentWindowSize.x), Mathf.Max(360, currentWindowSize.y));
        }

        private void Init(Action convertToTrunkAssetsAction, Action convertToPackageAssetsAction)
        {
            m_ConvertToTrunkAssetsButton.RegisterCallback<ClickEvent>(etv =>
            {
                convertToTrunkAssetsAction?.Invoke();
                CloseWindow();
            });
            m_ConvertToPackageAssetsButton.RegisterCallback<ClickEvent>(etv =>
            {
                convertToPackageAssetsAction?.Invoke();
                CloseWindow();
            });
        }

        private void CloseWindow()
        {
            Close();
            DestroyImmediate(this);
        }
    }
}
