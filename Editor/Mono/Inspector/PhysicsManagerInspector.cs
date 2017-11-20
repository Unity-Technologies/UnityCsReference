// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class LayerMatrixGUI
    {
        public delegate bool GetValueFunc(int layerA, int layerB);
        public delegate void SetValueFunc(int layerA, int layerB, bool val);

        public static void DoGUI(string title, ref bool show, ref Vector2 scrollPos, GetValueFunc getValue, SetValueFunc setValue)
        {
            const int kMaxLayers = 32;
            const int checkboxSize = 16;
            int labelSize = 100;
            const int indent = 30;

            int numLayers = 0;
            for (int i = 0; i < kMaxLayers; i++)
                if (LayerMask.LayerToName(i) != "")
                    numLayers++;

            // find the longest label
            for (int i = 0; i < kMaxLayers; i++)
            {
                var textDimensions = GUI.skin.label.CalcSize(new GUIContent(LayerMask.LayerToName(i)));
                if (labelSize < textDimensions.x)
                    labelSize = (int)textDimensions.x;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(0);
            show = EditorGUILayout.Foldout(show, title, true);
            GUILayout.EndHorizontal();
            if (show)
            {
                scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.MinHeight(labelSize + 20), GUILayout.MaxHeight(labelSize + (numLayers + 1) * checkboxSize));
                Rect topLabelRect = GUILayoutUtility.GetRect(checkboxSize * numLayers + labelSize, labelSize);
                Rect scrollArea = GUIClip.topmostRect;
                Vector2 topLeft = GUIClip.Unclip(new Vector2(topLabelRect.x, topLabelRect.y));
                int y = 0;
                for (int i = 0; i < kMaxLayers; i++)
                {
                    if (LayerMask.LayerToName(i) != "")
                    {
                        // Need to do some shifting around here, so the rotated text will still clip correctly
                        float clipOffset = (labelSize + indent + (numLayers - y) * checkboxSize) - (scrollArea.width + scrollPos.x);
                        if (clipOffset < 0)
                            clipOffset = 0;

                        Vector3 translate = new Vector3(labelSize + indent + checkboxSize * (numLayers - y) + topLeft.y + topLeft.x + scrollPos.y - clipOffset, topLeft.y + scrollPos.y, 0);
                        GUI.matrix = Matrix4x4.TRS(translate, Quaternion.identity, Vector3.one) * Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, 90), Vector3.one);

                        GUI.Label(new Rect(2 - topLeft.x - scrollPos.y, scrollPos.y - clipOffset, labelSize, checkboxSize), LayerMask.LayerToName(i), "RightLabel");
                        y++;
                    }
                }
                GUI.matrix = Matrix4x4.identity;
                y = 0;
                for (int i = 0; i < kMaxLayers; i++)
                {
                    if (LayerMask.LayerToName(i) != "")
                    {
                        int x = 0;
                        Rect r = GUILayoutUtility.GetRect(indent + checkboxSize * numLayers + labelSize, checkboxSize);
                        GUI.Label(new Rect(r.x + indent, r.y, labelSize, checkboxSize), LayerMask.LayerToName(i), "RightLabel");
                        for (int j = kMaxLayers - 1; j >= 0; j--)
                        {
                            if (LayerMask.LayerToName(j) != "")
                            {
                                if (x < numLayers - y)
                                {
                                    GUIContent tooltip = new GUIContent("", LayerMask.LayerToName(i) + "/" + LayerMask.LayerToName(j));
                                    bool val = getValue(i, j);
                                    bool toggle = GUI.Toggle(new Rect(labelSize + indent + r.x + x * checkboxSize, r.y, checkboxSize, checkboxSize), val, tooltip);
                                    if (toggle != val)
                                        setValue(i, j, toggle);
                                }
                                x++;
                            }
                        }
                        y++;
                    }
                }
                GUILayout.EndScrollView();
            }
        }
    }


    [CustomEditor(typeof(PhysicsManager))]
    internal class PhysicsManagerInspector : ProjectSettingsBaseEditor
    {
        Vector2 scrollPos;
        bool show = true;

        bool GetValue(int layerA, int layerB)
        {
            return !Physics.GetIgnoreLayerCollision(layerA, layerB);
        }

        void SetValue(int layerA, int layerB, bool val)
        {
            Physics.IgnoreLayerCollision(layerA, layerB, !val);
        }

        private static class Styles
        {
            public static readonly GUIContent interCollisionPropertiesLabel = EditorGUIUtility.TextContent("Cloth Inter-Collision");
            public static readonly GUIContent interCollisionDistanceLabel = EditorGUIUtility.TextContent("Distance");
            public static readonly GUIContent interCollisionStiffnessLabel = EditorGUIUtility.TextContent("Stiffness");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_ClothInterCollisionDistance", "m_ClothInterCollisionStiffness", "m_ClothInterCollisionSettingsToggle");
            LayerMatrixGUI.DoGUI("Layer Collision Matrix", ref show, ref scrollPos, GetValue, SetValue);

            EditorGUI.BeginChangeCheck();
            bool collisionSettings = EditorGUILayout.Toggle(Styles.interCollisionPropertiesLabel, Physics.interCollisionSettingsToggle);
            bool settingsChanged = EditorGUI.EndChangeCheck();
            if (settingsChanged)
            {
                Physics.interCollisionSettingsToggle = collisionSettings;
            }

            if (Physics.interCollisionSettingsToggle)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                float distance = EditorGUILayout.FloatField(Styles.interCollisionDistanceLabel, Physics.interCollisionDistance);
                bool distanceChanged = EditorGUI.EndChangeCheck();
                if (distanceChanged)
                {
                    if (distance < 0.0f)
                        distance = 0.0f;
                    Physics.interCollisionDistance = distance;
                }

                EditorGUI.BeginChangeCheck();
                float stiffness = EditorGUILayout.FloatField(Styles.interCollisionStiffnessLabel, Physics.interCollisionStiffness);
                bool stiffnessChanged = EditorGUI.EndChangeCheck();
                if (stiffnessChanged)
                {
                    if (stiffness < 0.0f)
                        stiffness = 0.0f;
                    Physics.interCollisionStiffness = stiffness;
                }
                EditorGUI.indentLevel--;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
