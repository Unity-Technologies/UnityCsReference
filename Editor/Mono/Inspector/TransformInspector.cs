// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor
{
    [CustomEditor(typeof(Transform))]
    [CanEditMultipleObjects]
    internal class TransformInspector : Editor
    {
        // Space in which the inspector displays a transform value.
        internal enum DisplaySpace
        {
            Local,
            World,
        }

        const float k_SpaceDropdownArrowSize = 16;
        const int k_SpaceDropdownMargin = 3;

        // EditorPrefs-backed setting wrappers; hold no user-code references and must persist across reload to retain the preferences.
        [NoAutoStaticsCleanup]
        static readonly SavedInt s_PositionSpacePref = new SavedInt("TransformInspector.PositionSpace", (int)DisplaySpace.Local);
        [NoAutoStaticsCleanup]
        static readonly SavedInt s_RotationSpacePref = new SavedInt("TransformInspector.RotationSpace", (int)DisplaySpace.Local);
        [NoAutoStaticsCleanup]
        static readonly SavedInt s_ScaleSpacePref = new SavedInt("TransformInspector.ScaleSpace", (int)DisplaySpace.Local);

        static DisplaySpace GetDisplaySpace(SavedInt spacePref) => spacePref.value == (int)DisplaySpace.World ? DisplaySpace.World : DisplaySpace.Local;

        internal static DisplaySpace positionSpace
        {
            get { return GetDisplaySpace(s_PositionSpacePref); }
            set { s_PositionSpacePref.value = (int)value; }
        }

        internal static DisplaySpace rotationSpace
        {
            get { return GetDisplaySpace(s_RotationSpacePref); }
            set { s_RotationSpacePref.value = (int)value; }
        }

        internal static DisplaySpace scaleSpace
        {
            get { return GetDisplaySpace(s_ScaleSpacePref); }
            set { s_ScaleSpacePref.value = (int)value; }
        }

        SerializedProperty m_Position;
        SerializedProperty m_Scale;
        TransformRotationGUI m_RotationGUI;
        TransformWorldSpaceGUI m_WorldSpaceGUI;
        TransformWorldSpaceGUIData m_WorldSpaceData;
        ConstrainProportionsTransformScale m_ConstrainProportionsScale;
        SerializedProperty m_ConstrainProportionsScaleProperty;
        bool m_IsScaleDirty;

        class Contents
        {
            public GUIContent positionContent = L10n.TextContent("Position", "The local position of this GameObject relative to the parent.", null, null);
            public GUIContent scaleContent = L10n.TextContent("Scale", "The local scaling of this GameObject relative to the parent.", null, null);
            public string floatingPointWarning = LocalizationDatabase.GetLocalizedString("Due to floating-point precision limitations, it is recommended to bring the world coordinates of the GameObject within a smaller range.");
            public GUIContent rotationContent = L10n.TextContent("Rotation", "The local rotation of this GameObject relative to the parent.", null, null);
            public GUIContent spaceDropdownArrowContent = EditorGUIUtility.TrIconContent("icon dropdown", null, null);
            // Icon button stretched over the label, which the field draws on top of it, with the arrow on its right.
            GUIStyle m_SpaceDropdownStyle;
            public GUIStyle spaceDropdownStyle => m_SpaceDropdownStyle ??= new GUIStyle(EditorStyles.iconButton)
            {
                fixedWidth = 0,
                fixedHeight = 0,
                alignment = TextAnchor.MiddleRight,
                padding = new RectOffset(0, k_SpaceDropdownMargin, 0, 0),
            };
            public GUIContent localSpaceContent = L10n.TextContent("Local", null, null, null);
            public GUIContent worldSpaceContent = L10n.TextContent("World", null, null, null);

            public GUIContent worldPositionContent = L10n.TextContent("World Position", "The world position of this GameObject.", null, null);
            public GUIContent worldRotationContent = L10n.TextContent("World Rotation", "The world rotation of this GameObject, as euler angles in degrees.", null, null);
            public GUIContent worldScaleContent = L10n.TextContent("World Scale", "The approximate world scale of this GameObject. It cannot be edited in world space.", null, null);
        }
        [NoAutoStaticsCleanup] // GUIContent/string-only lazy Contents cache; contents survive code reload
        static Contents s_Contents;

        public void OnEnable()
        {
            s_Contents ??= new Contents();
            m_Position = serializedObject.FindProperty("m_LocalPosition");
            m_Scale = serializedObject.FindProperty("m_LocalScale");
            m_ConstrainProportionsScaleProperty = serializedObject.FindProperty("m_ConstrainProportionsScale");

            if (m_RotationGUI == null)
                m_RotationGUI = new TransformRotationGUI();
            m_RotationGUI.OnEnable(serializedObject.FindProperty("m_LocalRotation"), s_Contents.rotationContent);
            // Created when first displayed, as computing the world values is wasted while all values are in local space.
            m_WorldSpaceData = null;
            m_WorldSpaceGUI = null;
            m_ConstrainProportionsScale = new ConstrainProportionsTransformScale(m_Scale.vector3Value);
        }

        public override void OnInspectorGUI()
        {
            if (s_Contents == null)
                s_Contents = new Contents();

            if (!EditorGUIUtility.wideMode)
            {
                EditorGUIUtility.wideMode = true;
                EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth - 212;
            }

            serializedObject.Update();

            if (positionSpace == DisplaySpace.World || rotationSpace == DisplaySpace.World || scaleSpace == DisplaySpace.World)
                UpdateWorldSpaceData();

            PositionField(positionSpace);
            RotationField(rotationSpace);
            ScaleField(scaleSpace);

            // If scale is dirty, update serializedObject to avoid overwriting new value with previous one when applying modifications
            if (m_IsScaleDirty)
                serializedObject.Update();

            // Warning if global position is too large for floating point errors.
            // SanitizeBounds function doesn't even support values beyond 100000
            Transform t = target as Transform;
            Vector3 pos = t.position;
            if (Mathf.Abs(pos.x) > 100000 || Mathf.Abs(pos.y) > 100000 || Mathf.Abs(pos.z) > 100000)
                EditorGUILayout.HelpBox(s_Contents.floatingPointWarning, MessageType.Warning);

            bool saveUndoName = serializedObject.hasModifiedProperties;
            serializedObject.ApplyModifiedProperties();

            if (saveUndoName)
            {
                int selection = Selection.knownCount;
                if (t.position != pos)
                    Undo.SetCurrentGroupName(string.Format("Set Position to {0} in {1}", t.localPosition, selection == 1 ? target.name : "Selected Objects"));
                else
                    Undo.SetCurrentGroupName(string.Format("Set Scale to {0} in {1}", t.localScale, selection == 1 ? target.name : "Selected Objects"));
            }

            if (m_IsScaleDirty)
            {
                m_IsScaleDirty = false;
                OnForceReloadInspector();
            }
            //resetting label width as it is carried over to other windows
            EditorGUIUtility.labelWidth = 0;
        }

        void UpdateWorldSpaceData()
        {
            if (m_WorldSpaceData == null)
            {
                m_WorldSpaceData = new TransformWorldSpaceGUIData(targets);
                m_WorldSpaceGUI = new TransformWorldSpaceGUI(m_WorldSpaceData, m_Position, serializedObject.FindProperty("m_LocalRotation"), m_Scale);
            }
            else
            {
                m_WorldSpaceData.Update();
            }
        }

        // Makes the label of the field and the arrow after it a dropdown. It is drawn before the field, so that it gets the left
        // clicks before the field, which still draws the label and gets the right clicks for its context menu.
        // reservedLabelWidth is the width used by the field at the end of its label area.
        static void SpaceDropdown(Rect rect, GUIContent label, SavedInt spacePref, float reservedLabelWidth = 0)
        {
            // The bold width is used whatever the label style, so that the arrow does not move when the value gets overridden.
            float labelWidth = EditorStyles.boldLabel.CalcSize(label).x;
            Rect labelRect = EditorGUI.IndentedRect(rect);
            // Extended on both sides, so that its box is not flush with the label and the arrow, which keep their positions.
            float xMin = labelRect.x - k_SpaceDropdownMargin;
            float xMax = labelRect.x + labelWidth + k_SpaceDropdownArrowSize + k_SpaceDropdownMargin;
            GUIContent content = s_Contents.spaceDropdownArrowContent;

            // When the label is cut by the field, the arrow would overlap the field, so only the visible part of the label is kept.
            float availableXMax = rect.x + EditorGUIUtility.labelWidth - reservedLabelWidth;
            if (xMax > availableXMax)
            {
                xMax = availableXMax;
                content = GUIContent.none;
            }

            var dropdownRect = new Rect(xMin, rect.y + (EditorGUIUtility.singleLineHeight - k_SpaceDropdownArrowSize) / 2, xMax - xMin, k_SpaceDropdownArrowSize);
            if (dropdownRect.width <= 0 || !EditorGUI.DropdownButton(dropdownRect, content, FocusType.Passive, s_Contents.spaceDropdownStyle))
                return;

            DisplaySpace space = GetDisplaySpace(spacePref);
            var menu = new GenericMenu();
            menu.AddItem(s_Contents.localSpaceContent, space == DisplaySpace.Local, () => SetDisplaySpace(spacePref, DisplaySpace.Local));
            menu.AddItem(s_Contents.worldSpaceContent, space == DisplaySpace.World, () => SetDisplaySpace(spacePref, DisplaySpace.World));
            menu.DropDown(dropdownRect);
        }

        static void SetDisplaySpace(SavedInt spacePref, DisplaySpace space)
        {
            spacePref.value = (int)space;
            InspectorWindow.RepaintAllInspectors(); // Keep all open inspectors in sync.
        }

        [MenuItem("CONTEXT/Transform/Display All in Local Space", false, 200000)]
        static void DisplayAllInLocalSpace() => SetAllDisplaySpaces(DisplaySpace.Local);

        [MenuItem("CONTEXT/Transform/Display All in Local Space", true)]
        static bool CanDisplayAllInLocalSpace() => !AreAllDisplaySpaces(DisplaySpace.Local);

        [MenuItem("CONTEXT/Transform/Display All in World Space", false, 200001)]
        static void DisplayAllInWorldSpace() => SetAllDisplaySpaces(DisplaySpace.World);

        [MenuItem("CONTEXT/Transform/Display All in World Space", true)]
        static bool CanDisplayAllInWorldSpace() => !AreAllDisplaySpaces(DisplaySpace.World);

        static bool AreAllDisplaySpaces(DisplaySpace space)
        {
            return GetDisplaySpace(s_PositionSpacePref) == space &&
                GetDisplaySpace(s_RotationSpacePref) == space &&
                GetDisplaySpace(s_ScaleSpacePref) == space;
        }

        static void SetAllDisplaySpaces(DisplaySpace space)
        {
            s_PositionSpacePref.value = (int)space;
            s_RotationSpacePref.value = (int)space;
            s_ScaleSpacePref.value = (int)space;
            InspectorWindow.RepaintAllInspectors();
        }

        void PositionField(DisplaySpace space)
        {
            Rect rect = EditorGUILayout.GetControlRect(true);
            GUIContent label = space == DisplaySpace.World ? s_Contents.worldPositionContent : s_Contents.positionContent;
            SpaceDropdown(rect, label, s_PositionSpacePref);

            if (space == DisplaySpace.World)
                m_WorldSpaceGUI.WorldPositionField(rect, label);
            else
                EditorGUI.PropertyField(rect, m_Position, label);
        }

        void RotationField(DisplaySpace space)
        {
            Rect rect = EditorGUILayout.GetControlRect(true);
            GUIContent label = space == DisplaySpace.World ? s_Contents.worldRotationContent : s_Contents.rotationContent;
            // The local rotation field can add the rotation order to its label, which the arrow must not overlap.
            SpaceDropdown(rect, space == DisplaySpace.World ? label : EditorGUIUtility.TempContent(m_RotationGUI.GetLabelText()), s_RotationSpacePref);

            if (space == DisplaySpace.World)
                m_WorldSpaceGUI.WorldRotationField(rect, label);
            else
                m_RotationGUI.RotationField(rect, false);
        }

        void ScaleField(DisplaySpace space)
        {
            Rect rect = EditorGUILayout.GetControlRect(true);
            GUIContent label = space == DisplaySpace.World ? s_Contents.worldScaleContent : s_Contents.scaleContent;
            // The local scale field draws its constrain proportions toggle at the end of its label area.
            float reservedLabelWidth = space == DisplaySpace.Local ? EditorStyles.toggle.CalcSize(GUIContent.none).x + EditorGUI.kDefaultSpacing : 0;
            SpaceDropdown(rect, label, s_ScaleSpacePref, reservedLabelWidth);

            if (space == DisplaySpace.World)
                m_WorldSpaceGUI.WorldScaleField(rect, label);
            else
                LocalScaleField(rect);
        }

        void LocalScaleField(Rect rect)
        {
            Transform t = target as Transform;

            if (t != null && m_ConstrainProportionsScale.Initialize(serializedObject.targetObjects) && m_ConstrainProportionsScaleProperty != null)
            {
                //AxisModified values [-1;2] : [none, x, y, z]
                int axisModified = -1;
                var mixedFields = ConstrainProportionsTransformScale.GetMixedValueFields(m_Scale);
                Vector3 currentScale = m_ConstrainProportionsScale.DoGUI(rect,
                    s_Contents.scaleContent, m_Scale.vector3Value, serializedObject.targetObjects, ref axisModified, m_Scale, m_ConstrainProportionsScaleProperty);
                var mixedFieldsAfterGUI = ConstrainProportionsTransformScale.GetMixedValueFields(m_Scale);

                if (currentScale != m_Scale.vector3Value || mixedFields != mixedFieldsAfterGUI)
                {
                    if (serializedObject.targetObjectsCount > 1)
                    {
                        if (mixedFields != mixedFieldsAfterGUI)
                        {
                            axisModified = -1;
                            for (int i = 0; i < 3; i++)
                            {
                                if (ConstrainProportionsTransformScale.IsBit(mixedFields, i) && !ConstrainProportionsTransformScale.IsBit(mixedFieldsAfterGUI, i))
                                {
                                    axisModified = i;
                                    break;
                                }
                            }
                        }

                        if (axisModified != -1)
                            m_IsScaleDirty = ConstrainProportionsTransformScale.HandleMultiSelectionScaleChanges(
                                m_Scale.vector3Value,
                                currentScale, m_ConstrainProportionsScale.constrainProportionsScale,
                                serializedObject.targetObjects, ref axisModified);
                    }

                    if (currentScale != m_Scale.vector3Value)
                        m_Scale.vector3Value = currentScale;
                }
            }
            else
            {
                EditorGUI.PropertyField(rect, m_Scale, s_Contents.scaleContent);
            }
        }
    }
}
