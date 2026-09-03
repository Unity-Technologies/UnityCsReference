// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal class VisualElementEditingStage : PreviewSceneStage, ISerializationCallbackReceiver
{
    const CommandCategory k_ExternalChangesTrackedCategories = ~CommandCategory.Save;

    private GlobalObjectId m_MainAsset;
    private int[] m_SerializedPath;
    private SubDocumentOptions m_Options;
    private GlobalObjectId m_PanelSettings;
    private Clipboard m_Clipboard;
    private bool m_FrameUpdateRequested;
    private bool m_ExternalChanges;
    private bool m_ExternalSave;
    private bool m_InsideGroup;
    // Set while we drive our own discard reload, so the registry's AssetReloaded event (which we also
    // subscribe to for other tools' changes) does not make us re-clone a second time.
    private bool m_SelfReloading;

    private GUIContent m_HeaderContent;

    private VisualTreeAssetEditingContext m_Context;

    private PanelElement m_PanelElement;
    readonly MatchedRulesExtractor m_RulesExtractor = new (AssetDatabase.GetAssetPath);

    public event Action<VisualElementEditingStage> MainDocumentWasCloned;
    public event Action<PanelElement> PanelWasRepainted;

    public override string assetPath => AssetDatabase.GetAssetPath(EditedVisualTreeAsset);

    internal Panel GetAuthoringPanel() => m_PanelElement?.SubPanel;

    /// <summary>
    /// The live element the edited (sub-)document hangs from: the authoring panel's root, or — when a
    /// sub-document is edited in context — the template instance along
    /// <see cref="VisualTreeAssetEditingContext.SubDocumentPath"/> that is being edited.
    /// </summary>
    /// <remarks>
    /// The stage has no panel component, so this instance is the only thing that tells apart two clones of one
    /// template in the same document: they are cloned from the same assets and differ only in the chain of
    /// instances they hang from. A caller that authors without a live parent of its own — an add with nothing
    /// selected — names this one, so its result is selected where the user is editing rather than in whichever
    /// clone comes first.
    /// </remarks>
    internal VisualElement ResolveLocalRoot()
    {
        var panel = GetAuthoringPanel();
        if (panel == null)
            return null;

        var localRoot = panel is PanelElement.RuntimePanel runtimePanel ? runtimePanel.Root : panel.visualTree;

        var context = Context;
        if (context.SubDocumentOptions is SubDocumentOptions.None or SubDocumentOptions.Isolation)
            return localRoot;

        var subDocumentPath = context.SubDocumentPath;
        if (subDocumentPath == null)
            return localRoot;

        for (var i = 0; i < subDocumentPath.Length && localRoot != null; ++i)
        {
            var template = subDocumentPath[i];
            localRoot = localRoot.Query<TemplateContainer>()
                .Where(tc => (tc.visualElementAsset as TemplateAsset)?.id == template.id)
                .First();
        }

        return localRoot;
    }

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

    internal static VisualElementEditingStage GoToStage(VisualTreeAssetEditingContext context, BreadcrumbBar.SeparatorStyle separatorStyle, bool setAsFirstItemAfterMainStage = false)
    {
        var stage = ScriptableObject.CreateInstance<VisualElementEditingStage>();
        stage.SeparatorStyle = separatorStyle;
        stage.SetContext(context);
        StageUtility.GoToStage(stage, setAsFirstItemAfterMainStage);
        return stage;
    }

    public VisualElementEditingStage()
    {
        m_HeaderContent = new GUIContent();
    }

    internal void RequestRefresh()
    {
        if (m_PanelElement == null)
            return;

        // Process whatever changes we previously add before cloning to ensure everything is up to date.
        PanelElement.FrameUpdate();
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

    internal override void SyncSceneViewToStage(SceneView sceneView)
    {
        // VisualElementEditingStage renders via UIViewportWindow, not the SceneView.
        // Leave the SceneView state unchanged.
    }

    internal override Stage GetContextStage()
    {
        // Share camera state with the main stage so SceneView zoom/offset
        // is not independently saved and restored when entering this stage.
        var history = StageNavigationManager.instance.stageHistory;
        return history.Count > 0 ? history[0] : this;
    }

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
        UIAssetRegistry.instance.RefreshPanel(m_PanelElement?.SubPanel);
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

        // This is temporary fix for domain issues that are very specific to timings.
        // TODO: [MP] Remove once we have the proper reload attributes for managed objects.
        if (StageUtility.GetCurrentStage() == this)
        {
            TrackStagePanel();
            AttachToRegistry();
        }
        UICommandQueue.RegisterHandler<RequestHighlightsCommand>(OnHighlightsRequested);
        UICommandQueue.RegisterHandlerForCategory(CommandCategory.Styling, OnStylingChanged);
        UICommandQueue.GroupBegan += OnGroupBegan;
        UICommandQueue.GroupEnded += OnGroupEnded;
        UICommandQueue.RegisterHandlerForCategory(k_ExternalChangesTrackedCategories, CheckForBuilderChanges);
        UICommandQueue.RegisterHandlerForCategory(CommandCategory.Save, OnBuilderSave);
        UIAssetRegistry.instance.AssetReloaded += OnRegistryAssetReloaded;
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
        UICommandQueue.UnregisterHandler<RequestHighlightsCommand>(OnHighlightsRequested);
        UICommandQueue.UnregisterHandlerForCategory(CommandCategory.Styling, OnStylingChanged);
        UICommandQueue.UnregisterHandlerForCategory(k_ExternalChangesTrackedCategories, CheckForBuilderChanges);
        UICommandQueue.UnregisterHandlerForCategory(CommandCategory.Save, OnBuilderSave);
        UICommandQueue.GroupBegan -= OnGroupBegan;
        UICommandQueue.GroupEnded -= OnGroupEnded;
        UIAssetRegistry.LiveInstance?.AssetReloaded -= OnRegistryAssetReloaded;
    }

    protected internal override bool OnOpenStage()
    {
        m_PanelElement.PanelSettings = Context.PanelSettings;
        TrackStagePanel();
        ReloadAssets();
        AttachToRegistry();
        RequestRefresh();
        return true;
    }

    void BeforeLeavingStage(Stage stage)
    {
        if (stage != this)
            return;

        UntrackStagePanel();
        m_PanelElement.subRootVisualElement.Clear();
    }

    void OnUndoRedoPerformed()
    {
        if (StageUtility.GetCurrentStage() == this)
        {
            foreach (var styleSheet in EditedVisualTreeAsset.GetAllReferencedStyleSheets())
            {
                styleSheet.RequestRebuild(StyleSheet.RebuildOptions.Synchronous);
            }
            UIElementsEditorUtility.ClearStyleCacheAfterUndoIfTracked(default);
            ReloadAssets();
            CloneTree();
        }
    }

    protected override void OnCloseStage()
    {
        UntrackStagePanel();
        DetachFromRegistry();
        m_PanelElement?.DestroyPanelPermanently();
        m_PanelElement = null;
        base.OnCloseStage();
    }

    protected internal override void OnReturnToStage()
    {
        base.OnReturnToStage();
        TrackStagePanel();
        AttachToRegistry();
        ReimportAssets();
        RequestRefresh();
    }

    protected internal override GUIContent CreateHeaderContent()
    {
        if (EditedVisualTreeAsset != null)
            m_HeaderContent.text = EditedVisualTreeAsset.name;

        return m_HeaderContent;
    }

    internal override bool SupportsSaving()
    {
        return true;
    }

    internal override bool hasUnsavedChanges => AnyReferencedAssetDirty();

    private bool AnyReferencedAssetDirty()
    {
        var vta = EditedVisualTreeAsset;
        if (vta == null)
            return false;

        var registry = UIAssetRegistry.instance;
        if (registry.IsDirty(vta))
            return true;

        using var _ = ListPool<StyleSheet>.Get(out var styleSheets);
        UIAssetRegistry.CollectDocumentStyleSheets(vta, styleSheets);
        foreach (var styleSheet in styleSheets)
            if (registry.IsDirty(styleSheet))
                return true;
        return false;
    }

    void AttachToRegistry()
    {
        var panel = m_PanelElement?.SubPanel;
        if (panel != null)
            UIAssetRegistry.instance.AttachPanel(panel, this, CollectStageRoots, ResolveStageAccess);
    }

    void DetachFromRegistry()
    {
        var panel = m_PanelElement?.SubPanel;
        if (panel != null)
            UIAssetRegistry.instance.DetachPanel(panel);
    }

    void CollectStageRoots(List<VisualTreeAsset> roots)
    {
        if (Context.RootVisualTreeAsset != null)
            roots.Add(Context.RootVisualTreeAsset);
        if (EditedVisualTreeAsset != null)
            roots.Add(EditedVisualTreeAsset);
    }

    // The edited document and the stylesheets it references are writable; everything else in the hierarchy
    // (enclosing/nested templates, imported sheets) is tracked read-only.
    UIAssetAccess ResolveStageAccess(UnityEngine.Object asset)
    {
        var edited = EditedVisualTreeAsset;
        if (ReferenceEquals(asset, edited))
            return UIAssetAccess.ReadWrite;

        if (asset is StyleSheet styleSheet && edited != null)
        {
            using var _ = ListPool<StyleSheet>.Get(out var sheets);
            edited.GetAllReferencedStyleSheets(sheets);
            if (sheets.Contains(styleSheet))
                return UIAssetAccess.ReadWrite;
        }
        return UIAssetAccess.ReadOnly;
    }

    void MarkTrackedAssetsClean()
    {
        var vta = EditedVisualTreeAsset;
        if (vta == null)
            return;

        var registry = UIAssetRegistry.instance;
        registry.MarkClean(vta);
        using var _ = ListPool<StyleSheet>.Get(out var styleSheets);
        UIAssetRegistry.CollectDocumentStyleSheets(vta, styleSheets);
        foreach (var styleSheet in styleSheets)
            if (styleSheet != null)
                registry.MarkClean(styleSheet);
    }

    internal override bool Save()
    {
        var succeeded = UIAssetRegistry.instance.SaveAsset(EditedVisualTreeAsset, CommandSources.Stage);
        ReloadAssets();
        CloneTree();
        return succeeded;
    }

    internal override void DiscardChanges()
    {
        m_SelfReloading = true;
        try
        {
            UIAssetRegistry.instance.DiscardAsset(EditedVisualTreeAsset, CommandSources.Stage);
        }
        finally
        {
            m_SelfReloading = false;
        }

        ReloadAssets();
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
            "Discard Changes",
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
            content.tooltip = L10n.Tr("VisualTreeAsset Asset has been deleted.", null);
        }

        return new BreadcrumbBar.Item
        {
            content = content,
            guistyle = style,
            userdata = this,
            separatorstyle = SeparatorStyle
        };
    }

    private void ReloadAssets()
    {
        Context = VisualTreeAssetEditingContext.Reload(Context);
    }

    private void ReimportAssets()
    {
        Context = VisualTreeAssetEditingContext.Reimport(Context);
        // A force reimport brings the assets back to their on-disk state, so re-baseline them clean.
        MarkTrackedAssetsClean();
        ClearUndoForEditedAssets();
    }

    private void ClearUndoForEditedAssets()
    {
        var vta = EditedVisualTreeAsset;
        if (vta == null)
            return;

        Undo.ClearUndo(vta);
        if (vta.inlineSheet != null)
            Undo.ClearUndo(vta.inlineSheet);

        foreach (var styleSheet in vta.GetAllReferencedStyleSheets())
            Undo.ClearUndo(styleSheet);
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

    void TrackStagePanel()
    {
        var panel = m_PanelElement?.SubPanel;
        if (panel != null)
            VisualElementSelectionRegistry.Instance?.TrackStagePanel(this);
    }

    void UntrackStagePanel()
    {
        var panel = m_PanelElement?.SubPanel;
        if (panel != null)
            VisualElementSelectionRegistry.Instance?.UntrackStagePanel(this);
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

        HighlightCommand.Execute(command.Source, elementSet, ruleSet);
    }

    static VisualElement FindElementById(VisualElement root, int veaId)
    {
        return root.Query().Where(e => e.visualElementAsset?.id == veaId).First();
    }

    void OnStylingChanged(in CommandContext context)
    {
        m_FrameUpdateRequested = true;
        if (m_InsideGroup)
            return;

        ProcessDelayedCommands();
    }

    void OnGroupBegan(string undoGroup)
    {
        m_InsideGroup = true;
    }

    void OnGroupEnded(in GroupEndedContext context)
    {
        m_InsideGroup = false;
        ProcessDelayedCommands();
    }

    void ProcessDelayedCommands()
    {
        if (m_FrameUpdateRequested)
        {
            PanelElement.FrameUpdate();
            m_FrameUpdateRequested = false;
        }

        if (m_ExternalChanges)
        {
            ProcessBuilderChanges();
        }
    }

    void CheckForBuilderChanges(in CommandContext context)
    {
        if (context.Status != CommandExecutionStatus.Success || context.Source != CommandSources.Builder)
            return;

        m_ExternalChanges = true;
        if (m_InsideGroup)
            return;

        ProcessBuilderChanges();
    }

    void ProcessBuilderChanges()
    {
        // Here, we take for granted that the UI Builder made a change. Go nuclear.
        EditorApplication.delayCall += DoProcessBuilderChanges;
    }

    void DoProcessBuilderChanges()
    {
        if (!m_ExternalChanges)
            return;
        RequestRefresh();
        m_ExternalChanges = false;
    }

    void OnBuilderSave(in CommandContext context)
    {
        if (context.Status != CommandExecutionStatus.Success || context.Source != CommandSources.Builder)
            return;

        if (context.Command is not PostSaveCommand postSaveCommand)
            return;

        // A save that failed to write leaves the document unsaved, so adopting it as clean would drop the "*"
        // while the edits are still only in memory.
        if (!postSaveCommand.Succeeded)
            return;

        // Ignore saves of documents we are not editing so a save of an unrelated document never discards our
        // own in-memory edits.
        if (postSaveCommand.Context.EditedVisualTreeAsset != EditedVisualTreeAsset)
            return;

        m_ExternalSave = true;
        // Defer so the reload runs after the Builder's save has fully settled.
        EditorApplication.delayCall += DoProcessBuilderSave;
    }

    void DoProcessBuilderSave()
    {
        if (!m_ExternalSave)
            return;
        m_ExternalSave = false;

        AdoptReloadedContext(markClean: true);
    }

    void OnRegistryAssetReloaded(UnityEngine.Object asset)
    {
        if (m_SelfReloading || m_PanelElement == null || !IsPartOfEditedDocument(asset))
            return;

        AdoptReloadedContext(markClean: false);
    }

    bool IsPartOfEditedDocument(UnityEngine.Object asset)
    {
        if (asset == null)
            return false;

        // Fast path: the common reimport reuses the managed instance, so a reference match settles it.
        if (ReferenceEquals(asset, EditedVisualTreeAsset) || ReferenceEquals(asset, Context.RootVisualTreeAsset))
            return true;

        var path = AssetDatabase.GetAssetPath(asset);
        if (string.IsNullOrEmpty(path))
            return false;

        var root = (VisualTreeAsset)GlobalObjectId.GlobalObjectIdentifierToObjectSlow(m_MainAsset);
        if (root != null && AssetDatabase.GetAssetPath(root) == path)
            return true;

        var edited = EditedVisualTreeAsset;
        if (edited == null)
            return false;
        if (AssetDatabase.GetAssetPath(edited) == path)
            return true;

        using var _ = ListPool<StyleSheet>.Get(out var sheets);
        edited.GetAllReferencedStyleSheets(sheets);
        foreach (var sheet in sheets)
            if (sheet != null && AssetDatabase.GetAssetPath(sheet) == path)
                return true;
        return false;
    }

    void AdoptReloadedContext(bool markClean)
    {
        if (m_PanelElement == null)
            return;

        var freshRoot = (VisualTreeAsset)GlobalObjectId.GlobalObjectIdentifierToObjectSlow(m_MainAsset);
        if (freshRoot && (Context.SubDocumentPath == null || Context.SubDocumentPath.Length == 0))
            Context = new VisualTreeAssetEditingContext(freshRoot, Context.PanelSettings);
        else
            ReloadAssets();

        if (markClean)
            MarkTrackedAssetsClean();
        CloneTree();
        ClearUndoForEditedAssets();
    }

    internal override string GetErrorMessage()
    {
        return "The UI document being edited is no longer valid.\n\nReturning to the main stage.";
    }
}
