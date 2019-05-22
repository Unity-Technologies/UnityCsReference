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
                return Values().GetStyleFloat(StylePropertyID.PositionTop).value;
            }
            set
            {
                SetValue(StylePropertyID.PositionTop, value);
            }
        }
        public float left
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyID.PositionLeft).value;
            }
            set
            {
                SetValue(StylePropertyID.PositionLeft, value);
            }
        }
        public float width
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyID.Width).value;
            }
            set
            {
                SetValue(StylePropertyID.Width, value);
            }
        }
        public float height
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyID.Height).value;
            }
            set
            {
                SetValue(StylePropertyID.Height, value);
            }
        }
        public float right
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyID.PositionRight).value;
            }
            set
            {
                SetValue(StylePropertyID.PositionRight, value);
            }
        }
        public float bottom
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyID.PositionBottom).value;
            }
            set
            {
                SetValue(StylePropertyID.PositionBottom, value);
            }
        }
        public Color color
        {
            get
            {
                return Values().GetStyleColor(StylePropertyID.Color).value;
            }
            set
            {
                SetValue(StylePropertyID.Color, value);
            }
        }
        public Color backgroundColor
        {
            get
            {
                return Values().GetStyleColor(StylePropertyID.BackgroundColor).value;
            }
            set
            {
                SetValue(StylePropertyID.BackgroundColor, value);
            }
        }
        public Color unityBackgroundImageTintColor
        {
            get
            {
                return Values().GetStyleColor(StylePropertyID.BackgroundImageTintColor).value;
            }
            set
            {
                SetValue(StylePropertyID.BackgroundImageTintColor, value);
            }
        }
        public Color borderColor
        {
            get
            {
                return Values().GetStyleColor(StylePropertyID.BorderColor).value;
            }
            set
            {
                SetValue(StylePropertyID.BorderColor, value);
            }
        }
        public float marginLeft
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyID.MarginLeft).value;
            }
            set
            {
                SetValue(StylePropertyID.MarginLeft, value);
            }
        }
        public float marginTop
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyID.MarginTop).value;
            }
            set
            {
                SetValue(StylePropertyID.MarginTop, value);
            }
        }
        public float marginRight
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyID.MarginRight).value;
            }
            set
            {
                SetValue(StylePropertyID.MarginRight, value);
            }
        }
        public float marginBottom
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyID.MarginBottom).value;
            }
            set
            {
                SetValue(StylePropertyID.MarginBottom, value);
            }
        }
        public float paddingLeft
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyID.PaddingLeft).value;
            }
            set
            {
                SetValue(StylePropertyID.PaddingLeft, value);
            }
        }
        public float paddingTop
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyID.PaddingTop).value;
            }
            set
            {
                SetValue(StylePropertyID.PaddingTop, value);
            }
        }
        public float paddingRight
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyID.PaddingRight).value;
            }
            set
            {
                SetValue(StylePropertyID.PaddingRight, value);
            }
        }
        public float paddingBottom
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyID.PaddingBottom).value;
            }
            set
            {
                SetValue(StylePropertyID.PaddingBottom, value);
            }
        }
        public float borderLeftWidth
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyID.BorderLeftWidth).value;
            }
            set
            {
                SetValue(StylePropertyID.BorderLeftWidth, value);
            }
        }
        public float borderRightWidth
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyID.BorderRightWidth).value;
            }
            set
            {
                SetValue(StylePropertyID.BorderRightWidth, value);
            }
        }
        public float borderTopWidth
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyID.BorderTopWidth).value;
            }
            set
            {
                SetValue(StylePropertyID.BorderTopWidth, value);
            }
        }
        public float borderBottomWidth
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyID.BorderBottomWidth).value;
            }
            set
            {
                SetValue(StylePropertyID.BorderBottomWidth, value);
            }
        }
        public float borderTopLeftRadius
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyID.BorderTopLeftRadius).value;
            }
            set
            {
                SetValue(StylePropertyID.BorderTopLeftRadius, value);
            }
        }
        public float borderTopRightRadius
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyID.BorderTopRightRadius).value;
            }
            set
            {
                SetValue(StylePropertyID.BorderTopRightRadius, value);
            }
        }
        public float borderBottomLeftRadius
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyID.BorderBottomLeftRadius).value;
            }
            set
            {
                SetValue(StylePropertyID.BorderBottomLeftRadius, value);
            }
        }
        public float borderBottomRightRadius
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyID.BorderBottomRightRadius).value;
            }
            set
            {
                SetValue(StylePropertyID.BorderBottomRightRadius, value);
            }
        }

        public float opacity
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyID.Opacity).value;
            }
            set
            {
                SetValue(StylePropertyID.Opacity, value);
            }
        }
        public float flexGrow
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyID.FlexGrow).value;
            }
            set
            {
                SetValue(StylePropertyID.FlexGrow, value);
            }
        }
        public float flexShrink
        {
            get
            {
                return Values().GetStyleFloat(StylePropertyID.FlexShrink).value;
            }
            set
            {
                SetValue(StylePropertyID.FlexGrow, value);
            }
        }

        internal void SetValue(StylePropertyID id, float value)
        {
            var sv = new StyleValue();

            sv.id = id;
            sv.number = value;
            Values().SetStyleValue(sv);
        }

        internal void SetValue(StylePropertyID id, Color value)
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
                    case StylePropertyID.MarginLeft:
                    case StylePropertyID.MarginTop:
                    case StylePropertyID.MarginRight:
                    case StylePropertyID.MarginBottom:
                    case StylePropertyID.PaddingLeft:
                    case StylePropertyID.PaddingTop:
                    case StylePropertyID.PaddingRight:
                    case StylePropertyID.PaddingBottom:
                    case StylePropertyID.PositionLeft:
                    case StylePropertyID.PositionTop:
                    case StylePropertyID.PositionRight:
                    case StylePropertyID.PositionBottom:
                    case StylePropertyID.Width:
                    case StylePropertyID.Height:
                    case StylePropertyID.MinWidth:
                    case StylePropertyID.MinHeight:
                    case StylePropertyID.MaxWidth:
                    case StylePropertyID.MaxHeight:
                    case StylePropertyID.FlexBasis:
                    case StylePropertyID.FlexGrow:
                    case StylePropertyID.FlexShrink:
                    case StylePropertyID.BorderLeftWidth:
                    case StylePropertyID.BorderTopWidth:
                    case StylePropertyID.BorderRightWidth:
                    case StylePropertyID.BorderBottomWidth:
                    case StylePropertyID.BorderTopLeftRadius:
                    case StylePropertyID.BorderTopRightRadius:
                    case StylePropertyID.BorderBottomRightRadius:
                    case StylePropertyID.BorderBottomLeftRadius:
                    case StylePropertyID.FontSize:
                    case StylePropertyID.Opacity:
                        //We've got floats!
                    {
                        result.SetValue(endValue.id, Lerp.Interpolate(startValue.number, endValue.number, ratio));
                    }
                    break;
                    case StylePropertyID.Color:
                    case StylePropertyID.BackgroundColor:
                    case StylePropertyID.BorderColor:
                    case StylePropertyID.BackgroundImageTintColor:
                        //We've got floats!
                    {
                        result.SetValue(endValue.id, Lerp.Interpolate(startValue.color, endValue.color, ratio));
                    }

                    break;
                    case StylePropertyID.Position:

                    case StylePropertyID.FlexDirection:
                    case StylePropertyID.FlexWrap:
                    case StylePropertyID.JustifyContent:
                    case StylePropertyID.AlignContent:
                    case StylePropertyID.AlignSelf:
                    case StylePropertyID.AlignItems:
                    case StylePropertyID.UnityTextAlign:
                    case StylePropertyID.WhiteSpace:
                    case StylePropertyID.Font:
                    case StylePropertyID.FontStyleAndWeight:
                    case StylePropertyID.BackgroundScaleMode:
                    case StylePropertyID.Visibility:
                    case StylePropertyID.Overflow:
                    case StylePropertyID.Display:
                    case StylePropertyID.BackgroundImage:
                    case StylePropertyID.Custom:
                    case StylePropertyID.Unknown:
                    case StylePropertyID.SliceLeft:
                    case StylePropertyID.SliceTop:
                    case StylePropertyID.SliceRight:
                    case StylePropertyID.SliceBottom:
                    case StylePropertyID.BorderRadius:
                    case StylePropertyID.BorderWidth:
                    case StylePropertyID.Margin:
                    case StylePropertyID.Padding:
                    case StylePropertyID.Flex:
                    case StylePropertyID.Cursor:
                    default:
                        throw new ArgumentException("Style Value can't be animated");
                }
            }

            return result;
        }
    }
}
