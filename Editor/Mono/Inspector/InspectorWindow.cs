// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

using Object = UnityEngine.Object;

using AssetImporterEditor = UnityEditor.AssetImporters.AssetImporterEditor;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Inspector", useTypeNameAsIconName = true)]
    internal class InspectorWindow : PropertyEditor, IPropertyView, IHasCustomMenu
    {
        static readonly List<InspectorWindow> m_AllInspectors = new List<InspectorWindow>();
        static bool s_AllOptimizedGUIBlocksNeedsRebuild;

        [SerializeField] EditorGUIUtility.EditorLockTrackerWithActiveEditorTracker m_LockTracker = new EditorGUIUtility.EditorLockTrackerWithActiveEditorTracker();
        [SerializeField] PreviewWindow m_PreviewWindow;

        readonly HashSet<DataMode> m_UserSupportedDataModes = new(4);
        IMGUIContainer m_TrackerResetter;


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

        public bool isVisible => m_Parent.actualView == this;

        internal void Awake()
        {
            AddInspectorWindow(this);
        }

        protected override void OnDestroy()
        {
            if (m_PreviewWindow != null)
                m_PreviewWindow.Close();
            if (m_Tracker != null && !m_Tracker.Equals(ActiveEditorTracker.sharedTracker))
            {
                // Ensure that m_Tracker is null before calling Destroy(), as a callback could be made to redraw the
                // InspectorWindow, and the native representation of the tracker will already be gone
                var trackerToDestroy = m_Tracker;
                m_Tracker = null;
                trackerToDestroy.Destroy();
            }

            if (m_TrackerResetter != null)
            {
                m_TrackerResetter.Dispose();
                m_TrackerResetter = null;
            }
        }

        protected override void OnEnable()
        {
            // Enable MSAA for UIElements inspectors, which is the only supported
            // antialiasing solution for UIElements.
            antiAliasing = 4;

            RefreshTitle();
            AddInspectorWindow(this);

            base.OnEnable();

            RestoreLockStateFromSerializedData();

            if (m_LockTracker == null)
            {
                m_LockTracker = new EditorGUIUtility.EditorLockTrackerWithActiveEditorTracker();
            }

            m_LockTracker.tracker = tracker;
            m_LockTracker.lockStateChanged.AddListener(LockStateChanged);
            m_Tracker.dataMode = GetDataModeController_Internal().dataMode;

            EditorApplication.projectWasLoaded += OnProjectWasLoaded;
            Selection.selectionChanged += OnSelectionChanged;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        private void OnAfterAssemblyReload()
        {
            // Case 1348788: After reloading the assemblies after a script compilation,
            // active editors are not rebuilt automatically. If a custom editor changed
            // the way it handles multi object editing you won't see the effect in the inspector unless
            // you change the selection. It is a minor issue. But this call makes sure to rebuild the active
            // editors if necessary.
            // Note: This is only a problem when adding the attribute CanEditMultipleObjects. When the attribute is not
            // there, the editor used for multi editing is the generic inspector. If you add the CanEditMultipleObjects attribute,
            // a refresh is triggered and we check if the editor instance is still valid, which is the case for the generic inspector
            // so we don't rebuild it. When removing the CanEditMultipleObjects, the refresh sees that the editor was the custom inspector
            // but its instance is no longer valid, so it rebuilds the inspector.
            if (EditorsForMultiEditingChanged())
                tracker.ForceRebuild();
        }

        void OnBecameVisible()
        {
            SceneView.SetActiveEditorsDirty(true);
        }

        void OnBecameInvisible()
        {
            SceneView.SetActiveEditorsDirty();
        }

        private void OnProjectWasLoaded()
        {
            // EditorApplication.projectWasLoaded, which calls this, fires after OnEnabled
            // therefore the logic in OnEnabled already tried to de-serialize the locked objects, including those it only had InstanceIDs of
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

        private void OnSelectionChanged()
        {
            RebuildContentsContainers();
            if (Selection.objects.Length == 0 && m_MultiEditLabel != null)
            {
                m_MultiEditLabel.RemoveFromHierarchy();
            }

            if (isLocked)
                return;

            UpdateSupportedDataModesList();
        }

        // Note: supportedModes is cleared before and sorted after this method is called
        protected override void OnUpdateSupportedDataModes(List<DataMode> supportedModes)
        {
            // Not showing data modes in debug
            if (m_InspectorMode != InspectorMode.Normal)
                return;

            m_UserSupportedDataModes.Clear();
            DataModeSupportUtils.GetDataModeSupport(Selection.activeObject, Selection.activeContext, m_UserSupportedDataModes);
            supportedModes.AddRange(m_UserSupportedDataModes);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            RemoveInspectorWindow(this);
            m_LockTracker?.lockStateChanged.RemoveListener(LockStateChanged);

            EditorApplication.projectWasLoaded -= OnProjectWasLoaded;
            Selection.selectionChanged -= OnSelectionChanged;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
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
        internal static void RedrawFromNative()
        {
            var propertyEditors = Resources.FindObjectsOfTypeAll<PropertyEditor>();
            foreach (var propertyEditor in propertyEditors)
                propertyEditor.RebuildContentsContainers();
        }

        internal static InspectorWindow[] GetAllInspectorWindows()
        {
            return m_AllInspectors.ToArray();
        }

        public override void AddItemsToMenu(GenericMenu menu)
        {
            m_LockTracker.AddItemsToMenu(menu);
            menu.AddItem(EditorGUIUtility.TrTextContent("Properties..."), false, () => OpenPropertyEditor(GetInspectedObjects()));
            menu.AddSeparator(String.Empty);
            base.AddItemsToMenu(menu);
        }

        protected override void RefreshTitle()
        {
            string iconName = "UnityEditor.InspectorWindow";
            if (m_InspectorMode == InspectorMode.Normal)
                titleContent = EditorGUIUtility.TrTextContentWithIcon("Inspector", iconName);
            else
                titleContent = EditorGUIUtility.TrTextContentWithIcon("Debug", iconName);
        }

        protected override void UpdateWindowObjectNameTitle()
        {
            // The inspector window doesn't not track the object name.
        }

        protected override void CreateTracker()
        {
            if (m_Tracker != null)
            {
                // Ensure that inspector mode
                // This shouldn't be necessary but there are some non-reproducable bugs objects showing up as not able to multi-edit
                // because this state goes out of sync.
                m_Tracker.inspectorMode = m_InspectorMode;
                return;
            }

            m_Tracker = sharedTrackerInUse ? new ActiveEditorTracker() : ActiveEditorTracker.sharedTracker;
            m_Tracker.inspectorMode = m_InspectorMode;
            m_Tracker.RebuildIfNecessary();
        }

        bool sharedTrackerInUse
        {
            get
            {
                return m_AllInspectors.Any(i => i.m_Tracker != null && i.m_Tracker.Equals(ActiveEditorTracker.sharedTracker));
            }
        }

        protected virtual void ShowButton(Rect r)
        {
            m_LockTracker.ShowButton(r, Styles.lockButton);
        }

        private void LockStateChanged(bool lockState)
        {
            if (lockState)
            {
                PrepareLockedObjectsForSerialization();
            }
            else
            {
                ClearSerializedLockedObjects();
            }

            tracker.RebuildIfNecessary();
        }

        protected override bool CloseIfEmpty()
        {
            return false;
        }

        protected override void BeginRebuildContentContainers()
        {
            FlushAllOptimizedGUIBlocksIfNeeded();

            if (m_TrackerResetter == null)
            {
                m_TrackerResetInserted = false;
                m_TrackerResetter = CreateIMGUIContainer(() => {}, "activeEditorTrackerResetter");
                rootVisualElement.Insert(0, m_TrackerResetter);
            }
        }

        protected override bool BeginDrawPreviewAndLabels()
        {
            if (m_PreviewWindow && Event.current?.type == EventType.Repaint)
                m_PreviewWindow.Repaint();
            return m_PreviewWindow == null;
        }

        protected override void EndDrawPreviewAndLabels(Event evt, Rect rect, Rect dragRect)
        {
            if (m_HasPreview || m_PreviewWindow != null)
            {
                if (EditorGUILayout.DropdownButton(Styles.menuIcon, FocusType.Passive, Styles.preOptionsButton))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(
                        EditorGUIUtility.TrTextContent(m_PreviewWindow == null
                            ? "Convert to Floating Window"
                            : "Dock Preview to Inspector"), false,
                        () =>
                        {
                            if (m_PreviewWindow == null)
                            {
                                hasFloatingPreviewWindow = true;
                                DetachPreview(false);
                            }
                            else
                            {
                                m_PreviewWindow.Close();
                                hasFloatingPreviewWindow = false;
                            }
                        });
                    menu.AddItem(
                        EditorGUIUtility.TrTextContent(m_PreviewResizer.GetExpanded()
                            ? "Minimize in Inspector"
                            : "Restore in Inspector"), false,
                        () =>
                        {
                            m_PreviewResizer.SetExpanded(position, k_InspectorPreviewMinTotalHeight,
                                k_MinAreaAbovePreview, kBottomToolbarHeight, dragRect,
                                !m_PreviewResizer.GetExpanded());
                        });
                    menu.ShowAsContext();
                }
            }

            // Detach preview on right click in preview title bar
            if (evt.type == EventType.MouseUp && evt.button == 1 && rect.Contains(evt.mousePosition) && m_PreviewWindow == null)
                DetachPreview();
        }

        protected override void CreatePreviewEllipsisMenu(InspectorPreviewWindow window, PropertyEditor editor)
        {
            if (editor.previewWindow != null)
            {
                window.ClearEllipsisMenu();
                window.AppendActionToEllipsisMenu(
                    "Convert to Floating Window",
                    (e) =>
                    {
                        if (m_PreviewWindow == null)
                        {
                            DetachPreview(false);
                            hasFloatingPreviewWindow = true;
                            window.parent.Remove(window);
                        }
                        else
                        {
                            hasFloatingPreviewWindow = false;
                            m_PreviewWindow.Close();
                        }
                    },
                    a => hasFloatingPreviewWindow ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

                window.AppendActionToEllipsisMenu(
                    "Minimize in Inspector",
                    (e) =>
                    {
                        editor.ExpandCollapsePreview();
                    },
                    a => !editor.showingPreview ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal
                );
            }
        }

        private void DetachPreview(bool exitGUI = true)
        {
            if (Event.current != null)
                Event.current.Use();
            m_PreviewWindow = CreateInstance(typeof(PreviewWindow)) as PreviewWindow;
            m_PreviewWindow.SetParentInspector(this);
            m_PreviewWindow.RebuildContentsContainers();
            m_PreviewWindow.Show();
            Repaint();
            UIEventRegistration.MakeCurrentIMGUIContainerDirty();
            if (exitGUI)
                GUIUtility.ExitGUI();
        }

        [UsedByNativeCode]
        internal static void ShowWindow()
        {
            GetWindow(typeof(InspectorWindow));
        }

        private static void FlushAllOptimizedGUIBlocksIfNeeded()
        {
            if (!s_AllOptimizedGUIBlocksNeedsRebuild)
                return;
            s_AllOptimizedGUIBlocksNeedsRebuild = false;
        }

        private void PrepareLockedObjectsForSerialization()
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
                        if (m_ObjectsLockedBeforeSerialization[i] != null)
                            m_InstanceIDsLockedBeforeSerialization.Add(m_ObjectsLockedBeforeSerialization[i].GetInstanceID());
                        m_ObjectsLockedBeforeSerialization.RemoveAt(i);
                    }
                }
            }
        }

        private void ClearSerializedLockedObjects()
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

        private void RestoreLockStateFromSerializedData()
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
            EditorApplication.CallDelayed(tracker.RebuildIfNecessary, 0f);
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

        internal static void ApplyChanges()
        {
            foreach (var inspector in m_AllInspectors)
            {
                foreach (var editor in inspector.tracker.activeEditors)
                {
                    if(editor.hasUnsavedChanges)
                        editor.SaveChanges();
                }
            }
        }

        internal static void RefreshInspectors()
        {
            foreach (var inspector in m_AllInspectors)
            {
                inspector.tracker.ForceRebuild();
            }
        }

        internal override Object GetInspectedObject()
        {
            if (tracker.hasComponentsWhichCannotBeMultiEdited && !tracker.isLocked)
                return Selection.activeObject;

            Editor editor = InspectorWindowUtils.GetFirstNonImportInspectorEditor(tracker.activeEditors);
            if (editor == null)
                return null;
            return editor.target;
        }

        internal Object[] GetInspectedObjects()
        {
            if (tracker.hasComponentsWhichCannotBeMultiEdited && !tracker.isLocked)
                return Selection.objects;

            Editor editor = InspectorWindowUtils.GetFirstNonImportInspectorEditor(tracker.activeEditors);
            if (editor == null)
                return null;
            return editor.targets;
        }

        private bool EditorsForMultiEditingChanged()
        {
            foreach (var editor in tracker.activeEditors)
            {
                if (EditorForMultiEditingChanged(editor, editor.target))
                    return true;
            }

            return false;
        }

        private static bool EditorForMultiEditingChanged(Editor editor, Object target)
        {
            if (editor.targets.Length <= 1)
                return false;

            var currentEditorType = editor.GetType();
            var expectedEditorType = CustomEditorAttributes.FindCustomEditorType(target, true);

            // Going from generic to generic inspector for multi editing is correctly handled.
            if (editor is GenericInspector && expectedEditorType == null)
                return false;
            return currentEditorType != expectedEditorType;
        }
    }
}
