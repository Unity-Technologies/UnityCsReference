// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Scripting;
using UnityEditorInternal;
using UnityEditor.Overlays;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEditor.Toolbars;
using UnityEditor.UIElements;
using System;
using System.Reflection;
using System.IO;
using System.ComponentModel;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor
{
    sealed partial class MainToolbarWindow : EditorWindow, ISupportsOverlaysCustomMode
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal sealed class TestScope
        {
            MainToolbarWindow m_Instance;

            public TestScope(MainToolbarWindow instance)
            {
                m_Instance = instance;
            }

            public void PopulateFullMenu(AbstractGenericMenu menu)
            {
                m_Instance.PopulateFullMenu(menu);
            }

            public void PopulateMenuWithOverlays(AbstractGenericMenu menu, bool includeUtilityFunctions)
            {
                m_Instance.PopulateMenuWithOverlays(menu, includeUtilityFunctions);
            }

            public void UpdateClutchInput(Event evt)
            {
                m_Instance.m_EditModeState.UpdateClutchInput(evt);
            }

            public bool editModeActive => m_Instance.m_EditModeState.active;
        }

        sealed class EditMode
        {
            public bool active => m_CurrentState != MainToolbarEditMode.Inactive;

            MainToolbarEditMode m_CurrentState;

            public bool userEnabled { get; set; }

            bool m_ClutchActive = false;
            OverlayCanvas m_Canvas;

            public EditMode(EditorWindow owner)
            {
                m_Canvas = owner.overlayCanvas;
            }

            public void UpdateClutchInput(Event evt)
            {
                if (Application.platform == RuntimePlatform.OSXEditor ||
                    Application.platform == RuntimePlatform.OSXPlayer)
                {
                    m_ClutchActive = evt.command;
                }
                else
                {
                    m_ClutchActive = evt.control;
                }

                Update();
            }

            void Update()
            {
                MainToolbarEditMode oldState = m_CurrentState;
                m_CurrentState = MainToolbarEditMode.Inactive;
                if (userEnabled)
                    m_CurrentState = MainToolbarEditMode.Active;
                else if (m_ClutchActive)
                    m_CurrentState = MainToolbarEditMode.TempActivation;

                if (oldState != m_CurrentState)
                {
                    m_Canvas.rootVisualElement.EnableInClassList(k_MainToolbarEditModeClassName, m_CurrentState == MainToolbarEditMode.Active);
                    m_Canvas.rootVisualElement.EnableInClassList(k_MainToolbarTempEditModeClassName, m_CurrentState == MainToolbarEditMode.TempActivation);
                    foreach (var overlay in m_Canvas.overlays)
                    {
                        var mto = overlay as MainToolbarOverlay;
                        mto?.SetEditMode(m_CurrentState);
                    }
                }
            }
        }

        const string k_MainToolbarUSSClassName = "unity-editor-main-toolbar";
        const string k_MainToolbarEditModeClassName = k_MainToolbarUSSClassName + "--edit-mode";
        const string k_MainToolbarTempEditModeClassName = k_MainToolbarUSSClassName + "--temp-edit-mode";
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static readonly string editModeName = L10n.Tr("Edit Mode", null);
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static readonly string menuItemSearchName = L10n.Tr("Menu Items", null);
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static readonly string showAllName = L10n.Tr("Show All", null);
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static readonly string mainToolbarElementSearchName = L10n.Tr("Toolbar Elements", null);
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static readonly string hideAllName = L10n.Tr("Hide All", null);

        [AutoStaticsCleanupOnCodeReload]
        // Points at the live main toolbar window: Toolbar.OnEnable creates it again after a code reload and
        // the MainToolbarWindow constructor re-assigns the slot.
        [IgnoreForUAL0015("Window instance slot re-assigned by the constructor when Toolbar.OnEnable recreates it")]
        internal static MainToolbarWindow instance;

        MainToolbarAnalytics m_Analytics;

        OverlayCanvasMode ISupportsOverlaysCustomMode.overlayCanvasMode => OverlayCanvasMode.MainToolbar;

        string[] m_UniqueMenuCategories;

        MainToolbarWindow()
        {
            #pragma warning disable UAL0015 // rebuilt/resubscribed wholesale on the next reload via this object's own lifecycle; a stale value in the interim is never observed
            instance = this;
            #pragma warning restore UAL0015
        }

        // UUM-116278: re-normalizes '\\' to '/' on every iteration, since Path.GetDirectoryName reintroduces it each call.
        static void AddAncestorPaths(string leafPath, HashSet<string> paths)
        {
            var path = Path.GetDirectoryName(leafPath)?.Replace(Path.DirectorySeparatorChar, '/');
            while (!string.IsNullOrEmpty(path))
            {
                paths.Add(path);
                path = Path.GetDirectoryName(path)?.Replace(Path.DirectorySeparatorChar, '/');
            }
        }

        string[] GetAllUniquePaths()
        {
            HashSet<string> uniquePaths = new HashSet<string>();
            foreach (var def in MainToolbar.GetAllElementDefinitions())
                AddAncestorPaths(def.attr.path, uniquePaths);

            string[] results = new string[uniquePaths.Count];
            int count = 0;
            foreach (var path in uniquePaths)
            {
                results[count] = path;
                ++count;
            }

            return results;
        }

        void OnEnable()
        {
#pragma warning disable UAL0018 // m_Parent lives on this MainToolbarWindow, which a code reload recreates; OnEnable runs again on the new instance and re-reads the toolbar rebuilt by the same reload
            m_Parent = Toolbar.instance;
#pragma warning restore UAL0018

            m_UniqueMenuCategories = GetAllUniquePaths();

            overlayCanvas.rootVisualElement.AddToClassList(k_MainToolbarUSSClassName);

            UIElementsEditorUtility.AddDefaultEditorStyleSheets(rootVisualElement);
            EditorToolbarUtility.LoadStyleSheets("MainToolbar", overlayCanvas.rootVisualElement);

            rootVisualElement.style.unityEditorTextRenderingMode = new StyleEnum<EditorTextRenderingMode>(EditorTextSettings.GetEditorTextRenderingMode());
            rootVisualElement.style.unityTextGenerator = new StyleEnum<TextGeneratorType>(EditorTextSettings.GetEditorTextGeneratorType());

            windowFocusChanged += () => { editModeActive = false; };

            m_EditModeState = new EditMode(this);
            EditorApplication.modifierKeysChanged += OnModifierKeyChanged;

            if (OverlayCanvasesData.instance.GetCanvasData(this, out var data))
            {
                overlayCanvas.ApplySaveData(data.m_SaveData.ToArray(), data.m_DynamicPanelContainerData.ToArray());
            }

            overlayCanvas.presetChanged += OnPresetChanged;

            // Setup initial save state (menuItemPaths can be null alone, from an older preferences file)
            if (OverlayCanvasesData.instance.toolbarSaveState.overlays == null
                || OverlayCanvasesData.instance.toolbarSaveState.overlays.Length == 0
                || OverlayCanvasesData.instance.toolbarSaveState.menuItemPaths == null)
            {
                UpdateLatestSaveState();
            }

            m_Analytics = new MainToolbarAnalytics(this);
        }

        void OnPresetChanged()
        {
            UpdateLatestSaveState();
        }

        void UpdateLatestSaveState()
        {
            OverlayCanvasesData.instance.SetToolbarSaveState(overlayCanvas.CopySaveData());
        }

        void OnDisable()
        {
            overlayCanvas.presetChanged -= OnPresetChanged;
            EditorApplication.modifierKeysChanged -= OnModifierKeyChanged;
            OverlayCanvasesData.instance.SetLastActiveCanvasForWindowType(overlayCanvas);
            m_Analytics.Dispose();
        }

        void CreateGUI()
        {
            overlayCanvas.rootVisualElement.RegisterCallback<ContextClickEvent>((evt) =>
            {
                ShowMenu(new Rect(evt.mousePosition, Vector2.zero));
            });
        }

        private void OnGUI()
        {
            var evt = Event.current;

            if (evt.type == EventType.KeyDown &&
                evt.keyCode == KeyCode.Escape &&
                editModeActive)
            {
                editModeActive = false;
            }

            m_EditModeState.UpdateClutchInput(evt);
        }

        void OnModifierKeyChanged()
        {
            Repaint();
        }

        EditMode m_EditModeState;
        internal bool editModeActive
        {
            get => m_EditModeState.userEnabled;
            set => m_EditModeState.userEnabled = value;
        }

        void ToggleEditMode()
        {
            editModeActive = !editModeActive;
        }

        [AutoStaticsCleanupOnCodeReload]
        internal static event Action<MainToolbar.PickerMode, string> pickerRequested;

        void OpenMenuItemSearch()
        {
            RaisePickerRequested(MainToolbar.PickerMode.MenuItems, string.Empty);
        }

        void OpenMainToolbarElementSearch()
        {
            RaisePickerRequested(MainToolbar.PickerMode.ToolbarElements, string.Empty);
        }

        internal static void RaisePickerRequested(MainToolbar.PickerMode mode, string filter) => pickerRequested?.Invoke(mode, filter);

        [NoAutoStaticsCleanup] // Scratch set of menu category paths, rebuilt (??= / Clear) each menu population; strings only, safe to persist.
        static HashSet<string> s_UsedMenuCategoryPaths;
        void PopulateMenuWithOverlays(AbstractGenericMenu dropdown, bool includeUtilityFunctions = true)
        {
            var overlays = MainToolbar.GetSortedAvailableOverlays();

            s_UsedMenuCategoryPaths ??= new();
            s_UsedMenuCategoryPaths.Clear();
            (Overlay overlay, bool isUnityOnly)? prev = null;
            foreach (var entry in overlays)
            {
                if (prev.HasValue && prev.Value.isUnityOnly && !entry.isUnityOnly)
                    dropdown.AddSeparator("");

                if (entry.attrib.path != Toolbar.deprecatedElementsId || Toolbar.instance.deprecatedElements.Count > 0)
                {
                    var overlay = entry.overlay;
                    dropdown.AddItem(entry.attrib.path, overlay.displayed, () =>
                    {
                        overlay.displayed = !overlay.displayed;
                    });

                    // Marks every ancestor category used, not just the immediate one, so a category holding only subcategories still gets Show/Hide All.
                    AddAncestorPaths(entry.attrib.path, s_UsedMenuCategoryPaths);
                }
                prev = (entry.overlay, entry.isUnityOnly);
            }

            if (includeUtilityFunctions)
            {
                // Add Show/Hide All to each unique category
                foreach (var path in m_UniqueMenuCategories)
                {
                    if (!s_UsedMenuCategoryPaths.Contains(path))
                        continue;

                    dropdown.AddSeparator($"{path}/");
                    dropdown.AddItem($"{path}/{showAllName}", false, () => MainToolbar.ShowAll(path));
                    dropdown.AddItem($"{path}/{hideAllName}", false, () => MainToolbar.HideAll(path));
                }
            }
        }

        void PopulateFullMenu(AbstractGenericMenu dropdown)
        {
            dropdown.AddItem(editModeName, editModeActive, ToggleEditMode);
            dropdown.AddSeparator("");

            PopulateMenuWithOverlays(dropdown);
            dropdown.AddSeparator("");

            dropdown.AddItem(mainToolbarElementSearchName, false, OpenMainToolbarElementSearch);
            dropdown.AddItem(menuItemSearchName, false, OpenMenuItemSearch);
            dropdown.AddSeparator("");

            OverlayPresetManager.GenerateMenu(dropdown, "Presets/", this, false, CheckIfCanvasChangedSinceLastPreset, new UnityOnlyToolbarPreset());
        }

        internal void ShowMenu(Rect dropdownRect)
        {
            var dropdown = rootVisualElement.panel.CreateMenu();

            PopulateFullMenu(dropdown);

            dropdown.DropDown(dropdownRect, rootVisualElement, DropdownMenuSizeMode.Auto);
        }

        bool CheckIfCanvasChangedSinceLastPreset(OverlayCanvas canvas)
        {
            return OverlayUtilities.IsCanvasStateDifferent(canvas.CopySaveData(), OverlayCanvasesData.instance.toolbarSaveState);
        }
    }

    partial class Toolbar : HostView
    {
        [AutoStaticsCleanupOnCodeReload]
        // Points at the live toolbar host view, which a code reload recreates; the Toolbar constructor
        // re-assigns the slot and every reader null-checks it first.
        [IgnoreForUAL0015("Toolbar instance slot re-assigned by the constructor when the host view is recreated")]
        static Toolbar s_Instance;
        public const float ToolbarHeight = 36f;

        internal static Toolbar instance => s_Instance;
        internal static readonly string k_MainToolbarAPIDocumentationLink = $"https://docs.unity3d.com/{Application.unityVersionVer}.{Application.unityVersionMaj}/Documentation/ScriptReference/Toolbars.MainToolbar.html";

        Toolbar()
        {
            #pragma warning disable UAL0015 // rebuilt/resubscribed wholesale on the next reload via this object's own lifecycle; a stale value in the interim is never observed
            s_Instance = this;
#pragma warning disable CS0618 // Type or member is obsolete
            get = s_Instance;
#pragma warning restore CS0618 // Type or member is obsolete
            #pragma warning restore UAL0015
        }

        // Matches the toolbar content's app-toolbar color so the panel root doesn't show the lighter hostview fill.
        private protected override string rootViewClassName => "unity-app-toolbar";

        protected override void OnEnable()
        {
            base.OnEnable();
            m_EventInterests.wantsLessLayoutEvents = true;

            if (actualView is not MainToolbarWindow)
                SetActualViewInternal(CreateInstance<MainToolbarWindow>(), false);

            InitializeFakeHierarchyForDeprecatedToolbarHacks();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        public float CalcHeight()
        {
            return ToolbarHeight;
        }

        [RequiredByNativeCode]
        internal static void RepaintToolbar()
        {
            if (instance != null)
                instance.Repaint();
        }

        // Repaints all views, called from C++ when playmode entering is aborted
        // and when the user clicks on the playmode button.
        [RequiredByNativeCode]
        static void InternalWillTogglePlaymode()
        {
            InternalEditorUtility.RepaintAllViews();
        }

        // TODO remove the following code, SubToolbar.cs and SubToolbarZone.cs when collab has stopped using it
        [NoAutoStaticsCleanup] // Legacy collab sub-toolbar registry (deprecated AddSubToolbar path); intentionally persisted across reload.
        static List<SubToolbar> s_SubToolbars = new List<SubToolbar>();
        internal static IEnumerable<SubToolbar> subToolbars => s_SubToolbars;

        [Obsolete("Use MainToolbarElementAttribute Instead")]
        internal static void AddSubToolbar(SubToolbar subToolbar)
        {
            s_SubToolbars.Add(subToolbar);
        }

        VisualElement m_Root;
        internal const string deprecatedElementsId = "Unsupported User Elements";
        [Obsolete($"Use {nameof(instance)} instead")]
        [AutoStaticsCleanupOnCodeReload]
        // Second slot pointing at the live toolbar host view, assigned alongside s_Instance by the Toolbar
        // constructor when a code reload recreates the view.
        [IgnoreForUAL0015("Toolbar instance slot re-assigned by the constructor when the host view is recreated")]
        internal static Toolbar get;
        List<VisualElement> m_DeprecatedElements = new List<VisualElement>();
        internal IReadOnlyList<VisualElement> deprecatedElements => m_DeprecatedElements;
        // Legacy fake-toolbar population hook (deprecated AddSubToolbar path, same as s_SubToolbars above).
        // Cleared on reload like the other toolbar events: the in-repo subscriber (DeprecatedElementsToolbar
        // in SubToolbarZone.cs) re-subscribes from its [OnCodeLoaded] Initialize(); persisting the list would
        // keep third-party delegates alive and pin their unloaded assemblies.
        [AutoStaticsCleanupOnCodeReload]
        [IgnoreForUAL0015("Event re-subscribed on every code load by DeprecatedElementsToolbar's [OnCodeLoaded] Initialize()")]
        internal static event Action<MainToolbarDockPosition, VisualElement> populateFakeToolbar;

        void InitializeFakeHierarchyForDeprecatedToolbarHacks()
        {
            const string k_MainToolbarUSSClassName = "unity-editor-main-toolbar";

            var name = VisualElement.k_RootVisualContainerName;
            m_Root = new VisualElement()
            {
                name = VisualElementUtils.GetUniqueName(name),
                pickingMode = PickingMode.Ignore, // do not eat events so IMGUI gets them
                viewDataKey = name,
                renderHints = RenderHints.ClipWithScissors
            };
            m_Root.pseudoStates |= PseudoStates.Root;
            m_Root.AddToClassList(k_MainToolbarUSSClassName);

            var ve = new VisualElement();
            var toolbarContainerContent = new VisualElement { name = "ToolbarContainerContent" }.WithClassList("unity-editor-toolbar-container");
            var leftZone = new ToolbarZone { name = "ToolbarZoneLeftAlign" }.WithClassList("unity-editor-toolbar-container__zone");
            var toolbarProductCaption = new VisualElement { name = "ToolbarProductCaption" }.WithClassList("unity-editor-toolbar-product-caption");
            var middleZone = new ToolbarZone { name = "ToolbarZonePlayMode" }.WithClassList("unity-editor-toolbar-container__zone");
            var rightZone = new ToolbarZone { name = "ToolbarZoneRightAlign" }.WithClassList("unity-editor-toolbar-container__zone");
            leftZone.Add(toolbarProductCaption);
            toolbarContainerContent.Add(leftZone);
            toolbarContainerContent.Add(middleZone);
            toolbarContainerContent.Add(rightZone);
            ve.Add(toolbarContainerContent);
            m_Root.Add(ve);

            populateFakeToolbar?.Invoke(MainToolbarDockPosition.Left, leftZone);
            leftZone.TrackElementsAddedToFakeToolbar(this);

            populateFakeToolbar?.Invoke(MainToolbarDockPosition.Middle, middleZone);
            middleZone.TrackElementsAddedToFakeToolbar(this);

            populateFakeToolbar?.Invoke(MainToolbarDockPosition.Right, rightZone);
            rightZone.TrackElementsAddedToFakeToolbar(this);
        }

        internal void LogWarningForElementAddedToFakeToolbar(VisualElement ve)
        {
            Debug.LogWarning($"We have detected that your project includes the following custom element added to the Unity Editor's main toolbar using unsupported methods: \n\n {ve.name} \n\nThis approach is not supported and will lead to issues in future versions. Refer to the official <a href=\"" + k_MainToolbarAPIDocumentationLink + "\">API documentation</a> for adding custom elements to the main toolbar.\n\nYour custom toolbar elements can be unhidden via the context menu (right-click the main toolbar -> <i>Unsupported User Elements</i>).");
            m_DeprecatedElements.Add(ve);
            MainToolbar.Refresh(deprecatedElementsId);
        }
    }

    internal class ToolbarZone : VisualElement
    {
        private Toolbar m_Toolbar;

        public void TrackElementsAddedToFakeToolbar(Toolbar toolbar)
        {
            m_Toolbar = toolbar;
        }

        internal override void OnChildAdded(VisualElement ve)
        {
            if (m_Toolbar != null)
                m_Toolbar.LogWarningForElementAddedToFakeToolbar(ve);
        }
    }
}
