// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

using UnityEditor.VersionControl;

// List control that manages VCAssets.  This is used in a number of places in the plugin to display and manipulate asset lists.
namespace UnityEditorInternal.VersionControl
{
    [System.Serializable]
    public class ListControl
    {
        public enum SelectDirection
        {
            Up,
            Down,
            Current
        }

        // List state persisted
        [System.Serializable]
        public class ListState
        {
            [SerializeField] public float Scroll = 0;
            [SerializeField] public List<string> Expanded = new List<string>();
        }

        // List item explansion event
        public delegate void ExpandDelegate(ChangeSet expand, ListItem item);
        ExpandDelegate expandDelegate;

        // Drag and drop completion event
        public delegate void DragDelegate(ChangeSet target);
        DragDelegate dragDelegate;

        public delegate void ActionDelegate(ListItem item, int actionIdx);
        ActionDelegate actionDelegate;

        ListItem root = new ListItem();
        ListItem active = null;
        List<ListItem> visibleList = new List<ListItem>();
        Texture2D blueTex = null;
        Texture2D greyTex = null;
        Texture2D yellowTex = null;
        [SerializeField] ListState m_listState;
        Dictionary<string, ListItem> pathSearch = new Dictionary<string, ListItem>();
        Texture2D defaultIcon = null;
        bool readOnly = false;
        bool scrollVisible = false;
        string menuFolder = null;
        string menuDefault = null;
        bool dragAcceptOnly = false;
        ListItem dragTarget = null;
        int dragCount = 0;
        SelectDirection dragAdjust = SelectDirection.Current;
        Dictionary<string, ListItem> selectList = new Dictionary<string, ListItem>();
        ListItem singleSelect = null;
        GUIContent calcSizeTmpContent = new GUIContent();

        const float c_lineHeight = 16;
        const float c_scrollWidth = 14;

        const string c_changeKeyPrefix = "_chkeyprfx_"; // Workaround to make path mapping refresh work with changesets
        const string c_metaSuffix = ".meta";
        internal const string c_emptyChangeListMessage = "Empty change list";

        // Handle unique id access to the list.  This was added to enable us to pass the list as
        // an integer.  If the unity pop up menu could handle passing simple C# objects then we
        // would not need this.  This is not thread safe and feels like a bad workaround.
        [System.NonSerialized]
        int uniqueID = 0;
        static int s_uniqueIDCount = 1;
        static Dictionary<int, ListControl> s_uniqueIDList = new Dictionary<int, ListControl>();
        static public ListControl FromID(int id) { try { return s_uniqueIDList[id]; } catch { return null; } }

        public ListState listState
        {
            get
            {
                if (m_listState == null)
                    m_listState = new ListState();
                return m_listState;
            }
        }

        // Delegate for handling list expansion events
        public ExpandDelegate ExpandEvent
        {
            get { return expandDelegate; }
            set { expandDelegate = value; }
        }

        // Delegate for handling drag and drop events
        public DragDelegate DragEvent
        {
            get { return dragDelegate; }
            set { dragDelegate = value; }
        }

        // Delegate for handling action button clicks
        public ActionDelegate ActionEvent
        {
            get { return actionDelegate; }
            set { actionDelegate = value; }
        }

        // Root item in the list
        public ListItem Root
        {
            get { return root; }
        }

        public AssetList SelectedAssets
        {
            get
            {
                AssetList list = new AssetList();
                foreach (KeyValuePair<string, ListItem> listItem in selectList)
                    if ((listItem.Value.Item as Asset) != null)
                        list.Add(listItem.Value.Item as Asset);

                return list;
            }
        }

        public ChangeSets SelectedChangeSets
        {
            get
            {
                ChangeSets list = new ChangeSets();
                foreach (KeyValuePair<string, ListItem> listItem in selectList)
                    if (listItem.Value != null && (listItem.Value.Item as ChangeSet) != null)
                        list.Add(listItem.Value.Item as ChangeSet);

                return list;
            }
        }

        public ChangeSets EmptyChangeSets
        {
            get
            {
                ListItem cur = root.FirstChild;
                ChangeSets list = new ChangeSets();
                while (cur != null)
                {
                    ChangeSet c = cur.Change;
                    bool hasEmptyChangeNotice =
                        c != null &&
                        cur.HasChildren &&
                        cur.FirstChild.Item == null &&
                        cur.FirstChild.Name == c_emptyChangeListMessage;

                    if (hasEmptyChangeNotice)
                        list.Add(c);

                    cur = cur.Next;
                }
                return list;
            }
        }

        // Is the list read only?
        public bool ReadOnly
        {
            get { return readOnly; }
            set { readOnly = value; }
        }

        // Context menu overide for folders
        public string MenuFolder
        {
            get { return menuFolder; }
            set { menuFolder = value; }
        }

        // Default context menu
        public string MenuDefault
        {
            get { return menuDefault; }
            set { menuDefault = value; }
        }

        // Folder postfix
        public bool DragAcceptOnly
        {
            get { return dragAcceptOnly; }
            set { dragAcceptOnly = value; }
        }

        public int Size
        {
            get { return visibleList.Count; }
        }

        public ListControl()
        {
            // Assign a unique id.  A workaround to pass the class as an int
            uniqueID = s_uniqueIDCount++;
            s_uniqueIDList.Add(uniqueID, this);

            // Set the active list
            active = root;
            Clear();
        }

        ~ListControl()
        {
            s_uniqueIDList.Remove(uniqueID);
        }

        public ListItem FindItemWithIdentifier(int identifier)
        {
            ListItem result = root.FindWithIdentifierRecurse(identifier);
            //if (result == null)
            //  Debug.Log("Failed to find identifier: " + identifier);

            return result;
        }

        public ListItem Add(ListItem parent, string name, Asset asset)
        {
            ListItem insert = (parent != null) ? parent : root;
            ListItem item = new ListItem();
            item.Name = name;
            // Can this be null
            item.Asset = asset;
            insert.Add(item);

            // Meta files that are next to their asset file are collapsed into
            // the asset file when rendered but only if they are the same state.
            ListItem twinAsset = GetTwinAsset(item);
            if (twinAsset != null && item.Asset != null &&
                twinAsset.Asset.state == (item.Asset.state & ~Asset.States.MetaFile))
                item.Hidden = true;

            if (item.Asset == null)
                return item;

            // Create a lookup table
            if (item.Asset.path.Length > 0)
                pathSearch[item.Asset.path.ToLower()] = item;
            return item;
        }

        public ListItem Add(ListItem parent, string name, ChangeSet change)
        {
            ListItem insert = (parent != null) ? parent : root;
            ListItem item = new ListItem();
            item.Name = name;
            item.Change = change ?? new ChangeSet(name);
            insert.Add(item);

            // Create a lookup table
            pathSearch[c_changeKeyPrefix + change.id.ToString()] = item;

            return item;
        }

        internal ListItem GetChangeSetItem(ChangeSet change)
        {
            if (change == null)
                return null;

            ListItem i = root.FirstChild;
            while (i != null)
            {
                ChangeSet c = i.Item as ChangeSet;
                if (c != null && c.id == change.id)
                {
                    return i;
                }
                i = i.Next;
            }
            return null;
        }

        public void Clear()
        {
            root.Clear();
            pathSearch.Clear();

            // Ensure the root nodes are expanded
            root.Name = "ROOT";
            root.Expanded = true;
        }

        public void Refresh()
        {
            Refresh(true);
        }

        public void Refresh(bool updateExpanded)
        {
            if (updateExpanded)
            {
                // Update the expanded state
                LoadExpanded(root);

                // Ensure the root nodes are expanded
                root.Name = "ROOT";
                root.Expanded = true;

                listState.Expanded.Clear();
                CallExpandedEvent(root, false);
            }

            SelectedRefresh();
        }

        // Synchronise selection from Unity
        public void Sync()
        {
            SelectedClear();

            foreach (UnityEngine.Object obj in Selection.objects)
            {
                if (AssetDatabase.IsMainAsset(obj))
                {
                    string projectPath = Application.dataPath.Substring(0, Application.dataPath.Length - 6);
                    string path = projectPath + AssetDatabase.GetAssetPath(obj);
                    ListItem item = PathSearchFind(path);

                    if (item != null)
                    {
                        SelectedAdd(item);
                    }
                }
            }
        }

        public bool OnGUI(Rect area, bool focus)
        {
            bool repaint = false;

            // Late init here for now
            CreateResources();

            Event e = Event.current;
            int open = active.OpenCount;
            int max = (int)(area.height / c_lineHeight);

            // Scroll up/down
            if (e.type == EventType.ScrollWheel)
            {
                repaint = true;
                listState.Scroll += e.delta.y;
                listState.Scroll = Mathf.Clamp(listState.Scroll, 0, open - max);
            }

            // Draw the scrollbar if required
            if (open > max)
            {
                Rect scrollRect = new Rect(area.x + area.width - 14, area.y, 14, area.height);
                area.width -= c_scrollWidth;
                float tmp = listState.Scroll;

                listState.Scroll = GUI.VerticalScrollbar(scrollRect, listState.Scroll, max, 0, open);
                listState.Scroll = Mathf.Clamp(listState.Scroll, 0, open - max);

                if (tmp != listState.Scroll)
                    repaint = true;

                // Seems to be some sort of bug with focus when the scrollbar 1st appears
                // This is a work around for that so on the first update we clear the hot control.
                if (!scrollVisible)
                {
                    //GUIUtility.hotControl = 0;
                    scrollVisible = true;
                }
            }
            else
            {
                if (scrollVisible)
                {
                    //GUIUtility.hotControl = 0;
                    scrollVisible = false;
                }
            }

            // Update the visible list
            UpdateVisibleList(area, listState.Scroll);

            // Only do the following in non read only mode
            if (focus && !readOnly)
            {
                // Key Handling
                if (e.isKey)
                {
                    repaint = true;
                    HandleKeyInput(e);
                }

                HandleSelectAll();

                repaint = HandleMouse(area) || repaint;

                // Adjust scroll position when dragging
                if (e.type == EventType.DragUpdated && area.Contains(e.mousePosition))
                {
                    if (e.mousePosition.y < area.y + c_lineHeight)
                    {
                        listState.Scroll = Mathf.Clamp(listState.Scroll - 1, 0, open - max);
                    }
                    else if (e.mousePosition.y > area.y + area.height - c_lineHeight)
                    {
                        listState.Scroll = Mathf.Clamp(listState.Scroll + 1, 0, open - max);
                    }
                }
            }

            DrawItems(area, focus);

            return repaint;
        }

        private bool HandleMouse(Rect area)
        {
            Event e = Event.current;
            bool repaint = false;

            // Handle mouse down clicks
            bool mouseInArea = area.Contains(e.mousePosition);
            if (e.type == EventType.MouseDown && mouseInArea)
            {
                repaint = true;
                dragCount = 0;
                GUIUtility.keyboardControl = 0;
                singleSelect = GetItemAt(area, e.mousePosition);

                // Ensure a valid selection
                if (singleSelect != null && !singleSelect.Dummy)
                {
                    // Double click handling
                    if (e.button == 0 && e.clickCount > 1 && singleSelect.Asset != null)
                        singleSelect.Asset.Edit();

                    // Expand/Contract
                    if (e.button < 2)
                    {
                        float x = area.x + ((singleSelect.Indent - 1) * 18);
                        if (e.mousePosition.x >= x && e.mousePosition.x < x + 16 && singleSelect.CanExpand)
                        {
                            singleSelect.Expanded = !singleSelect.Expanded;
                            CallExpandedEvent(singleSelect, true);
                            singleSelect = null;
                        }
                        else if (e.control || e.command)
                        {
                            // Right clicking can never de-toggle something
                            if (e.button == 1)
                                SelectedAdd(singleSelect);
                            else
                                SelectedToggle(singleSelect);
                            singleSelect = null;
                        }
                        else if (e.shift)
                        {
                            SelectionFlow(singleSelect);
                            singleSelect = null;
                        }
                        else
                        {
                            if (!IsSelected(singleSelect))
                            {
                                SelectedSet(singleSelect);
                                // Do not set singleSelect to null in order for drag to
                                // know what is dragged
                                singleSelect = null;
                            }
                        }
                    }
                }
                else if (e.button == 0)
                {
                    // Clear selection when a click was made on nothing
                    SelectedClear();
                    singleSelect = null;
                }
            }
            // Handle mouse up clicks
            else if ((e.type == EventType.MouseUp || e.type == EventType.ContextClick) && mouseInArea)
            {
                GUIUtility.keyboardControl = 0;
                singleSelect = GetItemAt(area, e.mousePosition);
                dragCount = 0;
                repaint = true;

                if (singleSelect != null && !singleSelect.Dummy)
                {
                    // right click menus - we pass the static index of the list so the menu can find what is selected
                    if (e.type == EventType.ContextClick)
                    {
                        singleSelect = null;

                        if (!IsSelectedAsset() && !string.IsNullOrEmpty(menuFolder))
                        {
                            s_uniqueIDList[uniqueID] = this;
                            EditorUtility.DisplayPopupMenu(new Rect(e.mousePosition.x, e.mousePosition.y, 0, 0), menuFolder, new MenuCommand(null, uniqueID));
                        }
                        else if (!string.IsNullOrEmpty(menuDefault))
                        {
                            s_uniqueIDList[uniqueID] = this;
                            EditorUtility.DisplayPopupMenu(new Rect(e.mousePosition.x, e.mousePosition.y, 0, 0), menuDefault, new MenuCommand(null, uniqueID));
                        }
                    }
                    // Left click up should set selection if singleSelect is in selection since
                    // this is the case where the user has clicked and hold on a selection (which may start a drag or not)
                    // and then released it.
                    else if (e.type != EventType.ContextClick && e.button == 0 && !(e.control || e.command || e.shift))
                    {
                        if (IsSelected(singleSelect))
                        {
                            SelectedSet(singleSelect);
                            singleSelect = null;
                        }
                    }
                }
            }

            if (e.type == EventType.MouseDrag && mouseInArea)
            {
                // Ive added this to stop the accidental drag messages that pop up
                // you only seem to get one when its not intentional so this should
                // give a better effect.
                ++dragCount;

                if (dragCount > 2 && Selection.objects.Length > 0)
                {
                    DragAndDrop.PrepareStartDrag();
                    if (singleSelect != null)
                    {
                        DragAndDrop.objectReferences = new UnityEngine.Object[] { singleSelect.Asset.Load() };
                    }
                    else
                    {
                        DragAndDrop.objectReferences = Selection.objects;
                    }
                    DragAndDrop.StartDrag("Move");
                }
            }

            // Drag has been updated
            if (e.type == EventType.DragUpdated)
            {
                repaint = true;
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                dragTarget = GetItemAt(area, e.mousePosition);

                // Dont target selected items
                if (dragTarget != null)
                {
                    if (IsSelected(dragTarget))
                    {
                        dragTarget = null;
                    }
                    else
                    {
                        if (dragAcceptOnly)
                        {
                            if (!dragTarget.CanAccept)
                                dragTarget = null;
                        }
                        else
                        {
                            bool canPrev = !dragTarget.CanAccept || dragTarget.PrevOpenVisible != dragTarget.Parent;
                            bool canNext = !dragTarget.CanAccept || dragTarget.NextOpenVisible != dragTarget.FirstChild;
                            float size = dragTarget.CanAccept ? 2 : c_lineHeight / 2;
                            int index = (int)((e.mousePosition.y - area.y) / c_lineHeight);
                            float pos = area.y + (index * c_lineHeight);
                            dragAdjust = SelectDirection.Current;

                            if (canPrev && e.mousePosition.y <= pos + size)
                                dragAdjust = SelectDirection.Up;
                            else if (canNext && e.mousePosition.y >= pos + c_lineHeight - size)
                                dragAdjust = SelectDirection.Down;
                        }
                    }
                }
            }

            // Drag and drop completion
            if (e.type == EventType.DragPerform)
            {
                if (dragTarget != null)
                {
                    ListItem drag = dragAdjust == SelectDirection.Current ? dragTarget : dragTarget.Parent;
                    if (dragDelegate != null && drag != null && drag.CanAccept)
                        dragDelegate(drag.Change);

                    dragTarget = null;
                }
            }

            if (e.type == EventType.DragExited)
                dragTarget = null;
            return repaint;
        }

        private void DrawItems(Rect area, bool focus)
        {
            float y = area.y;
            foreach (ListItem it in visibleList)
            {
                float x = area.x + ((it.Indent - 1) * 18);
                bool isSelected = readOnly ? false : IsSelected(it);

                if (it.Parent != null && it.Parent.Parent != null && it.Parent.Parent.Parent == null)
                    x -= 16;

                DrawItem(it, area, x, y, focus, isSelected);
                y += c_lineHeight;
            }
        }

        private void HandleSelectAll()
        {
            if (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "SelectAll")
            {
                Event.current.Use();
            }
            else if (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "SelectAll")
            {
                SelectedAll();
                Event.current.Use();
            }
        }

        // Late resource creation
        void CreateResources()
        {
            if (blueTex == null)
            {
                blueTex = new Texture2D(1, 1);
                blueTex.SetPixel(0, 0, new Color(0.23f, 0.35f, 0.55f));
                blueTex.hideFlags = HideFlags.HideAndDontSave;
                blueTex.name = "BlueTex";
                blueTex.Apply();
            }

            if (greyTex == null)
            {
                greyTex = new Texture2D(1, 1);
                greyTex.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.3f));
                greyTex.hideFlags = HideFlags.HideAndDontSave;
                greyTex.name = "GrayTex";
                greyTex.Apply();
            }

            if (yellowTex == null)
            {
                yellowTex = new Texture2D(1, 1);
                yellowTex.SetPixel(0, 0, new Color(0.5f, 0.5f, 0.2f));
                yellowTex.name = "YellowTex";
                yellowTex.hideFlags = HideFlags.HideAndDontSave;
                yellowTex.Apply();
            }

            if (defaultIcon == null)
            {
                defaultIcon = EditorGUIUtility.LoadIcon("vcs_document");
                defaultIcon.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        // Parse all key input supported by the list here
        void HandleKeyInput(Event e)
        {
            // Only handle blip events in here on
            if (e.type != EventType.KeyDown)
                return;

            // Nothing selected?
            if (selectList.Count == 0)
                return;

            //  Arrow key up
            if (e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.DownArrow)
            {
                ListItem sel = null;

                if (e.keyCode == KeyCode.UpArrow)
                {
                    sel = SelectedFirstIn(active);
                    if (sel != null) sel = sel.PrevOpenSkip;
                }
                else
                {
                    sel = SelectedLastIn(active);
                    if (sel != null) sel = sel.NextOpenSkip;
                }

                if (sel != null)
                {
                    // Ensure that the item is in view - linear search but handles all the corner cases
                    if (!ScrollUpTo(sel))
                        ScrollDownTo(sel);

                    if (e.shift)
                    {
                        SelectionFlow(sel);
                    }
                    else
                    {
                        SelectedSet(sel);
                    }
                }
            }

            // Expand/contract on left/right arrow keys
            if (e.keyCode == KeyCode.LeftArrow || e.keyCode == KeyCode.RightArrow)
            {
                ListItem sel = SelectedCurrentIn(active);
                sel.Expanded = (e.keyCode == KeyCode.RightArrow);
                CallExpandedEvent(sel, true);
            }

            // Edit on return key
            if (e.keyCode == KeyCode.Return && GUIUtility.keyboardControl == 0)
            {
                ListItem sel = SelectedCurrentIn(active);
                sel.Asset.Edit();
            }
        }

        // Draw a list item.  A few too many magic numbers in here so will need to tidy this a bit
        void DrawItem(ListItem item, Rect area, float x, float y, bool focus, bool selected)
        {
            bool drag = (item == dragTarget);
            bool highlight = selected;

            if (selected)
            {
                Texture2D selectTex = focus ? blueTex : greyTex;
                GUI.DrawTexture(new Rect(area.x, y, area.width, c_lineHeight), selectTex, ScaleMode.StretchToFill, false);
            }
            else if (drag)
            {
                switch (dragAdjust)
                {
                    case SelectDirection.Up:
                        if (item.PrevOpenVisible != item.Parent)
                        {
                            GUI.DrawTexture(new Rect(x, y - 1, area.width, 2), yellowTex, ScaleMode.StretchToFill, false);
                        }
                        break;
                    case SelectDirection.Down:
                        GUI.DrawTexture(new Rect(x, y + c_lineHeight - 1, area.width, 2), yellowTex, ScaleMode.StretchToFill, false);
                        break;
                    default:
                        if (item.CanAccept)
                        {
                            GUI.DrawTexture(new Rect(area.x, y, area.width, c_lineHeight), yellowTex, ScaleMode.StretchToFill, false);
                            highlight = true;
                        }
                        break;
                }
            }
            else if (dragTarget != null && item == dragTarget.Parent && dragAdjust != SelectDirection.Current)
            {
                GUI.DrawTexture(new Rect(area.x, y, area.width, c_lineHeight), yellowTex, ScaleMode.StretchToFill, false);
                highlight = true;
            }

            if (item.HasActions)
            {
                // Draw any actions available
                for (int i = 0; i < item.Actions.Length; ++i)
                {
                    calcSizeTmpContent.text = item.Actions[i];
                    Vector2 sz = GUI.skin.button.CalcSize(calcSizeTmpContent);
                    if (GUI.Button(new Rect(x, y, sz.x, c_lineHeight), item.Actions[i]))
                    {
                        // Action performed. Callback delegate
                        actionDelegate(item, i);
                    }
                    x += sz.x + 4; // offset by 4 px
                }
            }

            if (item.CanExpand)
            {
                EditorGUI.Foldout(new Rect(x, y, 16, c_lineHeight), item.Expanded, GUIContent.none);
            }

            Texture icon = item.Icon;
            Color tmpColor = GUI.color;
            Color tmpContentColor = GUI.contentColor;

            // We grey the items when we dont know the state or its a dummy item
            if (/*item.Asset.State == Asset.States.Local ||*/ item.Dummy) //< Locals shown with icon for now
                GUI.color = new Color(0.65f, 0.65f, 0.65f);

            // This should not be an else statement as the previous if can set icon
            if (!item.Dummy)
            {
                // If there is no icon set then we look for cached items
                if (icon == null)
                {
                    icon = InternalEditorUtility.GetIconForFile(item.Name);
                    //                  icon = defaultIcon;
                }

                var iconRect = new Rect(x + 14, y, 16, c_lineHeight);

                if (icon != null)
                    GUI.DrawTexture(iconRect, icon);

                if (item.Asset != null)
                {
                    bool drawOverlay = true;
                    string vcsType = EditorSettings.externalVersionControl;
                    if (vcsType == ExternalVersionControl.Disabled ||
                        vcsType == ExternalVersionControl.AutoDetect ||
                        vcsType == ExternalVersionControl.Generic)
                        drawOverlay = false; // no overlays for these version control systems

                    if (drawOverlay)
                    {
                        Rect overlayRect = iconRect;
                        overlayRect.width += 12;
                        overlayRect.x -= 6;
                        Overlay.DrawOverlay(item.Asset, overlayRect);
                    }
                }
            }

            string displayName = DisplayName(item);
            Vector2 displayNameSize = EditorStyles.label.CalcSize(EditorGUIUtility.TempContent(displayName));
            float labelOffsetX = x + 32;

            if (highlight)
            {
                GUI.contentColor = new Color(3, 3, 3);
                GUI.Label(new Rect(labelOffsetX, y, area.width - labelOffsetX, c_lineHeight + 2), displayName);
            }
            else
            {
                GUI.Label(new Rect(labelOffsetX, y, area.width - labelOffsetX, c_lineHeight + 2), displayName);
            }

            if (HasHiddenMetaFile(item))
            {
                GUI.color = new Color(0.55f, 0.55f, 0.55f);
                float spaceBefore = labelOffsetX + displayNameSize.x + 2;
                GUI.Label(new Rect(spaceBefore, y, area.width - spaceBefore, c_lineHeight + 2), "+meta");
            }
            GUI.contentColor = tmpContentColor;
            GUI.color = tmpColor;
        }

        void UpdateVisibleList(Rect area, float scrollPos)
        {
            float y = area.y;
            float h = area.y + area.height - c_lineHeight;
            ListItem first = active.NextOpenVisible; // Skip the root node

            visibleList.Clear();

            // Move to the first visible item
            for (float i = 0; i < scrollPos; ++i)
            {
                if (first == null)
                    return;

                first = first.NextOpenVisible;
            }

            for (ListItem it = first; it != null && y < h; it = it.NextOpenVisible)
            {
                visibleList.Add(it);
                y += c_lineHeight;
            }
        }

        ListItem GetItemAt(Rect area, Vector2 pos)
        {
            int index = (int)((pos.y - area.y) / c_lineHeight);

            if (index >= 0 && index < visibleList.Count)
                return visibleList[index];

            return null;
        }

        // Scroll up towards a target item
        bool ScrollUpTo(ListItem item)
        {
            int count = (int)listState.Scroll;

            ListItem find = visibleList.Count > 0 ? visibleList[0] : null;
            while (find != null && count >= 0)
            {
                if (find == item)
                {
                    listState.Scroll = count;
                    return true;
                }

                --count;
                find = find.PrevOpenVisible;
            }

            return false;
        }

        // Scroll up towards a target item
        bool ScrollDownTo(ListItem item)
        {
            int count = (int)listState.Scroll;

            ListItem find = visibleList.Count > 0 ? visibleList[visibleList.Count - 1] : null;
            while (find != null && count >= 0)
            {
                if (find == item)
                {
                    listState.Scroll = count;
                    return true;
                }

                ++count;
                find = find.NextOpenVisible;
            }

            return false;
        }

        // Load the expanded list state
        void LoadExpanded(ListItem item)
        {
            if (item.Change != null)
                item.Expanded = listState.Expanded.Contains(item.Change.id);

            ListItem en = item.FirstChild;
            while (en != null)
            {
                LoadExpanded(en);
                en = en.Next;
            }
        }

        internal void ExpandLastItem()
        {
            if (root.LastChild != null)
            {
                root.LastChild.Expanded = true;
                CallExpandedEvent(root.LastChild, true);
            }
        }

        // Parameter added for removing entries as this isnt necessary when updating the full list
        void CallExpandedEvent(ListItem item, bool remove)
        {
            if (item.Change != null)
            {
                if (item.Expanded)
                {
                    if (expandDelegate != null)
                        expandDelegate(item.Change, item);

                    listState.Expanded.Add(item.Change.id);
                }
                else if (remove)
                {
                    listState.Expanded.Remove(item.Change.id);
                }
            }

            ListItem en = item.FirstChild;
            while (en != null)
            {
                CallExpandedEvent(en, remove);
                en = en.Next;
            }
        }

        // Find an item in the list given a path
        ListItem PathSearchFind(string path)
        {
            try { return pathSearch[path.ToLower()]; }
            catch { return null; }
        }

        // Recursively update items for path search mapping
        void PathSearchUpdate(ListItem item)
        {
            if (item.Asset != null && item.Asset.path.Length > 0)
                pathSearch.Add(item.Asset.path.ToLower(), item);
            else if (item.Change != null)
            {
                pathSearch.Add(c_changeKeyPrefix + item.Change.id.ToString(), item);
                return;
            }
            ListItem en = item.FirstChild;
            while (en != null)
            {
                PathSearchUpdate(en);
                en = en.Next;
            }
        }

        // Is the item selected?
        bool IsSelected(ListItem item)
        {
            if (item.Asset != null)
                return selectList.ContainsKey(item.Asset.path.ToLower());
            if (item.Change != null)
                return selectList.ContainsKey(c_changeKeyPrefix + item.Change.id.ToString());

            return false;
        }

        // Is an asset selected in the list
        bool IsSelectedAsset()
        {
            foreach (KeyValuePair<string, ListItem> de in selectList)
            {
                if (de.Value != null && de.Value.Asset != null)
                {
                    return true;
                }
            }

            return false;
        }

        // Clear the current selection list
        void SelectedClear()
        {
            selectList.Clear();
            Selection.activeObject = null;
            Selection.instanceIDs = new int[0];
        }

        // Single selection - clears all previous selections
        void SelectedRefresh()
        {
            Dictionary<string, ListItem> newSelect = new Dictionary<string, ListItem>();

            foreach (KeyValuePair<string, ListItem> de in selectList)
            {
                newSelect[de.Key] = PathSearchFind(de.Key);
            }

            selectList = newSelect;
        }

        // Single selection - clears all previous selections
        public void SelectedSet(ListItem item)
        {
            // Dont select or do anything with invalid entries
            if (item.Dummy)
                return;

            SelectedClear();
            if (item.Asset != null)
            {
                SelectedAdd(item);
            }
            else if (item.Change != null)
            {
                selectList[c_changeKeyPrefix + item.Change.id.ToString()] = item;
            }
        }

        public void SelectedAll()
        {
            SelectedClear();
            SelectedAllHelper(Root);
        }

        void SelectedAllHelper(ListItem _root)
        {
            ListItem cur = _root.FirstChild;
            while (cur != null)
            {
                if (cur.HasChildren)
                {
                    // Recurse
                    SelectedAllHelper(cur);
                }

                if (cur.Asset != null)
                {
                    SelectedAdd(cur);
                }
                cur = cur.Next;
            }
        }

        ListItem GetTwinAsset(ListItem item)
        {
            ListItem prev = item.Prev;
            if (item.Name.EndsWith(c_metaSuffix) &&
                prev != null &&
                prev.Asset != null &&
                AssetDatabase.GetTextMetaFilePathFromAssetPath(prev.Asset.path).ToLower() == item.Asset.path.ToLower())
                return prev;
            return null;
        }

        ListItem GetTwinMeta(ListItem item)
        {
            ListItem next = item.Next;
            if (!item.Name.EndsWith(c_metaSuffix) &&
                next != null &&
                next.Asset != null &&
                next.Asset.path.ToLower() == AssetDatabase.GetTextMetaFilePathFromAssetPath(item.Asset.path).ToLower())
                return next;
            return null;
        }

        ListItem GetTwin(ListItem item)
        {
            ListItem a = GetTwinAsset(item);
            if (a != null)
                return a;
            return GetTwinMeta(item);
        }

        // Add a selection to the list
        public void SelectedAdd(ListItem item)
        {
            // Dont select or do anything with invalid entries
            if (item.Dummy)
                return;

            ListItem current = SelectedCurrentIn(active);

            // Handle exclusive selections
            if (item.Exclusive || (current != null && current.Exclusive))
            {
                SelectedSet(item);
                return;
            }

            //
            string name = item.Asset.path.ToLower();
            int selectListPrevCount = selectList.Count;
            selectList[name] = item;

            // Auto select meta files together with asset
            ListItem twin = GetTwin(item);

            if (twin != null)
                selectList[twin.Asset.path.ToLower()] = twin;

            if (selectListPrevCount == selectList.Count)
                return; // The items were already present

            // Update core selection list... Only non-meta files can be selectable
            int[] sel = Selection.instanceIDs;

            int arrayLen = 0;
            if (sel != null) arrayLen = sel.Length;

            // UnityEngine.Object tmpObj = item.Asset.Load ();

            name = name.EndsWith(c_metaSuffix) ? name.Substring(0, name.Length - 5) : name;
            int itemID = AssetDatabase.GetMainAssetInstanceID(name.TrimEnd('/'));
            if (itemID != 0)
            {
                int[] newSel = new int[arrayLen + 1];
                newSel[arrayLen] = itemID;
                Array.Copy(sel, newSel, arrayLen);
                Selection.instanceIDs = newSel;
            }
        }

        void SelectedRemove(ListItem item)
        {
            string name = item.Asset.path.ToLower();
            // Remove item and twin
            selectList.Remove(name);
            selectList.Remove(name.EndsWith(c_metaSuffix) ? name.Substring(0, name.Length - 5) : name + c_metaSuffix);

            // Sync with core selection list.
            name = name.EndsWith(c_metaSuffix) ? name.Substring(0, name.Length - 5) : name;
            int itemID = AssetDatabase.GetMainAssetInstanceID(name.TrimEnd('/'));
            int[] sel = Selection.instanceIDs;
            if (itemID != 0 && sel.Length > 0)
            {
                int idx = Array.IndexOf(sel, itemID);
                if (idx < 0)
                    return;

                int[] newSel = new int[sel.Length - 1];
                Array.Copy(sel, newSel, idx);
                if (idx < (sel.Length - 1))
                    Array.Copy(sel, idx + 1, newSel, idx, sel.Length - idx - 1);

                Selection.instanceIDs = newSel;
            }
        }

        // Toggle the selected state of the item
        void SelectedToggle(ListItem item)
        {
            if (IsSelected(item))
                SelectedRemove(item);
            else
                SelectedAdd(item);
        }

        // Selection flow.  Flows all selection towards the item.  Used for SHIFT selection.
        void SelectionFlow(ListItem item)
        {
            if (selectList.Count == 0)
            {
                SelectedSet(item);
            }
            else
            {
                if (!SelectionFlowDown(item))
                    SelectionFlowUp(item);
            }
        }

        // Select all items up the list
        bool SelectionFlowUp(ListItem item)
        {
            ListItem it;
            ListItem limit = item;

            for (it = item; it != null; it = it.PrevOpenVisible)
            {
                if (IsSelected(it))
                    limit = it;
            }

            if (item == limit)
                return false;

            SelectedClear();
            SelectedAdd(limit);

            for (it = item; it != limit; it = it.PrevOpenVisible)
                SelectedAdd(it);

            return true;
        }

        // Select all items down the list
        bool SelectionFlowDown(ListItem item)
        {
            ListItem it;
            ListItem limit = item;

            for (it = item; it != null; it = it.NextOpenVisible)
            {
                if (IsSelected(it))
                    limit = it;
            }

            if (item == limit)
                return false;

            SelectedClear();
            SelectedAdd(limit);

            for (it = item; it != limit; it = it.NextOpenVisible)
                SelectedAdd(it);

            return true;
        }

        // Utility function to return the 1st selected item in a list item hierarchy
        ListItem SelectedCurrentIn(ListItem root)
        {
            foreach (KeyValuePair<string, ListItem> de in selectList)
            {
                if (de.Value.IsChildOf(root))
                    return de.Value;
            }

            return null;
        }

        ListItem SelectedFirstIn(ListItem root)
        {
            ListItem item = SelectedCurrentIn(root);
            ListItem find = item;

            while (find != null)
            {
                if (IsSelected(find))
                    item = find;

                find = find.PrevOpenVisible;
            }

            return item;
        }

        ListItem SelectedLastIn(ListItem root)
        {
            ListItem item = SelectedCurrentIn(root);
            ListItem find = item;

            while (find != null)
            {
                if (IsSelected(find))
                    item = find;

                find = find.NextOpenVisible;
            }

            return item;
        }

        // Display name rules for items.
        string DisplayName(ListItem item)
        {
            string name = item.Name;

            // Select first nonblank line
            string n = "";
            while (n == "")
            {
                int i = name.IndexOf('\n');
                if (i < 0) break;
                n = name.Substring(0, i).Trim();
                name = name.Substring(i + 1);
            }

            if (n != "")
                name = n;

            name = name.Trim();

            if (name == "")
            {
                if (item.Change != null)
                {
                    name = item.Change.id.ToString() + " " + item.Change.description;
                }
            }

            return name;
        }

        private bool HasHiddenMetaFile(ListItem item)
        {
            ListItem twinMeta = GetTwinMeta(item);
            return twinMeta != null && twinMeta.Hidden;
        }
    }
}
