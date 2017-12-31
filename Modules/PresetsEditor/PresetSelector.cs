// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace UnityEditor.Presets
{
    public abstract class PresetSelectorReceiver : ScriptableObject
    {
        public virtual void OnSelectionChanged(Preset selection) {}
        public virtual void OnSelectionClosed(Preset selection) {}
    }

    public class DefaultPresetSelectorReceiver : PresetSelectorReceiver
    {
        Object[] m_Targets;
        Preset[] m_InitialValues;

        internal void Init(Object[] targets)
        {
            m_Targets = targets;
            m_InitialValues = targets.Select(a => new Preset(a)).ToArray();
        }

        public override void OnSelectionChanged(Preset selection)
        {
            if (selection != null)
            {
                Undo.RecordObjects(m_Targets, "Apply Preset " + selection.name);
                foreach (var target in m_Targets)
                {
                    selection.ApplyTo(target);
                }
            }
            else
            {
                Undo.RecordObjects(m_Targets, "Cancel Preset");
                for (int i = 0; i < m_Targets.Length; i++)
                {
                    m_InitialValues[i].ApplyTo(m_Targets[i]);
                }
            }
        }

        public override void OnSelectionClosed(Preset selection)
        {
            OnSelectionChanged(selection);
            DestroyImmediate(this);
        }
    }

    public class PresetSelector : EditorWindow
    {
        static class Style
        {
            public static GUIStyle bottomBarBg = "ProjectBrowserBottomBarBg";
            public static GUIStyle toolbarBack = "ObjectPickerToolbar";
            public static GUIContent presetIcon = EditorGUIUtility.IconContent("Preset.Context");
        }

        // Filter
        string m_SearchField;
        IEnumerable<Preset> m_Presets;

        ObjectListAreaState m_ListAreaState;
        ObjectListArea  m_ListArea;

        // Layout
        const float kMinTopSize = 250;
        const float kMinWidth = 200;
        const float kPreviewMargin = 5;
        const float kPreviewExpandedAreaHeight = 75;
        SavedInt    m_StartGridSize = new SavedInt("PresetSelector.GridSize", 64);

        bool m_CanCreateNew;
        int m_ModalUndoGroup = -1;
        Object m_MainTarget;

        // get an existing ObjectSelector or create one
        static PresetSelector s_SharedPresetSelector = null;
        PresetSelectorReceiver m_EventObject;

        internal static PresetSelector get
        {
            get
            {
                if (s_SharedPresetSelector == null)
                {
                    Object[] objs = Resources.FindObjectsOfTypeAll(typeof(PresetSelector));
                    if (objs != null && objs.Length > 0)
                        s_SharedPresetSelector = (PresetSelector)objs[0];
                    if (s_SharedPresetSelector == null)
                        s_SharedPresetSelector = CreateInstance<PresetSelector>();
                }
                return s_SharedPresetSelector;
            }
        }

        [EditorHeaderItem(typeof(Object), -1001)]
        public static bool DrawPresetButton(Rect rectangle, Object[] targets)
        {
            var target = targets[0];

            if (Preset.IsObjectExcludedFromPresets(target)
                || (target.hideFlags & HideFlags.NotEditable) != 0)
                return false;

            if (EditorGUI.DropdownButton(rectangle, Style.presetIcon , FocusType.Passive,
                    EditorStyles.iconButton))
            {
                PresetContextMenu.CreateAndShow(targets);
            }
            return true;
        }

        public static void ShowSelector(Object[] targets, Preset currentSelection, bool createNewAllowed)
        {
            var eventHolder = CreateInstance<DefaultPresetSelectorReceiver>();
            eventHolder.Init(targets);
            ShowSelector(targets[0], currentSelection, createNewAllowed, eventHolder);
        }

        public static void ShowSelector(Object target, Preset currentSelection, bool createNewAllowed, PresetSelectorReceiver eventReceiver)
        {
            get.Init(target, currentSelection, createNewAllowed, eventReceiver);
        }

        void Init(Object target, Preset currentSelection, bool createNewAllowed, PresetSelectorReceiver eventReceiver)
        {
            m_ModalUndoGroup = Undo.GetCurrentGroup();

            // Freeze to prevent flicker on OSX.
            // Screen will be updated again when calling
            // SetFreezeDisplay(false) further down.
            ContainerWindow.SetFreezeDisplay(true);

            // Set member variables
            m_SearchField = string.Empty;
            m_MainTarget = target;
            InitListArea();
            m_Presets = FindAllPresetsForObject(target);
            UpdateSearchResult(currentSelection != null ? currentSelection.GetInstanceID() : 0);

            m_EventObject = eventReceiver;
            m_CanCreateNew = createNewAllowed;

            ShowWithMode(ShowMode.AuxWindow);
            titleContent = EditorGUIUtility.TrTextContent("Select Preset");

            // Deal with window size
            Rect rect = m_Parent.window.position;
            rect.width = EditorPrefs.GetFloat("PresetSelectorWidth", 200);
            rect.height = EditorPrefs.GetFloat("PresetSelectorHeight", 390);
            position = rect;
            minSize = new Vector2(kMinWidth, kMinTopSize + kPreviewExpandedAreaHeight + 2 * kPreviewMargin);
            maxSize = new Vector2(10000, 10000);

            // Focus
            Focus();
            ContainerWindow.SetFreezeDisplay(false);

            // Add after unfreezing display because AuxWindowManager.cpp assumes that aux windows are added after we get 'got/lost'- focus calls.
            m_Parent.AddToAuxWindowList();
        }

        static IEnumerable<Preset> FindAllPresetsForObject(Object target)
        {
            return AssetDatabase.FindAssets("t:Preset")
                .Select(a => AssetDatabase.LoadAssetAtPath<Preset>(AssetDatabase.GUIDToAssetPath(a)))
                .Where(preset => preset.CanBeAppliedTo(target));
        }

        void InitListArea()
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
                m_ListArea.allowBuiltinResources = false;
                m_ListArea.repaintCallback += Repaint;
                m_ListArea.itemSelectedCallback += ListAreaItemSelectedCallback;
                m_ListArea.gridSize = m_StartGridSize.value;
            }
        }

        void UpdateSearchResult(int currentSelection)
        {
            var searchResult = m_Presets
                .Where(p => p.name.ToLower().Contains(m_SearchField.ToLower()))
                .Select(p => p.GetInstanceID())
                .ToArray();
            m_ListArea.ShowObjectsInList(searchResult);
            m_ListArea.InitSelection(new[] { currentSelection });
        }

        void ListAreaItemSelectedCallback(bool doubleClicked)
        {
            if (doubleClicked)
            {
                Close();
                GUIUtility.ExitGUI();
            }
            else
            {
                if (m_EventObject != null)
                {
                    m_EventObject.OnSelectionChanged(GetCurrentSelection());
                }
            }
        }

        void OnGUI()
        {
            m_ListArea.HandleKeyboard(false);
            HandleKeyInput();
            EditorGUI.FocusTextInControl("ComponentSearch");
            DrawSearchField();

            var listPosition = EditorGUILayout.GetControlRect(true, GUILayout.ExpandHeight(true));
            int listKeyboardControlID = GUIUtility.GetControlID(FocusType.Keyboard);
            m_ListArea.OnGUI(new Rect(0, listPosition.y, position.width, listPosition.height), listKeyboardControlID);

            using (new EditorGUILayout.HorizontalScope(Style.bottomBarBg, GUILayout.MinHeight(24f)))
            {
                if (m_CanCreateNew)
                {
                    if (GUILayout.Button("Save current to..."))
                    {
                        CreatePreset(m_MainTarget);
                    }
                }
                GUILayout.FlexibleSpace();
                if (m_ListArea.CanShowThumbnails())
                {
                    using (var change = new EditorGUI.ChangeCheckScope())
                    {
                        int newGridSize = (int)GUILayout.HorizontalSlider(m_ListArea.gridSize, m_ListArea.minGridSize, m_ListArea.maxGridSize, GUILayout.Width(55f));
                        if (change.changed)
                        {
                            m_ListArea.gridSize = newGridSize;
                        }
                    }
                }
            }
        }

        void DrawSearchField()
        {
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                GUI.SetNextControlName("ComponentSearch");
                var rect = EditorGUILayout.GetControlRect(false, 24f, Style.toolbarBack);
                rect.height = 40f;
                GUI.Label(rect, GUIContent.none, Style.toolbarBack);
                m_SearchField = EditorGUI.SearchField(new Rect(5f, 5f, position.width - 10f, 15f), m_SearchField);
                if (change.changed)
                {
                    UpdateSearchResult(0);
                }
            }
        }

        void HandleKeyInput()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.Escape:
                        if (m_SearchField == string.Empty)
                        {
                            Cancel();
                        }
                        break;
                    case KeyCode.KeypadEnter:
                    case KeyCode.Return:
                        Close();
                        Event.current.Use();
                        GUIUtility.ExitGUI();
                        break;
                }
            }
        }

        void OnDisable()
        {
            if (m_ListArea != null)
                m_StartGridSize.value = m_ListArea.gridSize;
            if (m_EventObject != null)
            {
                m_EventObject.OnSelectionClosed(GetCurrentSelection());
            }

            Undo.CollapseUndoOperations(m_ModalUndoGroup);
        }

        void OnDestroy()
        {
            if (m_ListArea != null)
                m_ListArea.OnDestroy();
        }

        Preset GetCurrentSelection()
        {
            Preset selection = null;
            if (m_ListArea != null)
            {
                var id = m_ListArea.GetSelection();
                if (id != null && id.Length > 0)
                    selection = EditorUtility.InstanceIDToObject(id[0]) as Preset;
            }
            return selection;
        }

        void Cancel()
        {
            Undo.RevertAllDownToGroup(m_ModalUndoGroup);

            // Clear selection so that object field doesn't grab it
            m_ListArea.InitSelection(new int[0]);

            Close();
            GUI.changed = true;
            GUIUtility.ExitGUI();
        }

        static bool ApplyImportSettingsBeforeSavingPreset(ref Preset preset, Object target)
        {
            // make sure modifications to importer get applied before creating preset.
            foreach (InspectorWindow i in InspectorWindow.GetAllInspectorWindows())
            {
                ActiveEditorTracker activeEditor = i.tracker;
                foreach (Editor e in activeEditor.activeEditors)
                {
                    var editor = e as AssetImporterEditor;
                    if (editor != null && editor.target == target && editor.HasModified())
                    {
                        if (EditorUtility.DisplayDialog("Unapplied import settings", "Apply settings before creating a new preset", "Apply", "Cancel"))
                        {
                            editor.ApplyAndImport();
                            // after reimporting, the target object has changed, so update the preset with the newly imported values.
                            preset.UpdateProperties(editor.target);
                            return false;
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        static string CreatePresetDialog(ref Preset preset, Object target)
        {
            if (target is AssetImporter && ApplyImportSettingsBeforeSavingPreset(ref preset, target))
                return null;

            return EditorUtility.SaveFilePanelInProject("New Preset",
                preset.GetTargetTypeName(),
                "preset",
                "",
                ProjectWindowUtil.GetActiveFolderPath());
        }

        static void CreatePreset(Object target)
        {
            var preset = new Preset(target);
            var path = CreatePresetDialog(ref preset, target);
            if (!string.IsNullOrEmpty(path))
            {
                // Known issue 958603 - PPtr are NULL if we replace an asset until Unity is restarted.
                // This workaround prevent the replace and keep the PPtr valid.
                var oldPreset = AssetDatabase.LoadAssetAtPath<Preset>(path);
                if (oldPreset != null)
                {
                    EditorUtility.CopySerialized(preset, oldPreset);
                    // replace name because it was erased by the CopySerialized
                    oldPreset.name = System.IO.Path.GetFileNameWithoutExtension(path);
                }
                else
                {
                    AssetDatabase.CreateAsset(preset, path);
                }
            }
        }
    }
}
