// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Animations;

namespace UnityEditor
{
    [CustomEditor(typeof(ParentConstraint))]
    [CanEditMultipleObjects]
    internal class ParentConstraintEditor : ConstraintEditorBase
    {
        private SerializedProperty m_TranslationAtRest;
        private SerializedProperty m_TranslationOffsets;
        private SerializedProperty m_RotationAtRest;
        private SerializedProperty m_RotationOffsets;
        private SerializedProperty m_Weight;
        private SerializedProperty m_IsContraintActive;
        private SerializedProperty m_IsLocked;
        private SerializedProperty m_Sources;

        private ReorderableList m_OffsetList;

        internal override SerializedProperty atRest { get { throw new NotImplementedException(); } }
        internal override SerializedProperty offset { get { throw new NotImplementedException(); } }
        internal override SerializedProperty weight { get { return m_Weight; } }
        internal override SerializedProperty isContraintActive { get { return m_IsContraintActive; } }
        internal override SerializedProperty isLocked { get { return m_IsLocked; } }
        internal override SerializedProperty sources { get { return m_Sources; } }

        private class Styles : ConstraintStyleBase
        {
            GUIContent m_RestTranslation = EditorGUIUtility.TextContent("Position At Rest");
            GUIContent m_TranslationOffset = EditorGUIUtility.TextContent("Position Offset");

            GUIContent m_RestRotation = EditorGUIUtility.TextContent("Rotation At Rest");
            GUIContent m_RotationOffset = EditorGUIUtility.TextContent("Rotation Offset");

            GUIContent m_TranslationAxes = EditorGUIUtility.TextContent("Freeze Position Axes");
            GUIContent m_RotationAxes = EditorGUIUtility.TextContent("Freeze Rotation Axes");

            GUIContent m_DefaultSourceName = EditorGUIUtility.TextContent("None");

            GUIContent m_SourceOffsets = EditorGUIUtility.TextContent("Source Offsets");

            public override GUIContent AtRest { get { throw new NotImplementedException(); } }
            public override GUIContent Offset { get { throw new NotImplementedException(); } }
            public GUIContent TranslationAtRest { get { return m_RestTranslation; } }
            public GUIContent RotationAtRest { get { return m_RestRotation; } }
            public GUIContent TranslationOffset { get { return m_TranslationOffset; } }
            public GUIContent RotationOffset { get { return m_RotationOffset; } }
            public GUIContent FreezeTranslationAxes { get { return m_TranslationAxes; } }
            public GUIContent FreezeRotationAxes { get { return m_RotationAxes; } }
            public GUIContent SourceOffsets { get { return m_SourceOffsets; } }
            public GUIContent DefaultSourceName { get { return m_DefaultSourceName; } }
        }

        private static Styles s_Style;

        public void OnEnable()
        {
            if (s_Style == null)
                s_Style = new Styles();

            m_TranslationAtRest = serializedObject.FindProperty("m_TranslationAtRest");
            m_TranslationOffsets = serializedObject.FindProperty("m_TranslationOffsets");
            m_RotationAtRest = serializedObject.FindProperty("m_RotationAtRest");
            m_RotationOffsets = serializedObject.FindProperty("m_RotationOffsets");
            m_Weight = serializedObject.FindProperty("m_Weight");
            m_IsContraintActive = serializedObject.FindProperty("m_IsContraintActive");
            m_IsLocked = serializedObject.FindProperty("m_IsLocked");
            m_Sources = serializedObject.FindProperty("m_Sources");

            m_OffsetList = new ReorderableList(serializedObject, sources, false, false, false, false);
            m_OffsetList.drawHeaderCallback += rect => EditorGUI.LabelField(rect, s_Style.SourceOffsets);
            m_OffsetList.drawElementCallback += DrawOffsetElementCallback;
            m_OffsetList.elementHeightCallback += OnElementHeightCallback;

            OnEnable(s_Style);

            m_OffsetList.index = base.selectedSourceIndex;
        }

        private void DrawOffsetElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            var translationOffset = m_TranslationOffsets.GetArrayElementAtIndex(index);
            var rotationOffset = m_RotationOffsets.GetArrayElementAtIndex(index);
            var element = m_Sources.GetArrayElementAtIndex(index);
            var sourceElement = element.FindPropertyRelative("sourceTransform");
            var sourceName = s_Style.DefaultSourceName;
            if (sourceElement.objectReferenceValue != null)
            {
                sourceName = EditorGUIUtility.TextContent(sourceElement.objectReferenceValue.name);
            }

            Rect drawRect = rect;
            drawRect.height = EditorGUIUtility.singleLineHeight;
            drawRect.y += EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.LabelField(drawRect, sourceName);

            drawRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(drawRect, translationOffset, s_Style.TranslationOffset);
            drawRect.y += EditorGUIUtility.wideMode ? EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing : 2 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
            EditorGUI.PropertyField(drawRect, rotationOffset, s_Style.RotationOffset);
        }

        protected override void OnRemoveCallback(ReorderableList list)
        {
            int index = list.index;
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
            m_TranslationOffsets.DeleteArrayElementAtIndex(index);
            m_RotationOffsets.DeleteArrayElementAtIndex(index);
            if (selectedSourceIndex >= list.serializedProperty.arraySize)
            {
                SelectSource(list.serializedProperty.arraySize - 1);
            }
        }

        protected override void OnAddCallback(ReorderableList list)
        {
            var index = list.serializedProperty.arraySize;
            ReorderableList.defaultBehaviours.DoAddButton(list);
            m_TranslationOffsets.arraySize++;
            m_RotationOffsets.arraySize++;

            var source = list.serializedProperty.GetArrayElementAtIndex(index);
            source.FindPropertyRelative("sourceTransform").objectReferenceValue = null;
            source.FindPropertyRelative("weight").floatValue = 1.0f;

            SelectSource(index);
        }

        protected override void OnReorderCallback(ReorderableList list, int oldActiveElement, int newActiveElement)
        {
            m_TranslationOffsets.MoveArrayElement(oldActiveElement, newActiveElement);
            m_RotationOffsets.MoveArrayElement(oldActiveElement, newActiveElement);
            m_SerializedObject.ApplyModifiedProperties();
            m_SerializedObject.Update();
        }

        protected override void OnSelectedCallback(ReorderableList list)
        {
            base.OnSelectedCallback(list);
            m_OffsetList.index = list.index;
        }

        static readonly float kListItemHeight = 3 * (EditorGUIUtility.singleLineHeight + 2 * EditorGUIUtility.standardVerticalSpacing);
        static readonly float kListItemNarrowHeight = 5 * (EditorGUIUtility.singleLineHeight + 2 * EditorGUIUtility.standardVerticalSpacing);
        private float OnElementHeightCallback(int index)
        {
            if (EditorGUIUtility.wideMode)
            {
                return kListItemHeight;
            }

            return kListItemNarrowHeight;
        }

        internal override void OnValueAtRestChanged()
        {
            foreach (var t in targets)
            {
                (t as IConstraintInternal).transform.localPosition = m_TranslationAtRest.vector3Value;
                (t as IConstraintInternal).transform.SetLocalEulerAngles(m_RotationAtRest.vector3Value, RotationOrder.OrderZXY);
            }
        }

        internal override void ShowFreezeAxesControl()
        {
            Rect drawRectT = EditorGUILayout.GetControlRect(true, EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector3, s_Style.FreezeTranslationAxes), EditorStyles.toggle);
            EditorGUI.MultiPropertyField(drawRectT, s_Style.Axes, serializedObject.FindProperty("m_AffectTranslationX"), s_Style.FreezeTranslationAxes);

            Rect drawRectR = EditorGUILayout.GetControlRect(true, EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector3, s_Style.FreezeRotationAxes), EditorStyles.toggle);
            EditorGUI.MultiPropertyField(drawRectR, s_Style.Axes, serializedObject.FindProperty("m_AffectRotationX"), s_Style.FreezeRotationAxes);
        }

        internal override void ShowValueAtRest(ConstraintStyleBase style)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_TranslationAtRest, (style as Styles).TranslationAtRest);
            EditorGUILayout.PropertyField(m_RotationAtRest, (style as Styles).RotationAtRest);
            if (EditorGUI.EndChangeCheck())
            {
                OnValueAtRestChanged();
            }
        }

        internal override void ShowOffset<T>(ConstraintStyleBase style)
        {
            using (new EditorGUI.DisabledGroupScope(isLocked.boolValue))
            {
                EditorGUI.BeginChangeCheck();
                m_OffsetList.DoLayoutList();
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var t in targets)
                        (t as IConstraintInternal).UserUpdateOffset();
                }
            }
        }

        public override void OnInspectorGUI()
        {
            if (s_Style == null)
                s_Style = new Styles();

            serializedObject.Update();

            ShowConstraintEditor<ParentConstraint>(s_Style);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
