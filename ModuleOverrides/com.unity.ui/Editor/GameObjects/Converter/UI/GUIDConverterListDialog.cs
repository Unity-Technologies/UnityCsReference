// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    // Dialog with a message, a list of assets and buttons to help with the UI Toolkit Package Asset conversion.
    internal class GUIDConverterListDialog : EditorWindow
    {
        private const string k_DialogVisualTreeAssetPath = "UIPackageResources/UXML/Converter/GUIDConverterListDialog.uxml";
        private const string k_DialogStylePath = "UIPackageResources/StyleSheets/Converter/GUIDConverterListDialog.uss";

        public static GUIDConverterListDialog OpenListDialog(string title, string message, List<string> assetsList,
            string confirmButtonText, bool cancelable = false, Action confirmAction = null, Action cancelAction = null)
        {
            GUIDConverterListDialog wnd = CreateWindow<GUIDConverterListDialog>(title);
            wnd.Init(message, assetsList, confirmButtonText, cancelable, confirmAction, cancelAction);
            wnd.Show();

            return wnd;
        }

        private const string k_StyleDisplayHidden = "unity-guid-converter__display-hidden";

        private const string k_TopMessageName = "top-message";
        private const string k_AssetsListName = "assets-list";
        private const string k_OkButtonName = "ok-button";
        private const string k_CancelButtonName = "cancel-button";

        private Label m_Message;
        private ListView m_ListView;
        private Button m_ConfirmButton;
        private Button m_CancelButton;

        public void CreateGUI()
        {
            SetEditorWindowSize();

            var visualTree = EditorGUIUtility.Load(k_DialogVisualTreeAssetPath) as VisualTreeAsset;
            var contents = visualTree.Instantiate();
            contents.style.flexGrow = 1;

            m_Message = contents.MandatoryQ<Label>(k_TopMessageName);
            m_ListView = contents.MandatoryQ<ListView>(k_AssetsListName);
            m_ConfirmButton = contents.MandatoryQ<Button>(k_OkButtonName);
            m_CancelButton = contents.MandatoryQ<Button>(k_CancelButtonName);

            rootVisualElement.Add(contents);

            var styleSheet = EditorGUIUtility.Load(k_DialogStylePath) as StyleSheet;
            rootVisualElement.styleSheets.Add(styleSheet);
        }

        private void SetEditorWindowSize()
        {
            EditorWindow editorWindow = this;

            Vector2 currentWindowSize = editorWindow.minSize;

            editorWindow.minSize = new Vector2(Mathf.Max(600, currentWindowSize.x), Mathf.Max(300, currentWindowSize.y));
        }

        private void Init(string message, List<string> assetsList, string confirmButtonText, bool cancelable, Action confirmAction,
            Action cancelAction)
        {
            m_Message.text = message;

            if (m_ListView.makeItem == null)
            {
                Func<VisualElement> makeItem = () =>
                {
                    var label = new Label();
                    label.style.flexWrap = Wrap.Wrap;
                    label.style.unityTextAlign = TextAnchor.MiddleLeft;
                    return label;
                };
                Action<VisualElement, int> bindItem = (e, i) => (e as Label).text = "  " + assetsList[i];
                m_ListView.makeItem = makeItem;
                m_ListView.bindItem = bindItem;
            }

            m_ListView.itemsSource = assetsList;
            m_ListView.selectionType = SelectionType.None;

            m_ConfirmButton.text = confirmButtonText;

            m_ConfirmButton.RegisterCallback<ClickEvent>(evt =>
            {
                confirmAction?.Invoke();
                CloseWindow();
            });
            if (!cancelable)
            {
                m_CancelButton.EnableInClassList(k_StyleDisplayHidden, true);
            }
            else
            {
                m_CancelButton.RegisterCallback<ClickEvent>(evt =>
                {
                    cancelAction?.Invoke();
                    CloseWindow();
                });
            }
        }

        public void CloseWindow()
        {
            Close();
            DestroyImmediate(this);
        }
    }
}
