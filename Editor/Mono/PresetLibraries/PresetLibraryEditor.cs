// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;

namespace UnityEditor
{
    // Client code should allocate and ensure its being serialized (e.g as a field in an EditorWindow)
    [System.Serializable]
    internal class PresetLibraryEditorState
    {
        public enum ItemViewMode { Grid, List }

        [SerializeField]
        private ItemViewMode m_ItemViewMode;
        public float m_PreviewHeight = 32f;
        public Vector2 m_ScrollPosition;
        public string m_CurrrentLibrary = PresetLibraryLocations.defaultPresetLibraryPath;
        public int m_HoverIndex = -1;
        public RenameOverlay m_RenameOverlay = new RenameOverlay();
        public string m_Prefix;

        static public ItemViewMode GetItemViewMode(string prefix)
        {
            return (ItemViewMode)EditorPrefs.GetInt(prefix + "ViewMode", (int)ItemViewMode.Grid);
        }

        public PresetLibraryEditorState(string prefix)
        {
            m_Prefix = prefix;
        }

        public ItemViewMode itemViewMode
        {
            get { return m_ItemViewMode; }
            set
            {
                if (m_ItemViewMode != value)
                {
                    m_ItemViewMode = value;
                    InspectorWindow.RepaintAllInspectors(); // if inspector is showing a preset library we want it to follow the state
                    EditorPrefs.SetInt(m_Prefix + "ViewMode", (int)m_ItemViewMode);
                }
            }
        }

        public void TransferEditorPrefsState(bool load)
        {
            if (load)
            {
                m_ItemViewMode = (ItemViewMode)EditorPrefs.GetInt(m_Prefix + "ViewMode", (int)m_ItemViewMode);
                m_PreviewHeight = EditorPrefs.GetFloat(m_Prefix + "ItemHeight", m_PreviewHeight);
                m_ScrollPosition.y = EditorPrefs.GetFloat(m_Prefix + "Scroll", m_ScrollPosition.y);
                m_CurrrentLibrary = EditorPrefs.GetString(m_Prefix + "CurrentLib", m_CurrrentLibrary);
            }
            else // save
            {
                EditorPrefs.SetInt(m_Prefix + "ViewMode", (int)m_ItemViewMode);
                EditorPrefs.SetFloat(m_Prefix + "ItemHeight", m_PreviewHeight);
                EditorPrefs.SetFloat(m_Prefix + "Scroll", m_ScrollPosition.y);
                EditorPrefs.SetString(m_Prefix + "CurrentLib", m_CurrrentLibrary);
            }
        }
    }


    internal partial class PresetLibraryEditor<T> where T : PresetLibrary
    {
        class Styles
        {
            public GUIStyle innerShadowBg = GetStyle("InnerShadowBg");
            public GUIStyle optionsButton = GetStyle("PaneOptions");
            public GUIStyle newPresetStyle = new GUIStyle(EditorStyles.boldLabel);
            public GUIContent plusButtonText = new GUIContent("", "Add new preset");
            public GUIContent plusButtonTextNotCheckedOut = new GUIContent("", "To add presets you need to press the 'Check out' button below");
            public GUIContent header = new GUIContent("Presets");
            public GUIContent newPreset = new GUIContent("New");

            public Styles()
            {
                newPresetStyle.alignment = TextAnchor.MiddleCenter;
                newPresetStyle.normal.textColor = Color.white;
            }

            static GUIStyle GetStyle(string styleName)
            {
                return styleName; // Implicit construction of GUIStyle

                // For faster testing of GUISkin changes in editor resources load directly from skin
                //GUISkin skin = EditorGUIUtility.LoadRequired ("Builtin Skins/DarkSkin/Skins/Presets.guiSkin") as GUISkin;
                //return skin.GetStyle (styleName);
            }
        }
        static Styles s_Styles;

        class DragState
        {
            public int dragUponIndex { get; set; }
            public int draggingIndex { get; set; }
            public bool insertAfterIndex { get; set; }
            public Rect dragUponRect { get; set; }
            public bool IsDragging() {return draggingIndex != -1; }
            public DragState() {dragUponIndex = -1; draggingIndex = -1; }
        }
        DragState m_DragState = new DragState();

        readonly VerticalGrid m_Grid = new VerticalGrid();
        readonly PresetLibraryEditorState m_State;
        readonly ScriptableObjectSaveLoadHelper<T> m_SaveLoadHelper;
        readonly System.Action<int, object> m_ItemClickedCallback;      // <click count, clicked preset>

        public System.Action<PresetLibrary> addDefaultPresets;
        public System.Action presetsWasReordered;

        // layout
        const float kGridLabelHeight = 16f;
        const float kCheckoutButtonMaxWidth = 100f;
        const float kCheckoutButtonMargin = 2f;
        Vector2 m_MinMaxPreviewHeight = new Vector2(14, 64);
        float m_PreviewAspect = 8f;
        bool m_ShowAddNewPresetItem = true;
        bool m_ShowedScrollBarLastFrame = false;
        bool m_IsOpenForEdit = true;
        PresetFileLocation m_PresetLibraryFileLocation;
        public float contentHeight { get; private set; }
        float topAreaHeight { get { return 20f; } }
        float versionControlAreaHeight { get { return 20f; } }
        float gridWidth { get; set; }

        public bool wantsToCreateLibrary { get; set; }
        public bool showHeader { get; set; }
        public float settingsMenuRightMargin { get; set; }
        public bool alwaysShowScrollAreaHorizontalLines {get; set; }
        public bool useOnePixelOverlappedGrid {get; set; }
        public RectOffset marginsForList {get; set; }
        public RectOffset marginsForGrid {get; set; }

        public PresetLibraryEditor(ScriptableObjectSaveLoadHelper<T> helper,
                                   PresetLibraryEditorState state,
                                   System.Action<int, object> itemClickedCallback
                                   )
        {
            m_SaveLoadHelper = helper;
            m_State = state;
            m_ItemClickedCallback = itemClickedCallback;
            settingsMenuRightMargin = 10;
            useOnePixelOverlappedGrid = false;
            alwaysShowScrollAreaHorizontalLines = true;
            marginsForList = new RectOffset(10, 10, 5, 5);
            marginsForGrid = new RectOffset(5, 5, 5, 5);
            m_PresetLibraryFileLocation = PresetLibraryLocations.GetFileLocationFromPath(currentLibraryWithoutExtension);
        }

        public void InitializeGrid(float availableWidth)
        {
            T lib = GetCurrentLib();
            if (lib != null)
            {
                if (availableWidth > 0f)
                    SetupGrid(availableWidth, lib.Count());
            }
            else
                Debug.LogError("Could not load preset library " + currentLibraryWithoutExtension);
        }

        void Repaint()
        {
            // Repaints current view
            HandleUtility.Repaint();
        }

        void ValidateNoExtension(string value)
        {
            if (Path.HasExtension(value))
                Debug.LogError("currentLibraryWithoutExtension should not have an extension: " + value);
        }

        public string currentLibraryWithoutExtension
        {
            get
            {
                return m_State.m_CurrrentLibrary;
            }
            set
            {
                m_State.m_CurrrentLibrary = Path.ChangeExtension(value, null); // ensure no extension
                m_PresetLibraryFileLocation = PresetLibraryLocations.GetFileLocationFromPath(m_State.m_CurrrentLibrary);
                OnLayoutChanged();
                Repaint();
            }
        }

        public float previewAspect
        {
            get { return m_PreviewAspect; }
            set { m_PreviewAspect = value; }
        }

        public Vector2 minMaxPreviewHeight
        {
            get { return m_MinMaxPreviewHeight; }
            set
            {
                m_MinMaxPreviewHeight = value;
                previewHeight = previewHeight; // clamps to min max
            }
        }

        public float previewHeight
        {
            get { return m_State.m_PreviewHeight; }
            set
            {
                m_State.m_PreviewHeight = Mathf.Clamp(value, minMaxPreviewHeight.x, minMaxPreviewHeight.y);
                Repaint();
            }
        }

        public PresetLibraryEditorState.ItemViewMode itemViewMode
        {
            get { return m_State.itemViewMode; }
            set
            {
                m_State.itemViewMode = value;
                OnLayoutChanged();
                Repaint();
            }
        }

        bool drawLabels {get {return m_State.itemViewMode == PresetLibraryEditorState.ItemViewMode.List; }}

        // Returns an error string. If no errors occured then null is returned
        string CreateNewLibraryCallback(string libraryName, PresetFileLocation fileLocation)
        {
            string defaultPath = PresetLibraryLocations.GetDefaultFilePathForFileLocation(fileLocation);
            string pathWithoutExtension = Path.Combine(defaultPath, libraryName);
            if (CreateNewLibrary(pathWithoutExtension) != null)
                currentLibraryWithoutExtension = pathWithoutExtension;
            return PresetLibraryManager.instance.GetLastError();
        }

        static bool IsItemVisible(float scrollHeight, float itemYMin, float itemYMax, float scrollPos)
        {
            float yMin = itemYMin - scrollPos;
            float yMax = itemYMax - scrollPos;
            if (yMax < 0f)
                return false;
            if (yMin > scrollHeight)
                return false;

            return true;
        }

        void OnLayoutChanged()
        {
            T lib = GetCurrentLib();
            if (lib == null || gridWidth <= 0f)
                return;

            SetupGrid(gridWidth, lib.Count());
        }

        void SetupGrid(float width, int itemCount)
        {
            if (width < 1f)
            {
                Debug.LogError("Invalid width " + width + ", " + Event.current.type);
                return;
            }

            if (m_ShowAddNewPresetItem)
                itemCount++;

            m_Grid.useFixedHorizontalSpacing = useOnePixelOverlappedGrid;
            m_Grid.fixedHorizontalSpacing = useOnePixelOverlappedGrid ? -1 : 0;

            switch (m_State.itemViewMode)
            {
                case PresetLibraryEditorState.ItemViewMode.Grid:

                    m_Grid.fixedWidth = width;
                    m_Grid.topMargin = marginsForGrid.top;
                    m_Grid.bottomMargin = marginsForGrid.bottom;
                    m_Grid.leftMargin = marginsForGrid.left;
                    m_Grid.rightMargin = marginsForGrid.right;
                    m_Grid.verticalSpacing = useOnePixelOverlappedGrid ? -1 : 2;
                    m_Grid.minHorizontalSpacing = 1f;
                    m_Grid.itemSize = new Vector2(m_State.m_PreviewHeight * m_PreviewAspect, m_State.m_PreviewHeight); // no text
                    m_Grid.InitNumRowsAndColumns(itemCount, int.MaxValue);
                    break;
                case PresetLibraryEditorState.ItemViewMode.List:
                    m_Grid.fixedWidth = width;
                    m_Grid.topMargin = marginsForList.top;
                    m_Grid.bottomMargin = marginsForList.bottom;
                    m_Grid.leftMargin = marginsForList.left;
                    m_Grid.rightMargin = marginsForList.right;
                    m_Grid.verticalSpacing = 2f;
                    m_Grid.minHorizontalSpacing = 0f;
                    m_Grid.itemSize = new Vector2(width - m_Grid.leftMargin, m_State.m_PreviewHeight);
                    m_Grid.InitNumRowsAndColumns(itemCount, int.MaxValue);
                    break;
            }


            float gridHeight = m_Grid.CalcRect(itemCount - 1, 0f).yMax + m_Grid.bottomMargin;
            contentHeight = topAreaHeight + gridHeight + (m_IsOpenForEdit ? 0 : versionControlAreaHeight);
        }

        public void OnGUI(Rect rect, object presetObject)
        {
            // If removing this early out grid setup needs to be ignored for layout and used events
            if (rect.width < 2f)
                return;

            m_State.m_RenameOverlay.OnEvent();

            T lib = GetCurrentLib();

            if (s_Styles == null)
                s_Styles = new Styles();

            Rect topArea = new Rect(rect.x, rect.y, rect.width, topAreaHeight);
            Rect presetRect = new Rect(rect.x, topArea.yMax, rect.width, rect.height - topAreaHeight);

            TopArea(topArea);
            ListArea(presetRect, lib, presetObject);
        }

        void TopArea(Rect rect)
        {
            GUI.BeginGroup(rect);
            {
                if (showHeader)
                    GUI.Label(new Rect(10, 2, rect.width - 20, rect.height), s_Styles.header);

                const float optionsButtonWidth = 16f;
                const float optionsButtonHeight = 6f;
                Rect buttonRect = new Rect(rect.width - optionsButtonWidth - settingsMenuRightMargin, (rect.height - optionsButtonHeight) * 0.5f, optionsButtonWidth, rect.height);
                if (Event.current.type == EventType.Repaint)
                    s_Styles.optionsButton.Draw(buttonRect, false, false, false, false);

                // We want larger click area than the button icon
                buttonRect.y = 0f;
                buttonRect.height = rect.height;
                buttonRect.width = 24f;
                if (GUI.Button(buttonRect, GUIContent.none, GUIStyle.none))
                    SettingsMenu.Show(buttonRect, this);

                if (wantsToCreateLibrary)
                {
                    wantsToCreateLibrary = false;
                    PopupWindow.Show(buttonRect, new PopupWindowContentForNewLibrary(CreateNewLibraryCallback));
                    EditorGUIUtility.ExitGUI();
                }
            } GUI.EndGroup();
        }

        Rect GetDragRect(Rect itemRect)
        {
            int extraHorz = Mathf.FloorToInt(m_Grid.horizontalSpacing * 0.5f + 0.5f);
            int extraVert = Mathf.FloorToInt(m_Grid.verticalSpacing * 0.5f + 0.5f);
            return new RectOffset(extraHorz, extraHorz, extraVert, extraVert).Add(itemRect);
        }

        void ClearDragState()
        {
            m_DragState.dragUponIndex = -1;
            m_DragState.draggingIndex = -1;
        }

        void DrawHoverEffect(Rect itemRect, bool drawAsSelection)
        {
            Color orgColor = GUI.color;
            GUI.color = new Color(0, 0, 0.4f, drawAsSelection ? 0.8f : 0.3f);
            Rect hoverRect = new RectOffset(3, 3, 3, 3).Add(itemRect);
            GUI.Label(hoverRect, GUIContent.none, EditorStyles.helpBox);
            GUI.color = orgColor;
        }

        private string pathWithExtension
        {
            get { return currentLibraryWithoutExtension + "." + m_SaveLoadHelper.fileExtensionWithoutDot; }
        }

        void VersionControlArea(Rect rect)
        {
            if (rect.width > kCheckoutButtonMaxWidth)
                rect = new Rect(rect.xMax - kCheckoutButtonMaxWidth - kCheckoutButtonMargin, rect.y + kCheckoutButtonMargin, kCheckoutButtonMaxWidth, rect.height - kCheckoutButtonMargin * 2);

            if (GUI.Button(rect, "Check out", EditorStyles.miniButton))
            {
                Provider.Checkout(new[] { pathWithExtension }, CheckoutMode.Asset);
            }
        }

        void ListArea(Rect rect, PresetLibrary lib, object newPresetObject)
        {
            if (lib == null)
                return;

            Event evt = Event.current;

            if (m_PresetLibraryFileLocation == PresetFileLocation.ProjectFolder && evt.type == EventType.Repaint)
                m_IsOpenForEdit = AssetDatabase.IsOpenForEdit(pathWithExtension, StatusQueryOptions.UseCachedIfPossible);
            else if (m_PresetLibraryFileLocation == PresetFileLocation.PreferencesFolder)
                m_IsOpenForEdit = true;

            if (!m_IsOpenForEdit)
            {
                Rect versionControlRect = new Rect(rect.x, rect.yMax - versionControlAreaHeight, rect.width, versionControlAreaHeight);
                VersionControlArea(versionControlRect);
                rect.height -= versionControlAreaHeight;
            }

            // To ensure we setup grid to visible rect we need to run once to check if scrollbar is taking up screen estate.
            // To optimize the first width is based on the last frame and we therefore most likely will only run once.
            for (int i = 0; i < 2; i++)
            {
                gridWidth = m_ShowedScrollBarLastFrame ? rect.width - 17 : rect.width;
                SetupGrid(gridWidth, lib.Count());
                bool isShowingScrollBar = m_Grid.height > rect.height;
                if (isShowingScrollBar == m_ShowedScrollBarLastFrame)
                    break;
                else
                    m_ShowedScrollBarLastFrame = isShowingScrollBar;
            }

            // Draw horizontal lines for scrollview content to clip against
            if ((m_ShowedScrollBarLastFrame || alwaysShowScrollAreaHorizontalLines) && Event.current.type == EventType.Repaint)
            {
                Rect scrollEdgeRect = new RectOffset(1, 1, 1, 1).Add(rect);
                scrollEdgeRect.height = 1;
                EditorGUI.DrawRect(scrollEdgeRect, new Color(0, 0, 0, 0.3f));
                scrollEdgeRect.y += rect.height + 1;
                EditorGUI.DrawRect(scrollEdgeRect, new Color(0, 0, 0, 0.3f));
            }

            Rect contentRect = new Rect(0, 0, 1, m_Grid.height);
            m_State.m_ScrollPosition = GUI.BeginScrollView(rect, m_State.m_ScrollPosition, contentRect);
            {
                int startIndex, endIndex;
                float yOffset = 0f;
                int maxIndex = m_ShowAddNewPresetItem ? lib.Count() : lib.Count() - 1;
                bool isGridVisible = m_Grid.IsVisibleInScrollView(rect.height, m_State.m_ScrollPosition.y, yOffset, maxIndex, out startIndex, out endIndex);
                bool drawDragInsertionMarker = false;
                if (isGridVisible)
                {
                    // Handle renaming overlay before item handling because its needs mouse input first to end renaming if clicked outside
                    if (GetRenameOverlay().IsRenaming() && !GetRenameOverlay().isWaitingForDelay)
                    {
                        if (!m_State.m_RenameOverlay.OnGUI())
                        {
                            EndRename();
                            evt.Use();
                        }
                        Repaint();
                    }

                    for (int i = startIndex; i <= endIndex; ++i)
                    {
                        int itemControlID = i + 1000000;

                        Rect itemRect = m_Grid.CalcRect(i, yOffset);
                        Rect previewRect = itemRect;
                        Rect labelRect = itemRect;
                        switch (m_State.itemViewMode)
                        {
                            case PresetLibraryEditorState.ItemViewMode.List:
                                previewRect.width = m_State.m_PreviewHeight * m_PreviewAspect;
                                labelRect.x += previewRect.width + 8f;
                                labelRect.width -= previewRect.width + 10f;
                                labelRect.height = kGridLabelHeight;
                                labelRect.y = itemRect.yMin + (itemRect.height - kGridLabelHeight) * 0.5f;
                                break;

                            case PresetLibraryEditorState.ItemViewMode.Grid:
                                // only preview is shown: no label
                                break;
                        }

                        // Add new preset button
                        if (m_ShowAddNewPresetItem && i == lib.Count())
                        {
                            CreateNewPresetButton(previewRect, newPresetObject, lib, m_IsOpenForEdit);
                            continue;
                        }

                        // Rename overlay
                        bool isRenamingThisItem = IsRenaming(i);
                        if (isRenamingThisItem)
                        {
                            Rect renameRect = labelRect;
                            renameRect.y -= 1f; renameRect.x -= 1f; // adjustment to fit perfectly
                            m_State.m_RenameOverlay.editFieldRect = renameRect;
                        }

                        // Handle event
                        switch (evt.type)
                        {
                            case EventType.Repaint:
                                if (m_State.m_HoverIndex == i)
                                {
                                    if (itemRect.Contains(evt.mousePosition))
                                    {
                                        // TODO: We need a better hover effect so disabling for now...
                                        //if (!GetRenameOverlay().IsRenaming ())
                                        //  DrawHoverEffect (itemRect, false);
                                    }
                                    else
                                        m_State.m_HoverIndex = -1;
                                }

                                if (m_DragState.draggingIndex == i || GUIUtility.hotControl == itemControlID)
                                    DrawHoverEffect(itemRect, false);

                                lib.Draw(previewRect, i);
                                if (!isRenamingThisItem && drawLabels)
                                    GUI.Label(labelRect, GUIContent.Temp(lib.GetName(i)));

                                if (m_DragState.dragUponIndex == i && m_DragState.draggingIndex != m_DragState.dragUponIndex)
                                    drawDragInsertionMarker = true;

                                // We delete presets on alt-click
                                if (GUIUtility.hotControl == 0 && Event.current.alt && m_IsOpenForEdit)
                                    EditorGUIUtility.AddCursorRect(itemRect, MouseCursor.ArrowMinus);

                                break;
                            case EventType.MouseDown:
                                if (evt.button == 0 && itemRect.Contains(evt.mousePosition))
                                {
                                    GUIUtility.hotControl = itemControlID;
                                    if (evt.clickCount == 1)
                                    {
                                        m_ItemClickedCallback(evt.clickCount, lib.GetPreset(i));
                                        evt.Use();
                                    }
                                }
                                break;
                            case EventType.MouseDrag:
                                if (GUIUtility.hotControl == itemControlID && m_IsOpenForEdit)
                                {
                                    DragAndDropDelay delay = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), itemControlID);
                                    if (delay.CanStartDrag())
                                    {
                                        // Start drag
                                        DragAndDrop.PrepareStartDrag();
                                        DragAndDrop.SetGenericData("DraggingPreset", i);
                                        DragAndDrop.StartDrag("");
                                        m_DragState.draggingIndex = i;
                                        m_DragState.dragUponIndex = i;
                                        GUIUtility.hotControl = 0;
                                    }
                                    evt.Use();
                                }
                                break;

                            case EventType.DragUpdated:
                            case EventType.DragPerform:
                            {
                                Rect dragRect = GetDragRect(itemRect);
                                if (dragRect.Contains(evt.mousePosition))
                                {
                                    m_DragState.dragUponIndex = i;
                                    m_DragState.dragUponRect = itemRect;

                                    if (m_State.itemViewMode == PresetLibraryEditorState.ItemViewMode.List)
                                        m_DragState.insertAfterIndex = ((evt.mousePosition.y - dragRect.y) / dragRect.height) > 0.5f;
                                    else
                                        m_DragState.insertAfterIndex = ((evt.mousePosition.x - dragRect.x) / dragRect.width) > 0.5f;

                                    bool perform = evt.type == EventType.DragPerform;
                                    if (perform)
                                    {
                                        if (m_DragState.draggingIndex >= 0)
                                        {
                                            MovePreset(m_DragState.draggingIndex, m_DragState.dragUponIndex, m_DragState.insertAfterIndex);
                                            DragAndDrop.AcceptDrag();
                                        }
                                        ClearDragState();
                                    }
                                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                                    evt.Use();
                                }
                            }
                            break;
                            case EventType.DragExited:
                                if (m_DragState.IsDragging())
                                {
                                    ClearDragState();
                                    evt.Use();
                                }
                                break;

                            case EventType.MouseUp:
                                if (GUIUtility.hotControl == itemControlID)
                                {
                                    GUIUtility.hotControl = 0;
                                    if (evt.button == 0 && itemRect.Contains(evt.mousePosition))
                                    {
                                        if (Event.current.alt && m_IsOpenForEdit)
                                        {
                                            DeletePreset(i);
                                            evt.Use();
                                        }
                                    }
                                }
                                break;
                            case EventType.ContextClick:
                                if (itemRect.Contains(evt.mousePosition))
                                {
                                    PresetContextMenu.Show(m_IsOpenForEdit, i, newPresetObject, this);
                                    evt.Use();
                                }
                                break;
                            case EventType.MouseMove:
                                if (itemRect.Contains(evt.mousePosition))
                                {
                                    if (m_State.m_HoverIndex != i)
                                    {
                                        m_State.m_HoverIndex = i;
                                        Repaint();
                                    }
                                }
                                else if (m_State.m_HoverIndex == i)
                                {
                                    m_State.m_HoverIndex = -1;
                                    Repaint();
                                }

                                break;
                        }
                    } // end foreach item

                    // Draw above all items
                    if (drawDragInsertionMarker)
                        DrawDragInsertionMarker();
                }
            } GUI.EndScrollView();
        }

        void DrawDragInsertionMarker()
        {
            if (!m_DragState.IsDragging())
                return;

            Rect dragRect = GetDragRect(m_DragState.dragUponRect);
            Rect insertRect;
            const float inserterThickness = 2f;
            const float halfInserter = inserterThickness * 0.5f;
            if (m_State.itemViewMode == PresetLibraryEditorState.ItemViewMode.List)
            {
                if (m_DragState.insertAfterIndex)
                    insertRect = new Rect(dragRect.xMin, dragRect.yMax - halfInserter, dragRect.width, inserterThickness);
                else
                    insertRect = new Rect(dragRect.xMin, dragRect.yMin - halfInserter, dragRect.width, inserterThickness);
            }
            else // grid
            {
                if (m_DragState.insertAfterIndex)
                    insertRect = new Rect(dragRect.xMax - halfInserter, dragRect.yMin, inserterThickness, dragRect.height);
                else
                    insertRect = new Rect(dragRect.xMin - halfInserter, dragRect.yMin, inserterThickness, dragRect.height);
            }
            EditorGUI.DrawRect(insertRect, new Color(0.3f, 0.3f, 1.0f));
        }

        void CreateNewPresetButton(Rect buttonRect, object newPresetObject, PresetLibrary lib, bool isOpenForEdit)
        {
            using (new EditorGUI.DisabledScope(!isOpenForEdit))
            {
                if (GUI.Button(buttonRect, isOpenForEdit ? s_Styles.plusButtonText : s_Styles.plusButtonTextNotCheckedOut))
                {
                    int newIndex = CreateNewPreset(newPresetObject, "");
                    if (drawLabels)
                        BeginRenaming("", newIndex, 0f);
                    InspectorWindow.RepaintAllInspectors(); // If inspecting a preset libarary we want to show the new preset there as well
                }

                if (Event.current.type == EventType.Repaint)
                {
                    Rect rect2 = new RectOffset(-3, -3, -3, -3).Add(buttonRect);
                    lib.Draw(rect2, newPresetObject);

                    if (buttonRect.width > 30)
                    {
                        LabelWithOutline(buttonRect, s_Styles.newPreset, new Color(0.1f, 0.1f, 0.1f), s_Styles.newPresetStyle);
                    }
                    else
                    {
                        if (lib.Count() == 0 && isOpenForEdit)
                        {
                            buttonRect.x = buttonRect.xMax + 5f;
                            buttonRect.width = 200;
                            buttonRect.height = EditorGUI.kSingleLineHeight;
                            using (new EditorGUI.DisabledScope(true))
                            {
                                GUI.Label(buttonRect, "Click to add new preset");
                            }
                        }
                    }
                }
            }
        }

        static void LabelWithOutline(Rect rect, GUIContent content, Color outlineColor, GUIStyle style)
        {
            const int outlineWidth = 1;

            Color orgColor = GUI.color;
            GUI.color = outlineColor;
            for (int i = -outlineWidth; i <= outlineWidth; ++i)
            {
                for (int j = -outlineWidth; j <= outlineWidth; ++j)
                {
                    if (i == 0 && j == 0)
                        continue;

                    Rect outlineRect = rect;
                    outlineRect.x += j;
                    outlineRect.y += i;
                    GUI.Label(outlineRect, content, style);
                }
            }
            GUI.color = orgColor;

            GUI.Label(rect, content, style);
        }

        bool IsRenaming(int itemID)
        {
            return GetRenameOverlay().IsRenaming() && GetRenameOverlay().userData == itemID && !GetRenameOverlay().isWaitingForDelay;
        }

        RenameOverlay GetRenameOverlay()
        {
            return m_State.m_RenameOverlay;
        }

        void BeginRenaming(string name, int itemIndex, float delay)
        {
            GetRenameOverlay().BeginRename(name, itemIndex, delay);
        }

        void EndRename()
        {
            if (!GetRenameOverlay().userAcceptedRename)
                return;

            // We are done renaming (user accepted/rejected, we lost focus etc, other grabbed renameOverlay etc.)
            string name = string.IsNullOrEmpty(GetRenameOverlay().name) ? GetRenameOverlay().originalName : GetRenameOverlay().name;
            int itemIndex = GetRenameOverlay().userData; // we passed in an instanceID as userData

            T lib = GetCurrentLib();
            if (itemIndex >= 0 && itemIndex < lib.Count())
            {
                lib.SetName(itemIndex, name);
                SaveCurrentLib();
            }

            // Ensure cleanup
            GetRenameOverlay().EndRename(true);
        }

        public T GetCurrentLib()
        {
            T lib = PresetLibraryManager.instance.GetLibrary<T>(m_SaveLoadHelper, currentLibraryWithoutExtension);
            if (lib == null)
            {
                // If current library not found then get the default library (or create the default library if not present)
                lib = PresetLibraryManager.instance.GetLibrary<T>(m_SaveLoadHelper, PresetLibraryLocations.defaultPresetLibraryPath);
                if (lib == null)
                {
                    lib = CreateNewLibrary(PresetLibraryLocations.defaultPresetLibraryPath);
                    if (lib != null)
                    {
                        // Add default set of presets
                        if (addDefaultPresets != null)
                        {
                            addDefaultPresets(lib);
                            PresetLibraryManager.instance.SaveLibrary(m_SaveLoadHelper, lib, PresetLibraryLocations.defaultPresetLibraryPath);
                        }
                    }
                    else
                    {
                        Debug.LogError("Could not create Default preset library " + PresetLibraryManager.instance.GetLastError());
                    }
                }

                currentLibraryWithoutExtension = PresetLibraryLocations.defaultPresetLibraryPath;
            }
            return lib;
        }

        public void UnloadUsedLibraries()
        {
            PresetLibraryManager.instance.UnloadAllLibrariesFor(m_SaveLoadHelper);
        }

        public void DeletePreset(int presetIndex)
        {
            T lib = GetCurrentLib();
            if (lib == null)
                return;

            if (presetIndex < 0 || presetIndex >= lib.Count())
            {
                Debug.LogError("DeletePreset: Invalid index: out of bounds");
                return;
            }

            lib.Remove(presetIndex);
            SaveCurrentLib();
            if (presetsWasReordered != null)
                presetsWasReordered();

            OnLayoutChanged();
        }

        public void ReplacePreset(int presetIndex, object presetObject)
        {
            T lib = GetCurrentLib();
            if (lib == null)
                return;

            if (presetIndex < 0 || presetIndex >= lib.Count())
            {
                Debug.LogError("ReplacePreset: Invalid index: out of bounds");
                return;
            }

            lib.Replace(presetIndex, presetObject);
            SaveCurrentLib();
            if (presetsWasReordered != null)
                presetsWasReordered();
        }

        public void MovePreset(int presetIndex, int destPresetIndex, bool insertAfterDestIndex)
        {
            T lib = GetCurrentLib();
            if (lib == null)
                return;

            if (presetIndex < 0 || presetIndex >= lib.Count())
            {
                Debug.LogError("ReplacePreset: Invalid index: out of bounds");
                return;
            }

            lib.Move(presetIndex, destPresetIndex, insertAfterDestIndex);
            SaveCurrentLib();
            if (presetsWasReordered != null)
                presetsWasReordered();
        }

        // returns index of newly created preset. -1 if no library to add to.
        public int CreateNewPreset(object presetObject, string presetName)
        {
            T lib = GetCurrentLib();
            if (lib == null)
            {
                Debug.Log("No current library selected!");
                return -1;
            }

            lib.Add(presetObject, presetName);
            SaveCurrentLib();
            if (presetsWasReordered != null)
                presetsWasReordered();

            Repaint();
            OnLayoutChanged();
            return lib.Count() - 1;
        }

        public void SaveCurrentLib()
        {
            T lib = GetCurrentLib();
            if (lib == null)
            {
                Debug.Log("No current library selected!");
                return;
            }
            PresetLibraryManager.instance.SaveLibrary(m_SaveLoadHelper, lib, currentLibraryWithoutExtension);
            InternalEditorUtility.RepaintAllViews();
        }

        public T CreateNewLibrary(string presetLibraryPathWithoutExtension)
        {
            T newLib = PresetLibraryManager.instance.CreateLibrary<T>(m_SaveLoadHelper, presetLibraryPathWithoutExtension);
            if (newLib != null)
            {
                PresetLibraryManager.instance.SaveLibrary(m_SaveLoadHelper, newLib, presetLibraryPathWithoutExtension);
                InternalEditorUtility.RepaintAllViews(); // Needed because we call this function from another window
            }
            return newLib;
        }

        public void RevealCurrentLibrary()
        {
            if (m_PresetLibraryFileLocation == PresetFileLocation.PreferencesFolder)
                EditorUtility.RevealInFinder(Path.GetFullPath(pathWithExtension));
            else
                EditorGUIUtility.PingObject(AssetDatabase.GetMainAssetInstanceID(pathWithExtension));
        }

        internal class PresetContextMenu
        {
            static PresetLibraryEditor<T> s_Caller;
            static int s_PresetIndex;

            static internal void Show(bool isOpenForEdit, int presetIndex, object newPresetObject, PresetLibraryEditor<T> caller)
            {
                s_Caller = caller;
                s_PresetIndex = presetIndex;

                GUIContent replaceText = new GUIContent("Replace");
                GUIContent deleteText = new GUIContent("Delete");
                GUIContent renameText = new GUIContent("Rename");
                GUIContent moveToText = new GUIContent("Move To First");

                GenericMenu menu = new GenericMenu();
                if (isOpenForEdit)
                {
                    menu.AddItem(replaceText, false, new PresetContextMenu().Replace, newPresetObject);
                    menu.AddItem(deleteText, false, new PresetContextMenu().Delete, 0);
                    if (caller.drawLabels)
                        menu.AddItem(renameText, false, new PresetContextMenu().Rename, 0);
                    menu.AddItem(moveToText, false, new PresetContextMenu().MoveToTop, 0);
                }
                else
                {
                    menu.AddDisabledItem(replaceText);
                    menu.AddDisabledItem(deleteText);
                    if (caller.drawLabels)
                        menu.AddDisabledItem(renameText);
                    menu.AddDisabledItem(moveToText);
                }
                menu.ShowAsContext();
            }

            private void Delete(object userData)
            {
                s_Caller.DeletePreset(s_PresetIndex);
            }

            private void Replace(object userData)
            {
                object newPresetObject = userData;
                s_Caller.ReplacePreset(s_PresetIndex, newPresetObject);
            }

            private void Rename(object userData)
            {
                string name = s_Caller.GetCurrentLib().GetName(s_PresetIndex);
                s_Caller.BeginRenaming(name, s_PresetIndex, 0.0f);
            }

            private void MoveToTop(object userData)
            {
                s_Caller.MovePreset(s_PresetIndex, 0, false);
            }
        }
    }
} // UnityEditor
