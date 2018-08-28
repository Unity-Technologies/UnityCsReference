// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor
{
    [CustomEditor(typeof(LineRenderer))]
    [CanEditMultipleObjects]
    internal class LineRendererInspector : RendererEditorBase
    {
        private class Styles
        {
            public readonly GUIContent alignment = EditorGUIUtility.TrTextContent("Alignment", "Lines can rotate to face their transform component or the camera. When using Local mode, lines face the XY plane of the Transform.");
            public readonly GUIContent colorGradient = EditorGUIUtility.TrTextContent("Color", "The gradient describing the color along the line.");
            public readonly string disabledEditMessage = L10n.Tr("Editing is only available when editing a single LineRenderer");
            public readonly GUIContent inputMode = EditorGUIUtility.TrTextContent("Input", "Use mouse position or physics raycast to determine where to create points.");
            public readonly GUIContent layerMask = EditorGUIUtility.TrTextContent("Layer Mask", "The layer mask to use when performing raycasts.");
            public readonly GUIContent normalOffset = EditorGUIUtility.TrTextContent("Offset", "The offset applied to created points either from the scene camera or raycast normal, when using physics.");
            public readonly GUIContent numCapVertices = EditorGUIUtility.TrTextContent("End Cap Vertices", "How many vertices to add at each end.");
            public readonly GUIContent numCornerVertices = EditorGUIUtility.TrTextContent("Corner Vertices", "How many vertices to add for each corner.");
            public readonly GUIContent pointSeparation = EditorGUIUtility.TrTextContent("Min Vertex Distance", "When dragging the mouse a new point will be created after the distance has been exceeded.");
            public readonly GUIContent positions = EditorGUIUtility.TrTextContent("Positions");
            public readonly GUIContent propertyMenuContent = EditorGUIUtility.TrTextContent("Delete Selected Array Elements");
            public readonly GUIContent showWireframe = EditorGUIUtility.TrTextContent("Show Wireframe", "Show the wireframe visualizing the line.");
            public readonly GUIContent simplify = EditorGUIUtility.TrTextContent("Simplify", "Generates a simplified version of the original line by removing points that fall within the specified tolerance.");
            public readonly GUIContent simplifyPreview = EditorGUIUtility.TrTextContent("Simplify Preview", "Show a preview of the simplified version of the line.");
            public readonly GUIContent subdivide = EditorGUIUtility.TrTextContent("Subdivide Selected" , "Inserts a new point in between selected adjacent points.");
            public readonly GUIContent textureMode = EditorGUIUtility.TrTextContent("Texture Mode", "Should the U coordinate be stretched or tiled?");
            public readonly GUIContent tolerance = EditorGUIUtility.TrTextContent("Tolerance", "Used to evaluate which points should be removed from the line. A higher value results in a simpler line (fewer points). A value of 0 results in the exact same line with little to no reduction.");
            public readonly GUIStyle richTextMiniLabel = new GUIStyle(EditorStyles.miniLabel) { richText = true };
            public readonly GUIContent shadowBias = EditorGUIUtility.TrTextContent("Shadow Bias", "Apply a shadow bias to prevent self-shadowing artifacts. The specified value is the proportion of the line width at each segment.");
            public readonly GUIContent generateLightingData = EditorGUIUtility.TrTextContent("Generate Lighting Data", "Toggle generation of normal and tangent data, for use in lit shaders.");
            public readonly GUIContent[] toolContents =
            {
                EditorGUIUtility.IconContent("EditCollider", "|Edit Points in Scene View"),
                EditorGUIUtility.IconContent("Toolbar Plus", "|Create Points in Scene View.")
            };

            public readonly EditMode.SceneViewEditMode[] sceneViewEditModes = new[]
            {
                EditMode.SceneViewEditMode.LineRendererEdit,
                EditMode.SceneViewEditMode.LineRendererCreate
            };

            public const string baseSceneEditingToolText = "<color=grey>Line Renderer Scene Editing Mode:</color> ";
            public readonly GUIContent[] toolNames =
            {
                new GUIContent(L10n.Tr(baseSceneEditingToolText + "Edit Points"), ""),
                new GUIContent(L10n.Tr(baseSceneEditingToolText + "Create Points"), "")
            };
        }
        static Styles s_Styles;

        private string[] m_ExcludedProperties;
        private bool m_EditingPositions;

        public static float simplifyTolerance
        {
            get { return EditorPrefs.GetFloat("LineRendererInspectorSimplifyTolerance", 1.0f); }
            set { EditorPrefs.SetFloat("LineRendererInspectorSimplifyTolerance", value < 0 ? 0 : value); }
        }

        public static bool showSimplifyPreview
        {
            get { return EditorPrefs.GetBool("LineRendererEditorShowSimplifyPreview", false); }
            set { EditorPrefs.SetBool("LineRendererEditorShowSimplifyPreview", value); }
        }

        private Vector3[] m_PreviewPoints;

        private LineRendererCurveEditor m_CurveEditor = new LineRendererCurveEditor();
        private LineRendererEditor m_PointEditor;
        private SerializedProperty m_Alignment;
        private SerializedProperty m_ColorGradient;
        private SerializedProperty m_ShadowBias;
        private SerializedProperty m_GenerateLightingData;
        private SerializedProperty m_Loop;
        private SerializedProperty m_NumCapVertices;
        private SerializedProperty m_NumCornerVertices;
        private SerializedProperty m_Positions;
        private SerializedProperty m_PositionsSize;
        private SerializedProperty m_TextureMode;

        private LineRendererPositionsView m_PositionsView;

        AnimBool m_ShowPositionsAnimation;

        bool m_IsMultiEditing;

        public static readonly float kPositionsViewMinHeight = 30;

        private static bool IsLineRendererEditMode(EditMode.SceneViewEditMode editMode)
        {
            return editMode == EditMode.SceneViewEditMode.LineRendererEdit || editMode == EditMode.SceneViewEditMode.LineRendererCreate;
        }

        private bool sceneViewEditing
        {
            get { return IsLineRendererEditMode(EditMode.editMode) && EditMode.IsOwner(this); }
        }

        public override void OnEnable()
        {
            base.OnEnable();

            m_PointEditor = new LineRendererEditor(target as LineRenderer, this);
            m_PointEditor.Deselect();
            SceneView.onSceneGUIDelegate += OnSceneGUIDelegate;
            Undo.undoRedoPerformed += UndoRedoPerformed;
            EditMode.onEditModeStartDelegate += EditModeStarted;
            EditMode.onEditModeEndDelegate += EditModeEnded;

            List<string> excludedProperties = new List<string>();
            excludedProperties.Add("m_Loop");
            excludedProperties.Add("m_Parameters");
            excludedProperties.Add("m_Positions");
            excludedProperties.AddRange(Probes.GetFieldsStringArray());
            if (!SupportedRenderingFeatures.active.rendererSupportsMotionVectors)
                excludedProperties.Add("m_MotionVectors");
            if (!SupportedRenderingFeatures.active.rendererSupportsReceiveShadows)
                excludedProperties.Add("m_ReceiveShadows");
            excludedProperties.Add("m_RenderingLayerMask");
            m_ExcludedProperties = excludedProperties.ToArray();

            m_CurveEditor.OnEnable(serializedObject);
            m_Loop = serializedObject.FindProperty("m_Loop");
            m_Positions = serializedObject.FindProperty("m_Positions");
            m_PositionsSize = serializedObject.FindProperty("m_Positions.Array.size");
            m_ColorGradient = serializedObject.FindProperty("m_Parameters.colorGradient");
            m_NumCornerVertices = serializedObject.FindProperty("m_Parameters.numCornerVertices");
            m_NumCapVertices = serializedObject.FindProperty("m_Parameters.numCapVertices");
            m_Alignment = serializedObject.FindProperty("m_Parameters.alignment");
            m_TextureMode = serializedObject.FindProperty("m_Parameters.textureMode");
            m_GenerateLightingData = serializedObject.FindProperty("m_Parameters.generateLightingData");
            m_ShadowBias = serializedObject.FindProperty("m_Parameters.shadowBias");

            m_PositionsView = new LineRendererPositionsView(m_Positions);
            m_PositionsView.selectionChangedCallback += PositionsViewSelectionChanged;

            m_ShowPositionsAnimation = new AnimBool(false, Repaint) { value = m_Positions.isExpanded };
            EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;

            // We cannot access isEditingMultipleObjects when drawing the SceneView so we need to cache it here for later use.
            m_IsMultiEditing = serializedObject.isEditingMultipleObjects;

            InitializeProbeFields();
        }

        void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
        {
            if (m_PositionsView == null)
                return;

            if (property.propertyPath.Contains("m_Positions") && m_PositionsView.GetSelection().Count > 1)
            {
                menu.AddItem(s_Styles.propertyMenuContent, false, () =>
                {
                    var selection = m_PositionsView.GetSelection().ToList();
                    var query = selection.OrderByDescending(c => c);

                    foreach (var index in query)
                    {
                        m_Positions.DeleteArrayElementAtIndex(index);
                    }
                    m_Positions.serializedObject.ApplyModifiedProperties();
                    m_PositionsView.SetSelection(new int[0]);
                    m_PositionsView.Reload();
                    ResetSimplifyPreview();
                });
            }
        }

        void PositionsViewSelectionChanged(List<int> selected)
        {
            m_PointEditor.m_Selection = selected;
        }

        public void OnDisable()
        {
            m_CurveEditor.OnDisable();
            EndEditPositions();
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            SceneView.onSceneGUIDelegate -= OnSceneGUIDelegate;
            EditorApplication.contextualPropertyMenu -= OnPropertyContextMenu;
        }

        private void UndoRedoPerformed()
        {
            m_PointEditor.RemoveInvalidSelections();
            m_PositionsView.Reload();
            m_PositionsView.SetSelection(m_PointEditor.m_Selection);
            ResetSimplifyPreview();
        }

        private void EditModeEnded(Editor editor)
        {
            if (editor == this)
            {
                EndEditPositions();
            }
        }

        private void EditModeStarted(Editor editor, EditMode.SceneViewEditMode mode)
        {
            if (editor == this && IsLineRendererEditMode(mode))
            {
                StartEditPositions();
            }
        }

        private void DrawEditPointTools()
        {
            LineRendererEditor.showWireframe = GUILayout.Toggle(LineRendererEditor.showWireframe, s_Styles.showWireframe);

            bool adjacentPointsSelected = HasAdjacentPointsSelected();
            using (new EditorGUI.DisabledGroupScope(!adjacentPointsSelected))
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(s_Styles.subdivide, GUILayout.Width(150)))
                {
                    SubdivideSelected();
                }
                GUILayout.EndHorizontal();
            }
        }

        private static void CreatePointTools()
        {
            LineRendererEditor.inputMode = (LineRendererEditor.InputMode)EditorGUILayout.EnumPopup(s_Styles.inputMode, LineRendererEditor.inputMode);
            if (LineRendererEditor.inputMode == LineRendererEditor.InputMode.PhysicsRaycast)
            {
                LineRendererEditor.raycastMask = EditorGUILayout.LayerMaskField(LineRendererEditor.raycastMask, s_Styles.layerMask);
            }

            LineRendererEditor.createPointSeparation = EditorGUILayout.FloatField(s_Styles.pointSeparation, LineRendererEditor.createPointSeparation);
            LineRendererEditor.creationOffset = EditorGUILayout.FloatField(s_Styles.normalOffset, LineRendererEditor.creationOffset);
        }

        Bounds GetBounds()
        {
            var lineRenderer = (target as LineRenderer);
            return m_Positions.arraySize > 0 ? lineRenderer.bounds : new Bounds(lineRenderer.useWorldSpace ? lineRenderer.transform.position : Vector3.zero, Vector3.zero);
        }

        private void DrawToolbar()
        {
            if (m_IsMultiEditing)
            {
                EditorGUILayout.HelpBox(s_Styles.disabledEditMessage, MessageType.Info);
            }

            EditorGUI.BeginDisabled(m_IsMultiEditing);
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditMode.DoInspectorToolbar(s_Styles.sceneViewEditModes, s_Styles.toolContents, GetBounds, this);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Tools box
            GUILayout.BeginVertical(EditorStyles.helpBox);
            string helpText = Styles.baseSceneEditingToolText;
            if (sceneViewEditing)
            {
                int index = ArrayUtility.IndexOf(s_Styles.sceneViewEditModes, EditMode.editMode);
                if (index >= 0)
                    helpText = s_Styles.toolNames[index].text;
            }

            GUILayout.Label(helpText, s_Styles.richTextMiniLabel);
            GUILayout.EndVertical();

            // Editing mode toolbar
            if (sceneViewEditing)
            {
                switch (EditMode.editMode)
                {
                    case EditMode.SceneViewEditMode.LineRendererEdit:
                        DrawEditPointTools();
                        break;
                    case EditMode.SceneViewEditMode.LineRendererCreate:
                        CreatePointTools();
                        break;
                }
            }
            if (!sceneViewEditing)
            {
                EditorGUI.BeginChangeCheck();
                showSimplifyPreview = EditorGUILayout.Toggle(s_Styles.simplifyPreview, showSimplifyPreview);
                EditorGUILayout.BeginHorizontal();
                simplifyTolerance = Mathf.Max(0, EditorGUILayout.FloatField(s_Styles.tolerance, simplifyTolerance));
                if (GUILayout.Button(s_Styles.simplify, EditorStyles.miniButton))
                {
                    SimplifyPoints();
                }
                if (EditorGUI.EndChangeCheck())
                {
                    ResetSimplifyPreview();
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
            EditorGUILayout.Space();
            EditorGUI.EndDisabled();
        }

        private bool HasAdjacentPointsSelected()
        {
            var selection = m_PointEditor.m_Selection;
            selection.Sort();
            if (selection.Count < 2)
                return false;

            for (int i = 0; i < selection.Count - 1; ++i)
            {
                if (selection[i + 1] == selection[i] + 1)
                    return true;
            }

            return false;
        }

        private void SubdivideSelected()
        {
            var selection = m_PointEditor.m_Selection;
            if (selection.Count < 2)
                return;
            selection.Sort();
            var insertedIndexes = new List<int>();
            int numInserted = 0; // As we insert new nodes, the selected indexes will become offset so we need to keep track of this.
            for (int i = 0; i < selection.Count - 1; ++i)
            {
                if (selection[i + 1] == selection[i] + 1)
                {
                    int fromIndex = selection[i] + numInserted;
                    int toIndex = selection[i + 1] + numInserted;
                    var from = m_Positions.GetArrayElementAtIndex(fromIndex).vector3Value;
                    var to = m_Positions.GetArrayElementAtIndex(toIndex).vector3Value;
                    var midPoint = Vector3.Lerp(from, to, 0.5f);
                    m_Positions.InsertArrayElementAtIndex(toIndex);
                    m_Positions.GetArrayElementAtIndex(toIndex).vector3Value = midPoint;
                    insertedIndexes.Add(toIndex);
                    numInserted++;
                }
            }

            m_PointEditor.m_Selection = insertedIndexes;
        }

        private void SimplifyPoints()
        {
            var lineRenderer = target as LineRenderer;
            Undo.RecordObject(lineRenderer, "Simplify Line");
            lineRenderer.Simplify(simplifyTolerance);
        }

        private void ResetSimplifyPreview()
        {
            m_PreviewPoints = null;
        }

        private void DrawSimplifyPreview()
        {
            var lineRenderer = target as LineRenderer;

            if (!showSimplifyPreview || m_IsMultiEditing || !lineRenderer.enabled)
                return;

            if (m_PreviewPoints == null && lineRenderer.positionCount > 2)
            {
                m_PreviewPoints = new Vector3[lineRenderer.positionCount];
                lineRenderer.GetPositions(m_PreviewPoints);
                var simplePoints = new List<Vector3>();
                LineUtility.Simplify(m_PreviewPoints.ToList(), simplifyTolerance, simplePoints);
                if (lineRenderer.loop)
                    simplePoints.Add(simplePoints[0]);
                m_PreviewPoints = simplePoints.ToArray();
            }

            if (m_PreviewPoints != null)
            {
                Handles.color = Color.yellow;
                var oldMatrix = Handles.matrix;
                if (!lineRenderer.useWorldSpace)
                    Handles.matrix = lineRenderer.transform.localToWorldMatrix;
                Handles.DrawAAPolyLine(10, m_PreviewPoints.Length, m_PreviewPoints);
                Handles.matrix = oldMatrix;
            }
        }

        public override void OnInspectorGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            serializedObject.Update();
            DrawPropertiesExcluding(m_SerializedObject, m_ExcludedProperties);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_Loop);
            if (EditorGUI.EndChangeCheck())
                ResetSimplifyPreview();

            DrawToolbar();

            m_ShowPositionsAnimation.target = m_Positions.isExpanded = EditorGUILayout.Foldout(m_Positions.isExpanded, s_Styles.positions, true);
            if (m_ShowPositionsAnimation.faded > 0)
            {
                EditorGUILayout.PropertyField(m_PositionsSize);
                if (m_Positions.arraySize != m_PositionsView.GetRows().Count)
                {
                    m_PositionsView.Reload();
                    ResetSimplifyPreview();
                }

                m_PositionsView.OnGUI(EditorGUILayout.GetControlRect(false, Mathf.Lerp(kPositionsViewMinHeight, m_PositionsView.totalHeight, m_ShowPositionsAnimation.faded)));
                if (serializedObject.hasModifiedProperties)
                    ResetSimplifyPreview();
            }

            EditorGUILayout.Space();
            m_CurveEditor.CheckCurveChangedExternally();
            m_CurveEditor.OnInspectorGUI();
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_ColorGradient, s_Styles.colorGradient);
            EditorGUILayout.PropertyField(m_NumCornerVertices, s_Styles.numCornerVertices);
            EditorGUILayout.PropertyField(m_NumCapVertices, s_Styles.numCapVertices);
            EditorGUILayout.PropertyField(m_Alignment, s_Styles.alignment);
            EditorGUILayout.PropertyField(m_TextureMode, s_Styles.textureMode);
            EditorGUILayout.PropertyField(m_ShadowBias, s_Styles.shadowBias);
            EditorGUILayout.PropertyField(m_GenerateLightingData, s_Styles.generateLightingData);

            EditorGUILayout.Space();

            RenderSortingLayerFields();

            m_Probes.OnGUI(targets, (Renderer)target, false);

            RenderRenderingLayer();

            serializedObject.ApplyModifiedProperties();
        }

        public void StartEditPositions()
        {
            if (m_EditingPositions)
                return;

            m_EditingPositions = true;
            Tools.s_Hidden = true;
            SceneView.RepaintAll();
        }

        public void EndEditPositions()
        {
            if (!m_EditingPositions)
                return;

            if (m_PointEditor != null)
                m_PointEditor.Deselect();

            ResetSimplifyPreview();
            Tools.s_Hidden = false;
            SceneView.RepaintAll();
        }

        private void InternalOnSceneView()
        {
            if (!m_EditingPositions)
                return;

            switch (EditMode.editMode)
            {
                case EditMode.SceneViewEditMode.LineRendererEdit:
                    m_PointEditor.EditSceneGUI();
                    if (m_Positions.arraySize != m_PositionsView.GetRows().Count)
                    {
                        m_PositionsView.Reload();
                        ResetSimplifyPreview();
                    }
                    m_PositionsView.SetSelection(m_PointEditor.m_Selection, TreeViewSelectionOptions.RevealAndFrame);
                    break;
                case EditMode.SceneViewEditMode.LineRendererCreate:
                    m_PointEditor.CreateSceneGUI();
                    break;
            }
        }

        public void OnSceneGUI()
        {
            if (Event.current.type != EventType.Repaint)
                InternalOnSceneView();

            if (!sceneViewEditing)
                DrawSimplifyPreview();
        }

        public void OnSceneGUIDelegate(SceneView sceneView)
        {
            if (Event.current.type == EventType.Repaint)
                InternalOnSceneView();
        }

        public bool HasFrameBounds()
        {
            return m_EditingPositions && m_PointEditor.m_Selection.Count > 0;
        }

        public Bounds OnGetFrameBounds()
        {
            return m_PointEditor.selectedPositionsBounds;
        }
    }
}
