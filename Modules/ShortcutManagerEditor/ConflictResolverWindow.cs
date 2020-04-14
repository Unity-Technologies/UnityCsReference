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
        internal const string performButtonControlName = "PerformButton";
        internal const string cancelButtonControlName = "CancelButton";
        internal const string rebindToggleCommandName = "rebindToggle";


        internal static ConflictResolverWindow Show(IConflictResolver conflictResolver, IEnumerable<KeyCombination> keyCombinationSequence, IEnumerable<ShortcutEntry> entries)
        {
            var win = CreateInstance<ConflictResolverWindow>();
            win.Init(conflictResolver, keyCombinationSequence, entries, GUIView.focusedView);
            win.minSize = new Vector2(550, 250);
            win.maxSize = new Vector2(550, 600);
            win.ShowModal();
            win.Focus();
            return win;
        }

        static class Styles
        {
            public static GUIStyle wordWrapped = EditorStyles.wordWrappedLabel;
            public static GUIStyle commandsArea;
            public static GUIStyle panel;
            public static GUIStyle warningIcon;

            static Styles()
            {
                commandsArea = new GUIStyle();
                commandsArea.margin = new RectOffset(0, 10, 0, 0);
                commandsArea.stretchHeight = true;

                panel = new GUIStyle();
                panel.margin = new RectOffset(10, 10, 10, 10);

                warningIcon = new GUIStyle();
                warningIcon.margin = new RectOffset(15, 15, 15, 15);
            }
        }

        static class Contents
        {
            public static GUIContent description = EditorGUIUtility.TrTextContent("You can choose to perform a single command, rebind the shortcut to the selected command, or resolve the conflict in the Shortcut Manager.");
            public static GUIContent cancel = EditorGUIUtility.TrTextContent("Cancel");
            public static GUIContent perform = EditorGUIUtility.TrTextContent("Perform Selected");
            public static GUIContent rebind = EditorGUIUtility.TrTextContent("Rebind Selected");

            public static GUIContent itemName = EditorGUIUtility.TrTextContent("Name");
            public static GUIContent itemType = EditorGUIUtility.TrTextContent("Type");
            public static GUIContent itemBindings = EditorGUIUtility.TrTextContent("Shortcut");

            public static GUIContent windowTitle = EditorGUIUtility.TrTextContent("Shortcut Conflict");
            public static GUIContent rebindToSelectedCommand = EditorGUIUtility.TrTextContent("Rebind to selected command");
            public static GUIContent SelectCommandHeading = EditorGUIUtility.TrTextContent("Select a command to perform:");
            public static GUIContent OpenShortcutManager = EditorGUIUtility.TrTextContent("Resolve Conflict...");

            public static Texture2D warningIcon = (Texture2D)EditorGUIUtility.LoadRequired("Icons/ShortcutManager/alertDialog.png");
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
                    allItems.Add(new TreeViewItem { id = index, depth = 0, displayName = shortcutEntry.displayName });
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
                        GUI.Label(getCellRect, item.displayName);
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

        enum ActionSelected
        {
            Cancel,
            ExecuteOnce,
            ExecuteAlways
        }

        IConflictResolver m_ConflictResolver;
        List<ShortcutEntry> m_Entries;

        [SerializeField]
        TreeViewState m_TreeViewState;
        [SerializeField]
        MultiColumnHeaderState m_MulticolumnHeaderState;
        ConflictListView m_ConflictListView;

        bool m_Rebind;
        string m_Header;
        bool m_InitialSizingDone;
        ShortcutEntry m_SelectedEntry;

        ActionSelected m_CloseBehaviour = ActionSelected.Cancel;
        GUIView m_PreviouslyFocusedView;

        internal void Init(IConflictResolver conflictResolver, IEnumerable<KeyCombination> keyCombinationSequence, IEnumerable<ShortcutEntry> entries, GUIView previouslyFocusedView)
        {
            m_PreviouslyFocusedView = previouslyFocusedView;
            m_ConflictResolver = conflictResolver;
            m_Entries = entries.ToList();

            var multiColumnHeader = new MultiColumnHeader(m_MulticolumnHeaderState);
            multiColumnHeader.ResizeToFit();
            m_ConflictListView = new ConflictListView(m_TreeViewState, multiColumnHeader, m_Entries);


            m_Header = string.Format(L10n.Tr("The binding \"{0}\" conflicts with multiple commands."), KeyCombination.SequenceToString(keyCombinationSequence));
        }

        private void OnEnable()
        {
            titleContent = Contents.windowTitle;
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();

            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = Contents.itemName,
                    headerTextAlignment = TextAlignment.Left,
                    canSort = false,
                    autoResize = true, allowToggleVisibility = false,
                    width = 350,
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
                    width = 75f,
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
            if (m_CloseBehaviour == ActionSelected.Cancel)
                m_ConflictResolver.Cancel();

            EditorApplication.UnlockReloadAssemblies();

            //We need to delay this action, since actions can depend on the right view having focus, and when closing a window
            //that will change the current focused view to null.
            EditorApplication.CallDelayed(() => {
                m_PreviouslyFocusedView?.Focus();

                switch (m_CloseBehaviour)
                {
                    case ActionSelected.ExecuteAlways:
                        m_ConflictResolver.ExecuteAlways(m_SelectedEntry);
                        break;
                    case ActionSelected.ExecuteOnce:
                        m_ConflictResolver.ExecuteOnce(m_SelectedEntry);
                        break;
                }
            }, 0f);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(Styles.panel);

            HandleInitialSizing();
            Rect rect;
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.Label(Contents.warningIcon, Styles.warningIcon);
                EditorGUILayout.BeginVertical(Styles.commandsArea);
                {
                    GUILayout.Label(m_Header, EditorStyles.boldLabel);
                    GUILayout.Label(Contents.description, EditorStyles.wordWrappedLabel);
                    GUILayout.Label(Contents.SelectCommandHeading, EditorStyles.boldLabel);
                    rect = GUILayoutUtility.GetRect(100, 4000, 50, 10000);
                    GUI.Box(rect, GUIContent.none);

                    GUI.SetNextControlName(rebindToggleCommandName);
                    m_Rebind = GUILayout.Toggle(m_Rebind, Contents.rebindToSelectedCommand);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            m_ConflictListView.OnGUI(rect);

            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(Contents.OpenShortcutManager))
                {
                    Close();
                    m_ConflictResolver.GoToShortcutManagerConflictCategory();
                }

                GUILayout.FlexibleSpace();


                bool hasSelection = m_ConflictListView.HasSelection();
                using (new EditorGUI.DisabledScope(!hasSelection))
                {
                    ShortcutEntry entry = null;
                    if (hasSelection)
                        entry = m_Entries[m_ConflictListView.GetSelection().First()];

                    using (new EditorGUI.DisabledScope(entry == null || (entry.type == ShortcutType.Clutch && !m_Rebind)))
                    {
                        var buttonLabel = hasSelection && entry.type == ShortcutType.Clutch ? Contents.rebind : Contents.perform;
                        GUI.SetNextControlName(performButtonControlName);
                        if (GUILayout.Button(buttonLabel))
                        {
                            m_SelectedEntry = entry;
                            if (m_Rebind)
                                m_CloseBehaviour = ActionSelected.ExecuteAlways;
                            else
                                m_CloseBehaviour = ActionSelected.ExecuteOnce;
                            Close();
                            GUIUtility.ExitGUI();
                        }
                    }
                }

                GUI.SetNextControlName(cancelButtonControlName);
                if (GUILayout.Button(Contents.cancel))
                {
                    Close();
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        void HandleInitialSizing()
        {
            if (m_InitialSizingDone)
                return;

            var pos = position;
            pos.height = 250;
            position = pos;

            m_InitialSizingDone = true;
        }
    }
}
