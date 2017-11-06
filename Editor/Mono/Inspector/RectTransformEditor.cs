// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using UnityEditorInternal;
using System.Linq;
using UnityEditor.AnimatedValues;

namespace UnityEditor
{
    [CustomEditor(typeof(RectTransform))]
    [CanEditMultipleObjects]
    internal class RectTransformEditor : Editor
    {
        // Consts
        // (Some are technically statics, but still have k prefix because they're not meant to be changed.)

        private const string kShowAnchorPropsPrefName = "RectTransformEditor.showAnchorProperties";
        private const string kLockRectPrefName = "RectTransformEditor.lockRect";

        private static Vector2 kShadowOffset = new Vector2(1, -1);
        private static Color kShadowColor = new Color(0, 0, 0, 0.5f);
        private const float kDottedLineSize = 5f;
        private static float kDropdownSize = 49;
        private static Color kRectInParentSpaceColor = new Color(1, 1, 1, 0.4f);
        private static Color kParentColor = new Color(1, 1, 1, 0.6f);
        private static Color kSiblingColor = new Color(1, 1, 1, 0.2f);
        private static Color kAnchorColor = new Color(1, 1, 1, 1);
        private static Color kAnchorLineColor = new Color(1, 1, 1, 0.6f);
        private static Vector3[] s_Corners = new Vector3[4];

        // Statics

        class Styles
        {
            public GUIStyle measuringLabelStyle = new GUIStyle("PreOverlayLabel");

            public GUIContent anchorsContent = new GUIContent("Anchors");
            public GUIContent anchorMinContent = new GUIContent("Min", "The normalized position in the parent rectangle that the lower left corner is anchored to.");
            public GUIContent anchorMaxContent = new GUIContent("Max", "The normalized position in the parent rectangle that the upper right corner is anchored to.");
            public GUIContent pivotContent = new GUIContent("Pivot", "The pivot point specified in normalized values between 0 and 1. The pivot point is the origin of this rectangle. Rotation and scaling is around this point.");
            public GUIContent transformScaleContent = new GUIContent("Scale", "The local scaling of this Game Object relative to the parent. This scales everything including image borders and text.");
            public GUIContent rawEditContent;
            public GUIContent blueprintContent;

            public Styles()
            {
                rawEditContent = EditorGUIUtility.IconContent(@"RectTransformRaw", "|Raw edit mode. When enabled, editing pivot and anchor values will not counter-adjust the position and size of the rectangle in order to make it stay in place.");
                blueprintContent = EditorGUIUtility.IconContent(@"RectTransformBlueprint", "|Blueprint mode. Edit RectTransforms as if they were not rotated and scaled. This enables snapping too.");
            }
        }
        static Styles s_Styles;
        static Styles styles { get { if (s_Styles == null) { s_Styles = new Styles(); } return s_Styles; } }

        private static int s_FoldoutHash = "Foldout".GetHashCode();
        private static int s_FloatFieldHash = "EditorTextField".GetHashCode();
        private static int s_ParentRectPreviewHandlesHash = "ParentRectPreviewDragHandles".GetHashCode();
        private static GUIContent[] s_XYLabels = {new GUIContent("X"), new GUIContent("Y")};
        private static GUIContent[] s_XYZLabels = {new GUIContent("X"), new GUIContent("Y"), new GUIContent("Z")};
        private static bool[] s_ScaleDisabledMask = new bool[3];

        private static bool s_DragAnchorsTogether;
        private static Vector2 s_StartDragAnchorMin;
        private static Vector2 s_StartDragAnchorMax;

        enum AnchorFusedState { None, All, Horizontal, Vertical }
        private static AnchorFusedState s_AnchorFusedState = AnchorFusedState.None;

        // Instance members

        private SerializedProperty m_AnchorMin;
        private SerializedProperty m_AnchorMax;
        private SerializedProperty m_AnchoredPosition;
        private SerializedProperty m_SizeDelta;
        private SerializedProperty m_Pivot;

        private SerializedProperty m_LocalPositionZ;
        private SerializedProperty m_LocalScale;
        private TransformRotationGUI m_RotationGUI;

        private bool m_ShowLayoutOptions = false;
        private bool m_RawEditMode = false;
        private int m_TargetCount = 0;

        private Dictionary<int, AnimBool> m_KeyboardControlIDs = new Dictionary<int, AnimBool>();
        private AnimatedValues.AnimBool m_ChangingAnchors = new AnimatedValues.AnimBool();
        private AnimatedValues.AnimBool m_ChangingPivot = new AnimatedValues.AnimBool();
        private AnimatedValues.AnimBool m_ChangingWidth = new AnimatedValues.AnimBool();
        private AnimatedValues.AnimBool m_ChangingHeight = new AnimatedValues.AnimBool();
        private AnimatedValues.AnimBool m_ChangingPosX = new AnimatedValues.AnimBool();
        private AnimatedValues.AnimBool m_ChangingPosY = new AnimatedValues.AnimBool();
        private AnimatedValues.AnimBool m_ChangingLeft = new AnimatedValues.AnimBool();
        private AnimatedValues.AnimBool m_ChangingRight = new AnimatedValues.AnimBool();
        private AnimatedValues.AnimBool m_ChangingTop = new AnimatedValues.AnimBool();
        private AnimatedValues.AnimBool m_ChangingBottom = new AnimatedValues.AnimBool();

        private delegate float FloatGetter(RectTransform rect);
        private delegate void FloatSetter(RectTransform rect, float f);

        void OnEnable()
        {
            m_AnchorMin = serializedObject.FindProperty("m_AnchorMin");
            m_AnchorMax = serializedObject.FindProperty("m_AnchorMax");
            m_AnchoredPosition = serializedObject.FindProperty("m_AnchoredPosition");
            m_SizeDelta = serializedObject.FindProperty("m_SizeDelta");
            m_Pivot = serializedObject.FindProperty("m_Pivot");

            m_TargetCount = targets.Length;
            m_LocalPositionZ = serializedObject.FindProperty("m_LocalPosition.z");
            m_LocalScale = serializedObject.FindProperty("m_LocalScale");
            if (m_RotationGUI == null)
                m_RotationGUI = new TransformRotationGUI();
            m_RotationGUI.OnEnable(serializedObject.FindProperty("m_LocalRotation"), new GUIContent("Rotation"));

            m_ShowLayoutOptions = EditorPrefs.GetBool(kShowAnchorPropsPrefName, false);
            m_RawEditMode = EditorPrefs.GetBool(kLockRectPrefName, false);

            m_ChangingAnchors.valueChanged.AddListener(RepaintScene);
            m_ChangingPivot.valueChanged.AddListener(RepaintScene);
            m_ChangingWidth.valueChanged.AddListener(RepaintScene);
            m_ChangingHeight.valueChanged.AddListener(RepaintScene);
            m_ChangingPosX.valueChanged.AddListener(RepaintScene);
            m_ChangingPosY.valueChanged.AddListener(RepaintScene);
            m_ChangingLeft.valueChanged.AddListener(RepaintScene);
            m_ChangingRight.valueChanged.AddListener(RepaintScene);
            m_ChangingTop.valueChanged.AddListener(RepaintScene);
            m_ChangingBottom.valueChanged.AddListener(RepaintScene);

            ManipulationToolUtility.handleDragChange += HandleDragChange;
        }

        void OnDisable()
        {
            m_ChangingAnchors.valueChanged.RemoveListener(RepaintScene);
            m_ChangingPivot.valueChanged.RemoveListener(RepaintScene);
            m_ChangingWidth.valueChanged.RemoveListener(RepaintScene);
            m_ChangingHeight.valueChanged.RemoveListener(RepaintScene);
            m_ChangingPosX.valueChanged.RemoveListener(RepaintScene);
            m_ChangingPosY.valueChanged.RemoveListener(RepaintScene);
            m_ChangingLeft.valueChanged.RemoveListener(RepaintScene);
            m_ChangingRight.valueChanged.RemoveListener(RepaintScene);
            m_ChangingTop.valueChanged.RemoveListener(RepaintScene);
            m_ChangingBottom.valueChanged.RemoveListener(RepaintScene);

            ManipulationToolUtility.handleDragChange -= HandleDragChange;

            if (m_DropdownWindow != null && m_DropdownWindow.editorWindow != null)
                m_DropdownWindow.editorWindow.Close();
        }

        void HandleDragChange(string handleName, bool dragging)
        {
            AnimatedValues.AnimBool animBool;
            switch (handleName)
            {
                case RectTool.kChangingLeft: animBool = m_ChangingLeft; break;
                case RectTool.kChangingRight: animBool = m_ChangingRight; break;
                case RectTool.kChangingPosY: animBool = m_ChangingPosY; break;
                case RectTool.kChangingWidth: animBool = m_ChangingWidth; break;
                case RectTool.kChangingBottom: animBool = m_ChangingBottom; break;
                case RectTool.kChangingTop: animBool = m_ChangingTop; break;
                case RectTool.kChangingPosX: animBool = m_ChangingPosX; break;
                case RectTool.kChangingHeight: animBool = m_ChangingHeight; break;
                case RectTool.kChangingPivot: animBool = m_ChangingPivot; break;
                default: animBool = null; break;
            }
            if (animBool != null)
                animBool.target = dragging;
        }

        void SetFadingBasedOnMouseDownUp(ref AnimatedValues.AnimBool animBool, Event eventBefore)
        {
            if (eventBefore.type == EventType.MouseDrag && Event.current.type != EventType.MouseDrag)
                animBool.value = true;
            else if (eventBefore.type == EventType.MouseUp && Event.current.type != EventType.MouseUp)
                animBool.target = false;
        }

        void SetFadingBasedOnControlID(ref AnimatedValues.AnimBool animBool, int id)
        {
            GUIView focusedView = (EditorWindow.focusedWindow == null ? null : EditorWindow.focusedWindow.m_Parent);
            if (GUIUtility.keyboardControl == id && GUIView.current == focusedView)
            {
                animBool.value = true;
                m_KeyboardControlIDs[id] = animBool;
            }
            else if ((GUIUtility.keyboardControl != id || GUIView.current != focusedView) && m_KeyboardControlIDs.ContainsKey(id))
            {
                m_KeyboardControlIDs.Remove(id);
                if (!m_KeyboardControlIDs.ContainsValue(animBool))
                    animBool.target = false;
            }
        }

        void RepaintScene()
        {
            SceneView.RepaintAll();
        }

        private static bool ShouldDoIntSnapping(RectTransform rect)
        {
            Canvas canvas = rect.gameObject.GetComponentInParent<Canvas>();
            return (canvas != null && canvas.renderMode != RenderMode.WorldSpace);
        }

        public override void OnInspectorGUI()
        {
            if (!EditorGUIUtility.wideMode)
            {
                EditorGUIUtility.wideMode = true;
                EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth - 212;
            }

            bool anyDriven = false;
            bool anyDrivenXPositionOrSize = false;
            bool anyDrivenYPositionOrSize = false;
            bool anyWithoutParent = false;
            foreach (RectTransform gui in targets)
            {
                if (gui.drivenByObject != null)
                {
                    anyDriven = true;
                    if ((gui.drivenProperties & (DrivenTransformProperties.AnchoredPositionX | DrivenTransformProperties.SizeDeltaX)) != 0)
                        anyDrivenXPositionOrSize = true;
                    if ((gui.drivenProperties & (DrivenTransformProperties.AnchoredPositionY | DrivenTransformProperties.SizeDeltaY)) != 0)
                        anyDrivenYPositionOrSize = true;
                }

                PrefabType prefabType = PrefabUtility.GetPrefabType(gui.gameObject);
                if ((gui.transform.parent == null || gui.transform.parent.GetComponent<RectTransform>() == null)
                    && (prefabType != PrefabType.Prefab && prefabType != PrefabType.ModelPrefab))
                    anyWithoutParent = true;
            }

            if (anyDriven)
            {
                if (targets.Length == 1)
                    EditorGUILayout.HelpBox("Some values driven by " + (target as RectTransform).drivenByObject.GetType().Name + ".", MessageType.None);
                else
                    EditorGUILayout.HelpBox("Some values in some or all objects are driven.", MessageType.None);
            }

            serializedObject.Update();

            LayoutDropdownButton(anyWithoutParent);

            // Position and Size Delta
            SmartPositionAndSizeFields(anyWithoutParent, anyDrivenXPositionOrSize, anyDrivenYPositionOrSize);

            // Anchor and pivot fields
            SmartAnchorFields();
            SmartPivotField();

            EditorGUILayout.Space();

            // Rotation
            m_RotationGUI.RotationField(targets.Any(x => ((x as RectTransform).drivenProperties & DrivenTransformProperties.Rotation) != 0));

            // Scale
            s_ScaleDisabledMask[0] = targets.Any(x => ((x as RectTransform).drivenProperties & DrivenTransformProperties.ScaleX) != 0);
            s_ScaleDisabledMask[1] = targets.Any(x => ((x as RectTransform).drivenProperties & DrivenTransformProperties.ScaleY) != 0);
            s_ScaleDisabledMask[2] = targets.Any(x => ((x as RectTransform).drivenProperties & DrivenTransformProperties.ScaleZ) != 0);
            Vector3FieldWithDisabledMash(EditorGUILayout.GetControlRect(), m_LocalScale, styles.transformScaleContent, s_ScaleDisabledMask);

            serializedObject.ApplyModifiedProperties();
        }

        // A Vector3 field where each of the x, y and z elements can be disabled.
        static void Vector3FieldWithDisabledMash(Rect position, SerializedProperty property, GUIContent label, bool[] disabledMask)
        {
            EditorGUI.BeginProperty(position, label, property);

            int id = GUIUtility.GetControlID(s_FoldoutHash, FocusType.Keyboard, position);
            position = EditorGUI.MultiFieldPrefixLabel(position, id, label, 3);
            position.height = EditorGUIUtility.singleLineHeight;
            SerializedProperty cur = property.Copy();
            cur.NextVisible(true);
            EditorGUI.MultiPropertyField(position, s_XYZLabels, cur, EditorGUI.PropertyVisibility.OnlyVisible, EditorGUI.kMiniLabelW, disabledMask);

            EditorGUI.EndProperty();
        }

        private LayoutDropdownWindow m_DropdownWindow;
        void LayoutDropdownButton(bool anyWithoutParent)
        {
            Rect dropdownPosition = GUILayoutUtility.GetRect(0, 0);
            dropdownPosition.x += 2;
            dropdownPosition.y += 17;
            dropdownPosition.height = kDropdownSize;
            dropdownPosition.width = kDropdownSize;

            using (new EditorGUI.DisabledScope(anyWithoutParent))
            {
                Color oldColor = GUI.color;
                GUI.color = new Color(1, 1, 1, 0.6f) * oldColor;
                if (EditorGUI.DropdownButton(dropdownPosition, GUIContent.none, FocusType.Passive, "box"))
                {
                    GUIUtility.keyboardControl = 0;
                    m_DropdownWindow = new LayoutDropdownWindow(serializedObject);
                    PopupWindow.Show(dropdownPosition, m_DropdownWindow, null, ShowMode.PopupMenuWithKeyboardFocus);
                }
                GUI.color = oldColor;
            }

            if (!anyWithoutParent)
            {
                LayoutDropdownWindow.DrawLayoutMode(new RectOffset(7, 7, 7, 7).Remove(dropdownPosition), m_AnchorMin, m_AnchorMax, m_AnchoredPosition, m_SizeDelta);
                LayoutDropdownWindow.DrawLayoutModeHeadersOutsideRect(dropdownPosition, m_AnchorMin, m_AnchorMax, m_AnchoredPosition, m_SizeDelta);
            }
        }

        void SmartPositionAndSizeFields(bool anyWithoutParent, bool anyDrivenX, bool anyDrivenY)
        {
            Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * 4);

            rect.height = EditorGUIUtility.singleLineHeight * 2;
            Rect rect2;

            bool anyStretchX = targets.Any(x => (x as RectTransform).anchorMin.x != (x as RectTransform).anchorMax.x);
            bool anyStretchY = targets.Any(x => (x as RectTransform).anchorMin.y != (x as RectTransform).anchorMax.y);
            bool anyNonStretchX = targets.Any(x => (x as RectTransform).anchorMin.x == (x as RectTransform).anchorMax.x);
            bool anyNonStretchY = targets.Any(x => (x as RectTransform).anchorMin.y == (x as RectTransform).anchorMax.y);

            rect2 = GetColumnRect(rect, 0);
            if (anyNonStretchX || anyWithoutParent || anyDrivenX)
            {
                EditorGUI.BeginProperty(rect2, null, m_AnchoredPosition.FindPropertyRelative("x"));
                FloatFieldLabelAbove(rect2,
                    rectTransform => rectTransform.anchoredPosition.x,
                    (rectTransform, val) => rectTransform.anchoredPosition = new Vector2(val, rectTransform.anchoredPosition.y),
                    DrivenTransformProperties.AnchoredPositionX,
                    new GUIContent("Pos X"));
                SetFadingBasedOnControlID(ref m_ChangingPosX, EditorGUIUtility.s_LastControlID);
                EditorGUI.EndProperty();
            }
            else
            {
                // Affected by both anchored position and size delta so do property handling for both. (E.g. showing animated value, prefab override etc.)
                EditorGUI.BeginProperty(rect2, null, m_AnchoredPosition.FindPropertyRelative("x"));
                EditorGUI.BeginProperty(rect2, null, m_SizeDelta.FindPropertyRelative("x"));
                FloatFieldLabelAbove(rect2,
                    rectTransform => rectTransform.offsetMin.x,
                    (rectTransform, val) => rectTransform.offsetMin = new Vector2(val, rectTransform.offsetMin.y),
                    DrivenTransformProperties.None,
                    new GUIContent("Left"));
                SetFadingBasedOnControlID(ref m_ChangingLeft, EditorGUIUtility.s_LastControlID);
                EditorGUI.EndProperty();
                EditorGUI.EndProperty();
            }

            rect2 = GetColumnRect(rect, 1);
            if (anyNonStretchY || anyWithoutParent || anyDrivenY)
            {
                EditorGUI.BeginProperty(rect2, null, m_AnchoredPosition.FindPropertyRelative("y"));
                FloatFieldLabelAbove(rect2,
                    rectTransform => rectTransform.anchoredPosition.y,
                    (rectTransform, val) => rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, val),
                    DrivenTransformProperties.AnchoredPositionY,
                    new GUIContent("Pos Y"));
                SetFadingBasedOnControlID(ref m_ChangingPosY, EditorGUIUtility.s_LastControlID);
                EditorGUI.EndProperty();
            }
            else
            {
                // Affected by both anchored position and size delta so do property handling for both. (E.g. showing animated value, prefab override etc.)
                EditorGUI.BeginProperty(rect2, null, m_AnchoredPosition.FindPropertyRelative("y"));
                EditorGUI.BeginProperty(rect2, null, m_SizeDelta.FindPropertyRelative("y"));
                FloatFieldLabelAbove(rect2,
                    rectTransform => - rectTransform.offsetMax.y,
                    (rectTransform, val) => rectTransform.offsetMax = new Vector2(rectTransform.offsetMax.x, -val),
                    DrivenTransformProperties.None,
                    new GUIContent("Top"));
                SetFadingBasedOnControlID(ref m_ChangingTop, EditorGUIUtility.s_LastControlID);
                EditorGUI.EndProperty();
                EditorGUI.EndProperty();
            }

            rect2 = GetColumnRect(rect, 2);
            EditorGUI.BeginProperty(rect2, null, m_LocalPositionZ);
            FloatFieldLabelAbove(rect2,
                rectTransform => rectTransform.transform.localPosition.z,
                (rectTransform, val) => rectTransform.transform.localPosition = new Vector3(rectTransform.transform.localPosition.x, rectTransform.transform.localPosition.y, val),
                DrivenTransformProperties.AnchoredPositionZ,
                new GUIContent("Pos Z"));
            EditorGUI.EndProperty();

            rect.y += EditorGUIUtility.singleLineHeight * 2;

            rect2 = GetColumnRect(rect, 0);
            if (anyNonStretchX || anyWithoutParent || anyDrivenX)
            {
                EditorGUI.BeginProperty(rect2, null, m_SizeDelta.FindPropertyRelative("x"));
                FloatFieldLabelAbove(rect2,
                    rectTransform => rectTransform.sizeDelta.x,
                    (rectTransform, val) => rectTransform.sizeDelta = new Vector2(val, rectTransform.sizeDelta.y),
                    DrivenTransformProperties.SizeDeltaX,
                    anyStretchX ? new GUIContent("W Delta") : new GUIContent("Width"));
                SetFadingBasedOnControlID(ref m_ChangingWidth, EditorGUIUtility.s_LastControlID);
                EditorGUI.EndProperty();
            }
            else
            {
                // Affected by both anchored position and size delta so do property handling for both. (E.g. showing animated value, prefab override etc.)
                EditorGUI.BeginProperty(rect2, null, m_AnchoredPosition.FindPropertyRelative("x"));
                EditorGUI.BeginProperty(rect2, null, m_SizeDelta.FindPropertyRelative("x"));
                FloatFieldLabelAbove(rect2,
                    rectTransform => - rectTransform.offsetMax.x,
                    (rectTransform, val) => rectTransform.offsetMax = new Vector2(-val, rectTransform.offsetMax.y),
                    DrivenTransformProperties.None,
                    new GUIContent("Right"));
                SetFadingBasedOnControlID(ref m_ChangingRight, EditorGUIUtility.s_LastControlID);
                EditorGUI.EndProperty();
                EditorGUI.EndProperty();
            }

            rect2 = GetColumnRect(rect, 1);
            if (anyNonStretchY || anyWithoutParent || anyDrivenY)
            {
                EditorGUI.BeginProperty(rect2, null, m_SizeDelta.FindPropertyRelative("y"));
                FloatFieldLabelAbove(rect2,
                    rectTransform => rectTransform.sizeDelta.y,
                    (rectTransform, val) => rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, val),
                    DrivenTransformProperties.SizeDeltaY,
                    anyStretchY ? new GUIContent("H Delta") : new GUIContent("Height"));
                SetFadingBasedOnControlID(ref m_ChangingHeight, EditorGUIUtility.s_LastControlID);
                EditorGUI.EndProperty();
            }
            else
            {
                // Affected by both anchored position and size delta so do property handling for both. (E.g. showing animated value, prefab override etc.)
                EditorGUI.BeginProperty(rect2, null, m_AnchoredPosition.FindPropertyRelative("y"));
                EditorGUI.BeginProperty(rect2, null, m_SizeDelta.FindPropertyRelative("y"));
                FloatFieldLabelAbove(rect2,
                    rectTransform => rectTransform.offsetMin.y,
                    (rectTransform, val) => rectTransform.offsetMin = new Vector2(rectTransform.offsetMin.x, val),
                    DrivenTransformProperties.None,
                    new GUIContent("Bottom"));
                SetFadingBasedOnControlID(ref m_ChangingBottom, EditorGUIUtility.s_LastControlID);
                EditorGUI.EndProperty();
                EditorGUI.EndProperty();
            }

            rect2 = rect;
            rect2.height = EditorGUIUtility.singleLineHeight;
            rect2.y += EditorGUIUtility.singleLineHeight;
            rect2.yMin -= 2;
            rect2.xMin = rect2.xMax - 26;
            rect2.x -= rect2.width;
            BlueprintButton(rect2);

            rect2.x += rect2.width;
            RawButton(rect2);
        }

        void SmartAnchorFields()
        {
            Rect anchorRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * (m_ShowLayoutOptions ? 3 : 1));
            anchorRect.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.BeginChangeCheck();
            m_ShowLayoutOptions = EditorGUI.Foldout(anchorRect, m_ShowLayoutOptions, styles.anchorsContent);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(kShowAnchorPropsPrefName, m_ShowLayoutOptions);

            if (!m_ShowLayoutOptions)
                return;

            EditorGUI.indentLevel++;

            anchorRect.y += EditorGUIUtility.singleLineHeight;
            Vector2Field(anchorRect,
                rectTransform => rectTransform.anchorMin.x,
                (rectTransform, val) => SetAnchorSmart(rectTransform, val, 0, false, !m_RawEditMode, true),
                rectTransform => rectTransform.anchorMin.y,
                (rectTransform, val) => SetAnchorSmart(rectTransform, val, 1, false, !m_RawEditMode, true),
                DrivenTransformProperties.AnchorMinX,
                DrivenTransformProperties.AnchorMinY,
                m_AnchorMin,
                styles.anchorMinContent);

            anchorRect.y += EditorGUIUtility.singleLineHeight;
            Vector2Field(anchorRect,
                rectTransform => rectTransform.anchorMax.x,
                (rectTransform, val) => SetAnchorSmart(rectTransform, val, 0, true, !m_RawEditMode, true),
                rectTransform => rectTransform.anchorMax.y,
                (rectTransform, val) => SetAnchorSmart(rectTransform, val, 1, true, !m_RawEditMode, true),
                DrivenTransformProperties.AnchorMaxX,
                DrivenTransformProperties.AnchorMaxY,
                m_AnchorMax,
                styles.anchorMaxContent);

            EditorGUI.indentLevel--;
        }

        void SmartPivotField()
        {
            Vector2Field(EditorGUILayout.GetControlRect(),
                rectTransform => rectTransform.pivot.x,
                (rectTransform, val) => SetPivotSmart(rectTransform, val, 0, !m_RawEditMode, false),
                rectTransform => rectTransform.pivot.y,
                (rectTransform, val) => SetPivotSmart(rectTransform, val, 1, !m_RawEditMode, false),
                DrivenTransformProperties.PivotX,
                DrivenTransformProperties.PivotY,
                m_Pivot,
                styles.pivotContent);
        }

        void RawButton(Rect position)
        {
            EditorGUI.BeginChangeCheck();
            m_RawEditMode = GUI.Toggle(position, m_RawEditMode, styles.rawEditContent, "ButtonRight");
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(kLockRectPrefName, m_RawEditMode);
        }

        void BlueprintButton(Rect position)
        {
            EditorGUI.BeginChangeCheck();
            bool newValue = GUI.Toggle(position, Tools.rectBlueprintMode, styles.blueprintContent, "ButtonLeft");
            if (EditorGUI.EndChangeCheck())
            {
                Tools.rectBlueprintMode = newValue;
                Tools.RepaintAllToolViews();
            }
        }

        void FloatFieldLabelAbove(Rect position, FloatGetter getter, FloatSetter setter, DrivenTransformProperties driven, GUIContent label)
        {
            using (new EditorGUI.DisabledScope(targets.Any(x => ((x as RectTransform).drivenProperties & driven) != 0)))
            {
                float value = getter(target as RectTransform);
                EditorGUI.showMixedValue = targets.Select(x => getter(x as RectTransform)).Distinct().Count() >= 2;

                EditorGUI.BeginChangeCheck();

                int id = GUIUtility.GetControlID(s_FloatFieldHash, FocusType.Keyboard, position);
                Rect positionLabel = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                Rect positionField = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.HandlePrefixLabel(position, positionLabel, label, id);
                float newValue = EditorGUI.DoFloatField(EditorGUI.s_RecycledEditor, positionField, positionLabel, id, value, EditorGUI.kFloatFieldFormatString, EditorStyles.textField, true);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObjects(targets, "Inspector");
                    foreach (RectTransform tr in targets)
                        setter(tr, newValue);
                }
            }
        }

        void Vector2Field(Rect position,
            FloatGetter xGetter, FloatSetter xSetter,
            FloatGetter yGetter, FloatSetter ySetter,
            DrivenTransformProperties xDriven, DrivenTransformProperties yDriven,
            SerializedProperty vec2Property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, vec2Property);

            SerializedProperty xProperty = vec2Property.FindPropertyRelative("x");
            SerializedProperty yProperty = vec2Property.FindPropertyRelative("y");

            EditorGUI.PrefixLabel(position, -1, label);
            float t = EditorGUIUtility.labelWidth;
            int l = EditorGUI.indentLevel;
            Rect r0 = GetColumnRect(position, 0);
            Rect r1 = GetColumnRect(position, 1);
            EditorGUIUtility.labelWidth = EditorGUI.kMiniLabelW;
            EditorGUI.indentLevel = 0;

            EditorGUI.BeginProperty(r0, s_XYLabels[0], xProperty);
            FloatField(r0, xGetter, xSetter, xDriven, s_XYLabels[0]);
            EditorGUI.EndProperty();

            EditorGUI.BeginProperty(r0, s_XYLabels[1], yProperty);
            FloatField(r1, yGetter, ySetter, yDriven, s_XYLabels[1]);
            EditorGUI.EndProperty();

            EditorGUIUtility.labelWidth = t;
            EditorGUI.indentLevel = l;

            EditorGUI.EndProperty();
        }

        void FloatField(Rect position, FloatGetter getter, FloatSetter setter, DrivenTransformProperties driven, GUIContent label)
        {
            using (new EditorGUI.DisabledScope(targets.Any(x => ((x as RectTransform).drivenProperties & driven) != 0)))
            {
                float value = getter(target as RectTransform);
                EditorGUI.showMixedValue = targets.Select(x => getter(x as RectTransform)).Distinct().Count() >= 2;

                EditorGUI.BeginChangeCheck();
                float newValue = EditorGUI.FloatField(position, label, value);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObjects(targets, "Inspector");
                    foreach (RectTransform tr in targets)
                        setter(tr, newValue);
                }
            }
        }

        Rect GetColumnRect(Rect totalRect, int column)
        {
            totalRect.xMin += EditorGUIUtility.labelWidth - 1;
            Rect rect = totalRect;
            rect.xMin += (totalRect.width - 4) * (column / 3f) + column * 2;
            rect.width = (totalRect.width - 4) / 3f;
            return rect;
        }

        void DrawRect(Rect rect, Transform space, bool dotted)
        {
            Vector3 p0 = space.TransformPoint(new Vector2(rect.x, rect.y));
            Vector3 p1 = space.TransformPoint(new Vector2(rect.x, rect.yMax));
            Vector3 p2 = space.TransformPoint(new Vector2(rect.xMax, rect.yMax));
            Vector3 p3 = space.TransformPoint(new Vector2(rect.xMax, rect.y));
            if (!dotted)
            {
                Handles.DrawLine(p0, p1);
                Handles.DrawLine(p1, p2);
                Handles.DrawLine(p2, p3);
                Handles.DrawLine(p3, p0);
            }
            else
            {
                RectHandles.DrawDottedLineWithShadow(kShadowColor, kShadowOffset, p0, p1, kDottedLineSize);
                RectHandles.DrawDottedLineWithShadow(kShadowColor, kShadowOffset, p1, p2, kDottedLineSize);
                RectHandles.DrawDottedLineWithShadow(kShadowColor, kShadowOffset, p2, p3, kDottedLineSize);
                RectHandles.DrawDottedLineWithShadow(kShadowColor, kShadowOffset, p3, p0, kDottedLineSize);
            }
        }

        void OnSceneGUI()
        {
            RectTransform gui = target as RectTransform;

            Rect rectInOwnSpace = gui.rect;
            Rect rectInUserSpace = rectInOwnSpace;
            Rect rectInParentSpace = rectInOwnSpace;
            Transform ownSpace = gui.transform;
            Transform userSpace = ownSpace;
            Transform parentSpace = ownSpace;
            RectTransform guiParent = null;
            if (ownSpace.parent != null)
            {
                parentSpace = ownSpace.parent;
                rectInParentSpace.x += ownSpace.localPosition.x;
                rectInParentSpace.y += ownSpace.localPosition.y;

                guiParent = parentSpace.GetComponent<RectTransform>();
            }

            if (Tools.rectBlueprintMode)
            {
                userSpace = parentSpace;
                rectInUserSpace = rectInParentSpace;
            }

            // Show unrotated/unscaled rect if moving anchor/pivot
            float alpha = Mathf.Max(m_ChangingAnchors.faded, m_ChangingPivot.faded);
            // Also show when moving or resizing rect if anchors are scaling on either axis
            if (gui.anchorMin != gui.anchorMax)
            {
                alpha = Mathf.Max(alpha,
                        m_ChangingPosX.faded,
                        m_ChangingPosY.faded,
                        m_ChangingLeft.faded,
                        m_ChangingRight.faded,
                        m_ChangingTop.faded,
                        m_ChangingBottom.faded);
            }

            Color rectInParentSpaceColor = kRectInParentSpaceColor;
            rectInParentSpaceColor.a *= alpha;
            Handles.color = rectInParentSpaceColor;
            DrawRect(rectInParentSpace, parentSpace, true);

            if (m_TargetCount == 1)
            {
                RectTransformSnapping.OnGUI();

                if (guiParent != null)
                    AllAnchorsSceneGUI(gui, guiParent, parentSpace, ownSpace);

                DrawSizes(rectInUserSpace, userSpace, rectInParentSpace, parentSpace, gui, guiParent);

                RectTransformSnapping.DrawGuides();

                if (Tools.current == Tool.Rect)
                    ParentRectPreviewDragHandles(guiParent, parentSpace);
            }
        }

        void ParentRectPreviewDragHandles(RectTransform gui, Transform space)
        {
            if (gui == null)
                return;

            float size = 0.05f * HandleUtility.GetHandleSize(space.position);
            Rect rect = gui.rect;
            for (int xHandle = 0; xHandle <= 2; xHandle++)
            {
                for (int yHandle = 0; yHandle <= 2; yHandle++)
                {
                    // Exactly one of the axes should be 1
                    if ((xHandle == 1) == (yHandle == 1))
                        continue;

                    Vector3 curPos = Vector2.zero;
                    for (int axis = 0; axis < 2; axis++)
                        curPos[axis] = Mathf.Lerp(rect.min[axis], rect.max[axis], (axis == 0 ? xHandle : yHandle) * 0.5f);
                    curPos = space.TransformPoint(curPos);

                    int id = GUIUtility.GetControlID(s_ParentRectPreviewHandlesHash, FocusType.Passive);

                    Vector3 sideDir = (xHandle == 1 ? space.right * rect.width : space.up * rect.height);
                    Vector3 slideDir = (xHandle == 1 ? space.up : space.right);

                    // could happen if gui.rect.{width,height} == 0
                    if (sideDir == Vector3.zero)
                        continue;

                    EditorGUI.BeginChangeCheck();
                    Vector3 newPos = RectHandles.SideSlider(id, curPos, sideDir, slideDir, size, null, 0, -3);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Vector2 curPosInSpace = space.InverseTransformPoint(curPos);
                        Vector2 newPosInSpace = space.InverseTransformPoint(newPos);
                        Rect newRect = rect;
                        Vector2 offset = newPosInSpace - curPosInSpace;
                        if (xHandle == 0)
                            newRect.min = new Vector2(newRect.min.x + offset.x, newRect.min.y);
                        if (xHandle == 2)
                            newRect.max = new Vector2(newRect.max.x + offset.x, newRect.max.y);
                        if (yHandle == 0)
                            newRect.min = new Vector2(newRect.min.x, newRect.min.y + offset.y);
                        if (yHandle == 2)
                            newRect.max = new Vector2(newRect.max.x, newRect.max.y + offset.y);
                        SetTemporaryRect(gui, newRect, id);
                    }

                    if (EditorGUIUtility.hotControl == id)
                    {
                        Handles.BeginGUI();
                        EditorGUI.DropShadowLabel(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 60, 16), "Preview");
                        Handles.EndGUI();
                    }
                }
            }
        }

        private static float s_ParentDragTime = 0;
        private static float s_ParentDragId = 0;
        private static Rect s_ParentDragOrigRect = new Rect();
        private static Rect s_ParentDragPreviewRect = new Rect();
        private static RectTransform s_ParentDragRectTransform = null;
        void SetTemporaryRect(RectTransform gui, Rect rect, int id)
        {
            if (s_ParentDragRectTransform == null)
            {
                s_ParentDragRectTransform = gui;
                s_ParentDragOrigRect = gui.rect;
                s_ParentDragId = id;
            }
            else if (s_ParentDragRectTransform != gui)
            {
                return;
            }

            s_ParentDragPreviewRect = rect;
            s_ParentDragTime = Time.realtimeSinceStartup;

            InternalEditorUtility.SetRectTransformTemporaryRect(gui, rect);

            // Remove if it was already added so it's not added more than once.
            EditorApplication.update -= UpdateTemporaryRect;
            EditorApplication.update += UpdateTemporaryRect;
        }

        void UpdateTemporaryRect()
        {
            if (s_ParentDragRectTransform == null)
                return;

            if (EditorGUIUtility.hotControl == s_ParentDragId)
            {
                s_ParentDragTime = Time.realtimeSinceStartup;
                Canvas.ForceUpdateCanvases();
                GameView.RepaintAll();
                return;
            }

            float elapsed = Time.realtimeSinceStartup - s_ParentDragTime;
            float lerp = Mathf.Clamp01(1 - elapsed * 8);
            if (lerp > 0)
            {
                Rect r = new Rect();
                r.position = Vector2.Lerp(s_ParentDragOrigRect.position, s_ParentDragPreviewRect.position, lerp);
                r.size = Vector2.Lerp(s_ParentDragOrigRect.size, s_ParentDragPreviewRect.size, lerp);
                InternalEditorUtility.SetRectTransformTemporaryRect(s_ParentDragRectTransform, r);
            }
            else
            {
                InternalEditorUtility.SetRectTransformTemporaryRect(s_ParentDragRectTransform, new Rect());
                EditorApplication.update -= UpdateTemporaryRect;
                s_ParentDragRectTransform = null;
            }
            Canvas.ForceUpdateCanvases();
            SceneView.RepaintAll();
            GameView.RepaintAll();
        }

        void AllAnchorsSceneGUI(RectTransform gui, RectTransform guiParent, Transform parentSpace, Transform transform)
        {
            Handles.color = kParentColor;
            // Draw parent rect
            DrawRect(guiParent.rect, parentSpace, false);

            // Draw sibling rects and anchors
            Handles.color = kSiblingColor;
            foreach (Transform tr in parentSpace)
            {
                if (!tr.gameObject.activeInHierarchy)
                    continue;
                RectTransform sibling = tr.GetComponent<RectTransform>();
                if (sibling)
                {
                    Rect siblingRect = sibling.rect;
                    siblingRect.x += sibling.transform.localPosition.x;
                    siblingRect.y += sibling.transform.localPosition.y;
                    DrawRect(sibling.rect, sibling, false);
                    if (sibling != transform)
                        AnchorsSceneGUI(sibling, guiParent, parentSpace, false);
                }
            }

            // Draw anchors for RectTransform itself
            Handles.color = kAnchorColor;
            AnchorsSceneGUI(gui, guiParent, parentSpace, true);
        }

        Vector3 GetAnchorLocal(RectTransform guiParent, Vector2 anchor)
        {
            return NormalizedToPointUnclamped(guiParent.rect, anchor);
        }

        static Vector2 NormalizedToPointUnclamped(Rect rectangle, Vector2 normalizedRectCoordinates)
        {
            return new Vector2(
                Mathf.LerpUnclamped(rectangle.x, rectangle.xMax, normalizedRectCoordinates.x),
                Mathf.LerpUnclamped(rectangle.y, rectangle.yMax, normalizedRectCoordinates.y)
                );
        }

        static bool AnchorAllowedOutsideParent(int axis, int minmax)
        {
            // Allow dragging outside if action key is held down (same key that disables snapping).
            // Also allow when not dragging at all - for e.g. typing values into the Inspector.
            if (EditorGUI.actionKey || EditorGUIUtility.hotControl == 0)
                return true;
            // Also allow if drag started outside of range to begin with.
            float value = (minmax == 0 ? s_StartDragAnchorMin[axis] : s_StartDragAnchorMax[axis]);
            return (value < -0.001f || value > 1.001f);
        }

        void AnchorsSceneGUI(RectTransform gui, RectTransform guiParent, Transform parentSpace, bool interactive)
        {
            if (Event.current.type == EventType.MouseDown)
            {
                s_AnchorFusedState = AnchorFusedState.None;
                if (gui.anchorMin == gui.anchorMax)
                    s_AnchorFusedState = AnchorFusedState.All;
                else if (gui.anchorMin.x == gui.anchorMax.x)
                    s_AnchorFusedState = AnchorFusedState.Horizontal;
                else if (gui.anchorMin.y == gui.anchorMax.y)
                    s_AnchorFusedState = AnchorFusedState.Vertical;
            }

            // Handles for the four corners
            AnchorSceneGUI(gui, guiParent, parentSpace, interactive, 0, 0, GUIUtility.GetControlID(FocusType.Passive));
            AnchorSceneGUI(gui, guiParent, parentSpace, interactive, 0, 1, GUIUtility.GetControlID(FocusType.Passive));
            AnchorSceneGUI(gui, guiParent, parentSpace, interactive, 1, 0, GUIUtility.GetControlID(FocusType.Passive));
            AnchorSceneGUI(gui, guiParent, parentSpace, interactive, 1, 1, GUIUtility.GetControlID(FocusType.Passive));

            if (!interactive)
                return;

            // Aditional handles for dragging multiple of the corners simultaneously.
            // These are not drawn, so don't need to be done for non-interactive handles.

            // Get all ids always, regardless of whether the handles are done or not,
            // to prevent id mismatch issues.
            int idAll = GUIUtility.GetControlID(FocusType.Passive);
            int idH1 = GUIUtility.GetControlID(FocusType.Passive);
            int idH2 = GUIUtility.GetControlID(FocusType.Passive);
            int idV1 = GUIUtility.GetControlID(FocusType.Passive);
            int idV2 = GUIUtility.GetControlID(FocusType.Passive);

            if (s_AnchorFusedState == AnchorFusedState.All)
            {
                AnchorSceneGUI(gui, guiParent, parentSpace, interactive, 2, 2, idAll);
            }
            if (s_AnchorFusedState == AnchorFusedState.Horizontal)
            {
                AnchorSceneGUI(gui, guiParent, parentSpace, interactive, 2, 0, idH1);
                AnchorSceneGUI(gui, guiParent, parentSpace, interactive, 2, 1, idH2);
            }
            if (s_AnchorFusedState == AnchorFusedState.Vertical)
            {
                AnchorSceneGUI(gui, guiParent, parentSpace, interactive, 0, 2, idV1);
                AnchorSceneGUI(gui, guiParent, parentSpace, interactive, 1, 2, idV2);
            }
        }

        // Minmax here means: 0 = min, 1 = max, 2 = both at once
        void AnchorSceneGUI(RectTransform gui, RectTransform guiParent, Transform parentSpace, bool interactive, int minmaxX, int minmaxY, int id)
        {
            Vector3 curPos = new Vector2();
            curPos.x = (minmaxX == 0 ? gui.anchorMin.x : gui.anchorMax.x);
            curPos.y = (minmaxY == 0 ? gui.anchorMin.y : gui.anchorMax.y);
            curPos = GetAnchorLocal(guiParent, curPos);
            curPos = parentSpace.TransformPoint(curPos);

            float size = 0.05f * HandleUtility.GetHandleSize(curPos);

            if (minmaxX < 2)
                curPos += parentSpace.right * size * (minmaxX * 2 - 1);
            if (minmaxY < 2)
                curPos += parentSpace.up * size * (minmaxY * 2 - 1);

            if (minmaxX < 2 && minmaxY < 2)
                DrawAnchor(curPos, parentSpace.right * size * 2 * (minmaxX * 2 - 1), parentSpace.up * size * 2 * (minmaxY * 2 - 1));

            if (!interactive)
                return;

            Event evtCopy = new Event(Event.current);

            EditorGUI.BeginChangeCheck();
            Vector3 newPos = Handles.Slider2D(id, curPos, parentSpace.forward, parentSpace.right, parentSpace.up, size, (Handles.CapFunction)null, Vector2.zero);

            if (evtCopy.type == EventType.MouseDown && GUIUtility.hotControl == id)
            {
                s_DragAnchorsTogether = EditorGUI.actionKey;
                s_StartDragAnchorMin = gui.anchorMin;
                s_StartDragAnchorMax = gui.anchorMax;
                RectTransformSnapping.CalculateAnchorSnapValues(parentSpace, gui.transform, gui, minmaxX, minmaxY);
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(gui, "Move Rectangle Anchors");
                Vector2 offset = parentSpace.InverseTransformVector(newPos - curPos);
                for (int axis = 0; axis <= 1; axis++)
                {
                    offset[axis] /= guiParent.rect.size[axis];

                    int minmaxForAxis = (axis == 0 ? minmaxX : minmaxY);
                    bool isMax = (minmaxForAxis == 1);
                    float old = isMax ? gui.anchorMax[axis] : gui.anchorMin[axis];
                    float newValue = old + offset[axis];

                    // Constraint to valid values
                    float snappedValue = newValue;
                    if (!AnchorAllowedOutsideParent(axis, minmaxForAxis))
                        snappedValue = Mathf.Clamp01(snappedValue);
                    if (minmaxForAxis == 0)
                        snappedValue = Mathf.Min(snappedValue, gui.anchorMax[axis]);
                    if (minmaxForAxis == 1)
                        snappedValue = Mathf.Max(snappedValue, gui.anchorMin[axis]);

                    // Snap to sibling anchors
                    float snapSize = HandleUtility.GetHandleSize(newPos) * RectTransformSnapping.kSnapThreshold / guiParent.rect.size[axis];
                    snapSize *= parentSpace.InverseTransformVector(axis == 0 ? Vector3.right : Vector3.up)[axis];
                    snappedValue = RectTransformSnapping.SnapToGuides(snappedValue, snapSize, axis);

                    bool snap = snappedValue != newValue;
                    newValue = snappedValue;

                    if (minmaxForAxis == 2)
                    {
                        SetAnchorSmart(gui, newValue, axis, false, !evtCopy.shift, snap, false, s_DragAnchorsTogether);
                        SetAnchorSmart(gui, newValue, axis, true, !evtCopy.shift, snap, false, s_DragAnchorsTogether);
                    }
                    else
                    {
                        SetAnchorSmart(gui, newValue, axis, isMax, !evtCopy.shift, snap, true, s_DragAnchorsTogether);
                    }
                    EditorUtility.SetDirty(gui);
                    if (gui.drivenByObject != null)
                        RectTransform.SendReapplyDrivenProperties(gui);
                }
            }

            SetFadingBasedOnMouseDownUp(ref m_ChangingAnchors, evtCopy);
        }

        static float Round(float value) { return Mathf.Floor(0.5f + value); }
        static int RoundToInt(float value) { return Mathf.FloorToInt(0.5f + value); }

        void DrawSizes(Rect rectInUserSpace, Transform userSpace, Rect rectInParentSpace, Transform parentSpace, RectTransform gui, RectTransform guiParent)
        {
            float size = 0.05f * HandleUtility.GetHandleSize(parentSpace.position);
            float alpha;

            bool stretchW = (gui.anchorMin.x != gui.anchorMax.x);
            bool stretchH = (gui.anchorMin.y != gui.anchorMax.y);

            alpha = Mathf.Max(
                    m_ChangingPosX.faded,
                    m_ChangingLeft.faded,
                    m_ChangingRight.faded,
                    m_ChangingAnchors.faded);
            DrawAnchorRect(parentSpace, gui, guiParent, 0, alpha);

            alpha = Mathf.Max(
                    m_ChangingPosY.faded,
                    m_ChangingTop.faded,
                    m_ChangingBottom.faded,
                    m_ChangingAnchors.faded);
            DrawAnchorRect(parentSpace, gui, guiParent, 1, alpha);

            DrawAnchorDistances(parentSpace, gui, guiParent, size, m_ChangingAnchors.faded);

            if (stretchW)
            {
                DrawPositionDistances(userSpace, rectInParentSpace, parentSpace, gui, guiParent, size, 0, 1, m_ChangingLeft.faded);
                DrawPositionDistances(userSpace, rectInParentSpace, parentSpace, gui, guiParent, size, 0, 2, m_ChangingRight.faded);
            }
            else
            {
                DrawPositionDistances(userSpace, rectInParentSpace, parentSpace, gui, guiParent, size, 0, 0, m_ChangingPosX.faded);
                DrawSizeDistances(userSpace, rectInParentSpace, parentSpace, gui, guiParent, size, 0, m_ChangingWidth.faded);
            }

            if (stretchH)
            {
                DrawPositionDistances(userSpace, rectInParentSpace, parentSpace, gui, guiParent, size, 1, 1, m_ChangingBottom.faded);
                DrawPositionDistances(userSpace, rectInParentSpace, parentSpace, gui, guiParent, size, 1, 2, m_ChangingTop.faded);
            }
            else
            {
                DrawPositionDistances(userSpace, rectInParentSpace, parentSpace, gui, guiParent, size, 1, 0, m_ChangingPosY.faded);
                DrawSizeDistances(userSpace, rectInParentSpace, parentSpace, gui, guiParent, size, 1, m_ChangingHeight.faded);
            }
        }

        void DrawSizeDistances(Transform userSpace, Rect rectInParentSpace, Transform parentSpace, RectTransform gui, RectTransform guiParent, float size, int axis, float alpha)
        {
            if (alpha <= 0)
                return;

            Color col = kAnchorColor;
            col.a *= alpha;
            GUI.color = col;

            if (userSpace == gui.transform)
            {
                gui.GetWorldCorners(s_Corners);
            }
            else
            {
                gui.GetLocalCorners(s_Corners);
                for (int i = 0; i < 4; i++)
                {
                    s_Corners[i] += gui.transform.localPosition;
                    s_Corners[i] = userSpace.TransformPoint(s_Corners[i]);
                }
            }

            string str = gui.sizeDelta[axis].ToString();
            GUIContent label = new GUIContent(str);
            Vector3 dir = (axis == 0 ? userSpace.up : userSpace.right) * size * 2;
            DrawLabelBetweenPoints(s_Corners[0] + dir, s_Corners[axis == 0 ? 3 : 1] + dir, label);
        }

        void DrawPositionDistances(Transform userSpace, Rect rectInParentSpace, Transform parentSpace, RectTransform gui, RectTransform guiParent, float size, int axis, int side, float alpha)
        {
            if (guiParent == null || alpha <= 0)
                return;

            Color col = kAnchorLineColor;
            col.a *= alpha;
            Handles.color = col;
            col = kAnchorColor;
            col.a *= alpha;
            GUI.color = col;

            Vector3 posA;
            Vector3 posB;
            float value;
            if (side == 0)
            {
                Vector2 pivot = Rect.NormalizedToPoint(rectInParentSpace, gui.pivot);
                posA = pivot;
                posB = pivot;
                posA[axis] = Mathf.LerpUnclamped(guiParent.rect.min[axis], guiParent.rect.max[axis], gui.anchorMin[axis]);
                value = gui.anchoredPosition[axis];
            }
            else
            {
                Vector2 center = rectInParentSpace.center;
                posA = center;
                posB = center;
                if (side == 1)
                {
                    posA[axis] = Mathf.LerpUnclamped(guiParent.rect.min[axis], guiParent.rect.max[axis], gui.anchorMin[axis]);
                    posB[axis] = rectInParentSpace.min[axis];
                    value = gui.offsetMin[axis];
                }
                else
                {
                    posA[axis] = Mathf.LerpUnclamped(guiParent.rect.min[axis], guiParent.rect.max[axis], gui.anchorMax[axis]);
                    posB[axis] = rectInParentSpace.max[axis];
                    value = -gui.offsetMax[axis];
                }
            }

            posA = parentSpace.TransformPoint(posA);
            posB = parentSpace.TransformPoint(posB);

            RectHandles.DrawDottedLineWithShadow(kShadowColor, kShadowOffset, posA, posB, kDottedLineSize);
            GUIContent label = new GUIContent(value.ToString());
            DrawLabelBetweenPoints(posA, posB, label);
        }

        void DrawAnchorDistances(Transform parentSpace, RectTransform gui, RectTransform guiParent, float size, float alpha)
        {
            if (guiParent == null || alpha <= 0)
                return;

            Color col = kAnchorColor;
            col.a *= alpha;
            GUI.color = col;

            // Show percentages in Scene View while dragging anchors.
            Vector3[,] points = new Vector3[2, 4];
            for (int axis = 0; axis < 2; axis++)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector3 p = Vector3.zero;
                    switch (i)
                    {
                        case 0: p = Vector3.zero; break;
                        case 1: p = gui.anchorMin; break;
                        case 2: p = gui.anchorMax; break;
                        case 3: p = Vector3.one; break;
                    }
                    p[axis] = gui.anchorMin[axis];
                    p = parentSpace.TransformPoint(GetAnchorLocal(guiParent, p));
                    points[axis, i] = p;
                }
            }

            for (int axis = 0; axis < 2; axis++)
            {
                Vector3 dir = (axis == 0 ? parentSpace.right : parentSpace.up) * size * 2;
                int startValue = RoundToInt((gui.anchorMin[1 - axis]) * 100);
                int midValue   = RoundToInt((gui.anchorMax[1 - axis] - gui.anchorMin[1 - axis]) * 100);
                int endValue   = RoundToInt((1 - gui.anchorMax[1 - axis]) * 100);
                if (startValue > 0)
                    DrawLabelBetweenPoints(points[axis, 0] - dir, points[axis, 1] - dir, GUIContent.Temp(startValue.ToString() + "%"));
                if (midValue > 0)
                    DrawLabelBetweenPoints(points[axis, 1] - dir, points[axis, 2] - dir, GUIContent.Temp(midValue.ToString() + "%"));
                if (endValue > 0)
                    DrawLabelBetweenPoints(points[axis, 2] - dir, points[axis, 3] - dir, GUIContent.Temp(endValue.ToString() + "%"));
            }
        }

        void DrawAnchorRect(Transform parentSpace, RectTransform gui, RectTransform guiParent, int axis, float alpha)
        {
            if (guiParent == null || alpha <= 0)
                return;

            Color col = kAnchorLineColor;
            col.a *= alpha;
            Handles.color = col;

            Vector3[,] points = new Vector3[2, 2];
            for (int side = 0; side < 2; side++)
            {
                if (side == 1 && gui.anchorMin[axis] == gui.anchorMax[axis])
                    continue;

                points[side, 0][1 - axis] = Mathf.Min(0, gui.anchorMin[1 - axis]);
                points[side, 1][1 - axis] = Mathf.Max(1, gui.anchorMax[1 - axis]);
                for (int endpoint = 0; endpoint < 2; endpoint++)
                {
                    points[side, endpoint][axis] = (side == 0 ? gui.anchorMin[axis] : gui.anchorMax[axis]);
                    points[side, endpoint] = parentSpace.TransformPoint(GetAnchorLocal(guiParent, points[side, endpoint]));
                }
                RectHandles.DrawDottedLineWithShadow(kShadowColor, kShadowOffset, points[side, 0], points[side, 1], kDottedLineSize);
            }
        }

        void DrawLabelBetweenPoints(Vector3 pA, Vector3 pB, GUIContent label)
        {
            if (pA == pB)
                return;

            Vector2 vA = HandleUtility.WorldToGUIPoint(pA);
            Vector2 vB = HandleUtility.WorldToGUIPoint(pB);
            Vector2 center = (vA + vB) * 0.5f;
            center.x = Round(center.x);
            center.y = Round(center.y);
            float angle = Mathf.Atan2(vB.y - vA.y, vB.x - vA.x) * Mathf.Rad2Deg;
            angle = Mathf.Repeat(angle + 89f, 180f) - 89f;

            Handles.BeginGUI();
            Matrix4x4 oldMatrix = GUI.matrix;

            GUIStyle style = styles.measuringLabelStyle;
            style.alignment = TextAnchor.MiddleCenter;

            GUIUtility.RotateAroundPivot(angle, center);
            EditorGUI.DropShadowLabel(new Rect(center.x - 50, center.y - 9, 100, 16), label, style);

            GUI.matrix = oldMatrix;
            Handles.EndGUI();
        }

        static Vector3 GetRectReferenceCorner(RectTransform gui, bool worldSpace)
        {
            if (worldSpace)
            {
                Transform t = gui.transform;
                gui.GetWorldCorners(s_Corners);
                if (t.parent)
                    return t.parent.InverseTransformPoint(s_Corners[0]);
                else
                    return s_Corners[0];
            }
            return (Vector3)gui.rect.min + gui.transform.localPosition;
        }

        void DrawAnchor(Vector3 pos, Vector3 right, Vector3 up)
        {
            pos -= up * 0.5f;
            pos -= right * 0.5f;
            up *= 1.4f;
            right *= 1.4f;
            RectHandles.DrawPolyLineWithShadow(kShadowColor, kShadowOffset,
                pos,
                pos + up + right * 0.5f,
                pos + right + up * 0.5f,
                pos);
        }

        public static void SetPivotSmart(RectTransform rect, float value, int axis, bool smart, bool parentSpace)
        {
            Vector3 cornerBefore = GetRectReferenceCorner(rect, !parentSpace);

            Vector2 rectPivot = rect.pivot;
            rectPivot[axis] = value;
            rect.pivot = rectPivot;

            if (smart)
            {
                Vector3 cornerAfter = GetRectReferenceCorner(rect, !parentSpace);
                Vector3 cornerOffset = cornerAfter - cornerBefore;
                rect.anchoredPosition -= (Vector2)cornerOffset;

                Vector3 pos = rect.transform.position;
                pos.z -= cornerOffset.z;
                rect.transform.position = pos;
            }
        }

        public static void SetAnchorSmart(RectTransform rect, float value, int axis, bool isMax, bool smart)
        {
            SetAnchorSmart(rect, value, axis, isMax, smart, false, false, false);
        }

        public static void SetAnchorSmart(RectTransform rect, float value, int axis, bool isMax, bool smart, bool enforceExactValue)
        {
            SetAnchorSmart(rect, value, axis, isMax, smart, enforceExactValue, false, false);
        }

        public static void SetAnchorSmart(RectTransform rect, float value, int axis, bool isMax, bool smart, bool enforceExactValue, bool enforceMinNoLargerThanMax, bool moveTogether)
        {
            RectTransform parent = null;
            if (rect.transform.parent == null)
            {
                smart = false;
            }
            else
            {
                parent = rect.transform.parent.GetComponent<RectTransform>();
                if (parent == null)
                    smart = false;
            }

            bool clampToParent = !AnchorAllowedOutsideParent(axis, isMax ? 1 : 0);
            if (clampToParent)
                value = Mathf.Clamp01(value);
            if (enforceMinNoLargerThanMax)
            {
                if (isMax)
                    value = Mathf.Max(value, rect.anchorMin[axis]);
                else
                    value = Mathf.Min(value, rect.anchorMax[axis]);
            }

            float offsetSizePixels = 0;
            float offsetPositionPixels = 0;
            if (smart)
            {
                float oldValue = isMax ? rect.anchorMax[axis] : rect.anchorMin[axis];

                offsetSizePixels = (value - oldValue) * parent.rect.size[axis];

                // Ensure offset is in whole pixels.
                // Note: In this particular instance we want to use Mathf.Round (which rounds towards nearest even number)
                // instead of Round from this class which always rounds down.
                // This makes the position of rect more stable when their anchors are changed.
                float roundingDelta = 0;
                if (ShouldDoIntSnapping(rect))
                    roundingDelta = Mathf.Round(offsetSizePixels) - offsetSizePixels;
                offsetSizePixels += roundingDelta;

                if (!enforceExactValue)
                {
                    value += roundingDelta / parent.rect.size[axis];

                    // Snap value to whole percent if close
                    if (Mathf.Abs(Round(value * 1000) - value * 1000) < 0.1f)
                        value = Round(value * 1000) * 0.001f;

                    if (clampToParent)
                        value = Mathf.Clamp01(value);
                    if (enforceMinNoLargerThanMax)
                    {
                        if (isMax)
                            value = Mathf.Max(value, rect.anchorMin[axis]);
                        else
                            value = Mathf.Min(value, rect.anchorMax[axis]);
                    }
                }

                if (moveTogether)
                    offsetPositionPixels = offsetSizePixels;
                else
                    offsetPositionPixels = (isMax ? offsetSizePixels * rect.pivot[axis] : (offsetSizePixels * (1 - rect.pivot[axis])));
            }

            if (isMax)
            {
                Vector2 rectAnchorMax = rect.anchorMax;
                rectAnchorMax[axis] = value;
                rect.anchorMax = rectAnchorMax;

                Vector2 other = rect.anchorMin;
                if (moveTogether)
                    other[axis] = s_StartDragAnchorMin[axis] + rectAnchorMax[axis] - s_StartDragAnchorMax[axis];
                rect.anchorMin = other;
            }
            else
            {
                Vector2 rectAnchorMin = rect.anchorMin;
                rectAnchorMin[axis] = value;
                rect.anchorMin = rectAnchorMin;

                Vector2 other = rect.anchorMax;
                if (moveTogether)
                    other[axis] = s_StartDragAnchorMax[axis] + rectAnchorMin[axis] - s_StartDragAnchorMin[axis];
                rect.anchorMax = other;
            }

            if (smart)
            {
                Vector2 rectPosition = rect.anchoredPosition;
                rectPosition[axis] -= offsetPositionPixels;
                rect.anchoredPosition = rectPosition;

                if (!moveTogether)
                {
                    Vector2 rectSizeDelta = rect.sizeDelta;
                    rectSizeDelta[axis] += offsetSizePixels * (isMax ? -1 : 1);
                    rect.sizeDelta = rectSizeDelta;
                }
            }
        }
    }
}
