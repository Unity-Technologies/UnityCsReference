// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(MeshCollider))]
    [CanEditMultipleObjects]
    internal class MeshColliderEditor : Collider3DEditorBase
    {
        private SerializedProperty m_Mesh;
        private SerializedProperty m_Convex;
        private SerializedProperty m_CookingOptions;

        private static class Styles
        {
            public static readonly GUIContent isTriggerText = EditorGUIUtility.TrTextContent("Is Trigger", "Is this collider a trigger? Triggers are only supported on convex colliders.");
            public static readonly GUIContent convexText = EditorGUIUtility.TrTextContent("Convex", "Is this collider convex?");
            public static readonly GUIContent cookingOptionsText = EditorGUIUtility.TrTextContent("Cooking Options", "Options affecting the result of the mesh processing by the physics engine.");
            public static readonly GUIContent meshText = EditorGUIUtility.TrTextContent("Mesh", "Reference to the Mesh to use for collisions.");
        }

        public override void OnEnable()
        {
            base.OnEnable();

            m_Mesh = serializedObject.FindProperty("m_Mesh");
            m_Convex = serializedObject.FindProperty("m_Convex");
            m_CookingOptions = serializedObject.FindProperty("m_CookingOptions");
        }

        private MeshColliderCookingOptions GetCookingOptions()
        {
            return (MeshColliderCookingOptions)m_CookingOptions.intValue;
        }

        private void SetCookingOptions(MeshColliderCookingOptions cookingOptions)
        {
            m_CookingOptions.intValue = (int)cookingOptions;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_Convex, Styles.convexText);

            if (EditorGUI.EndChangeCheck() && m_Convex.boolValue == false)
            {
                m_IsTrigger.boolValue = false;
            }

            EditorGUI.indentLevel++;
            using (new EditorGUI.DisabledScope(!m_Convex.boolValue))
            {
                EditorGUILayout.PropertyField(m_IsTrigger, Styles.isTriggerText);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.PropertyField(m_ProvidesContacts, BaseStyles.providesContacts);

            using (var horizontal = new EditorGUILayout.HorizontalScope())
            {
                using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, GUIContent.none, m_CookingOptions))
                {
                    EditorGUI.BeginChangeCheck();
                    var newOptions = (MeshColliderCookingOptions)EditorGUILayout.EnumFlagsField(Styles.cookingOptionsText, GetCookingOptions());
                    if (EditorGUI.EndChangeCheck())
                        SetCookingOptions(newOptions);
                }
            }

            EditorGUILayout.PropertyField(m_Material, BaseStyles.materialContent);

            EditorGUILayout.PropertyField(m_Mesh, Styles.meshText);

            ShowLayerOverridesProperties();
            serializedObject.ApplyModifiedProperties();

            ValidateMesh();
        }

        private void ValidateMesh()
        {
            MeshCollider meshCollider = serializedObject.targetObject as MeshCollider;

            if (m_Mesh.hasMultipleDifferentValues)
                return;

            Mesh mesh = m_Mesh.objectReferenceValue as Mesh;
            if (mesh == null)
                return;

            string meshPath = AssetDatabase.GetAssetPath(mesh);
            if (meshCollider.IsScaleBakingRequired())
            {
                if (!mesh.isReadable)
                {
                    InternalEditorUtility.DrawMeshNotReadableHelpBox(mesh, "Mesh Collider");
                }
            }
            else
            {
                var isConvex = meshCollider.convex;
                if (!mesh.HasPreBakeCollisionMesh(isConvex))
                {
                    var collisionName = isConvex ? "convex" : "triangle";
                    var message = $"Mesh '{mesh.name}' used by the Mesh Collider is missing pre-baked {collisionName} collision. In future versions of Unity, the build process will no longer automatically pre bake collision data for Meshes referenced by Mesh Colliders.";

                    if (InternalEditorUtility.CanMeshBeModifiedFromCode(meshPath))
                    {
                        if (InternalEditorUtility.DrawWarningHelpBoxWithButton(
                            EditorGUIUtility.TrTextContent(message),
                            EditorGUIUtility.TrTextContent("Enable Pre-bake Collision"), 200.0f))
                        {
                            InternalEditorUtility.ImportMeshWithPreBakeCollision(mesh, isConvex);
                        }
                    }
                    else
                    {
                        if (InternalEditorUtility.DrawWarningHelpBoxWithButton(
                            EditorGUIUtility.TrTextContent(message),
                            EditorGUIUtility.TrTextContent("View")))
                        {
                            Selection.objects = new UnityEngine.Object[] { mesh };
                        }
                    }
                }
            }
        }
    }
}
