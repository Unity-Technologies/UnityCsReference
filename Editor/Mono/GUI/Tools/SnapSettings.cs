// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    internal class SnapSettings : EditorWindow
    {
        private static float s_MoveSnapX;
        private static float s_MoveSnapY;
        private static float s_MoveSnapZ;

        private static float s_ScaleSnap;
        private static float s_RotationSnap;

        private static bool s_Initialized;

        private static void Initialize()
        {
            if (!s_Initialized)
            {
                s_MoveSnapX = EditorPrefs.GetFloat("MoveSnapX", 1f);
                s_MoveSnapY = EditorPrefs.GetFloat("MoveSnapY", 1f);
                s_MoveSnapZ = EditorPrefs.GetFloat("MoveSnapZ", 1f);

                s_ScaleSnap = EditorPrefs.GetFloat("ScaleSnap", .1f);
                s_RotationSnap = EditorPrefs.GetFloat("RotationSnap", 15);

                s_Initialized = true;
            }
        }

        public static Vector3 move
        {
            get
            {
                Initialize();
                return new Vector3(s_MoveSnapX, s_MoveSnapY, s_MoveSnapZ);
            }
            set
            {
                EditorPrefs.SetFloat("MoveSnapX", value.x);
                s_MoveSnapX = value.x;
                EditorPrefs.SetFloat("MoveSnapY", value.y);
                s_MoveSnapY = value.y;
                EditorPrefs.SetFloat("MoveSnapZ", value.z);
                s_MoveSnapZ = value.z;
            }
        }

        public static float scale
        {
            get
            {
                Initialize();
                return s_ScaleSnap;
            }
            set
            {
                EditorPrefs.SetFloat("ScaleSnap", value);
                s_ScaleSnap = value;
            }
        }

        public static float rotation
        {
            get
            {
                Initialize();
                return s_RotationSnap;
            }
            set
            {
                EditorPrefs.SetFloat("RotationSnap", value);
                s_RotationSnap = value;
            }
        }

        [MenuItem("Edit/Snap Settings...")]
        static void ShowSnapSettings()
        {
            EditorWindow.GetWindowWithRect<SnapSettings>(new Rect(100, 100, 230, 130), true, "Snap settings");
        }

        class Styles
        {
            public GUIStyle buttonLeft = "ButtonLeft";
            public GUIStyle buttonMid = "ButtonMid";
            public GUIStyle buttonRight = "ButtonRight";
            public GUIContent snapAllAxes = EditorGUIUtility.TextContent("Snap All Axes|Snaps selected objects to the grid");
            public GUIContent snapX = EditorGUIUtility.TextContent("X|Snaps selected objects to the grid on the x axis");
            public GUIContent snapY = EditorGUIUtility.TextContent("Y|Snaps selected objects to the grid on the y axis");
            public GUIContent snapZ = EditorGUIUtility.TextContent("Z|Snaps selected objects to the grid on the z axis");
            public GUIContent moveX = EditorGUIUtility.TextContent("Move X|Grid spacing X");
            public GUIContent moveY = EditorGUIUtility.TextContent("Move Y|Grid spacing Y");
            public GUIContent moveZ = EditorGUIUtility.TextContent("Move Z|Grid spacing Z");
            public GUIContent scale = EditorGUIUtility.TextContent("Scale|Grid spacing for scaling");
            public GUIContent rotation = EditorGUIUtility.TextContent("Rotation|Grid spacing for rotation in degrees");
        }
        static Styles ms_Styles;

        void OnGUI()
        {
            if (ms_Styles == null)
                ms_Styles = new Styles();

            GUILayout.Space(5);

            EditorGUI.BeginChangeCheck();
            Vector3 m = move;
            m.x = EditorGUILayout.FloatField(ms_Styles.moveX, m.x);
            m.y = EditorGUILayout.FloatField(ms_Styles.moveY, m.y);
            m.z = EditorGUILayout.FloatField(ms_Styles.moveZ, m.z);

            if (EditorGUI.EndChangeCheck())
            {
                if (m.x <= 0) m.x = move.x;
                if (m.y <= 0) m.y = move.y;
                if (m.z <= 0) m.z = move.z;
                move = m;
            }
            scale = EditorGUILayout.FloatField(ms_Styles.scale, scale);
            rotation = EditorGUILayout.FloatField(ms_Styles.rotation, rotation);

            GUILayout.Space(5);

            bool snapX = false, snapY = false, snapZ = false;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(ms_Styles.snapAllAxes, ms_Styles.buttonLeft)) { snapX = true; snapY = true; snapZ = true; }
            if (GUILayout.Button(ms_Styles.snapX, ms_Styles.buttonMid)) { snapX = true; }
            if (GUILayout.Button(ms_Styles.snapY, ms_Styles.buttonMid)) { snapY = true; }
            if (GUILayout.Button(ms_Styles.snapZ, ms_Styles.buttonRight)) { snapZ = true; }
            GUILayout.EndHorizontal();

            if (snapX | snapY | snapZ)
            {
                Vector3 scaleTmp = new Vector3(1.0f / move.x, 1.0f / move.y, 1.0f / move.z);

                Undo.RecordObjects(Selection.transforms, "Snap " + (Selection.transforms.Length == 1 ? Selection.activeGameObject.name : " selection") + " to grid");
                foreach (Transform t in Selection.transforms)
                {
                    Vector3 pos = t.position;
                    if (snapX) pos.x = Mathf.Round(pos.x * scaleTmp.x) / scaleTmp.x;
                    if (snapY) pos.y = Mathf.Round(pos.y * scaleTmp.y) / scaleTmp.y;
                    if (snapZ) pos.z = Mathf.Round(pos.z * scaleTmp.z) / scaleTmp.z;
                    t.position = pos;
                }
            }
        }
    }
} // namespace
