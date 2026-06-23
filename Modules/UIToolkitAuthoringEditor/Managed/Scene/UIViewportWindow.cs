// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Hierarchy.Editor;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[Icon("UIToolkit/Icons/UIViewportWindow.png")]
partial class UIViewportWindow : EditorWindow
{
    class ShortcutContext : IShortcutContext
    {
        public bool active =>
            (focusedWindow is UIViewportWindow or HierarchyWindow) &&
            s_OpenWindows.Count > 0 &&
            StageUtility.GetCurrentStage() is VisualElementEditingStage;
    }

    static readonly List<UIViewportWindow> s_OpenWindows = new();
    static UIViewportWindow s_LastFocusedWindow;
    static ShortcutContext s_ShortcutContext;

    const string k_MenuPath = "Window/UI Toolkit/UI Viewport";
    const int k_MenuPriority = 3019;

    const string k_VisualTreeAsset = "UIToolkitAuthoring/UIViewportWindow/UIViewportWindow.uxml";
    const string k_StyleSheetDark = "UIToolkitAuthoring/UIViewportWindow/UIViewportWindowDark.uss";
    const string k_StyleSheetLight = "UIToolkitAuthoring/UIViewportWindow/UIViewportWindowLight.uss";

    const string HiddenPostFix = "--hidden";

    public const string UssClass = "unity-ui-viewport";

    public const string EnterStageModeWarningContainerUssClass = UssClass + "__container__enter-stage-mode-warning";
    const string HiddenEnterStageModeWarningContainerUssClass = EnterStageModeWarningContainerUssClass + HiddenPostFix;
    public const string ViewportWrapperContainerUssClass = UssClass + "__container__viewport-wrapper";
    const string HiddenViewportWrapperContainerUssClass = ViewportWrapperContainerUssClass + HiddenPostFix;

    const string CanvasUssClass = UssClass + "__canvas";
    const string ViewportUssClass = UssClass + "__viewport";

    VisualElement m_EnterStageModeOverlay;
    VisualElement m_ViewportOverlay;
    Button m_OpenSettingsButton;

    [OnCodeLoaded]
    static void Initialize()
    {
        s_ShortcutContext = new ShortcutContext();
        EditorApplication.delayCall += () => ShortcutIntegration.instance.contextManager.RegisterToolContext(s_ShortcutContext);
    }

    [MenuItem(k_MenuPath, false, 3010, secondaryPriority = 3)]
    static void ShowWindow()
    {
        GetWindow<UIViewportWindow>();
    }

    [Shortcut("UI Viewport/Fit Viewport", typeof(ShortcutContext), KeyCode.F)]
    static void OnFitViewportShortcut(ShortcutArguments args)
    {
        var window = s_LastFocusedWindow ?? (s_OpenWindows.Count > 0 ? s_OpenWindows[0] : null);
        window?.m_Viewport?.FitViewport();
    }

    [NonSerialized]
    EntityId m_StageId;
    UICanvas m_Canvas;
    UIViewport m_Viewport;

    PreviewThemeState m_ThemeState;
    UxmlCodePreview m_UxmlPreview;
    UssCodePreview m_UssPreview;

    public EntityId StageId
    {
        get => m_StageId;
        private set
        {
            if (m_StageId == value)
                return;

            ReleaseStage(m_StageId);
            m_StageId = value;
            AcquireStage(m_StageId);
        }
    }

    void OnEnable()
    {
        titleContent.text = "UI Viewport";
        titleContent.image = UIResources.GetIconForType(typeof(UIViewportWindow), UIResources.RequestSize.Px16, GetPixelsPerPoint(rootVisualElement)).texture;
        StageNavigationManager.instance.afterSuccessfullySwitchedToStage += OnStageChanged;
        UIToolkitAuthoringSettings.EnableInSceneAuthoringChanged += OnEnableInSceneAuthoringChanged;
        s_OpenWindows.Add(this);
    }

    void OnDisable()
    {
        StageNavigationManager.instance.afterSuccessfullySwitchedToStage -= OnStageChanged;
        UIToolkitAuthoringSettings.EnableInSceneAuthoringChanged -= OnEnableInSceneAuthoringChanged;
        s_OpenWindows.Remove(this);
        if (s_LastFocusedWindow == this)
            s_LastFocusedWindow = null;
    }

    void OnFocus()
    {
        s_LastFocusedWindow = this;
    }

    [UsedImplicitly]
    void CreateGUI()
    {
        titleContent.image = UIResources.GetIconForType(typeof(UIViewportWindow), UIResources.RequestSize.Px16, GetPixelsPerPoint(rootVisualElement)).texture;

        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        if (vta)
            vta.CloneTree(rootVisualElement);

        var styleSheetPath = EditorGUIUtility.isProSkin ? k_StyleSheetDark : k_StyleSheetLight;
        var styleSheet = EditorGUIUtility.Load(styleSheetPath) as StyleSheet;
        if (styleSheet)
            rootVisualElement.styleSheets.Add(styleSheet);

        m_EnterStageModeOverlay = rootVisualElement.Q(className: EnterStageModeWarningContainerUssClass);
        m_ViewportOverlay = rootVisualElement.Q(className: ViewportWrapperContainerUssClass);
        m_OpenSettingsButton = rootVisualElement.Q<Button>("unity-ui-viewport__open-settings-button");
        m_OpenSettingsButton.clicked += UIToolkitAuthoringSettingsProvider.OpenSettings;
        UpdateOpenSettingsButton();
        m_Canvas = rootVisualElement.Q<UICanvas>(CanvasUssClass);
        m_Viewport = rootVisualElement.Q<UIViewport>(ViewportUssClass);
        m_UxmlPreview = rootVisualElement.Q<UxmlCodePreview>();
        m_UssPreview = rootVisualElement.Q<UssCodePreview>();

        rootVisualElement.RegisterCallback<CanvasManipulatorMessageEvent>(OnCanvasManipulatorMessage);

        OnStageChanged(StageUtility.GetCurrentStage());
    }

    void OnCanvasManipulatorMessage(CanvasManipulatorMessageEvent e) =>
        ShowNotification(new GUIContent(e.Message, EditorGUIUtility.FindTexture("console.warnicon")), 4);

    void OnDestroy()
    {
        ReleaseStage(StageId);
    }

    void OnStageChanged(Stage stage)
    {
        if (m_Canvas == null)
            return;

        var uiStage = stage as VisualElementEditingStage;

        var isUIStage = uiStage != null;
        m_EnterStageModeOverlay.EnableInClassList(HiddenEnterStageModeWarningContainerUssClass, isUIStage);
        m_ViewportOverlay.EnableInClassList(HiddenViewportWrapperContainerUssClass, !isUIStage);

        StageId = stage.GetEntityId();

        if (isUIStage)
            SetToolbarBreadcrumbs();
        else
            m_Viewport.ClearBreadcrumbs();
    }

    void OnEnableInSceneAuthoringChanged(bool enabled)
    {
        UpdateOpenSettingsButton();
    }

    void UpdateOpenSettingsButton()
    {
        if (m_OpenSettingsButton == null)
            return;

        m_OpenSettingsButton.style.display = UIToolkitAuthoringSettings.EnableInSceneUIAuthoring
            ? DisplayStyle.None
            : DisplayStyle.Flex;
    }

    void SetToolbarBreadcrumbs()
    {
        if (m_Viewport == null)
            return;

        m_Viewport.ClearBreadcrumbs();

        var history = StageNavigationManager.instance.stageHistory;
        for (var i = 0; i < history.Count; i++)
        {
            var stage = history[i];
            var content = stage.CreateHeaderContent();
            var icon = content.image as Texture2D;
            var label = content.text;

            var isCurrentStage = i == history.Count - 1;
            if (isCurrentStage)
            {
                m_Viewport.PushBreadcrumb(label, icon);
            }
            else
            {
                m_Viewport.PushBreadcrumb(label, icon, () => StageUtility.GoToStage(stage, false));
            }
        }
    }

    void AcquireStage(EntityId stageId)
    {
        var stage = EditorUtility.EntityIdToObject(stageId) as VisualElementEditingStage;
        if (!stage)
            return;

        m_Canvas.HeaderTitle = stage.EditedVisualTreeAsset.name + ".uxml";

        var hash = stage.GetHashForStateStorage();
        var storageKey = $"CanvasSettings-{hash}";

        m_Canvas.SetContext(stage.PanelElement, storageKey);

        m_ThemeState = PreviewThemeState.ForDocument(stage.Context.RootVisualTreeAsset);
        SetupThemeMenu(stage.Context.PanelSettings, m_ThemeState.SelectedTheme);

        m_Viewport.DropManipulator.EditedVisualTreeAsset = stage.EditedVisualTreeAsset;
        m_Viewport.DropManipulator.RequestRefresh = stage.RequestRefresh;
        m_Viewport.DropManipulator.WouldCauseCircularDependency = stage.Context.WillCauseCircularDependency;

        m_UxmlPreview.Asset = stage.EditedVisualTreeAsset;
        m_UssPreview.Asset = GetActiveStyleSheetQuery.Get() ?? stage.EditedVisualTreeAsset.GetAllReferencedStyleSheets().FirstOrDefault();
        UICommandQueue.RegisterHandler<ActiveStyleSheetChangedMessage>(ActiveStyleSheetChanged);
    }

    void SetupThemeMenu(PanelSettings panelSettings, ThemeStyleSheet selectedTheme)
    {
        m_Viewport.ThemeMenu.ThemeSelected += OnThemeMenuThemeSelected;
        m_Viewport.ThemeMenu.SelectedTheme = selectedTheme;
        m_Viewport.ThemeMenu.PanelSettings = panelSettings;

        if (m_Canvas.PanelElement != null)
            m_Canvas.PanelElement.ThemeStyleSheet = selectedTheme;
    }

    void OnThemeMenuThemeSelected(ThemeStyleSheet theme)
    {
        SetPreviewThemeCommand.Execute(CommandSources.Viewport, m_ThemeState, theme);

        if (m_Canvas.PanelElement != null)
            m_Canvas.PanelElement.ThemeStyleSheet = theme;
    }

    internal void ClearThemeMenu()
    {
        if (m_Viewport?.ThemeMenu != null)
        {
            m_Viewport.ThemeMenu.ThemeSelected -= OnThemeMenuThemeSelected;
            m_Viewport.ThemeMenu.ClearItems();
        }

        if (m_Canvas?.PanelElement != null)
            m_Canvas.PanelElement.ThemeStyleSheet = null;
    }

    void ReleaseStage(EntityId stageId)
    {
        m_Canvas.DestroySettingsPermanently();
        ClearThemeMenu();

        m_Viewport.DropManipulator.EditedVisualTreeAsset = null;
        m_Viewport.DropManipulator.RequestRefresh = null;
        m_Viewport.DropManipulator.WouldCauseCircularDependency = null;

        m_UxmlPreview.Asset = null;
        m_UssPreview.Asset = null;
        UICommandQueue.UnregisterHandler<ActiveStyleSheetChangedMessage>(ActiveStyleSheetChanged);
    }

    static float GetPixelsPerPoint(VisualElement element)
    {
        return element?.panel == null
            ? EditorGUIUtility.pixelsPerPoint : element.scaledPixelsPerPoint;
    }

    void ActiveStyleSheetChanged(in CommandContext context)
    {
        m_UssPreview.Asset = ((ActiveStyleSheetChangedMessage)context.Command).StyleSheet;
    }
}
