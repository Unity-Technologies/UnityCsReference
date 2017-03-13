// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.U2D.Interface;

namespace UnityEditor
{
    internal partial class SpritePolygonModeModule : SpriteFrameModuleBase, ISpriteEditorModule
    {
        private static class SpritePolygonModeStyles
        {
            public static readonly GUIContent changeShapeLabel = EditorGUIUtility.TextContent("Change Shape");
            public static readonly GUIContent sidesLabel = EditorGUIUtility.TextContent("Sides");
            public static readonly GUIContent polygonChangeShapeHelpBoxContent = EditorGUIUtility.TextContent("Sides can only be either 0 or anything between 3 and 128");
            public static readonly GUIContent changeButtonLabel = EditorGUIUtility.TextContent("Change|Change to the new number of sides");
        }

        private const int k_PolygonChangeShapeWindowMargin = EditorGUI.kWindowToolbarHeight;
        private const int k_PolygonChangeShapeWindowWidth = 150;
        private const int k_PolygonChangeShapeWindowHeight = 45;
        private const int k_PolygonChangeShapeWindowWarningHeight = k_PolygonChangeShapeWindowHeight + 20;

        private Rect m_PolygonChangeShapeWindowRect = new Rect(0, k_PolygonChangeShapeWindowMargin, k_PolygonChangeShapeWindowWidth, k_PolygonChangeShapeWindowHeight);

        // overrides for SpriteFrameModuleViewBase
        public override void OnPostGUI()
        {
            // Polygon change shape window
            DoPolygonChangeShapeWindow();
            base.OnPostGUI();
        }

        public override void DoTextureGUI()
        {
            base.DoTextureGUI();
            DrawGizmos();

            HandleGizmoMode();

            HandleBorderCornerScalingHandles();
            HandleBorderSidePointScalingSliders();

            HandleBorderSideScalingHandles();
            HandlePivotHandle();

            if (!MouseOnTopOfInspector())
                spriteEditor.HandleSpriteSelection();
        }

        public override void DrawToolbarGUI(Rect toolbarRect)
        {
            using (new EditorGUI.DisabledScope(spriteEditor.editingDisabled))
            {
                GUIStyle skin = EditorStyles.toolbarPopup;
                Rect drawArea = toolbarRect;
                drawArea.width = skin.CalcSize(SpritePolygonModeStyles.changeShapeLabel).x;
                SpriteUtilityWindow.DrawToolBarWidget(ref drawArea, ref toolbarRect, (adjustedDrawArea) =>
                    {
                        showChangeShapeWindow = GUI.Toggle(adjustedDrawArea, showChangeShapeWindow, SpritePolygonModeStyles.changeShapeLabel, EditorStyles.toolbarButton);
                    });
            }
        }

        private void DrawGizmos()
        {
            if (eventSystem.current.type != EventType.Repaint)
                return;
            for (int i = 0; i < spriteCount; i++)
            {
                List<SpriteOutline> outline = GetSpriteOutlineAt(i);
                Vector2 offset = GetSpriteRectAt(i).size * 0.5f;
                if (outline.Count > 0)
                {
                    SpriteEditorUtility.BeginLines(new Color(0.75f, 0.75f, 0.75f, 0.75f));
                    for (int j = 0; j < outline.Count; ++j)
                    {
                        for (int k = 0, last = outline[j].Count - 1; k < outline[j].Count; last = k, ++k)
                            SpriteEditorUtility.DrawLine(outline[j][last] + offset, outline[j][k] + offset);
                    }
                    SpriteEditorUtility.EndLines();
                }
            }
            DrawSpriteRectGizmos();
        }

        private void DoPolygonChangeShapeWindow()
        {
            if (showChangeShapeWindow && !spriteEditor.editingDisabled)
            {
                bool createAndClose = false;

                float oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 45f;

                GUILayout.BeginArea(m_PolygonChangeShapeWindowRect);
                GUILayout.BeginVertical(GUI.skin.box);

                // Catch "return" key before text box
                IEvent evt = eventSystem.current;
                if (isSidesValid &&
                    evt.type == EventType.KeyDown &&
                    evt.keyCode == KeyCode.Return)
                {
                    createAndClose = true;
                    evt.Use();
                }

                EditorGUI.BeginChangeCheck();
                polygonSides = EditorGUILayout.IntField(SpritePolygonModeStyles.sidesLabel, polygonSides);
                if (EditorGUI.EndChangeCheck())
                    m_PolygonChangeShapeWindowRect.height = isSidesValid ? k_PolygonChangeShapeWindowHeight : k_PolygonChangeShapeWindowWarningHeight;

                GUILayout.FlexibleSpace();

                if (!isSidesValid)
                    EditorGUILayout.HelpBox(SpritePolygonModeStyles.polygonChangeShapeHelpBoxContent.text, MessageType.Warning, true);
                else
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    EditorGUI.BeginDisabledGroup(!isSidesValid);
                    if (GUILayout.Button(SpritePolygonModeStyles.changeButtonLabel))
                        createAndClose = true;
                    EditorGUI.EndDisabledGroup();

                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();

                if (createAndClose)
                {
                    if (isSidesValid)
                        GeneratePolygonOutline();
                    showChangeShapeWindow = false;
                    GUIUtility.hotControl = 0;
                    GUIUtility.keyboardControl = 0;
                }

                EditorGUIUtility.labelWidth = oldLabelWidth;
                GUILayout.EndArea();
            }
        }

        private bool isSidesValid
        {
            get
            {
                return polygonSides == 0 || (polygonSides >= 3 && polygonSides <= 128);
            }
        }

        public bool showChangeShapeWindow
        {
            get;
            set;
        }
    }
}
