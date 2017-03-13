// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Linq;

namespace UnityEditor
{
    /**
     * Internal class for handling the drawing of ShadowCascade splits GUI and user interaction.
     */
    internal static class ShadowCascadeSplitGUI
    {
        private const int kSliderbarTopMargin = 2;
        private const int kSliderbarHeight = 24;
        private const int kSliderbarBottomMargin = 2;
        private const int kPartitionHandleWidth = 2;
        private const int kPartitionHandleExtraHitAreaWidth = 2;

        private static readonly Color[] kCascadeColors =
        {
            new Color(0.5f, 0.5f, 0.6f, 1.0f),
            new Color(0.5f, 0.6f, 0.5f, 1.0f),
            new Color(0.6f, 0.6f, 0.5f, 1.0f),
            new Color(0.6f, 0.5f, 0.5f, 1.0f),
        };

        // using a LODGroup skin
        private static readonly GUIStyle s_CascadeSliderBG = "LODSliderRange";
        private static readonly GUIStyle s_TextCenteredStyle =  new GUIStyle(EditorStyles.whiteMiniLabel)
        {
            alignment = TextAnchor.MiddleCenter
        };

        // Internal struct to bundle drag information
        private class DragCache
        {
            public int      m_ActivePartition;          // the cascade partition that we are currently dragging/resizing
            public float    m_NormalizedPartitionSize;  // the normalized size of the partition (0.0f < size < 1.0f)
            public Vector2  m_LastCachedMousePosition;  // mouse position the last time we registered a drag or mouse down.

            public DragCache(int activePartition, float normalizedPartitionSize, Vector2 currentMousePos)
            {
                m_ActivePartition = activePartition;
                m_NormalizedPartitionSize = normalizedPartitionSize;
                m_LastCachedMousePosition = currentMousePos;
            }
        };
        private static DragCache  s_DragCache;

        private static readonly int s_CascadeSliderId = "s_CascadeSliderId".GetHashCode();

        private static SceneView s_RestoreSceneView;
        private static DrawCameraMode s_OldSceneDrawMode = DrawCameraMode.Textured;
        private static bool s_OldSceneLightingMode;


        /**
         *  Static function to handle the GUI and User input related to the cascade slider.
         *
         *  @param  normalizedCascadePartition      The array of partition sizes in the range 0.0f - 1.0f; expects ONE entry if cascades = 2, and THREE if cascades=4
         *                                          The last entry will be automatically determined by summing up the array, and doing 1.0f - sum
         */
        public static void HandleCascadeSliderGUI(ref float[] normalizedCascadePartitions)
        {
            GUILayout.Label("Cascade splits");

            // get the inspector width since we need it while drawing the partition rects.
            // Only way currently is to reserve the block in the layout using GetRect(), and then immediately drawing the empty box
            // to match the call to GetRect.
            // From this point on, we move to non-layout based code.
            var sliderRect = GUILayoutUtility.GetRect(GUIContent.none
                    , s_CascadeSliderBG
                    , GUILayout.Height(kSliderbarTopMargin + kSliderbarHeight + kSliderbarBottomMargin)
                    , GUILayout.ExpandWidth(true));
            GUI.Box(sliderRect, GUIContent.none);

            float currentX = sliderRect.x;
            float cascadeBoxStartY = sliderRect.y + kSliderbarTopMargin;
            float cascadeSliderWidth = sliderRect.width - (normalizedCascadePartitions.Length * kPartitionHandleWidth);
            Color origTextColor = GUI.color;
            Color origBackgroundColor = GUI.backgroundColor;
            int colorIndex = -1;

            // setup the array locally with the last partition
            float[] adjustedCascadePartitions = new float[normalizedCascadePartitions.Length + 1];
            Array.Copy(normalizedCascadePartitions, adjustedCascadePartitions, normalizedCascadePartitions.Length);
            adjustedCascadePartitions[adjustedCascadePartitions.Length - 1] = 1.0f - normalizedCascadePartitions.Sum();


            // check for user input on any of the partition handles
            // this mechanism gets the current event in the queue... make sure that the mouse is over our control before consuming the event
            int sliderControlId = GUIUtility.GetControlID(s_CascadeSliderId, FocusType.Passive);
            Event currentEvent = Event.current;
            int hotPartitionHandleIndex = -1; // the index of any partition handle that we are hovering over or dragging

            // draw each cascade partition
            for (int i = 0; i < adjustedCascadePartitions.Length; ++i)
            {
                float currentPartition = adjustedCascadePartitions[i];

                colorIndex = (colorIndex + 1) % kCascadeColors.Length;
                GUI.backgroundColor = kCascadeColors[colorIndex];
                float boxLength = (cascadeSliderWidth * currentPartition);

                // main cascade box
                Rect partitionRect = new Rect(currentX, cascadeBoxStartY, boxLength, kSliderbarHeight);
                GUI.Box(partitionRect, GUIContent.none, s_CascadeSliderBG);
                currentX += boxLength;

                // cascade box percentage text
                GUI.color = Color.white;
                Rect textRect = partitionRect;
                var cascadeText = string.Format("{0}\n{1:F1}%", i, currentPartition * 100.0f);

                GUI.Label(textRect, GUIContent.Temp(cascadeText, cascadeText), s_TextCenteredStyle);

                // no need to draw the partition handle for last box
                if (i == adjustedCascadePartitions.Length - 1)
                    break;

                // partition handle
                GUI.backgroundColor = Color.black;
                Rect handleRect = partitionRect;
                handleRect.x = currentX;
                handleRect.width = kPartitionHandleWidth;
                GUI.Box(handleRect, GUIContent.none, s_CascadeSliderBG);
                // we want a thin handle visually (since wide black bar looks bad), but a slightly larger
                // hit area for easier manipulation
                Rect handleHitRect = handleRect;
                handleHitRect.xMin -= kPartitionHandleExtraHitAreaWidth;
                handleHitRect.xMax += kPartitionHandleExtraHitAreaWidth;
                if (handleHitRect.Contains(currentEvent.mousePosition))
                    hotPartitionHandleIndex = i;

                // add regions to slider where the cursor changes to Resize-Horizontal
                if (s_DragCache == null)
                {
                    EditorGUIUtility.AddCursorRect(handleHitRect, MouseCursor.ResizeHorizontal, sliderControlId);
                }

                currentX += kPartitionHandleWidth;
            }

            GUI.color = origTextColor;
            GUI.backgroundColor = origBackgroundColor;

            EventType eventType = currentEvent.GetTypeForControl(sliderControlId);
            switch (eventType)
            {
                case EventType.MouseDown:
                    if (hotPartitionHandleIndex >= 0)
                    {
                        s_DragCache = new DragCache(hotPartitionHandleIndex, normalizedCascadePartitions[hotPartitionHandleIndex], currentEvent.mousePosition);
                        if (GUIUtility.hotControl == 0)
                            GUIUtility.hotControl = sliderControlId;
                        currentEvent.Use();

                        // Switch active scene view into shadow cascades visualization mode, once we start
                        // tweaking cascade splits.
                        if (s_RestoreSceneView == null)
                        {
                            s_RestoreSceneView = SceneView.lastActiveSceneView;
                            if (s_RestoreSceneView != null)
                            {
                                s_OldSceneDrawMode = s_RestoreSceneView.renderMode;
                                s_OldSceneLightingMode = s_RestoreSceneView.m_SceneLighting;
                                s_RestoreSceneView.renderMode = DrawCameraMode.ShadowCascades;
                            }
                        }
                    }
                    break;

                case EventType.MouseUp:
                    // mouseUp event anywhere should release the hotcontrol (if it belongs to us), drags (if any)
                    if (GUIUtility.hotControl == sliderControlId)
                    {
                        GUIUtility.hotControl = 0;
                        currentEvent.Use();
                    }
                    s_DragCache = null;

                    // Restore previous scene view drawing mode once we stop tweaking cascade splits.
                    if (s_RestoreSceneView != null)
                    {
                        s_RestoreSceneView.renderMode = s_OldSceneDrawMode;
                        s_RestoreSceneView.m_SceneLighting = s_OldSceneLightingMode;
                        s_RestoreSceneView = null;
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != sliderControlId)
                        break;

                    // convert the mouse movement to normalized cascade width. Make sure that we are safe to apply the delta before using it.
                    float delta = (currentEvent.mousePosition - s_DragCache.m_LastCachedMousePosition).x / cascadeSliderWidth;
                    bool isLeftPartitionHappy = ((adjustedCascadePartitions[s_DragCache.m_ActivePartition] + delta) > 0.0f);
                    bool isRightPartitionHappy = ((adjustedCascadePartitions[s_DragCache.m_ActivePartition + 1] - delta) > 0.0f);
                    if (isLeftPartitionHappy && isRightPartitionHappy)
                    {
                        s_DragCache.m_NormalizedPartitionSize += delta;
                        normalizedCascadePartitions[s_DragCache.m_ActivePartition] = s_DragCache.m_NormalizedPartitionSize;
                        if (s_DragCache.m_ActivePartition < normalizedCascadePartitions.Length - 1)
                            normalizedCascadePartitions[s_DragCache.m_ActivePartition + 1] -= delta;
                        GUI.changed = true;
                    }
                    s_DragCache.m_LastCachedMousePosition = currentEvent.mousePosition;
                    currentEvent.Use();
                    break;
            }
        }
    }
}
