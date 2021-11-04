// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Snap;
using UnityEngine;

namespace UnityEditorInternal
{
    internal class SliderScale
    {
        private static float s_StartScale, s_ScaleDrawLength = 1.0f;
        private static Vector2 s_StartMousePosition, s_CurrentMousePosition;
        private static Vector3 s_Direction;

        public static float DoAxis(int id, float scale, Vector3 position, Vector3 direction, Quaternion rotation, float size, float snap)
        {
            return DoAxis(id, scale, position, direction, rotation, size, snap, 0, 1);
        }

        internal static Vector3 DoAxis(int id, Vector3 scale, int scaleItemIndex, Vector3 position, Vector3 direction, Quaternion rotation, float size, float snap, float handleOffset, float lineScale, Vector3 initialScale, bool constrainProportionsScaling)
        {
            // If constrainProportionsScaling enabled, transforms behave the same way as Cube Handle does
            if (constrainProportionsScaling)
            {
                var value = DoAxis(id, scale.x, position, direction, rotation, size, EditorSnapSettings.scale, handleOffset, lineScale);
                return initialScale * DoCenter(id, value, position, rotation, size, Handles.CubeHandleCap, snap);
            }
            else
            {
                scale[scaleItemIndex] = DoAxis(id, scale[scaleItemIndex], position, direction, rotation, size, snap, handleOffset, lineScale);
            }

            return scale;
        }

        internal static float DoAxis(int id, float scale, Vector3 position, Vector3 direction, Quaternion rotation, float size, float snap, float handleOffset, float lineScale)
        {
            if (GUIUtility.hotControl == id)
                Handles.handleLength = size * scale / s_StartScale;
            var positionOffset = direction * size * handleOffset;
            var s = GUIUtility.hotControl == id || Handles.proportionalScale
                ? Handles.handleLength
                : size;
            var startPosition = position + positionOffset;
            var cubePosition = position + direction * (s * s_ScaleDrawLength * lineScale) + positionOffset;

            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseMove:
                case EventType.Layout:
                    HandleUtility.AddControl(id, HandleUtility.DistanceToLine(startPosition, cubePosition));
                    HandleUtility.AddControl(id, HandleUtility.DistanceToCube(cubePosition, rotation, size * .1f));
                    break;

                case EventType.MouseDown:
                    // am I closest to the thingy?
                    if (HandleUtility.nearestControl == id && evt.button == 0 && !evt.alt)
                    {
                        GUIUtility.hotControl = id;    // Grab mouse focus
                        s_CurrentMousePosition = s_StartMousePosition = evt.mousePosition;
                        s_StartScale = scale;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        s_CurrentMousePosition += evt.delta;
                        var dist = 1 + HandleUtility.CalcLineTranslation(s_StartMousePosition, s_CurrentMousePosition, position, direction) / size;
                        dist = Handles.SnapValue(dist, snap);
                        scale = s_StartScale * dist;
                        GUI.changed = true;
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
                    {
                        GUIUtility.hotControl = 0;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;

                case EventType.Repaint:
                    Handles.SetupHandleColor(id, evt, out var prevColor, out var thickness);
                    float capSize = size * .1f;
                    if (Handles.IsHovering(id, evt))
                        capSize *= Handles.s_HoverExtraScale;

                    var camera = Camera.current;
                    var viewDir = camera != null ? camera.transform.forward : -direction;
                    var facingAway = Vector3.Dot(viewDir, direction) < 0.0f;
                    // draw line vs cube in the appropriate order based on viewing
                    // direction, for correct transparency sorting
                    var lineEndPos = position + direction * (s * s_ScaleDrawLength * lineScale - capSize * 0.5f) + positionOffset;
                    if (facingAway)
                    {
                        Handles.DrawLine(startPosition, lineEndPos, thickness);
                        Handles.CubeHandleCap(id, cubePosition, rotation, capSize, EventType.Repaint);
                    }
                    else
                    {
                        Handles.CubeHandleCap(id, cubePosition, rotation, capSize, EventType.Repaint);
                        Handles.DrawLine(startPosition, lineEndPos, thickness);
                    }
                    Handles.color = prevColor;
                    break;
            }

            return scale;
        }

        public static float DoCenter(int id, float value, Vector3 position, Quaternion rotation, float size, Handles.CapFunction capFunction, float snap)
        {
            Event evt = Event.current;
            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseMove:
                case EventType.Layout:
                    capFunction(id, position, rotation, size * .15f, EventType.Layout);
                    break;

                case EventType.MouseDown:
                    // am I closest to the thingy?
                    if (HandleUtility.nearestControl == id && evt.button == 0 && !evt.alt)
                    {
                        GUIUtility.hotControl = id;     // Grab mouse focus
                        Tools.LockHandlePosition();
                        s_StartScale = value;
                        s_CurrentMousePosition = s_StartMousePosition = evt.mousePosition;

                        var camera = SceneView.lastActiveSceneView.camera;
                        s_Direction = camera == null? Vector3.one : (camera.transform.right + camera.transform.up).normalized;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        s_CurrentMousePosition += evt.delta;
                        var dist = HandleUtility.CalcLineTranslation(s_StartMousePosition, s_CurrentMousePosition, position, s_Direction) / size;
                        value = (Handles.SnapValue(dist, snap) + 1.0f) * s_StartScale;
                        s_ScaleDrawLength = value;
                        GUI.changed = true;
                        evt.Use();
                    }
                    break;

                case EventType.KeyDown:
                    if (GUIUtility.hotControl == id)
                    {
                        // Cancel dragging on ESC
                        if (evt.keyCode == KeyCode.Escape)
                        {
                            Tools.UnlockHandlePosition();
                            s_ScaleDrawLength = 1.0f;
                        }
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && (evt.button == 0 || evt.button == 2))
                    {
                        GUIUtility.hotControl = 0;
                        Tools.UnlockHandlePosition();
                        s_ScaleDrawLength = 1.0f;
                        evt.Use();
                        EditorGUIUtility.SetWantsMouseJumping(0);
                    }
                    break;

                case EventType.Repaint:
                    Handles.SetupHandleColor(id, evt, out var prevColor, out var thickness);
                    float capSize = size * .15f;
                    if (Handles.IsHovering(id, evt))
                        capSize *= Handles.s_HoverExtraScale;
                    capFunction(id, position, rotation, capSize, EventType.Repaint);
                    Handles.color = prevColor;
                    break;
            }

            return value;
        }
    }
}
