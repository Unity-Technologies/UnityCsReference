// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class LayerMatrixGUI
    {
        public delegate bool GetValueFunc(int layerA, int layerB);
        public delegate void SetValueFunc(int layerA, int layerB, bool val);

        public static void DoGUI(string title, ref bool show, GetValueFunc getValue, SetValueFunc setValue)
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
                var scrollState = GUI.GetTopScrollView();
                var scrollStatePosOffset = scrollState?.position.y ?? 0;
                var scrollPos = scrollState?.scrollPosition ?? Vector2.zero;
                var topLabelRect = GUILayoutUtility.GetRect(checkboxSize + labelSize, labelSize);
                var topLeft = new Vector2(topLabelRect.x, topLabelRect.y);
                var y = 0;
                for (int i = 0; i < kMaxLayers; i++)
                {
                    if (LayerMask.LayerToName(i) != "")
                    {
                        var translate = new Vector3(labelSize + indent + checkboxSize * (numLayers - y) + topLeft.x - scrollPos.x + scrollStatePosOffset, topLeft.y - scrollPos.y + scrollStatePosOffset, 0);
                        GUI.matrix = Matrix4x4.TRS(translate, Quaternion.Euler(0, 0, 90), Vector3.one);
                        GUI.Label(new Rect(scrollPos.x, scrollPos.y, labelSize, checkboxSize), LayerMask.LayerToName(i), "RightLabel");
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
                        var r = GUILayoutUtility.GetRect(indent + checkboxSize * numLayers + labelSize, checkboxSize);
                        GUI.Label(new Rect(r.x + indent, r.y, labelSize, checkboxSize), LayerMask.LayerToName(i), "RightLabel");
                        for (int j = kMaxLayers - 1; j >= 0; j--)
                        {
                            if (LayerMask.LayerToName(j) != "")
                            {
                                if (x < numLayers - y)
                                {
                                    var tooltip = new GUIContent("", LayerMask.LayerToName(i) + "/" + LayerMask.LayerToName(j));
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
            }
        }
    }


    [CustomEditor(typeof(PhysicsManager))]
    internal class PhysicsManagerInspector : ProjectSettingsBaseEditor
    {
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
            public static readonly GUIContent interCollisionPropertiesLabel = EditorGUIUtility.TrTextContent("Cloth Inter-Collision");
            public static readonly GUIContent interCollisionDistanceLabel = EditorGUIUtility.TrTextContent("Distance");
            public static readonly GUIContent interCollisionStiffnessLabel = EditorGUIUtility.TrTextContent("Stiffness");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_ClothInterCollisionDistance", "m_ClothInterCollisionStiffness", "m_ClothInterCollisionSettingsToggle");
            serializedObject.ApplyModifiedProperties();

            LayerMatrixGUI.DoGUI("Layer Collision Matrix", ref show, GetValue, SetValue);

            EditorGUI.BeginChangeCheck();
            bool collisionSettings = EditorGUILayout.Toggle(Styles.interCollisionPropertiesLabel, Physics.interCollisionSettingsToggle);
            bool settingsChanged = EditorGUI.EndChangeCheck();
            if (settingsChanged)
            {
                Undo.RecordObject(target, "Change inter collision settings");
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
                    Undo.RecordObject(target, "Change inter collision distance");
                    if (distance < 0.0f)
                        distance = 0.0f;
                    Physics.interCollisionDistance = distance;
                }

                EditorGUI.BeginChangeCheck();
                float stiffness = EditorGUILayout.FloatField(Styles.interCollisionStiffnessLabel, Physics.interCollisionStiffness);
                bool stiffnessChanged = EditorGUI.EndChangeCheck();
                if (stiffnessChanged)
                {
                    Undo.RecordObject(target, "Change inter collision stiffness");
                    if (stiffness < 0.0f)
                        stiffness = 0.0f;
                    Physics.interCollisionStiffness = stiffness;
                }
                EditorGUI.indentLevel--;
            }
        }

        [SettingsProvider]
        static SettingsProvider CreateProjectSettingsProvider()
        {
            var provider = AssetSettingsProvider.CreateProviderFromAssetPath(
                "Project/Physics", "ProjectSettings/DynamicsManager.asset",
                SettingsProvider.GetSearchKeywordsFromPath("ProjectSettings/DynamicsManager.asset"));
            return provider;
        }
    }
}
