using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.StyleSheets;
using UnityEditor;
using UnityEditor.UIElements;

namespace Unity.UI.Builder
{
    internal class UIExtensionsTestWindow : EditorWindow
    {
        static readonly string s_DefaultUSSPath = BuilderConstants.UIBuilderPackagePath + "/SampleDocument/BuilderSampleCanvas.uss";
        static readonly string s_DefaultUXMLPath = BuilderConstants.UIBuilderPackagePath + "/SampleDocument/BuilderSampleCanvas.uxml";
        static readonly string s_CanvasInstanceUSSPath = BuilderConstants.UIBuilderPackagePath + "/SampleDocument/BuilderSampleCanvasSection.uss";

        ObjectField m_StyleSheetField;
        VisualElement m_StyleSheetContents;

        ObjectField m_VisualTreeAssetField;
        VisualElement m_VisualTreeAssetContents;

        VisualElement m_Container;

        //[SerializeField]
        StyleSheet m_StyleSheet;

        //[SerializeField]
        VisualTreeAsset m_VisualTreeAsset;

        //[MenuItem("Tests/UI Builder/UI Extensions Test")]
        public static void ShowWindow()
        {
            var window = GetWindow<UIExtensionsTestWindow>();
            window.titleContent = new GUIContent("UI Extensions Test");
            window.Show();
        }

        public void OnEnable()
        {
            var root = rootVisualElement;

            // Load styles.
            root.styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(
                BuilderConstants.UtilitiesPath + "/UIExtensionsTestWindow/UIExtensionsTestWindow.uss"));

            // Load template.
            var builderTemplate = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UtilitiesPath + "/UIExtensionsTestWindow/UIExtensionsTestWindow.uxml");
            builderTemplate.CloneTree(root);

            // Init fields.
            m_StyleSheetField = root.Q<ObjectField>("uss-object-field");
            m_StyleSheetField.objectType = typeof(StyleSheet);
            m_StyleSheetField.RegisterValueChangedCallback(StyleSheetChanged);
            m_StyleSheetContents = root.Q("uss-contents");
            m_VisualTreeAssetField = root.Q<ObjectField>("uxml-object-field");
            m_VisualTreeAssetField.objectType = typeof(VisualTreeAsset);
            m_VisualTreeAssetContents = root.Q("uxml-contents");
            m_VisualTreeAssetField.RegisterValueChangedCallback(VisualTreeAssetChanged);

            // Get container;
            m_Container = root.Q("container");

            AssetDatabase.ImportAsset(s_DefaultUSSPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(s_DefaultUXMLPath, ImportAssetOptions.ForceUpdate);

            root.schedule.Execute(AfterImport);
        }

        void AfterImport()
        {
            m_StyleSheet = BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(s_DefaultUSSPath);
            m_VisualTreeAsset = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(s_DefaultUXMLPath);

            // Clone tree if we have an asset loaded.
            m_StyleSheetField.SetValueWithoutNotify(m_StyleSheet);
            m_VisualTreeAssetField.SetValueWithoutNotify(m_VisualTreeAsset);

            try
            {
                CloneTree();
            }
            catch (System.Exception e)
            {
                Debug.Log(e.ToString());
            }
        }

        void StyleSheetChanged(ChangeEvent<Object> evt)
        {
            m_StyleSheet = evt.newValue as StyleSheet;
            CloneTree();
        }

        void VisualTreeAssetChanged(ChangeEvent<Object> evt)
        {
            m_VisualTreeAsset = evt.newValue as VisualTreeAsset;
            CloneTree();
        }

        void CloneTree()
        {
            m_Container.Clear();
            m_StyleSheetContents.Clear();
            m_VisualTreeAssetContents.Clear();

            m_Container.styleSheets.Clear();

            if (m_VisualTreeAsset != null)
            {
                m_VisualTreeAsset.LinkedCloneTree(m_Container);

                var canvas = m_Container.Q("sample-canvas");
                var canvasAsset = canvas.GetVisualElementAsset();

                var newButton = m_VisualTreeAsset.AddElement(canvasAsset, "UnityEngine.UIElements.Button");
                newButton.AddProperty("name", "new-guy");
                newButton.AddProperty("text", "Canvas Button 2!");
                newButton.AddStyleClass("new-guy-type");
                newButton.AddStyleClass("some-button");
                newButton.RemoveStyleClass("some-button");
                newButton.AddStyleClass("some-fancy-button");
                { // Add max-width to newButton.
                    var rule = m_VisualTreeAsset.GetOrCreateInlineStyleRule(newButton);
                    var prop = m_VisualTreeAsset.inlineSheet.AddProperty(rule, "max-width");
                    var val = m_VisualTreeAsset.inlineSheet.AddValue(prop, 200);
                }

                { // Add max-width to canvas.
                    var rule = m_VisualTreeAsset.GetOrCreateInlineStyleRule(canvasAsset);
                    var prop = m_VisualTreeAsset.inlineSheet.AddProperty(rule, "max-width");
                    var val = m_VisualTreeAsset.inlineSheet.AddValue(prop, 500);
                }
                { // Change border of canvas.
                    var rule = m_VisualTreeAsset.GetOrCreateInlineStyleRule(canvasAsset);
                    var prop = m_VisualTreeAsset.inlineSheet.FindProperty(rule, "border-width");
                    m_VisualTreeAsset.inlineSheet.SetValue(prop.values[0], 10);
                }
                { // Remove max-width
                    var rule = m_VisualTreeAsset.GetOrCreateInlineStyleRule(canvasAsset);
                    var prop = m_VisualTreeAsset.inlineSheet.FindProperty(rule, "max-width");
                    m_VisualTreeAsset.inlineSheet.RemoveProperty(rule, prop);
                }

                var newButton2 = m_VisualTreeAsset.AddElement(canvasAsset, "UnityEngine.UIElements.Button");
                m_VisualTreeAsset.RemoveElement(newButton2);

                var newInstance = m_VisualTreeAsset.AddTemplateInstance(canvasAsset, "SampleSection");
                newInstance.SetAttributeOverride("section-text-field", "label", "label programmatically written!");
                newInstance.SetAttributeOverride("section-text-field", "text", "text programmatically written!");
                newInstance.RemoveAttributeOverride("section-text-field", "text");
                newInstance.AddStyleSheetPath(s_CanvasInstanceUSSPath);

                var overriddenSection = m_VisualTreeAsset.FindElementByName("overridden-section");
                if (overriddenSection != null)
                    overriddenSection.RemoveStyleSheetPath(s_CanvasInstanceUSSPath);

                // Add UXML string.
                var uxmlString = m_VisualTreeAsset.GenerateUXML(null);
                m_VisualTreeAssetContents.Add(new Label(uxmlString));

                // Add inline stylesheet.
                var inlineBuilder = new StringBuilder();
                foreach (var rule in m_VisualTreeAsset.inlineSheet.rules)
                {
                    inlineBuilder.Append("{\n");

                    var exportOptions = new UssExportOptions();
                    StyleSheetToUss.ToUssString(m_VisualTreeAsset.inlineSheet, exportOptions, rule, inlineBuilder);

                    inlineBuilder.Append("}\n");
                }
                var inlineStyleSheetString = inlineBuilder.ToString();
                m_VisualTreeAssetContents.Add(new Label(inlineStyleSheetString));

                m_Container.Clear();
                m_VisualTreeAsset.LinkedCloneTree(m_Container);
            }

            if (m_StyleSheet != null)
            {
                // Add width
                //var firstSelector = m_StyleSheet.complexSelectors.First();
                //var firstSelector = m_StyleSheet.FindSelector(".blue#red > .green .pink");
                var firstSelector = m_StyleSheet.FindSelector(".unity-button");
                if (firstSelector != null)
                {
                    var widthProperty = m_StyleSheet.AddProperty(firstSelector, "width");
                    var widthValue = m_StyleSheet.AddValue(widthProperty, 62);
                    m_StyleSheet.SetValue(widthValue, 82);
                    m_StyleSheet.RemoveProperty(firstSelector, widthProperty);

                    var borderWidthProperty = m_StyleSheet.AddProperty(firstSelector, "border-width");
                    m_StyleSheet.AddValue(borderWidthProperty, 1);
                    m_StyleSheet.AddValue(borderWidthProperty, 2);
                    m_StyleSheet.AddValue(borderWidthProperty, 5);
                    var leftBorderWidthValue = m_StyleSheet.AddValue(borderWidthProperty, 8);
                    m_StyleSheet.RemoveValue(borderWidthProperty, leftBorderWidthValue);

                    var borderColorProperty = m_StyleSheet.AddProperty(firstSelector, "border-color");
                    var borderColorValue = m_StyleSheet.AddValue(borderColorProperty, Color.red);
                    m_StyleSheet.SetValue(borderColorValue, Color.green);
                }

                var newSelector = m_StyleSheet.AddSelector(".unity-button Label");
                {
                    var widthProperty = m_StyleSheet.AddProperty(newSelector, "width");
                    var widthValue = m_StyleSheet.AddValue(widthProperty, 62);
                    m_StyleSheet.SetValue(widthValue, 82);
                }

                //

                // Add USS contents.
                //var selectorStrings = m_StyleSheet.GetSelectorStrings();
                //foreach (var selectorString in selectorStrings)
                //m_StyleSheetContents.Add(new Label(selectorString));

                // Add USS string.
                var ussString = m_StyleSheet.GenerateUSS();
                m_StyleSheetContents.Add(new Label(ussString));

                //m_Container.styleSheets.Add(m_StyleSheet);
            }
        }
    }
}
