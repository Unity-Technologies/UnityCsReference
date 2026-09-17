// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UnityEditor
{
    [EditorTool("Edit Box Collider", typeof(BoxCollider))]
    class BoxPrimitiveColliderTool : PrimitiveColliderTool<BoxCollider>
    {
        readonly BoxBoundsHandle m_BoundsHandle = new BoxBoundsHandle();
        protected override PrimitiveBoundsHandle boundsHandle { get { return m_BoundsHandle; } }

        protected override void CopyColliderPropertiesToHandle(BoxCollider collider)
        {
            m_BoundsHandle.center = TransformColliderCenterToHandleSpace(collider.transform, collider.center);
            m_BoundsHandle.size = Vector3.Scale(collider.size, collider.transform.lossyScale);
        }

        protected override void CopyHandlePropertiesToCollider(BoxCollider collider)
        {
            collider.center = TransformHandleCenterToColliderSpace(collider.transform, m_BoundsHandle.center);
            Vector3 size = Vector3.Scale(m_BoundsHandle.size, InvertScaleVector(collider.transform.lossyScale));
            size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            collider.size = size;
        }
    }

    static class BoxColliderFitUtility
    {
        public static void FitTargets(IEnumerable<UnityObject> targets, bool fitToChildren)
        {
            foreach (var t in targets)
            {
                if (t is not BoxCollider collider)
                    continue;

                if (fitToChildren)
                    FitToChildren(collider);
                else
                    FitToSelf(collider);
            }
        }

        public static bool HasAnyTargetWithChildren(IEnumerable<UnityObject> targets)
        {
            foreach (var t in targets)
            {
                if (t is BoxCollider collider && collider != null && collider.transform.childCount > 0)
                    return true;
            }
            return false;
        }

        public static bool HasAnyTargetWithRenderer(IEnumerable<UnityObject> targets)
        {
            foreach (var t in targets)
            {
                if (t is BoxCollider collider && collider != null
                    && collider.TryGetComponent(out Renderer renderer)
                    && renderer.localBounds.extents != Vector3.zero)
                    return true;
            }
            return false;
        }

        public static void FitToSelf(BoxCollider collider)
        {
            if (collider == null)
                return;

            Bounds bounds = new Bounds();
            if (AABBUtility.CalculateLocalAABBFromGameObject(collider.gameObject, ref bounds))
            {
                Undo.RecordObject(collider, "Fit Box Collider to Self");
                collider.center = bounds.center;
                collider.size = bounds.size;
                EditorUtility.SetDirty(collider);
            }
        }

        public static void FitToChildren(BoxCollider collider)
        {
            if (collider == null)
                return;

            Bounds bounds = new Bounds();
            if (AABBUtility.CalculateCombinedAABBFromHierarchy(collider.gameObject, ref bounds, includeRoot: true))
            {
                Undo.RecordObject(collider, "Fit Box Collider to Children");
                collider.center = bounds.center;
                collider.size = bounds.size;
                EditorUtility.SetDirty(collider);
            }
        }
    }

    [CustomEditor(typeof(BoxCollider))]
    [CanEditMultipleObjects]
    class BoxColliderEditor : Collider3DEditorBase
    {
        SerializedProperty m_Center;
        SerializedProperty m_Size;

        private static class Styles
        {
            public static readonly GUIContent sizeContent = L10n.TextContent("Size", "The size of the Collider in the X, Y, Z directions.", null, null);
            public static readonly GUIContent fitToSelfContent = L10n.TextContent("Fit to Self", "Resize the collider to match this GameObject's renderer bounds.", null, null);
            public static readonly GUIContent fitToSelfDisabledContent = L10n.TextContent("Fit to Self", "No renderer to fit to.", null, null);
            public static readonly GUIContent fitToChildrenContent = L10n.TextContent("Fit to Children", "Resize the collider to match the combined renderer bounds of this GameObject and its children.", null, null);
            public static readonly GUIContent fitToChildrenDisabledContent = L10n.TextContent("Fit to Children", "No children to fit to.", null, null);
        }

        public override void OnEnable()
        {
            base.OnEnable();

            m_Center = serializedObject.FindProperty("m_Center");
            m_Size = serializedObject.FindProperty("m_Size");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.EditorToolbarForTarget(L10n.TempContent("Edit Collider", null), this);
            GUILayout.Space(5);
            EditorGUILayout.PropertyField(m_IsTrigger, BaseStyles.triggerContent);
            EditorGUILayout.PropertyField(m_ProvidesContacts, BaseStyles.providesContacts);
            EditorGUILayout.PropertyField(m_Material, BaseStyles.materialContent);
            EditorGUILayout.PropertyField(m_Center, BaseStyles.centerContent);
            EditorGUILayout.PropertyField(m_Size, Styles.sizeContent);
            DrawFitButtons();

            ShowLayerOverridesProperties();
            serializedObject.ApplyModifiedProperties();
        }

        void DrawFitButtons()
        {
            const float spacing = 2f;

            Rect fieldRect = EditorGUILayout.GetControlRect();
            fieldRect.xMin += EditorGUIUtility.labelWidth;

            float buttonWidth = (fieldRect.width - spacing) * 0.5f;
            Rect selfRect = new Rect(fieldRect.x, fieldRect.y, buttonWidth, fieldRect.height);
            Rect childrenRect = new Rect(selfRect.xMax + spacing, fieldRect.y, buttonWidth, fieldRect.height);

            bool canFitSelf = BoxColliderFitUtility.HasAnyTargetWithRenderer(targets);
            using (new EditorGUI.DisabledScope(!canFitSelf))
            {
                if (GUI.Button(selfRect, canFitSelf ? Styles.fitToSelfContent : Styles.fitToSelfDisabledContent))
                {
                    BoxColliderFitUtility.FitTargets(targets, fitToChildren: false);
                    serializedObject.Update();
                }
            }

            bool canFitChildren = BoxColliderFitUtility.HasAnyTargetWithChildren(targets);
            using (new EditorGUI.DisabledScope(!canFitChildren))
            {
                if (GUI.Button(childrenRect, canFitChildren ? Styles.fitToChildrenContent : Styles.fitToChildrenDisabledContent))
                {
                    BoxColliderFitUtility.FitTargets(targets, fitToChildren: true);
                    serializedObject.Update();
                }
            }
        }
    }
}
