// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEditorInternal.VersionControl;
using System.Collections.Generic;
using Math = System.Math;

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
        protected class LocalGroup : Group
        {
            public class ExtraItem : BuiltinResource
            {
                public Texture2D m_Icon = null;
            }
            ExtraItem[] m_NoneList;
            public ExtraItem[] NoneList => m_NoneList;

            GUIContent m_Content = new GUIContent();

            List<EntityId> m_DragSelection = new List<EntityId>();                // Temp instanceID state while dragging (not serialized)
            int m_DropTargetControlID = 0;

            private List<Type> m_AssetPreviewIgnoreList = new List<Type>();
            private List<string> m_AssetExtensionsPreviewIgnoreList = new List<string>();

            // Type name if resource is the key
            Dictionary<string, BuiltinResource[]> m_BuiltinResourceMap;
            BuiltinResource[] m_CurrentBuiltinResources;
            bool m_ShowNoneItem;
            public bool ShowNone { get { return m_ShowNoneItem; } }
            public override bool NeedsRepaint { get { return false; } protected set {} }
            List<EntityId> m_LastRenderedAssetInstanceIDs = new List<EntityId>();
            List<int> m_LastRenderedAssetDirtyCounts = new List<int>();

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
            private readonly Action m_OwnerRepaintAction;

            public int projectItemCount { get { return m_FilteredHierarchy.results.Length; } }

            public override int ItemCount
            {
                get
                {
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
                m_OwnerRepaintAction = () => m_Owner.Repaint();
                InitAssetPreviewIgnoreList();
            }

            //Initialize the list with known asset types that has no Preview Image and should use icons instead like Text, Folder, Scene etc.
            //This will prevent the AssetPreview generation for asset types that has no asset previews.
            //This has been added to handle projects with a large number of assets that has no preview,
            //as preview generation for all those assets is consuming significant amount of CPU,
            //it feels like Unity is frozen for repaint event in ProjectBrowse/ObjectSelector.
            private void InitAssetPreviewIgnoreList()
            {
                m_AssetPreviewIgnoreList.Add(typeof(DefaultAsset));                                                 //DLLs, corrupted files, pdf, Folder etc.
                m_AssetPreviewIgnoreList.Add(typeof(MonoScript));                                                   //Monobehaviour scripts
                m_AssetPreviewIgnoreList.Add(typeof(SceneAsset));
                m_AssetPreviewIgnoreList.Add(typeof(AnimationClip));
                m_AssetPreviewIgnoreList.Add(typeof(Animations.AnimatorController));
                m_AssetPreviewIgnoreList.Add(typeof(TextAsset));
                m_AssetPreviewIgnoreList.Add(typeof(Shader));
                m_AssetPreviewIgnoreList.Add(typeof(LightingSettings));
                m_AssetPreviewIgnoreList.Add(typeof(LightmapParameters));

                m_AssetExtensionsPreviewIgnoreList.Add(".index");
                m_AssetExtensionsPreviewIgnoreList.Add(".vfx");
            }

            //Use this to add the specific types that needs to ignored for AssetPreview image generation.
            //External packages can use these to add support for their specific asset types.
            public void AddTypetoAssetPreviewIgnoreList(Type assetType)
            {
                if (m_AssetPreviewIgnoreList.Contains(assetType))
                    return;

                m_AssetPreviewIgnoreList.Add(assetType);
            }

            public override void UpdateAssets()
            {
                // Set up our builtin list
                if (m_FilteredHierarchy?.hierarchyType == HierarchyType.Assets)
                    m_ActiveBuiltinList = m_CurrentBuiltinResources;
                else
                    m_ActiveBuiltinList = Array.Empty<BuiltinResource>();   // The Scene tab does not display builtin resources

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

                    base.DrawHeaderBackground(rect, true, Visible);

                    // Draw the group toggle
                    if (collapsable)
                    {
                        rect.x += 7;
                        bool oldVisible = Visible;
                        Visible = GUI.Toggle(new Rect(rect.x, rect.y, 14, rect.height), Visible, GUIContent.none, Styles.groupFoldout);
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
                        newAsset.m_EntityId = m_Owner.GetCreateAssetUtility().entityId;

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
                m_LastRenderedAssetDirtyCounts.Clear();
            }

            void AddDirtyStateFor(EntityId entityId)
            {
                m_LastRenderedAssetInstanceIDs.Add(entityId);
                m_LastRenderedAssetDirtyCounts.Add(EditorUtility.GetDirtyCount(entityId));
            }

            public bool IsAnyLastRenderedAssetsDirty()
            {
                for (int i = 0; i < m_LastRenderedAssetInstanceIDs.Count; ++i)
                {
                    int dirtyCount = EditorUtility.GetDirtyCount(m_LastRenderedAssetInstanceIDs[i]);
                    if (dirtyCount != m_LastRenderedAssetDirtyCounts[i])
                    {
                        m_LastRenderedAssetDirtyCounts[i] = dirtyCount;
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
                                EntityId entityId = AssetDatabase.GetMainAssetEntityId(folder);
                                bool perform = evt.type == EventType.DragPerform;
                                mode = DoDrag(entityId, perform);
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
                    case EventType.DragExited:
                        m_DragSelection.Clear();
                        break;
                }
            }

            void HandleMouseWithDragging(EntityId assetEntityId, int controlID, Rect rect)
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
                                var newSelection = GetNewSelection(assetEntityId, false, false);
                                m_Owner.SetSelection(newSelection.ToArray(), true);
                                m_DragSelection.Clear();
                            }
                            else
                            {
                                // Begin drag
                                var newSelection = GetNewSelection(assetEntityId, false, false);
                                var oldItemControlID = controlID;
                                controlID = GetControlIDFromEntityId(assetEntityId);
                                if (controlID == oldItemControlID)
                                {
                                    newSelection = GetNewSelection(assetEntityId, true, false);
                                    m_DragSelection = newSelection;
                                    DragAndDropDelay delay = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), controlID);
                                    delay.mouseDownPosition = Event.current.mousePosition;
                                    m_Owner.ScrollToPosition(ObjectListArea.AdjustRectForFraming(rect));
                                }
                                else
                                {
                                    m_Owner.SetSelection(newSelection.ToArray(), false);
                                    m_DragSelection.Clear();
                                }
                                GUIUtility.hotControl = controlID;
                            }

                            evt.Use();
                        }
                        else if (Event.current.button == 1 && rect.Contains(Event.current.mousePosition))
                        {
                            if (assetEntityId == EntityId.None)
                            {
                                // For non selectable assets, don't show context menu. Selection is deselected
                                m_Owner.SetSelection(Array.Empty<EntityId>(), false);
                                Event.current.Use();
                            }
                            else
                            {
                                // Right mouse down selection (do NOT use event since we need ContextClick event, which is not fired if right click is used)
                                m_Owner.SetSelection(GetNewSelection(assetEntityId, true, false).ToArray(), false);
                            }
                        }
                        break;
                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == controlID)
                        {
                            DragAndDropDelay delay = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), controlID);
                            if (delay.CanStartDrag())
                            {
                                StartDrag(assetEntityId, m_DragSelection);
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
                            DragAndDropVisualMode mode = DoDrag(assetEntityId, perform);

                            if (mode == DragAndDropVisualMode.Rejected && perform)
                                evt.Use();
                            else if (mode != DragAndDropVisualMode.None)
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
                                    rect.y = rect.y + rect.height - Styles.resultsGridLabel.fixedHeight;
                                    rect.height = Styles.resultsGridLabel.fixedHeight;
                                    clickedOnText = rect.Contains(evt.mousePosition);
                                }

                                var selected = m_Owner.m_State.m_SelectedInstanceIDs;
                                if (clickedOnText && m_Owner.allowRenaming && m_Owner.m_AllowRenameOnMouseUp && selected.Count == 1 && selected[0] == assetEntityId && !EditorGUIUtility.HasHolddownKeyModifiers(evt))
                                {
                                    m_Owner.BeginRename(0.5f);
                                }
                                else
                                {
                                    var newSelection = GetNewSelection(assetEntityId, false, false);
                                    m_Owner.SetSelection(newSelection.ToArray(), false);
                                }

                                GUIUtility.hotControl = 0;
                                evt.Use();
                            }

                            m_DragSelection.Clear();
                        }
                        break;

                    case EventType.ContextClick:
                        HandleContextClick(evt, rect);
                        break;
                }
            }

            void HandleMouseWithoutDragging(EntityId assetEntityId, int controlID, Rect position)
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
                            var newSelection = GetNewSelection(assetEntityId, false, false);
                            m_Owner.SetSelection(newSelection.ToArray(), evt.clickCount == 2);
                        }
                        break;

                    case EventType.ContextClick:
                        if (position.Contains(evt.mousePosition))
                        {
                            // Select it
                            var newSelection = GetNewSelection(assetEntityId, false, false);
                            m_Owner.SetSelection(newSelection.ToArray(), false);

                            HandleContextClick(evt, position);
                        }
                        break;
                }
            }

            static void HandleContextClick(Event evt, Rect rect)
            {
                var overlayRect = rect;
                overlayRect.x += 2;
                overlayRect = ProjectHooks.GetOverlayRect(overlayRect);

                var vco = VersionControlManager.activeVersionControlObject;
                if (vco != null && !vco.isConnected)
                    vco = null;
                var vcConnected = vco != null || Provider.isActive;
                if (vcConnected && overlayRect.width != rect.width && overlayRect.Contains(evt.mousePosition))
                {
                    if (vco != null)
                        vco.GetExtension<IPopupMenuExtension>()?.DisplayPopupMenu(new Rect(evt.mousePosition.x, evt.mousePosition.y, 0, 0));
                    else
                        EditorUtility.DisplayPopupMenu(new Rect(evt.mousePosition.x, evt.mousePosition.y, 0, 0), "Assets/Version Control", new MenuCommand(null, 0));
                    evt.Use();
                }
            }

            public void ChangeExpandedState(EntityId entityId, bool expanded)
            {
                m_Owner.m_State.m_ExpandedInstanceIDs.Remove(entityId);
                if (expanded)
                    m_Owner.m_State.m_ExpandedInstanceIDs.Add(entityId);
                m_FilteredHierarchy.RefreshVisibleItems(m_Owner.m_State.m_ExpandedInstanceIDs);
            }

            bool IsExpanded(EntityId entityId)
            {
                return (m_Owner.m_State.m_ExpandedInstanceIDs.IndexOf(entityId) >= 0);
            }

            void SelectAndFrameParentOf(EntityId entityId)
            {
                EntityId parentEntityId = EntityId.None;
                FilteredHierarchy.FilterResult[] results = m_FilteredHierarchy.results;
                for (int i = 0; i < results.Length; ++i)
                {
                    if (results[i].entityId == entityId)
                    {
                        if (results[i].isMainRepresentation)
                            parentEntityId = EntityId.None;
                        break;
                    }

                    if (results[i].isMainRepresentation)
                        parentEntityId = results[i].entityId;
                }

                if (parentEntityId != EntityId.None)
                {
                    m_Owner.SetSelection(new EntityId[] {parentEntityId}, false);
                    m_Owner.Frame(parentEntityId, true, false);
                }
            }

            bool IsRenaming(EntityId instanceID)
            {
                var renameOverlay = m_Owner.GetRenameOverlay();
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
                Styles.subAssetBg.Draw(rect, GUIContent.none, false, false, false, false);

                // End
                float scaledWidth = texWidth * fraction;
                bool endIsOnLastColumn = (endSubAssetIndex % m_Grid.columns) == (m_Grid.columns - 1);
                float extendEnd = (continued || endIsOnLastColumn) ? 16 * fraction : 8 * fraction;
                Rect rect2 = new Rect(endRect.xMax - scaledWidth + extendEnd, endRect.y + shrinkHeight, scaledWidth, rect.height);
                rect2.y = Mathf.Round(rect2.y);
                rect2.height = Mathf.Ceil(rect2.height);
                GUIStyle endStyle = continued ? Styles.subAssetBgOpenEnded : Styles.subAssetBgCloseEnded;
                endStyle.Draw(rect2, GUIContent.none, false, false, false, false);

                // Middle
                rect = new Rect(rect.xMax, rect.y, rect2.xMin - rect.xMax, rect.height);
                rect.y = Mathf.Round(rect.y);
                rect.height = Mathf.Ceil(rect.height);
                Styles.subAssetBgMiddle.Draw(rect, GUIContent.none, false, false, false, false);
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
                Rect orgPosition = position;

                EntityId assetEntityId = EntityId.None;
                string assetGuid = null;
                bool showFoldout = false;
                bool isSubAssetAndFolderBrowsing = false;
                if (filterItem != null)
                {
                    assetEntityId = filterItem.entityId;
                    assetGuid = filterItem.guid;
                    showFoldout = filterItem.hasChildren && !filterItem.isFolder && isFolderBrowsing; // we do not want to be able to expand folders
                    isSubAssetAndFolderBrowsing = !filterItem.isMainRepresentation && isFolderBrowsing;
                }
                else if (builtinResource != null)
                {
                    assetEntityId = builtinResource.m_EntityId;
                }

                int controlID = GetControlIDFromEntityId(assetEntityId);

                bool selected;
                if (m_Owner.allowDragging)
                    selected = m_DragSelection.Count > 0 ? m_DragSelection.Contains(assetEntityId) : m_Owner.IsSelected(assetEntityId);
                else
                    selected = m_Owner.IsSelected(assetEntityId);

                if (selected && assetEntityId == m_Owner.m_State.m_LastClickedEntityId)
                    m_LastClickedDrawTime = EditorApplication.timeSinceStartup;

                Rect foldoutRect = new Rect(position.x + Styles.groupFoldout.margin.left, position.y, Styles.groupFoldout.padding.left, position.height); // ListMode foldout
                if (showFoldout && !ListMode)
                {
                    float fraction = position.height / 128f;
                    float buttonWidth = 28f;
                    float buttonHeight = 32f;

                    if (fraction < 0.5f)
                    {
                        buttonWidth = 14f;
                        buttonHeight = 16;
                    }
                    else if (fraction < 0.75f)
                    {
                        buttonWidth = 21f;
                        buttonHeight = 24f;
                    }

                    foldoutRect = new Rect(position.xMax - buttonWidth * 0.5f, position.y + (position.height - Styles.resultsGridLabel.fixedHeight) * 0.5f - buttonWidth * 0.5f, buttonWidth, buttonHeight);
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
                                if (IsExpanded(assetEntityId))
                                    toggleState = true;
                                else
                                    SelectAndFrameParentOf(assetEntityId);
                                evt.Use();
                            }
                            break;

                        // Fold out
                        case KeyCode.RightArrow:
                            if (ListMode || m_Owner.IsPreviewIconExpansionModifierPressed())
                            {
                                if (!IsExpanded(assetEntityId))
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
                    bool expanded = !IsExpanded(assetEntityId);
                    if (expanded)
                        m_ItemFader.Start(m_FilteredHierarchy.GetSubAssetInstanceIDs(assetEntityId));
                    ChangeExpandedState(assetEntityId, expanded);
                    evt.Use();
                    GUIUtility.ExitGUI();
                }

                bool isRenaming = IsRenaming(assetEntityId);

                Rect labelRect = position;
                if (!ListMode)
                    labelRect = new Rect(position.x, position.yMax + 1 - Styles.resultsGridLabel.fixedHeight, position.width - 1, Styles.resultsGridLabel.fixedHeight);

                var vcPadding = VersionControlUtils.isVersionControlConnected && ListMode ? k_ListModeVersionControlOverlayPadding : 0;

                float contentStartX = foldoutRect.xMax;
                if (ListMode)
                {
                    itemRect.x = contentStartX;
                    if (isSubAssetAndFolderBrowsing)
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
                    bool isDropTarget = controlID == m_DropTargetControlID;

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
                        Texture2D icon;

                        if (string.IsNullOrEmpty(assetGuid) && m_Owner.GetCreateAssetUtility().entityId == assetEntityId && m_Owner.GetCreateAssetUtility().icon != null)
                        {
                            // If we are creating a new asset we might have an icon to use
                            icon = m_Owner.GetCreateAssetUtility().icon;
                        }
                        else if (builtinResource is ExtraItem extraItem)
                        {
                            icon = extraItem.m_Icon;
                        }
                        else
                        {
                            icon = filterItem != null ? filterItem.icon : null;
                            if (icon == null)
                            {
                                if (ShouldGetAssetPreview(assetEntityId))
                                {
                                    if (assetEntityId != EntityId.None)
                                        icon = AssetPreview.GetAssetPreview(assetEntityId, m_Owner.GetAssetPreviewManagerID());
                                    else if (!string.IsNullOrEmpty(assetGuid))
                                        icon = AssetPreview.GetAssetPreviewFromGUID(assetGuid, m_Owner.GetAssetPreviewManagerID());
                                }
                                else if (assetEntityId != EntityId.None)
                                {
                                    icon = AssetPreview.GetMiniTypeThumbnail(EditorUtility.EntityIdToObject(assetEntityId));
                                }
                            }
                        }

                        if (selected)
                            Styles.resultsLabel.Draw(position, GUIContent.none, false, false, selected, m_Owner.HasFocus());

                        if (isDropTarget)
                            Styles.resultsLabel.Draw(position, GUIContent.none, true, true, false, false);

                        DrawIconAndLabel(new Rect(contentStartX, position.y, position.width - contentStartX, position.height),
                            filterItem, labeltext, icon, selected, m_Owner.HasFocus());

                        // Foldout!
                        if (showFoldout)
                            Styles.groupFoldout.Draw(foldoutRect, !ListMode, !ListMode, IsExpanded(assetEntityId), false);
                    }
                    else // Icon grid
                    {
                        Texture previewImage = null;

                        // Get icon
                        if (string.IsNullOrEmpty(assetGuid) && m_Owner.GetCreateAssetUtility().entityId == assetEntityId && m_Owner.GetCreateAssetUtility().icon != null)
                        {
                            // If we are creating a new asset we might have an icon to use
                            m_Content.image = m_Owner.GetCreateAssetUtility().icon;
                        }
                        else if (builtinResource is ExtraItem extraItem)
                        {
                            m_Content.image = extraItem.m_Icon;
                        }
                        else
                        {
                            // Check for asset preview
                            bool shouldGetAssetPreview = ShouldGetAssetPreview(assetEntityId);
                            if (shouldGetAssetPreview)
                            {
                                if (assetEntityId != EntityId.None)
                                    previewImage = AssetPreview.GetAssetPreview(assetEntityId, m_Owner.GetAssetPreviewManagerID());
                                else if (!string.IsNullOrEmpty(assetGuid))
                                    previewImage = AssetPreview.GetAssetPreviewFromGUID(assetGuid, m_Owner.GetAssetPreviewManagerID());
                            }

                            m_Content.image = previewImage;

                            if (filterItem != null)
                            {
                                // Otherwise use cached icon
                                if (m_Content.image == null)
                                    m_Content.image = filterItem.icon;
                            }

                            // If the icon is still hasn't been found, fall back to the default one
                            if (m_Content.image == null && assetEntityId != EntityId.None)
                            {
                                m_Content.image = AssetPreview.GetMiniTypeThumbnail(EditorUtility.EntityIdToObject(assetEntityId));
                            }
                        }


                        position.height -= Styles.resultsGridLabel.fixedHeight; // get icon rect (remove label height which is included in the position rect)

                        Rect actualImageDrawPosition = (m_Content.image == null) ? new Rect() : ActualImageDrawPosition(position, m_Content.image.width, m_Content.image.height);
                        m_Content.text = null;
                        float alpha = 1f;

                        if (filterItem != null)
                        {
                            AddDirtyStateFor(filterItem.entityId);

                            if (isSubAssetAndFolderBrowsing)
                            {
                                // Adjust subasset size and position on the background slate
                                position.x += 8;
                                position.y += 8;
                                position.width -= 16;
                                position.height -= 16;

                                actualImageDrawPosition = (m_Content.image == null) ? new Rect() : ActualImageDrawPosition(position, m_Content.image.width, m_Content.image.height);

                                alpha = m_ItemFader.GetAlpha(filterItem.entityId);
                                if (alpha < 1f)
                                    m_Owner.Repaint();
                            }
                        }

                        var color = ProjectBrowser.GetAssetItemColor(assetEntityId);

                        using (new GUI.ColorScope(color))
                        {
                            Color orgColor = GUI.color;
                            if (selected)
                                GUI.color = GUI.color * new Color(0.85f, 0.9f, 1f);

                            if (m_Content.image != null)
                            {
                                Color orgColor2 = GUI.color;
                                if (alpha < 1f)
                                    GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, alpha);

                                // Render sub assets with rounded corners so they are visually different than the main representation when shown on the slate background
                                float borderRadius = isSubAssetAndFolderBrowsing ? Mathf.Round(5f * position.width / m_Owner.maxGridSize) : 0f;
                                GUI.DrawTexture(actualImageDrawPosition, m_Content.image, ScaleMode.ScaleToFit, true, 0f, GUI.color, 0f, borderRadius);


                                if (alpha < 1f)
                                    GUI.color = orgColor2;
                            }

                            if (selected)
                                GUI.color = orgColor;

                            // Draw label
                            if (!isRenaming)
                            {
                                if (isDropTarget)
                                    Styles.resultsLabel.Draw(new Rect(labelRect.x - 10, labelRect.y, labelRect.width + 20, labelRect.height), GUIContent.none, true, true, false, false);


                                Texture2D typeIcon = null;
                                if (filterItem != null && previewImage != null)
                                {
                                    Type type = InternalEditorUtility.GetTypeWithoutLoadingObject(filterItem.entityId);

                                    if (type != typeof(Texture2D))
                                    {
                                        typeIcon = filterItem.icon;
                                        if (selected)
                                        {
                                            var activeIcon = EditorUtility.GetIconInActiveState(typeIcon) as Texture2D;
                                            if (activeIcon)
                                                typeIcon = activeIcon;
                                        }
                                    }
                                }

                                if (builtinResource != null)
                                {
                                    Type type = InternalEditorUtility.GetTypeWithoutLoadingObject((EntityId)builtinResource.m_EntityId);

                                    if (type != typeof(Texture2D))
                                    {
                                        typeIcon = AssetPreview.GetMiniTypeThumbnail(type);
                                    }
                                }

                                var orgClipping = Styles.resultsGridLabel.clipping;
                                var orgAlignment = Styles.resultsLabel.alignment;
                                var size = Styles.resultsGridLabel.CalcSizeWithConstraints(GUIContent.Temp(labeltext, typeIcon), orgPosition.size);
                                size.x += Styles.resultsGridLabel.padding.horizontal;
                                labelRect.x = orgPosition.x + (orgPosition.width - size.x) / 2.0f;
                                labelRect.width = size.x;
                                labelRect.height = size.y;
                                m_Owner.sizeUsedForCroppingName = orgPosition.size;

                                Styles.resultsGridLabel.clipping = TextClipping.Ellipsis;
                                Styles.resultsGridLabel.alignment = TextAnchor.MiddleCenter;
                                Styles.resultsGridLabel.Draw(labelRect, GUIContent.Temp(labeltext, typeIcon), false, false, selected, m_Owner.HasFocus());
                                Styles.resultsGridLabel.clipping = orgClipping;
                                Styles.resultsLabel.alignment = orgAlignment;

                                // We only need to set the tooltip once, and not for every item.
                                if (labelRect.Contains(Event.current.mousePosition))
                                {
                                    string tooltip = null;

                                    if (filterItem != null)
                                    {
                                        //We use GetAssetPath to have the file extension as well
                                        string path = AssetDatabase.GetAssetPath(filterItem.entityId);
                                        tooltip = path.Substring(path.LastIndexOf('/') + 1);
                                    }
                                    else if (builtinResource != null)
                                    {
                                        //We have a "None" item in the ObjectSelector that has a 0 instanceID
                                        if (builtinResource.m_EntityId != EntityId.None)
                                            tooltip = builtinResource.m_Name + "\n" + "(Built-in Resource)";
                                    }

                                    if (tooltip != null)
                                    {
                                        GUI.Label(labelRect, GUIContent.Temp("", tooltip));
                                    }
                                }
                            }
                        }

                        if (showFoldout)
                        {
                            var style = Styles.subAssetExpandButton;

                            if (foldoutRect.height <= 16)
                            {
                                style = Styles.subAssetExpandButtonSmall;
                            }
                            else if (foldoutRect.height <= 24)
                            {
                                style = Styles.subAssetExpandButtonMedium;
                            }
                            style.Draw(foldoutRect, !ListMode, !ListMode, IsExpanded(assetEntityId), false);
                        }

                        if (filterItem != null && filterItem.isMainRepresentation)
                        {
                            if (null != postAssetIconDrawCallback)
                            {
                                postAssetIconDrawCallback(position, filterItem.guid, false);
                            }

                            ProjectHooks.OnProjectWindowItem(filterItem.guid, position, m_OwnerRepaintAction);
                        }
                    }
                }
                // Adjust edit field if needed
                if (isRenaming)
                {
                    if (ListMode)
                    {
                        float iconOffset = vcPadding + k_IconWidth + k_SpaceBetweenIconAndText + Styles.resultsLabel.margin.left;
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
                if (filterItem != null && m_Owner.allowUserRenderingHook)
                {
                    if (EditorApplication.projectWindowItemOnGUI != null)
                        EditorApplication.projectWindowItemOnGUI(filterItem.guid, itemRect);

                    if (EditorApplication.projectWindowItemByEntityIdOnGUI != null)
                        EditorApplication.projectWindowItemByEntityIdOnGUI(filterItem.entityId, itemRect);
                }

                // Mouse handling (must be after rename overlay to ensure overlay get mouseevents)
                if (m_Owner.allowDragging)
                    HandleMouseWithDragging(assetEntityId, controlID, position);
                else
                    HandleMouseWithoutDragging(assetEntityId, controlID, position);
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

            public List<KeyValuePair<string, EntityId>> GetVisibleNameAndEntityIds()
            {
                List<KeyValuePair<string, EntityId>> result = new List<KeyValuePair<string, EntityId>>();

                // 1. None item
                if (m_NoneList.Length > 0)
                    result.Add(new KeyValuePair<string, EntityId>(m_NoneList[0].m_Name, m_NoneList[0].m_EntityId)); // 0

                // 2. Project Assets
                foreach (FilteredHierarchy.FilterResult r in m_FilteredHierarchy.results)
                    result.Add(new KeyValuePair<string, EntityId>(r.name, r.entityId));

                // 3. Builtin
                for (int i = 0; i < m_ActiveBuiltinList.Length; ++i)
                    result.Add(new KeyValuePair<string, EntityId>(m_ActiveBuiltinList[i].m_Name, m_ActiveBuiltinList[i].m_EntityId));

                return result;
            }

            private void BeginPing(int instanceID)
            {
            }

            public void GetAssetIds(out List<EntityId> assetIds)
            {
                assetIds = new List<EntityId>();

                // 1. None item
                if (m_NoneList.Length > 0)
                {
                    assetIds.Add(m_NoneList[0].m_EntityId);
                }

                // 2. Project Assets
                foreach (FilteredHierarchy.FilterResult r in m_FilteredHierarchy.results)
                {
                    assetIds.Add(r.entityId);
                }

                if (m_Owner.m_State.m_NewAssetIndexInList >= 0)
                {
                    assetIds.Add(m_Owner.GetCreateAssetUtility().entityId);
                }

                // 3. Builtin
                for (int i = 0; i < m_ActiveBuiltinList.Length; ++i)
                {
                    assetIds.Add(m_ActiveBuiltinList[i].m_EntityId);
                }
            }

            // Returns list of selected instanceIDs
            public List<EntityId> GetNewSelection(EntityId clickedAssetEntityId, bool beginOfDrag, bool useShiftAsActionKey)
            {
                // Flatten grid
                List<EntityId> assetIds;
                GetAssetIds(out assetIds);
                var selectedIds = m_Owner.m_State.m_SelectedInstanceIDs;
                EntityId lastClickedAssetId = m_Owner.m_State.m_LastClickedEntityId;
                bool allowMultiselection = m_Owner.allowMultiSelect;

                return InternalEditorUtility.HandleMultiSelectionWithCurrentModifiers(clickedAssetEntityId, assetIds, selectedIds, lastClickedAssetId, beginOfDrag, allowMultiselection, useShiftAsActionKey);
            }

            public override void UpdateFilter(HierarchyType hierarchyType, SearchFilter searchFilter, bool foldersFirst, SearchService.SearchSessionOptions searchSessionOptions)
            {
                // Filtered hierarchy list
                RefreshHierarchy(hierarchyType, searchFilter, foldersFirst, searchSessionOptions);

                // Filtered builtin list
                RefreshBuiltinResourceList(searchFilter);
            }

            private void RefreshHierarchy(HierarchyType hierarchyType, SearchFilter searchFilter, bool foldersFirst, SearchService.SearchSessionOptions searchSessionOptions)
            {
                m_FilteredHierarchy = new FilteredHierarchy(hierarchyType, searchSessionOptions);
                m_FilteredHierarchy.foldersFirst = foldersFirst;
                m_FilteredHierarchy.searchFilter = searchFilter;
                m_FilteredHierarchy.RefreshVisibleItems(m_Owner.m_State.m_ExpandedInstanceIDs);
            }

            void RefreshBuiltinResourceList(SearchFilter searchFilter)
            {
                // Early out if we do not want to show builtin resources
                if (!m_Owner.allowBuiltinResources || (searchFilter.GetState() == SearchFilter.State.FolderBrowsing) || (searchFilter.GetState() == SearchFilter.State.EmptySearchFilter))
                {
                    m_CurrentBuiltinResources = Array.Empty<BuiltinResource>();
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

            public string GetNameOfLocalAsset(EntityId entityId)
            {
                foreach (var r in m_FilteredHierarchy.results)
                {
                    if (r.entityId == entityId)
                        return r.name;
                }
                return null;
            }

            public bool IsBuiltinAsset(EntityId entityId)
            {
                foreach (KeyValuePair<string, BuiltinResource[]> kvp in m_BuiltinResourceMap)
                {
                    BuiltinResource[] list = kvp.Value;
                    for (int i = 0; i < list.Length; ++i)
                        if (list[i].m_EntityId == entityId)
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
                string typeName = type.ToString().Substring(type.Namespace.Length + 1);

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

            private bool ShouldGetAssetPreview(EntityId assetId)
            {
                string path = AssetDatabase.GetAssetPath(assetId);
                if (m_AssetExtensionsPreviewIgnoreList.Contains(System.IO.Path.GetExtension(path).ToLowerInvariant()))
                    return false;
                Type assetDataType = InternalEditorUtility.GetTypeWithoutLoadingObject(assetId);
                if (m_AssetPreviewIgnoreList.Contains(assetDataType))
                    return false;
                return true;
            }

            public void InitBuiltinResources()
            {
                if (m_BuiltinResourceMap != null)
                    return;

                m_BuiltinResourceMap = new Dictionary<string, BuiltinResource[]>();

                if (m_ShowNoneItem)
                {
                    m_NoneList = new ExtraItem[1];
                    m_NoneList[0] = new ExtraItem();
                    m_NoneList[0].m_EntityId = EntityId.None;
                    m_NoneList[0].m_Name = "None";
                }
                else
                {
                    m_NoneList = Array.Empty<ExtraItem>();
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
                    string propertyPath = AssetDatabase.GetAssetPath(r.entityId);
                    if (EditorUtility.NaturalCompare(System.IO.Path.GetFileNameWithoutExtension(propertyPath), newText) > 0)
                    {
                        return idx;
                    }
                }
                return idx;
            }

            public int IndexOf(EntityId instanceID)
            {
                int idx = 0;

                // 1. 'none' first (has instanceID 0)
                if (m_ShowNoneItem)
                {
                    if (instanceID == EntityId.None)
                        return 0;
                    else
                        idx++;
                }
                else if (instanceID == EntityId.None)
                    return -1;

                // 2. Project assets
                foreach (FilteredHierarchy.FilterResult r in m_FilteredHierarchy.results)
                {
                    // When creating new asset we jump over that item (assuming we do not search for that new asset)
                    if (m_Owner.m_State.m_NewAssetIndexInList == idx)
                        idx++;

                    if (r.entityId == instanceID)
                        return idx;
                    idx++;
                }

                // 3. Builtin resources
                foreach (BuiltinResource b in m_ActiveBuiltinList)
                {
                    if (instanceID == b.m_EntityId)
                        return idx;
                    idx++;
                }
                return -1;
            }

            public FilteredHierarchy.FilterResult LookupByInstanceID(EntityId instanceID)
            {
                if (instanceID == EntityId.None)
                    return null;

                int idx = 0;
                foreach (FilteredHierarchy.FilterResult r in m_FilteredHierarchy.results)
                {
                    // When creating new asset we jump over that item (assuming we do not search for that new asset)
                    if (m_Owner.m_State.m_NewAssetIndexInList == idx)
                        idx++;

                    if (r.entityId == instanceID)
                        return r;
                    idx++;
                }
                return null;
            }

            // Returns true if index was valid. Note that instance can be 0 if 'None' item was found at index
            public bool AssetIdAtIndex(int index, out EntityId assetId)
            {
                assetId = EntityId.None;
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
                    assetId = r.entityId;
                    if (idx == index)
                        return true;
                    idx++;
                }

                // 3. Builtin resources
                foreach (BuiltinResource b in m_ActiveBuiltinList)
                {
                    assetId = b.m_EntityId;
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

            public virtual void StartDrag(EntityId draggedInstanceID, List<EntityId> selectedInstanceIDs)
            {
                ProjectWindowUtil.StartDrag(draggedInstanceID, selectedInstanceIDs);
            }

            public DragAndDropVisualMode DoDrag(EntityId dragToInstanceID, bool perform)
            {
                return DragAndDrop.DropOnProjectBrowserWindow(dragToInstanceID, AssetDatabase.GetAssetPath(dragToInstanceID), perform);
            }

            private const int ImGUI_IdOffset = 100000000;
            static internal int GetControlIDFromEntityId(EntityId entityId) => entityId.GetHashCode() + ImGUI_IdOffset;

            public bool DoCharacterOffsetSelection()
            {
                if (Event.current.type == EventType.KeyDown && Event.current.shift && Event.current.character != 0)
                {
                    System.StringComparison ignoreCase = System.StringComparison.CurrentCultureIgnoreCase;
                    string startName = "";
                    if (Selection.activeObject != null)
                        startName = Selection.activeObject.name;

                    string c = new string(new[] {Event.current.character});
                    List<KeyValuePair<string, EntityId>> list = GetVisibleNameAndEntityIds();
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
                            Selection.activeEntityId = list[index].Value;
                            m_Owner.Repaint();
                            return true;
                        }
                    }
                }

                return false;
            }

            public void ShowObjectsInList(EntityId[] entityIds)
            {
                m_FilteredHierarchy = new FilteredHierarchy(HierarchyType.Assets);
                m_FilteredHierarchy.SetResults(entityIds);
            }

            internal void ShowObjectsInList(EntityId[] entityIds, string[] rootPaths)
            {
                m_FilteredHierarchy = new FilteredHierarchy(HierarchyType.Assets);
                m_FilteredHierarchy.SetResults(entityIds, rootPaths);
            }

            public void DrawIconAndLabel(Rect rect, FilteredHierarchy.FilterResult filterItem, string label, Texture2D icon, bool selected, bool focus)
            {
                var color = filterItem == null ? GUI.color : ProjectBrowser.GetAssetItemColor(filterItem.entityId);

                float vcPadding = s_VCEnabled ? k_ListModeVersionControlOverlayPadding : 0f;
                using (new GUI.ColorScope(color))
                {
                    rect.xMin += Styles.resultsLabel.margin.left;

                    // Reduce the label width to allow delegate drawing on the right.
                    float delegateDrawWidth = (k_ListModeExternalIconPadding * 2) + k_IconWidth;
                    Rect delegateDrawRect = new Rect(rect.xMax - delegateDrawWidth, rect.y, delegateDrawWidth, rect.height);
                    Rect labelRect = new Rect(rect);
                    if (DrawExternalPostLabelInList(delegateDrawRect, filterItem))
                    {
                        labelRect.width = (rect.width - delegateDrawWidth);
                    }

                    Styles.resultsLabel.padding.left = (int)(vcPadding + k_IconWidth + k_SpaceBetweenIconAndText);
                    Styles.resultsLabel.Draw(labelRect, label, false, false, selected, focus);

                    Rect iconRect = rect;
                    iconRect.width = k_IconWidth;
                    iconRect.x += vcPadding * 0.5f;

                    if (selected && focus)
                    {
                        var activeIcon = EditorUtility.GetIconInActiveState(icon) as Texture2D;

                        if (activeIcon)
                            icon = activeIcon;
                    }

                    if (icon != null)
                        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
                }

                if (filterItem != null && filterItem.guid != null && filterItem.isMainRepresentation)
                {
                    Rect overlayRect = rect;
                    overlayRect.width = vcPadding + k_IconWidth;

                    if (null != postAssetIconDrawCallback)
                    {
                        postAssetIconDrawCallback(overlayRect, filterItem.guid, true);
                    }
                    ProjectHooks.OnProjectWindowItem(filterItem.guid, overlayRect, m_OwnerRepaintAction);
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
                List<EntityId> m_EntityIds;

                public void Start(List<EntityId> entityIds)
                {
                    m_EntityIds = entityIds;
                    m_FadeStartTime = EditorApplication.timeSinceStartup;
                    m_FirstToLastDuration = Math.Min(0.5, entityIds.Count * 0.03);
                    m_TimeBetweenEachItem = 0;
                    if (m_EntityIds.Count > 1)
                        m_TimeBetweenEachItem = m_FirstToLastDuration / (m_EntityIds.Count - 1);
                }

                public float GetAlpha(EntityId entityId)
                {
                    if (m_EntityIds == null)
                        return 1f;

                    if (EditorApplication.timeSinceStartup > m_FadeStartTime + m_FadeDuration + m_FirstToLastDuration)
                    {
                        m_EntityIds = null; // reset
                        return 1f;
                    }

                    int index = m_EntityIds.IndexOf(entityId);
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
