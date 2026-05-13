// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal class VisualElementEditingStage : PreviewSceneStage, ISerializationCallbackReceiver
{
    private GlobalObjectId m_MainAsset;
    private int[] m_SerializedPath;
    private SubDocumentOptions m_Options;
    private GlobalObjectId m_PanelSettings;
    private Clipboard m_Clipboard;

    private GUIContent m_HeaderContent;

    private ScopedMenuItemGenerator m_MenuScope;

    private VisualTreeAssetEditingContext m_Context;

    private PanelElement m_PanelElement;
    readonly MatchedRulesExtractor m_RulesExtractor = new (AssetDatabase.GetAssetPath);

    public event Action<VisualElementEditingStage> MainDocumentWasCloned;
    public event Action<PanelElement> PanelWasRepainted;

    public override string assetPath => AssetDatabase.GetAssetPath(EditedVisualTreeAsset);

    internal Panel GetAuthoringPanel() => m_PanelElement.SubPanel;

    internal override bool isValid => ValidateContext();

    internal PanelElement PanelElement => m_PanelElement;

    public VisualTreeAssetEditingContext Context
    {
        get => m_Context;
        private set
        {
            if (m_Context == value)
                return;

            m_Context = value;
            if (m_Context.SubDocumentOptions != SubDocumentOptions.None)
            {
                var template = m_Context.SubDocumentPath[^1];
                EditedVisualTreeAsset = template.ResolveTemplate();
            }
            else
            {
                EditedVisualTreeAsset = m_Context.RootVisualTreeAsset;
            }
        }
    }

    public BreadcrumbBar.SeparatorStyle SeparatorStyle { get; set; }

    public VisualTreeAsset EditedVisualTreeAsset { get; private set; }

    public Clipboard Clipboard => m_Clipboard;

    public void SetContext(VisualTreeAssetEditingContext context)
    {
        Context = context;
        m_HeaderContent.text = EditedVisualTreeAsset.name;
        m_HeaderContent.image = EditorGUIUtility.Load("VisualTreeAsset Icon") as Texture2D;
    }

    public VisualElementEditingStage()
    {
        m_HeaderContent = new GUIContent();
    }

    public void RequestRefresh()
    {
        CloneTree();
        PanelElement.FrameUpdate();
    }

    public void RequestCanvasSize(Vector2 viewportSize, Vector2 canvasSize, Vector2 offset, float zoomFactor)
    {
        if (m_PanelElement == null)
            return;

        m_PanelElement.ResizeRenderTexture(viewportSize);
        m_PanelElement.Offset = offset;
        m_PanelElement.ScaleFactor = zoomFactor;
        m_PanelElement.Size = canvasSize;

        if (viewportSize.x == 0 || viewportSize.y == 0)
            return;

        m_PanelElement.FrameUpdate();
    }

    internal override Scene GetSceneAt(int index)
    {
        // Don't want no scene.
        return default;
    }

    internal override ulong GetSceneCullingMask() { return 0; }

    private void CloneTree()
    {
        m_PanelElement.subRootVisualElement.Clear();

        switch (Context.SubDocumentOptions)
        {
            case SubDocumentOptions.None:
                Context.RootVisualTreeAsset.CloneTree(m_PanelElement.subRootVisualElement);
                break;
            case SubDocumentOptions.InContext:
                Context.RootVisualTreeAsset.CloneTree(m_PanelElement.subRootVisualElement);
                break;
            case SubDocumentOptions.Isolation:
                EditedVisualTreeAsset.CloneTree(m_PanelElement.subRootVisualElement);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        MainDocumentWasCloned?.Invoke(this);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        m_PanelElement = new PanelElement();
        m_PanelElement.OnAfterRepaint += OnPanelRepainted;
        m_PanelElement.CreateSubPanel();
        Binding.SetPanelLogLevel(m_PanelElement.SubPanel, BindingLogLevel.None);
        m_PanelElement.SetPanelSize(new Vector2Int(480, 640));
        DoDeserialize();
        StageNavigationManager.instance.beforeSwitchingAwayFromStage += BeforeLeavingStage;
        Undo.undoRedoPerformed += OnUndoRedoPerformed;
        m_Clipboard = new Clipboard();
        m_MenuScope = new ScopedMenuItemGenerator();

        // This is temporary fix for domain issues that are very specific to timings.
        // TODO: [MP] Remove once we have the proper reload attributes for managed objects.
        HierarchyWindow.RegisterNodeTypeHandler<VisualElementEditingNodeHandler>();
        UICommandQueue.RegisterHandler<RequestHighlightsCommand>(OnHighlightsRequested);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        StageNavigationManager.instance.beforeSwitchingAwayFromStage -= BeforeLeavingStage;
        m_PanelElement.subRootVisualElement.Clear();
        m_PanelElement.DestroySubPanel();
        m_PanelElement.OnAfterRepaint -= OnPanelRepainted;
        m_Clipboard.Dispose();
        m_Clipboard = null;
        m_MenuScope?.Dispose();
        m_MenuScope = null;
        UICommandQueue.UnregisterHandler<RequestHighlightsCommand>(OnHighlightsRequested);
    }

    protected internal override bool OnOpenStage()
    {
        m_PanelElement.PanelSettings = Context.PanelSettings;
        RequestRefresh();
        m_MenuScope ??= new ScopedMenuItemGenerator();
        return true;
    }

    void BeforeLeavingStage(Stage stage)
    {
        if (stage != this)
            return;

        m_PanelElement.subRootVisualElement.Clear();
    }

    void OnUndoRedoPerformed()
    {
        if (StageUtility.GetCurrentStage() == this)
            CloneTree();
    }

    protected override void OnCloseStage()
    {
        m_PanelElement?.DestroyPanelPermanently();
        m_PanelElement = null;
        m_MenuScope?.Dispose();
        m_MenuScope = null;
        base.OnCloseStage();
    }

    protected internal override void OnReturnToStage()
    {
        base.OnReturnToStage();
        ReimportAssets();
        CloneTree();
    }

    protected internal override GUIContent CreateHeaderContent()
    {
        return m_HeaderContent;
    }

    internal override bool SupportsSaving()
    {
        return true;
    }

    internal override bool hasUnsavedChanges => AnyReferencedAssetDirty();

    private bool AnyReferencedAssetDirty()
    {
        if (EditorUtility.IsDirty(EditedVisualTreeAsset))
            return true;
        var styleSheets = EditedVisualTreeAsset.GetAllReferencedStyleSheets();
        foreach(var styleSheet in styleSheets)
            if (EditorUtility.IsDirty(styleSheet))
                return true;
        return false;
    }

    internal override bool Save()
    {
        var succeeded = true;
        using (new AssetDatabase.AssetEditingScope())
        {

            var styleSheets = EditedVisualTreeAsset.GetAllReferencedStyleSheets();
            foreach (var styleSheet in styleSheets)
            {
                if (EditorUtility.IsDirty(styleSheet))
                {
                    var styleSheetPath = AssetDatabase.GetAssetPath(styleSheet);
                    if (string.IsNullOrEmpty(styleSheetPath))
                        // [TODO] Figure out Save as...
                        continue;
                    var styleSheetStr = StyleSheetExporter.Default.ToUssString(styleSheet);
                    succeeded &= WriteTextFileToDisk(styleSheetPath, styleSheetStr);
                    AssetDatabase.ImportAsset(styleSheetPath);
                }
            }

            if (string.IsNullOrEmpty(assetPath))
            {
                // [TODO] Figure out Save as...
                return false;
            }

            if (EditorUtility.IsDirty(EditedVisualTreeAsset))
            {
                VisualTreeAsset.HarmonizeIds(EditedVisualTreeAsset);
                var assetStr = VisualTreeAssetExporter.Default.ToUxmlString(EditedVisualTreeAsset);
                succeeded &= WriteTextFileToDisk(assetPath, assetStr);
                AssetDatabase.ImportAsset(assetPath);
            }
        }

        ReloadAssets();
        CloneTree();

        return succeeded;
    }

    internal override void DiscardChanges()
    {
        ReimportAssets();
        CloneTree();
    }

    internal bool AskUserToSaveModifiedStage()
    {
        return AskUserToSaveModifiedStageBeforeSwitchingStage();
    }

    internal override bool AskUserToSaveModifiedStageBeforeSwitchingStage()
    {
        if (!hasUnsavedChanges)
            return true;

        var result = EditorDialog.DisplayComplexDecisionDialog(
            "UI Stage - Unsaved Changes Detected",
            "Do you want to save changes you made?",
            "Save",
            "Discard",
            "Cancel",
            DialogIconType.Info
            );
        switch (result)
        {
            case DialogResult.Cancel:
                return false;
            case DialogResult.DefaultAction:
                if (Save())
                    return true;
                // TODO: Display error message.
                break;
            case DialogResult.AlternateAction:
                DiscardChanges();
                return true;
            default:
                throw new ArgumentOutOfRangeException();
        }

        // We'll see what happens here.
        return false;
    }

    internal override BreadcrumbBar.Item CreateBreadcrumbItem()
    {
        GUIContent content = CreateHeaderContent();

        var history = StageNavigationManager.instance.stageHistory;
        bool isLastCrumb = this == history[^1];
        var style = isLastCrumb ? BreadcrumbBar.DefaultStyles.labelBold : BreadcrumbBar.DefaultStyles.label;
        if (isAssetMissing)
        {
            style = isLastCrumb ? BreadcrumbBar.DefaultStyles.labelBoldMissing : BreadcrumbBar.DefaultStyles.labelMissing;
            content.tooltip = L10n.Tr("VisualTreeAsset Asset has been deleted.");
        }

        return new BreadcrumbBar.Item
        {
            content = content,
            guistyle = style,
            userdata = this,
            separatorstyle = SeparatorStyle
        };
    }

    private bool WriteTextFileToDisk(string path, string content)
    {
        // Make sure the folders exist.
        var folder = Path.GetDirectoryName(path);
        if (folder != null && !Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var success = FileUtil.WriteTextFileToDisk(path, content, out var message);

        if (!success)
            Debug.LogError(message);
        return success;
    }

    private void ReloadAssets()
    {
        Context = VisualTreeAssetEditingContext.Reload(Context);
    }

    private void ReimportAssets()
    {
        Context = VisualTreeAssetEditingContext.Reimport(Context);
    }

    protected internal override Hash128 GetHashForStateStorage()
    {
        switch (m_Context.SubDocumentOptions)
        {
            // When editing the root VisualTreeAsset or editing a VisualTreeAsset in isolation,
            // use the hash for the edited VisualTreeAsset
            case SubDocumentOptions.None:
            case SubDocumentOptions.Isolation:
                return base.GetHashForStateStorage();
            // When editing a VisualTreeAsset in context, use a hash calculated from the all
            // the VisualTreeAssets from the root to the edited one.
            case SubDocumentOptions.InContext:
            {
                using var _ = StringBuilderPool.Get(out var sb);
                sb.Append(AssetDatabase.GetAssetPath(m_Context.RootVisualTreeAsset));

                for (var i = 0; i < m_Context.SubDocumentPath.Length; ++i)
                {
                    sb.Append('|');
                    var templateAsset = m_Context.SubDocumentPath[i];
                    var vta = templateAsset.ResolveTemplate();
                    sb.Append(AssetDatabase.GetAssetPath(vta));
                    sb.Append(templateAsset.id);
                }
                return Hash128.Compute(sb.ToString());
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void OnBeforeSerialize()
    {
        m_MainAsset = GlobalObjectId.GetGlobalObjectIdSlow(m_Context.RootVisualTreeAsset);

        if (m_Context.SubDocumentPath != null)
        {
            m_SerializedPath = new int[m_Context.SubDocumentPath.Length];
            for (var i = 0; i < m_Context.SubDocumentPath.Length; ++i)
                m_SerializedPath[i] = m_Context.SubDocumentPath[i].id;
        }
        else
        {
            m_SerializedPath = null;
        }

        m_PanelSettings = GlobalObjectId.GetGlobalObjectIdSlow(m_Context.PanelSettings);
        m_Options = m_Context.SubDocumentOptions;
    }

    public void OnAfterDeserialize()
    {
        // Sadly, here, we can't deserialize GlobalObjectIds.
    }

    public void DoDeserialize()
    {
        var main = (VisualTreeAsset)GlobalObjectId.GlobalObjectIdentifierToObjectSlow(m_MainAsset);
        if (!main)
            return;

        var path = new TemplateAsset[m_SerializedPath?.Length ?? 0];
        var vta = main;
        for (var i = 0; i < m_SerializedPath?.Length; ++i)
        {
            var templates = vta.DepthFirstTraversalOfType<TemplateAsset>();
            var found = false;
            foreach (var template in templates)
            {
                if (template.id == m_SerializedPath[i])
                {
                    path[i] = template;
                    vta = template.ResolveTemplate();
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                break;
            }
        }
        var options = m_Options;
        var settings = (PanelSettings)GlobalObjectId.GlobalObjectIdentifierToObjectSlow(m_PanelSettings);
        Context = new VisualTreeAssetEditingContext(main, path, options, settings);
        m_PanelElement.PanelSettings = Context.PanelSettings;
        CloneTree();
    }

    private bool ValidateContext()
    {
        if (!Context.RootVisualTreeAsset)
            return false;

        if (Context.SubDocumentPath != null)
        {
            for (var i = Context.SubDocumentPath.Length - 1; i >= 1; --i)
            {
                var template = Context.SubDocumentPath[i];
                if (template.visualTreeAsset != Context.SubDocumentPath[i - 1].ResolveTemplate())
                    return false;
            }
        }
        return true;
    }

    void OnPanelRepainted(PanelElement panel)
    {
        PanelWasRepainted?.Invoke(panel);
    }

    internal void ContentOverflowMode(Overflow overflow)
    {
        if (m_PanelElement != null)
            m_PanelElement.ContentOverflowMode = overflow;
    }

    void OnHighlightsRequested(in CommandContext context)
    {
        if (context.Status != CommandExecutionStatus.Success)
            return;

        if (context.Command is not RequestHighlightsCommand command)
            return;

        using var elementSetHandle = HashSetPool<VisualElement>.Get(out var elementSet);
        using var ruleSetHandle = HashSetPool<StyleRule>.Get(out var ruleSet);

        if (command.Element != null)
        {
            elementSet.Add(command.Element);
            m_RulesExtractor.FindMatchingRules(command.Element);
            foreach (var matchRecord in m_RulesExtractor.matchRecords)
            {
                var rule = matchRecord.complexSelector.rule;
                if (rule != null)
                    ruleSet.Add(rule);
            }
            m_RulesExtractor.Clear();
        }

        if (command.ElementId.HasValue)
        {
            var element = FindElementById(m_PanelElement.subRootVisualElement, command.ElementId.Value);
            if (element != null && elementSet.Add(element))
            {
                m_RulesExtractor.FindMatchingRules(element);
                foreach (var matchRecord in m_RulesExtractor.matchRecords)
                {
                    var rule = matchRecord.complexSelector.rule;
                    if (rule != null)
                        ruleSet.Add(rule);
                }

                m_RulesExtractor.Clear();
            }
        }

        if (command.Rule != null)
        {
            ruleSet.Add(command.Rule);
            foreach(var selector in command.Rule.complexSelectors)
                HighlightUtility.GetMatchingElementsForSelector(m_PanelElement.SubPanel.visualTree, selector, elementSet);
        }

        using (var highlightCommand = HighlightCommand.GetPooled(elementSet, ruleSet))
        {
            highlightCommand.Source = command.Source;
            UICommandQueue.EnqueueCommand(highlightCommand);
        }
    }

    static VisualElement FindElementById(VisualElement root, int veaId)
    {
        return root.Query().Where(e => e.visualElementAsset?.id == veaId).First();
    }
}
