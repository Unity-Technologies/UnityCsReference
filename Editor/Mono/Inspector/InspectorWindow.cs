// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.AdvancedDropdown;
using UnityEditor.Experimental.AssetImporters;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEditorInternal;
using UnityEditorInternal.VersionControl;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Scripting;

// includes specific to UIElements/IMGui
using UnityEngine.Profiling;

using Object = UnityEngine.Object;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Inspector", useTypeNameAsIconName = true)]
    internal class InspectorWindow : EditorWindow, IHasCustomMenu
    {
        internal Vector2 m_ScrollPosition;
        internal InspectorMode   m_InspectorMode = InspectorMode.Normal;

        static readonly List<InspectorWindow> m_AllInspectors = new List<InspectorWindow>();
        static bool s_AllOptimizedGUIBlocksNeedsRebuild;


        const float kBottomToolbarHeight = 17f;
        const float kAddComponentButtonHeight = 45f;
        internal const int kInspectorPaddingLeft = 4 + 10;
        internal const int kInspectorPaddingRight = 4;

        private const long delayRepaintWhilePlayingAnimation = 150; // Delay between repaints in milliseconds while playing animation
        private long s_LastUpdateWhilePlayingAnimation = 0;

        bool m_ResetKeyboardControl;
        protected ActiveEditorTracker m_Tracker;

        [SerializeField]
        protected List<Object> m_ObjectsLockedBeforeSerialization = new List<Object>();

        [SerializeField]
        protected List<int> m_InstanceIDsLockedBeforeSerialization = new List<int>();

        [SerializeField]
        EditorGUIUtility.EditorLockTrackerWithActiveEditorTracker m_LockTracker = new EditorGUIUtility.EditorLockTrackerWithActiveEditorTracker();

        Editor m_LastInteractedEditor;
        bool m_IsOpenForEdit = false;
        int m_LastInitialEditorInstanceID;
        Component[] m_ComponentsInPrefabSource;
        HashSet<Component> m_RemovedComponents;

        [SerializeField]
        PreviewResizer m_PreviewResizer = new PreviewResizer();
        [SerializeField]
        PreviewWindow m_PreviewWindow;

        LabelGUI m_LabelGUI = new LabelGUI();
        AssetBundleNameGUI m_AssetBundleNameGUI = new AssetBundleNameGUI();

        TypeSelectionList m_TypeSelectionList = null;

        private double m_lastRenderedTime;
        private bool   m_InvalidateGUIBlockCache = true;

        private List<IPreviewable> m_Previews;
        private Dictionary<Type, List<Type>> m_PreviewableTypes;
        private IPreviewable m_SelectedPreview;

        private EditorDragging editorDragging;



        internal static class Styles
        {
            public static readonly GUIStyle preToolbar = "preToolbar";
            public static readonly GUIStyle preToolbar2 = "preToolbar2";
            public static readonly GUIStyle preDropDown = "preDropDown";
            public static readonly GUIStyle dragHandle = "RL DragHandle";
            public static readonly GUIStyle lockButton = "IN LockButton";
            public static readonly GUIStyle insertionMarker = "InsertionMarker";
            public static readonly GUIContent preTitle = EditorGUIUtility.TrTextContent("Preview");
            public static readonly GUIContent labelTitle = EditorGUIUtility.TrTextContent("Asset Labels");
            public static readonly GUIContent addComponentLabel = EditorGUIUtility.TrTextContent("Add Component");
            public static GUIStyle preBackground = "preBackground";
            public static GUIStyle addComponentArea = EditorStyles.inspectorTitlebar;
            public static GUIStyle addComponentButtonStyle = "AC Button";
            public static GUIStyle previewMiniLabel = EditorStyles.whiteMiniLabel;
            public static GUIStyle typeSelection = "IN TypeSelection";
            public static GUIStyle lockedHeaderButton = "preButton";
            public static GUIStyle stickyNote = "VCS_StickyNote";
            public static GUIStyle stickyNoteArrow = "VCS_StickyNoteArrow";
            public static GUIStyle stickyNotePerforce = "VCS_StickyNoteP4";
            public static GUIStyle stickyNoteLabel = "VCS_StickyNoteLabel";
            public static readonly GUIContent VCS_NotConnectedMessage = EditorGUIUtility.TrTextContent("VCS Plugin {0} is enabled but not connected");
            public static readonly string objectDisabledModuleWarningFormat = L10n.Tr(
                "The built-in package '{0}', which implements this component type, has been disabled in Package Manager. This object will be removed in play mode and from any builds you make."
            );
            public static readonly string objectDisabledModuleWithDependencyWarningFormat = L10n.Tr(
                "The built-in package '{0}', which is required by the package '{1}', which implements this component type, has been disabled in Package Manager. This object will be removed in play mode and from any builds you make."
            );
        }

        internal InspectorWindow()
        {
            editorDragging = new EditorDragging(this);
        }

        void Awake()
        {
            if (!m_AllInspectors.Contains(this))
                m_AllInspectors.Add(this);
        }

        void OnDestroy()
        {
            if (m_PreviewWindow != null)
                m_PreviewWindow.Close();
            if (m_Tracker != null && !m_Tracker.Equals(ActiveEditorTracker.sharedTracker))
                m_Tracker.Destroy();
        }

        protected virtual void OnEnable()
        {
            RefreshTitle();
            minSize = new Vector2(275, 50);

            AddInspectorWindow(this);

            m_PreviewResizer.Init("InspectorPreview");
            m_LabelGUI.OnEnable();

            // ensure tracker is valid here in case domain is reloaded before first time inspector is drawn
            // fixes case 829182
            CreateTracker();

            RestoreLockStateFromSerializedData();

            if (m_LockTracker == null)
            {
                m_LockTracker = new EditorGUIUtility.EditorLockTrackerWithActiveEditorTracker();
            }
            m_LockTracker.tracker = tracker;
            m_LockTracker.lockStateChanged.AddListener(LockStateChanged);


            EditorApplication.projectWasLoaded += OnProjectWasLoaded;
        }


        private void OnProjectWasLoaded()
        {
            // EditorApplication.projectWasLoaded, which acalls this, fires after OnEnabled
            // therefore the logic in OnEnabled already tried to de-serialize the locked opbjects, including those it only had InstanceIDs of
            // This needs to get fixed here as the InstanceIDs have been reshuffled with the new session and could resolving to random Objects
            if (m_InstanceIDsLockedBeforeSerialization.Count > 0)
            {
                // Game objects will have new instanceIDs in a new Unity session, so take out all objects that where reconstructed from InstanceIDs
                for (int i = m_InstanceIDsLockedBeforeSerialization.Count - 1; i >= 0; i--)
                {
                    for (int j = m_ObjectsLockedBeforeSerialization.Count - 1; j >= 0; j--)
                    {
                        if (m_ObjectsLockedBeforeSerialization[j] == null || m_ObjectsLockedBeforeSerialization[j].GetInstanceID() == m_InstanceIDsLockedBeforeSerialization[i])
                        {
                            m_ObjectsLockedBeforeSerialization.RemoveAt(j);
                            break;
                        }
                    }
                }
                m_InstanceIDsLockedBeforeSerialization.Clear();
                RestoreLockStateFromSerializedData();
            }
        }

        protected virtual void OnDisable()
        {
            RemoveInspectorWindow(this);
            m_LockTracker?.lockStateChanged.RemoveListener(LockStateChanged);

            EditorApplication.projectWasLoaded -= OnProjectWasLoaded;
        }

        void OnLostFocus()
        {
            m_LabelGUI.OnLostFocus();
        }

        static internal void RepaintAllInspectors()
        {
            foreach (var win in m_AllInspectors)
                win.Repaint();
        }

        internal static List<InspectorWindow> GetInspectors()
        {
            return m_AllInspectors;
        }

        [UsedByNativeCode]
        static private void RedrawFromNative()
        {
        }

        void OnSelectionChange()
        {
            m_Previews = null;
            m_SelectedPreview = null;
            m_TypeSelectionList = null;

            if (m_Parent != null) // parent may be null in some situations (case 970700, 851988)
                m_Parent.ClearKeyboardControl();
            ScriptAttributeUtility.ClearGlobalCache();
            Repaint();
        }


        static internal InspectorWindow[] GetAllInspectorWindows()
        {
            return m_AllInspectors.ToArray();
        }

        private void OnInspectorUpdate()
        {
            // Check if scripts have changed without calling set dirty
            tracker.VerifyModifiedMonoBehaviours();

            if (!tracker.isDirty || !ReadyToRepaint())
                return;

            Repaint();
        }

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(EditorGUIUtility.TrTextContent("Normal"), m_InspectorMode == InspectorMode.Normal, SetNormal);
            menu.AddItem(EditorGUIUtility.TrTextContent("Debug"), m_InspectorMode == InspectorMode.Debug, SetDebug);
            if (Unsupported.IsDeveloperMode())
                menu.AddItem(EditorGUIUtility.TrTextContent("Debug-Internal"), m_InspectorMode == InspectorMode.DebugInternal, SetDebugInternal);

            menu.AddSeparator(String.Empty);
            m_LockTracker.AddItemsToMenu(menu);
        }

        void RefreshTitle()
        {
            string iconName = "UnityEditor.InspectorWindow";
            if (m_InspectorMode == InspectorMode.Normal)
                titleContent = EditorGUIUtility.TrTextContentWithIcon("Inspector", iconName);
            else
                titleContent = EditorGUIUtility.TrTextContentWithIcon("Debug", iconName);
        }

        void SetMode(InspectorMode mode)
        {
            m_InspectorMode = mode;
            RefreshTitle();
            tracker.inspectorMode = mode;
            m_ResetKeyboardControl = true;
        }

        void SetDebug()
        {
            SetMode(InspectorMode.Debug);
        }

        void SetNormal()
        {
            SetMode(InspectorMode.Normal);
        }

        void SetDebugInternal()
        {
            SetMode(InspectorMode.DebugInternal);
        }

        public bool isLocked
        {
            get
            {
                //this makes sure the getter for InspectorWindow.tracker gets called and creates an ActiveEditorTracker if needed
                m_LockTracker.tracker = tracker;
                return m_LockTracker.isLocked;
            }
            set
            {
                //this makes sure the getter for InspectorWindow.tracker gets called and creates an ActiveEditorTracker if needed
                m_LockTracker.tracker = tracker;
                m_LockTracker.isLocked = value;
            }
        }

        static void DoInspectorDragAndDrop(Rect rect, Object[] targets)
        {
            if (!Dragging(rect))
                return;

            DragAndDrop.visualMode = InternalEditorUtility.InspectorWindowDrag(targets, Event.current.type == EventType.DragPerform);

            if (Event.current.type == EventType.DragPerform)
                DragAndDrop.AcceptDrag();
        }

        private static bool Dragging(Rect rect)
        {
            return (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform) && rect.Contains(Event.current.mousePosition);
        }

        internal ActiveEditorTracker tracker
        {
            get
            {
                CreateTracker();
                return m_Tracker;
            }
        }

        protected virtual void CreateTracker()
        {
            if (m_Tracker != null)
            {
                // Ensure that inspector mode
                // This shouldn't be necessary but there are some non-reproducable bugs objects showing up as not able to multi-edit
                // because this state goes out of sync.
                m_Tracker.inspectorMode = m_InspectorMode;
                return;
            }

            var sharedTracker = ActiveEditorTracker.sharedTracker;
            bool sharedTrackerInUse = m_AllInspectors.Any(i => i.m_Tracker != null && i.m_Tracker.Equals(sharedTracker));
            m_Tracker = sharedTrackerInUse ? new ActiveEditorTracker() : ActiveEditorTracker.sharedTracker;
            m_Tracker.inspectorMode = m_InspectorMode;
            m_Tracker.RebuildIfNecessary();
        }

        void OnTrackerRebuilt()
        {
            ExtractPrefabComponents();
        }

        void ExtractPrefabComponents()
        {
            m_LastInitialEditorInstanceID = m_Tracker.activeEditors[0].GetInstanceID();

            m_ComponentsInPrefabSource = null;
            if (m_RemovedComponents == null)
                m_RemovedComponents = new HashSet<Component>();
            m_RemovedComponents.Clear();

            if (m_Tracker.activeEditors.Length == 0)
                return;
            if (m_Tracker.activeEditors[0].targets.Length != 1)
                return;
            GameObject go = m_Tracker.activeEditors[0].target as GameObject;
            if (go == null)
                return;
            GameObject sourceGo = PrefabUtility.GetCorrespondingObjectFromSource(go);
            if (sourceGo == null)
                return;

            m_ComponentsInPrefabSource = sourceGo.GetComponents<Component>();
            var removedComponentsList = PrefabUtility.GetRemovedComponents(PrefabUtility.GetOutermostPrefabInstanceRoot(go));
            for (int i = 0; i < removedComponentsList.Count; i++)
            {
                m_RemovedComponents.Add(removedComponentsList[i].assetComponent);
            }
        }

        protected virtual void CreatePreviewables()
        {
            if (m_Previews != null)
                return;

            m_Previews = new List<IPreviewable>();

            var activeEditors = tracker.activeEditors;
            if (activeEditors.Length == 0)
                return;

            if (EditorStyles.s_Current == null)
            {
                EditorStyles.UpdateSkinCache();
            }

            foreach (var editor in activeEditors)
            {
                IEnumerable<IPreviewable> previews = GetPreviewsForType(editor);
                foreach (var preview in previews)
                {
                    m_Previews.Add(preview);
                }
            }
        }

        private Dictionary<Type, List<Type>> GetPreviewableTypes()
        {
            // We initialize this list once per InspectorWindow, instead of globally.
            // This means that if the user is debugging an IPreviewable structure,
            // the InspectorWindow can be closed and reopened to refresh this list.
            //
            if (m_PreviewableTypes == null)
            {
                InspectorWindowUtils.GetPreviewableTypes(out m_PreviewableTypes);
            }

            return m_PreviewableTypes;
        }

        private IEnumerable<IPreviewable> GetPreviewsForType(Editor editor)
        {
            List<IPreviewable> previews = new List<IPreviewable>();

            // Retrieve the type we are looking for.
            if (editor == null || editor.target == null)
                return previews;
            Type targetType = editor.target.GetType();
            if (!GetPreviewableTypes().ContainsKey(targetType))
                return previews;

            var previewerList = GetPreviewableTypes()[targetType];
            if (previewerList == null)
                return previews;

            foreach (var previewerType in previewerList)
            {
                var preview = Activator.CreateInstance(previewerType) as IPreviewable;
                preview.Initialize(editor.targets);
                previews.Add(preview);
            }
            return previews;
        }

        protected virtual void ShowButton(Rect r)
        {
            m_LockTracker.ShowButton(r, Styles.lockButton);
        }

        private void LockStateChanged(bool lockeState)
        {
            if (lockeState)
            {
                PrepareLockedObjectsForSerialization();
            }
            else
            {
                ClearSerializedLockedObjects();
            }

            tracker.RebuildIfNecessary();

        }

        static internal InspectorWindow s_CurrentInspectorWindow;

        protected virtual void OnGUI()
        {
            CreatePreviewables();
            FlushAllOptimizedGUIBlocksIfNeeded();


            ResetKeyboardControl();

            m_ScrollPosition = EditorGUILayout.BeginVerticalScrollView(m_ScrollPosition);
            {
                if (Event.current.type == EventType.Repaint)
                    tracker.ClearDirty();

                s_CurrentInspectorWindow = this;
                Editor[] editors = tracker.activeEditors;

                Profiler.BeginSample("InspectorWindow.DrawEditors()");
                DrawEditors(editors);
                Profiler.EndSample();


                if (tracker.hasComponentsWhichCannotBeMultiEdited)
                {
                    if (editors.Length == 0 && !tracker.isLocked && Selection.objects.Length > 0)
                    {
                        DrawSelectionPickerList();
                    }
                    else
                    {
                        // Visually separates the Add Component button from the existing components
                        Rect lineRect = GUILayoutUtility.GetRect(10, 4, EditorStyles.inspectorTitlebar);
                        if (Event.current.type == EventType.Repaint)
                            DrawSplitLine(lineRect.y);

                        GUILayout.Label(
                            "Components that are only on some of the selected objects cannot be multi-edited.",
                            EditorStyles.helpBox);

                        GUILayout.Space(4);
                    }
                }

                s_CurrentInspectorWindow = null;
                EditorGUI.indentLevel = 0;

                AddComponentButton(tracker.activeEditors);

                GUI.enabled = true;
                CheckDragAndDrop(tracker.activeEditors);
                MoveFocusOnKeyPress();
            }
            EditorGUILayout.EndScrollView();

            Profiler.BeginSample("InspectorWindow.DrawPreviewAndLabels");
            DrawPreviewAndLabels();
            Profiler.EndSample();

            if (tracker.activeEditors.Length > 0)
            {
                Editor assetEditor = InspectorWindowUtils.GetFirstNonImportInspectorEditor(tracker.activeEditors);
                DrawVCSShortInfo(this, assetEditor);
            }
        }


        internal virtual Editor GetLastInteractedEditor()
        {
            return m_LastInteractedEditor;
        }

        internal IPreviewable GetEditorThatControlsPreview(IPreviewable[] editors)
        {
            if (editors.Length == 0)
                return null;

            if (m_SelectedPreview != null)
            {
                return m_SelectedPreview;
            }

            // Find last interacted editor, if not found check if we had an editor of similar type,
            // if not found use first editor that can show a preview otherwise return null.

            IPreviewable lastInteractedEditor = GetLastInteractedEditor();
            Type lastType = lastInteractedEditor?.GetType();

            IPreviewable firstEditorThatHasPreview = null;
            IPreviewable similarEditorAsLast = null;
            foreach (IPreviewable e in editors)
            {
                if (e == null || e.target == null)
                    continue;

                // If target is an asset, but not the same asset as the asset
                // of the first editor, then ignore it. This will prevent showing
                // preview of materials attached to a GameObject but should cover
                // future use cases as well.
                if (EditorUtility.IsPersistent(e.target) &&
                    AssetDatabase.GetAssetPath(e.target) != AssetDatabase.GetAssetPath(editors[0].target))
                    continue;

                // If main editor is an asset importer editor and this is an editor of the imported object, ignore.
                if (editors[0] is AssetImporterEditor && !(e is AssetImporterEditor))
                    continue;

                if (e.HasPreviewGUI())
                {
                    if (e == lastInteractedEditor)
                        return e;

                    if (similarEditorAsLast == null && e.GetType() == lastType)
                        similarEditorAsLast = e;

                    if (firstEditorThatHasPreview == null)
                        firstEditorThatHasPreview = e;
                }
            }

            if (similarEditorAsLast != null)
                return similarEditorAsLast;

            if (firstEditorThatHasPreview != null)
                return firstEditorThatHasPreview;

            // Found no valid editor
            return null;
        }

        internal IPreviewable[] GetEditorsWithPreviews(Editor[] editors)
        {
            IList<IPreviewable> editorsWithPreview = new List<IPreviewable>();

            int i = -1;
            foreach (Editor e in editors)
            {
                ++i;
                if (e.target == null)
                    continue;

                // If target is an asset, but not the same asset as the asset
                // of the first editor, then ignore it. This will prevent showing
                // preview of materials attached to a GameObject but should cover
                // future use cases as well.
                if (EditorUtility.IsPersistent(e.target) &&
                    AssetDatabase.GetAssetPath(e.target) != AssetDatabase.GetAssetPath(editors[0].target))
                    continue;

                if (!EditorUtility.IsPersistent(editors[0].target) && EditorUtility.IsPersistent(e.target))
                    continue;

                if (ShouldCullEditor(editors, i))
                    continue;

                // If main editor is an asset importer editor and this is an editor of the imported object, ignore.
                if (editors[0] is AssetImporterEditor && !(e is AssetImporterEditor))
                    continue;

                if (e.HasPreviewGUI())
                {
                    editorsWithPreview.Add(e);
                }
            }

            foreach (var previewable in m_Previews)
            {
                if (previewable.HasPreviewGUI())
                    editorsWithPreview.Add(previewable);
            }

            return editorsWithPreview.ToArray();
        }

        internal Object GetInspectedObject()
        {
            Editor editor = InspectorWindowUtils.GetFirstNonImportInspectorEditor(tracker.activeEditors);
            if (editor == null)
                return null;
            return editor.target;
        }

        private static void MoveFocusOnKeyPress()
        {
            var key = Event.current.keyCode;

            if (Event.current.type != EventType.KeyDown || (key != KeyCode.DownArrow && key != KeyCode.UpArrow && key != KeyCode.Tab))
                return;

            if (key != KeyCode.Tab)
                EditorGUIUtility.MoveFocusAndScroll(key == KeyCode.DownArrow);
            else
                EditorGUIUtility.ScrollForTabbing(!Event.current.shift);

            Event.current.Use();
        }

        private void ResetKeyboardControl()
        {
            if (m_ResetKeyboardControl)
            {
                GUIUtility.keyboardControl = 0;
                m_ResetKeyboardControl = false;
            }
        }

        private void CheckDragAndDrop(Editor[] editors)
        {
            Rect remainingRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandHeight(true));

            if (remainingRect.Contains(Event.current.mousePosition))
            {
                Editor editor = InspectorWindowUtils.GetFirstNonImportInspectorEditor(editors);
                if (editor != null)
                    DoInspectorDragAndDrop(remainingRect, editor.targets);

                if (Event.current.type == EventType.MouseDown)
                {
                    GUIUtility.keyboardControl = 0;
                    Event.current.Use();
                }
            }

            editorDragging.HandleDraggingToBottomArea(editors, remainingRect);
        }

        static bool HasLabel(Object target)
        {
            return HasLabel(target, AssetDatabase.GetAssetPath(target));
        }

        static bool HasLabel(Object target, string assetPath)
        {
            return EditorUtility.IsPersistent(target) && assetPath.StartsWith("assets", StringComparison.OrdinalIgnoreCase);
        }

        private Object[] GetInspectedAssets()
        {
            // We use this technique to support locking of the inspector. An inspector locks via an editor, so we need to use an editor to get the selection
            Editor assetEditor = InspectorWindowUtils.GetFirstNonImportInspectorEditor(tracker.activeEditors);
            if (assetEditor != null && assetEditor.targets.Length == 1)
            {
                string assetPath = AssetDatabase.GetAssetPath(assetEditor.target);
                if (HasLabel(assetEditor.target, assetPath) && !Directory.Exists(assetPath))
                    return assetEditor.targets;
            }

            // This is used if more than one asset is selected
            // Ideally the tracker should be refactored to track not just editors but also the selection that caused them, so we wouldn't need this
            return Selection.objects.Where(HasLabel).ToArray();
        }

        private void DrawPreviewAndLabels()
        {
            if (m_PreviewWindow && Event.current.type == EventType.Repaint)
                m_PreviewWindow.Repaint();

            IPreviewable[] editorsWithPreviews = GetEditorsWithPreviews(tracker.activeEditors);
            IPreviewable previewEditor = GetEditorThatControlsPreview(editorsWithPreviews);

            // Do we have a preview?
            bool hasPreview = previewEditor != null &&
                previewEditor.HasPreviewGUI() &&
                (m_PreviewWindow == null);

            Object[] assets = GetInspectedAssets();
            bool hasLabels = assets.Length > 0;
            bool hasBundleName = assets.Any(a => !(a is MonoScript) && AssetDatabase.IsMainAsset(a));

            if (!hasPreview && !hasLabels)
                return;

            Event evt = Event.current;

            // Preview / Asset Labels toolbar
            Rect rect = EditorGUILayout.BeginHorizontal(GUIContent.none, Styles.preToolbar, GUILayout.Height(kBottomToolbarHeight));
            Rect dragRect;
            Rect dragIconRect = new Rect();
            const float dragPadding = 3f;
            const float minDragWidth = 20f;
            {
                GUILayout.FlexibleSpace();
                dragRect = GUILayoutUtility.GetLastRect();
                // The label rect is also needed to know which area should be draggable.
                GUIContent title;
                if (hasPreview)
                {
                    GUIContent userDefinedTitle = previewEditor.GetPreviewTitle();
                    title = userDefinedTitle ?? Styles.preTitle;
                }
                else
                {
                    title = Styles.labelTitle;
                }

                dragIconRect.x = dragRect.x + dragPadding;
                dragIconRect.y = dragRect.y + (kBottomToolbarHeight - Styles.dragHandle.fixedHeight) / 2 + 1;
                dragIconRect.width = dragRect.width - dragPadding * 2;
                dragIconRect.height = Styles.dragHandle.fixedHeight;

                //If we have more than one component with Previews, show a DropDown menu.
                if (editorsWithPreviews.Length > 1)
                {
                    Vector2 foldoutSize = Styles.preDropDown.CalcSize(title);
                    float maxFoldoutWidth = (dragIconRect.xMax - dragRect.xMin) - dragPadding - minDragWidth;
                    float foldoutWidth = Mathf.Min(maxFoldoutWidth, foldoutSize.x);
                    Rect foldoutRect = new Rect(dragRect.x, dragRect.y, foldoutWidth, foldoutSize.y);
                    dragRect.xMin += foldoutWidth;
                    dragIconRect.xMin += foldoutWidth;

                    GUIContent[] panelOptions = new GUIContent[editorsWithPreviews.Length];
                    int selectedPreview = -1;
                    for (int index = 0; index < editorsWithPreviews.Length; index++)
                    {
                        IPreviewable currentEditor = editorsWithPreviews[index];
                        GUIContent previewTitle = currentEditor.GetPreviewTitle() ?? Styles.preTitle;

                        string fullTitle;
                        if (previewTitle == Styles.preTitle)
                        {
                            string componentTitle = ObjectNames.GetTypeName(currentEditor.target);
                            if (NativeClassExtensionUtilities.ExtendsANativeType(currentEditor.target))
                            {
                                componentTitle = MonoScript.FromScriptedObject(currentEditor.target).GetClass().Name;
                            }
                            fullTitle = previewTitle.text + " - " + componentTitle;
                        }
                        else
                        {
                            fullTitle = previewTitle.text;
                        }

                        panelOptions[index] = new GUIContent(fullTitle);
                        if (editorsWithPreviews[index] == previewEditor)
                        {
                            selectedPreview = index;
                        }
                    }

                    if (GUI.Button(foldoutRect, title, Styles.preDropDown))
                    {
                        EditorUtility.DisplayCustomMenu(foldoutRect, panelOptions, selectedPreview, OnPreviewSelected, editorsWithPreviews);
                    }
                }
                else
                {
                    float maxLabelWidth = (dragIconRect.xMax - dragRect.xMin) - dragPadding - minDragWidth;
                    float labelWidth = Mathf.Min(maxLabelWidth, Styles.preToolbar2.CalcSize(title).x);
                    Rect labelRect = new Rect(dragRect.x, dragRect.y, labelWidth, dragRect.height);

                    dragIconRect.xMin = labelRect.xMax + dragPadding;

                    GUI.Label(labelRect, title, Styles.preToolbar2);
                }

                if (hasPreview && Event.current.type == EventType.Repaint)
                {
                    Styles.dragHandle.Draw(dragIconRect, GUIContent.none, false, false, false, false);
                }

                if (hasPreview && m_PreviewResizer.GetExpandedBeforeDragging())
                    previewEditor.OnPreviewSettings();
            } EditorGUILayout.EndHorizontal();


            // Detach preview on right click in preview title bar
            if (evt.type == EventType.MouseUp && evt.button == 1 && rect.Contains(evt.mousePosition) && m_PreviewWindow == null)
                DetachPreview();

            // Logic for resizing and collapsing
            float previewSize;
            if (hasPreview)
            {
                // If we have a preview we'll use the ResizerControl which handles both resizing and collapsing

                // We have to subtract space used by version control bar from the window size we pass to the PreviewResizer
                Rect windowRect = position;
                if (EditorSettings.externalVersionControl != ExternalVersionControl.Disabled &&
                    EditorSettings.externalVersionControl != ExternalVersionControl.AutoDetect &&
                    EditorSettings.externalVersionControl != ExternalVersionControl.Generic
                )
                {
                    windowRect.height -= kBottomToolbarHeight;
                }
                previewSize = m_PreviewResizer.ResizeHandle(windowRect, 100, 100, kBottomToolbarHeight, dragRect);
            }
            else
            {
                // If we don't have a preview, just toggle the collapseble state with a button
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
                    m_PreviewResizer.ToggleExpanded();

                previewSize = 0;
            }

            // If collapsed, early out
            if (!m_PreviewResizer.GetExpanded())
                return;

            // The preview / label area (not including the toolbar)
            GUILayout.BeginVertical(Styles.preBackground, GUILayout.Height(previewSize));
            {
                // Draw preview
                if (hasPreview)
                {
                    previewEditor.DrawPreview(GUILayoutUtility.GetRect(0, 10240, 64, 10240));
                }

                if (hasLabels)
                {
                    using (new EditorGUI.DisabledScope(assets.Any(a => EditorUtility.IsPersistent(a) && !Editor.IsAppropriateFileOpenForEdit(a))))
                    {
                        m_LabelGUI.OnLabelGUI(assets);
                    }
                }

                if (hasBundleName)
                {
                    m_AssetBundleNameGUI.OnAssetBundleNameGUI(assets);
                }
            } GUILayout.EndVertical();
        }

        internal Object[] GetTargetsForPreview(IPreviewable previewEditor)
        {
            Editor editorForType = null;
            foreach (var editor in tracker.activeEditors)
            {
                if (editor.target.GetType() != previewEditor.target.GetType())
                {
                    continue;
                }

                editorForType = editor;
                break;
            }
            return editorForType.targets;
        }

        private void OnPreviewSelected(object userData, string[] options, int selected)
        {
            IPreviewable[] availabePreviews = userData as IPreviewable[];
            m_SelectedPreview = availabePreviews[selected];
        }

        private void DetachPreview()
        {
            Event.current.Use();
            m_PreviewWindow = CreateInstance(typeof(PreviewWindow)) as PreviewWindow;
            m_PreviewWindow.SetParentInspector(this);
            m_PreviewWindow.Show();
            Repaint();
            GUIUtility.ExitGUI();
        }

        private static void DrawVCSSticky(Rect anchorRect, Editor assetEditor, float offset)
        {
            string message = "";
            bool hasRemovedSticky = EditorPrefs.GetBool("vcssticky");
            if (!hasRemovedSticky && !Editor.IsAppropriateFileOpenForEdit(assetEditor.target, out message))
            {
                const int stickyRectHeight = 80;
                var rect = new Rect(10, anchorRect.y - stickyRectHeight, anchorRect.width - 30, stickyRectHeight);
                rect.y -= offset;
                if (Event.current.type == EventType.Repaint)
                {
                    Styles.stickyNote.Draw(rect, false, false, false, false);

                    Rect iconRect = new Rect(rect.x, rect.y + rect.height / 2 - 32, 64, 64);
                    if (EditorSettings.externalVersionControl == "Perforce") // TODO: remove hardcoding of perforce
                    {
                        Styles.stickyNotePerforce.Draw(iconRect, false, false, false, false);
                    }

                    Rect textRect = new Rect(rect.x + iconRect.width, rect.y, rect.width - iconRect.width, rect.height);
                    GUI.Label(textRect, EditorGUIUtility.TrTextContent("<b>Under Version Control</b>\nCheck out this asset in order to make changes."), Styles.stickyNoteLabel);

                    Rect arrowRect = new Rect(rect.x + rect.width / 2, rect.y + 80, 19, 14);
                    Styles.stickyNoteArrow.Draw(arrowRect, false, false, false, false);
                }
            }
        }

        internal static void DrawVCSShortInfo(EditorWindow hostWindow, Editor assetEditor)
        {
            if (Provider.enabled &&
                EditorSettings.externalVersionControl != ExternalVersionControl.Disabled &&
                EditorSettings.externalVersionControl != ExternalVersionControl.AutoDetect &&
                EditorSettings.externalVersionControl != ExternalVersionControl.Generic)
            {
                string assetPath = AssetDatabase.GetAssetPath(assetEditor.target);
                Asset asset = Provider.GetAssetByPath(assetPath);
                if (asset == null || !(asset.path.StartsWith("Assets") || asset.path.StartsWith("ProjectSettings")))
                    return;

                Asset metaAsset = Provider.GetAssetByPath(assetPath.Trim('/') + ".meta");

                string currentState = asset.StateToString();
                string currentMetaState = metaAsset == null ? String.Empty : metaAsset.StateToString();

                //We also need to take into account the global VCS state here, as it being offline (or not connected)
                //can also cause IsOpenForEdit to return false for checkout-enabled or lock-enabled VCS
                if (currentState == String.Empty && Provider.onlineState != OnlineState.Online)
                {
                    currentState = String.Format(Styles.VCS_NotConnectedMessage.text, Provider.GetActivePlugin().name);
                }

                bool showMetaState = metaAsset != null && (metaAsset.state & ~Asset.States.MetaFile) != asset.state;
                bool showAssetState = currentState != "";

                float labelHeight = showMetaState && showAssetState ? kBottomToolbarHeight * 2 : kBottomToolbarHeight;
                GUILayout.Label(GUIContent.none, Styles.preToolbar, GUILayout.Height(labelHeight));

                var rect = GUILayoutUtility.GetLastRect();

                bool isLayoutOrRepaint = Event.current.type == EventType.Layout || Event.current.type == EventType.Repaint;

                if (showAssetState && isLayoutOrRepaint)
                {
                    Texture2D icon = AssetDatabase.GetCachedIcon(assetPath) as Texture2D;
                    if (showMetaState)
                    {
                        Rect assetRect = rect;
                        assetRect.height = kBottomToolbarHeight;
                        DrawVCSShortInfoAsset(asset, BuildTooltip(asset, null), assetRect, icon, currentState);

                        Texture2D metaIcon = InternalEditorUtility.GetIconForFile(metaAsset.path);
                        assetRect.y += kBottomToolbarHeight;
                        DrawVCSShortInfoAsset(metaAsset, BuildTooltip(null, metaAsset), assetRect, metaIcon, currentMetaState);
                    }
                    else
                    {
                        DrawVCSShortInfoAsset(asset, BuildTooltip(asset, metaAsset), rect, icon, currentState);
                    }
                }
                else if (currentMetaState != "" && isLayoutOrRepaint)
                {
                    Texture2D metaIcon = InternalEditorUtility.GetIconForFile(metaAsset.path);
                    DrawVCSShortInfoAsset(metaAsset, BuildTooltip(asset, metaAsset), rect, metaIcon, currentMetaState);
                }

                string message = "";
                bool openForEdit = Editor.IsAppropriateFileOpenForEdit(assetEditor.target, out message);
                if (!openForEdit)
                {
                    if (Provider.isActive)  //Only offer a checkout button if we think we're in a state to open the file for edit
                    {
                        float buttonWidth = 80;
                        Rect buttonRect = new Rect(rect.x + rect.width - buttonWidth, rect.y, buttonWidth, rect.height);
                        if (GUI.Button(buttonRect, "Check out", Styles.lockedHeaderButton))
                        {
                            EditorPrefs.SetBool("vcssticky", true);
                            // TODO: Retrieve default CheckoutMode from VC settings (depends on asset type; native vs. imported)
                            Task task = Provider.Checkout(assetEditor.targets, CheckoutMode.Both);
                            task.Wait();
                            hostWindow.Repaint();
                        }
                    }
                    DrawVCSSticky(rect, assetEditor, rect.height / 2);
                }
            }
        }

        protected static string BuildTooltip(Asset asset, Asset metaAsset)
        {
            var sb = new StringBuilder();
            if (asset != null)
            {
                sb.AppendLine("Asset:");
                sb.AppendLine(asset.AllStateToString());
            }
            if (metaAsset != null)
            {
                sb.AppendLine("Meta file:");
                sb.AppendLine(metaAsset.AllStateToString());
            }
            return sb.ToString();
        }

        protected static void DrawVCSShortInfoAsset(Asset asset, string tooltip, Rect rect, Texture2D icon, string currentState)
        {
            Rect overlayRect = new Rect(rect.x, rect.y, 28, 16);
            Rect iconRect = overlayRect;
            iconRect.x += 6;
            iconRect.width = 16;
            if (icon != null)
                GUI.DrawTexture(iconRect, icon);
            Overlay.DrawOverlay(asset, overlayRect);

            Rect textRect = new Rect(rect.x + 26, rect.y, rect.width - 31, rect.height);
            GUIContent content = GUIContent.Temp(currentState);
            content.tooltip = tooltip;
            EditorGUI.LabelField(textRect, content, Styles.preToolbar2);
        }

        private void DrawEditors(Editor[] editors)
        {
            if (editors.Length == 0)
                return;



            // We need to force optimized GUI to dirty when object becomes open for edit
            // e.g. after checkout in version control. If this is not done the optimized
            // GUI will need an extra repaint before it gets ungrayed out.

            Object inspectedObject = GetInspectedObject();

            // Force header to be flush with the top of the window
            GUILayout.Space(0);

            var rebuildOptimizedGUIBlocks = InspectorWindowUtils.GetRebuildOptimizedGUIBlocks(inspectedObject,
                ref m_IsOpenForEdit, ref m_InvalidateGUIBlockCache);

            Editor.m_AllowMultiObjectAccess = true;
            bool showImportedObjectBarNext = false;
            Rect importedObjectBarRect = new Rect();

            if (editors.Length > 0 && editors[0].GetInstanceID() != m_LastInitialEditorInstanceID)
                OnTrackerRebuilt();
            int prefabComponentIndex = -1;
            for (int editorIndex = 0; editorIndex < editors.Length; editorIndex++)
            {
                if (m_ComponentsInPrefabSource != null && editorIndex != 0)
                {
                    while (prefabComponentIndex < m_ComponentsInPrefabSource.Length)
                    {
                        Object target = editors[editorIndex].target;

                        // This is possible if there's a component with a missing script.
                        if (target != null)
                        {
                            Object correspondingSource = PrefabUtility.GetCorrespondingObjectFromSource(target);
                            Component nextInSource = m_ComponentsInPrefabSource[prefabComponentIndex];
                            if (correspondingSource == nextInSource)
                                break;

                            DisplayRemovedComponent((GameObject)editors[0].target, nextInSource);
                        }

                        prefabComponentIndex++;
                    }
                }
                prefabComponentIndex++;

                if (ShouldCullEditor(editors, editorIndex))
                {
                    if (Event.current.type == EventType.Repaint)
                        editors[editorIndex].isInspectorDirty = false;
                    continue;
                }

                bool oldValue = GUIUtility.textFieldInput;
                DrawEditor(editors, editorIndex, rebuildOptimizedGUIBlocks, ref showImportedObjectBarNext, ref importedObjectBarRect);
                if (Event.current.type == EventType.Repaint && !oldValue && GUIUtility.textFieldInput) // Did this editor set textFieldInput=true?
                {
                    // If so, We need to flush the OptimizedGUIBlock every frame, so that EditorGUI.DoTextField() repaint keeps getting called and textFieldInput set to true.
                    // textFieldInput is reset to false at beginning of every repaint in c++ GUIView::DoPaint()
                    InspectorWindowUtils.FlushOptimizedGUIBlock(editors[editorIndex]);
                }
            }

            // Make sure to display any remaining removed components that come after the last component on the GameObject.
            if (m_ComponentsInPrefabSource != null)
            {
                while (prefabComponentIndex < m_ComponentsInPrefabSource.Length)
                {
                    Component nextInSource = m_ComponentsInPrefabSource[prefabComponentIndex];
                    DisplayRemovedComponent((GameObject)editors[0].target, nextInSource);
                    prefabComponentIndex++;
                }
            }

            EditorGUIUtility.ResetGUIState();

            // Draw the bar to show that the imported object is below
            DrawImportedObjectLabel(importedObjectBarRect);
        }

        private static void DrawImportedObjectLabel(Rect importedObjectBarRect)
        {
            if (importedObjectBarRect.height > 0)
            {
                // Clip the label to avoid a black border at the bottom
                GUI.BeginGroup(importedObjectBarRect);
                GUI.Label(new Rect(0, 0, importedObjectBarRect.width, importedObjectBarRect.height), "Imported Object", "OL Title");
                GUI.EndGroup();
            }
        }

        void DisplayRemovedComponent(GameObject go, Component comp)
        {
            if (comp == null)
                return;
            if ((comp.hideFlags & HideFlags.HideInInspector) != 0)
                return;
            if (!m_RemovedComponents.Contains(comp))
                return;

            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.inspectorTitlebar);
            EditorGUI.RemovedComponentTitlebar(rect, go, comp);
        }

        internal override void OnResized()
        {
            m_InvalidateGUIBlockCache = true;
        }

        private void DrawEditor(Editor[] editors, int editorIndex, bool rebuildOptimizedGUIBlock, ref bool showImportedObjectBarNext, ref Rect importedObjectBarRect)
        {
            var editor = editors[editorIndex];
            if (Event.current.type == EventType.Repaint)
            {
                editor.isInspectorDirty = false;
            }

            // Protect us against someone triggering an asset reimport during
            // OnGUI as that will kill all active editors.
            if (editor == null)
                return;

            var genericEditor = editor as GenericInspector;
            if (genericEditor)
                genericEditor.m_InspectorMode = m_InspectorMode;

            Object target = editor.target;

            // Avoid drawing editor if native target object is not alive, unless it's a MonoBehaviour/ScriptableObject
            // We want to draw the generic editor with a warning about missing/invalid script
            // Case 891450:
            // - ActiveEditorTracker will automatically create editors for materials of components on tracked game objects
            // - UnityEngine.UI.Mask will destroy this material in OnDisable (e.g. disabling it with the checkbox) causing problems when drawing the material editor
            if (target == null && !NativeClassExtensionUtilities.ExtendsANativeType(target))
                return;

            GUIUtility.GetControlID(target.GetInstanceID(), FocusType.Passive);
            EditorGUIUtility.ResetGUIState();

            // cache the layout group we expect to have at the end of drawing this editor
            GUILayoutGroup expectedGroup = GUILayoutUtility.current.topLevel;

            // Display title bar and early out if folded
            int wasVisibleState = tracker.GetVisible(editorIndex);
            bool wasVisible;

            if (wasVisibleState == -1)
            {
                // Some inspectors (MaterialEditor) needs to be told when they are the main visible asset.
                if (editorIndex == 0 || (editorIndex == 1 && ShouldCullEditor(editors, 0)))
                {
                    editor.firstInspectedEditor = true;
                }

                // Init our state with last state
                // Large headers should always be considered visible
                // because they need to at least update their Icons when they have a static preview (Material for exemple)
                wasVisible = InternalEditorUtility.GetIsInspectorExpanded(target) || EditorHasLargeHeader(editorIndex, editors);
                tracker.SetVisible(editorIndex, wasVisible ? 1 : 0);
            }
            else
            {
                wasVisible = wasVisibleState == 1;
            }

            rebuildOptimizedGUIBlock |= editor.isInspectorDirty;

            // Reset dirtiness when repainting
            if (Event.current.type == EventType.Repaint)
            {
                editor.isInspectorDirty = false;
            }

            //set the current PropertyHandlerCache to the current editor
            ScriptAttributeUtility.propertyHandlerCache = editor.propertyHandlerCache;

            // Dragging handle used for editor reordering
            var dragRect = DrawEditorHeader(editors, editorIndex, ref showImportedObjectBarNext, ref importedObjectBarRect, editor, target, ref wasVisible);

            if (editor.target is AssetImporter)
                showImportedObjectBarNext = true;

            var multiEditingSupported = IsMultiEditingSupported(editor, target);


            if (!multiEditingSupported && wasVisible)
            {
                GUILayout.Label("Multi-object editing not supported.", EditorStyles.helpBox);
                return;
            }

            InspectorWindowUtils.DisplayDeprecationMessageIfNecessary(editor);

            // We need to reset again after drawing the header.
            EditorGUIUtility.ResetGUIState();

            Rect contentRect = new Rect();
            bool excludedClass = ModuleMetadata.GetModuleIncludeSettingForObject(target) == ModuleIncludeSetting.ForceExclude;
            if (excludedClass)
            {
                var objectModule = ModuleMetadata.GetModuleForObject(target);
                var excludingModule = ModuleMetadata.GetExcludingModuleForObject(target);
                if (objectModule == excludingModule)
                    EditorGUILayout.HelpBox(string.Format(Styles.objectDisabledModuleWarningFormat, objectModule), MessageType.Warning);
                else
                    EditorGUILayout.HelpBox(string.Format(Styles.objectDisabledModuleWithDependencyWarningFormat, excludingModule, objectModule), MessageType.Warning);
            }


            using (new EditorGUI.DisabledScope(!editor.IsEnabled() || excludedClass))
            {
                EditorGUIUtility.hierarchyMode = true;
                EditorGUIUtility.wideMode = position.width > Editor.k_WideModeMinWidth;

                //set the current PropertyHandlerCache to the current editor
                ScriptAttributeUtility.propertyHandlerCache = editor.propertyHandlerCache;

                if (DoOnInspectorGUI(rebuildOptimizedGUIBlock, editor, wasVisible, ref contentRect))
                {
                    return;
                }
            }

            editorDragging.HandleDraggingToEditor(editors, editorIndex, dragRect, contentRect);

            // Check and try to cleanup layout groups.
            if (GUILayoutUtility.current.topLevel != expectedGroup)
            {
                if (!GUILayoutUtility.current.layoutGroups.Contains(expectedGroup))
                {
                    // We can't recover from this, so we error.
                    Debug.LogError("Expected top level layout group missing! Too many GUILayout.EndScrollView/EndVertical/EndHorizontal?");
                    GUIUtility.ExitGUI();
                }
                else
                {
                    // We can recover from this, so we warning.
                    Debug.LogWarning("Unexpected top level layout group! Missing GUILayout.EndScrollView/EndVertical/EndHorizontal?");

                    while (GUILayoutUtility.current.topLevel != expectedGroup)
                        GUILayoutUtility.EndLayoutGroup();
                }
            }

            HandleComponentScreenshot(contentRect, editor);
        }

        Rect DrawEditorHeader(Editor[] editors, int editorIndex, ref bool showImportedObjectBarNext, ref Rect importedObjectBarRect, Editor editor, Object target, ref bool wasVisible)
        {
            var largeHeader = DrawEditorLargeHeader(editors, editorIndex, ref showImportedObjectBarNext, ref importedObjectBarRect, editor, ref wasVisible);

            // Dragging handle used for editor reordering
            var dragRect = largeHeader ? new Rect() : DrawEditorSmallHeader(editors, editorIndex, target, editor, wasVisible);
            return dragRect;
        }

        bool DrawEditorLargeHeader(Editor[] editors, int editorIndex, ref bool showImportedObjectBarNext, ref Rect importedObjectBarRect, Editor editor, ref bool wasVisible)
        {
            bool largeHeader = EditorHasLargeHeader(editorIndex, editors);

            // Draw large headers before we do the culling of unsupported editors below,
            // so the large header is always shown even when the editor can't be.
            if (largeHeader)
            {
                String message = String.Empty;
                bool IsOpenForEdit = editor.IsOpenForEdit(out message);

                if (showImportedObjectBarNext)
                {
                    showImportedObjectBarNext = false;
                    GUILayout.Space(15);
                    importedObjectBarRect = GUILayoutUtility.GetRect(16, 16);
                    importedObjectBarRect.height = 17;
                }

                wasVisible = true;

                // Header
                using (new EditorGUI.DisabledScope(!IsOpenForEdit)) // Only disable the entire header if the asset is locked by VCS
                {
                    editor.DrawHeader();
                }
            }

            return largeHeader;
        }

        // Draw small headers (the header above each component) after the culling above
        // so we don't draw a component header for all the components that can't be shown.
        Rect DrawEditorSmallHeader(Editor[] editors, int editorIndex, Object target, Editor editor, bool wasVisible)
        {
            // ensure first component's title bar is flush with the header
            if (editorIndex > 0 && editors[editorIndex - 1].target is GameObject && target is Component)
            {
                GUILayout.Space(
                    -1f // move back up so line overlaps
                    - EditorStyles.inspectorBig.margin.bottom - EditorStyles.inspectorTitlebar.margin.top // move back up margins
                );
            }

            using (new EditorGUI.DisabledScope(!editor.IsEnabled()))
            {
                bool isVisible = EditorGUILayout.InspectorTitlebar(wasVisible, editor);
                if (wasVisible != isVisible)
                {
                    tracker.SetVisible(editorIndex, isVisible ? 1 : 0);
                    InternalEditorUtility.SetIsInspectorExpanded(target, isVisible);
                    if (isVisible)
                    {
                        m_LastInteractedEditor = editor;
                    }
                    else if (m_LastInteractedEditor == editor)
                    {
                        m_LastInteractedEditor = null;
                    }
                }
            }

            return GUILayoutUtility.GetLastRect();
        }

        bool DoOnInspectorGUI(bool rebuildOptimizedGUIBlock, Editor editor, bool wasVisible, ref Rect contentRect)
        {
            OptimizedGUIBlock optimizedBlock;
            float height;
            if (editor.GetOptimizedGUIBlock(rebuildOptimizedGUIBlock, wasVisible, out optimizedBlock, out height))
            {
                contentRect = GUILayoutUtility.GetRect(0, wasVisible ? height : 0);
                HandleLastInteractedEditor(contentRect, editor);

                // Layout events are ignored in the optimized code path
                if (Event.current.type == EventType.Layout)
                {
                    return true;
                }

                DrawAddedComponentBackground(contentRect, editor.targets);

                // Try reusing optimized block
                if (optimizedBlock.Begin(rebuildOptimizedGUIBlock, contentRect))
                {
                    // Draw content
                    if (wasVisible)
                    {
                        GUI.changed = false;
                        editor.OnOptimizedInspectorGUI(contentRect);
                    }
                }

                optimizedBlock.End();
            }
            else
            {
                // Render contents if folded out
                if (wasVisible)
                {
                    GUIStyle editorWrapper = (editor.UseDefaultMargins() ? EditorStyles.inspectorDefaultMargins : GUIStyle.none);
                    contentRect = EditorGUILayout.BeginVertical(editorWrapper);
                    {
                        DrawAddedComponentBackground(contentRect, editor.targets);

                        HandleLastInteractedEditor(contentRect, editor);

                        GUI.changed = false;

                        try
                        {
                            editor.OnInspectorGUI();
                        }
                        catch (Exception e)
                        {
                            if (GUIUtility.ShouldRethrowException(e))
                                throw;

                            Debug.LogException(e);
                        }
                    }
                    EditorGUILayout.EndVertical();
                }

                // early out if an event has been used
                if (Event.current.type == EventType.Used)
                {
                    return true;
                }
            }

            return false;
        }

        bool IsMultiEditingSupported(Editor editor, Object target)
        {
            // Culling of editors that can't be properly shown.
            // If the current editor is a GenericInspector even though a custom editor for it exists,
            // then it's either a fallback for a custom editor that doesn't support multi-object editing,
            // or we're in debug mode.
            bool multiEditingSupported = true;
            if (editor is GenericInspector && CustomEditorAttributes.FindCustomEditorType(target, false) != null)
            {
                if (m_InspectorMode == InspectorMode.DebugInternal)
                {
                    // Do nothing
                }
                else if (m_InspectorMode == InspectorMode.Normal)
                {
                    // If we're not in debug mode and it thus must be a fallback,
                    // hide the editor and show a notification.
                    multiEditingSupported = false;
                }
                else if (target is AssetImporter)
                {
                    // If we are in debug mode and it's an importer type,
                    // hide the editor and show a notification.
                    multiEditingSupported = false;
                }

                // If we are in debug mode and it's an NOT importer type,
                // just show the debug inspector as usual.
            }

            return multiEditingSupported;
        }

        internal void RepaintImmediately(bool rebuildOptimizedGUIBlocks)
        {
            m_InvalidateGUIBlockCache = rebuildOptimizedGUIBlocks;
            RepaintImmediately();
        }

        internal bool EditorHasLargeHeader(int editorIndex, Editor[] trackerActiveEditors)
        {
            return trackerActiveEditors[editorIndex].firstInspectedEditor || trackerActiveEditors[editorIndex].HasLargeHeader();
        }

        private void HandleComponentScreenshot(Rect contentRect, Editor editor)
        {
            if (ScreenShots.s_TakeComponentScreenshot)
            {
                contentRect.yMin -= 16;
                if (contentRect.Contains(Event.current.mousePosition))
                {
                    Rect globalComponentRect = GUIClip.Unclip(contentRect);
                    globalComponentRect.position = globalComponentRect.position + m_Parent.screenPosition.position;
                    ScreenShots.ScreenShotComponent(globalComponentRect, editor.target);
                }
            }
        }

        internal bool ShouldCullEditor(Editor[] editors, int editorIndex)
        {
            if (editors[editorIndex].hideInspector)
                return true;

            Object currentTarget = editors[editorIndex].target;

            // Objects that should always be hidden
            if (currentTarget is ParticleSystemRenderer)
                return true;

            // Hide regular AssetImporters (but not inherited types)
            if (currentTarget != null && currentTarget.GetType() == typeof(AssetImporter))
                return true;

            // Let asset importers decide if the imported object should be shown or not
            if (m_InspectorMode == InspectorMode.Normal && editorIndex != 0)
            {
                AssetImporterEditor importerEditor = editors[0] as AssetImporterEditor;
                if (importerEditor != null && !importerEditor.showImportedObject)
                    return true;
            }

            return false;
        }

        void DrawSelectionPickerList()
        {
            if (m_TypeSelectionList == null)
                m_TypeSelectionList = new TypeSelectionList(Selection.objects);

            // Force header to be flush with the top of the window
            GUILayout.Space(0);

            Editor.DrawHeaderGUI(null, Selection.objects.Length + " Objects");

            GUILayout.Label("Narrow the Selection:", EditorStyles.label);
            GUILayout.Space(4);

            Vector2 oldSize = EditorGUIUtility.GetIconSize();
            EditorGUIUtility.SetIconSize(new Vector2(16, 16));

            foreach (TypeSelection ts in m_TypeSelectionList.typeSelections)
            {
                Rect r = GUILayoutUtility.GetRect(16, 16, GUILayout.ExpandWidth(true));
                if (GUI.Button(r, ts.label, Styles.typeSelection))
                {
                    Selection.objects = ts.objects;
                    Event.current.Use();
                }
                if (GUIUtility.hotControl == 0)
                    EditorGUIUtility.AddCursorRect(r, MouseCursor.Link);
                GUILayout.Space(4);
            }

            EditorGUIUtility.SetIconSize(oldSize);
        }

        void HandleLastInteractedEditor(Rect componentRect, Editor editor)
        {
            if (editor != m_LastInteractedEditor &&
                Event.current.type == EventType.MouseDown && componentRect.Contains(Event.current.mousePosition))
            {
                // Don't use the event because the editor might want to use it.
                // But don't move the check down below the editor either,
                // because we want to set the last interacted editor simultaneously.
                m_LastInteractedEditor = editor;
                Repaint();
            }
        }

        private void AddComponentButton(Editor[] editors)
        {
            Editor editor = InspectorWindowUtils.GetFirstNonImportInspectorEditor(editors);
            if (editor != null && editor.target != null && editor.target is GameObject && editor.IsEnabled() && !EditorUtility.IsPersistent(editor.target))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                var content = Styles.addComponentLabel;
                Rect rect = GUILayoutUtility.GetRect(content, Styles.addComponentButtonStyle);

                // Visually separates the Add Component button from the existing components
                if (Event.current.type == EventType.Repaint)
                    DrawSplitLine(rect.y - 9);

                Event evt = Event.current;
                bool openWindow = false;
                switch (evt.type)
                {
                    case EventType.ExecuteCommand:
                        string commandName = evt.commandName;
                        if (commandName == AddComponentWindow.OpenAddComponentDropdown)
                        {
                            openWindow = true;
                            evt.Use();
                        }
                        break;
                }

                if (EditorGUI.DropdownButton(rect, content, FocusType.Passive, Styles.addComponentButtonStyle) || openWindow)
                {
                    if (AddComponentWindow.Show(rect, editor.targets.Select(o => (GameObject)o).ToArray()))
                    {
                        GUIUtility.ExitGUI();
                    }
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        private bool ReadyToRepaint()
        {
            if (AnimationMode.InAnimationPlaybackMode())
            {
                long timeNow = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                if (timeNow - s_LastUpdateWhilePlayingAnimation < delayRepaintWhilePlayingAnimation)
                    return false;
                s_LastUpdateWhilePlayingAnimation = timeNow;
            }

            return true;
        }

        private void DrawSplitLine(float y)
        {
            Rect position = new Rect(0, y, m_Pos.width + 1, 1);
            Rect uv = new Rect(0, 1f, 1, 1f - 1f / EditorStyles.inspectorTitlebar.normal.background.height);
            GUI.DrawTextureWithTexCoords(position, EditorStyles.inspectorTitlebar.normal.background, uv);
        }

        private void DrawAddedComponentBackground(Rect position, Object[] targets)
        {
            if (Event.current.type == EventType.Repaint && targets.Length == 1)
            {
                Component comp = targets[0] as Component;
                if (comp != null &&
                    EditorGUIUtility.comparisonViewMode == EditorGUIUtility.ComparisonViewMode.None &&
                    PrefabUtility.GetCorrespondingObjectFromSource(comp.gameObject) != null &&
                    PrefabUtility.GetCorrespondingObjectFromSource(comp) == null)
                {
                    // Ensure colored margin here for component body doesn't overlap colored margin from InspectorTitlebar,
                    // and extends down to exactly touch the separator line between/after components.
                    EditorGUI.DrawOverrideBackground(new Rect(position.x, position.y + 3, position.width, position.height - 2));
                }
            }
        }

        // Invoked from C++
        internal static void ShowWindow()
        {
            GetWindow(typeof(InspectorWindow));
        }

        // Called from OptimizedGUIBlock::FlushAll();
        private static void FlushOptimizedGUI()
        {
            // Delay flushing since FlushOptimizedGUI is called when going into playmode (assembly reload)
            // for every TextMeshGenerator::Flush () in c++.
            // By delaying we also ensure that our editor trackers are fully constructed before flushing (fixes case 718491)
            s_AllOptimizedGUIBlocksNeedsRebuild = true;
        }

        private static void FlushAllOptimizedGUIBlocksIfNeeded()
        {
            if (!s_AllOptimizedGUIBlocksNeedsRebuild)
                return;
            s_AllOptimizedGUIBlocksNeedsRebuild = false;

            foreach (var inspector in m_AllInspectors)
            {
                foreach (var editor in inspector.tracker.activeEditors)
                    InspectorWindowUtils.FlushOptimizedGUIBlock(editor);
            }
        }

        private void Update()
        {
            Editor[] editors = tracker.activeEditors;
            if (editors == null)
                return;

            bool wantsRepaint = false;
            foreach (var myEditor in editors)
            {
                if (myEditor.RequiresConstantRepaint() && !myEditor.hideInspector)
                    wantsRepaint = true;
            }

            if (wantsRepaint && m_lastRenderedTime + 0.033f < EditorApplication.timeSinceStartup)
            {
                m_lastRenderedTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        void PrepareLockedObjectsForSerialization()
        {
            ClearSerializedLockedObjects();

            if (m_Tracker != null && m_Tracker.isLocked)
            {
                m_Tracker.GetObjectsLockedByThisTracker(m_ObjectsLockedBeforeSerialization);

                // take out non persistent and track them in a list of instance IDs, because they wouldn't survive serialization as Objects
                for (int i = m_ObjectsLockedBeforeSerialization.Count - 1; i >= 0; i--)
                {
                    if (!EditorUtility.IsPersistent(m_ObjectsLockedBeforeSerialization[i]))
                    {
                        m_InstanceIDsLockedBeforeSerialization.Add(m_ObjectsLockedBeforeSerialization[i].GetInstanceID());
                        m_ObjectsLockedBeforeSerialization.RemoveAt(i);
                    }
                }
            }
        }

        void ClearSerializedLockedObjects()
        {
            m_ObjectsLockedBeforeSerialization.Clear();

            m_InstanceIDsLockedBeforeSerialization.Clear();
        }

        internal void GetObjectsLocked(List<Object> objs)
        {
            m_Tracker.GetObjectsLockedByThisTracker(objs);
        }

        internal void SetObjectsLocked(List<Object> objs)
        {
            m_LockTracker.isLocked = true;
            m_Tracker.SetObjectsLockedByThisTracker(objs);
        }

        void RestoreLockStateFromSerializedData()
        {
            if (m_Tracker == null)
            {
                return;
            }

            // try to retrieve all Objects from their stored instance ids in the list.
            // this is only used for non persistent objects (scene objects)

            if (m_InstanceIDsLockedBeforeSerialization.Count > 0)
            {
                for (int i = 0; i < m_InstanceIDsLockedBeforeSerialization.Count; i++)
                {
                    Object instance = EditorUtility.InstanceIDToObject(m_InstanceIDsLockedBeforeSerialization[i]);
                    //don't add null objects (i.e.
                    if (instance)
                    {
                        m_ObjectsLockedBeforeSerialization.Add(instance);
                    }
                }
            }

            for (int i = m_ObjectsLockedBeforeSerialization.Count - 1; i >= 0; i--)
            {
                if (m_ObjectsLockedBeforeSerialization[i] == null)
                {
                    m_ObjectsLockedBeforeSerialization.RemoveAt(i);
                }
            }

            // set the tracker to the serialized list. if it contains nulls or is empty, the tracker won't lock
            // this fixes case 775007
            m_Tracker.SetObjectsLockedByThisTracker(m_ObjectsLockedBeforeSerialization);
            // since this method likely got called during OnEnable, and rebuilding the tracker could call OnDisable on all Editors,
            // some of which might not have gotten their enable yet, the rebuilding needs to happen delayed in EditorApplication.update
            new DelayedCallback(tracker.RebuildIfNecessary, 0f);
        }

        internal static bool AddInspectorWindow(InspectorWindow window)
        {
            if (m_AllInspectors.Contains(window))
            {
                return false;
            }

            m_AllInspectors.Add(window);
            return true;
        }

        internal static void RemoveInspectorWindow(InspectorWindow window)
        {
            m_AllInspectors.Remove(window);
        }
    }
}
