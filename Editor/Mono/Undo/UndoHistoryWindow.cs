// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace UnityEditor
{
    internal class UndoHistoryWindow : EditorWindow, IHasCustomMenu
    {
        static UndoHistoryWindow s_Instance;
        public static UndoHistoryWindow instance => s_Instance;

        const string k_StyleCommon = "StyleSheets/UndoHistory/UndoHistoryCommon.uss";
        const string k_StyleDark = "StyleSheets/UndoHistory/UndoHistoryDark.uss";
        const string k_StyleLight = "StyleSheets/UndoHistory/UndoHistoryLight.uss";

        static long s_LastClosedTime;

        List<HistoryItem> m_History = new List<HistoryItem>();

        List<string> m_NewUndos = new List<string>();
        int m_UndoCursor;
        // Used for caching, this way lists won't be recreated every time
        List<string> m_LastUndos = new List<string>();
        int m_LastUndoCursor;
        bool m_UndoRedoPerformed = false;

        [SerializeField]
        bool m_ShowLatestFirst = true;

        ListView m_HistoryListView;

        [MenuItem("Edit/Undo History  %u", false, 2)]
        public static void OpenUndoHistory()
        {
            // Opens the window, otherwise focuses it if it’s already open.
            if (s_Instance == null)
            {
                s_Instance = GetWindow<UndoHistoryWindow>();
            }
            else
            {
                GetWindow<UndoHistoryWindow>();
            }
            s_Instance.ScrollToCurrent();
        }

        public UndoHistoryWindow()
        {
        }

        internal void OnDisable()
        {
            Undo.undoRedoEvent -= OnUndoRedoEvent;
            s_LastClosedTime = System.DateTime.Now.Ticks / System.TimeSpan.TicksPerMillisecond;
        }

        public enum HistoryType
        {
            None,
            Undo,
            Redo
        }

        public readonly struct HistoryItem
        {
            public readonly HistoryType type;
            public readonly string undoName;
            public readonly int index;

            public HistoryItem(HistoryType type, string undoName, int index)
            {
                this.type = type;
                this.undoName = undoName;
                this.index = index;
            }
        }

        void OnUndoRedoEvent(in UndoRedoInfo undo)
        {
            m_UndoRedoPerformed = true;
            Repaint();
        }

        void OnInspectorUpdate()
        {
            Undo.GetUndoList(m_NewUndos, out m_UndoCursor);
            // Needs undo cursor cache as well
            if (!m_LastUndos.SequenceEqual(m_NewUndos) || m_UndoCursor != m_LastUndoCursor ||
                m_History.Count == 0 || m_UndoRedoPerformed)
            {
                m_LastUndos = m_NewUndos.ToList();
                m_LastUndoCursor = m_UndoCursor;

                for (int i = 0; i < m_NewUndos.Count; i++)
                    m_NewUndos[i] = m_NewUndos[i].Replace("\n", "");

                // rebuild the history list
                m_History.Clear();
                if (m_ShowLatestFirst)
                {
                    for (int i = m_NewUndos.Count - 1; i >= 0; i--)
                        m_History.Add(new HistoryItem(i < m_UndoCursor ? HistoryType.Undo : HistoryType.Redo, m_NewUndos[i], i));
                    m_History.Add(new HistoryItem(HistoryType.None, "Scene Open", -1));
                }
                else
                {
                    m_History.Add(new HistoryItem(HistoryType.None, "Scene Open", -1));
                    for (int i = 0; i < m_NewUndos.Count; i++)
                        m_History.Add(new HistoryItem(i < m_UndoCursor ? HistoryType.Undo : HistoryType.Redo, m_NewUndos[i], i));
                }

                s_Instance.m_HistoryListView.RefreshItems();
                s_Instance.ScrollToCurrent();
                m_UndoRedoPerformed = false;
            }
        }

        public void OnEnable()
        {
            if (s_Instance == null)
            {
                this.titleContent = EditorGUIUtility.TrTextContentWithIcon("Undo History", "UnityEditor.HistoryWindow");
                Undo.undoRedoEvent += OnUndoRedoEvent;

                //root of the editorwindow
                VisualElement root = rootVisualElement;
                root.styleSheets.Add(EditorGUIUtility.Load(k_StyleCommon) as StyleSheet);
                root.styleSheets.Add(EditorGUIUtility.Load(EditorGUIUtility.isProSkin ? k_StyleDark : k_StyleLight) as StyleSheet);
                root.EnableInClassList("root", true);

                var theListView = EditorGUIUtility.Load("UXML/Undo/UndoListView.uxml") as VisualTreeAsset;
                theListView.CloneTree(root);

                var visualTree = EditorGUIUtility.Load("UXML/Undo/UndoListItem.uxml") as VisualTreeAsset;

                Func<VisualElement> makeItem = visualTree.Instantiate;

                Action<VisualElement, int> bindItem = (e, i) =>
                {
                    //get info on this action
                    var item = m_History[i];

                    //get the main container
                    var container = e.Q("HistoryItem");

                    //is this the the current state?
                    bool isCurrent = item.index == m_UndoCursor;

                    //set style class for the whole Container
                    container.EnableInClassList("current", isCurrent && m_ShowLatestFirst);
                    container.EnableInClassList("current-reverse", isCurrent && !m_ShowLatestFirst);
                    container.EnableInClassList("redo", !isCurrent && item.type == HistoryType.Redo);
                    container.EnableInClassList("undo", !isCurrent && (item.type == HistoryType.Undo || item.type == HistoryType.None));

                    //get the Action label and set it's text to this undo action
                    var label = e.Q("Text-Action") as Label;
                    label.text = item.undoName;

                    if (isCurrent)
                        m_HistoryListView.selectedIndex = i;
                };

                m_HistoryListView = root.Q<ListView>();
                m_HistoryListView.bindItem = bindItem;
                m_HistoryListView.makeItem = makeItem;
                m_HistoryListView.itemsSource = m_History;
                m_HistoryListView.RefreshItems();

                m_HistoryListView.selectionChanged += OnUndoSelectionChange;

                m_LastUndos.Clear();

                s_Instance = this;
            }
            wantsLessLayoutEvents = true;
        }

        public void OnGUI()
        {
        }

        public void ScrollToCurrent()
        {
            for (var i = 0; i < m_History.Count; i++)
            {
                if (m_History[i].index == m_UndoCursor)
                {
                    s_Instance.m_HistoryListView.ScrollToItem(i);
                    break;
                }
            }
        }

        void OnUndoSelectionChange(IEnumerable<object> selectedItems)
        {
            if (m_HistoryListView.selectedItem == null)
                return;

            HistoryItem item = (HistoryItem)m_HistoryListView.selectedItem;

            int actionsRequired = Math.Abs(item.index - m_UndoCursor);

            if (item.type == HistoryType.Undo || item.type == HistoryType.None)
            {
                for (var i = 0; i < actionsRequired; i++)
                {
                    EditorUtility.DisplayProgressBar("Undo", "Undoing selected actions", (float)i / actionsRequired);
                    Undo.PerformUndo();
                }
            }
            else
            {
                for (var i = 0; i < actionsRequired; i++)
                {
                    EditorUtility.DisplayProgressBar("Undo", "Redoing selected actions", (float)i / actionsRequired);
                    Undo.PerformRedo();
                }
            }
            EditorUtility.ClearProgressBar();
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(EditorGUIUtility.TrTextContent("Show Latest Action First"), m_ShowLatestFirst == true, SetLatestActionFirst);
            menu.AddItem(EditorGUIUtility.TrTextContent("Show Latest Action Last"), m_ShowLatestFirst  == false, SetLatestActionLast);
        }

        private void SetLatestActionFirst()
        {
            m_ShowLatestFirst = true;
            m_UndoRedoPerformed = true;
        }

        private void SetLatestActionLast()
        {
            m_ShowLatestFirst = false;
            m_UndoRedoPerformed = true;
        }
    }
}
