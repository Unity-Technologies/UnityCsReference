// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    public sealed partial class EditorGUILayout
    {
        internal static float AngularDial(
            GUIContent label, float angle, Texture thumbTexture, GUIStyle background, GUIStyle thumb,
            params GUILayoutOption[] options
            )
        {
            var hasLabel = label != null && label != GUIContent.none;
            var height = background == null || background.fixedHeight == 0 ?
                EditorGUIUtility.singleLineHeight : background.fixedHeight;
            var minWidth = (hasLabel ? EditorGUIUtility.labelWidth : 0f)
                + (background != null ? background.fixedWidth : 0f);
            var maxWidth = kLabelFloatMaxW;
            var rect = GUILayoutUtility.GetRect(minWidth, maxWidth, height, height, background, options);
            s_LastRect = rect;
            return EditorGUI.AngularDial(rect, label, angle, thumbTexture, background, thumb);
        }
    }

    public sealed partial class EditorGUI
    {
        internal static float AngularDial(
            Rect rect, GUIContent label, float angle, Texture thumbTexture, GUIStyle background, GUIStyle thumb
            )
        {
            var id = GUIUtility.GetControlID(FocusType.Passive);
            var evt = Event.current;

            if (label != null && label != GUIContent.none)
                rect = PrefixLabel(rect, id, label);

            var diameter = Mathf.Min(rect.width, rect.height);
            var thumbDimensions = thumb == null || thumb == GUIStyle.none
                ? Vector2.zero
                : thumb.CalcSize(GUIContent.Temp(thumbTexture));
            var thumbSize = Mathf.Max(thumbDimensions.x, thumbDimensions.y);

            switch (evt.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (rect.Contains(evt.mousePosition))
                    {
                        var p = evt.mousePosition - rect.center;
                        var r = Mathf.Sqrt(p.x * p.x + p.y * p.y);
                        if (r < diameter * 0.5f && r > diameter * 0.5f - thumbSize)
                            return UseAngularDialEventAndGetAngle(id, evt, rect.center, angle);
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                        return UseAngularDialEventAndGetAngle(id, evt, rect.center, angle);
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        return UseAngularDialEventAndGetAngle(id, evt, rect.center, angle);
                    }
                    break;
                case EventType.Repaint:
                    var hover = false;
                    if (rect.Contains(evt.mousePosition))
                    {
                        var p = evt.mousePosition - rect.center;
                        var r = Mathf.Sqrt(p.x * p.x + p.y * p.y);
                        hover = r<diameter * 0.5f && r> diameter * 0.5f - thumbSize;
                    }
                    var active = GUIUtility.hotControl == id;

                    if (background != null && background != GUIStyle.none)
                        background.Draw(rect, hover, active, false, false);

                    if (thumb != null && thumb != GUIStyle.none)
                    {
                        var thumbCenterRadius = diameter * 0.5f - thumbSize * 0.5f;
                        // negate angle since gui space goes top to bottom
                        var radians = -Mathf.Deg2Rad * angle;
                        var thumbCenter =
                            new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * thumbCenterRadius + rect.center;
                        var size = thumb.CalcSize(GUIContent.none);
                        if (thumb.fixedWidth == 0f)
                            size.x = Mathf.Max(size.x, thumbSize);
                        if (thumb.fixedHeight == 0f)
                            size.y = Mathf.Max(size.y, thumbSize);
                        var thumbRect = new Rect { size = size, center = thumbCenter };
                        thumbRect.center = thumbCenter;

                        thumb.Draw(thumbRect, thumbTexture, thumbRect.Contains(evt.mousePosition), active, false, false);
                    }
                    break;
            }

            return angle;
        }

        private static float UseAngularDialEventAndGetAngle(int id, Event evt, Vector2 center, float angle)
        {
            GUIUtility.hotControl = evt.type == EventType.MouseUp ? 0 : id;
            EditorGUIUtility.keyboardControl = 0;
            GUI.changed = true;
            evt.Use();

            var normalized = (evt.mousePosition - center).normalized;
            // negate angle since gui space goes top to bottom
            var newAngle = -Mathf.Rad2Deg
                * Mathf.Acos(normalized.x)
                * Mathf.Sign(Vector2.Dot(Vector2.up, normalized));

            // accumulate delta angle to allow for multiple revolutions
            return angle + Mathf.DeltaAngle(angle, newAngle);
        }
    }
}
