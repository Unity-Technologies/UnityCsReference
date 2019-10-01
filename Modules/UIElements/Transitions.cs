// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements.Experimental
{
    public struct StyleValues
    {
        public float top
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.Top).value;
            }
            set
            {
                SetValue(StylePropertyId.Top, value);
            }
        }
        public float left
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.Left).value;
            }
            set
            {
                SetValue(StylePropertyId.Left, value);
            }
        }
        public float width
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.Width).value;
            }
            set
            {
                SetValue(StylePropertyId.Width, value);
            }
        }
        public float height
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.Height).value;
            }
            set
            {
                SetValue(StylePropertyId.Height, value);
            }
        }
        public float right
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.Right).value;
            }
            set
            {
                SetValue(StylePropertyId.Right, value);
            }
        }
        public float bottom
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.Bottom).value;
            }
            set
            {
                SetValue(StylePropertyId.Bottom, value);
            }
        }
        public Color color
        {
            get
            {
                return Values().GetStyleColor(StylePropertyId.Color).value;
            }
            set
            {
                SetValue(StylePropertyId.Color, value);
            }
        }
        public Color backgroundColor
        {
            get
            {
                return Values().GetStyleColor(StylePropertyId.BackgroundColor).value;
            }
            set
            {
                SetValue(StylePropertyId.BackgroundColor, value);
            }
        }
        public Color unityBackgroundImageTintColor
        {
            get
            {
                return Values().GetStyleColor(StylePropertyId.UnityBackgroundImageTintColor).value;
            }
            set
            {
                SetValue(StylePropertyId.UnityBackgroundImageTintColor, value);
            }
        }
        public Color borderColor
        {
            get
            {
                return Values().GetStyleColor(StylePropertyId.BorderColor).value;
            }
            set
            {
                SetValue(StylePropertyId.BorderColor, value);
            }
        }
        public float marginLeft
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.MarginLeft).value;
            }
            set
            {
                SetValue(StylePropertyId.MarginLeft, value);
            }
        }
        public float marginTop
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.MarginTop).value;
            }
            set
            {
                SetValue(StylePropertyId.MarginTop, value);
            }
        }
        public float marginRight
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.MarginRight).value;
            }
            set
            {
                SetValue(StylePropertyId.MarginRight, value);
            }
        }
        public float marginBottom
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.MarginBottom).value;
            }
            set
            {
                SetValue(StylePropertyId.MarginBottom, value);
            }
        }
        public float paddingLeft
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.PaddingLeft).value;
            }
            set
            {
                SetValue(StylePropertyId.PaddingLeft, value);
            }
        }
        public float paddingTop
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.PaddingTop).value;
            }
            set
            {
                SetValue(StylePropertyId.PaddingTop, value);
            }
        }
        public float paddingRight
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.PaddingRight).value;
            }
            set
            {
                SetValue(StylePropertyId.PaddingRight, value);
            }
        }
        public float paddingBottom
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.PaddingBottom).value;
            }
            set
            {
                SetValue(StylePropertyId.PaddingBottom, value);
            }
        }
        public float borderLeftWidth
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.BorderLeftWidth).value;
            }
            set
            {
                SetValue(StylePropertyId.BorderLeftWidth, value);
            }
        }
        public float borderRightWidth
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.BorderRightWidth).value;
            }
            set
            {
                SetValue(StylePropertyId.BorderRightWidth, value);
            }
        }
        public float borderTopWidth
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.BorderTopWidth).value;
            }
            set
            {
                SetValue(StylePropertyId.BorderTopWidth, value);
            }
        }
        public float borderBottomWidth
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.BorderBottomWidth).value;
            }
            set
            {
                SetValue(StylePropertyId.BorderBottomWidth, value);
            }
        }
        public float borderTopLeftRadius
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.BorderTopLeftRadius).value;
            }
            set
            {
                SetValue(StylePropertyId.BorderTopLeftRadius, value);
            }
        }
        public float borderTopRightRadius
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.BorderTopRightRadius).value;
            }
            set
            {
                SetValue(StylePropertyId.BorderTopRightRadius, value);
            }
        }
        public float borderBottomLeftRadius
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.BorderBottomLeftRadius).value;
            }
            set
            {
                SetValue(StylePropertyId.BorderBottomLeftRadius, value);
            }
        }
        public float borderBottomRightRadius
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.BorderBottomRightRadius).value;
            }
            set
            {
                SetValue(StylePropertyId.BorderBottomRightRadius, value);
            }
        }

        public float opacity
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.Opacity).value;
            }
            set
            {
                SetValue(StylePropertyId.Opacity, value);
            }
        }
        public float flexGrow
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.FlexGrow).value;
            }
            set
            {
                SetValue(StylePropertyId.FlexGrow, value);
            }
        }
        public float flexShrink
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyId.FlexShrink).value;
            }
            set
            {
                SetValue(StylePropertyId.FlexGrow, value);
            }
        }

        internal void SetValue(StylePropertyId id, float value)
        {
            var sv = new StyleValue();

            sv.id = id;
            sv.number = value;
            Values().SetStyleValue(sv);
        }

        internal void SetValue(StylePropertyId id, Color value)
        {
            var sv = new StyleValue();

            sv.id = id;
            sv.color = value;
            Values().SetStyleValue(sv);
        }

        internal StyleValueCollection Values()
        {
            if (m_StyleValues == null)
            {
                m_StyleValues = new StyleValueCollection();
            }
            return m_StyleValues;
        }

        internal StyleValueCollection m_StyleValues;
    }

    public interface ITransitionAnimations
    {
        ValueAnimation<float> Start(float from, float to, int durationMs, Action<VisualElement, float> onValueChanged);
        ValueAnimation<Rect> Start(Rect from, Rect to, int durationMs, Action<VisualElement, Rect> onValueChanged);
        ValueAnimation<Color> Start(Color from, Color to, int durationMs, Action<VisualElement, Color> onValueChanged);
        ValueAnimation<Vector3> Start(Vector3 from, Vector3 to, int durationMs, Action<VisualElement, Vector3> onValueChanged);
        ValueAnimation<Vector2> Start(Vector2 from, Vector2 to, int durationMs, Action<VisualElement, Vector2> onValueChanged);
        ValueAnimation<Quaternion> Start(Quaternion from, Quaternion to, int durationMs, Action<VisualElement, Quaternion> onValueChanged);

        ValueAnimation<StyleValues> Start(StyleValues from, StyleValues to, int durationMs);
        ValueAnimation<StyleValues> Start(StyleValues to, int durationMs);

        ValueAnimation<float> Start(Func<VisualElement, float> fromValueGetter, float to, int durationMs, Action<VisualElement, float> onValueChanged);
        ValueAnimation<Rect> Start(Func<VisualElement, Rect> fromValueGetter, Rect to, int durationMs, Action<VisualElement, Rect> onValueChanged);
        ValueAnimation<Color> Start(Func<VisualElement, Color> fromValueGetter, Color to, int durationMs, Action<VisualElement, Color> onValueChanged);
        ValueAnimation<Vector3> Start(Func<VisualElement, Vector3> fromValueGetter, Vector3 to, int durationMs, Action<VisualElement, Vector3> onValueChanged);
        ValueAnimation<Vector2> Start(Func<VisualElement, Vector2> fromValueGetter, Vector2 to, int durationMs, Action<VisualElement, Vector2> onValueChanged);
        ValueAnimation<Quaternion> Start(Func<VisualElement, Quaternion> fromValueGetter, Quaternion to, int durationMs, Action<VisualElement, Quaternion> onValueChanged);

        ValueAnimation<Rect> Layout(Rect to, int durationMs);
        ValueAnimation<Vector2> TopLeft(Vector2 to, int durationMs);
        ValueAnimation<Vector2> Size(Vector2 to, int durationMs);

        ValueAnimation<float> Scale(float to, int duration);
        ValueAnimation<Vector3> Position(Vector3 to, int duration);
        ValueAnimation<Quaternion> Rotation(Quaternion to, int duration);
    }


    static class Lerp
    {
        public static float Interpolate(float start, float end, float ratio)
        {
            return Mathf.LerpUnclamped(start, end, ratio);
        }

        public static int Interpolate(int start, int end, float ratio)
        {
            return Mathf.RoundToInt(Mathf.LerpUnclamped(start, end, ratio));
        }

        public static Rect Interpolate(Rect r1, Rect r2, float ratio)
        {
            return new Rect(Mathf.LerpUnclamped(r1.x, r2.x, ratio)
                , Mathf.LerpUnclamped(r1.y, r2.y, ratio)
                , Mathf.LerpUnclamped(r1.width, r2.width, ratio)
                , Mathf.LerpUnclamped(r1.height, r2.height, ratio));
        }

        public static Color Interpolate(Color start, Color end, float ratio)
        {
            return Color.LerpUnclamped(start, end, ratio);
        }

        public static Vector2 Interpolate(Vector2 start, Vector2 end, float ratio)
        {
            return Vector2.LerpUnclamped(start, end, ratio);
        }

        public static Vector3 Interpolate(Vector3 start, Vector3 end, float ratio)
        {
            return Vector3.LerpUnclamped(start, end, ratio);
        }

        public static Quaternion Interpolate(Quaternion start, Quaternion end, float ratio)
        {
            return Quaternion.SlerpUnclamped(start, end, ratio);
        }

        internal static StyleValues Interpolate(StyleValues start, StyleValues end, float ratio)
        {
            StyleValues result = new StyleValues();

            //we assume both start/end have same values

            foreach (var endValue in end.m_StyleValues.m_Values)
            {
                StyleValue startValue = new StyleValue();

                if (!start.m_StyleValues.TryGetStyleValue(endValue.id, ref startValue))
                {
                    throw new ArgumentException("Start StyleValues must contain the same values as end values. Missing property:" + endValue.id);
                }

                switch (endValue.id)
                {
                    case StylePropertyId.MarginLeft:
                    case StylePropertyId.MarginTop:
                    case StylePropertyId.MarginRight:
                    case StylePropertyId.MarginBottom:
                    case StylePropertyId.PaddingLeft:
                    case StylePropertyId.PaddingTop:
                    case StylePropertyId.PaddingRight:
                    case StylePropertyId.PaddingBottom:
                    case StylePropertyId.Left:
                    case StylePropertyId.Top:
                    case StylePropertyId.Right:
                    case StylePropertyId.Bottom:
                    case StylePropertyId.Width:
                    case StylePropertyId.Height:
                    case StylePropertyId.MinWidth:
                    case StylePropertyId.MinHeight:
                    case StylePropertyId.MaxWidth:
                    case StylePropertyId.MaxHeight:
                    case StylePropertyId.FlexBasis:
                    case StylePropertyId.FlexGrow:
                    case StylePropertyId.FlexShrink:
                    case StylePropertyId.BorderLeftWidth:
                    case StylePropertyId.BorderTopWidth:
                    case StylePropertyId.BorderRightWidth:
                    case StylePropertyId.BorderBottomWidth:
                    case StylePropertyId.BorderTopLeftRadius:
                    case StylePropertyId.BorderTopRightRadius:
                    case StylePropertyId.BorderBottomRightRadius:
                    case StylePropertyId.BorderBottomLeftRadius:
                    case StylePropertyId.FontSize:
                    case StylePropertyId.Opacity:
                        //We've got floats!
                    {
                        result.SetValue(endValue.id, Lerp.Interpolate(startValue.number, endValue.number, ratio));
                    }
                    break;
                    case StylePropertyId.Color:
                    case StylePropertyId.BackgroundColor:
                    case StylePropertyId.BorderColor:
                    case StylePropertyId.UnityBackgroundImageTintColor:
                        //We've got colors!
                    {
                        result.SetValue(endValue.id, Lerp.Interpolate(startValue.color, endValue.color, ratio));
                    }

                    break;
                    case StylePropertyId.Position:

                    case StylePropertyId.FlexDirection:
                    case StylePropertyId.FlexWrap:
                    case StylePropertyId.JustifyContent:
                    case StylePropertyId.AlignContent:
                    case StylePropertyId.AlignSelf:
                    case StylePropertyId.AlignItems:
                    case StylePropertyId.UnityTextAlign:
                    case StylePropertyId.WhiteSpace:
                    case StylePropertyId.UnityFont:
                    case StylePropertyId.UnityFontStyleAndWeight:
                    case StylePropertyId.UnityBackgroundScaleMode:
                    case StylePropertyId.Visibility:
                    case StylePropertyId.Overflow:
                    case StylePropertyId.Display:
                    case StylePropertyId.BackgroundImage:
                    case StylePropertyId.Custom:
                    case StylePropertyId.Unknown:
                    case StylePropertyId.UnitySliceLeft:
                    case StylePropertyId.UnitySliceTop:
                    case StylePropertyId.UnitySliceRight:
                    case StylePropertyId.UnitySliceBottom:
                    case StylePropertyId.BorderRadius:
                    case StylePropertyId.BorderWidth:
                    case StylePropertyId.Margin:
                    case StylePropertyId.Padding:
                    case StylePropertyId.Flex:
                    case StylePropertyId.Cursor:
                    default:
                        throw new ArgumentException("Style Value can't be animated");
                }
            }

            return result;
        }
    }
}
