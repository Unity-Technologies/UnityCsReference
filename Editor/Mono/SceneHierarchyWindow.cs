// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    [EditorWindowTitle(title = "Hierarchy", useTypeNameAsIconName = true)]
    internal class SceneHierarchyWindow : SearchableEditorWindow, IHasCustomMenu
    {
        public static SceneHierarchyWindow lastInteractedHierarchyWindow { get { return s_LastInteractedHierarchy; } }
        static SceneHierarchyWindow s_LastInteractedHierarchy;
        public static List<SceneHierarchyWindow> GetAllSceneHierarchyWindows() { return s_SceneHierarchyWindows; }
        static List<SceneHierarchyWindow> s_SceneHierarchyWindows = new List<SceneHierarchyWindow>();

        static class Styles
        {
            const string kCustomSorting = "CustomSorting";
            const string kWarningSymbol = "console.warnicon.sml";
            const string kWarningMessage = "The current sorting method is taking a lot of time. Consider using 'Transform Sort' in playmode for better performance.";
            public const float kPrefabHeaderHeight = 25f;

            public static GUIStyle lockButton = "IN LockButton";

            public static GUIContent fetchWarning = new GUIContent("", EditorGUIUtility.FindTexture(kWarningSymbol), kWarningMessage);
        }

        [SerializeField]
        SceneHierarchy m_SceneHierarchy;
        SceneHierarchyStageHandling m_StageHandling;

        [SerializeField]
        string m_WindowGUID;
        public string windowGUID { get { return m_WindowGUID; } }

        public SceneHierarchy sceneHierarchy { get { return m_SceneHierarchy; } }

        bool showingPrefabHeader { get { return PrefabStageUtility.GetCurrentPrefabStage() != null; } }

        void Awake()
        {
            m_HierarchyType = HierarchyType.GameObjects;

            if (string.IsNullOrEmpty(m_WindowGUID))
                m_WindowGUID = GUID.Generate().ToString();

            if (m_SceneHierarchy == null)
                m_SceneHierarchy = new SceneHierarchy();

            m_SceneHierarchy.Awake(this);
        }

        public override void OnEnable()
        {
            base.OnEnable();

            s_LastInteractedHierarchy = this;
            s_SceneHierarchyWindows.Add(this);

            m_SceneHierarchy.position = treeViewRect; // ensure SceneHierarchy has a valid rect before initializing in OnEnable
            m_SceneHierarchy.OnEnable();
            m_StageHandling = new SceneHierarchyStageHandling(this);
            m_StageHandling.OnEnable();

            titleContent = GetLocalizedTitleContent();
        }

        public override void OnDisable()
        {
            base.OnDisable();

            s_SceneHierarchyWindows.Remove(this);

            m_SceneHierarchy.OnDisable();
            m_StageHandling.OnDisable();
        }

        void OnDestroy()
        {
            // Set another existing hierarchy as last interacted if available
            if (s_LastInteractedHierarchy == this)
            {
                s_LastInteractedHierarchy = null;
                foreach (var hierarchy in s_SceneHierarchyWindows)
                    if (hierarchy != this)
                        s_LastInteractedHierarchy = hierarchy;
            }

            m_SceneHierarchy.OnDestroy();
        }

        void OnBecameVisible()
        {
            m_SceneHierarchy.OnBecameVisible();
        }

        void OnLostFocus()
        {
            m_SceneHierarchy.OnLostFocus();
        }

        void OnSelectionChange()
        {
            m_SceneHierarchy.OnSelectionChange();
        }

        void OnHierarchyChange()
        {
            m_SceneHierarchy.OnHierarchyChange();
        }

        void OnProjectWasLoaded()
        {
            m_SceneHierarchy.OnProjectWasLoaded();
        }

        void OnQuit()
        {
            m_SceneHierarchy.OnQuit();
        }

        void OnGUI()
        {
            Rect sceneHierarchyRect = new Rect(0, 0, position.width, position.height);
            Event evt = Event.current;
            if (evt.type == EventType.MouseDown && sceneHierarchyRect.Contains(evt.mousePosition))
            {
                s_LastInteractedHierarchy = this;
            }

            DoToolbarLayout();
            DoSceneHierarchy();

            ExecuteCommands();
        }

        public void ReloadData()
        {
            m_SceneHierarchy.ReloadData();
        }

        void DoSceneHierarchy()
        {
            if (showingPrefabHeader)
            {
                m_StageHandling.PrefabStageHeaderGUI(prefabHeaderRect);
            }
            m_SceneHierarchy.OnGUI(treeViewRect);
        }

        Rect prefabHeaderRect
        {
            get { return new Rect(0, EditorGUI.kWindowToolbarHeight, position.width, Styles.kPrefabHeaderHeight); }
        }

        Rect treeViewRect
        {
            get
            {
                float startY = EditorGUI.kWindowToolbarHeight + (showingPrefabHeader ? Styles.kPrefabHeaderHeight : 0);
                return new Rect(0, startY, position.width, position.height - startY);
            }
        }

        void DoToolbarLayout()
        {
            // Gameobject popup dropdown
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            m_SceneHierarchy.GameObjectCreateDropdownButton();

            //Search field GUI
            GUILayout.Space(6);

            GUILayout.FlexibleSpace();
            Event evt = Event.current;

            // When searchfield has focus give keyboard focus to the tree view on Down/UpArrow
            if (hasSearchFilterFocus && evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.DownArrow || evt.keyCode == KeyCode.UpArrow))
            {
                m_SceneHierarchy.SetFocusAndEnsureSelectedItem();
                evt.Use();
            }

            SearchFieldGUI();

            // Sortmethod GUI
            if (m_SceneHierarchy.hasSortMethods)
            {
                GUILayout.Space(6);
                m_SceneHierarchy.SortMethodsDropDownButton();
            }

            GUILayout.EndHorizontal();
        }

        void ExecuteCommands()
        {
            Event evt = Event.current;

            if (evt.type != EventType.ExecuteCommand && evt.type != EventType.ValidateCommand)
                return;

            if (evt.commandName == "Find")
            {
                if (evt.type == EventType.ExecuteCommand)
                    FocusSearchField();
                evt.Use();
            }
            else if (evt.commandName == "FrameSelected")
            {
                if (evt.type == EventType.ExecuteCommand)
                {
                    FrameObject(Selection.activeInstanceID, false);
                }
                evt.Use();
                GUIUtility.ExitGUI();
            }
        }

        internal override void SetSearchFilter(string searchFilter, SearchableEditorWindow.SearchMode searchMode, bool setAll, bool delayed = false)
        {
            base.SetSearchFilter(searchFilter, searchMode, setAll, delayed);
            m_SceneHierarchy.SetSearchFilter(searchFilter, (SearchableEditorWindow.SearchModeHierarchyWindow)searchMode);
        }

        // This method is being used by the EditorTests/TreeViewControl tests
        internal int[] GetExpandedIDs()
        {
            return m_SceneHierarchy.GetExpandedIDs();
        }

        // This method is being used by the EditorTests/Searching tests
        public string[] GetCurrentVisibleObjects()
        {
            return m_SceneHierarchy.GetCurrentVisibleObjects();
        }

        internal void SelectPrevious()
        {
            m_SceneHierarchy.SelectPrevious();
        }

        internal void SelectNext()
        {
            m_SceneHierarchy.SelectNext();
        }

        // Called from DockArea
        protected virtual void ShowButton(Rect rect)
        {
            m_SceneHierarchy.DoWindowLockButton(rect);
        }

        public void SetExpandedRecursive(int id, bool expand)
        {
            m_SceneHierarchy.SetExpandedRecursive(id, expand);
        }

        public void FrameObject(int instanceID, bool ping)
        {
            // To be able to frame the object we need to clear the search filter
            SetSearchFilter("", SearchableEditorWindow.SearchMode.All, true);
            m_SceneHierarchy.FrameObject(instanceID, ping);
        }

        void OnSceneSelectionStateChanged(Scene selectedScene)
        {
            m_SceneHierarchy.customScenes = selectedScene.IsValid() ? new[] { selectedScene } : null;
            Repaint();
        }

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            m_SceneHierarchy.AddItemsToWindowMenu(menu);
        }
    }

    internal abstract class HierarchySorting
    {
        public virtual GUIContent content { get { return null; } }
    }

    internal class TransformSorting : HierarchySorting
    {
        readonly GUIContent m_Content = new GUIContent(EditorGUIUtility.FindTexture("DefaultSorting"), "Transform Child Order");
        public override GUIContent content { get { return m_Content; } }
    }

    internal class AlphabeticalSorting : HierarchySorting
    {
        readonly GUIContent m_Content = new GUIContent(EditorGUIUtility.FindTexture("AlphabeticalSorting"), "Alphabetical Order");
        public override GUIContent content { get { return m_Content; } }
    }
}
