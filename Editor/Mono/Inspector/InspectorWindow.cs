// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define PERF_PROFILE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using UnityEditorInternal;
using UnityEditorInternal.VersionControl;
using Object = UnityEngine.Object;
using UnityEditor.VersionControl;
using UnityEngine.Profiling;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Inspector", useTypeNameAsIconName = true)]
    internal class InspectorWindow : EditorWindow, IHasCustomMenu
    {
        public Vector2 m_ScrollPosition;
        public InspectorMode   m_InspectorMode = InspectorMode.Normal;

        static readonly List<InspectorWindow> m_AllInspectors = new List<InspectorWindow>();
        static bool s_AllOptimizedGUIBlocksNeedsRebuild;

        const float kBottomToolbarHeight = 17f;
        internal const int kInspectorPaddingLeft = 4 + 10;
        internal const int kInspectorPaddingRight = 4;

        private const long delayRepaintWhilePlayingAnimation = 150; // Delay between repaints in milliseconds while playing animation
        private long s_LastUpdateWhilePlayingAnimation = 0;

        bool m_ResetKeyboardControl;
        protected ActiveEditorTracker m_Tracker;
        Editor m_LastInteractedEditor;
        bool m_IsOpenForEdit = false;

        private static Styles s_Styles;
        internal static Styles styles { get { return s_Styles ?? (s_Styles = new Styles()); } }

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
        private IPreviewable m_SelectedPreview;

        private EditorDragging editorDragging;

        internal class Styles
        {
            public readonly GUIStyle preToolbar = "preToolbar";
            public readonly GUIStyle preToolbar2 = "preToolbar2";
            public readonly GUIStyle preDropDown = "preDropDown";
            public readonly GUIStyle dragHandle = "RL DragHandle";
            public readonly GUIStyle lockButton = "IN LockButton";
            public readonly GUIStyle insertionMarker = "InsertionMarker";
            public readonly GUIContent preTitle = EditorGUIUtility.TextContent("Preview");
            public readonly GUIContent labelTitle = EditorGUIUtility.TextContent("Asset Labels");
            public readonly GUIContent addComponentLabel = EditorGUIUtility.TextContent("Add Component");
            public GUIStyle preBackground = "preBackground";
            public GUIStyle addComponentArea = EditorStyles.inspectorTitlebar;
            public GUIStyle addComponentButtonStyle = "AC Button";
            public GUIStyle previewMiniLabel = new GUIStyle(EditorStyles.whiteMiniLabel);
            public GUIStyle typeSelection = new GUIStyle("PR Label");
            public GUIStyle lockedHeaderButton = "preButton";
            public GUIStyle stickyNote = new GUIStyle("VCS_StickyNote");
            public GUIStyle stickyNoteArrow = new GUIStyle("VCS_StickyNoteArrow");
            public GUIStyle stickyNotePerforce = new GUIStyle("VCS_StickyNoteP4");
            public GUIStyle stickyNoteLabel = new GUIStyle("VCS_StickyNoteLabel");
            public readonly GUIContent VCS_NotConnectedMessage = EditorGUIUtility.TextContent("VCS Plugin {0} is enabled but not connected");

            public Styles()
            {
                typeSelection.padding.left = 12;
            }
        }

        public InspectorWindow()
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

            if (!m_AllInspectors.Contains(this))
                m_AllInspectors.Add(this);

            m_PreviewResizer.Init("InspectorPreview");
            m_LabelGUI.OnEnable();
            // ensure tracker is valid here in case domain is reloaded before first time inspector is drawn
            // fixes case 829182
            CreateTracker();
        }

        protected virtual void OnDisable()
        {
            m_AllInspectors.Remove(this);
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

        void OnSelectionChange()
        {
            m_Previews = null;
            m_SelectedPreview = null;
            m_TypeSelectionList = null;
            m_Parent.ClearKeyboardControl();
            ScriptAttributeUtility.ClearGlobalCache();
            Repaint();
        }

        static public InspectorWindow[] GetAllInspectorWindows()
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
            menu.AddItem(new GUIContent("Normal"), m_InspectorMode == InspectorMode.Normal, SetNormal);
            menu.AddItem(new GUIContent("Debug"), m_InspectorMode == InspectorMode.Debug, SetDebug);
            if (Unsupported.IsDeveloperBuild())
                menu.AddItem(new GUIContent("Debug-Internal"), m_InspectorMode == InspectorMode.DebugInternal, SetDebugInternal);

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Lock"), isLocked, FlipLocked);
        }

        void RefreshTitle()
        {
            string iconName = "UnityEditor.InspectorWindow";
            if (m_InspectorMode == InspectorMode.Normal)
                titleContent = EditorGUIUtility.TextContentWithIcon("Inspector", iconName);
            else
                titleContent = EditorGUIUtility.TextContentWithIcon("Debug", iconName);
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

        void FlipLocked()
        {
            isLocked = !isLocked;
        }

        public bool isLocked
        {
            get
            {
                return tracker.isLocked;
            }
            set
            {
                tracker.isLocked = value;
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

        public ActiveEditorTracker tracker
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

        protected virtual void CreatePreviewables()
        {
            if (m_Previews != null)
                return;

            m_Previews = new List<IPreviewable>();

            if (tracker.activeEditors.Length == 0)
                return;

            foreach (var editor in tracker.activeEditors)
            {
                IEnumerable<IPreviewable> previews = GetPreviewsForType(editor);
                foreach (var preview in previews)
                {
                    m_Previews.Add(preview);
                }
            }
        }

        private IEnumerable<IPreviewable> GetPreviewsForType(Editor editor)
        {
            //TODO: cache this in a map. probably in another utility class
            List<IPreviewable> previews = new List<IPreviewable>();

            foreach (var assembly in EditorAssemblies.loadedAssemblies)
            {
                Type[] types = AssemblyHelper.GetTypesFromAssembly(assembly);
                foreach (var type in types)
                {
                    if (!typeof(IPreviewable).IsAssignableFrom(type))
                        continue;

                    if (typeof(Editor).IsAssignableFrom(type))   //we don't want Editor classes with preview here.
                        continue;

                    object[] attrs = type.GetCustomAttributes(typeof(CustomPreviewAttribute), false);
                    foreach (CustomPreviewAttribute previewAttr in attrs)
                    {
                        if (editor.target == null || previewAttr.m_Type != editor.target.GetType())
                            continue;

                        IPreviewable preview = Activator.CreateInstance(type) as IPreviewable;
                        preview.Initialize(editor.targets);
                        previews.Add(preview);
                    }
                }
            }
            return previews;
        }

        protected virtual void ShowButton(Rect r)
        {
            bool willLock = GUI.Toggle(r, isLocked, GUIContent.none, styles.lockButton);
            if (willLock != isLocked)
            {
                isLocked = willLock;
                tracker.RebuildIfNecessary();
            }
        }


        static public InspectorWindow s_CurrentInspectorWindow;


        protected virtual void OnGUI()
        {
            Profiler.BeginSample("InspectorWindow.OnGUI");

            CreatePreviewables();
            FlushAllOptimizedGUIBlocksIfNeeded();

            ResetKeyboardControl();
            m_ScrollPosition = EditorGUILayout.BeginVerticalScrollView(m_ScrollPosition);
            {
                if (Event.current.type == EventType.Repaint)
                    tracker.ClearDirty();

                s_CurrentInspectorWindow = this;
                Editor[] editors = tracker.activeEditors;

                AssignAssetEditor(editors);
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
                DrawVCSShortInfo();
            }


            Profiler.EndSample();
        }

        public virtual Editor GetLastInteractedEditor()
        {
            return m_LastInteractedEditor;
        }

        public IPreviewable GetEditorThatControlsPreview(IPreviewable[] editors)
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
            Type lastType = (lastInteractedEditor != null) ? lastInteractedEditor.GetType() : null;

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

        public IPreviewable[] GetEditorsWithPreviews(Editor[] editors)
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

        public Object GetInspectedObject()
        {
            Editor editor = GetFirstNonImportInspectorEditor(tracker.activeEditors);
            if (editor == null)
                return null;
            return editor.target;
        }

        Editor GetFirstNonImportInspectorEditor(Editor[] editors)
        {
            foreach (Editor e in editors)
            {
                // Check for target rather than the editor type itself,
                // because some importers use default inspector
                if (e.target is AssetImporter)
                    continue;
                return e;
            }
            return null;
        }

        private void MoveFocusOnKeyPress()
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
                Editor editor = GetFirstNonImportInspectorEditor(editors);
                if (editor != null)
                    DoInspectorDragAndDrop(remainingRect, editor.targets);

                if (Event.current.type == EventType.MouseDown)
                {
                    GUIUtility.keyboardControl = 0;
                    Event.current.Use();
                }
            }

            editorDragging.HandleDraggingToBottomArea(remainingRect, m_Tracker);
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
            Editor assetEditor = GetFirstNonImportInspectorEditor(tracker.activeEditors);
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
            Rect rect = EditorGUILayout.BeginHorizontal(GUIContent.none, styles.preToolbar, GUILayout.Height(kBottomToolbarHeight));
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
                    title = userDefinedTitle ?? styles.preTitle;
                }
                else
                {
                    title = styles.labelTitle;
                }

                dragIconRect.x = dragRect.x + dragPadding;
                dragIconRect.y = dragRect.y + (kBottomToolbarHeight - s_Styles.dragHandle.fixedHeight) / 2 + 1;
                dragIconRect.width = dragRect.width - dragPadding * 2;
                dragIconRect.height = s_Styles.dragHandle.fixedHeight;

                //If we have more than one component with Previews, show a DropDown menu.
                if (editorsWithPreviews.Length > 1)
                {
                    Vector2 foldoutSize = styles.preDropDown.CalcSize(title);
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
                        GUIContent previewTitle = currentEditor.GetPreviewTitle() ?? styles.preTitle;

                        string fullTitle;
                        if (previewTitle == styles.preTitle)
                        {
                            string componentTitle = ObjectNames.GetTypeName(currentEditor.target);
                            if (currentEditor.target is MonoBehaviour)
                            {
                                componentTitle = MonoScript.FromMonoBehaviour(currentEditor.target as MonoBehaviour).GetClass().Name;
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

                    if (GUI.Button(foldoutRect, title, styles.preDropDown))
                    {
                        EditorUtility.DisplayCustomMenu(foldoutRect, panelOptions, selectedPreview, OnPreviewSelected, editorsWithPreviews);
                    }
                }
                else
                {
                    float maxLabelWidth = (dragIconRect.xMax - dragRect.xMin) - dragPadding - minDragWidth;
                    float labelWidth = Mathf.Min(maxLabelWidth, styles.preToolbar2.CalcSize(title).x);
                    Rect labelRect = new Rect(dragRect.x, dragRect.y, labelWidth, dragRect.height);

                    dragIconRect.xMin = labelRect.xMax + dragPadding;

                    GUI.Label(labelRect, title, styles.preToolbar2);
                }

                if (hasPreview && Event.current.type == EventType.Repaint)
                {
                    s_Styles.dragHandle.Draw(dragIconRect, GUIContent.none, false, false, false, false);
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
            GUILayout.BeginVertical(styles.preBackground, GUILayout.Height(previewSize));
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

        protected Object[] GetTargetsForPreview(IPreviewable previewEditor)
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
            m_PreviewWindow = ScriptableObject.CreateInstance(typeof(PreviewWindow)) as PreviewWindow;
            m_PreviewWindow.SetParentInspector(this);
            m_PreviewWindow.Show();
            Repaint();
            GUIUtility.ExitGUI();
        }

        protected virtual void DrawVCSSticky(float offset)
        {
            string message = "";
            Editor assetEditor = GetFirstNonImportInspectorEditor(tracker.activeEditors);
            bool hasRemovedSticky = EditorPrefs.GetBool("vcssticky");
            if (!hasRemovedSticky && !Editor.IsAppropriateFileOpenForEdit(assetEditor.target, out message))
            {
                var rect = new Rect(10, position.height - 94, position.width - 20, 80);
                rect.y -= offset;
                if (Event.current.type == EventType.Repaint)
                {
                    styles.stickyNote.Draw(rect, false, false, false, false);

                    Rect iconRect = new Rect(rect.x, rect.y + rect.height / 2 - 32, 64, 64);
                    if (EditorSettings.externalVersionControl == "Perforce") // TODO: remove hardcoding of perforce
                    {
                        styles.stickyNotePerforce.Draw(iconRect, false, false, false, false);
                    }

                    Rect textRect = new Rect(rect.x + iconRect.width, rect.y, rect.width - iconRect.width, rect.height);
                    GUI.Label(textRect, new GUIContent("<b>Under Version Control</b>\nCheck out this asset in order to make changes."), styles.stickyNoteLabel);

                    Rect arrowRect = new Rect(rect.x + rect.width / 2, rect.y + 80, 19, 14);
                    styles.stickyNoteArrow.Draw(arrowRect, false, false, false, false);
                }
            }
        }

        private void DrawVCSShortInfo()
        {
            if (Provider.enabled &&
                EditorSettings.externalVersionControl != ExternalVersionControl.Disabled &&
                EditorSettings.externalVersionControl != ExternalVersionControl.AutoDetect &&
                EditorSettings.externalVersionControl != ExternalVersionControl.Generic)
            {
                Editor assetEditor = GetFirstNonImportInspectorEditor(tracker.activeEditors);
                string assetPath = AssetDatabase.GetAssetPath(assetEditor.target);
                Asset asset = Provider.GetAssetByPath(assetPath);
                if (asset == null || !(asset.path.StartsWith("Assets") || asset.path.StartsWith("ProjectSettings")))
                    return;

                Asset metaAsset = Provider.GetAssetByPath(assetPath.Trim('/') + ".meta");

                string currentState = asset.StateToString();
                string currentMetaState = metaAsset == null ? String.Empty : metaAsset.StateToString();

                //We also need to take into account the global VCS state here, as it being offline (or not connected)
                //can also cause IsOpenForEdit to return false for checkout-enabled or lock-enabled VCS
                if (currentState == string.Empty && Provider.onlineState != OnlineState.Online)
                {
                    currentState = String.Format(s_Styles.VCS_NotConnectedMessage.text, Provider.GetActivePlugin().name);
                }

                bool showMetaState = metaAsset != null && (metaAsset.state & ~Asset.States.MetaFile) != asset.state;
                bool showAssetState = currentState != "";

                float labelHeight = showMetaState && showAssetState ? kBottomToolbarHeight * 2 : kBottomToolbarHeight;
                GUILayout.Label(GUIContent.none, styles.preToolbar, GUILayout.Height(labelHeight));

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

                        Texture2D metaIcon = InternalEditorUtility.GetIconForFile(metaAsset.path) as Texture2D;
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
                    Texture2D metaIcon = InternalEditorUtility.GetIconForFile(metaAsset.path) as Texture2D;
                    DrawVCSShortInfoAsset(metaAsset, BuildTooltip(asset, metaAsset), rect, metaIcon, currentMetaState);
                }

                string message = "";
                bool openForEdit = Editor.IsAppropriateFileOpenForEdit(assetEditor.target, out message);
                if (!openForEdit)
                {
                    if (Provider.isActive)  //Only ofer a checkout button if we think we're in a state to open the file for edit
                    {
                        float buttonWidth = 80;
                        Rect buttonRect = new Rect(rect.x + rect.width - buttonWidth, rect.y, buttonWidth, rect.height);
                        if (GUI.Button(buttonRect, "Check out", styles.lockedHeaderButton))
                        {
                            EditorPrefs.SetBool("vcssticky", true);
                            // TODO: Retrieve default CheckoutMode from VC settings (depends on asset type; native vs. imported)
                            Task task = Provider.Checkout(assetEditor.targets, CheckoutMode.Both);
                            task.Wait();
                            Repaint();
                        }
                    }
                    DrawVCSSticky(rect.height / 2);
                }
            }
        }

        protected string BuildTooltip(Asset asset, Asset metaAsset)
        {
            var sb = new System.Text.StringBuilder();
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

        protected void DrawVCSShortInfoAsset(Asset asset, string tooltip, Rect rect, Texture2D icon, string currentState)
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
            EditorGUI.LabelField(textRect, content, styles.preToolbar2);
        }

        protected void AssignAssetEditor(Editor[] editors)
        {
            // Assign asset editor to importer editor
            if (editors.Length > 1 && editors[0] is AssetImporterEditor)
            {
                (editors[0] as AssetImporterEditor).assetEditor = editors[1];
            }
        }

        private void DrawEditors(Editor[] editors)
        {
            if (editors.Length == 0)
                return;

            // We need to force optimized GUI to dirty when object becomes open for edit
            // e.g. after checkout in version control. If this is not done the optimized
            // GUI will need an extra repaint before it gets ungrayed out.

            Object inspectedObject = GetInspectedObject();
            string msg = String.Empty;

            // Force header to be flush with the top of the window
            GUILayout.Space(0);

            // When inspecting a material asset force visible properties (MaterialEditor can be collapsed when shown on a GameObject)
            if (inspectedObject is Material)
            {
                // Find material editor. MaterialEditor is in either index 0 or 1 (ProceduralMaterialInspector is in index 0).
                for (int i = 0; i <= 1 && i < editors.Length; i++)
                {
                    MaterialEditor me = editors[i] as MaterialEditor;
                    if (me != null)
                    {
                        me.forceVisible = true;
                        break;
                    }
                }
            }

            bool rebuildOptimizedGUIBlocks = false;
            if (Event.current.type == EventType.Repaint)
            {
                if (inspectedObject != null
                    && m_IsOpenForEdit != Editor.IsAppropriateFileOpenForEdit(inspectedObject, out msg))
                {
                    m_IsOpenForEdit = !m_IsOpenForEdit;
                    rebuildOptimizedGUIBlocks = true;
                }
                if (m_InvalidateGUIBlockCache)
                {
                    rebuildOptimizedGUIBlocks = true;
                    m_InvalidateGUIBlockCache = false;
                }
            }
            else if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "EyeDropperUpdate")
            {
                rebuildOptimizedGUIBlocks = true;
            }

            Editor.m_AllowMultiObjectAccess = true;
            bool showImportedObjectBarNext = false;
            Rect importedObjectBarRect = new Rect();
            for (int editorIndex = 0; editorIndex < editors.Length; editorIndex++)
            {
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
                    FlushOptimizedGUIBlock(editors[editorIndex]);
                }
            }

            EditorGUIUtility.ResetGUIState();

            // Draw the bar to show that the imported object is below
            if (importedObjectBarRect.height > 0)
            {
                // Clip the label to avoid a black border at the bottom
                GUI.BeginGroup(importedObjectBarRect);
                GUI.Label(new Rect(0, 0, importedObjectBarRect.width, importedObjectBarRect.height), "Imported Object", "OL Title");
                GUI.EndGroup();
            }
        }

        internal override void OnResized()
        {
            m_InvalidateGUIBlockCache = true;
        }

        private void DrawEditor(Editor[] editors, int editorIndex, bool rebuildOptimizedGUIBlock, ref bool showImportedObjectBarNext, ref Rect importedObjectBarRect)
        {
            var editor = editors[editorIndex];
            // Protect us against someone triggering an asset reimport during
            // OnGUI as that will kill all active editors.
            if (editor == null)
                return;

            Object target = editor.target;
            // see case 891450:
            // inspector onGui starts, fetch ActiveEditorTrackers - this includes a MaterialEditor created with a canvasRenderer material
            // then, disabling a Mask component deletes this material
            // after that, either Active Editors are fetched again and the count is different OR the material is invalid and crashes the whole app
            // The target is considered invalid if the MonoBehaviour is missing, to ensure that the missing MonoBehaviour field is drawn
            // we only do not draw an editor if the target is invalid and it is not a MonoBehaviour. case: 917810
            if (!target && target.GetType() != typeof(UnityEngine.MonoBehaviour))
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
                // Init our state with last state
                wasVisible = InternalEditorUtility.GetIsInspectorExpanded(target);
                tracker.SetVisible(editorIndex, wasVisible ? 1 : 0);
            }
            else
                wasVisible = wasVisibleState == 1;

            rebuildOptimizedGUIBlock |= editor.isInspectorDirty;

            // Reset dirtyness when repainting
            if (Event.current.type == EventType.Repaint)
            {
                editor.isInspectorDirty = false;
            }

            //set the current PropertyHandlerCache to the current editor
            ScriptAttributeUtility.propertyHandlerCache = editor.propertyHandlerCache;

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

            if (editor.target is AssetImporter)
                showImportedObjectBarNext = true;

            // Culling of editors that can't be properly shown.
            // If the current editor is a GenericInspector even though a custom editor for it exists,
            // then it's either a fallback for a custom editor that doesn't support multi-object editing,
            // or we're in debug mode.
            bool multiEditingNotSupported = false;
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
                    multiEditingNotSupported = true;
                }
                else if (target is AssetImporter)
                {
                    // If we are in debug mode and it's an importer type,
                    // hide the editor and show a notification.
                    multiEditingNotSupported = true;
                }
                // If we are in debug mode and it's an NOT importer type,
                // just show the debug inspector as usual.
            }

            // Dragging handle used for editor reordering
            Rect dragRect = new Rect();

            // Draw small headers (the header above each component) after the culling above
            // so we don't draw a component header for all the components that can't be shown.
            if (!largeHeader)
            {
                using (new EditorGUI.DisabledScope(!editor.IsEnabled()))
                {
                    bool isVisible = UnityEditor.EditorGUILayout.InspectorTitlebar(wasVisible, editor.targets, editor.CanBeExpandedViaAFoldout());
                    if (wasVisible != isVisible)
                    {
                        tracker.SetVisible(editorIndex, isVisible ? 1 : 0);
                        InternalEditorUtility.SetIsInspectorExpanded(target, isVisible);
                        if (isVisible)
                            m_LastInteractedEditor = editor;
                        else if (m_LastInteractedEditor == editor)
                            m_LastInteractedEditor = null;
                    }
                }

                dragRect = GUILayoutUtility.GetLastRect();
            }

            if (multiEditingNotSupported && wasVisible)
            {
                GUILayout.Label("Multi-object editing not supported.", EditorStyles.helpBox);
                return;
            }

            DisplayDeprecationMessageIfNecessary(editor);

            // We need to reset again after drawing the header.
            EditorGUIUtility.ResetGUIState();

            Rect contentRect = new Rect();
            bool excludedClass = ModuleMetadata.GetModuleIncludeSettingForObject(target) == ModuleIncludeSetting.ForceExclude;
            if (excludedClass)
                EditorGUILayout.HelpBox("The module which implements this component type has been force excluded in player settings. This object will be removed in play mode and from any builds you make.", MessageType.Warning);

            using (new EditorGUI.DisabledScope(!editor.IsEnabled() || excludedClass))
            {
                var genericEditor = editor as GenericInspector;
                if (genericEditor)
                    genericEditor.m_InspectorMode = m_InspectorMode;

                // Optimize block code path
                float height;
                OptimizedGUIBlock optimizedBlock;

                EditorGUIUtility.hierarchyMode = true;
                EditorGUIUtility.wideMode = position.width > 330;

                //set the current PropertyHandlerCache to the current editor
                ScriptAttributeUtility.propertyHandlerCache = editor.propertyHandlerCache;

                if (editor.GetOptimizedGUIBlock(rebuildOptimizedGUIBlock, wasVisible, out optimizedBlock, out height))
                {
                    contentRect = GUILayoutUtility.GetRect(0, wasVisible ? height : 0);
                    HandleLastInteractedEditor(contentRect, editor);

                    // Layout events are ignored in the optimized code path
                    if (Event.current.type == EventType.Layout)
                    {
                        return;
                    }

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
                        return;
                    }
                }
            }

            editorDragging.HandleDraggingToEditor(editorIndex, dragRect, contentRect, m_Tracker);

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

        internal void RepaintImmediately(bool rebuildOptimizedGUIBlocks)
        {
            m_InvalidateGUIBlockCache = rebuildOptimizedGUIBlocks;
            RepaintImmediately();
        }

        public bool EditorHasLargeHeader(int editorIndex, Editor[] trackerActiveEditors)
        {
            var target = trackerActiveEditors[editorIndex].target;
            return AssetDatabase.IsMainAsset(target) || AssetDatabase.IsSubAsset(target) || editorIndex == 0 || target is Material;
        }

        private void DisplayDeprecationMessageIfNecessary(Editor editor)
        {
            if (!editor || !editor.target) return;

            var obsoleteAttribute = (ObsoleteAttribute)Attribute.GetCustomAttribute(editor.target.GetType(), typeof(ObsoleteAttribute));
            if (obsoleteAttribute == null) return;

            string message = String.IsNullOrEmpty(obsoleteAttribute.Message) ? "This component has been marked as obsolete." : obsoleteAttribute.Message;
            EditorGUILayout.HelpBox(message, obsoleteAttribute.IsError ? MessageType.Error : MessageType.Warning);
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

        public bool ShouldCullEditor(Editor[] editors, int editorIndex)
        {
            if (editors[editorIndex].hideInspector)
                return true;

            Object currentTarget = editors[editorIndex].target;

            // Objects that should always be hidden
            if (currentTarget is SubstanceImporter || currentTarget is ParticleSystemRenderer)
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
                if (GUI.Button(r, ts.label, styles.typeSelection))
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
            Editor editor = GetFirstNonImportInspectorEditor(editors);
            if (editor != null && editor.target != null && editor.target is GameObject && editor.IsEnabled())
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                var content = s_Styles.addComponentLabel;
                Rect rect = GUILayoutUtility.GetRect(content, styles.addComponentButtonStyle);

                // Visually separates the Add Component button from the existing components
                if (Event.current.type == EventType.Repaint)
                    DrawSplitLine(rect.y - 11);

                Event evt = Event.current;
                bool openWindow = false;
                switch (evt.type)
                {
                    case EventType.ExecuteCommand:
                        string commandName = evt.commandName;
                        if (commandName == "OpenAddComponentDropdown")
                        {
                            openWindow = true;
                            evt.Use();
                        }
                        break;
                }

                if (EditorGUI.DropdownButton(rect, content, FocusType.Passive, styles.addComponentButtonStyle) || openWindow)
                {
                    if (AddComponentWindow.Show(rect, editor.targets.Select(o => (GameObject)o).ToArray()))
                        GUIUtility.ExitGUI();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        private bool ReadyToRepaint()
        {
            if (AnimationMode.InAnimationPlaybackMode())
            {
                long timeNow = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
                if (timeNow - s_LastUpdateWhilePlayingAnimation < delayRepaintWhilePlayingAnimation)
                    return false;
                s_LastUpdateWhilePlayingAnimation = timeNow;
            }

            return true;
        }

        private void DrawSplitLine(float y)
        {
            Rect position = new Rect(0, y, this.m_Pos.width + 1, 1);
            Rect uv = new Rect(0, 1f, 1, 1f - 1f / EditorStyles.inspectorTitlebar.normal.background.height);
            GUI.DrawTextureWithTexCoords(position, EditorStyles.inspectorTitlebar.normal.background, uv);
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
                    FlushOptimizedGUIBlock(editor);
            }
        }

        private static void FlushOptimizedGUIBlock(Editor editor)
        {
            if (editor == null)
                return;

            OptimizedGUIBlock optimizedBlock;
            float height;
            if (editor.GetOptimizedGUIBlock(false, false, out optimizedBlock, out height))
            {
                optimizedBlock.valid = false;
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
    }
}
