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
    public const string ViewporWrapperContainerUssClass = UssClass + "__container__viewport-wrapper";
    const string HiddenViewportWrapperContainerUssClass = ViewporWrapperContainerUssClass + HiddenPostFix;

    const string CanvasUssClass = UssClass + "__canvas";
    const string ViewportUssClass = UssClass + "__viewport";

    VisualElement m_EnterStageModeOverlay;
    VisualElement m_ViewportOverlay;

    [OnCodeLoaded]
    static void Initialize()
    {
        UIToolkitAuthoringSettings.UIStagesChanged += OnUIStagesChanged;
        EditorApplication.delayCall += () => OnUIStagesChanged(UIToolkitAuthoringSettings.EnableUIStages);
        s_ShortcutContext = new ShortcutContext();
        EditorApplication.delayCall += () => ShortcutIntegration.instance.contextManager.RegisterToolContext(s_ShortcutContext);
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
        s_OpenWindows.Add(this);
    }

    void OnDisable()
    {
        StageNavigationManager.instance.afterSuccessfullySwitchedToStage -= OnStageChanged;
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
        m_ViewportOverlay = rootVisualElement.Q(className: ViewporWrapperContainerUssClass);
        m_Canvas = rootVisualElement.Q<UICanvas>(CanvasUssClass);
        m_Viewport = rootVisualElement.Q<UIViewport>(ViewportUssClass);
        m_UxmlPreview = rootVisualElement.Q<UxmlCodePreview>();
        m_UssPreview = rootVisualElement.Q<UssCodePreview>();

        OnStageChanged(StageUtility.GetCurrentStage());
    }

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

        m_UxmlPreview.Asset = stage.EditedVisualTreeAsset;
        m_UssPreview.Asset = stage.EditedVisualTreeAsset.GetAllReferencedStyleSheets().FirstOrDefault();
    }

    void ReleaseStage(EntityId stageId)
    {
        m_Canvas.DestroySettingsPermanently();

        m_UxmlPreview.Asset = null;
        m_UssPreview.Asset = null;
    }

    static float GetPixelsPerPoint(VisualElement element)
    {
        return element?.panel == null
            ? EditorGUIUtility.pixelsPerPoint : element.scaledPixelsPerPoint;
    }
}
