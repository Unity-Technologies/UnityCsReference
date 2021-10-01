// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class LayerMatrixGUI
    {
        static class Styles
        {
            public static readonly GUIStyle rightLabel = new GUIStyle("RightLabel");
            public static readonly GUIStyle hoverStyle = GetHoverStyle();
        }

        private static Color transparentColor = new Color(1, 1, 1, 0);
        private static Color highlightColor = EditorGUIUtility.isProSkin? new Color(1, 1, 1, 0.2f): new Color(0,0,0, 0.2f);
        public static GUIStyle GetHoverStyle()
        {
            GUIStyle style = new GUIStyle(EditorStyles.label);

            Texture2D texNormal = new Texture2D(1,1){alphaIsTransparency = true};
            texNormal.SetPixel(1,1, transparentColor);
            texNormal.Apply();

            Texture2D texHover = new Texture2D(1,1){alphaIsTransparency = true};
            texHover.SetPixel(1,1, highlightColor);
            texHover.Apply();

            style.normal.background = texNormal;
            style.hover.background = texHover;

            return style;
        }

        public delegate bool GetValueFunc(int layerA, int layerB);
        public delegate void SetValueFunc(int layerA, int layerB, bool val);
        const int kMaxLayers = 32;

        public static void DoGUI(GUIContent label, ref bool show, GetValueFunc getValue, SetValueFunc setValue)
        {
            const int checkboxSize = 16;
            int labelSize = 110;
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
            show = EditorGUILayout.Foldout(show, label, true);
            GUILayout.EndHorizontal();

            if (show)
            {
                GUILayout.BeginScrollView(new Vector2(0, 0), GUILayout.Height(labelSize + 15));
                Rect topLabelRect = GUILayoutUtility.GetRect(checkboxSize * numLayers + labelSize, labelSize);
                Rect scrollArea = GUIClip.topmostRect;
                Vector2 topLeft = GUIClip.Unclip(new Vector2(topLabelRect.x - 10, topLabelRect.y));
                int y = 0;
                for (int i = 0; i < kMaxLayers; i++)
                {
                    if (LayerMask.LayerToName(i) != "")
                    {
                        bool hidelabel = false;
                        bool hidelabelonscrollbar = false;
                        int defautlabelrectwidth = 311;
                        int defaultlablecount  = 10;
                        float clipOffset = labelSize + (checkboxSize * numLayers) + checkboxSize;

                        // hide vertical labels when overlap with the rest of the UI
                        if ((topLeft.x + (clipOffset - checkboxSize * y)) <= 0)
                            hidelabel = true;

                        // hide label when it touch horizontal scroll area
                        if (topLabelRect.height > scrollArea.height)
                        {
                            hidelabelonscrollbar = true;
                        }
                        else if (topLabelRect.width != scrollArea.width || topLabelRect.width != scrollArea.width - topLeft.x)
                        {
                            // hide label when it touch vertical scroll area
                            if (topLabelRect.width > defautlabelrectwidth)
                            {
                                float tmp = topLabelRect.width - scrollArea.width;
                                if (tmp > 1)
                                {
                                    if (topLeft.x < 0)
                                        tmp += topLeft.x;

                                    if (tmp / checkboxSize > y)
                                        hidelabelonscrollbar = true;
                                }
                            }
                            else
                            {
                                float tmp = defautlabelrectwidth;
                                if (numLayers < defaultlablecount)
                                {
                                    tmp -= checkboxSize * (defaultlablecount - numLayers);
                                }

                                if ((scrollArea.width + y * checkboxSize) + checkboxSize <= tmp)
                                    hidelabelonscrollbar = true;

                                //Re-enable the label when we move the scroll bar
                                if (topLeft.x < 0)
                                {
                                    if (topLabelRect.width == scrollArea.width - topLeft.x)
                                        hidelabelonscrollbar = false;

                                    if (numLayers <= defaultlablecount / 2)
                                    {
                                        if ((tmp - (scrollArea.width - ((topLeft.x - 10) * (y + 1)))) < 0)
                                            hidelabelonscrollbar = false;
                                    }
                                    else
                                    {
                                        float hiddenlables = (int)(tmp - scrollArea.width) / checkboxSize;
                                        int res = (int)((topLeft.x * -1) + 12) / checkboxSize;
                                        if (hiddenlables - res < y)
                                            hidelabelonscrollbar = false;
                                    }
                                }
                            }
                        }

                        Vector3 translate = new Vector3(labelSize + indent + checkboxSize * (numLayers - y) + topLeft.y + topLeft.x + 10, topLeft.y, 0);
                        GUI.matrix = Matrix4x4.TRS(translate, Quaternion.identity, Vector3.one) * Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, 90), Vector3.one);

                        var labelRect = new Rect(2 - topLeft.x, 0, labelSize, checkboxSize + 5);
                        if (hidelabel || hidelabelonscrollbar)
                            GUI.Label(labelRect, GUIContent.none, Styles.rightLabel);
                        else
                        {
                            GUI.Label(labelRect, LayerMask.LayerToName(i), Styles.rightLabel);
                            // Empty Transparent label used to indicate highlighted row
                            var checkRect = new Rect(2 - topLeft.x, 1  /*This centers the highlight*/ , labelSize + 4 + (y + 1) * checkboxSize, checkboxSize);
                            GUI.Label(checkRect, GUIContent.none, Styles.hoverStyle);

                            checkRect = new Rect(
                                GUI.matrix.MultiplyPoint(new Vector3(checkRect.position.x, checkRect.position.y + 200, 0)),
                                GUI.matrix.MultiplyPoint(new Vector3(checkRect.size.x, checkRect.size.y, 0)));
                            GUIView.current.MarkHotRegion(labelRect);
                            GUIView.current.MarkHotRegion(checkRect);
                        }

                        y++;
                    }
                }
                GUILayout.EndScrollView();

                GUI.matrix = Matrix4x4.identity;
                y = 0;
                for (int i = 0; i < kMaxLayers; i++)
                {
                    if (LayerMask.LayerToName(i) != "")
                    {
                        int x = 0;
                        var r = GUILayoutUtility.GetRect(indent + checkboxSize * numLayers + labelSize, checkboxSize);
                        var labelRect = new Rect(r.x + indent, r.y, labelSize, checkboxSize + 5);
                        GUI.Label(labelRect, LayerMask.LayerToName(i), Styles.rightLabel);
                        // Empty Transparent label used to indicate highlighted row
                        var checkRect = new Rect(r.x + indent, r.y, labelSize + (numLayers - y) * checkboxSize, checkboxSize);
                        GUI.Label(checkRect, GUIContent.none, Styles.hoverStyle);
                        GUIView.current.MarkHotRegion(labelRect);
                        GUIView.current.MarkHotRegion(checkRect);

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

                GUILayout.BeginHorizontal();

                // Padding on the left
                GUILayout.Label(GUIContent.none, GUILayout.Width(labelSize + 23));
                // Made the buttons span the entire matrix of layers
                if (GUILayout.Button("Disable All", GUILayout.MinWidth((checkboxSize * numLayers) / 2), GUILayout.ExpandWidth(false)))
                    SetAllLayerCollisions(false, setValue);

                if (GUILayout.Button("Enable All", GUILayout.MinWidth((checkboxSize * numLayers) / 2), GUILayout.ExpandWidth(false)))
                    SetAllLayerCollisions(true, setValue);

                GUILayout.EndHorizontal();
            }
        }

        static void SetAllLayerCollisions(bool flag, SetValueFunc setValue)
        {
            for (int i = 0; i < kMaxLayers; ++i)
                for (int j = i; j < kMaxLayers; ++j)
                    setValue(i, j, flag);
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
            public static readonly GUIContent kLayerCollisionMatrixLabel = EditorGUIUtility.TrTextContent("Layer Collision Matrix", "Allows the configuration of the layer-based collision detection.");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_ClothInterCollisionDistance", "m_ClothInterCollisionStiffness", "m_ClothInterCollisionSettingsToggle");
            serializedObject.ApplyModifiedProperties();

            LayerMatrixGUI.DoGUI(Styles.kLayerCollisionMatrixLabel, ref show, GetValue, SetValue);

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
