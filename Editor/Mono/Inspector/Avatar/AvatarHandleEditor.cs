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
    internal class AvatarHandleEditor : AvatarSubEditor
    {
        // Code below has not been updated to match refactoring work.
        // It will have to be updated and cleaned up when we start to implement support for these editors.
        /*class Styles
        {
            static public int FoldoutSize = 20;

            public int AlignTransform = FoldoutSize;

            public GUIContent Position = EditorGUIUtility.TextContent ("Position");
            public GUIContent Rotation = EditorGUIUtility.TextContent ("Rotation");
            public GUIContent Scale = EditorGUIUtility.TextContent ("Scale");
            public GUIContent LookAt = EditorGUIUtility.TextContent ("Look At");

            public GUILayoutOption[] ToggleSize = new GUILayoutOption[] { GUILayout.MaxWidth (FoldoutSize) };

        }

        static Styles styles { get { if (s_Styles == null) s_Styles = new Styles (); return s_Styles; } }
        static Styles s_Styles;

        Transform[] m_Transforms;

        bool[] m_Toggle;

        SerializedProperty m_HandleList;

        string[] m_HumanBoneName;

        string[] m_BoneName;

        static string sHandles = "m_HumanDescription.m_Handles";
        static string sLookAt = "m_LookAt";

        static string sDefaultHandleName = "Handle";

        public AvatarHandleEditor ()
        {
            m_Transforms = null;
            m_Toggle = null;
            m_BoneName = null;

            m_HumanBoneName = HumanTrait.BoneName;
        }

        internal void Initialize ()
        {
            if (m_HandleList != null && m_HandleList.isArray)
            {
                m_Toggle = new bool[m_HandleList.arraySize];
                for (int i = 0; i < m_HandleList.arraySize; i++)
                {
                    m_Toggle[i] = false;
                }
            }

            SerializedProperty humanBody = serializedObject.FindProperty (sHuman);
            if (humanBody.isArray)
            {
                m_BoneName = new string[0];
                for (int i = 0; i < humanBody.arraySize; i++)
                {
                    SerializedProperty boneProperty = humanBody.GetArrayElementAtIndex (i);
                    if (boneProperty != null)
                    {
                        SerializedProperty humanNameProperty = boneProperty.FindPropertyRelative (sHumanName);
                        SerializedProperty boneNameProperty = boneProperty.FindPropertyRelative (sBoneName);
                        if (boneNameProperty != null && humanNameProperty != null)
                        {
                            // Add only human body bone, hand are not supported for now
                            int index = ArrayUtility.FindIndex (m_HumanBoneName, delegate (string s) { return s == humanNameProperty.stringValue; });
                            if (index != -1 && index < (int)HumanBodyBones.LastBone)
                                ArrayUtility.Add (ref m_BoneName, boneNameProperty.stringValue);
                        }
                    }
                }
            }
        }

        public override void OnEnable (AvatarInspector inspector)
        {
            base.OnEnable (inspector);

            m_HandleList = serializedObject.FindProperty (sHandles);

            m_Transforms = Object.FindSceneObjectsOfType (typeof (Transform)) as Transform[];

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

        public override void OnInspectorGUI ()
        {
            bool wasEnabled = GUI.enabled;

            GUI.enabled = wasEnabled && avatarAsset.IsValid ();

            DisplayHandles ();

            ApplyRevertGUI ();

            GUI.enabled = wasEnabled;
        }

        public override void OnSceneGUI ()
        {
            if (gameObject != null)
            {
                Animator animator = gameObject.GetComponent (typeof (Animator)) as Animator;
                if (m_Transforms != null && animator != null && m_HandleList != null && m_HandleList.isArray)
                {
                    for (int i = 0; i < m_HandleList.arraySize; i++)
                    {
                        SerializedProperty handleP = m_HandleList.GetArrayElementAtIndex (i);
                        SerializedProperty positionP = handleP.FindPropertyRelative (sPosition);
                        SerializedProperty rotationP = handleP.FindPropertyRelative (sRotation);
                        SerializedProperty scaleP = handleP.FindPropertyRelative (sScale);
                        //SerializedProperty nameP = handleP.FindPropertyRelative (sName);
                        SerializedProperty boneNameP = handleP.FindPropertyRelative (sBoneName);

                        int index = ArrayUtility.FindIndex (m_Transforms, delegate (Transform t) { return t.name == boneNameP.stringValue; });
                        if (index != -1)
                        {
                            Vector3 boneGT = m_Transforms[index].position;
                            Quaternion boneGQ = m_Transforms[index].rotation;
                            Quaternion boneInvGQ = Quaternion.Inverse (boneGQ);
                            Vector3 boneGS = m_Transforms[index].lossyScale;

                            Vector3 handleLT = positionP.vector3Value;
                            handleLT.x *= boneGS.x;
                            handleLT.y *= boneGS.y;
                            handleLT.z *= boneGS.z;

                            Vector3 handleGT = boneGT + (boneGQ * handleLT);
                            Quaternion handleGQ = boneGQ * rotationP.quaternionValue;
                            Vector3 handleGS = scaleP.vector3Value;

                            MathUtils.QuaternionNormalize (ref handleGQ);

                            handleGS.x *= boneGS.x;
                            handleGS.y *= boneGS.y;
                            handleGS.z *= boneGS.z;

                            switch (Tools.current)
                            {
                                case Tool.Move:
                                    {
                                        Vector3 newHandleGT = Handles.PositionHandle (handleGT, handleGQ);
                                        if (newHandleGT != handleGT)
                                        {
                                            handleLT = boneInvGQ * (newHandleGT - boneGT);
                                            handleLT.x /= boneGS.x;
                                            handleLT.y /= boneGS.y;
                                            handleLT.z /= boneGS.z;

                                            positionP.vector3Value = handleLT;

                                            foreach (AvatarInspector ai in Resources.FindObjectsOfTypeAll (typeof (AvatarInspector)))
                                                ai.Repaint ();
                                        }
                                        break;
                                    }
                                case Tool.Rotate:
                                    {
                                        Quaternion newHandleGQ = Handles.RotationHandle (handleGQ, handleGT);
                                        if (newHandleGQ != handleGQ)
                                        {
                                            Quaternion handleLQ = boneInvGQ * newHandleGQ;
                                            MathUtils.QuaternionNormalize (ref handleLQ);
                                            rotationP.quaternionValue = handleLQ;

                                            foreach (AvatarInspector ai in Resources.FindObjectsOfTypeAll (typeof (AvatarInspector)))
                                                ai.Repaint ();
                                        }
                                        break;
                                    }
                                case Tool.Scale:
                                    {
                                        Vector3 newHandleGS = Handles.ScaleHandle (handleGS, handleGT, handleGQ, 0.5f);
                                        if (newHandleGS != handleGS)
                                        {
                                            Vector3 handleLS = newHandleGS;
                                            handleLS.x /= boneGS.x;
                                            handleLS.y /= boneGS.y;
                                            handleLS.z /= boneGS.z;

                                            scaleP.vector3Value = handleLS;

                                            foreach (AvatarInspector ai in Resources.FindObjectsOfTypeAll (typeof (AvatarInspector)))
                                                ai.Repaint ();
                                        }
                                        break;
                                    }
                            }
                        }
                    }
                }
            }
        }

        protected void DisplayHandles ()
        {
            int deleteHandle = -1;
            GUILayout.BeginVertical (GUI.skin.label);
            {
                if (m_HandleList != null && m_HandleList.isArray)
                {
                    for (int i = 0; i < m_HandleList.arraySize; i++)
                    {
                        SerializedProperty handleP = m_HandleList.GetArrayElementAtIndex (i);
                        SerializedProperty nameP = handleP.FindPropertyRelative (sName);
                        SerializedProperty boneNameP = handleP.FindPropertyRelative (sBoneName);

                        GUILayout.BeginHorizontal (GUI.skin.label);
                        {
                            m_Toggle[i] = GUILayout.Toggle (m_Toggle[i], "", EditorStyles.foldout, styles.ToggleSize);

                            string name = nameP.stringValue;
                            name = EditorGUILayout.TextField (name);
                            if (name != nameP.stringValue)
                            {
                                string[] names = GetHandlesName ();
                                nameP.stringValue = GenerateUniqueHandleName (name, names);
                            }

                            int option = ArrayUtility.FindIndex (m_BoneName, delegate (string s){return s == boneNameP.stringValue;} );
                            option = EditorGUILayout.Popup (option, m_BoneName);
                            if (boneNameP.stringValue != m_BoneName[option])
                            {
                                boneNameP.stringValue = m_BoneName[option];
                                SceneView.RepaintAll ();
                            }

                            if (GUILayout.Button (GUIContent.none, "OL Minus"))
                                deleteHandle = i;
                        }
                        GUILayout.EndHorizontal ();

                        if (m_Toggle[i])
                        {
                            SerializedProperty boneP = FindHumanBone (serializedObject, m_HumanBoneName[ (int)HumanBodyBones.Head], false);
                            if (boneP != null)
                            {
                                SerializedProperty avatarBoneNameP = boneP.FindPropertyRelative (sBoneName);
                                if (avatarBoneNameP != null && avatarBoneNameP.stringValue == boneNameP.stringValue)
                                {
                                    GUILayout.BeginHorizontal (GUI.skin.label);
                                    {
                                        GUILayout.Space (styles.AlignTransform);
                                        SerializedProperty lookAtP = handleP.FindPropertyRelative (sLookAt);
                                        bool isLookAt = lookAtP.boolValue;
                                        isLookAt = GUILayout.Toggle (isLookAt, styles.LookAt);
                                        if (isLookAt != lookAtP.boolValue)
                                            lookAtP.boolValue = isLookAt;
                                    }
                                    GUILayout.EndHorizontal ();
                                }
                            }

                            SerializedProperty positionP = handleP.FindPropertyRelative (sPosition);
                            SerializedProperty rotationP = handleP.FindPropertyRelative (sRotation);
                            SerializedProperty scaleP = handleP.FindPropertyRelative (sScale);

                            GUILayout.BeginHorizontal (GUI.skin.label);
                            {
                                GUILayout.Space (styles.AlignTransform);

                                Vector3 value = positionP.vector3Value;
                                value = EditorGUILayout.Vector3Field (styles.Position.text, value);
                                if (value != positionP.vector3Value)
                                {
                                    positionP.vector3Value = value;
                                    SceneView.RepaintAll ();
                                }
                            }
                            GUILayout.EndHorizontal ();

                            GUILayout.BeginHorizontal (GUI.skin.label);
                            {
                                GUILayout.Space (styles.AlignTransform);

                                Vector3 value = rotationP.quaternionValue.eulerAngles;
                                value = EditorGUILayout.Vector3Field (styles.Rotation.text, value);
                                if (value != rotationP.quaternionValue.eulerAngles)
                                {
                                    Quaternion q = new Quaternion ();
                                    q.eulerAngles = value;
                                    rotationP.quaternionValue = q;
                                    SceneView.RepaintAll ();
                                }
                            }
                            GUILayout.EndHorizontal ();

                            GUILayout.BeginHorizontal (GUI.skin.label);
                            {
                                GUILayout.Space (styles.AlignTransform);

                                Vector3 value = scaleP.vector3Value;
                                value = EditorGUILayout.Vector3Field (styles.Scale.text, value);
                                if (value != scaleP.vector3Value)
                                {
                                    scaleP.vector3Value = value;
                                    SceneView.RepaintAll ();
                                }
                            }
                            GUILayout.EndHorizontal ();
                        }
                    }

                    if (GUILayout.Button (GUIContent.none, "OL Plus"))
                    {
                        CreateHandle ();
                    }
                }
            }
            GUILayout.EndVertical ();

            if (deleteHandle != -1 && m_HandleList != null && m_HandleList.isArray)
            {
                m_HandleList.DeleteArrayElementAtIndex (deleteHandle);
            }
        }

        void ComputeLocal (Transform parent, Transform child, out Vector3 localP, out Quaternion localQ, out Vector3 localS)
        {
            Quaternion parentInvGQ = Quaternion.Inverse (parent.rotation);
            Vector3 parentGS = parent.lossyScale;
            localP = (parentInvGQ * (child.position - parent.position));
            localP.x /= parentGS.x;
            localP.y /= parentGS.y;
            localP.z /= parentGS.z;

            localQ = parentInvGQ * child.rotation;

            localS = child.lossyScale;
            localS.x /= parentGS.x;
            localS.y /= parentGS.y;
            localS.z /= parentGS.z;
        }

        protected void CreateHandle ()
        {
            string[] names = GetHandlesName ();
            if (Selection.objects.Length == 2)
            {
                // Find out which one is a human bone and which one is the prop
                Transform bone = null;
                Transform prop = null;
                int boneId = -1;

                for (int objIter = 0; objIter < Selection.objects.Length && bone == null; objIter++)
                {
                    for (int boneIter = 0; boneIter < m_BoneName.Length; boneIter++)
                    {
                        if (Selection.objects[objIter].name == m_BoneName[boneIter])
                        {
                            GameObject go1 = Selection.objects[objIter] as GameObject;
                            GameObject go2 = Selection.objects[ (objIter + 1) % 2] as GameObject;

                            bone = go1.GetComponent<Transform> ();
                            prop = go2.GetComponent<Transform> ();

                            boneId = boneIter;
                            break;
                        }
                    }
                }

                if (bone != null && prop != null)
                {
                    ArrayUtility.Add (ref m_Toggle, false);
                    m_HandleList.arraySize++;

                    SerializedProperty handleP = m_HandleList.GetArrayElementAtIndex (m_HandleList.arraySize - 1);

                    Vector3 localP = Vector3.zero;
                    Quaternion localQ = Quaternion.identity;
                    Vector3 localS = Vector3.one;

                    ComputeLocal (bone, prop, out localP, out localQ, out localS);

                    handleP.FindPropertyRelative (sName).stringValue = GenerateUniqueHandleName (prop.name+"_Handle", names);
                    handleP.FindPropertyRelative (sBoneName).stringValue = m_BoneName[boneId];
                    handleP.FindPropertyRelative (sPosition).vector3Value = localP;
                    handleP.FindPropertyRelative (sRotation).quaternionValue = localQ;
                    handleP.FindPropertyRelative (sScale).vector3Value = localS;
                }
            }
            else
            {
                ArrayUtility.Add (ref m_Toggle, false);
                m_HandleList.arraySize++;

                SerializedProperty handleP = m_HandleList.GetArrayElementAtIndex (m_HandleList.arraySize - 1);

                handleP.FindPropertyRelative (sName).stringValue = GenerateUniqueHandleName (sDefaultHandleName, names);
                handleP.FindPropertyRelative (sBoneName).stringValue = m_BoneName[0];
                handleP.FindPropertyRelative (sPosition).vector3Value = Vector3.zero;
                handleP.FindPropertyRelative (sRotation).quaternionValue = Quaternion.identity;
                handleP.FindPropertyRelative (sScale).vector3Value = Vector3.one;
            }
        }

        string[] GetHandlesName ()
        {
            string[] names = new string[0];
            if (m_HandleList != null && m_HandleList.isArray)
            {
                for (int i = 0; i < m_HandleList.arraySize; i++)
                {
                    SerializedProperty handleP = m_HandleList.GetArrayElementAtIndex (i);
                    SerializedProperty nameP = handleP.FindPropertyRelative (sName);

                    ArrayUtility.Add (ref names, nameP.stringValue);
                }
            }
            return names;
        }

        string GenerateUniqueHandleName (string name, string[] names)
        {
            string attemptName = name;
            int attempt = 1;

            bool collision = true;
            while (collision)
            {
                collision = false;
                foreach (string s in names)
                {
                    if (attemptName == s)
                    {
                        attemptName = name + attempt.ToString ();
                        attempt++;
                        collision = true;
                        break;
                    }
                }
            }

            return attemptName;
        }*/
    }
}
