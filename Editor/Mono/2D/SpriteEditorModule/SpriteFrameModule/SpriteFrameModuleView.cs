// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;
using UnityEngine.U2D.Interface;

namespace UnityEditor
{
    internal partial class SpriteFrameModule : SpriteFrameModuleBase
    {
        private static class SpriteFrameModuleStyles
        {
            public static readonly GUIContent sliceButtonLabel = EditorGUIUtility.TextContent("Slice");
            public static readonly GUIContent trimButtonLabel = EditorGUIUtility.TextContent("Trim|Trims selected rectangle (T)");
            public static readonly GUIContent okButtonLabel = EditorGUIUtility.TextContent("Ok");
            public static readonly GUIContent cancelButtonLabel = EditorGUIUtility.TextContent("Cancel");
        }

        internal static PrefKey k_SpriteEditorTrim = new PrefKey("Sprite Editor/Trim", "#t");


        // overrides for SpriteFrameModuleBase
        public override void DoTextureGUI()
        {
            base.DoTextureGUI();
            DrawSpriteRectGizmos();

            HandleGizmoMode();

            if (containsMultipleSprites)
                HandleRectCornerScalingHandles();

            HandleBorderCornerScalingHandles();
            HandleBorderSidePointScalingSliders();

            if (containsMultipleSprites)
                HandleRectSideScalingHandles();

            HandleBorderSideScalingHandles();
            HandlePivotHandle();

            if (containsMultipleSprites)
                HandleDragging();

            if (!MouseOnTopOfInspector())
                spriteEditor.HandleSpriteSelection();

            if (containsMultipleSprites)
            {
                HandleCreate();
                HandleDelete();
                HandleDuplicate();
            }
        }

        public override void DrawToolbarGUI(Rect toolbarRect)
        {
            using (new EditorGUI.DisabledScope(!containsMultipleSprites || spriteEditor.editingDisabled))
            {
                GUIStyle skin = EditorStyles.toolbarPopup;

                Rect drawArea = toolbarRect;

                drawArea.width = skin.CalcSize(SpriteFrameModuleStyles.sliceButtonLabel).x;
                SpriteUtilityWindow.DrawToolBarWidget(ref drawArea, ref toolbarRect, (adjustedDrawArea) =>
                    {
                        if (GUI.Button(adjustedDrawArea, SpriteFrameModuleStyles.sliceButtonLabel, skin))
                        {
                            if (SpriteEditorMenu.ShowAtPosition(adjustedDrawArea, this, spriteEditor.previewTexture, spriteEditor.selectedTexture))
                                GUIUtility.ExitGUI();
                        }
                    });

                using (new EditorGUI.DisabledScope(!hasSelected))
                {
                    drawArea.x += drawArea.width;
                    drawArea.width = skin.CalcSize(SpriteFrameModuleStyles.trimButtonLabel).x;
                    SpriteUtilityWindow.DrawToolBarWidget(ref drawArea, ref toolbarRect, (adjustedDrawArea) =>
                        {
                            if (GUI.Button(adjustedDrawArea, SpriteFrameModuleStyles.trimButtonLabel, EditorStyles.toolbarButton) ||
                                (string.IsNullOrEmpty(GUI.GetNameOfFocusedControl()) && k_SpriteEditorTrim.activated))
                            {
                                TrimAlpha();
                                Repaint();
                            }
                        });
                }
            }
        }

        private void HandleRectCornerScalingHandles()
        {
            if (!hasSelected)
                return;

            GUIStyle dragDot = styles.dragdot;
            GUIStyle dragDotActive = styles.dragdotactive;
            var color = Color.white;

            Rect rect = new Rect(selectedSpriteRect);

            float left = rect.xMin;
            float right = rect.xMax;
            float top = rect.yMax;
            float bottom = rect.yMin;

            EditorGUI.BeginChangeCheck();

            HandleBorderPointSlider(ref left, ref top, MouseCursor.ResizeUpLeft, false, dragDot, dragDotActive, color);
            HandleBorderPointSlider(ref right, ref top, MouseCursor.ResizeUpRight, false, dragDot, dragDotActive, color);
            HandleBorderPointSlider(ref left, ref bottom, MouseCursor.ResizeUpRight, false, dragDot, dragDotActive, color);
            HandleBorderPointSlider(ref right, ref bottom, MouseCursor.ResizeUpLeft, false, dragDot, dragDotActive, color);

            if (EditorGUI.EndChangeCheck())
            {
                rect.xMin = left;
                rect.xMax = right;
                rect.yMax = top;
                rect.yMin = bottom;
                ScaleSpriteRect(rect);
            }
        }

        private void HandleRectSideScalingHandles()
        {
            if (!hasSelected)
                return;

            Rect rect = new Rect(selectedSpriteRect);

            float left = rect.xMin;
            float right = rect.xMax;
            float top = rect.yMax;
            float bottom = rect.yMin;

            Vector2 screenRectTopLeft = Handles.matrix.MultiplyPoint(new Vector3(rect.xMin, rect.yMin));
            Vector2 screenRectBottomRight = Handles.matrix.MultiplyPoint(new Vector3(rect.xMax, rect.yMax));

            float screenRectWidth = Mathf.Abs(screenRectBottomRight.x - screenRectTopLeft.x);
            float screenRectHeight = Mathf.Abs(screenRectBottomRight.y - screenRectTopLeft.y);

            EditorGUI.BeginChangeCheck();

            left = HandleBorderScaleSlider(left, rect.yMax, screenRectWidth, screenRectHeight, true);
            right = HandleBorderScaleSlider(right, rect.yMax, screenRectWidth, screenRectHeight, true);

            top = HandleBorderScaleSlider(rect.xMin, top, screenRectWidth, screenRectHeight, false);
            bottom = HandleBorderScaleSlider(rect.xMin, bottom, screenRectWidth, screenRectHeight, false);

            if (EditorGUI.EndChangeCheck())
            {
                rect.xMin = left;
                rect.xMax = right;
                rect.yMax = top;
                rect.yMin = bottom;

                ScaleSpriteRect(rect);
            }
        }

        private void HandleDragging()
        {
            if (hasSelected && !MouseOnTopOfInspector())
            {
                Rect textureBounds = new Rect(0, 0, previewTexture.width, previewTexture.height);
                EditorGUI.BeginChangeCheck();

                Rect oldRect = selectedSpriteRect;
                Rect newRect = SpriteEditorUtility.ClampedRect(SpriteEditorUtility.RoundedRect(SpriteEditorHandles.SliderRect(oldRect)), textureBounds, true);

                if (EditorGUI.EndChangeCheck())
                    selectedSpriteRect = newRect;
            }
        }

        private void HandleCreate()
        {
            if (!MouseOnTopOfInspector() && !eventSystem.current.alt)
            {
                // Create new rects via dragging in empty space
                EditorGUI.BeginChangeCheck();
                Rect newRect = SpriteEditorHandles.RectCreator(previewTexture.width, previewTexture.height, styles.createRect);
                if (EditorGUI.EndChangeCheck() && newRect.width > 0f && newRect.height > 0f)
                {
                    CreateSprite(newRect);
                    GUIUtility.keyboardControl = 0;
                }
            }
        }

        private void HandleDuplicate()
        {
            IEvent evt = eventSystem.current;
            if ((evt.type == EventType.ValidateCommand || evt.type == EventType.ExecuteCommand)
                && evt.commandName == "Duplicate")
            {
                if (evt.type == EventType.ExecuteCommand)
                    DuplicateSprite();

                evt.Use();
            }
        }

        private void HandleDelete()
        {
            IEvent evt = eventSystem.current;

            if ((evt.type == EventType.ValidateCommand || evt.type == EventType.ExecuteCommand)
                && (evt.commandName == "SoftDelete" || evt.commandName == "Delete"))
            {
                if (evt.type == EventType.ExecuteCommand && hasSelected)
                    DeleteSprite();

                evt.Use();
            }
        }
    }
}
