// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor.AnimatedValues;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;

namespace UnityEditor
{
    internal abstract class ObjectSelectorReceiver : ScriptableObject
    {
        public abstract void OnSelectionChanged(UnityObject selection);
        public abstract void OnSelectionClosed(UnityObject selection);
    }

    internal class ObjectSelector : EditorWindow
    {
        // Styles used in the object selector
        class Styles
        {
            public GUIStyle smallStatus = "ObjectPickerSmallStatus";
            public GUIStyle largeStatus = "ObjectPickerLargeStatus";
            public GUIStyle toolbarBack = "ObjectPickerToolbar";
            public GUIStyle tab = "ObjectPickerTab";
            public GUIStyle bottomResize = "WindowBottomResize";
            public GUIStyle previewBackground = "PopupCurveSwatchBackground"; // TODO: Make dedicated style
            public GUIStyle previewTextureBackground = "ObjectPickerPreviewBackground"; // TODO: Make dedicated style
        }
        Styles m_Styles;

        // Filters
        string          m_RequiredType;
        string          m_SearchFilter;

        // Display state
        bool            m_FocusSearchFilter;
        bool            m_AllowSceneObjects;
        bool            m_IsShowingAssets;
        SavedInt        m_StartGridSize = new SavedInt("ObjectSelector.GridSize", 64);

        // Misc
        internal int    objectSelectorID = 0;
        ObjectSelectorReceiver m_ObjectSelectorReceiver;
        int             m_ModalUndoGroup = -1;
        UnityObject     m_OriginalSelection;
        EditorCache     m_EditorCache;
        GUIView         m_DelegateView;
        PreviewResizer  m_PreviewResizer = new PreviewResizer();
        List<int>       m_AllowedIDs;

        ObjectListAreaState m_ListAreaState;
        ObjectListArea  m_ListArea;
        ObjectTreeForSelector m_ObjectTreeWithSearch = new ObjectTreeForSelector();
        UnityObject m_ObjectBeingEdited;

        // Layout
        const float kMinTopSize = 250;
        const float kMinWidth = 200;
        const float kPreviewMargin = 5;
        const float kPreviewExpandedAreaHeight = 75;
        float m_ToolbarHeight = 44;
        float           m_PreviewSize = 0;
        float           m_TopSize = 0;
        AnimBool m_ShowWidePreview = new AnimBool();
        AnimBool m_ShowOverlapPreview = new AnimBool();

        Rect listPosition
        {
            get
            {
                return new Rect(0, m_ToolbarHeight, position.width, Mathf.Max(0f, m_TopSize - m_ToolbarHeight));
            }
        }

        public List<int> allowedInstanceIDs
        {
            get { return m_AllowedIDs; }
        }

        // get an existing ObjectSelector or create one
        static ObjectSelector s_SharedObjectSelector = null;
        public static ObjectSelector get
        {
            get
            {
                if (s_SharedObjectSelector == null)
                {
                    UnityObject[] objs = Resources.FindObjectsOfTypeAll(typeof(ObjectSelector));
                    if (objs != null && objs.Length > 0)
                        s_SharedObjectSelector = (ObjectSelector)objs[0];
                    if (s_SharedObjectSelector == null)
                        s_SharedObjectSelector = ScriptableObject.CreateInstance<ObjectSelector>();
                }
                return s_SharedObjectSelector;
            }
        }

        public static bool isVisible
        {
            get
            {
                return s_SharedObjectSelector != null;
            }
        }

        bool IsUsingTreeView()
        {
            return m_ObjectTreeWithSearch.IsInitialized();
        }

        int GetSelectedInstanceID()
        {
            int[] selection = IsUsingTreeView() ? m_ObjectTreeWithSearch.GetSelection() : m_ListArea.GetSelection();
            if (selection.Length >= 1)
                return selection[0];
            return 0;
        }

        void OnEnable()
        {
            hideFlags = HideFlags.DontSave;
            m_ShowOverlapPreview.valueChanged.AddListener(Repaint);
            m_ShowOverlapPreview.speed = 1.5f;
            m_ShowWidePreview.valueChanged.AddListener(Repaint);
            m_ShowWidePreview.speed = 1.5f;

            m_PreviewResizer.Init("ObjectPickerPreview");
            m_PreviewSize = m_PreviewResizer.GetPreviewSize(); // Init size

            AssetPreview.ClearTemporaryAssetPreviews();

            SetupPreview();
        }

        void OnDisable()
        {
            if (m_ObjectSelectorReceiver != null)
            {
                m_ObjectSelectorReceiver.OnSelectionClosed(GetCurrentObject());
            }
            SendEvent("ObjectSelectorClosed", false);
            if (m_ListArea != null)
                m_StartGridSize.value = m_ListArea.gridSize;

            Undo.CollapseUndoOperations(m_ModalUndoGroup);

            if (s_SharedObjectSelector == this)
                s_SharedObjectSelector = null;
            if (m_EditorCache != null)
                m_EditorCache.Dispose();

            AssetPreview.ClearTemporaryAssetPreviews();
        }

        public void SetupPreview()
        {
            bool open = PreviewIsOpen();
            bool wide = PreviewIsWide();
            m_ShowOverlapPreview.target = m_ShowOverlapPreview.value = (open && !wide);
            m_ShowWidePreview.target = m_ShowWidePreview.value = (open && wide);
        }

        void ListAreaItemSelectedCallback(bool doubleClicked)
        {
            if (doubleClicked)
            {
                ItemWasDoubleClicked();
            }
            else
            {
                m_FocusSearchFilter = false;
                if (m_ObjectSelectorReceiver != null)
                {
                    m_ObjectSelectorReceiver.OnSelectionChanged(GetCurrentObject());
                }
                SendEvent("ObjectSelectorUpdated", true);
            }
        }

        internal string searchFilter
        {
            get { return m_SearchFilter; }
            set
            {
                m_SearchFilter = value;
                FilterSettingsChanged();
            }
        }

        public ObjectSelectorReceiver objectSelectorReceiver
        {
            get { return m_ObjectSelectorReceiver; }
            set { m_ObjectSelectorReceiver = value; }
        }

        Scene GetSceneFromObject(UnityObject obj)
        {
            var go = obj as GameObject;
            if (go != null)
                return go.scene;

            var component = obj as Component;
            if (component != null)
                return component.gameObject.scene;

            return new Scene();
        }

        void FilterSettingsChanged()
        {
            SearchFilter filter = new SearchFilter();
            filter.SearchFieldStringToFilter(m_SearchFilter);
            if (!string.IsNullOrEmpty(m_RequiredType) && filter.classNames.Length == 0)
            {
                filter.classNames = new[] { m_RequiredType };
            }

            var hierarchyType = m_IsShowingAssets ? HierarchyType.Assets : HierarchyType.GameObjects;

            // We do not support cross scene references so ensure we only show game objects
            // from the same scene as the object being edited is part of.
            if (EditorSceneManager.preventCrossSceneReferences && hierarchyType == HierarchyType.GameObjects && m_ObjectBeingEdited != null)
            {
                var scene = GetSceneFromObject(m_ObjectBeingEdited);
                if (scene.IsValid())
                    filter.scenePaths = new[] { scene.path };
            }

            m_ListArea.Init(listPosition, hierarchyType, filter, true);
        }

        static bool ShouldTreeViewBeUsed(Type type)
        {
            return type == typeof(AudioMixerGroup);
        }

        public void Show(UnityObject obj, System.Type requiredType, SerializedProperty property, bool allowSceneObjects)
        {
            Show(obj, requiredType, property, allowSceneObjects, null);
        }

        private readonly Regex s_MatchPPtrTypeName = new Regex(@"PPtr\<(\w+)\>");

        internal void Show(UnityObject obj, Type requiredType, SerializedProperty property, bool allowSceneObjects, List<int> allowedInstanceIDs)
        {
            m_ObjectSelectorReceiver = null;
            m_AllowSceneObjects = allowSceneObjects;
            m_IsShowingAssets = true;
            m_AllowedIDs = allowedInstanceIDs;

            if (property != null)
            {
                if (requiredType == null)
                {
                    ScriptAttributeUtility.GetFieldInfoFromProperty(property, out requiredType);
                    // case 951876: built-in types do not actually have reflectable fields, so their object types must be extracted from the type string
                    // this works because built-in types will only ever have serialized references to other built-in types, which this window's filter expects as unqualified names
                    if (requiredType == null)
                        m_RequiredType = s_MatchPPtrTypeName.Match(property.type).Groups[1].Value;
                }

                obj = property.objectReferenceValue;
                m_ObjectBeingEdited = property.serializedObject.targetObject;

                // Do not allow to show scene objects if the object being edited is persistent
                if (m_ObjectBeingEdited != null && EditorUtility.IsPersistent(m_ObjectBeingEdited))
                    m_AllowSceneObjects = false;
            }

            // Set which tab should be visible at startup
            if (m_AllowSceneObjects)
            {
                if (obj != null)
                {
                    if (typeof(Component).IsAssignableFrom(obj.GetType()))
                    {
                        obj = ((Component)obj).gameObject;
                    }
                    // Set the right tab visible (so we can see our selection)
                    m_IsShowingAssets = EditorUtility.IsPersistent(obj);
                }
                else
                {
                    m_IsShowingAssets = (requiredType != typeof(GameObject) && !typeof(Component).IsAssignableFrom(requiredType));
                }
            }
            else
            {
                m_IsShowingAssets = true;
            }

            // Set member variables
            m_DelegateView = GUIView.current;
            // type filter requires unqualified names for built-in types, but will prioritize them over user types, so ensure user types are namespace-qualified
            if (requiredType != null)
                m_RequiredType = typeof(ScriptableObject).IsAssignableFrom(requiredType) || typeof(MonoBehaviour).IsAssignableFrom(requiredType) ? requiredType.FullName : requiredType.Name;
            m_SearchFilter = "";
            m_OriginalSelection = obj;
            m_ModalUndoGroup = Undo.GetCurrentGroup();

            // Freeze to prevent flicker on OSX.
            // Screen will be updated again when calling
            // SetFreezeDisplay(false) further down.
            ContainerWindow.SetFreezeDisplay(true);

            ShowWithMode(ShowMode.AuxWindow);
            titleContent = new GUIContent("Select " + (requiredType == null ? m_RequiredType : requiredType.Name));

            // Deal with window size
            Rect p = m_Parent.window.position;
            p.width = EditorPrefs.GetFloat("ObjectSelectorWidth", 200);
            p.height = EditorPrefs.GetFloat("ObjectSelectorHeight", 390);
            position = p;
            minSize = new Vector2(kMinWidth, kMinTopSize + kPreviewExpandedAreaHeight + 2 * kPreviewMargin);
            maxSize = new Vector2(10000, 10000);
            SetupPreview();

            // Focus
            Focus();
            ContainerWindow.SetFreezeDisplay(false);

            m_FocusSearchFilter = true;

            // Add after unfreezing display because AuxWindowManager.cpp assumes that aux windows are added after we get 'got/lost'- focus calls.
            m_Parent.AddToAuxWindowList();

            // Initial selection
            int initialSelection = obj != null ? obj.GetInstanceID() : 0;
            if (property != null && property.hasMultipleDifferentValues)
                initialSelection = 0; // don't select anything on multi selection

            if (ShouldTreeViewBeUsed(requiredType))
            {
                m_ObjectTreeWithSearch.Init(position, this, CreateAndSetTreeView, TreeViewSelection, ItemWasDoubleClicked, initialSelection, 0);
            }
            else
            {
                // To frame the selected item we need to wait to initialize the search until our window has been setup
                InitIfNeeded();
                m_ListArea.InitSelection(new[] { initialSelection });
                if (initialSelection != 0)
                    m_ListArea.Frame(initialSelection, true, false);
            }
        }

        void ItemWasDoubleClicked()
        {
            Close();
            GUIUtility.ExitGUI();
        }

        // TreeView Section

        void CreateAndSetTreeView(ObjectTreeForSelector.TreeSelectorData data)
        {
            TreeViewForAudioMixerGroup.CreateAndSetTreeView(data);
        }

        void TreeViewSelection(TreeViewItem item)
        {
            if (m_ObjectSelectorReceiver != null)
            {
                m_ObjectSelectorReceiver.OnSelectionChanged(GetCurrentObject());
            }
            SendEvent("ObjectSelectorUpdated", true);
        }

        // Grid Section

        void InitIfNeeded()
        {
            if (m_ListAreaState == null)
                m_ListAreaState = new ObjectListAreaState(); // is serialized

            if (m_ListArea == null)
            {
                m_ListArea = new ObjectListArea(m_ListAreaState, this, true);
                m_ListArea.allowDeselection = false;
                m_ListArea.allowDragging = false;
                m_ListArea.allowFocusRendering = false;
                m_ListArea.allowMultiSelect = false;
                m_ListArea.allowRenaming = false;
                m_ListArea.allowBuiltinResources = true;
                m_ListArea.repaintCallback += Repaint;
                m_ListArea.itemSelectedCallback += ListAreaItemSelectedCallback;
                m_ListArea.gridSize = m_StartGridSize.value;

                FilterSettingsChanged();
            }
        }

        public static UnityObject GetCurrentObject()
        {
            return EditorUtility.InstanceIDToObject(ObjectSelector.get.GetSelectedInstanceID());
        }

        // This is the public Object that the inspector might revert to
        public static UnityObject GetInitialObject()
        {
            return ObjectSelector.get.m_OriginalSelection;
        }

        // This is our search field
        void SearchArea()
        {
            GUI.Label(new Rect(0, 0, position.width, m_ToolbarHeight), GUIContent.none, m_Styles.toolbarBack);

            // ESC clears search field and removes it's focus. But if we get an esc event we only want to clear search field.
            // So we need special handling afterwards.
            bool wasEscape = Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape;

            GUI.SetNextControlName("SearchFilter");
            string searchFilter = EditorGUI.SearchField(new Rect(5, 5, position.width - 10, 15), m_SearchFilter);

            if (wasEscape && Event.current.type == EventType.Used)
            {
                // If we hit esc and the string WAS empty, it's an actual cancel event.
                if (m_SearchFilter == "")
                    Cancel();

                // Otherwise the string has been cleared and focus has been lost. We don't have anything else to recieve focus, so we want to refocus the search field.
                m_FocusSearchFilter = true;
            }

            if (searchFilter != m_SearchFilter || m_FocusSearchFilter)
            {
                m_SearchFilter = searchFilter;
                FilterSettingsChanged();
                Repaint();
            }

            if (m_FocusSearchFilter)
            {
                EditorGUI.FocusTextInControl("SearchFilter");
                m_FocusSearchFilter = false;
            }

            GUI.changed = false;

            // TAB BAR
            GUILayout.BeginArea(new Rect(0, 26, position.width, m_ToolbarHeight - 26));
            GUILayout.BeginHorizontal();

            // Asset Tab
            bool showAssets = GUILayout.Toggle(m_IsShowingAssets, "Assets", m_Styles.tab);
            if (!m_IsShowingAssets && showAssets)
                m_IsShowingAssets = true;


            // The Scene Tab
            if (!m_AllowSceneObjects)
            {
                GUI.enabled = false;
                GUI.color = new Color(1, 1, 1, 0);
            }

            bool showingSceneTab = !m_IsShowingAssets;
            showingSceneTab = GUILayout.Toggle(showingSceneTab, "Scene", m_Styles.tab);
            if (m_IsShowingAssets && showingSceneTab)
                m_IsShowingAssets = false;


            if (!m_AllowSceneObjects)
            {
                GUI.color = new Color(1, 1, 1, 1);
                GUI.enabled = true;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            if (GUI.changed)
                FilterSettingsChanged();

            if (m_ListArea.CanShowThumbnails())
            {
                EditorGUI.BeginChangeCheck();
                int newGridSize = (int)GUI.HorizontalSlider(new Rect(position.width - 60, 26, 55, m_ToolbarHeight - 28), m_ListArea.gridSize, m_ListArea.minGridSize, m_ListArea.maxGridSize);
                if (EditorGUI.EndChangeCheck())
                {
                    m_ListArea.gridSize = newGridSize;
                }
            }
        }

        void OnInspectorUpdate()
        {
            if (m_ListArea != null && AssetPreview.HasAnyNewPreviewTexturesAvailable(m_ListArea.GetAssetPreviewManagerID()))
                Repaint();
        }

        // This is the preview area at the bottom of the screen
        void PreviewArea()
        {
            GUI.Box(new Rect(0, m_TopSize, position.width, m_PreviewSize), "", m_Styles.previewBackground);

            if (m_ListArea.GetSelection().Length == 0)
                return;

            EditorWrapper p = null;
            UnityObject selectedObject = GetCurrentObject();
            if (m_PreviewSize < kPreviewExpandedAreaHeight)
            {
                // Get info string
                string s;
                if (selectedObject != null)
                {
                    p = m_EditorCache[selectedObject];
                    string typeName = ObjectNames.NicifyVariableName(selectedObject.GetType().Name);
                    if (p != null)
                        s = p.name + " (" + typeName + ")";
                    else
                        s = selectedObject.name + " (" + typeName + ")";

                    s += "      " + AssetDatabase.GetAssetPath(selectedObject);
                }
                else
                    s = "None";

                LinePreview(s, selectedObject, p);
            }
            else
            {
                if (m_EditorCache == null)
                    m_EditorCache = new EditorCache(EditorFeatures.PreviewGUI);

                // Get info string
                string s;
                if (selectedObject != null)
                {
                    p = m_EditorCache[selectedObject];
                    string typeName = ObjectNames.NicifyVariableName(selectedObject.GetType().Name);
                    if (p != null)
                    {
                        s = p.GetInfoString();
                        if (s != "")
                            s = p.name + "\n" + typeName + "\n" + s;
                        else
                            s = p.name + "\n" + typeName;
                    }
                    else
                    {
                        s = selectedObject.name + "\n" + typeName;
                    }

                    s += "\n" + AssetDatabase.GetAssetPath(selectedObject);
                }
                else
                    s = "None";

                // Make previews
                if (m_ShowWidePreview.faded != 0.0f)
                {
                    GUI.color = new Color(1, 1, 1, m_ShowWidePreview.faded);
                    WidePreview(m_PreviewSize, s, selectedObject, p);
                }
                if (m_ShowOverlapPreview.faded != 0.0f)
                {
                    GUI.color = new Color(1, 1, 1, m_ShowOverlapPreview.faded);
                    OverlapPreview(m_PreviewSize, s, selectedObject, p);
                }
                GUI.color = Color.white;
                m_EditorCache.CleanupUntouchedEditors();
            }
        }

        void WidePreview(float actualSize, string s, UnityObject o, EditorWrapper p)
        {
            float margin = kPreviewMargin;
            Rect previewRect = new Rect(margin, m_TopSize + margin, actualSize - margin * 2, actualSize - margin * 2);

            Rect labelRect = new Rect(m_PreviewSize + 3, m_TopSize + (m_PreviewSize - kPreviewExpandedAreaHeight) * 0.5f, m_Parent.window.position.width - m_PreviewSize - 3 - margin, kPreviewExpandedAreaHeight);

            if (p != null && p.HasPreviewGUI())
                p.OnPreviewGUI(previewRect, m_Styles.previewTextureBackground);
            else if (o != null)
                DrawObjectIcon(previewRect, m_ListArea.m_SelectedObjectIcon);

            if (EditorGUIUtility.isProSkin)
                EditorGUI.DropShadowLabel(labelRect, s, m_Styles.smallStatus);
            else
                GUI.Label(labelRect, s, m_Styles.smallStatus);
        }

        void OverlapPreview(float actualSize, string s, UnityObject o, EditorWrapper p)
        {
            float margin = kPreviewMargin;
            Rect previewRect = new Rect(margin, m_TopSize + margin, position.width - margin * 2, actualSize - margin * 2);

            if (p != null && p.HasPreviewGUI())
                p.OnPreviewGUI(previewRect, m_Styles.previewTextureBackground);
            else if (o != null)
                DrawObjectIcon(previewRect, m_ListArea.m_SelectedObjectIcon);

            if (EditorGUIUtility.isProSkin)
                EditorGUI.DropShadowLabel(previewRect, s, m_Styles.largeStatus);
            else
                EditorGUI.DoDropShadowLabel(previewRect, EditorGUIUtility.TempContent(s), m_Styles.largeStatus, .3f);
        }

        void LinePreview(string s, UnityObject o, EditorWrapper p)
        {
            if (m_ListArea.m_SelectedObjectIcon != null)
                GUI.DrawTexture(new Rect(2, (int)(m_TopSize + 2), 16, 16), m_ListArea.m_SelectedObjectIcon, ScaleMode.StretchToFill);
            Rect labelRect = new Rect(20, m_TopSize + 1, position.width - 22, 18);
            if (EditorGUIUtility.isProSkin)
                EditorGUI.DropShadowLabel(labelRect, s, m_Styles.smallStatus);
            else
                GUI.Label(labelRect, s, m_Styles.smallStatus);
        }

        void DrawObjectIcon(Rect position, Texture icon)
        {
            if (icon == null)
                return;
            int size = Mathf.Min((int)position.width, (int)position.height);
            if (size >= icon.width * 2)
                size = icon.width * 2;

            FilterMode temp = icon.filterMode;
            icon.filterMode = FilterMode.Point;
            GUI.DrawTexture(new Rect(position.x + ((int)position.width - size) / 2, position.y + ((int)position.height - size) / 2, size, size), icon, ScaleMode.ScaleToFit);
            icon.filterMode = temp;
        }

        // Resize the preview area
        void ResizeBottomPartOfWindow()
        {
            GUI.changed = false;

            // Handle preview size
            m_PreviewSize = m_PreviewResizer.ResizeHandle(position, kPreviewExpandedAreaHeight + kPreviewMargin * 2 - 20, kMinTopSize + 20, 20) + 20;
            m_TopSize = position.height - m_PreviewSize;

            bool open = PreviewIsOpen();
            bool wide = PreviewIsWide();
            m_ShowOverlapPreview.target = open && !wide;
            m_ShowWidePreview.target = open && wide;
        }

        bool PreviewIsOpen()
        {
            return m_PreviewSize >= 32 + kPreviewMargin;
        }

        bool PreviewIsWide()
        {
            return position.width - m_PreviewSize - kPreviewMargin > Mathf.Min(m_PreviewSize * 2 - 20, 256);
        }

        // send an event to the delegate view (the view that called us)
        void SendEvent(string eventName, bool exitGUI)
        {
            if (m_DelegateView)
            {
                Event e = EditorGUIUtility.CommandEvent(eventName);

                try
                {
                    m_DelegateView.SendEvent(e);
                }
                finally
                {
                }
                if (exitGUI)
                    GUIUtility.ExitGUI();
            }
        }

        void HandleKeyboard()
        {
            // Handle events on the object selector window
            if (Event.current.type != EventType.KeyDown)
                return;

            switch (Event.current.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    Close();
                    GUI.changed = true;
                    GUIUtility.ExitGUI();
                    break;
                default:
                    //Debug.Log ("Unhandled " + Event.current.keyCode);
                    return;
            }
            Event.current.Use();
            GUI.changed = true;
        }

        void Cancel()
        {
            // Undo changes we have done in the ObjectSelector
            Undo.RevertAllDownToGroup(m_ModalUndoGroup);

            // Clear selection so that object field doesn't grab it
            m_ListArea.InitSelection(new int[0]);
            m_ObjectTreeWithSearch.Clear();

            Close();
            GUI.changed = true;
            GUIUtility.ExitGUI();
        }

        void OnDestroy()
        {
            if (m_ListArea != null)
                m_ListArea.OnDestroy();

            m_ObjectTreeWithSearch.Clear();
        }

        void OnGUI()
        {
            HandleKeyboard();

            if (m_ObjectTreeWithSearch.IsInitialized())
                OnObjectTreeGUI();
            else
                OnObjectGridGUI();

            // Must be after gui so search field can use the Escape event if it has focus
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                Cancel();
        }

        void OnObjectTreeGUI()
        {
            m_ObjectTreeWithSearch.OnGUI(new Rect(0, 0, position.width, position.height));
        }

        void OnObjectGridGUI()
        {
            InitIfNeeded();

            // Initialize m_Styles
            if (m_Styles == null)
                m_Styles = new Styles();

            if (m_EditorCache == null)
                m_EditorCache = new EditorCache(EditorFeatures.PreviewGUI);

            // Handle window/preview stuff
            ResizeBottomPartOfWindow();

            Rect p = position;
            EditorPrefs.SetFloat("ObjectSelectorWidth", p.width);
            EditorPrefs.SetFloat("ObjectSelectorHeight", p.height);

            GUI.BeginGroup(new Rect(0, 0, position.width, position.height), GUIContent.none);

            // We want to check for arrow key presses first of all to allow arrow navigation
            // of grid/list even when search field has focus (which it has when the window opens).
            // That means arrows won't affect the cursor position in the text field
            // but that's by design (was the same in 3.4).
            m_ListArea.HandleKeyboard(false);

            SearchArea();
            GridListArea();
            PreviewArea();

            GUI.EndGroup();

            // overlay preview resize widget
            GUI.Label(new Rect(position.width * .5f - 16, position.height - m_PreviewSize + 2, 32, m_Styles.bottomResize.fixedHeight), GUIContent.none, m_Styles.bottomResize);
        }

        void GridListArea()
        {
            int listKeyboardControlID = GUIUtility.GetControlID(FocusType.Keyboard);
            m_ListArea.OnGUI(listPosition, listKeyboardControlID);
        }
    }
}
