// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

using UnityObject = UnityEngine.Object;
using System.Collections.ObjectModel;

namespace UnityEditor
{
    internal class InspectorHistory : ScriptableObject
    {
        static InspectorHistory()
        {
            Undo.selectionUndoRedoPerformed += NextChangeWillBeUndo;

            //Initialize the history with the current selection.
            Selection.selectionChanged += OnSelectionChanged;
        }

        static void CreateGlobalHistory()
        {
            if (s_GlobalHistory == null)
            {
                UnityObject[] histories = Resources.FindObjectsOfTypeAll(typeof(InspectorHistory));

                s_GlobalHistory = histories.Cast<InspectorHistory>().FirstOrDefault(t => t.m_IsGlobal);

                if (s_GlobalHistory == null)
                {
                    s_GlobalHistory = InspectorHistory.CreateInstance<InspectorHistory>();
                    s_GlobalHistory.m_Inspector = null;
                    s_GlobalHistory.m_GlobalHistory = new List<PrivateItem>();
                    s_GlobalHistory.m_IsGlobal = true;
                    s_GlobalHistory.PushHistory(Selection.objects);
                }
            }
        }

        internal static InspectorHistory globalHistory
        {
            get
            {
                CreateGlobalHistory();
                return s_GlobalHistory;
            }
        }

        static InspectorHistory s_GlobalHistory;
        static bool s_NextChangeUndoRedo;
        static bool s_NextChangeRedo;


        [SerializeField]
        bool m_IsGlobal = false;

        [SerializeField]
        bool m_ForceNextSelection = true;

        [SerializeField]
        bool m_WasEmpty = true;

        [SerializeField]
        bool m_IgnoreNextChange = false;

        [SerializeField]
        InspectorWindow m_Inspector; // inspector windows will not be null, if and only if Inspector window is locked, otherwise the inspector windows share the globalHistory

        [SerializeField]
        List<PrivateItem> m_History = new List<PrivateItem>();

        [SerializeField]
        List<PrivateItem> m_GlobalHistory = null;

        const int k_InvalidHistoryPos = -1;
        [SerializeField]
        int m_PosInHistory = k_InvalidHistoryPos;

        // track of navigation in history to keep up when a selection change with undo redo should undo/redo a navigation change made the the forward,/back buttons
        [SerializeField]
        List<int> m_SelectionChangeHistory = new List<int>();

        [SerializeField]
        int m_PosInSelectionChangeHistory = k_InvalidHistoryPos;


        internal bool IsSameAsSelection(Item item)
        {
            if (m_Inspector == null)
                return IsSame(Selection.objects, item.objects);
            else
            {
                List<UnityObject> lockedObjects = new List<UnityObject>();
                m_Inspector.GetObjectsLocked(lockedObjects);
                return IsSame(lockedObjects, item.objects);
            }
        }

        static void SelectionRedo()
        {
            InspectorHistory globalHistory = s_GlobalHistory;
            if (globalHistory.m_SelectionChangeHistory[globalHistory.m_PosInSelectionChangeHistory] == k_InvalidHistoryPos)
            {
                if (globalHistory.m_PosInHistory == k_InvalidHistoryPos)
                {
                    globalHistory.m_PosInHistory = 0;
                }
                else if (globalHistory.m_PosInHistory < globalHistory.m_History.Count - 1)
                {
                    globalHistory.m_PosInHistory++;
                    return;
                }
            }
            else
            {
                int wantedPos = globalHistory.m_SelectionChangeHistory[globalHistory.m_PosInSelectionChangeHistory];
                if (wantedPos >= 0 || wantedPos < globalHistory.m_History.Count)
                {
                    globalHistory.m_PosInHistory = wantedPos;
                    return;
                }
            }
            globalHistory.m_PosInSelectionChangeHistory++;
        }

        static void SelectionUndo()
        {
            if (globalHistory.m_PosInSelectionChangeHistory == 0 || globalHistory.m_SelectionChangeHistory[globalHistory.m_PosInSelectionChangeHistory] == k_InvalidHistoryPos)
            {
                if (globalHistory.m_PosInHistory > 0)
                {
                    globalHistory.m_PosInHistory--;
                    return;
                }
            }
            else
            {
                int wantedPos = globalHistory.m_SelectionChangeHistory[globalHistory.m_PosInSelectionChangeHistory];
                if (wantedPos >= 0 || wantedPos < globalHistory.m_History.Count)
                {
                    globalHistory.m_PosInHistory = wantedPos;
                    return;
                }
            }
            globalHistory.m_PosInSelectionChangeHistory--;
        }

        static void OnSelectionChanged()
        {
            if (s_GlobalHistory == null) return;

            InspectorHistory globalHistory = s_GlobalHistory;

            // Can happen if some selection change happen before an inspector is opened.
            if (globalHistory.m_PosInSelectionChangeHistory >= 0 && globalHistory.m_PosInSelectionChangeHistory < globalHistory.m_SelectionChangeHistory.Count)
            {
                if (s_NextChangeUndoRedo)
                {
                    if (s_NextChangeRedo)
                    {
                        SelectionRedo();
                    }
                    else
                    {
                        SelectionUndo();
                    }

                    s_NextChangeUndoRedo = false;
                    return;
                }
            }

            if (!globalHistory.m_IgnoreNextChange)
            {
                globalHistory.m_SelectionChangeHistory.Add(k_InvalidHistoryPos);
                globalHistory.m_PosInSelectionChangeHistory++;
            }
            globalHistory.PushHistory(Selection.objects.ToArray());
        }

        static void NextChangeWillBeUndo(Undo.UndoRedoType redo)
        {
            s_NextChangeUndoRedo = true;
            s_NextChangeRedo = redo == Undo.UndoRedoType.Redo;
        }

        void ForceNextSelection()
        {
            m_ForceNextSelection = true;
        }

        internal int posInHistory { get { return m_PosInHistory; } }

        void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;

            if (m_IsGlobal) //This allows setting the s_GlobalHistory on domain reload
            {
                s_GlobalHistory = this;
            }

            m_GUI = new InspectorHistoryGUI(this);
        }

        InspectorHistoryGUI m_GUI;

        internal static InspectorHistory CreateInstance(InspectorWindow lockedInspector)
        {
            CreateGlobalHistory();
            InspectorHistory newHistory = CreateInstance<InspectorHistory>();
            newHistory.m_Inspector = lockedInspector;
            if (newHistory.m_Inspector != null)
            {
                var objs = new List<UnityObject>();
                newHistory.m_Inspector.GetObjectsLocked(objs);
                newHistory.PushHistory(objs.ToArray());
            }

            return newHistory;
        }

        internal abstract class Item
        {
            public abstract UnityObject[] objects
            {
                get;
            }
        }
        class ItemImpl : Item
        {
            PrivateItem m_Item;
            public ItemImpl(PrivateItem item)
            {
                m_Item = item;
            }

            public UnityObject[] m_Objects;

            public override UnityObject[] objects
            {
                get { return m_Objects; }
            }

            internal PrivateItem item
            {
                get { return m_Item; }
            }
        }

        [Serializable]
        class PrivateItem
        {
            [SerializeField]
            UnityObject[] m_Objects;

            ItemImpl m_CleanupCache;

            public UnityObject[] objects
            {
                get
                {
                    return m_Objects;
                }
                set
                {
                    m_Objects = value;
                }
            }
            public bool empty
            {
                get
                {
                    return !m_Objects.Any(t => t != null);
                }
            }

            public Item CleanedUp()
            {
                if (empty)
                {
                    return null;
                }
                if (m_CleanupCache == null)
                {
                    m_CleanupCache = new ItemImpl(this);
                }
                if (m_Objects.Any(t => t == null))
                {
                    m_CleanupCache.m_Objects = m_Objects.Where(t => t != null).ToArray();
                }
                else
                {
                    m_CleanupCache.m_Objects = m_Objects;
                }
                return m_CleanupCache;
            }
        }

        internal bool canGoBack
        {
            get
            {
                if (m_PosInHistory > 0 || (m_WasEmpty && m_History.Count > 0))
                {
                    for (int i = m_WasEmpty ? m_PosInHistory : m_PosInHistory - 1; i >= 0; --i)
                    {
                        if (!m_History[i].empty) return true;
                    }
                }
                return false;
            }
        }

        internal bool canGoForward
        {
            get
            {
                if (m_PosInHistory != k_InvalidHistoryPos && m_PosInHistory < m_History.Count - 1)
                {
                    for (int i = m_PosInHistory + 1; i < m_History.Count; ++i)
                    {
                        if (!m_History[i].empty) return true;
                    }
                }
                return false;
            }
        }

        internal void GoBack()
        {
            if (canGoBack)
            {
                if (!m_WasEmpty)
                    --m_PosInHistory;
                else
                    m_WasEmpty = false;

                while (m_PosInHistory > 0 && m_History[m_PosInHistory].empty)
                {
                    --m_PosInHistory;
                }

                SelectCurrentItem();
            }
        }

        internal void GoForward()
        {
            if (canGoForward)
            {
                ++m_PosInHistory;

                while (m_PosInHistory < m_History.Count && m_History[m_PosInHistory].empty)
                {
                    --m_PosInHistory;
                }

                SelectCurrentItem();
            }
        }

        const int maxCountInBackForwardMenus = 20;
        const int maxCountHistoryMenus = 35;

        internal IEnumerable<Item> backItems
        {
            get
            {
                int cpt = 0;
                for (int i = (m_WasEmpty ? m_PosInHistory : m_PosInHistory - 1); i >= 0 && cpt < maxCountInBackForwardMenus; --i)
                {
                    var item = m_History[i].CleanedUp();
                    if (item != null)
                    {
                        yield return item;
                        ++cpt;
                    }
                }
            }
        }

        internal IEnumerable<Item> forwardItems
        {
            get
            {
                int cpt = 0;
                for (int i = m_PosInHistory + 1; i < m_History.Count && cpt < maxCountInBackForwardMenus; ++i)
                {
                    var item = m_History[i].CleanedUp();
                    if (item != null)
                    {
                        yield return item;
                        ++cpt;
                    }
                }
            }
        }

        internal void OnSelectBackItem(Item userData)
        {
            if (m_Inspector == null)
            {
                m_SelectionChangeHistory.Add(m_PosInHistory);
                m_PosInSelectionChangeHistory++;
            }
            m_PosInHistory = m_History.IndexOf((userData as ItemImpl).item);
            SelectCurrentItem();
        }

        void SelectCurrentItem()
        {
            m_ForceNextSelection = true;
            if (m_Inspector != null)
            {
                m_Inspector.SetObjectsLocked(m_History[m_PosInHistory].objects.ToList());
            }
            else
            {
                m_IgnoreNextChange = true;
                Selection.objects = m_History[m_PosInHistory].objects;
            }
        }

        internal void PushHistory(UnityObject[] objects)
        {
            if (m_IgnoreNextChange)
            {
                m_IgnoreNextChange = false;
                m_ForceNextSelection = false;
                return;
            }
            bool shouldCreateNewSelection = false;
            bool isEmpty = objects.Length == 0;
            if (!isEmpty)
            {
                if (m_WasEmpty)
                {
                    //create a new selection only if the new selection is different from the old selection
                    shouldCreateNewSelection = m_History.Count == 0 || !IsSame(m_History[m_PosInHistory].objects, objects);
                }
                else
                {
                    // if the user is just adding or removing from the selection :  simply replace the current item in the history
                    // that way each step of a multi selection is not recorded as a separate selection
                    bool containsNewSet = !objects.Except(m_History[m_PosInHistory].objects).Any();
                    bool isContainedByNewSet = !m_History[m_PosInHistory].objects.Except(objects).Any();

                    if (!containsNewSet && !isContainedByNewSet)
                    {
                        shouldCreateNewSelection = true;
                    }
                }
            }

            if (!isEmpty)
            {
                if (!shouldCreateNewSelection && !m_ForceNextSelection)
                {
                    m_History[m_PosInHistory].objects = objects;
                }
                else
                {
                    // If we first went back in the history and the make a new selection, erase
                    // all the old steps after the current selection.
                    if (m_PosInHistory < m_History.Count - 1)
                    {
                        m_History.RemoveRange(m_PosInHistory, m_History.Count - m_PosInHistory);
                        m_PosInHistory--;
                    }

                    PrivateItem item = new PrivateItem() { objects = objects };
                    m_History.Add(item);

                    item = new PrivateItem() { objects = objects.ToArray()};
                    globalHistory.m_GlobalHistory.Add(item);

                    if (m_PosInHistory == k_InvalidHistoryPos)
                        m_PosInHistory = 0;
                    else
                        m_PosInHistory++;
                }
            }
            m_WasEmpty = isEmpty;
            m_ForceNextSelection = false;
        }

        static bool IsSame(IList<UnityObject> a, IList<UnityObject> b)
        {
            if (a.Count != b.Count)
                return false;
            for (int i = 0; i < a.Count; ++i)
            {
                if (!b.Contains(a[i]))
                    return false;
            }
            return true;
        }

        internal void Select(Item item)
        {
            if (m_Inspector != null)
            {
                var objects = item.objects;
                m_Inspector.SetObjectsLocked(objects.ToList());
                PushHistory(item.objects);
            }
            else
                Selection.objects = item.objects;
        }

        internal IEnumerable<Item> globalHistoryItems
        {
            get
            {
                int cpt = 0;
                for (int i = globalHistory.m_GlobalHistory.Count - 1; i >= 0 && cpt < maxCountHistoryMenus; --i)
                {
                    var item = globalHistory.m_GlobalHistory[i];
                    var current = item.CleanedUp();
                    if (current == null)
                    {
                        continue;
                    }
                    bool found = false;
                    for (int j = globalHistory.m_GlobalHistory.Count - 1; j > i; --j)
                    {
                        if (IsSame(current.objects, globalHistory.m_GlobalHistory[j].objects))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        ++cpt;
                        yield return current;
                    }
                }
            }
        }

        internal void ClearHistory()
        {
            globalHistory.m_GlobalHistory.Clear();
        }

        internal void OnHistoryGUI(InspectorWindow window)
        {
            m_GUI.OnHistoryGUI(window);
        }
    }
}
