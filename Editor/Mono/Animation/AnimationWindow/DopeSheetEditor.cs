// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Boo.Lang.Compiler.Ast;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Event = UnityEngine.Event;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    [System.Serializable]
    internal class DopeSheetEditor : TimeArea, CurveUpdater
    {
        public AnimationWindowState state;

        // How much rendered keyframe left edge is visually offset when compared to the time it represents.
        // A diamond shape left edge isn't representing the time, the middle part is.
        private const float k_KeyframeOffset = -6.5f;
        // Pptr keyframe preview also needs 1px offset so it sits more tightly in the grid
        private const float k_PptrKeyframeOffset = -1;

        const int kLabelMarginHorizontal = 8;
        const int kLabelMarginVertical = 2;

        struct DrawElement
        {
            public Rect position;
            public Color color;
            public Texture2D texture;

            public DrawElement(Rect position, Color color, Texture2D texture)
            {
                this.position = position;
                this.color = color;
                this.texture = texture;
            }
        }

        //  Control point collection renderer
        class DopeSheetControlPointRenderer
        {
            // Unoptimized control point list.  Rendered through GUI.Label calls.
            private List<DrawElement> m_UnselectedKeysDrawBuffer = new List<DrawElement>();
            private List<DrawElement> m_SelectedKeysDrawBuffer = new List<DrawElement>();
            private List<DrawElement> m_DragDropKeysDrawBuffer = new List<DrawElement>();

            // Control point mesh renderers.
            private ControlPointRenderer m_UnselectedKeysRenderer;
            private ControlPointRenderer m_SelectedKeysRenderer;
            private ControlPointRenderer m_DragDropKeysRenderer;

            private Texture2D m_DefaultDopeKeyIcon;

            public void FlushCache()
            {
                m_UnselectedKeysRenderer.FlushCache();
                m_SelectedKeysRenderer.FlushCache();
                m_DragDropKeysRenderer.FlushCache();
            }

            private void DrawElements(List<DrawElement> elements)
            {
                if (elements.Count == 0)
                    return;

                Color oldColor = GUI.color;

                Color color = Color.white;
                GUI.color = color;
                Texture icon = m_DefaultDopeKeyIcon;

                for (int i = 0; i < elements.Count; ++i)
                {
                    DrawElement element = elements[i];

                    // Change color
                    if (element.color != color)
                    {
                        color = GUI.enabled ? element.color : element.color * 0.8f;
                        GUI.color = color;
                    }

                    // Element with specific texture (sprite).
                    if (element.texture != null)
                    {
                        GUI.Label(element.position, element.texture, GUIStyle.none);
                    }
                    // Ordinary control point.
                    else
                    {
                        Rect rect = new Rect((element.position.center.x - icon.width / 2),
                                (element.position.center.y - icon.height / 2),
                                icon.width,
                                icon.height);
                        GUI.Label(rect, icon, GUIStyle.none);
                    }
                }

                GUI.color = oldColor;
            }

            public DopeSheetControlPointRenderer()
            {
                m_DefaultDopeKeyIcon = EditorGUIUtility.LoadIcon("blendKey");

                m_UnselectedKeysRenderer = new ControlPointRenderer(m_DefaultDopeKeyIcon);
                m_SelectedKeysRenderer = new ControlPointRenderer(m_DefaultDopeKeyIcon);
                m_DragDropKeysRenderer = new ControlPointRenderer(m_DefaultDopeKeyIcon);
            }

            public void Clear()
            {
                m_UnselectedKeysDrawBuffer.Clear();
                m_SelectedKeysDrawBuffer.Clear();
                m_DragDropKeysDrawBuffer.Clear();

                m_UnselectedKeysRenderer.Clear();
                m_SelectedKeysRenderer.Clear();
                m_DragDropKeysRenderer.Clear();
            }

            public void Render()
            {
                DrawElements(m_UnselectedKeysDrawBuffer);
                m_UnselectedKeysRenderer.Render();

                DrawElements(m_SelectedKeysDrawBuffer);
                m_SelectedKeysRenderer.Render();

                DrawElements(m_DragDropKeysDrawBuffer);
                m_DragDropKeysRenderer.Render();
            }

            public void AddUnselectedKey(DrawElement element)
            {
                // Control point has a specific texture (sprite image).
                // This will not be batched rendered and must be handled separately.
                if (element.texture != null)
                {
                    m_UnselectedKeysDrawBuffer.Add(element);
                }
                else
                {
                    Rect rect = element.position;
                    rect.size = new Vector2(m_DefaultDopeKeyIcon.width, m_DefaultDopeKeyIcon.height);
                    m_UnselectedKeysRenderer.AddPoint(rect, element.color);
                }
            }

            public void AddSelectedKey(DrawElement element)
            {
                // Control point has a specific texture (sprite image).
                // This will not be batched rendered and must be handled separately.
                if (element.texture != null)
                {
                    m_SelectedKeysDrawBuffer.Add(element);
                }
                else
                {
                    Rect rect = element.position;
                    rect.size = new Vector2(m_DefaultDopeKeyIcon.width, m_DefaultDopeKeyIcon.height);
                    m_SelectedKeysRenderer.AddPoint(rect, element.color);
                }
            }

            public void AddDragDropKey(DrawElement element)
            {
                // Control point has a specific texture (sprite image).
                // This will not be batched rendered and must be handled separately.
                if (element.texture != null)
                {
                    m_DragDropKeysDrawBuffer.Add(element);
                }
                else
                {
                    Rect rect = element.position;
                    rect.size = new Vector2(m_DefaultDopeKeyIcon.width, m_DefaultDopeKeyIcon.height);
                    m_DragDropKeysRenderer.AddPoint(rect, element.color);
                }
            }
        }

        public float contentHeight
        {
            get
            {
                float height = 0f;

                foreach (DopeLine dopeline in state.dopelines)
                    height += dopeline.tallMode ? AnimationWindowHierarchyGUI.k_DopeSheetRowHeightTall : AnimationWindowHierarchyGUI.k_DopeSheetRowHeight;

                height += AnimationWindowHierarchyGUI.k_AddCurveButtonNodeHeight;
                return height;
            }
        }

        [SerializeField] public EditorWindow m_Owner;

        DopeSheetSelectionRect m_SelectionRect;

        float m_DragStartTime;
        bool m_MousedownOnKeyframe;
        bool m_IsDragging;
        bool m_IsDraggingPlayheadStarted;
        bool m_IsDraggingPlayhead;

        bool m_Initialized;

        bool m_SpritePreviewLoading;
        int m_SpritePreviewCacheSize;

        public Bounds m_Bounds = new Bounds(Vector3.zero, Vector3.zero);
        public override Bounds drawingBounds { get { return m_Bounds; } }

        public bool isDragging { get { return m_IsDragging; } }

        DopeSheetControlPointRenderer m_PointRenderer;

        DopeSheetEditorRectangleTool m_RectangleTool;

        internal int assetPreviewManagerID
        {
            get { return m_Owner != null ? m_Owner.GetInstanceID() : 0; }
        }

        public bool spritePreviewLoading { get { return m_SpritePreviewLoading; } }

        public DopeSheetEditor(EditorWindow owner) : base(false)
        {
            m_Owner = owner;
        }

        public void OnDisable()
        {
            if (m_PointRenderer != null)
                m_PointRenderer.FlushCache();
        }

        internal void OnDestroy()
        {
            AssetPreview.DeletePreviewTextureManagerByID(assetPreviewManagerID);
        }

        public void OnGUI(Rect position, Vector2 scrollPosition)
        {
            Init();

            // drag'n'drops outside any dopelines
            HandleDragAndDropToEmptyArea();

            GUIClip.Push(position, scrollPosition, Vector2.zero, false);

            HandleRectangleToolEvents();

            Rect localRect = new Rect(0, 0, position.width, position.height);
            Rect dopesheetRect = DopelinesGUI(localRect, scrollPosition);

            HandleKeyboard();
            HandleDragging();
            HandleSelectionRect(dopesheetRect);
            HandleDelete();

            RectangleToolGUI();

            GUIClip.Pop();
        }

        public void Init()
        {
            if (!m_Initialized)
            {
                // Set TimeArea constrains
                hSlider = true;
                vSlider = false;
                hRangeLocked = false;
                vRangeLocked = true;
                hRangeMin = 0;
                margin = 40;
                scaleWithWindow = true;
                ignoreScrollWheelUntilClicked = false;
            }
            m_Initialized = true;

            if (m_PointRenderer == null)
                m_PointRenderer = new DopeSheetControlPointRenderer();

            if (m_RectangleTool == null)
            {
                m_RectangleTool = new DopeSheetEditorRectangleTool();
                m_RectangleTool.Initialize(this);
            }
        }

        public void RecalculateBounds()
        {
            if (!state.disabled)
            {
                Vector2 timeRange = state.timeRange;
                m_Bounds.SetMinMax(new Vector3(timeRange.x, 0, 0), new Vector3(timeRange.y, 0, 0));
            }
        }

        private Rect DopelinesGUI(Rect position, Vector2 scrollPosition)
        {
            Color oldColor = GUI.color;
            Rect linePosition = position;

            m_PointRenderer.Clear();

            if (Event.current.type == EventType.Repaint)
                m_SpritePreviewLoading = false;

            // Workaround for cases when mouseup happens outside the window. Apparently the mouseup event is lost (not true on OSX, though).
            if (Event.current.type == EventType.MouseDown)
                m_IsDragging = false;

            // Find out how large preview pool is needed for sprite previews
            UpdateSpritePreviewCacheSize();

            List<DopeLine> dopelines = state.dopelines;
            for (int i = 0; i < dopelines.Count; ++i)
            {
                DopeLine dopeLine = dopelines[i];

                dopeLine.position = linePosition;
                dopeLine.position.height = (dopeLine.tallMode ? AnimationWindowHierarchyGUI.k_DopeSheetRowHeightTall : AnimationWindowHierarchyGUI.k_DopeSheetRowHeight);

                // Cull out dopelines that are not visible
                if (dopeLine.position.yMin + scrollPosition.y >= position.yMin && dopeLine.position.yMin + scrollPosition.y <= position.yMax ||
                    dopeLine.position.yMax + scrollPosition.y >= position.yMin && dopeLine.position.yMax + scrollPosition.y <= position.yMax)
                {
                    Event evt = Event.current;

                    switch (evt.type)
                    {
                        case EventType.DragUpdated:
                        case EventType.DragPerform:
                        {
                            HandleDragAndDrop(dopeLine);
                            break;
                        }
                        case EventType.ContextClick:
                        {
                            if (!m_IsDraggingPlayhead)
                            {
                                HandleContextMenu(dopeLine);
                            }

                            break;
                        }
                        case EventType.MouseDown:
                        {
                            if (evt.button == 0)
                            {
                                HandleMouseDown(dopeLine);
                            }
                            break;
                        }
                        case EventType.Repaint:
                        {
                            DopeLineRepaint(dopeLine);
                            break;
                        }
                    }
                }

                linePosition.y += dopeLine.position.height;
            }

            if (Event.current.type == EventType.MouseUp)
            {
                m_IsDraggingPlayheadStarted = false;
                m_IsDraggingPlayhead = false;
            }

            Rect dopelinesRect = new Rect(position.xMin, position.yMin, position.width, linePosition.yMax - position.yMin);

            if (Event.current.type == EventType.Repaint)
                m_PointRenderer.Render();

            GUI.color = oldColor;

            return dopelinesRect;
        }

        private void RectangleToolGUI()
        {
            m_RectangleTool.OnGUI();
        }

        private void DrawGrid(Rect position)
        {
            TimeRuler(position, state.frameRate, false, true, 0.2f);
        }

        public void DrawMasterDopelineBackground(Rect position)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            AnimationWindowStyles.eventBackground.Draw(position, false, false, false, false);
        }

        void UpdateSpritePreviewCacheSize()
        {
            int newPreviewCacheSize = 1;

            // Add all expanded sprite dopelines
            foreach (DopeLine dopeLine in state.dopelines)
            {
                if (dopeLine.tallMode && dopeLine.isPptrDopeline)
                {
                    newPreviewCacheSize += dopeLine.keys.Count;
                }
            }

            // Add all drag'n'drop objects
            newPreviewCacheSize += DragAndDrop.objectReferences.Length;

            if (newPreviewCacheSize > m_SpritePreviewCacheSize)
            {
                AssetPreview.SetPreviewTextureCacheSize(newPreviewCacheSize, assetPreviewManagerID);
                m_SpritePreviewCacheSize = newPreviewCacheSize;
            }
        }

        private void DopeLineRepaint(DopeLine dopeline)
        {
            Color oldColor = GUI.color;

            AnimationWindowHierarchyNode node = (AnimationWindowHierarchyNode)state.hierarchyData.FindItem(dopeline.hierarchyNodeID);
            bool isChild = node != null && node.depth > 0;
            Color color = isChild ? Color.gray.AlphaMultiplied(0.05f) : Color.gray.AlphaMultiplied(0.16f);

            // Draw background
            if (!dopeline.isMasterDopeline)
                DrawBox(dopeline.position, color);

            // Draw keys
            int? previousTimeHash = null;
            int length = dopeline.keys.Count;

            for (int i = 0; i < length; i++)
            {
                AnimationWindowKeyframe keyframe = dopeline.keys[i];
                int timeHash = keyframe.m_TimeHash ^ keyframe.curve.timeOffset.GetHashCode();
                // Hash optimizations
                if (previousTimeHash == timeHash)
                    continue;

                previousTimeHash = timeHash;

                // Default values
                Rect rect = GetKeyframeRect(dopeline, keyframe);
                color = dopeline.isMasterDopeline ? Color.gray.RGBMultiplied(0.85f) : Color.gray.RGBMultiplied(1.2f);
                Texture2D texture = null;

                if (keyframe.isPPtrCurve && dopeline.tallMode)
                    texture = keyframe.value == null ? null : AssetPreview.GetAssetPreview(((Object)keyframe.value).GetInstanceID(), assetPreviewManagerID);

                if (texture != null)
                {
                    rect = GetPreviewRectFromKeyFrameRect(rect);
                    color = Color.white.AlphaMultiplied(0.5f);
                }
                else if (keyframe.value != null && keyframe.isPPtrCurve && dopeline.tallMode)
                {
                    m_SpritePreviewLoading = true;
                }

                // TODO: Find out why zero time, and only zero time, is offset from grid
                if (Mathf.Approximately(keyframe.time, 0f))
                    rect.xMin -= 0.01f;

                if (AnyKeyIsSelectedAtTime(dopeline, i))
                {
                    color = dopeline.tallMode && dopeline.isPptrDopeline ? Color.white : new Color(0.34f, 0.52f, 0.85f, 1f);
                    if (dopeline.isMasterDopeline)
                        color = color.RGBMultiplied(0.85f);

                    m_PointRenderer.AddSelectedKey(new DrawElement(rect, color, texture));
                }
                else
                {
                    m_PointRenderer.AddUnselectedKey(new DrawElement(rect, color, texture));
                }
            }

            if (DoDragAndDrop(dopeline, dopeline.position, false))
            {
                float time = Mathf.Max(state.PixelToTime(Event.current.mousePosition.x, AnimationWindowState.SnapMode.SnapToClipFrame), 0f);

                Color keyColor = Color.gray.RGBMultiplied(1.2f);
                Texture2D texture = null;

                foreach (Object obj in GetSortedDragAndDropObjectReferences())
                {
                    Rect rect = GetDragAndDropRect(dopeline, time);

                    if (dopeline.isPptrDopeline && dopeline.tallMode)
                        texture = AssetPreview.GetAssetPreview(obj.GetInstanceID(), assetPreviewManagerID);

                    if (texture != null)
                    {
                        rect = GetPreviewRectFromKeyFrameRect(rect);
                        keyColor = Color.white.AlphaMultiplied(0.5f);
                    }

                    m_PointRenderer.AddDragDropKey(new DrawElement(rect, keyColor, texture));

                    time += 1f / state.frameRate;
                }
            }

            GUI.color = oldColor;
        }

        private Rect GetPreviewRectFromKeyFrameRect(Rect keyframeRect)
        {
            keyframeRect.width -= 2;
            keyframeRect.height -= 2;
            keyframeRect.xMin += 2;
            keyframeRect.yMin += 2;

            return keyframeRect;
        }

        private Rect GetDragAndDropRect(DopeLine dopeline, float time)
        {
            Rect rect = GetKeyframeRect(dopeline, null);
            float offsetX = GetKeyframeOffset(dopeline, null);
            rect.center = new Vector2(state.TimeToPixel(time) + rect.width * .5f + offsetX, rect.center.y);
            return rect;
        }

        // TODO: This is just temporary until real styles
        private static void DrawBox(Rect position, Color color)
        {
            Color oldColor = GUI.color;
            GUI.color = color;
            DopeLine.dopekeyStyle.Draw(position, GUIContent.none, 0, false);
            GUI.color = oldColor;
        }

        private GenericMenu GenerateMenu(DopeLine dopeline)
        {
            GenericMenu menu = new GenericMenu();

            // Collect hovering keys.
            List<AnimationWindowKeyframe> hoveringKeys = new List<AnimationWindowKeyframe>();
            foreach (var key in dopeline.keys)
            {
                Rect rect = GetKeyframeRect(dopeline, key);

                if (rect.Contains(Event.current.mousePosition))
                    hoveringKeys.Add(key);
            }

            AnimationKeyTime mouseKeyTime = AnimationKeyTime.Time(state.PixelToTime(Event.current.mousePosition.x, AnimationWindowState.SnapMode.SnapToClipFrame), state.frameRate);

            string str = "Add Key";
            if (dopeline.isEditable && hoveringKeys.Count == 0)
                menu.AddItem(new GUIContent(str), false, AddKeyToDopeline, new AddKeyToDopelineContext {dopeline = dopeline, time = mouseKeyTime});
            else
                menu.AddDisabledItem(new GUIContent(str));

            str = state.selectedKeys.Count > 1 ? "Delete Keys" : "Delete Key";
            if (dopeline.isEditable && (state.selectedKeys.Count > 0 || hoveringKeys.Count > 0))
                menu.AddItem(new GUIContent(str), false, DeleteKeys, state.selectedKeys.Count > 0 ? state.selectedKeys : hoveringKeys);
            else
                menu.AddDisabledItem(new GUIContent(str));

            // Float curve tangents
            if (dopeline.isEditable && AnimationWindowUtility.ContainsFloatKeyframes(state.selectedKeys))
            {
                menu.AddSeparator(string.Empty);

                List<KeyIdentifier> keyList = new List<KeyIdentifier>();
                Hashtable editorCurves = new Hashtable();
                foreach (AnimationWindowKeyframe key in state.selectedKeys)
                {
                    if (key.isDiscreteCurve)
                        continue;

                    int index = key.curve.GetKeyframeIndex(AnimationKeyTime.Time(key.time, state.frameRate));
                    if (index == -1)
                        continue;

                    int id = key.curve.GetHashCode();

                    AnimationCurve curve = (AnimationCurve)editorCurves[id];
                    if (curve == null)
                    {
                        curve = AnimationUtility.GetEditorCurve(key.curve.clip, key.curve.binding);
                        if (curve == null)
                            curve = new AnimationCurve();

                        editorCurves.Add(id, curve);
                    }

                    keyList.Add(new KeyIdentifier(curve, id, index, key.curve.binding));
                }

                CurveMenuManager menuManager = new CurveMenuManager(this);
                menuManager.AddTangentMenuItems(menu, keyList);
            }

            return menu;
        }

        private void HandleDragging()
        {
            int id = EditorGUIUtility.GetControlID("dopesheetdrag".GetHashCode(), FocusType.Passive, new Rect());
            EventType eventType = Event.current.GetTypeForControl(id);

            if ((eventType == EventType.MouseDrag || eventType == EventType.MouseUp) && m_MousedownOnKeyframe)
            {
                if (eventType == EventType.MouseDrag && !EditorGUI.actionKey && !Event.current.shift)
                {
                    if (!m_IsDragging && state.selectedKeys.Count > 0)
                    {
                        m_IsDragging = true;
                        m_IsDraggingPlayheadStarted = true;
                        GUIUtility.hotControl = id;
                        m_DragStartTime = state.PixelToTime(Event.current.mousePosition.x);
                        m_RectangleTool.OnStartMove(new Vector2(m_DragStartTime, 0f), m_RectangleTool.rippleTimeClutch);
                        Event.current.Use();
                    }
                }

                // What is the distance from first selected key to zero time. We need this in order to make sure no key goes to negative time while dragging.
                float firstSelectedKeyTime = float.MaxValue;
                foreach (AnimationWindowKeyframe selectedKey in state.selectedKeys)
                    firstSelectedKeyTime = Mathf.Min(selectedKey.time, firstSelectedKeyTime);

                float currentTime = state.SnapToFrame(state.PixelToTime(Event.current.mousePosition.x), AnimationWindowState.SnapMode.SnapToClipFrame);

                if (m_IsDragging)
                {
                    if (!Mathf.Approximately(currentTime, m_DragStartTime))
                    {
                        m_RectangleTool.OnMove(new Vector2(currentTime, 0f));
                        Event.current.Use();
                    }
                }

                if (eventType == EventType.MouseUp)
                {
                    if (m_IsDragging && GUIUtility.hotControl == id)
                    {
                        m_RectangleTool.OnEndMove();
                        Event.current.Use();
                        m_IsDragging = false;
                    }
                    m_MousedownOnKeyframe = false;
                    GUIUtility.hotControl = 0;
                }
            }

            if (m_IsDraggingPlayheadStarted && eventType == EventType.MouseDrag && Event.current.button == 1)
            {
                m_IsDraggingPlayhead = true;

                //int frame = state.m_Frame;
                //if (!m_IsDragging)
                //  frame = state.TimeToFrameFloor(state.SnapToFrame (state.PixelToTime (Event.current.mousePosition.x)));

                //state.animationWindow.PreviewFrame (frame);
                Event.current.Use();
            }

            if (m_IsDragging)
            {
                Vector2 mousePosition = Event.current.mousePosition;
                Rect mouseRect = new Rect(mousePosition.x - 10, mousePosition.y - 10, 20, 20);

                EditorGUIUtility.AddCursorRect(mouseRect, MouseCursor.MoveArrow);
            }
        }

        private void HandleKeyboard()
        {
            if (Event.current.type == EventType.ValidateCommand || Event.current.type == EventType.ExecuteCommand)
            {
                switch (Event.current.commandName)
                {
                    case "SelectAll":
                        if (Event.current.type == EventType.ExecuteCommand)
                            HandleSelectAll();
                        Event.current.Use();
                        break;
                    case "FrameSelected":
                        if (Event.current.type == EventType.ExecuteCommand)
                            FrameSelected();
                        Event.current.Use();
                        break;
                }
            }

            // Frame All.
            // Manually handle hotkey unless we decide to add it to default Unity hotkeys like
            // we did for FrameSelected.
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.A)
                {
                    FrameClip();
                    Event.current.Use();
                }
            }
        }

        private void HandleSelectAll()
        {
            foreach (DopeLine dopeline in state.dopelines)
            {
                foreach (AnimationWindowKeyframe keyframe in dopeline.keys)
                {
                    state.SelectKey(keyframe);
                }
                state.SelectHierarchyItem(dopeline, true, false);
            }
        }

        private void HandleDelete()
        {
            if (state.selectedKeys.Count == 0)
                return;

            switch (Event.current.type)
            {
                case EventType.ValidateCommand:
                case EventType.ExecuteCommand:
                    if ((Event.current.commandName == "SoftDelete" || Event.current.commandName == "Delete"))
                    {
                        if (Event.current.type == EventType.ExecuteCommand)
                            state.DeleteSelectedKeys();
                        Event.current.Use();
                    }
                    break;

                case EventType.KeyDown:
                    if (Event.current.keyCode == KeyCode.Backspace || Event.current.keyCode == KeyCode.Delete)
                    {
                        state.DeleteSelectedKeys();
                        Event.current.Use();
                    }
                    break;
            }
        }

        private void HandleSelectionRect(Rect rect)
        {
            if (m_SelectionRect == null)
                m_SelectionRect = new DopeSheetSelectionRect(this);

            if (!m_MousedownOnKeyframe)
                m_SelectionRect.OnGUI(rect);
        }

        // Handles drag and drop into empty area outside dopelines
        private void HandleDragAndDropToEmptyArea()
        {
            Event evt = Event.current;

            if (evt.type != EventType.DragPerform && evt.type != EventType.DragUpdated)
                return;

            if (!ValidateDragAndDropObjects())
                return;

            // TODO: handle multidropping of other types than sprites/textures
            if (DragAndDrop.objectReferences[0].GetType() == typeof(Sprite) || DragAndDrop.objectReferences[0].GetType() == typeof(Texture2D))
            {
                foreach (var selectedItem in state.selection.ToArray())
                {
                    if (!selectedItem.clipIsEditable)
                        continue;

                    if (!selectedItem.canAddCurves)
                        continue;

                    if (DopelineForValueTypeExists(typeof(Sprite)))
                        continue;

                    if (evt.type == EventType.DragPerform)
                    {
                        EditorCurveBinding? spriteBinding = CreateNewPptrDopeline(selectedItem, typeof(Sprite));
                        if (spriteBinding != null)
                        {
                            DoSpriteDropAfterGeneratingNewDopeline(selectedItem.animationClip, spriteBinding);
                        }
                    }

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    evt.Use();
                    return;
                }
            }
            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
        }

        private void DoSpriteDropAfterGeneratingNewDopeline(AnimationClip animationClip, EditorCurveBinding? spriteBinding)
        {
            if (DragAndDrop.objectReferences.Length == 1)
            {
                UsabilityAnalytics.Event("Sprite Drag and Drop", "Drop single sprite into empty dopesheet", "null", 1);
            }
            else
            {
                UsabilityAnalytics.Event("Sprite Drag and Drop", "Drop multiple sprites into empty dopesheet", "null", 1);
            }

            // Create the new curve for our sprites
            AnimationWindowCurve newCurve = new AnimationWindowCurve(animationClip, (EditorCurveBinding)spriteBinding, typeof(Sprite));

            // And finally perform the drop onto the curve
            PerformDragAndDrop(newCurve, 0f);
        }

        private void HandleRectangleToolEvents()
        {
            m_RectangleTool.HandleEvents();
        }

        private bool DopelineForValueTypeExists(Type valueType)
        {
            return state.allCurves.Exists(curve => curve.valueType == valueType);
        }

        private EditorCurveBinding? CreateNewPptrDopeline(AnimationWindowSelectionItem selectedItem, Type valueType)
        {
            List<EditorCurveBinding> potentialBindings = null;
            if (selectedItem.rootGameObject != null)
            {
                potentialBindings = AnimationWindowUtility.GetAnimatableProperties(selectedItem.rootGameObject, selectedItem.rootGameObject, valueType);
                if (potentialBindings.Count == 0 && valueType == typeof(Sprite))  // No animatable properties for Sprite available. Default as SpriteRenderer.
                {
                    return CreateNewSpriteRendererDopeline(selectedItem.rootGameObject, selectedItem.rootGameObject);
                }
            }
            else if (selectedItem.scriptableObject != null)
            {
                potentialBindings = AnimationWindowUtility.GetAnimatableProperties(selectedItem.scriptableObject, valueType);
            }

            if (potentialBindings == null || potentialBindings.Count == 0)
                return null;

            if (potentialBindings.Count == 1) // Single property for this valuetype, return it
            {
                return potentialBindings[0];
            }
            else // Multiple properties, dropdown selection
            {
                List<string> menuItems = new List<string>();
                foreach (EditorCurveBinding binding in potentialBindings)
                    menuItems.Add(binding.type.Name);

                List<object> userDataList = new List<object>();
                userDataList.Add(selectedItem.animationClip);
                userDataList.Add(potentialBindings);

                Rect r = new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 1, 1);
                EditorUtility.DisplayCustomMenu(r, EditorGUIUtility.TempContent(menuItems.ToArray()), -1, SelectTypeForCreatingNewPptrDopeline, userDataList);
                return null; // We return null, but creation is handled via dropdown callback code
            }
        }

        private void SelectTypeForCreatingNewPptrDopeline(object userData, string[] options, int selected)
        {
            List<object> userDataList = userData as List<object>;
            AnimationClip animationClip = userDataList[0] as AnimationClip;
            List<EditorCurveBinding> bindings = userDataList[1] as List<EditorCurveBinding>;

            if (bindings.Count > selected)
                DoSpriteDropAfterGeneratingNewDopeline(animationClip, bindings[selected]);
        }

        private EditorCurveBinding? CreateNewSpriteRendererDopeline(GameObject targetGameObject, GameObject rootGameObject)
        {
            // Let's make sure there is spriterenderer to animate
            if (!targetGameObject.GetComponent<SpriteRenderer>())
                targetGameObject.AddComponent<SpriteRenderer>();

            // Now we should always find an animatable binding for it
            List<EditorCurveBinding> curveBindings = AnimationWindowUtility.GetAnimatableProperties(targetGameObject, rootGameObject, typeof(SpriteRenderer), typeof(Sprite));
            if (curveBindings.Count == 1)
                return curveBindings[0];

            // Something went wrong
            Debug.LogError("Unable to create animatable SpriteRenderer component");
            return null;
        }

        private void HandleDragAndDrop(DopeLine dopeline)
        {
            Event evt = Event.current;

            if (evt.type != EventType.DragPerform && evt.type != EventType.DragUpdated)
                return;

            if (DoDragAndDrop(dopeline, dopeline.position, evt.type == EventType.DragPerform))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                evt.Use();
            }
            else
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }
        }

        private void HandleMouseDown(DopeLine dopeline)
        {
            Event evt = Event.current;
            if (!dopeline.position.Contains(evt.mousePosition))
                return;

            //state.animationWindow.EnsureAnimationMode ();

            bool keysAreSelected = false;
            foreach (AnimationWindowKeyframe keyframe in dopeline.keys)
            {
                Rect r = GetKeyframeRect(dopeline, keyframe);
                if (r.Contains(evt.mousePosition) && state.KeyIsSelected(keyframe))
                {
                    keysAreSelected = true;
                    break;
                }
            }

            // For ctrl selecting, we unselect keys if we clicked a selected key frame.
            bool canUnselectKeys = (keysAreSelected && EditorGUI.actionKey);

            // Only allow new selected keys if current clicked key frame is unselected.
            bool canSelectKeys = !keysAreSelected;

            // If there are no selected keyframe in click without shift or EditorGUI.actionKey, then clear all other selections
            if (!keysAreSelected && !EditorGUI.actionKey && !evt.shift)
                state.ClearSelections();

            float startTime = state.PixelToTime(Event.current.mousePosition.x);
            float endTime = startTime;

            // For shift selecting we need to have time range we choose between
            if (Event.current.shift)
            {
                foreach (AnimationWindowKeyframe key in dopeline.keys)
                {
                    if (state.KeyIsSelected(key))
                    {
                        if (key.time < startTime)
                            startTime = key.time;
                        if (key.time > endTime)
                            endTime = key.time;
                    }
                }
            }

            bool clickedOnKeyframe = false;
            foreach (AnimationWindowKeyframe keyframe in dopeline.keys)
            {
                Rect r = GetKeyframeRect(dopeline, keyframe);
                if (r.Contains(evt.mousePosition))
                {
                    clickedOnKeyframe = true;

                    if (canUnselectKeys)
                    {
                        if (state.KeyIsSelected(keyframe))
                        {
                            state.UnselectKey(keyframe);

                            if (!state.AnyKeyIsSelected(dopeline))
                                state.UnSelectHierarchyItem(dopeline);
                        }
                    }
                    else if (canSelectKeys)
                    {
                        if (!state.KeyIsSelected(keyframe))
                        {
                            if (Event.current.shift)
                            {
                                foreach (AnimationWindowKeyframe key in dopeline.keys)
                                    if (key == keyframe || key.time > startTime && key.time < endTime)
                                        state.SelectKey(key);
                            }
                            else
                            {
                                state.SelectKey(keyframe);
                            }

                            if (!dopeline.isMasterDopeline)
                                state.SelectHierarchyItem(dopeline, EditorGUI.actionKey || evt.shift);
                        }
                    }

                    state.activeKeyframe = keyframe;
                    m_MousedownOnKeyframe = true;
                    evt.Use();
                }
            }

            if (dopeline.isMasterDopeline)
            {
                state.ClearHierarchySelection();

                List<int> hierarchyIDs = state.GetAffectedHierarchyIDs(state.selectedKeys);
                foreach (int id in hierarchyIDs)
                    state.SelectHierarchyItem(id, true, true);
            }

            if (evt.clickCount == 2 && evt.button == 0 && !Event.current.shift && !EditorGUI.actionKey)
                HandleDopelineDoubleclick(dopeline);

            // Move playhead when clicked with right mouse button
            if (evt.button == 1 && !state.controlInterface.playing)
            {
                // Clear keyframe selection if right clicked empty space
                if (!clickedOnKeyframe)
                {
                    state.ClearSelections();
                    m_IsDraggingPlayheadStarted = true;
                    HandleUtility.Repaint();
                    evt.Use();
                }
            }
        }

        private void HandleDopelineDoubleclick(DopeLine dopeline)
        {
            float timeAtMousePosition = state.PixelToTime(Event.current.mousePosition.x, AnimationWindowState.SnapMode.SnapToClipFrame);
            AnimationKeyTime mouseKeyTime = AnimationKeyTime.Time(timeAtMousePosition, state.frameRate);
            AnimationWindowUtility.AddKeyframes(state, dopeline.curves.ToArray(), mouseKeyTime);

            Event.current.Use();
        }

        private void HandleContextMenu(DopeLine dopeline)
        {
            if (!dopeline.position.Contains(Event.current.mousePosition))
                return;

            // Actual context menu
            GenerateMenu(dopeline).ShowAsContext();
        }

        private Rect GetKeyframeRect(DopeLine dopeline, AnimationWindowKeyframe keyframe)
        {
            float time = keyframe != null ? keyframe.time + keyframe.curve.timeOffset : 0f;

            float width = 10f;
            if (dopeline.isPptrDopeline && dopeline.tallMode && (keyframe == null || keyframe.value != null))
                width = dopeline.position.height;

            return new Rect(state.TimeToPixel(state.SnapToFrame(time, AnimationWindowState.SnapMode.SnapToClipFrame)) + GetKeyframeOffset(dopeline, keyframe), dopeline.position.yMin, width, dopeline.position.height);
        }

        // This means "how much is the rendered keyframe offset in pixels for x-axis".
        // Say you are rendering keyframe to some time t. The time t relates to some pixel x, but you then need to offset because keyframe diamond center represents the time, not the left edge
        // However for pptr keyframes, the time is represented by left edge
        private float GetKeyframeOffset(DopeLine dopeline, AnimationWindowKeyframe keyframe)
        {
            if (dopeline.isPptrDopeline && dopeline.tallMode && (keyframe == null || keyframe.value != null))
                return k_PptrKeyframeOffset;
            else
                return k_KeyframeOffset;
        }

        // Frame the selected keyframes or selected dopelines
        public void FrameClip()
        {
            if (state.disabled)
                return;

            Vector2 timeRange = state.timeRange;
            timeRange.y = Mathf.Max(timeRange.x + 0.1f, timeRange.y);
            SetShownHRangeInsideMargins(timeRange.x, timeRange.y);
        }

        public void FrameSelected()
        {
            Bounds frameBounds = new Bounds();
            bool firstKey = true;

            bool keyframesSelected = state.selectedKeys.Count > 0;

            if (keyframesSelected)
            {
                foreach (AnimationWindowKeyframe key in state.selectedKeys)
                {
                    Vector2 pt = new Vector2(key.time + key.curve.timeOffset, 0.0f);
                    if (firstKey)
                    {
                        frameBounds.SetMinMax(pt, pt);
                        firstKey = false;
                    }
                    else
                    {
                        frameBounds.Encapsulate(pt);
                    }
                }
            }

            // No keyframes selected. Frame to selected dopelines
            bool frameToClip = !keyframesSelected;
            if (!keyframesSelected)
            {
                bool dopelinesSelected = state.hierarchyState.selectedIDs.Count > 0;
                if (dopelinesSelected)
                {
                    foreach (AnimationWindowCurve curve in state.activeCurves)
                    {
                        int keyCount = curve.m_Keyframes.Count;

                        if (keyCount > 1)
                        {
                            Vector2 pt1 = new Vector2(curve.m_Keyframes[0].time + curve.timeOffset, 0.0f);
                            Vector2 pt2 = new Vector2(curve.m_Keyframes[keyCount - 1].time + curve.timeOffset, 0.0f);

                            if (firstKey)
                            {
                                frameBounds.SetMinMax(pt1, pt2);
                                firstKey = false;
                            }
                            else
                            {
                                frameBounds.Encapsulate(pt1);
                                frameBounds.Encapsulate(pt2);
                            }

                            frameToClip = false;
                        }
                    }
                }
            }


            if (frameToClip)
                FrameClip();
            else
            {
                // Let's make sure we don't zoom too close.
                frameBounds.size = new Vector3(Mathf.Max(frameBounds.size.x, 0.1f), Mathf.Max(frameBounds.size.y, 0.1f), 0);
                SetShownHRangeInsideMargins(frameBounds.min.x, frameBounds.max.x);
            }
        }

        private bool DoDragAndDrop(DopeLine dopeLine, Rect position, bool perform)
        {
            if (position.Contains(Event.current.mousePosition) == false)
                return false;

            if (!ValidateDragAndDropObjects())
                return false;

            System.Type targetType = DragAndDrop.objectReferences[0].GetType();
            AnimationWindowCurve curve = null;
            if (dopeLine.valueType == targetType)
            {
                curve = dopeLine.curves[0];
            }
            else
            {
                // dopeline ValueType wasn't exact match. We can still look for a curve that accepts our drop object type
                foreach (AnimationWindowCurve dopelineCurve in dopeLine.curves)
                {
                    if (dopelineCurve.isPPtrCurve)
                    {
                        if (dopelineCurve.valueType == targetType)
                            curve = dopelineCurve;

                        List<Sprite> sprites = SpriteUtility.GetSpriteFromPathsOrObjects(DragAndDrop.objectReferences, DragAndDrop.paths, Event.current.type);
                        if (dopelineCurve.valueType == typeof(Sprite) && sprites.Count > 0)
                        {
                            curve = dopelineCurve;
                            targetType = typeof(Sprite);
                        }
                    }
                }
            }

            if (curve == null)
                return false;

            if (!curve.clipIsEditable)
                return false;

            if (perform)
            {
                if (DragAndDrop.objectReferences.Length == 1)
                    UsabilityAnalytics.Event("Sprite Drag and Drop", "Drop single sprite into existing dopeline", "null", 1);
                else
                    UsabilityAnalytics.Event("Sprite Drag and Drop", "Drop multiple sprites into existing dopeline", "null", 1);

                float time = Mathf.Max(state.PixelToTime(Event.current.mousePosition.x, AnimationWindowState.SnapMode.SnapToClipFrame), 0f);
                AnimationWindowCurve targetCurve = GetCurveOfType(dopeLine, targetType);
                PerformDragAndDrop(targetCurve, time);
            }

            return true;
        }

        private void PerformDragAndDrop(AnimationWindowCurve targetCurve, float time)
        {
            if (DragAndDrop.objectReferences.Length == 0 || targetCurve == null)
                return;

            string undoLabel = "Drop Key";
            state.SaveKeySelection(undoLabel);

            state.ClearSelections();
            Object[] objectReferences = GetSortedDragAndDropObjectReferences();

            foreach (var obj in objectReferences)
            {
                Object value = obj;

                if (value is Texture2D)
                    value = SpriteUtility.TextureToSprite(obj as Texture2D);

                CreateNewPPtrKeyframe(time, value, targetCurve);
                time += 1f / targetCurve.clip.frameRate;
            }

            state.SaveCurve(targetCurve, undoLabel);
            DragAndDrop.AcceptDrag();
        }

        private Object[] GetSortedDragAndDropObjectReferences()
        {
            Object[] objectReferences = DragAndDrop.objectReferences;

            // Use same name compare as when we sort in the backend: See AssetDatabase.cpp: SortChildren
            System.Array.Sort(objectReferences, (a, b) => EditorUtility.NaturalCompare(a.name, b.name));

            return objectReferences;
        }

        private void CreateNewPPtrKeyframe(float time, Object value, AnimationWindowCurve targetCurve)
        {
            ObjectReferenceKeyframe referenceKeyframe = new ObjectReferenceKeyframe();

            referenceKeyframe.time = time;
            referenceKeyframe.value = value;

            AnimationWindowKeyframe keyframe = new AnimationWindowKeyframe(targetCurve, referenceKeyframe);
            AnimationKeyTime newTime = AnimationKeyTime.Time(keyframe.time, state.frameRate);
            targetCurve.AddKeyframe(keyframe, newTime);
            state.SelectKey(keyframe);
        }

        // if targetType == null, it means that all types are fine (as long as they are all of the same type)
        private static bool ValidateDragAndDropObjects()
        {
            if (DragAndDrop.objectReferences.Length == 0)
                return false;

            // Let's be safe and early out if any of the objects are null or if they aren't all of the same type (exception beign sprite vs. texture2D, which are considered equal here)
            for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
            {
                Object obj = DragAndDrop.objectReferences[i];
                if (obj == null)
                {
                    return false;
                }

                if (i < DragAndDrop.objectReferences.Length - 1)
                {
                    Object nextObj = DragAndDrop.objectReferences[i + 1];
                    bool bothAreSpritesOrTextures = (obj is Texture2D || obj is Sprite) && (nextObj is Texture2D || nextObj is Sprite);

                    if (obj.GetType() != nextObj.GetType() && !bothAreSpritesOrTextures)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private AnimationWindowCurve GetCurveOfType(DopeLine dopeLine, System.Type type)
        {
            foreach (AnimationWindowCurve curve in dopeLine.curves)
            {
                if (curve.valueType == type)
                    return curve;
            }
            return null;
        }

        // For optimizing. Starting from keyIndex, we check through any key with same time and see if any are selected
        private bool AnyKeyIsSelectedAtTime(DopeLine dopeLine, int keyIndex)
        {
            AnimationWindowKeyframe keyframe = dopeLine.keys[keyIndex];
            int firstTimeHash = keyframe.m_TimeHash ^ keyframe.curve.timeOffset.GetHashCode();

            int length = dopeLine.keys.Count;
            for (int i = keyIndex; i < length; i++)
            {
                keyframe = dopeLine.keys[i];
                int timeHash = keyframe.m_TimeHash ^ keyframe.curve.timeOffset.GetHashCode();

                if (timeHash != firstTimeHash)
                    return false;

                if (state.KeyIsSelected(keyframe))
                    return true;
            }

            return false;
        }

        private struct AddKeyToDopelineContext
        {
            public DopeLine dopeline;
            public AnimationKeyTime time;
        }

        private void AddKeyToDopeline(object obj) { AddKeyToDopeline((AddKeyToDopelineContext)obj); }
        private void AddKeyToDopeline(AddKeyToDopelineContext context)
        {
            AnimationWindowUtility.AddKeyframes(state, context.dopeline.curves.ToArray(), context.time);
        }

        private void DeleteKeys(object obj) { DeleteKeys((List<AnimationWindowKeyframe>)obj); }
        private void DeleteKeys(List<AnimationWindowKeyframe> keys)
        {
            state.DeleteKeys(keys);
        }

        internal class DopeSheetSelectionRect
        {
            Vector2 m_SelectStartPoint;
            Vector2 m_SelectMousePoint;
            bool m_ValidRect;
            private DopeSheetEditor owner;

            enum SelectionType { Normal, Additive, Subtractive }
            public readonly GUIStyle createRect = "U2D.createRect";

            static int s_RectSelectionID = GUIUtility.GetPermanentControlID();

            public DopeSheetSelectionRect(DopeSheetEditor owner)
            {
                this.owner = owner;
            }

            public void OnGUI(Rect position)
            {
                Event evt = Event.current;
                Vector2 mousePos = evt.mousePosition;
                int id = s_RectSelectionID;
                switch (evt.GetTypeForControl(id))
                {
                    case EventType.MouseDown:
                        if (evt.button == 0 && position.Contains(mousePos))
                        {
                            GUIUtility.hotControl = id;
                            m_SelectStartPoint = mousePos;
                            m_ValidRect = false;
                            evt.Use();
                        }
                        break;
                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == id)
                        {
                            m_ValidRect = Mathf.Abs((mousePos - m_SelectStartPoint).x) > 1f;

                            if (m_ValidRect)
                                m_SelectMousePoint = new Vector2(mousePos.x, mousePos.y);

                            evt.Use();
                        }
                        break;

                    case EventType.Repaint:
                        if (GUIUtility.hotControl == id && m_ValidRect)
                            EditorStyles.selectionRect.Draw(GetCurrentPixelRect(), GUIContent.none, false, false, false, false);
                        break;

                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == id && evt.button == 0)
                        {
                            if (m_ValidRect)
                            {
                                if (!evt.shift && !EditorGUI.actionKey)
                                    owner.state.ClearSelections();

                                float frameRate = owner.state.frameRate;

                                Rect timeRect = GetCurrentTimeRect();
                                GUI.changed = true;

                                owner.state.ClearHierarchySelection();

                                List<AnimationWindowKeyframe> toBeUnselected = new List<AnimationWindowKeyframe>();
                                List<AnimationWindowKeyframe> toBeSelected = new List<AnimationWindowKeyframe>();

                                foreach (DopeLine dopeline in owner.state.dopelines)
                                {
                                    if (dopeline.position.yMin >= timeRect.yMin && dopeline.position.yMax <= timeRect.yMax)
                                    {
                                        foreach (AnimationWindowKeyframe keyframe in dopeline.keys)
                                        {
                                            AnimationKeyTime startTime = AnimationKeyTime.Time(timeRect.xMin - keyframe.curve.timeOffset, frameRate);
                                            AnimationKeyTime endTime = AnimationKeyTime.Time(timeRect.xMax - keyframe.curve.timeOffset, frameRate);

                                            AnimationKeyTime keyTime = AnimationKeyTime.Time(keyframe.time, frameRate);
                                            // for dopeline tallmode, we don't want to select the sprite at the end. It just feels wrong.
                                            if (!dopeline.tallMode && keyTime.frame >= startTime.frame && keyTime.frame <= endTime.frame ||
                                                dopeline.tallMode && keyTime.frame >= startTime.frame && keyTime.frame < endTime.frame)
                                            {
                                                if (!toBeSelected.Contains(keyframe) && !toBeUnselected.Contains(keyframe))
                                                {
                                                    if (!owner.state.KeyIsSelected(keyframe))
                                                        toBeSelected.Add(keyframe);
                                                    else if (owner.state.KeyIsSelected(keyframe))
                                                        toBeUnselected.Add(keyframe);
                                                }
                                            }
                                        }
                                    }
                                }

                                // Only if all the keys inside rect are selected, we want to unselect them.
                                if (toBeSelected.Count == 0)
                                    foreach (AnimationWindowKeyframe keyframe in toBeUnselected)
                                        owner.state.UnselectKey(keyframe);

                                foreach (AnimationWindowKeyframe keyframe in toBeSelected)
                                    owner.state.SelectKey(keyframe);

                                // Update hierarchy selection based on newly selected keys
                                foreach (DopeLine dopeline in owner.state.dopelines)
                                    if (owner.state.AnyKeyIsSelected(dopeline))
                                        owner.state.SelectHierarchyItem(dopeline, true, false);
                            }
                            else
                            {
                                owner.state.ClearSelections();
                            }
                            evt.Use();
                            GUIUtility.hotControl = 0;
                        }
                        break;
                }
            }

            public Rect GetCurrentPixelRect()
            {
                float height = AnimationWindowHierarchyGUI.k_DopeSheetRowHeight;
                Rect r = AnimationWindowUtility.FromToRect(m_SelectStartPoint, m_SelectMousePoint);
                r.xMin = owner.state.TimeToPixel(owner.state.PixelToTime(r.xMin, AnimationWindowState.SnapMode.SnapToClipFrame), AnimationWindowState.SnapMode.SnapToClipFrame);
                r.xMax = owner.state.TimeToPixel(owner.state.PixelToTime(r.xMax, AnimationWindowState.SnapMode.SnapToClipFrame), AnimationWindowState.SnapMode.SnapToClipFrame);
                r.yMin = Mathf.Floor(r.yMin / height) * height;
                r.yMax = (Mathf.Floor(r.yMax / height) + 1) * height;
                return r;
            }

            public Rect GetCurrentTimeRect()
            {
                float height = AnimationWindowHierarchyGUI.k_DopeSheetRowHeight;
                Rect r = AnimationWindowUtility.FromToRect(m_SelectStartPoint, m_SelectMousePoint);
                r.xMin = owner.state.PixelToTime(r.xMin, AnimationWindowState.SnapMode.SnapToClipFrame);
                r.xMax = owner.state.PixelToTime(r.xMax, AnimationWindowState.SnapMode.SnapToClipFrame);
                r.yMin = Mathf.Floor(r.yMin / height) * height;
                r.yMax = (Mathf.Floor(r.yMax / height) + 1) * height;
                return r;
            }
        }

        public void UpdateCurves(List<ChangedCurve> changedCurves, string undoText)
        {
            Undo.RegisterCompleteObjectUndo(state.activeAnimationClip, undoText);
            foreach (ChangedCurve changedCurve in changedCurves)
            {
                AnimationWindowCurve curve = state.allCurves.Find(c => changedCurve.curveId == c.GetHashCode());
                if (curve != null)
                {
                    AnimationUtility.SetEditorCurve(curve.clip, changedCurve.binding, changedCurve.curve);
                }
                else
                {
                    Debug.LogError("Could not match ChangedCurve data to destination curves.");
                }
            }
        }
    }
}
