// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIBuilder not yet converted
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderPreviewWindow : EditorWindow
    {
        #pragma warning disable UAL0015 // this side effect does not outlive the current call (global trigger / lazily-loaded asset re-fetched on next access); a stale reference is harmlessly replaced
        internal BuilderPreviewWindow() {}
        #pragma warning restore UAL0015

        ObjectField m_UxmlAssetField;
        VisualElement m_Container;

        [SerializeField]
        VisualTreeAsset m_CurrentVisualTreeAsset;

        internal override BindingLogLevel defaultBindingLogLevel => BindingLogLevel.None;

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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
