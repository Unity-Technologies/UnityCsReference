// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEditorInternal.VersionControl;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Math = System.Math;
using IndexOutOfRangeException = System.IndexOutOfRangeException;

namespace UnityEditor
{
    internal partial class ObjectListArea
    {
        // Enable external source(s) to draw in the project browser.
        // 'iconRect' frame for the asset icon.
        // 'guid' asset being drawn.
        internal delegate void OnAssetIconDrawDelegate(Rect iconRect, string guid, bool isListMode);
        internal static event OnAssetIconDrawDelegate postAssetIconDrawCallback = null;

        // 'drawRect' prescribed draw area after the asset label.
        // 'guid' asset being drawn.
        // return whether drawing occured (space will be redistributed if false)
        internal delegate bool OnAssetLabelDrawDelegate(Rect drawRect, string guid, bool isListMode);
        internal static event OnAssetLabelDrawDelegate postAssetLabelDrawCallback = null;

        // Asset on local disk in project
        class LocalGroup : Group
        {
            BuiltinResource[] m_NoneList;

            GUIContent m_Content = new GUIContent();

            List<int> m_DragSelection = new List<int>();                // Temp instanceID state while dragging (not serialized)
            int m_DropTargetControlID = 0;

            // Type name if resource is the key
            Dictionary<string, BuiltinResource[]> m_BuiltinResourceMap;
            BuiltinResource[] m_CurrentBuiltinResources;
            bool m_ShowNoneItem;
            public bool ShowNone { get { return m_ShowNoneItem; } }
            public override bool NeedsRepaint { get { return false; } protected set {} }
            List<int> m_LastRenderedAssetInstanceIDs = new List<int>();
            List<int> m_LastRenderedAssetDirtyIDs = new List<int>();

            public bool m_ListMode = false;
            FilteredHierarchy m_FilteredHierarchy;
            BuiltinResource[] m_ActiveBuiltinList;

            public const int k_ListModeLeftPadding = 13;
            public const int k_ListModeLeftPaddingForSubAssets = 28;
            public const int k_ListModeVersionControlOverlayPadding = 14;
            const int k_ListModeExternalIconPadding = 6;
            const float k_IconWidth = 16f;
            const float k_SpaceBetweenIconAndText = 2f;


            public SearchFilter searchFilter { get {return m_FilteredHierarchy.searchFilter; }}
            public override bool ListMode { get { return m_ListMode; } set { m_ListMode = value; } }
            public bool HasBuiltinResources { get {  return m_CurrentBuiltinResources.Length > 0; } }

            ItemFader m_ItemFader = new ItemFader();

            public override int ItemCount
            {
                get
                {
                    int projectItemCount = m_FilteredHierarchy.results.Length;
                    int totalItemCount = projectItemCount + m_ActiveBuiltinList.Length;
                    int noneItem = m_ShowNoneItem ? 1 : 0;
                    int newItem = m_Owner.m_State.m_NewAssetIndexInList != -1 ? 1 : 0;
                    return totalItemCount + noneItem + newItem;
                }
            }

            public LocalGroup(ObjectListArea owner, string groupTitle, bool showNone) : base(owner, groupTitle)
            {
                m_ShowNoneItem = showNone;
                m_ListMode = false;
                InitBuiltinResources();
                ItemsWantedShown = int.MaxValue;
                m_Collapsable = false;
            }

            public override void UpdateAssets()
            {
                // Set up our builtin list
                if (m_FilteredHierarchy.hierarchyType == HierarchyType.Assets)
                    m_ActiveBuiltinList = m_CurrentBuiltinResources;
                else
                    m_ActiveBuiltinList = new BuiltinResource[0];   // The Scene tab does not display builtin resources

                ItemsAvailable = m_FilteredHierarchy.results.Length + m_ActiveBuiltinList.Length;
            }

            protected override float GetHeaderHeight()
            {
                return 0f;
            }

            override protected void DrawHeader(float yOffset, bool collapsable)
            {
                if (GetHeaderHeight() > 0f)
                {
                    Rect rect = new Rect(0, GetHeaderYPosInScrollArea(yOffset), m_Owner.GetVisibleWidth(), kGroupSeparatorHeight);

                    base.DrawHeaderBackground(rect, true);

                    // Draw the group toggle
                    if (collapsable)
                    {
                        rect.x += 7;
                        bool oldVisible = Visible;
                        Visible = GUI.Toggle(new Rect(rect.x, rect.y, 14, rect.height), Visible, GUIContent.none, s_Styles.groupFoldout);
                        if (oldVisible ^ Visible)
                            EditorPrefs.SetBool(m_GroupSeparatorTitle, Visible);

                        rect.x += 7;
                    }

                    float usedWidth = 0f;
                    if (m_Owner.drawLocalAssetHeader != null)
                        usedWidth = m_Owner.drawLocalAssetHeader(rect) + 10f; // add space between arrow and count

                    rect.x += usedWidth;
                    rect.width -= usedWidth;
                    if (rect.width > 0)
                        base.DrawItemCount(rect);
                }
            }

            public override void UpdateHeight()
            {
                // Ensure that m_Grid is setup before calling UpdateHeight
                m_Height = GetHeaderHeight();

                if (!Visible)
                    return;

                m_Height += m_Grid.height;
            }

            bool IsCreatingAtThisIndex(int itemIdx)
            {
                return m_Owner.m_State.m_NewAssetIndexInList == itemIdx;
            }

            protected override void DrawInternal(int beginIndex, int endIndex, float yOffset)
            {
                int itemIndex = beginIndex;
                int itemCount = 0;

                FilteredHierarchy.FilterResult[] results = m_FilteredHierarchy.results;

                bool isFolderBrowsing = m_FilteredHierarchy.searchFilter.GetState() == SearchFilter.State.FolderBrowsing;

                // The seperator bar is drawn before all items
                yOffset += GetHeaderHeight();

                // 1. None item
                Rect itemRect;
                if (m_NoneList.Length > 0)
                {
                    if (beginIndex < 1)
                    {
                        itemRect = m_Grid.CalcRect(itemIndex, yOffset);
                        DrawItem(itemRect, null, m_NoneList[0], isFolderBrowsing);
                        itemIndex++;
                    }
                    itemCount++;
                }

                // 2. Project Assets
                if (!ListMode && isFolderBrowsing)
                    DrawSubAssetBackground(beginIndex, endIndex, yOffset); // only show sub asset bg when not searching i.e. folder browsing

                if (Event.current.type == EventType.Repaint)
                    ClearDirtyStateTracking();
                int resultsIdx = itemIndex - itemCount;
                while (true)
                {
                    // Insert new asset item here
                    if (IsCreatingAtThisIndex(itemIndex))
                    {
                        BuiltinResource newAsset = new BuiltinResource();
                        newAsset.m_Name = m_Owner.GetCreateAssetUtility().originalName;
                        newAsset.m_InstanceID = m_Owner.GetCreateAssetUtility().instanceID;

                        DrawItem(m_Grid.CalcRect(itemIndex, yOffset), null, newAsset, isFolderBrowsing);
                        itemIndex++; // Push following items forward
                        itemCount++;
                    }

                    // Stop conditions
                    if (itemIndex > endIndex)
                        break;
                    if (resultsIdx >= results.Length)
                        break;

                    // Draw item
                    FilteredHierarchy.FilterResult result = results[resultsIdx];
                    itemRect = m_Grid.CalcRect(itemIndex, yOffset);
                    DrawItem(itemRect, result, null, isFolderBrowsing);
                    itemIndex++;
                    resultsIdx++;
                }
                itemCount += results.Length;

                // 3. Builtin
                if (m_ActiveBuiltinList.Length > 0)
                {
                    int builtinStartIdx = beginIndex - itemCount;
                    builtinStartIdx = Math.Max(builtinStartIdx, 0);
                    for (int builtinIdx = builtinStartIdx; builtinIdx < m_ActiveBuiltinList.Length && itemIndex <= endIndex; itemIndex++, builtinIdx++)
                    {
                        DrawItem(m_Grid.CalcRect(itemIndex, yOffset), null, m_ActiveBuiltinList[builtinIdx], isFolderBrowsing);
                    }
                }

                // Repaint again if we are in preview icon mode and previews are being loaded
                if (!ListMode && AssetPreview.IsLoadingAssetPreviews(m_Owner.GetAssetPreviewManagerID()))
                    m_Owner.Repaint();
            }

            void ClearDirtyStateTracking()
            {
                m_LastRenderedAssetInstanceIDs.Clear();
                m_LastRenderedAssetDirtyIDs.Clear();
            }

            void AddDirtyStateFor(int instanceID)
            {
                m_LastRenderedAssetInstanceIDs.Add(instanceID);
                m_LastRenderedAssetDirtyIDs.Add(EditorUtility.GetDirtyIndex(instanceID));
            }

            public bool IsAnyLastRenderedAssetsDirty()
            {
                for (int i = 0; i < m_LastRenderedAssetInstanceIDs.Count; ++i)
                {
                    int dirtyIndex = EditorUtility.GetDirtyIndex(m_LastRenderedAssetInstanceIDs[i]);
                    if (dirtyIndex != m_LastRenderedAssetDirtyIDs[i])
                    {
                        m_LastRenderedAssetDirtyIDs[i] = dirtyIndex;
                        return true;
                    }
                }
                return false;
            }

            protected override void HandleUnusedDragEvents(float yOffset)
            {
                if (!m_Owner.allowDragging)
                    return;
                Event evt = Event.current;
                switch (evt.type)
                {
                    case EventType.DragUpdated:
                    case EventType.DragPerform:
                        Rect localGroupRect = new Rect(0, yOffset, m_Owner.m_TotalRect.width, m_Owner.m_TotalRect.height > Height ? m_Owner.m_TotalRect.height : Height);
                        if (localGroupRect.Contains(evt.mousePosition))
                        {
                            DragAndDropVisualMode mode;
                            bool isFolderBrowsing = (m_FilteredHierarchy.searchFilter.GetState() == SearchFilter.State.FolderBrowsing);
                            if (isFolderBrowsing && m_FilteredHierarchy.searchFilter.folders.Length == 1)
                            {
                                string folder = m_FilteredHierarchy.searchFilter.folders[0];
                                int instanceID = AssetDatabase.GetMainAssetInstanceID(folder);
                                bool perform = evt.type == EventType.DragPerform;
                                mode = DoDrag(instanceID, perform);
                                if (perform && mode != DragAndDropVisualMode.None)
                                    DragAndDrop.AcceptDrag();
                            }
                            else
                            {
                                // Disallow drop: more than one folder or search is active, since dropping would be ambiguous.
                                mode = DragAndDropVisualMode.None;
                            }
                            DragAndDrop.visualMode = mode;
                            evt.Use();
                        }
                        break;
                }
            }

            void HandleMouseWithDragging(int instanceID, int controlID, Rect rect)
            {
                // Handle mouse down on entire line
                Event evt = Event.current;

                switch (evt.GetTypeForControl(controlID))
                {
                    case EventType.MouseDown:
                        if (Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
                        {
                            if (evt.clickCount == 2)
                            {
                                // Double clicked
                                m_Owner.SetSelection(new[] {instanceID}, true);
                                m_DragSelection.Clear();
                            }
                            else
                            {
                                // Begin drag
                                m_DragSelection = GetNewSelection(instanceID, true, false);
                                GUIUtility.hotControl = controlID;
                                DragAndDropDelay delay = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), controlID);
                                delay.mouseDownPosition = Event.current.mousePosition;
                                m_Owner.ScrollToPosition(ObjectListArea.AdjustRectForFraming(rect));
                            }

                            evt.Use();
                        }
                        else if (Event.current.button == 1 && rect.Contains(Event.current.mousePosition))
                        {
                            // Right mouse down selection (do NOT use event since we need ContextClick event, which is not fired if right click is used)
                            m_Owner.SetSelection(GetNewSelection(instanceID, true, false).ToArray(), false);
                        }
                        break;
                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == controlID)
                        {
                            DragAndDropDelay delay = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), controlID);
                            if (delay.CanStartDrag())
                            {
                                StartDrag(instanceID, m_DragSelection);
                                GUIUtility.hotControl = 0;
                            }

                            evt.Use();
                        }
                        break;
                    case EventType.DragUpdated:
                    case EventType.DragPerform:
                    {
                        bool perform = evt.type == EventType.DragPerform;
                        if (rect.Contains(evt.mousePosition))
                        {
                            DragAndDropVisualMode mode = DoDrag(instanceID, perform);
                            if (mode != DragAndDropVisualMode.None)
                            {
                                if (perform)
                                    DragAndDrop.AcceptDrag();

                                m_DropTargetControlID = controlID;
                                DragAndDrop.visualMode = mode;
                                evt.Use();
                            }

                            if (perform)
                                m_DropTargetControlID = 0;
                        }

                        if (perform)
                            m_DragSelection.Clear();
                    }
                    break;
                    case EventType.DragExited:
                        m_DragSelection.Clear();
                        break;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == controlID)
                        {
                            if (rect.Contains(evt.mousePosition))
                            {
                                bool clickedOnText;
                                if (ListMode)
                                {
                                    rect.x += 28;
                                    rect.width += 28;
                                    clickedOnText = rect.Contains(evt.mousePosition);
                                }
                                else
                                {
                                    rect.y = rect.y + rect.height - s_Styles.resultsGridLabel.fixedHeight;
                                    rect.height = s_Styles.resultsGridLabel.fixedHeight;
                                    clickedOnText = rect.Contains(evt.mousePosition);
                                }

                                List<int> selected = m_Owner.m_State.m_SelectedInstanceIDs;
                                if (clickedOnText && m_Owner.allowRenaming && m_Owner.m_AllowRenameOnMouseUp && selected.Count == 1 && selected[0] == instanceID && !EditorGUIUtility.HasHolddownKeyModifiers(evt))
                                {
                                    m_Owner.BeginRename(0.5f);
                                }
                                else
                                {
                                    List<int> newSelection = GetNewSelection(instanceID, false, false);
                                    m_Owner.SetSelection(newSelection.ToArray(), false);
                                }

                                GUIUtility.hotControl = 0;
                                evt.Use();
                            }

                            m_DragSelection.Clear();
                        }
                        break;

                    case EventType.ContextClick:
                        Rect overlayPos = rect;
                        overlayPos.x += 2;
                        overlayPos = ProjectHooks.GetOverlayRect(overlayPos);
                        if (overlayPos.width != rect.width && Provider.isActive && overlayPos.Contains(evt.mousePosition))
                        {
                            EditorUtility.DisplayPopupMenu(new Rect(evt.mousePosition.x, evt.mousePosition.y, 0, 0), "Assets/Version Control", new MenuCommand(null, 0));
                            evt.Use();
                        }
                        break;
                }
            }

            void HandleMouseWithoutDragging(int instanceID, int controlID, Rect position)
            {
                Event evt = Event.current;

                switch (evt.GetTypeForControl(controlID))
                {
                    case EventType.MouseDown:
                        if (evt.button == 0 && position.Contains(evt.mousePosition))
                        {
                            m_Owner.Repaint();

                            if (evt.clickCount == 1)
                            {
                                m_Owner.ScrollToPosition(ObjectListArea.AdjustRectForFraming(position));
                            }

                            evt.Use();
                            List<int> newSelection = GetNewSelection(instanceID, false, false);
                            m_Owner.SetSelection(newSelection.ToArray(), evt.clickCount == 2);
                        }
                        break;

                    case EventType.ContextClick:
                        if (position.Contains(evt.mousePosition))
                        {
                            // Select it
                            m_Owner.SetSelection(new[] {instanceID}, false);

                            Rect overlayPos = position;
                            overlayPos.x += 2;
                            overlayPos = ProjectHooks.GetOverlayRect(overlayPos);
                            if (overlayPos.width != position.width && Provider.isActive && overlayPos.Contains(evt.mousePosition))
                            {
                                EditorUtility.DisplayPopupMenu(new Rect(evt.mousePosition.x, evt.mousePosition.y, 0, 0), "Assets/Version Control", new MenuCommand(null, 0));
                                evt.Use();
                            }
                        }
                        break;
                }
            }

            public void ChangeExpandedState(int instanceID, bool expanded)
            {
                m_Owner.m_State.m_ExpandedInstanceIDs.Remove(instanceID);
                if (expanded)
                    m_Owner.m_State.m_ExpandedInstanceIDs.Add(instanceID);
                m_FilteredHierarchy.RefreshVisibleItems(m_Owner.m_State.m_ExpandedInstanceIDs);
            }

            bool IsExpanded(int instanceID)
            {
                return (m_Owner.m_State.m_ExpandedInstanceIDs.IndexOf(instanceID) >= 0);
            }

            void SelectAndFrameParentOf(int instanceID)
            {
                int parentInstanceID = 0;
                FilteredHierarchy.FilterResult[] results = m_FilteredHierarchy.results;
                for (int i = 0; i < results.Length; ++i)
                {
                    if (results[i].instanceID == instanceID)
                    {
                        if (results[i].isMainRepresentation)
                            parentInstanceID = 0;
                        break;
                    }

                    if (results[i].isMainRepresentation)
                        parentInstanceID = results[i].instanceID;
                }

                if (parentInstanceID != 0)
                {
                    m_Owner.SetSelection(new int[] {parentInstanceID}, false);
                    m_Owner.Frame(parentInstanceID, true, false);
                }
            }

            bool IsRenaming(int instanceID)
            {
                RenameOverlay renameOverlay = m_Owner.GetRenameOverlay();
                return renameOverlay.IsRenaming() && renameOverlay.userData == instanceID && !renameOverlay.isWaitingForDelay;
            }

            protected void DrawSubAssetRowBg(int startSubAssetIndex, int endSubAssetIndex, bool continued, float yOffset)
            {
                Rect startRect = m_Grid.CalcRect(startSubAssetIndex, yOffset);
                Rect endRect = m_Grid.CalcRect(endSubAssetIndex, yOffset);

                float texWidth = 30f;
                float texHeight = 128f;
                float fraction = startRect.width / texHeight;
                float overflowHeight = 9f * fraction;
                float shrinkHeight = 4f;

                // Start
                bool startIsOnFirstColumn = (startSubAssetIndex % m_Grid.columns) == 0;
                float adjustStart = startIsOnFirstColumn ? 18f * fraction : m_Grid.horizontalSpacing + fraction * 10f;
                Rect rect = new Rect(startRect.x - adjustStart, startRect.y + shrinkHeight, texWidth * fraction, startRect.width - shrinkHeight * 2 + overflowHeight - 1);
                rect.y = Mathf.Round(rect.y);
                rect.height = Mathf.Ceil(rect.height);
                s_Styles.subAssetBg.Draw(rect, GUIContent.none, false, false, false, false);

                // End
                float scaledWidth = texWidth * fraction;
                bool endIsOnLastColumn = (endSubAssetIndex % m_Grid.columns) == (m_Grid.columns - 1);
                float extendEnd = (continued || endIsOnLastColumn) ? 16 * fraction : 8 * fraction;
                Rect rect2 = new Rect(endRect.xMax - scaledWidth + extendEnd, endRect.y + shrinkHeight, scaledWidth, rect.height);
                rect2.y = Mathf.Round(rect2.y);
                rect2.height = Mathf.Ceil(rect2.height);
                GUIStyle endStyle = continued ? s_Styles.subAssetBgOpenEnded : s_Styles.subAssetBgCloseEnded;
                endStyle.Draw(rect2, GUIContent.none, false, false, false, false);

                // Middle
                rect = new Rect(rect.xMax, rect.y, rect2.xMin - rect.xMax, rect.height);
                rect.y = Mathf.Round(rect.y);
                rect.height = Mathf.Ceil(rect.height);
                s_Styles.subAssetBgMiddle.Draw(rect, GUIContent.none, false, false, false, false);
            }

            void DrawSubAssetBackground(int beginIndex, int endIndex, float yOffset)
            {
                if (Event.current.type != EventType.Repaint)
                    return;

                FilteredHierarchy.FilterResult[] results = m_FilteredHierarchy.results;

                int columns = m_Grid.columns;
                int rows = (endIndex - beginIndex) / columns + 1;

                for (int y = 0; y < rows; ++y)
                {
                    int startSubAssetIndex = -1;
                    int endSubAssetIndex = -1;
                    for (int x = 0; x < columns; ++x)
                    {
                        int index = beginIndex + (x + y * columns);
                        if (index >= results.Length)
                            break;

                        FilteredHierarchy.FilterResult result = results[index];
                        if (!result.isMainRepresentation)
                        {
                            if (startSubAssetIndex == -1)
                                startSubAssetIndex = index;
                            endSubAssetIndex = index;
                        }
                        else
                        {
                            // Check if a section was ended
                            if (startSubAssetIndex != -1)
                            {
                                DrawSubAssetRowBg(startSubAssetIndex, endSubAssetIndex, false, yOffset);
                                startSubAssetIndex = -1;
                                endSubAssetIndex = -1;
                            }
                        }
                    }

                    if (startSubAssetIndex != -1)
                    {
                        bool continued = false;
                        if (y < rows - 1)
                        {
                            int indexFirstColumnNextRow = beginIndex + (y + 1) * columns;
                            if (indexFirstColumnNextRow < results.Length)
                                continued = !results[indexFirstColumnNextRow].isMainRepresentation;
                        }

                        DrawSubAssetRowBg(startSubAssetIndex, endSubAssetIndex, continued, yOffset);
                    }
                }
            }

            void DrawItem(Rect position, FilteredHierarchy.FilterResult filterItem, BuiltinResource builtinResource, bool isFolderBrowsing)
            {
                System.Diagnostics.Debug.Assert((filterItem != null && builtinResource == null) ||
                    (builtinResource != null && filterItem == null));          // only one should be valid

                Event evt = Event.current;
                Rect itemRect = position;

                int instanceID = 0;
                bool showFoldout = false;
                if (filterItem != null)
                {
                    instanceID = filterItem.instanceID;
                    showFoldout = filterItem.hasChildren && !filterItem.isFolder && isFolderBrowsing; // we do not want to be able to expand folders
                }
                else if (builtinResource != null)
                {
                    instanceID = builtinResource.m_InstanceID;
                }

                int controlID = GetControlIDFromInstanceID(instanceID);

                bool selected;
                if (m_Owner.allowDragging)
                    selected = m_DragSelection.Count > 0 ? m_DragSelection.Contains(instanceID) : m_Owner.IsSelected(instanceID);
                else
                    selected = m_Owner.IsSelected(instanceID);

                if (selected && instanceID == m_Owner.m_State.m_LastClickedInstanceID)
                    m_LastClickedDrawTime = EditorApplication.timeSinceStartup;

                Rect foldoutRect = new Rect(position.x + s_Styles.groupFoldout.margin.left, position.y, s_Styles.groupFoldout.padding.left, position.height); // ListMode foldout
                if (showFoldout && !ListMode)
                {
                    float fraction = position.height / 128f;
                    float buttonWidth = 28f * fraction;
                    float buttonHeight = 32f * fraction;
                    foldoutRect = new Rect(position.xMax - buttonWidth * 0.5f, position.y + (position.height - s_Styles.resultsGridLabel.fixedHeight) * 0.5f - buttonWidth * 0.5f, buttonWidth, buttonHeight);
                    //foldoutRect = new Rect(position.xMax - 16, position.yMax - 16 - s_Styles.resultsGridLabel.fixedHeight, 16, 16);   // bottom right corner
                }

                bool toggleState = false;
                if (selected && evt.type == EventType.KeyDown && m_Owner.HasFocus()) // We need to ensure we have keyboard focus because rename overlay might have it - and need the key events)
                {
                    switch (evt.keyCode)
                    {
                        // Fold in
                        case KeyCode.LeftArrow:
                            if (ListMode || m_Owner.IsPreviewIconExpansionModifierPressed())
                            {
                                if (IsExpanded(instanceID))
                                    toggleState = true;
                                else
                                    SelectAndFrameParentOf(instanceID);
                                evt.Use();
                            }
                            break;

                        // Fold out
                        case KeyCode.RightArrow:
                            if (ListMode || m_Owner.IsPreviewIconExpansionModifierPressed())
                            {
                                if (!IsExpanded(instanceID))
                                    toggleState = true;
                                evt.Use();
                            }
                            break;
                    }
                }

                // Foldout mouse button logic (rendering the item itself can be found below)
                if (showFoldout && evt.type == EventType.MouseDown && evt.button == 0 && foldoutRect.Contains(evt.mousePosition))
                    toggleState = true;

                if (toggleState)
                {
                    bool expanded = !IsExpanded(instanceID);
                    if (expanded)
                        m_ItemFader.Start(m_FilteredHierarchy.GetSubAssetInstanceIDs(instanceID));
                    ChangeExpandedState(instanceID, expanded);
                    evt.Use();
                    GUIUtility.ExitGUI();
                }

                bool isRenaming = IsRenaming(instanceID);

                Rect labelRect = position;
                if (!ListMode)
                    labelRect = new Rect(position.x, position.yMax + 1 - s_Styles.resultsGridLabel.fixedHeight, position.width - 1, s_Styles.resultsGridLabel.fixedHeight);

                int vcPadding = Provider.isActive && ListMode ? k_ListModeVersionControlOverlayPadding : 0;

                float contentStartX = foldoutRect.xMax;
                if (ListMode)
                {
                    itemRect.x = contentStartX;
                    if (filterItem != null && !filterItem.isMainRepresentation && isFolderBrowsing)
                    {
                        contentStartX = k_ListModeLeftPaddingForSubAssets;
                        itemRect.x = k_ListModeLeftPaddingForSubAssets + vcPadding * 0.5f;
                    }
                    itemRect.width -= itemRect.x;
                }

                // Draw section
                if (Event.current.type == EventType.Repaint)
                {
                    if (m_DropTargetControlID == controlID && !position.Contains(evt.mousePosition))
                        m_DropTargetControlID = 0;
                    bool isDropTarget = controlID == m_DropTargetControlID && m_DragSelection.IndexOf(m_DropTargetControlID) == -1;

                    string labeltext = filterItem != null ? filterItem.name : builtinResource.m_Name;
                    if (ListMode)
                    {
                        if (isRenaming)
                        {
                            selected = false;
                            labeltext = "";
                        }

                        m_Content.text = labeltext;
                        m_Content.image = null;
                        Texture2D icon = filterItem != null ? filterItem.icon : AssetPreview.GetAssetPreview(instanceID, m_Owner.GetAssetPreviewManagerID());

                        if (icon == null && m_Owner.GetCreateAssetUtility().icon != null)
                            icon = m_Owner.GetCreateAssetUtility().icon;

                        if (selected)
                            s_Styles.resultsLabel.Draw(position, GUIContent.none, false, false, selected, m_Owner.HasFocus());

                        if (isDropTarget)
                            s_Styles.resultsLabel.Draw(position, GUIContent.none, true, true, false, false);

                        DrawIconAndLabel(new Rect(contentStartX, position.y, position.width - contentStartX, position.height),
                            filterItem, labeltext, icon, selected, m_Owner.HasFocus());

                        // Foldout
                        if (showFoldout)
                            s_Styles.groupFoldout.Draw(foldoutRect, !ListMode, !ListMode, IsExpanded(instanceID), false);
                    }
                    else // Icon grid
                    {
                        // Get icon
                        bool drawDropShadow = false;
                        if (m_Owner.GetCreateAssetUtility().instanceID == instanceID && m_Owner.GetCreateAssetUtility().icon != null)
                        {
                            // If we are creating a new asset we might have an icon to use
                            m_Content.image = m_Owner.GetCreateAssetUtility().icon;
                        }
                        else
                        {
                            // Check for asset preview
                            m_Content.image = AssetPreview.GetAssetPreview(instanceID, m_Owner.GetAssetPreviewManagerID());
                            if (m_Content.image != null)
                                drawDropShadow = true;

                            if (filterItem != null)
                            {
                                // Otherwise use cached icon
                                if (m_Content.image == null)
                                    m_Content.image = filterItem.icon;

                                // When folder browsing sub assets are shown on a background slate and do not need rounded corner overlay
                                if (isFolderBrowsing && !filterItem.isMainRepresentation)
                                    drawDropShadow = false;
                            }
                        }

                        float padding = (drawDropShadow) ? 2.0f : 0.0f; // the padding compensates for the drop shadow (so it doesn't get too close to the label text)
                        position.height -= s_Styles.resultsGridLabel.fixedHeight + 2 * padding; // get icon rect (remove label height which is included in the position rect)
                        position.y += padding;

                        Rect actualImageDrawPosition = (m_Content.image == null) ? new Rect() : ActualImageDrawPosition(position, m_Content.image.width, m_Content.image.height);
                        m_Content.text = null;
                        float alpha = 1f;
                        if (filterItem != null)
                        {
                            AddDirtyStateFor(filterItem.instanceID);

                            if (!filterItem.isMainRepresentation && isFolderBrowsing)
                            {
                                position.x += 4f;
                                position.y += 4f;
                                position.width -= 8f;
                                position.height -= 8f;

                                actualImageDrawPosition = (m_Content.image == null) ? new Rect() : ActualImageDrawPosition(position, m_Content.image.width, m_Content.image.height);

                                alpha = m_ItemFader.GetAlpha(filterItem.instanceID);
                                if (alpha < 1f)
                                    m_Owner.Repaint();
                            }

                            // Draw static preview bg color as bg for small textures and non-square textures
                            if (drawDropShadow && filterItem.iconDrawStyle == IconDrawStyle.NonTexture)
                                s_Styles.previewBg.Draw(actualImageDrawPosition, GUIContent.none, false, false, false, false);
                        }

                        Color orgColor = GUI.color;
                        if (selected)
                            GUI.color = GUI.color * new Color(0.85f, 0.9f, 1f);

                        if (m_Content.image != null)
                        {
                            Color orgColor2 = GUI.color;
                            if (alpha < 1f)
                                GUI.color =  new Color(GUI.color.r, GUI.color.g, GUI.color.b, alpha);

                            s_Styles.resultsGrid.Draw(actualImageDrawPosition, m_Content, false, false, selected, m_Owner.HasFocus());

                            if (alpha < 1f)
                                GUI.color = orgColor2;
                        }

                        if (selected)
                            GUI.color = orgColor;

                        if (drawDropShadow)
                        {
                            Rect borderPosition = new RectOffset(1, 1, 1, 1).Remove(s_Styles.textureIconDropShadow.border.Add(actualImageDrawPosition));
                            s_Styles.textureIconDropShadow.Draw(borderPosition, GUIContent.none, false, false, selected || isDropTarget, m_Owner.HasFocus() || isRenaming || isDropTarget);
                        }


                        // Draw label
                        if (!isRenaming)
                        {
                            if (isDropTarget)
                                s_Styles.resultsLabel.Draw(new Rect(labelRect.x - 10, labelRect.y, labelRect.width + 20, labelRect.height), GUIContent.none, true, true, false, false);

                            labeltext = m_Owner.GetCroppedLabelText(instanceID, labeltext, position.width);
                            s_Styles.resultsGridLabel.Draw(labelRect, labeltext, false, false, selected, m_Owner.HasFocus());
                        }

                        if (showFoldout)
                        {
                            s_Styles.subAssetExpandButton.Draw(foldoutRect, !ListMode, !ListMode, IsExpanded(instanceID), false);
                        }

                        if (filterItem != null && filterItem.isMainRepresentation)
                        {
                            if (null != postAssetIconDrawCallback)
                            {
                                postAssetIconDrawCallback(position, filterItem.guid, false);
                            }

                            ProjectHooks.OnProjectWindowItem(filterItem.guid, position);
                        }
                    }
                }
                // Adjust edit field if needed
                if (isRenaming)
                {
                    if (ListMode)
                    {
                        float iconOffset = vcPadding + k_IconWidth + k_SpaceBetweenIconAndText + s_Styles.resultsLabel.margin.left;
                        labelRect.x = itemRect.x + iconOffset;
                        labelRect.width -= labelRect.x;
                    }
                    else
                    {
                        labelRect.x -= 4;
                        labelRect.width += 8f;
                    }
                    m_Owner.GetRenameOverlay().editFieldRect = labelRect;
                    m_Owner.HandleRenameOverlay();
                }

                // User hook for rendering stuff on top of items (notice it being called after rendering but before mouse handling to make user able to react on mouse events)
                if (EditorApplication.projectWindowItemOnGUI != null && filterItem != null && m_Owner.allowUserRenderingHook)
                    EditorApplication.projectWindowItemOnGUI(filterItem.guid, itemRect);

                // Mouse handling (must be after rename overlay to ensure overlay get mouseevents)
                if (m_Owner.allowDragging)
                    HandleMouseWithDragging(instanceID, controlID, position);
                else
                    HandleMouseWithoutDragging(instanceID, controlID, position);
            }

            private static Rect ActualImageDrawPosition(Rect position, float imageWidth, float imageHeight)
            {
                if (imageWidth > position.width || imageHeight > position.height)
                {
                    Rect screenRect = new Rect();
                    Rect sourceRect = new Rect();
                    float imageAspect = imageWidth / imageHeight;
                    GUI.CalculateScaledTextureRects(position, ScaleMode.ScaleToFit, imageAspect, ref screenRect, ref sourceRect);
                    return screenRect;
                }
                else
                {
                    float x = position.x + Mathf.Round((position.width - imageWidth) / 2.0f);
                    float y = position.y + Mathf.Round((position.height - imageHeight) / 2.0f);
                    return new Rect(x, y, imageWidth, imageHeight);
                }
            }

            public List<KeyValuePair<string, int>> GetVisibleNameAndInstanceIDs()
            {
                List<KeyValuePair<string, int>> result = new List<KeyValuePair<string, int>>();

                // 1. None item
                if (m_NoneList.Length > 0)
                    result.Add(new KeyValuePair<string, int>(m_NoneList[0].m_Name, m_NoneList[0].m_InstanceID)); // 0

                // 2. Project Assets
                foreach (FilteredHierarchy.FilterResult r in m_FilteredHierarchy.results)
                    result.Add(new KeyValuePair<string, int>(r.name, r.instanceID));

                // 3. Builtin
                for (int i = 0; i < m_ActiveBuiltinList.Length; ++i)
                    result.Add(new KeyValuePair<string, int>(m_ActiveBuiltinList[i].m_Name, m_ActiveBuiltinList[i].m_InstanceID));

                return result;
            }

            private void BeginPing(int instanceID)
            {
            }

            public List<int> GetInstanceIDs()
            {
                List<int> result = new List<int>();

                // 1. None item
                if (m_NoneList.Length > 0)
                    result.Add(m_NoneList[0].m_InstanceID); // 0

                // 2. Project Assets
                foreach (FilteredHierarchy.FilterResult r in m_FilteredHierarchy.results)
                    result.Add(r.instanceID);
                if (m_Owner.m_State.m_NewAssetIndexInList >= 0)
                    result.Add(m_Owner.GetCreateAssetUtility().instanceID);

                // 3. Builtin
                for (int i = 0; i < m_ActiveBuiltinList.Length; ++i)
                    result.Add(m_ActiveBuiltinList[i].m_InstanceID);

                return result;
            }

            // Returns list of selected instanceIDs
            public List<int> GetNewSelection(int clickedInstanceID, bool beginOfDrag, bool useShiftAsActionKey)
            {
                // Flatten grid
                List<int> allInstanceIDs = GetInstanceIDs();
                List<int> selectedInstanceIDs = m_Owner.m_State.m_SelectedInstanceIDs;
                int lastClickedInstanceID = m_Owner.m_State.m_LastClickedInstanceID;
                bool allowMultiselection = m_Owner.allowMultiSelect;

                return InternalEditorUtility.GetNewSelection(clickedInstanceID, allInstanceIDs, selectedInstanceIDs, lastClickedInstanceID, beginOfDrag, useShiftAsActionKey, allowMultiselection);
            }

            public override void UpdateFilter(HierarchyType hierarchyType, SearchFilter searchFilter, bool foldersFirst)
            {
                // Filtered hierarchy list
                RefreshHierarchy(hierarchyType, searchFilter, foldersFirst);

                // Filtered builtin list
                RefreshBuiltinResourceList(searchFilter);
            }

            private void RefreshHierarchy(HierarchyType hierarchyType, SearchFilter searchFilter, bool foldersFirst)
            {
                m_FilteredHierarchy = new FilteredHierarchy(hierarchyType);
                m_FilteredHierarchy.foldersFirst = foldersFirst;
                m_FilteredHierarchy.searchFilter = searchFilter;
                m_FilteredHierarchy.RefreshVisibleItems(m_Owner.m_State.m_ExpandedInstanceIDs);
            }

            void RefreshBuiltinResourceList(SearchFilter searchFilter)
            {
                // Early out if we do not want to show builtin resources
                if (!m_Owner.allowBuiltinResources || (searchFilter.GetState() == SearchFilter.State.FolderBrowsing) || (searchFilter.GetState() == SearchFilter.State.EmptySearchFilter))
                {
                    m_CurrentBuiltinResources = new BuiltinResource[0];
                    return;
                }

                List<BuiltinResource> currentBuiltinResources = new List<BuiltinResource>();

                // Filter by assets labels (Builtins have no asset labels currently)
                if (searchFilter.assetLabels != null && searchFilter.assetLabels.Length > 0)
                {
                    m_CurrentBuiltinResources = currentBuiltinResources.ToArray();
                    return;
                }

                // Filter by class/type
                List<int> requiredClassIDs = new List<int>();
                foreach (string className in searchFilter.classNames)
                {
                    var unityType = UnityType.FindTypeByNameCaseInsensitive(className);
                    if (unityType != null)
                        requiredClassIDs.Add(unityType.persistentTypeID);
                }
                if (requiredClassIDs.Count > 0)
                {
                    foreach (KeyValuePair<string, BuiltinResource[]> kvp in m_BuiltinResourceMap)
                    {
                        UnityType classType = UnityType.FindTypeByName(kvp.Key);
                        if (classType == null)
                            continue;

                        foreach (int requiredClassID in requiredClassIDs)
                        {
                            if (classType.IsDerivedFrom(UnityType.FindTypeByPersistentTypeID(requiredClassID)))
                                currentBuiltinResources.AddRange(kvp.Value);
                        }
                    }
                }

                // Filter by name
                BuiltinResource[] builtinList = currentBuiltinResources.ToArray();
                if (builtinList.Length > 0 && !string.IsNullOrEmpty(searchFilter.nameFilter))
                {
                    List<BuiltinResource> filtered = new List<BuiltinResource>(); // allocated here to prevent from allocating on every event.
                    string nameFilter = searchFilter.nameFilter.ToLower();
                    foreach (BuiltinResource br in builtinList)
                        if (br.m_Name.ToLower().Contains(nameFilter))
                            filtered.Add(br);

                    builtinList = filtered.ToArray();
                }

                m_CurrentBuiltinResources = builtinList;
            }

            public string GetNameOfLocalAsset(int instanceID)
            {
                foreach (var r in m_FilteredHierarchy.results)
                {
                    if (r.instanceID == instanceID)
                        return r.name;
                }
                return null;
            }

            public bool IsBuiltinAsset(int instanceID)
            {
                foreach (KeyValuePair<string, BuiltinResource[]> kvp in m_BuiltinResourceMap)
                {
                    BuiltinResource[] list = kvp.Value;
                    for (int i = 0; i < list.Length; ++i)
                        if (list[i].m_InstanceID == instanceID)
                            return true;
                }
                return false;
            }

            private void InitBuiltinAssetType(System.Type type)
            {
                if (type == null)
                {
                    Debug.LogWarning("ObjectSelector::InitBuiltinAssetType: type is null!");
                    return;
                }
                string typeName = type.ToString().Substring(type.Namespace.ToString().Length + 1);

                var unityType = UnityType.FindTypeByName(typeName);
                if (unityType == null)
                {
                    Debug.LogWarning("ObjectSelector::InitBuiltinAssetType: class '" + typeName + "' not found");
                    return;
                }

                BuiltinResource[] resourceList = EditorGUIUtility.GetBuiltinResourceList(unityType.persistentTypeID);
                if (resourceList != null)
                    m_BuiltinResourceMap.Add(typeName, resourceList);
            }

            public void InitBuiltinResources()
            {
                if (m_BuiltinResourceMap != null)
                    return;

                m_BuiltinResourceMap = new Dictionary<string, BuiltinResource[]>();

                if (m_ShowNoneItem)
                {
                    m_NoneList = new BuiltinResource[1];
                    m_NoneList[0] = new BuiltinResource();
                    m_NoneList[0].m_InstanceID = 0;
                    m_NoneList[0].m_Name = "None";
                }
                else
                {
                    m_NoneList = new BuiltinResource[0];
                }

                // We don't show all built-in resources; just the ones where their type
                // makes sense. The actual lists are in ResourceManager.cpp,
                // GetBuiltinResourcesOfClass
                InitBuiltinAssetType(typeof(Mesh));
                InitBuiltinAssetType(typeof(Material));
                InitBuiltinAssetType(typeof(Texture2D));
                InitBuiltinAssetType(typeof(Font));
                InitBuiltinAssetType(typeof(Shader));
                InitBuiltinAssetType(typeof(Sprite));
                InitBuiltinAssetType(typeof(LightmapParameters));

                // PrintBuiltinResourcesAvailable();
            }

            public void PrintBuiltinResourcesAvailable()
            {
                string text = "";
                text += "ObjectSelector -Builtin Assets Available:\n";
                foreach (KeyValuePair<string, BuiltinResource[]> kvp in m_BuiltinResourceMap)
                {
                    BuiltinResource[] list = kvp.Value;
                    text += "    " + kvp.Key + ": ";
                    for (int i = 0; i < list.Length; ++i)
                    {
                        if (i != 0)
                            text += ", ";
                        text += list[i].m_Name;
                    }
                    text += "\n";
                }
                Debug.Log(text);
            }

            // Can return an index 1 past end of existing items (if newText is last in sort)
            public int IndexOfNewText(string newText, bool isCreatingNewFolder, bool foldersFirst)
            {
                int idx = 0;
                if (m_ShowNoneItem)
                    idx++;

                for (; idx < m_FilteredHierarchy.results.Length; ++idx)
                {
                    FilteredHierarchy.FilterResult r = m_FilteredHierarchy.results[idx];

                    // Skip folders when inserting a normal asset if folders is sorted first
                    if (foldersFirst && r.isFolder && !isCreatingNewFolder)
                        continue;

                    // When inserting a folder in folders first list break when we reach normal assets
                    if (foldersFirst && !r.isFolder && isCreatingNewFolder)
                        break;

                    // Use same name compare as when we sort in the backend: See AssetDatabase.cpp: SortChildren
                    string propertyPath = AssetDatabase.GetAssetPath(r.instanceID);
                    if (EditorUtility.NaturalCompare(System.IO.Path.GetFileNameWithoutExtension(propertyPath), newText) > 0)
                    {
                        return idx;
                    }
                }
                return idx;
            }

            public int IndexOf(int instanceID)
            {
                int idx = 0;

                // 1. 'none' first (has instanceID 0)
                if (m_ShowNoneItem)
                {
                    if (instanceID == 0)
                        return 0;
                    else
                        idx++;
                }
                else if (instanceID == 0)
                    return -1;

                // 2. Project assets
                foreach (FilteredHierarchy.FilterResult r in m_FilteredHierarchy.results)
                {
                    // When creating new asset we jump over that item (assuming we do not search for that new asset)
                    if (m_Owner.m_State.m_NewAssetIndexInList == idx)
                        idx++;

                    if (r.instanceID == instanceID)
                        return idx;
                    idx++;
                }

                // 3. Builtin resources
                foreach (BuiltinResource b in m_ActiveBuiltinList)
                {
                    if (instanceID == b.m_InstanceID)
                        return idx;
                    idx++;
                }
                return -1;
            }

            public FilteredHierarchy.FilterResult LookupByInstanceID(int instanceID)
            {
                if (instanceID == 0)
                    return null;

                int idx = 0;
                foreach (FilteredHierarchy.FilterResult r in m_FilteredHierarchy.results)
                {
                    // When creating new asset we jump over that item (assuming we do not search for that new asset)
                    if (m_Owner.m_State.m_NewAssetIndexInList == idx)
                        idx++;

                    if (r.instanceID == instanceID)
                        return r;
                    idx++;
                }
                return null;
            }

            // Returns true if index was valid. Note that instance can be 0 if 'None' item was found at index
            public bool InstanceIdAtIndex(int index, out int instanceID)
            {
                instanceID = 0;
                if (index >= m_Grid.rows * m_Grid.columns)
                    return false;

                int idx = 0;

                // 1. 'none' first (has instanceID 0)
                if (m_ShowNoneItem)
                {
                    if (index == 0)
                        return true;
                    else
                        idx++;
                }

                // 2. Project assets
                foreach (FilteredHierarchy.FilterResult r in m_FilteredHierarchy.results)
                {
                    instanceID = r.instanceID;
                    if (idx == index)
                        return true;
                    idx++;
                }

                // 3. Builtin resources
                foreach (BuiltinResource b in m_ActiveBuiltinList)
                {
                    instanceID = b.m_InstanceID;
                    if (idx == index)
                        return true;
                    idx++;
                }

                // If last row and the row is not entirely filled
                // we just use the last item on that row
                if (index < m_Grid.rows * m_Grid.columns)
                    return true;

                return false;
            }

            public virtual void StartDrag(int draggedInstanceID, List<int> selectedInstanceIDs)
            {
                ProjectWindowUtil.StartDrag(draggedInstanceID, selectedInstanceIDs);
            }

            public DragAndDropVisualMode DoDrag(int dragToInstanceID, bool perform)
            {
                // Need to get a real HierarchyProperty class, which can be casted in C++.
                HierarchyProperty search = new HierarchyProperty(HierarchyType.Assets);
                if (!search.Find(dragToInstanceID, null))
                    search = null;

                return InternalEditorUtility.ProjectWindowDrag(search, perform);
            }

            static internal int GetControlIDFromInstanceID(int instanceID)
            {
                return instanceID + 100000000;
            }

            public bool DoCharacterOffsetSelection()
            {
                if (Event.current.type == EventType.KeyDown && Event.current.shift)
                {
                    System.StringComparison ignoreCase = System.StringComparison.CurrentCultureIgnoreCase;
                    string startName = "";
                    if (Selection.activeObject != null)
                        startName = Selection.activeObject.name;

                    string c = new string(new[] {Event.current.character});
                    List<KeyValuePair<string, int>> list = GetVisibleNameAndInstanceIDs();
                    if (list.Count == 0)
                        return false;


                    // If same c is same start char as current selected then find current selected index
                    int startIndex = 0;
                    if (startName.StartsWith(c, ignoreCase))
                    {
                        // Iterate from there until next char is found
                        for (int i = 0; i < list.Count; ++i)
                        {
                            if (list[i].Key == startName)
                            {
                                startIndex = i + 1;
                            }
                        }
                    }

                    // Check all items starting with startIndex
                    for (int i = 0; i < list.Count; i++)
                    {
                        int index = (i + startIndex) % list.Count;

                        if (list[index].Key.StartsWith(c, ignoreCase))
                        {
                            Selection.activeInstanceID = list[index].Value;
                            m_Owner.Repaint();
                            return true;
                        }
                    }
                }

                return false;
            }

            public void ShowObjectsInList(int[] instanceIDs)
            {
                m_FilteredHierarchy = new FilteredHierarchy(HierarchyType.Assets);
                m_FilteredHierarchy.SetResults(instanceIDs);
            }

            public static void DrawIconAndLabel(Rect rect, FilteredHierarchy.FilterResult filterItem, string label, Texture2D icon, bool selected, bool focus)
            {
                float vcPadding = s_VCEnabled ? k_ListModeVersionControlOverlayPadding : 0f;
                rect.xMin += s_Styles.resultsLabel.margin.left;

                // Reduce the label width to allow delegate drawing on the right.
                float delegateDrawWidth = (k_ListModeExternalIconPadding * 2) + k_IconWidth;
                Rect delegateDrawRect = new Rect(rect.xMax - delegateDrawWidth, rect.y, delegateDrawWidth, rect.height);
                Rect labelRect = new Rect(rect);
                if (DrawExternalPostLabelInList(delegateDrawRect, filterItem))
                {
                    labelRect.width = (rect.width - delegateDrawWidth);
                }

                s_Styles.resultsLabel.padding.left = (int)(vcPadding + k_IconWidth + k_SpaceBetweenIconAndText);
                s_Styles.resultsLabel.Draw(labelRect, label, false, false, selected, focus);

                Rect iconRect = rect;
                iconRect.width = k_IconWidth;
                iconRect.x += vcPadding * 0.5f;

                if (icon != null)
                    GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

                if (filterItem != null && filterItem.guid != null && filterItem.isMainRepresentation)
                {
                    Rect overlayRect = rect;
                    overlayRect.width = vcPadding + k_IconWidth;

                    if (null != postAssetIconDrawCallback)
                    {
                        postAssetIconDrawCallback(overlayRect, filterItem.guid, true);
                    }
                    ProjectHooks.OnProjectWindowItem(filterItem.guid, overlayRect);
                }
            }

            private static bool DrawExternalPostLabelInList(Rect drawRect, FilteredHierarchy.FilterResult filterItem)
            {
                bool didDraw = false;
                if (filterItem != null && filterItem.guid != null && filterItem.isMainRepresentation)
                {
                    if (null != postAssetLabelDrawCallback)
                    {
                        didDraw = postAssetLabelDrawCallback(drawRect, filterItem.guid, true);
                    }
                }
                return didDraw;
            }

            class ItemFader
            {
                double m_FadeDuration = 0.3;
                double m_FirstToLastDuration = 0.3;
                double m_FadeStartTime;
                double m_TimeBetweenEachItem;
                List<int> m_InstanceIDs;

                public void Start(List<int> instanceIDs)
                {
                    m_InstanceIDs = instanceIDs;
                    m_FadeStartTime = EditorApplication.timeSinceStartup;
                    m_FirstToLastDuration = Math.Min(0.5, instanceIDs.Count * 0.03);
                    m_TimeBetweenEachItem = 0;
                    if (m_InstanceIDs.Count > 1)
                        m_TimeBetweenEachItem = m_FirstToLastDuration / (m_InstanceIDs.Count - 1);
                }

                public float GetAlpha(int instanceID)
                {
                    if (m_InstanceIDs == null)
                        return 1f;

                    if (EditorApplication.timeSinceStartup > m_FadeStartTime + m_FadeDuration + m_FirstToLastDuration)
                    {
                        m_InstanceIDs = null; // reset
                        return 1f;
                    }

                    int index = m_InstanceIDs.IndexOf(instanceID);
                    if (index >= 0)
                    {
                        double elapsed = EditorApplication.timeSinceStartup - m_FadeStartTime;
                        double itemStartTime = m_TimeBetweenEachItem * index;

                        float alpha = 0f;
                        if (itemStartTime < elapsed)
                        {
                            alpha = Mathf.Clamp((float)((elapsed - itemStartTime) / m_FadeDuration), 0f, 1f);
                        }
                        return alpha;
                    }
                    return 1f;
                }
            }
        }
    }
}  // namespace UnityEditor
