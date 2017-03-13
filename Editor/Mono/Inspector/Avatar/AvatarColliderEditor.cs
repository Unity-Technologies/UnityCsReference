// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class AvatarColliderEditor : AvatarSubEditor
    {
        // Code below has not been updated to match refactoring work.
        // It will have to be updated and cleaned up when we start to implement support for these editors.
        /*class Styles
        {
            public GUIContent CreateCollider = EditorGUIUtility.TextContent ("Create Collider");
            public GUIContent UserLayer = EditorGUIUtility.TextContent ("Colliders physics layer");

            public GUILayoutOption[] CreateColliderSize = new GUILayoutOption[] { GUILayout.MaxWidth (60) };
        }

        static Styles styles { get { if (s_Styles == null) s_Styles = new Styles (); return s_Styles; } }
        static Styles s_Styles;

        SerializedProperty[] m_Position;
        SerializedProperty[] m_Rotation;
        SerializedProperty[] m_Scale;

        SerializedProperty m_CreateCollider;
        SerializedProperty m_UserLayer;

        //string[] m_HumanBoneName;

        int m_HumanBoneCount;

        protected Transform[] m_Bones;

        static string sCreateCollider = "m_CreateCollider";
        static string sUserLayer = "m_UserLayer";
        static string sColliderPosition = "m_ColliderPosition";
        static string sColliderRotation = "m_ColliderRotation";
        static string sColliderScale = "m_ColliderScale";

        public AvatarColliderEditor ()
        {
            m_CreateCollider = null;
            m_UserLayer = null;

            m_HumanBoneCount = HumanTrait.BoneCount;

            m_Position = new SerializedProperty[m_HumanBoneCount];
            m_Rotation = new SerializedProperty[m_HumanBoneCount];
            m_Scale = new SerializedProperty[m_HumanBoneCount];
            m_Bones = new Transform[m_HumanBoneCount];
            for (int i = 0; i < m_HumanBoneCount; i++)
            {
                m_Position[i] = null;
                m_Rotation[i] = null;
                m_Scale[i] = null;
                m_Bones[i] = null;
            }
        }

        internal void Initialize ()
        {
            Transform[] allTransforms = null;

            allTransforms = Object.FindSceneObjectsOfType (typeof (Transform)) as Transform[];

            //string path = AssetDatabase.GetAssetPath (m_AvatarAsset);
            //GameObject mainAsset = AssetDatabase.LoadMainAssetAtPath (path) as GameObject;
            //if (mainAsset != null)
            //{
            //  Transform root = mainAsset.GetComponent (typeof (Transform)) as Transform;
            //  if (root)
            //  {
            //      allTransforms = new Transform[0];
            //      ArrayUtility.Add (ref allTransforms, root);
            //      for (int i = 0; i < allTransforms.Length; i++)
            //      {
            //          for (int j = 0; j < allTransforms[i].GetChildCount (); j++)
            //              ArrayUtility.Add (ref allTransforms, allTransforms[i].GetChild (j));
            //      }
            //  }
            //}

            SerializedProperty humanBody = serializedObject.FindProperty (sHuman);
            if (humanBody.isArray)
            {
                for (int i = 0; i < humanBody.arraySize; i++)
                {
                    SerializedProperty boneProperty = humanBody.GetArrayElementAtIndex (i);
                    if (boneProperty != null)
                    {
                        SerializedProperty positionProperty = boneProperty.FindPropertyRelative (sColliderPosition);
                        SerializedProperty rotationProperty = boneProperty.FindPropertyRelative (sColliderRotation);
                        SerializedProperty scaleProperty = boneProperty.FindPropertyRelative (sColliderScale);
                        SerializedProperty boneNameProperty = boneProperty.FindPropertyRelative (sBoneName);

                        if (positionProperty != null && rotationProperty != null && scaleProperty != null)
                        {
                            m_Position[i] = positionProperty;
                            m_Rotation[i] = rotationProperty;
                            m_Scale[i] = scaleProperty;
                        }

                        if (boneNameProperty != null && allTransforms != null)
                            m_Bones[i] = ArrayUtility.Find (allTransforms, delegate (Transform t) { return t != null && t.name == boneNameProperty.stringValue; });
                    }
                }
            }
        }

        public override void OnEnable (AvatarInspector inspector)
        {
            base.OnEnable (inspector);
            m_CreateCollider = serializedObject.FindProperty (sCreateCollider);
            m_UserLayer = serializedObject.FindProperty (sUserLayer);

            Initialize ();

            if (gameObject)
            {
                Animator animator = gameObject.GetComponent (typeof (Animator)) as Animator;
                if (animator != null)
                {
                    animator.WriteDefaultPose ();
                    SceneView.RepaintAll ();
                }
            }
        }

        internal void LayerField (SerializedProperty property, GUIContent label)
        {
            float height = EditorGUI.GetSinglePropertyHeight (property, label);
            Rect position = GUILayoutUtility.GetRect (10, height);
            label = EditorGUI.BeginProperty (position, label, property);

            EditorGUI.BeginChangeCheck ();
            int newValue = EditorGUI.LayerField (position, label, property.intValue);
            if (EditorGUI.EndChangeCheck ())
                property.intValue = newValue;

            EditorGUI.EndProperty ();
        }

        public override void OnInspectorGUI ()
        {
            bool wasEnabled = GUI.enabled;

            GUI.enabled = wasEnabled && avatarAsset.IsValid ();

            GUILayout.BeginVertical (GUI.skin.label);
            {
                EditorGUILayout.Space ();
                EditorGUILayout.PropertyField (m_CreateCollider, styles.CreateCollider);
                LayerField (m_UserLayer, styles.UserLayer);

                //EditorGUILayout.PropertyField (m_UserLayer, styles.UserLayer);
            }
            GUILayout.EndVertical ();

            ApplyRevertGUI ();

            GUI.enabled = wasEnabled;
        }

        public override void OnSceneGUI ()
        {
            if (gameObject != null)
            {
                Animator animator = gameObject.GetComponent (typeof (Animator)) as Animator;
                if (animator != null)
                {
                    Avatar avatar = animator.GetAvatar ();

                    for (int i = 0; i < m_HumanBoneCount; i++)
                    {
                        if (HumanTrait.HasCollider (avatar, i))
                        {
                            //AvatarUtility.HumanGetColliderTransform (avatar, i, );
                        }
                    }

                    switch (Tools.current)
                    {
                        case Tool.Move:
                        {
            //              Vector3 newHandleGT = Handles.PositionHandle (handleGT, handleGQ);
            //              if (newHandleGT != handleGT)
            //              {
            //                  handleLT = boneInvGQ * (newHandleGT - boneGT);
            //                  handleLT.x /= boneGS.x;
            //                  handleLT.y /= boneGS.y;
            //                  handleLT.z /= boneGS.z;

            //                  positionP.vector3Value = handleLT;

            //                  foreach (AvatarInspector ai in Resources.FindObjectsOfTypeAll (typeof (AvatarInspector)))
            //                      ai.Repaint ();
            //              }
                            break;
                        }
                        case Tool.Rotate:
                        {
            //              Quaternion newHandleGQ = Handles.RotationHandle (handleGQ, handleGT);
            //              if (newHandleGQ != handleGQ)
            //              {
            //                  Quaternion handleLQ = boneInvGQ * newHandleGQ;
            //                  MathUtils.QuaternionNormalize (ref handleLQ);
            //                  rotationP.quaternionValue = handleLQ;

            //                  foreach (AvatarInspector ai in Resources.FindObjectsOfTypeAll (typeof (AvatarInspector)))
            //                      ai.Repaint ();
            //              }
                            break;
                        }
                        case Tool.Scale:
                        {
            //              Vector3 newHandleGS = Handles.ScaleHandle (handleGS, handleGT, handleGQ, 0.5f);
            //              if (newHandleGS != handleGS)
            //              {
            //                  Vector3 handleLS = newHandleGS;
            //                  handleLS.x /= boneGS.x;
            //                  handleLS.y /= boneGS.y;
            //                  handleLS.z /= boneGS.z;

            //                  scaleP.vector3Value = handleLS;

            //                  foreach (AvatarInspector ai in Resources.FindObjectsOfTypeAll (typeof (AvatarInspector)))
            //                      ai.Repaint ();
            //              }
                            break;
                        }
                    }
                }
            }
        }*/
    }
}
