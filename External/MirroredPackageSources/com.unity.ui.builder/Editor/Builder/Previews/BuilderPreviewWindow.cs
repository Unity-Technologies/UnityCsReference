using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderPreviewWindow : EditorWindow
    {
        ObjectField m_UxmlAssetField;
        VisualElement m_Container;

        [SerializeField]
        VisualTreeAsset m_CurrentVisualTreeAsset;

        //[MenuItem("Tests/UI Builder/Document Preview")]
        public static void ShowWindow()
        {
            var window = GetWindow<BuilderPreviewWindow>();
            window.titleContent = new GUIContent("Builder Document Preview");
            window.Show();
        }

        public void OnEnable()
        {
            var root = rootVisualElement;

            // Load styles.
            root.styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UIBuilderPackagePath + "/Builder.uss"));
            if (EditorGUIUtility.isProSkin)
                root.styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UIBuilderPackagePath + "/BuilderDark.uss"));
            else
                root.styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(BuilderConstants.UIBuilderPackagePath + "/BuilderLight.uss"));

            // Load template.
            var builderTemplate = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(BuilderConstants.UIBuilderPackagePath + "/BuilderPreviewWindow.uxml");
            builderTemplate.CloneTree(root);

            // Init field.
            m_UxmlAssetField = root.Q<ObjectField>();
            m_UxmlAssetField.objectType = typeof(VisualTreeAsset);
            m_UxmlAssetField.RegisterValueChangedCallback(VisualTreeAssetChanged);
            m_Container = root.Q("container");

            // Clone tree if we have an asset loaded.
            m_UxmlAssetField.value = m_CurrentVisualTreeAsset;
            CloneTree();
        }

        void VisualTreeAssetChanged(ChangeEvent<Object> evt)
        {
            m_CurrentVisualTreeAsset = evt.newValue as VisualTreeAsset;
            CloneTree();
        }

        void CloneTree()
        {
            m_Container.Clear();

            if (m_CurrentVisualTreeAsset == null)
                return;

            m_CurrentVisualTreeAsset.CloneTree(m_Container);
        }
    }
}
