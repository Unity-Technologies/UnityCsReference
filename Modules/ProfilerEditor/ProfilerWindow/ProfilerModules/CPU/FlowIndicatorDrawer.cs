// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Profiling;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine;

namespace UnityEditorInternal.Profiling
{
    internal static class FlowIndicatorDrawer
    {
        public enum IndicatorAppearanceMode
        {
            NoActiveEvents = 0,
            Active,
            Inactive,
        }

        public static Vector2 textureVisualSize
        {
            get
            {
                return Styles.textureVisualSize;
            }
        }

        // Used by Tests/ProfilerEditorTests/Editor/FlowIndicatorDrawerTests
        internal static int appearanceModeAlphaValuesCount
        {
            get
            {
                return Styles.appearanceModeAlphaValues.Length;
            }
        }

        public static void DrawFlowIndicatorForFlowEvent(RawFrameDataView.FlowEvent flowEvent, Rect sampleRect, IndicatorAppearanceMode indicatorAppearanceMode)
        {
            float indicatorAngularRotationDegrees;
            var flowIndicatorRect = IndicatorRectForFlowEventType(flowEvent.FlowEventType, sampleRect, out indicatorAngularRotationDegrees);
            var color = IndicatorColorForMode(indicatorAppearanceMode);

            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(indicatorAngularRotationDegrees, flowIndicatorRect.center);
            GUI.DrawTexture(flowIndicatorRect, Styles.texture, ScaleMode.StretchToFill, true, 0f, color, 0f, 0f);
            GUI.matrix = matrix;
        }

        static Rect IndicatorRectForFlowEventType(ProfilerFlowEventType flowEventType, Rect sampleRect, out float indicatorAngularRotationDegrees)
        {
            indicatorAngularRotationDegrees = 0f;

            var flowIndicatorPosition = Vector2.zero;
            var texturePadding = Styles.textureSize * Styles.texturePadding01;
            switch (flowEventType)
            {
                case ProfilerFlowEventType.Begin:
                {
                    // Begin arrows anchor to the bottom left corner of the sample rect.
                    flowIndicatorPosition = new Vector2(sampleRect.position.x - texturePadding.x, sampleRect.max.y - texturePadding.y);
                    break;
                }

                case ProfilerFlowEventType.Next:
                {
                    // Next arrows anchor to the bottom left corner of the sample rect. The arrow texture will be rotated by -90deg, so adjust padding accordingly.
                    var halfTextureSize = Styles.textureSize * 0.5f;
                    var adjustedPadding = new Vector2(texturePadding.y, texturePadding.x);
                    flowIndicatorPosition = new Vector2(sampleRect.position.x - adjustedPadding.x, sampleRect.max.y - halfTextureSize.y);
                    indicatorAngularRotationDegrees = -90f;
                    break;
                }

                case ProfilerFlowEventType.End:
                {
                    // End arrows anchor to the lower right corner of the sample rect. The arrow texture will be rotated by 180deg.
                    var halfTextureSize = Styles.textureSize * 0.5f;
                    flowIndicatorPosition = new Vector2(sampleRect.max.x - halfTextureSize.x, sampleRect.max.y - Styles.textureSize.y + texturePadding.y);
                    indicatorAngularRotationDegrees = 180f;
                    break;
                }

                default:
                    break;
            }

            return new Rect(flowIndicatorPosition, Styles.textureSize);
        }

        static Color IndicatorColorForMode(IndicatorAppearanceMode mode)
        {
            var color = Styles.color;
            color.a = Styles.appearanceModeAlphaValues[(int)mode];
            return color;
        }

        static class Styles
        {
            public const string textureName = "ProfilerTimelineFlowMarker";
            public static readonly Texture2D texture = EditorGUIUtility.LoadIcon(textureName);
            public static readonly Vector2 textureSize = new Vector2(16f, 16f);
            // The arrow texture has transparent padding. This value is the normalized padding pixels.
            public static readonly Vector2 texturePadding01 = new Vector2(0.21f, 0.33f);
            // The arrow texture has transparent padding. This value is the visible size of the arrow without the transparent padding.
            public static readonly Vector2 textureVisualSize = textureSize * new Vector3(1f - (texturePadding01.x * 2f), 1f - (texturePadding01.y * 2f));
            public static readonly Color color = new Color(255f, 255f, 255f, 1f);
            public const float noActiveEventsAlpha = 1f;
            public const float activeAlpha = 1f;
            public const float inactiveAlpha = 0.3f;
            public static readonly float[] appearanceModeAlphaValues = new float[]
            {
                noActiveEventsAlpha,
                activeAlpha,
                inactiveAlpha
            };
        }
    }
}
