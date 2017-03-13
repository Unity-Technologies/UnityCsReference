// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;

namespace UnityEditor
{
    // State for when we're dragging a knob.
    internal class KnobState
    {
        public float dragStartPos;
        public float dragStartValue;
        public bool isDragging;
        public bool isEditing;
    }

    public sealed partial class EditorGUILayout
    {
        public static float Knob(Vector2 knobSize, float value, float minValue, float maxValue, string unit, Color backgroundColor, Color activeColor, bool showValue, params GUILayoutOption[] options)
        {
            Rect r = s_LastRect = GetControlRect(false, knobSize.y, options);
            return EditorGUI.Knob(r, knobSize, value, minValue, maxValue, unit, backgroundColor, activeColor, showValue, GUIUtility.GetControlID("Knob".GetHashCode(), FocusType.Passive, r));
        }
    }

    public sealed partial class EditorGUI
    {
        internal struct KnobContext
        {
            readonly Rect position;
            readonly Vector2 knobSize;
            readonly float currentValue;
            readonly float start;
            readonly float end;
            readonly string unit;
            readonly Color activeColor;
            readonly Color backgroundColor;
            readonly bool showValue;
            readonly int id;

            private const int kPixelRange = 250;

            public KnobContext(Rect position, Vector2 knobSize, float currentValue, float start, float end, string unit, Color backgroundColor, Color activeColor, bool showValue, int id)
            {
                this.position = position;
                this.knobSize = knobSize;
                this.currentValue = currentValue;
                this.start = start;
                this.end = end;
                this.unit = unit;
                this.activeColor = activeColor;
                this.backgroundColor = backgroundColor;
                this.showValue = showValue;
                this.id = id;
            }

            public float Handle()
            {
                if (KnobState().isEditing && CurrentEventType() != EventType.Repaint)
                    return DoKeyboardInput();

                switch (CurrentEventType())
                {
                    case EventType.MouseDown:
                        return OnMouseDown();

                    case EventType.MouseDrag:
                        return OnMouseDrag();

                    case EventType.MouseUp:
                        return OnMouseUp();

                    case EventType.Repaint:
                        return OnRepaint();
                }

                return currentValue;
            }

            private EventType CurrentEventType()
            {
                return CurrentEvent().type;
            }

            private bool IsEmptyKnob()
            {
                return start == end;
            }

            private Event CurrentEvent()
            {
                return Event.current;
            }

            private float Clamp(float value)
            {
                return Mathf.Clamp(value, MinValue(), MaxValue());
            }

            private float ClampedCurrentValue()
            {
                return Clamp(currentValue);
            }

            private float MaxValue()
            {
                return Mathf.Max(start, end);
            }

            private float MinValue()
            {
                return Mathf.Min(start, end);
            }

            private float GetCurrentValuePercent()
            {
                return (ClampedCurrentValue() - MinValue()) / (MaxValue() - MinValue());
            }

            private float MousePosition()
            {
                return CurrentEvent().mousePosition.y - position.y;
            }

            private bool WasDoubleClick()
            {
                return CurrentEventType() == EventType.MouseDown &&
                    CurrentEvent().clickCount == 2;
            }

            private float ValuesPerPixel()
            {
                return kPixelRange / (MaxValue() - MinValue());
            }

            private KnobState KnobState()
            {
                return (KnobState)GUIUtility.GetStateObject(typeof(KnobState), id);
            }

            private void StartDraggingWithValue(float dragStartValue)
            {
                var state = KnobState();
                state.dragStartPos = MousePosition();
                state.dragStartValue = dragStartValue;
                state.isDragging = true;
            }

            private float OnMouseDown()
            {
                // if the click is outside this control, just bail out...
                if (!position.Contains(CurrentEvent().mousePosition) || KnobState().isEditing || IsEmptyKnob())
                    return currentValue;

                GUIUtility.hotControl = id;

                if (WasDoubleClick())
                {
                    KnobState().isEditing = true;
                }
                else
                {
                    // Record where we're dragging from, so the user can get back.
                    StartDraggingWithValue(ClampedCurrentValue());
                }

                CurrentEvent().Use();

                return currentValue;
            }

            private float OnMouseDrag()
            {
                if (GUIUtility.hotControl != id)
                    return currentValue;

                var state = KnobState();
                if (!state.isDragging)
                    return currentValue;

                GUI.changed = true;
                CurrentEvent().Use();

                // Recalculate the value from the mouse position. this has the side effect that values are relative to the
                // click point - no matter where inside the trough the original value was. Also means user can get back original value
                // if he drags back to start position.
                float deltaPos = state.dragStartPos - MousePosition();
                var newValue = state.dragStartValue + (deltaPos / ValuesPerPixel());
                return Clamp(newValue);
            }

            private float OnMouseUp()
            {
                if (GUIUtility.hotControl == id)
                {
                    CurrentEvent().Use();
                    GUIUtility.hotControl = 0;
                    KnobState().isDragging = false;
                }
                return currentValue;
            }

            private void PrintValue()
            {
                Rect textRect = new Rect(position.x + knobSize.x / 2 - 8, position.y + knobSize.y / 2 - 8, position.width, 20);
                string value = currentValue.ToString("0.##");  //FIXME: This needs to be done so it can handle any range types.
                GUI.Label(textRect, value + " " + unit);
            }

            private float DoKeyboardInput()
            {
                GUI.SetNextControlName("KnobInput");
                GUI.FocusControl("KnobInput");

                EditorGUI.BeginChangeCheck();
                Rect inputRect = new Rect(position.x + knobSize.x / 2 - 6, position.y + knobSize.y / 2 - 7, position.width, 20);

                //FIXME: Hack
                GUIStyle style = GUIStyle.none;
                style.normal.textColor = new Color(.703f, .703f, .703f, 1.0f);
                style.fontStyle = FontStyle.Normal;

                string newStr = EditorGUI.DelayedTextField(inputRect, currentValue.ToString("0.##"), style);
                if (EditorGUI.EndChangeCheck() && !String.IsNullOrEmpty(newStr))
                {
                    KnobState().isEditing = false;
                    float newValue;
                    if (float.TryParse(newStr, out newValue) && newValue != currentValue)
                    {
                        return Clamp(newValue);
                    }
                }

                return currentValue;
            }

            private static Material knobMaterial;
            static void CreateKnobMaterial()
            {
                if (!knobMaterial)
                {
                    // use a regular IMGUI content shader
                    Shader shader = AssetDatabase.GetBuiltinExtraResource(typeof(Shader), "Internal-GUITextureClip.shader") as Shader;
                    knobMaterial = new Material(shader);
                    knobMaterial.hideFlags = HideFlags.HideAndDontSave;
                    knobMaterial.mainTexture = EditorGUIUtility.FindTexture("KnobCShape");
                    knobMaterial.name = "Knob Material";
                    if (knobMaterial.mainTexture == null)
                        Debug.Log("Did not find 'KnobCShape'");
                }
            }

            Vector3 GetUVForPoint(Vector3 point)
            {
                Vector3 uv = new Vector3(
                        (point.x - position.x) / knobSize.x,
                        (point.y - position.y - knobSize.y) / -knobSize.y // we need to flip uv over Y otherwise image is upside-down
                        );
                return uv;
            }

            void VertexPointWithUV(Vector3 point)
            {
                GL.TexCoord(GetUVForPoint(point));
                GL.Vertex(point);
            }

            private void DrawValueArc(float angle)
            {
                if (Event.current.type != EventType.Repaint)
                    return;

                CreateKnobMaterial();

                Vector3 center = new Vector3(position.x + knobSize.x / 2, position.y + knobSize.y / 2, 0);
                Vector3 centerRight = new Vector3(position.x + knobSize.x, position.y + knobSize.y / 2, 0);
                Vector3 bottomRight = new Vector3(position.x + knobSize.x, position.y + knobSize.y, 0);
                Vector3 bottomleft = new Vector3(position.x, position.y + knobSize.y, 0);
                Vector3 topLeft = new Vector3(position.x, position.y, 0);
                Vector3 topRight = new Vector3(position.x + knobSize.x, position.y, 0);

                Vector3 lastCorner = bottomRight;

                knobMaterial.SetPass(0);
                //Background
                GL.Begin(GL.QUADS);
                GL.Color(backgroundColor);

                VertexPointWithUV(topLeft);
                VertexPointWithUV(topRight);
                VertexPointWithUV(bottomRight);
                VertexPointWithUV(bottomleft);

                GL.End();

                //Forground
                GL.Begin(GL.TRIANGLES);
                GL.Color(activeColor * (GUI.enabled ? 1.0f : 0.5f));
                if (angle > 0.0f)
                {
                    VertexPointWithUV(center);
                    VertexPointWithUV(centerRight);
                    VertexPointWithUV(bottomRight);
                    lastCorner = bottomRight;

                    if (angle > (Mathf.PI / 2.0f))
                    {
                        VertexPointWithUV(center);
                        VertexPointWithUV(bottomRight);
                        VertexPointWithUV(bottomleft);
                        lastCorner = bottomleft;

                        if (angle > Mathf.PI)
                        {
                            VertexPointWithUV(center);
                            VertexPointWithUV(bottomleft);
                            VertexPointWithUV(topLeft);
                            lastCorner = topLeft;
                        }
                    }
                    if (angle == (Mathf.PI * (3.0f / 2.0f)))
                    {
                        VertexPointWithUV(center);
                        VertexPointWithUV(topLeft);
                        VertexPointWithUV(topRight);
                        VertexPointWithUV(center);
                        VertexPointWithUV(topRight);
                        VertexPointWithUV(centerRight);
                    }
                    else
                    {
                        //Project the last segment onto the square and draw it.
                        Vector3 projection;
                        float adjustedAngle = angle + Mathf.PI / 4.0f;
                        if (angle < Mathf.PI / 2.0f)
                            projection = bottomRight + new Vector3((knobSize.y / 2 * Mathf.Tan(Mathf.PI / 2.0f - adjustedAngle)) - knobSize.x / 2, 0, 0);
                        else if (angle < Mathf.PI)
                            projection = bottomleft + new Vector3(0, (knobSize.x / 2 * Mathf.Tan(Mathf.PI - adjustedAngle)) - knobSize.y / 2, 0);
                        else
                            projection = topLeft + new Vector3(-(knobSize.y / 2 * Mathf.Tan((Mathf.PI * 3.0f / 2.0f) - adjustedAngle)) + knobSize.x / 2, 0, 0);

                        VertexPointWithUV(center);
                        VertexPointWithUV(lastCorner);
                        VertexPointWithUV(projection);
                    }
                }
                GL.End();
            }

            private float OnRepaint()
            {
                DrawValueArc(GetCurrentValuePercent() * Mathf.PI * (3.0f / 2.0f));

                if (KnobState().isEditing)
                    return DoKeyboardInput();

                if (showValue)
                    PrintValue();

                return currentValue;
            }
        }

        // Show text, fixed Size input
        internal static float Knob(Rect position, Vector2 knobSize, float currentValue, float start, float end, string unit, Color backgroundColor, Color activeColor, bool showValue, int id)
        {
            return new KnobContext(position, knobSize, currentValue, start, end, unit, backgroundColor, activeColor, showValue, id).Handle();
        }

        internal static float OffsetKnob(Rect position, float currentValue, float start, float end, float median, string unit, Color backgroundColor, Color activeColor, GUIStyle knob, int id)
        {
            ///@TODO Implement
            return 0f;
        }
    }
}
