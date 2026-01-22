// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Hierarchy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal class StyleSheetsWindow : EditorWindow
{
    const string k_MenuPath = "Window/UI Toolkit/Style Sheets";
    const int k_MenuPriority = 3020;

    Hierarchy.Hierarchy m_Hierarchy;
    HierarchyView m_HierarchyView;
    StyleSheetNodeTypeHandler m_Handler;
    Dictionary<StyleSheet, HierarchyNode> m_StyleSheetNodes = new();

    [InitializeOnLoadMethod]
    static void Initialize()
    {
        UIToolkitAuthoringSettings.UIStagesChanged += OnUIStagesChanged;
        EditorApplication.delayCall += () => OnUIStagesChanged(UIToolkitAuthoringSettings.EnableUIStages);
    }

    static void OnUIStagesChanged(bool enabled)
    {
        if (enabled)
        {
            Menu.AddMenuItem(k_MenuPath, "", false, k_MenuPriority, ShowWindow, null);
            return;
        }

        Menu.RemoveMenuItem(k_MenuPath);
    }

    public static void ShowWindow()
    {
        GetWindow<StyleSheetsWindow>().Show();
    }

    void OnEnable()
    {
        titleContent.text = "Style Sheets";
        StageNavigationManager.instance.afterSuccessfullySwitchedToStage += PrepareUI;
    }

    void OnDestroy()
    {
        m_StyleSheetNodes?.Clear();
        m_Hierarchy?.Dispose();
        m_HierarchyView?.Dispose();
        StageNavigationManager.instance.afterSuccessfullySwitchedToStage -= PrepareUI;
    }

    void CreateGUI()
    {
        PrepareUI(StageUtility.GetCurrentStage());
    }

    void PrepareUI(Stage newStage)
    {
        rootVisualElement.Clear();

        if (newStage is not VisualElementEditingStage stage)
        {
            // TODO: Move this style to a stylesheet
            rootVisualElement?.Add(new Label() { text = "Enter staging mode to have access to this feature." , style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleCenter }});
            return;
        }

        var visualTree = EditorGUIUtility.Load("UIToolkitAuthoring/StyleSheets/StyleSheetsWindow.uxml") as VisualTreeAsset;
        visualTree.CloneTree(rootVisualElement);

        var toolbarMenu = rootVisualElement.Q<ToolbarMenu>("add-uss-menu");
        toolbarMenu.menu.AppendAction(
            "Create New USS",
            _ =>
            {
                var ussPath = StyleSheetAssetUtilities.DisplaySaveFileDialogForUSS();
                if (string.IsNullOrEmpty(ussPath))
                    return;

                var command = new CreateNewStyleSheetCommand(stage.EditedVisualTreeAsset, ussPath);
                command.Execute();
            });

        toolbarMenu.menu.AppendAction(
            "Add Existing USS",
            _ =>
            {
                var ussPath = StyleSheetAssetUtilities.DisplayOpenFileDialogForUSS();
                if (string.IsNullOrEmpty(ussPath))
                    return;

                var command = new AddStyleSheetCommand(stage.EditedVisualTreeAsset, ussPath);
                command.Execute();
            });

        var selector = rootVisualElement.Q<ObjectField>("stylesheet-selector");
        selector.objectType = typeof(StyleSheet);
        selector.RegisterValueChangedCallback(evt =>
        {
            // [TODO]: Remove this whole if block once the ObjectField properly retrieves only .uss/.tss files.
            // Validate new value first
            if (evt.newValue is StyleSheet newSheet)
            {
                var assetPath = AssetDatabase.GetAssetPath(newSheet);

                // Check if it's a valid standalone StyleSheet asset
                if (string.IsNullOrEmpty(assetPath) || !typeof(StyleSheet).IsAssignableFrom(AssetDatabase.GetMainAssetTypeAtPath(assetPath)))
                {
                    // Reject and revert to old value
                    selector.SetValueWithoutNotify(evt.previousValue as StyleSheet);
                    Debug.LogWarning("Only standalone .uss or .tss StyleSheet files are supported. Inline stylesheets from UXML are not supported.");
                    return;
                }
            }

            // Remove old stylesheet if it exists
            if (evt.previousValue is StyleSheet oldSheet && m_StyleSheetNodes.TryGetValue(oldSheet, out var oldRootNode))
            {
                m_Handler.RemoveStyleSheet(oldRootNode);
                m_StyleSheetNodes.Remove(oldSheet);
            }

            if (evt.newValue is StyleSheet sheet)
            {
                var rootNode = m_Handler.AddStyleSheet(sheet);
                m_StyleSheetNodes[sheet] = rootNode;
            }
        });

        m_Hierarchy = new Hierarchy.Hierarchy();
        m_Handler = m_Hierarchy.GetOrCreateNodeTypeHandler<StyleSheetNodeTypeHandler>();

        m_HierarchyView = new HierarchyView();
        m_HierarchyView.NameColumn.title = "Rules";

        var specificityColumn = new Column
        {
            title = "Specificity",
            bindCell = (element, index) =>
            {
                var label = element as Label;
                var node = m_HierarchyView.ViewModel[index];

                if (m_Handler.Mappings.TryGetNode(node, out var styleNode) && styleNode.Rule != null)
                {
                    label.text = $"({styleNode.Rule.complexSelectors[0].specificity})";
                }
                else
                {
                    label.text = "";
                }
            }
        };

        // Add the column after setting source
        var columns = new List<Column>
        {
            m_HierarchyView.NameColumn,
            specificityColumn
        };

        m_HierarchyView.SetColumns(columns);
        m_HierarchyView.SetSourceHierarchy(m_Hierarchy);

        rootVisualElement.Add(m_HierarchyView);
    }

    void Update()
    {
        if (m_HierarchyView?.UpdateNeeded == true)
            m_HierarchyView.UpdateIncremental();
    }
}
