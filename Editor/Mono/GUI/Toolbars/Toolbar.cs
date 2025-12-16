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
        internal static readonly string editModeName = L10n.Tr("Edit Mode");
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static readonly string showAllName = L10n.Tr("Show All");
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal static readonly string hideAllName = L10n.Tr("Hide All");

        internal static MainToolbarWindow instance;

        OverlayCanvasMode ISupportsOverlaysCustomMode.overlayCanvasMode => OverlayCanvasMode.MainToolbar;

        string[] m_UniqueMenuCategories;

        MainToolbarWindow()
        {
            instance = this;
        }

        string[] GetAllUniquePaths()
        {
            HashSet<string> uniquePaths = new HashSet<string>();
            foreach (var def in MainToolbar.GetAllElementDefinitions())
            {
                var path = Path.GetDirectoryName(def.attr.path);
                // UUM-116278: On Windows leaving the '\\' will result in a new empty menu later in
                // the menu creation as dropdown.AddItem uses '/' as a submenu separator.
                path = path.Replace(Path.DirectorySeparatorChar, '/');

                while (!string.IsNullOrEmpty(path))
                {
                    uniquePaths.Add(path);
                    path = Path.GetDirectoryName(path);
                }
            }

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
            m_Parent = Toolbar.instance;

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

            overlayCanvas.presetChanged += UpdateLatestSaveState;

            // Setup initial save state
            if (OverlayCanvasesData.instance.toolbarSaveState.overlays == null
                || OverlayCanvasesData.instance.toolbarSaveState.overlays.Length == 0)
            {
                UpdateLatestSaveState();
            }
        }

        void UpdateLatestSaveState()
        {
            OverlayCanvasesData.instance.SetToolbarSaveState(overlayCanvas.CopySaveData());
        }

        void OnDisable()
        {
            EditorApplication.modifierKeysChanged -= OnModifierKeyChanged;
            OverlayCanvasesData.instance.SetLastActiveCanvasForWindowType(overlayCanvas);
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

        void PopulateMenuWithOverlays(AbstractGenericMenu dropdown, bool includeUtilityFunctions = true)
        {
            var overlays = new List<(Overlay overlay, MainToolbarElementAttribute attrib)>();

            s_UnityOnlyOverlays.Clear();
            foreach (var overlay in overlayCanvas.overlays)
            {
                var mto = overlay as MainToolbarOverlay;
                overlays.Add((overlay, mto.createElementMethod.GetCustomAttribute<MainToolbarElementAttribute>()));
                if (mto.createElementMethod.GetCustomAttribute<UnityOnlyMainToolbarPresetAttribute>() != null)
                    s_UnityOnlyOverlays.Add(overlay);
            }

            overlays.Sort((a, b) =>
            {
                // Group into unity vs non-unity first
                if (s_UnityOnlyOverlays.Contains(a.overlay) && !s_UnityOnlyOverlays.Contains(b.overlay))
                    return -1;
                if (s_UnityOnlyOverlays.Contains(b.overlay) && !s_UnityOnlyOverlays.Contains(a.overlay))
                    return 1;

                // Sort by menu priority first
                var result = a.attrib.menuPriority.CompareTo(b.attrib.menuPriority);
                if (result != 0)
                    return result;

                // Then alphabetically by path
                result = String.Compare(a.attrib.path, b.attrib.path, StringComparison.OrdinalIgnoreCase);
                if (result != 0)
                    return result;

                // Then by dock position and index
                return ((int)a.attrib.defaultDockPosition * 100 + a.attrib.defaultDockIndex)
                    .CompareTo((int)b.attrib.defaultDockPosition * 100 + b.attrib.defaultDockIndex);
            });

            Overlay prevOverlay = null;
            foreach (var pair in overlays)
            {
                if (s_UnityOnlyOverlays.Contains(prevOverlay) && !s_UnityOnlyOverlays.Contains(pair.overlay))
                    dropdown.AddSeparator("");

                if (pair.attrib.path != Toolbar.deprecatedElementsId || Toolbar.instance.deprecatedElements.Count > 0)
                {
                    dropdown.AddItem(pair.attrib.path, pair.overlay.displayed, () =>
                    {
                        pair.overlay.displayed = !pair.overlay.displayed;
                    });
                }
                prevOverlay = pair.overlay;
            }

            if (includeUtilityFunctions)
            {
                // Add Show/Hide All to each unique category
                foreach (var path in m_UniqueMenuCategories)
                {
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

            OverlayPresetManager.GenerateMenu(dropdown, "Presets/", this, false, CheckIfCanvasChangedSinceLastPreset, new UnityOnlyToolbarPreset());
        }

        static HashSet<Overlay> s_UnityOnlyOverlays = new();
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
        static Toolbar s_Instance;
        public const float ToolbarHeight = 36f;

        internal static Toolbar instance => s_Instance;
        internal static readonly string k_MainToolbarAPIDocumentationLink = $"https://docs.unity3d.com/{Application.unityVersionVer}.{Application.unityVersionMaj}/Documentation/ScriptReference/Toolbars.MainToolbar.html";

        Toolbar()
        {
            s_Instance = this;
#pragma warning disable CS0618 // Type or member is obsolete
            get = s_Instance;
#pragma warning restore CS0618 // Type or member is obsolete
        }

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
        internal static Toolbar get;
        List<VisualElement> m_DeprecatedElements = new List<VisualElement>();
        internal IReadOnlyList<VisualElement> deprecatedElements => m_DeprecatedElements;
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
            var toolbarContainerContent = new VisualElement { name = "ToolbarContainerContent", classList = { "unity-editor-toolbar-container" } };
            var leftZone = new ToolbarZone { name = "ToolbarZoneLeftAlign", classList = { "unity-editor-toolbar-container__zone" } };
            var toolbarProductCaption = new VisualElement { name = "ToolbarProductCaption", classList = { "unity-editor-toolbar-product-caption" } };
            var middleZone = new ToolbarZone { name = "ToolbarZonePlayMode", classList = { "unity-editor-toolbar-container__zone" } };
            var rightZone = new ToolbarZone { name = "ToolbarZoneRightAlign", classList = { "unity-editor-toolbar-container__zone" } };
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
            Debug.LogWarning($"We have detected that your project includes custom elements added to the Unity Editor's main toolbar using unsupported methods. This approach is not supported and will lead to issues in future versions. Refer to the official <a href=\"" + k_MainToolbarAPIDocumentationLink + "\">API documentation</a> for adding custom elements to the main toolbar.\n\nYour custom toolbar elements can be unhidden via the context menu (right-click the main toolbar -> <i>Unsupported User Elements</i>).");
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
