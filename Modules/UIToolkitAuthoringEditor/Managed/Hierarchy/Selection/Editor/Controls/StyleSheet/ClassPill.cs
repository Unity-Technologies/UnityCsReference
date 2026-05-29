// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class ClassPill : VisualElement
    {
        static readonly string s_UxmlPath = "UIToolkitAuthoring/Inspector/StyleSheet/ClassPill.uxml";
        static readonly string s_UssPath = "UIToolkitAuthoring/Inspector/StyleSheet/ClassPill.uss";
        static readonly string k_StyleSheetDark = "UIToolkitAuthoring/Inspector/StyleSheet/ClassPillDark.uss";
        static readonly string k_StyleSheetLight = "UIToolkitAuthoring/Inspector/StyleSheet/ClassPillLight.uss";

        public const string ussClassName = "unity-class-pill";
        public const string NotInDocumentClassName = ussClassName + "--not-in-document";

        Label m_Label;
        Button m_DeleteButton;
        bool m_SimpleSelectorExists;

        public Label labelElement => m_Label;
        public string selectorAsString { get; set; }
        public VisualElement selectorElement { get; set; }
        public Clickable onDeleteClickable => m_DeleteButton.clickable;
        public bool isDragged;

        // Set's the Label element's text
        public string text
        {
            get => m_Label.text;
            set => m_Label.text = value;
        }

        public bool canBeRemoved
        {
            get => m_DeleteButton.style.display == DisplayStyle.Flex;
            set
            {
                m_DeleteButton.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public bool simpleSelectorExists
        {
            get => m_SimpleSelectorExists;
            set
            {
                if (m_SimpleSelectorExists == value)
                    return;
                m_SimpleSelectorExists = value;
                EnableInClassList(NotInDocumentClassName, !simpleSelectorExists);
            }
        }

        public ClassPill()
        {
            styleSheets.Add(EditorGUIUtility.Load(s_UssPath) as StyleSheet);

            var styleSheetPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
            var styleSheet = EditorGUIUtility.Load(styleSheetPath) as StyleSheet;
            styleSheets.Add(styleSheet);

            var visualAsset = EditorGUIUtility.Load(s_UxmlPath) as VisualTreeAsset;
            visualAsset.CloneTree(this);
            AddToClassList(ussClassName);
            m_Label = this.Q<Label>("class-name-label");
            m_DeleteButton = this.Q<Button>("delete-class-button");
            simpleSelectorExists = true;
        }

        public void SetDeleteButtonUserData(object data)
        {
            m_DeleteButton.userData = data;
        }
    }
}
