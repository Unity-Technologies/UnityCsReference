// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Experimental.U2D;
using UnityEditorInternal;
using UnityEngine.U2D.Interface;
using UnityEditor.U2D.Interface;


namespace UnityEditor.U2D
{
    internal class SpriteOutlineModule : ISpriteEditorModule
    {
        class Styles
        {
            public GUIContent generateOutlineLabel = EditorGUIUtility.TextContent("Update|Update new outline based on mesh detail value.");
            public GUIContent outlineTolerance = EditorGUIUtility.TextContent("Outline Tolerance|Sets how tight the outline should be from the sprite.");
            public GUIContent snapButtonLabel = EditorGUIUtility.TextContent("Snap|Snap points to nearest pixel");
            public GUIContent generatingOutlineDialogTitle = EditorGUIUtility.TextContent("Outline");
            public GUIContent generatingOutlineDialogContent = EditorGUIUtility.TextContent("Generating outline {0}/{1}");
            public Color spriteBorderColor = new Color(0.25f, 0.5f, 1f, 0.75f);
        }

        protected SpriteRect m_Selected;

        private const float k_HandleSize = 5f;
        private readonly string k_DeleteCommandName = "Delete";
        private readonly string k_SoftDeleteCommandName = "SoftDelete";

        private ShapeEditor[] m_ShapeEditors;
        private bool m_RequestRepaint;
        private Matrix4x4 m_HandleMatrix;
        private Vector2 m_MousePosition;
        private bool m_Snap;
        private ShapeEditorRectSelectionTool m_ShapeSelectionUI;
        private bool m_WasRectSelecting = false;
        private Rect? m_SelectionRect;
        private ITexture2D m_OutlineTexture;
        private Styles m_Styles;

        public SpriteOutlineModule(ISpriteEditor sem, IEventSystem es, IUndoSystem us, IAssetDatabase ad, IGUIUtility gu, IShapeEditorFactory sef, ITexture2D outlineTexture)
        {
            spriteEditorWindow = sem;
            undoSystem = us;
            eventSystem = es;
            assetDatabase = ad;
            guiUtility = gu;
            shapeEditorFactory = sef;
            m_OutlineTexture = outlineTexture;

            m_ShapeSelectionUI = new ShapeEditorRectSelectionTool(gu);

            m_ShapeSelectionUI.RectSelect += RectSelect;
            m_ShapeSelectionUI.ClearSelection += ClearSelection;
        }

        public virtual string moduleName
        {
            get { return "Custom Outline"; }
        }

        private Styles styles
        {
            get
            {
                if (m_Styles == null)
                    m_Styles = new Styles();
                return m_Styles;
            }
        }

        protected virtual List<SpriteOutline> selectedShapeOutline
        {
            get { return m_Selected.outline; }
            set { m_Selected.outline = value; }
        }

        private bool shapeEditorDirty
        {
            get; set;
        }

        private bool editingDisabled
        {
            get { return spriteEditorWindow.editingDisabled; }
        }

        private ISpriteEditor spriteEditorWindow
        {
            get; set;
        }

        private IUndoSystem undoSystem
        {
            get; set;
        }

        private IEventSystem eventSystem
        {
            get; set;
        }

        private IAssetDatabase assetDatabase
        {
            get; set;
        }

        private IGUIUtility guiUtility
        {
            get; set;
        }

        private IShapeEditorFactory shapeEditorFactory
        {
            get; set;
        }

        private void RectSelect(Rect r, ShapeEditor.SelectionType selectionType)
        {
            var localRect = EditorGUIExt.FromToRect(ScreenToLocal(r.min), ScreenToLocal(r.max));
            m_SelectionRect = localRect;
        }

        private void ClearSelection()
        {
            m_RequestRepaint = true;
        }

        public void OnModuleActivate()
        {
            GenerateOutlineIfNotExist();
            undoSystem.RegisterUndoCallback(UndoRedoPerformed);
            shapeEditorDirty = true;
            SetupShapeEditor();
            spriteEditorWindow.enableMouseMoveEvent = true;
        }

        void GenerateOutlineIfNotExist()
        {
            var rectCache = spriteEditorWindow.spriteRects;
            if (rectCache != null)
            {
                bool needApply = false;
                for (int i = 0; i < rectCache.Count; ++i)
                {
                    var rect = rectCache.RectAt(i);
                    if (!HasShapeOutline(rect))
                    {
                        spriteEditorWindow.DisplayProgressBar(styles.generatingOutlineDialogTitle.text,
                            string.Format(styles.generatingOutlineDialogContent.text, i + 1 , rectCache.Count),
                            (float)(i) / rectCache.Count);

                        SetupShapeEditorOutline(rect);
                        needApply = true;
                    }
                }
                if (needApply)
                {
                    spriteEditorWindow.ClearProgressBar();
                    spriteEditorWindow.ApplyOrRevertModification(true);
                }
            }
        }

        public void OnModuleDeactivate()
        {
            undoSystem.UnregisterUndoCallback(UndoRedoPerformed);
            CleanupShapeEditors();
            m_Selected = null;
            spriteEditorWindow.enableMouseMoveEvent = false;
        }

        public void DoTextureGUI()
        {
            IEvent evt = eventSystem.current;

            m_RequestRepaint = false;
            m_HandleMatrix = Handles.matrix;

            m_MousePosition = Handles.inverseMatrix.MultiplyPoint(eventSystem.current.mousePosition);
            if (m_Selected == null || !m_Selected.rect.Contains(m_MousePosition) && !IsMouseOverOutlinePoints() && evt.shift == false)
                spriteEditorWindow.HandleSpriteSelection();

            HandleCreateNewOutline();

            m_WasRectSelecting = m_ShapeSelectionUI.isSelecting;

            UpdateShapeEditors();

            m_ShapeSelectionUI.OnGUI();

            DrawGizmos();

            if (m_RequestRepaint || evt.type == EventType.MouseMove)
                spriteEditorWindow.RequestRepaint();
        }

        public void DrawToolbarGUI(Rect drawArea)
        {
            var style = styles;

            Rect snapDrawArea = new Rect(drawArea.x, drawArea.y, EditorStyles.toolbarButton.CalcSize(style.snapButtonLabel).x, drawArea.height);
            m_Snap = GUI.Toggle(snapDrawArea, m_Snap, style.snapButtonLabel, EditorStyles.toolbarButton);

            using (new EditorGUI.DisabledScope(editingDisabled || m_Selected == null))
            {
                float totalWidth = drawArea.width - snapDrawArea.width;
                drawArea.x = snapDrawArea.xMax;
                drawArea.width = EditorStyles.toolbarButton.CalcSize(style.outlineTolerance).x;
                totalWidth -= drawArea.width;
                if (totalWidth < 0)
                    drawArea.width += totalWidth;
                if (drawArea.width > 0)
                    GUI.Label(drawArea, style.outlineTolerance, EditorStyles.miniLabel);
                drawArea.x += drawArea.width;

                drawArea.width = 100;
                totalWidth -= drawArea.width;
                if (totalWidth < 0)
                    drawArea.width += totalWidth;

                if (drawArea.width > 0)
                {
                    float tesselationValue = m_Selected != null ? m_Selected.tessellationDetail : 0;
                    EditorGUI.BeginChangeCheck();
                    float oldFieldWidth = EditorGUIUtility.fieldWidth;
                    float oldLabelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.fieldWidth = 30;
                    EditorGUIUtility.labelWidth = 1;
                    tesselationValue = EditorGUI.Slider(drawArea, Mathf.Clamp01(tesselationValue), 0, 1);
                    if (EditorGUI.EndChangeCheck())
                    {
                        RecordUndo();
                        m_Selected.tessellationDetail = tesselationValue;
                    }
                    EditorGUIUtility.fieldWidth = oldFieldWidth;
                    EditorGUIUtility.labelWidth = oldLabelWidth;
                }

                drawArea.x += drawArea.width;
                drawArea.width = EditorStyles.toolbarButton.CalcSize(style.generateOutlineLabel).x;
                totalWidth -= drawArea.width;
                if (totalWidth < 0)
                    drawArea.width += totalWidth;

                if (drawArea.width > 0 && GUI.Button(drawArea, style.generateOutlineLabel, EditorStyles.toolbarButton))
                {
                    RecordUndo();
                    selectedShapeOutline.Clear();
                    SetupShapeEditorOutline(m_Selected);
                    spriteEditorWindow.SetDataModified();
                    shapeEditorDirty = true;
                }
            }
        }

        public void OnPostGUI()
        {}

        public bool CanBeActivated()
        {
            return SpriteUtility.GetSpriteImportMode(spriteEditorWindow.spriteEditorDataProvider) != SpriteImportMode.None;
        }

        private void RecordUndo()
        {
            undoSystem.RegisterCompleteObjectUndo(spriteEditorWindow.spriteRects, "Outline changed");
        }

        public void CreateNewOutline(Rect rectOutline)
        {
            Rect rect = m_Selected.rect;
            if (rect.Contains(rectOutline.min) && rect.Contains(rectOutline.max))
            {
                RecordUndo();
                SpriteOutline so = new SpriteOutline();
                Vector2 outlineOffset = new Vector2(0.5f * rect.width + rect.x, 0.5f * rect.height + rect.y);
                Rect selectionRect = new Rect(rectOutline);
                selectionRect.min = SnapPoint(rectOutline.min);
                selectionRect.max = SnapPoint(rectOutline.max);
                so.Add(CapPointToRect(new Vector2(selectionRect.xMin, selectionRect.yMin), rect) - outlineOffset);
                so.Add(CapPointToRect(new Vector2(selectionRect.xMin, selectionRect.yMax), rect) - outlineOffset);
                so.Add(CapPointToRect(new Vector2(selectionRect.xMax, selectionRect.yMax), rect) - outlineOffset);
                so.Add(CapPointToRect(new Vector2(selectionRect.xMax, selectionRect.yMin), rect) - outlineOffset);
                selectedShapeOutline.Add(so);
                spriteEditorWindow.SetDataModified();
                shapeEditorDirty = true;
            }
        }

        private void HandleCreateNewOutline()
        {
            if (m_WasRectSelecting && m_ShapeSelectionUI.isSelecting == false && m_SelectionRect != null && m_Selected != null)
            {
                bool createNewOutline = true;
                foreach (var se in m_ShapeEditors)
                {
                    if (se.selectedPoints.Count != 0)
                    {
                        createNewOutline = false;
                        break;
                    }
                }

                if (createNewOutline)
                    CreateNewOutline(m_SelectionRect.Value);
            }
            m_SelectionRect = null;
        }

        public void UpdateShapeEditors()
        {
            SetupShapeEditor();

            if (m_Selected != null)
            {
                IEvent currentEvent = eventSystem.current;
                var wantsDelete = currentEvent.type == EventType.ExecuteCommand && (currentEvent.commandName == k_SoftDeleteCommandName || currentEvent.commandName == k_DeleteCommandName);

                for (int i = 0; i < m_ShapeEditors.Length; ++i)
                {
                    if (m_ShapeEditors[i].GetPointsCount() == 0)
                        continue;

                    m_ShapeEditors[i].inEditMode = true;
                    m_ShapeEditors[i].OnGUI();
                    if (shapeEditorDirty)
                        break;
                }

                if (wantsDelete)
                {
                    // remove outline which have lesser than 3 points
                    for (int i = selectedShapeOutline.Count - 1; i >= 0; --i)
                    {
                        if (selectedShapeOutline[i].Count < 3)
                        {
                            selectedShapeOutline.RemoveAt(i);
                            shapeEditorDirty = true;
                        }
                    }
                }
            }
        }

        private bool IsMouseOverOutlinePoints()
        {
            if (m_Selected == null)
                return false;
            Vector2 outlineOffset = new Vector2(0.5f * m_Selected.rect.width + m_Selected.rect.x, 0.5f * m_Selected.rect.height + m_Selected.rect.y);
            float handleSize = GetHandleSize();
            Rect r = new Rect(0, 0, handleSize * 2, handleSize * 2);
            for (int i = 0; i < selectedShapeOutline.Count; ++i)
            {
                var outline = selectedShapeOutline[i];
                for (int j = 0; j < outline.Count; ++j)
                {
                    r.center = outline[j] + outlineOffset;
                    if (r.Contains(m_MousePosition))
                        return true;
                }
            }
            return false;
        }

        private float GetHandleSize()
        {
            return k_HandleSize / m_HandleMatrix.m00;
        }

        private void CleanupShapeEditors()
        {
            if (m_ShapeEditors != null)
            {
                for (int i = 0; i < m_ShapeEditors.Length; ++i)
                {
                    for (int j = 0; j < m_ShapeEditors.Length; ++j)
                    {
                        if (i != j)
                            m_ShapeEditors[j].UnregisterFromShapeEditor(m_ShapeEditors[i]);
                    }
                    m_ShapeEditors[i].OnDisable();
                }
            }
            m_ShapeEditors = null;
        }

        public void SetupShapeEditor()
        {
            if (shapeEditorDirty || m_Selected != spriteEditorWindow.selectedSpriteRect)
            {
                m_Selected = spriteEditorWindow.selectedSpriteRect;
                CleanupShapeEditors();

                if (m_Selected != null)
                {
                    if (!HasShapeOutline(m_Selected))
                        SetupShapeEditorOutline(m_Selected);
                    m_ShapeEditors = new ShapeEditor[selectedShapeOutline.Count];

                    for (int i = 0; i < selectedShapeOutline.Count; ++i)
                    {
                        int outlineIndex = i;
                        m_ShapeEditors[i] = shapeEditorFactory.CreateShapeEditor();
                        m_ShapeEditors[i].SetRectSelectionTool(m_ShapeSelectionUI);
                        m_ShapeEditors[i].LocalToWorldMatrix = () => m_HandleMatrix;
                        m_ShapeEditors[i].LocalToScreen = (point) => Handles.matrix.MultiplyPoint(point);
                        m_ShapeEditors[i].ScreenToLocal = ScreenToLocal;
                        m_ShapeEditors[i].RecordUndo = RecordUndo;
                        m_ShapeEditors[i].GetHandleSize = GetHandleSize;
                        m_ShapeEditors[i].lineTexture = m_OutlineTexture;
                        m_ShapeEditors[i].Snap = SnapPoint;
                        m_ShapeEditors[i].GetPointPosition = (index) => GetPointPosition(outlineIndex, index);
                        m_ShapeEditors[i].SetPointPosition = (index, position) => SetPointPosition(outlineIndex, index, position);
                        m_ShapeEditors[i].InsertPointAt = (index, position) => InsertPointAt(outlineIndex, index, position);
                        m_ShapeEditors[i].RemovePointAt = (index) => RemovePointAt(outlineIndex, index);
                        m_ShapeEditors[i].GetPointsCount = () => GetPointsCount(outlineIndex);
                    }
                    for (int i = 0; i < selectedShapeOutline.Count; ++i)
                    {
                        for (int j = 0; j < selectedShapeOutline.Count; ++j)
                        {
                            if (i != j)
                                m_ShapeEditors[j].RegisterToShapeEditor(m_ShapeEditors[i]);
                        }
                    }
                }
                else
                {
                    m_ShapeEditors = new ShapeEditor[0];
                }
            }
            shapeEditorDirty = false;
        }

        protected virtual bool HasShapeOutline(SpriteRect spriteRect)
        {
            return (spriteRect.outline != null);
        }

        protected virtual void SetupShapeEditorOutline(SpriteRect spriteRect)
        {
            spriteRect.outline = GenerateSpriteRectOutline(spriteRect.rect, spriteEditorWindow.selectedTexture, spriteRect.tessellationDetail, 0, spriteEditorWindow.spriteEditorDataProvider);
            if (spriteRect.outline.Count == 0)
            {
                Vector2 halfSize = spriteRect.rect.size * 0.5f;
                spriteRect.outline = new List<SpriteOutline>()
                {
                    new SpriteOutline()
                    {
                        m_Path = new List<Vector2>()
                        {
                            new Vector2(-halfSize.x, -halfSize.y),
                            new Vector2(-halfSize.x, halfSize.y),
                            new Vector2(halfSize.x, halfSize.y),
                            new Vector2(halfSize.x, -halfSize.y),
                        }
                    }
                };
            }
        }

        public Vector3 SnapPoint(Vector3 position)
        {
            if (m_Snap)
            {
                position.x = Mathf.RoundToInt(position.x);
                position.y = Mathf.RoundToInt(position.y);
            }
            return position;
        }

        public Vector3 GetPointPosition(int outlineIndex, int pointIndex)
        {
            if (outlineIndex >= 0 && outlineIndex < selectedShapeOutline.Count)
            {
                var outline = selectedShapeOutline[outlineIndex];
                if (pointIndex >= 0 && pointIndex < outline.Count)
                {
                    return ConvertSpriteRectSpaceToTextureSpace(outline[pointIndex]);
                }
            }
            return new Vector3(float.NaN, float.NaN, float.NaN);
        }

        public void SetPointPosition(int outlineIndex, int pointIndex, Vector3 position)
        {
            selectedShapeOutline[outlineIndex][pointIndex] = ConvertTextureSpaceToSpriteRectSpace(CapPointToRect(position, m_Selected.rect));
            spriteEditorWindow.SetDataModified();
        }

        public void InsertPointAt(int outlineIndex, int pointIndex, Vector3 position)
        {
            selectedShapeOutline[outlineIndex].Insert(pointIndex, ConvertTextureSpaceToSpriteRectSpace(CapPointToRect(position, m_Selected.rect)));
            spriteEditorWindow.SetDataModified();
        }

        public void RemovePointAt(int outlineIndex, int i)
        {
            selectedShapeOutline[outlineIndex].RemoveAt(i);
            spriteEditorWindow.SetDataModified();
        }

        public int GetPointsCount(int outlineIndex)
        {
            return selectedShapeOutline[outlineIndex].Count;
        }

        private Vector2 ConvertSpriteRectSpaceToTextureSpace(Vector2 value)
        {
            Vector2 outlineOffset = new Vector2(0.5f * m_Selected.rect.width + m_Selected.rect.x, 0.5f * m_Selected.rect.height + m_Selected.rect.y);
            value += outlineOffset;
            return value;
        }

        private Vector2 ConvertTextureSpaceToSpriteRectSpace(Vector2 value)
        {
            Vector2 outlineOffset = new Vector2(0.5f * m_Selected.rect.width + m_Selected.rect.x, 0.5f * m_Selected.rect.height + m_Selected.rect.y);
            value -= outlineOffset;
            return value;
        }

        private Vector3 ScreenToLocal(Vector2 point)
        {
            return Handles.inverseMatrix.MultiplyPoint(point);
        }

        private void UndoRedoPerformed()
        {
            shapeEditorDirty = true;
        }

        private void DrawGizmos()
        {
            if (eventSystem.current.type == EventType.Repaint)
            {
                var selected = spriteEditorWindow.selectedSpriteRect;
                if (selected != null)
                {
                    SpriteEditorUtility.BeginLines(styles.spriteBorderColor);
                    SpriteEditorUtility.DrawBox(selected.rect);
                    SpriteEditorUtility.EndLines();
                }
            }
        }

        protected static List<SpriteOutline> GenerateSpriteRectOutline(Rect rect, ITexture2D texture, float detail, byte alphaTolerance, ISpriteEditorDataProvider spriteEditorDataProvider)
        {
            List<SpriteOutline> outline = new List<SpriteOutline>();
            if (texture != null)
            {
                Vector2[][] paths;

                // we might have a texture that is capped because of max size or NPOT.
                // in that case, we need to convert values from capped space to actual texture space and back.
                int actualWidth = 0, actualHeight = 0;
                int cappedWidth, cappedHeight;
                spriteEditorDataProvider.GetTextureActualWidthAndHeight(out actualWidth, out actualHeight);
                cappedWidth = texture.width;
                cappedHeight = texture.height;

                Vector2 scale = new Vector2(cappedWidth / (float)actualWidth, cappedHeight / (float)actualHeight);
                Rect spriteRect = rect;
                spriteRect.xMin *= scale.x;
                spriteRect.xMax *= scale.x;
                spriteRect.yMin *= scale.y;
                spriteRect.yMax *= scale.y;

                Sprites.SpriteUtility.GenerateOutline(texture, spriteRect, detail, alphaTolerance, true, out paths);

                Rect capRect = new Rect();
                capRect.size = rect.size;
                capRect.center = Vector2.zero;
                for (int j = 0; j < paths.Length; ++j)
                {
                    SpriteOutline points = new SpriteOutline();
                    foreach (Vector2 v in paths[j])
                        points.Add(CapPointToRect(new Vector2(v.x / scale.x, v.y / scale.y), capRect));

                    outline.Add(points);
                }
            }
            return outline;
        }

        private static Vector2 CapPointToRect(Vector2 so, Rect r)
        {
            so.x = Mathf.Min(r.xMax, so.x);
            so.x = Mathf.Max(r.xMin, so.x);
            so.y = Mathf.Min(r.yMax, so.y);
            so.y = Mathf.Max(r.yMin, so.y);
            return so;
        }
    }
}
