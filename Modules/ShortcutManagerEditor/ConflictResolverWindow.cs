// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.ShortcutManagement
{
    interface IConflictResolverView
    {
        void Show(IConflictResolver conflictResolver, IEnumerable<KeyCombination> keyCombinationSequence, IEnumerable<ShortcutEntry> entries);
    }

    class ConflictResolverView : IConflictResolverView
    {
        public void Show(IConflictResolver conflictResolver, IEnumerable<KeyCombination> keyCombinationSequence, IEnumerable<ShortcutEntry> entries)
        {
            ConflictResolverWindow.Show(conflictResolver, keyCombinationSequence, entries);
        }
    }

    class ConflictResolverWindow : EditorWindow
    {
        internal const string justOnceButtonControlName = "JustOnceButton";
        internal const string alwaysButtonControlName = "AlwaysButton";
        internal const string cancelButtonControlName = "CancelButton";

        internal static ConflictResolverWindow Show(IConflictResolver conflictResolver, IEnumerable<KeyCombination> keyCombinationSequence, IEnumerable<ShortcutEntry> entries)
        {
            var win = CreateInstance<ConflictResolverWindow>();

            win.Init(conflictResolver, keyCombinationSequence, entries);
            win.minSize = new Vector2(300, 200);
            win.ShowModal();
            return win;
        }

        static class Styles
        {
            public static GUIStyle wordWrapped = EditorStyles.wordWrappedLabel;
        }

        static class Contents
        {
            public static GUIContent cancel = EditorGUIUtility.TrTextContent("Cancel");
            public static GUIContent justOnce = EditorGUIUtility.TrTextContent("Just Once");
            public static GUIContent always = EditorGUIUtility.TrTextContent("Always");

            public static GUIContent itemName = EditorGUIUtility.TrTextContent("Name");
            public static GUIContent itemType = EditorGUIUtility.TrTextContent("Type");
            public static GUIContent itemBindings = EditorGUIUtility.TrTextContent("Bindings");
        }


        class ConflictListView : TreeView
        {
            List<ShortcutEntry> m_Entries;

            enum MyColumns
            {
                Name,
                Type,
                Binding
            }

            public ConflictListView(TreeViewState state, List<ShortcutEntry> entries)
                : base(state)
            {
                m_Entries = entries;

                Reload();
            }

            public ConflictListView(TreeViewState state,
                                    MultiColumnHeader multicolumnHeader,
                                    List<ShortcutEntry> entries)
                : base(state, multicolumnHeader)
            {
                rowHeight = 20;
                showAlternatingRowBackgrounds = true;
                showBorder = true;

                m_Entries = entries;

                Reload();
            }

            protected override bool CanMultiSelect(TreeViewItem item)
            {
                return false;
            }

            protected override TreeViewItem BuildRoot()
            {
                var root = new TreeViewItem {id = -1, depth = -1, displayName = "Root"};
                var allItems = new List<TreeViewItem>(m_Entries.Count);
                for (var index = 0; index < m_Entries.Count; index++)
                {
                    var shortcutEntry = m_Entries[index];
                    allItems.Add(new TreeViewItem { id = index, depth = 0, displayName = shortcutEntry.identifier.path });
                }

                SetupParentsAndChildrenFromDepths(root, allItems);

                return root;
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                var item = m_Entries[args.item.id];

                for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
                {
                    CellGUI(args.GetCellRect(i), item, (MyColumns)args.GetColumn(i), ref args);
                }
            }

            static void CellGUI(Rect getCellRect, ShortcutEntry item, MyColumns getColumn, ref RowGUIArgs args)
            {
                switch (getColumn)
                {
                    case MyColumns.Name:
                    {
                        GUI.Label(getCellRect, item.identifier.path);
                        break;
                    }
                    case MyColumns.Type:
                    {
                        GUI.Label(getCellRect, item.type.ToString());
                        break;
                    }
                    case MyColumns.Binding:
                    {
                        GUI.Label(getCellRect, KeyCombination.SequenceToString(item.combinations));
                        break;
                    }
                }
            }
        }

        IConflictResolver m_ConflictResolver;
        List<ShortcutEntry> m_Entries;

        [SerializeField]
        TreeViewState m_TreeViewState;
        [SerializeField]
        MultiColumnHeaderState m_MulticolumnHeaderState;
        ConflictListView m_ConflictListView;

        string m_Description;

        internal void Init(IConflictResolver conflictResolver, IEnumerable<KeyCombination> keyCombinationSequence, IEnumerable<ShortcutEntry> entries)
        {
            m_ConflictResolver = conflictResolver;
            m_Entries = entries.ToList();

            var multiColumnHeader = new MultiColumnHeader(m_MulticolumnHeaderState);
            multiColumnHeader.ResizeToFit();
            m_ConflictListView = new ConflictListView(m_TreeViewState, multiColumnHeader, m_Entries);


            m_Description = string.Format("Unity has detected a conflict in your shortcut configuration, the key sequence {0} is currently assigned to the following shortcuts:", KeyCombination.SequenceToString(keyCombinationSequence));
        }

        private void OnEnable()
        {
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();

            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = Contents.itemName,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = false,
                    autoResize = true, allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = Contents.itemType,
                    headerTextAlignment = TextAlignment.Left,
                    width = 50f,
                    canSort = false,
                    autoResize = true, allowToggleVisibility = false,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = Contents.itemBindings,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = false,
                    autoResize = true, allowToggleVisibility = false,
                }
            };

            var newHeader = new MultiColumnHeaderState(columns);
            if (m_MulticolumnHeaderState != null)
            {
                MultiColumnHeaderState.OverwriteSerializedFields(m_MulticolumnHeaderState, newHeader);
            }
            m_MulticolumnHeaderState = newHeader;


            EditorApplication.LockReloadAssemblies();
        }

        private void OnDisable()
        {
            EditorApplication.UnlockReloadAssemblies();
        }

        public new void Close()
        {
            base.Close();
            GUIUtility.ExitGUI();
        }

        private void OnGUI()
        {
            GUILayout.Label(m_Description, Styles.wordWrapped);
            var rect = GUILayoutUtility.GetRect(100, 400, 50, 100);
            GUI.Box(rect, GUIContent.none);
            m_ConflictListView.OnGUI(rect);

            GUILayout.BeginHorizontal();
            bool hasSelection = m_ConflictListView.HasSelection();
            using (var scope = new EditorGUI.DisabledScope(!hasSelection))
            {
                ShortcutEntry entry = null;
                if (hasSelection)
                    entry = m_Entries[m_ConflictListView.GetSelection().First()];

                using (var onceScope = new EditorGUI.DisabledScope(entry == null || entry.type == ShortcutType.Clutch))
                {
                    GUI.SetNextControlName(justOnceButtonControlName);
                    if (GUILayout.Button(Contents.justOnce))
                    {
                        m_ConflictResolver.ExecuteOnce(entry);
                        Close();
                    }
                }

                GUI.SetNextControlName(alwaysButtonControlName);
                if (GUILayout.Button(Contents.always))
                {
                    m_ConflictResolver.ExecuteAlways(entry);
                    Close();
                }
            }

            GUI.SetNextControlName(cancelButtonControlName);
            if (GUILayout.Button(Contents.cancel))
            {
                m_ConflictResolver.Cancel();
                Close();
            }
            GUILayout.EndHorizontal();
        }
    }
}
