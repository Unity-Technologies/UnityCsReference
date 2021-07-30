// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements.Experimental;

namespace UnityEngine.UIElements
{
    public partial class VisualElement : ITransitionAnimations
    {
        List<IValueAnimationUpdate> m_RunningAnimations;
        private VisualElementAnimationSystem GetAnimationSystem()
        {
            if (elementPanel != null)
            {
                return elementPanel.GetUpdater(VisualTreeUpdatePhase.Animation) as VisualElementAnimationSystem;
            }

            return null;
        }

        internal void RegisterAnimation(IValueAnimationUpdate anim)
        {
            if (m_RunningAnimations == null)
            {
                m_RunningAnimations = new List<IValueAnimationUpdate>();
            }

            m_RunningAnimations.Add(anim);

            var sys = GetAnimationSystem();

            if (sys != null)
            {
                sys.RegisterAnimation(anim);
            }
        }

        internal void UnregisterAnimation(IValueAnimationUpdate anim)
        {
            if (m_RunningAnimations != null)
            {
                m_RunningAnimations.Remove(anim);
            }

            var sys = GetAnimationSystem();

            if (sys != null)
            {
                sys.UnregisterAnimation(anim);
            }
        }

        private void UnregisterRunningAnimations()
        {
            if (m_RunningAnimations != null && m_RunningAnimations.Count > 0)
            {
                var sys = GetAnimationSystem();

                if (sys != null)
                    sys.UnregisterAnimations(m_RunningAnimations);
            }
        }

        private void RegisterRunningAnimations()
        {
            if (m_RunningAnimations != null && m_RunningAnimations.Count > 0)
            {
                var sys = GetAnimationSystem();

                if (sys != null)
                    sys.RegisterAnimations(m_RunningAnimations);
            }
        }

        ValueAnimation<float> ITransitionAnimations.Start(float from, float to, int durationMs, Action<VisualElement, float> onValueChanged)
        {
            return experimental.animation.Start((e) => from,  to, durationMs, onValueChanged);
        }

        ValueAnimation<Rect> ITransitionAnimations.Start(Rect from, Rect to, int durationMs, Action<VisualElement, Rect> onValueChanged)
        {
            return experimental.animation.Start((e) => from, to, durationMs, onValueChanged);
        }

        ValueAnimation<Color> ITransitionAnimations.Start(Color from, Color to, int durationMs, Action<VisualElement, Color> onValueChanged)
        {
            return experimental.animation.Start((e) => from, to, durationMs, onValueChanged);
        }

        ValueAnimation<Vector3> ITransitionAnimations.Start(Vector3 from, Vector3 to, int durationMs, Action<VisualElement, Vector3> onValueChanged)
        {
            return experimental.animation.Start((e) => from, to, durationMs, onValueChanged);
        }

        ValueAnimation<Vector2> ITransitionAnimations.Start(Vector2 from, Vector2 to, int durationMs, Action<VisualElement, Vector2> onValueChanged)
        {
            return experimental.animation.Start((e) => from, to, durationMs, onValueChanged);
        }

        ValueAnimation<Quaternion> ITransitionAnimations.Start(Quaternion from, Quaternion to, int durationMs, Action<VisualElement, Quaternion> onValueChanged)
        {
            return experimental.animation.Start((e) => from, to, durationMs, onValueChanged);
        }

        ValueAnimation<StyleValues> ITransitionAnimations.Start(StyleValues from, StyleValues to, int durationMs)
        {
            if (from.m_StyleValues == null)
                from.Values();

            if (to.m_StyleValues == null)
                to.Values();

            return Start((e) => from, to, durationMs);
        }

        ValueAnimation<float> ITransitionAnimations.Start(Func<VisualElement, float> fromValueGetter, float to, int durationMs, Action<VisualElement, float> onValueChanged)
        {
            return StartAnimation(ValueAnimation<float>.Create(this, Lerp.Interpolate), fromValueGetter, to, durationMs, onValueChanged);
        }

        ValueAnimation<Rect> ITransitionAnimations.Start(Func<VisualElement, Rect> fromValueGetter, Rect to, int durationMs, Action<VisualElement, Rect> onValueChanged)
        {
            return StartAnimation(ValueAnimation<Rect>.Create(this, Lerp.Interpolate), fromValueGetter, to, durationMs, onValueChanged);
        }

        ValueAnimation<Color> ITransitionAnimations.Start(Func<VisualElement, Color> fromValueGetter, Color to, int durationMs, Action<VisualElement, Color> onValueChanged)
        {
            return StartAnimation(ValueAnimation<Color>.Create(this, Lerp.Interpolate), fromValueGetter, to, durationMs, onValueChanged);
        }

        ValueAnimation<Vector3> ITransitionAnimations.Start(Func<VisualElement, Vector3> fromValueGetter, Vector3 to, int durationMs, Action<VisualElement, Vector3> onValueChanged)
        {
            return StartAnimation(ValueAnimation<Vector3>.Create(this, Lerp.Interpolate), fromValueGetter, to, durationMs, onValueChanged);
        }

        ValueAnimation<Vector2> ITransitionAnimations.Start(Func<VisualElement, Vector2> fromValueGetter, Vector2 to, int durationMs, Action<VisualElement, Vector2> onValueChanged)
        {
            return StartAnimation(ValueAnimation<Vector2>.Create(this, Lerp.Interpolate), fromValueGetter, to, durationMs, onValueChanged);
        }

        ValueAnimation<Quaternion> ITransitionAnimations.Start(Func<VisualElement, Quaternion> fromValueGetter, Quaternion to, int durationMs, Action<VisualElement, Quaternion> onValueChanged)
        {
            return StartAnimation(ValueAnimation<Quaternion>.Create(this, Lerp.Interpolate), fromValueGetter, to, durationMs, onValueChanged);
        }

        private static ValueAnimation<T> StartAnimation<T>(ValueAnimation<T> anim, Func<VisualElement, T> fromValueGetter, T to, int durationMs, Action<VisualElement, T> onValueChanged)
        {
            anim.initialValue = fromValueGetter;
            anim.to = to;
            anim.durationMs = durationMs;
            anim.valueUpdated = onValueChanged;

            anim.Start();
            return anim;
        }

        private static void AssignStyleValues(VisualElement ve, StyleValues src)
        {
            var s = ve.style;

            if (src.m_StyleValues != null)
            {
                foreach (var styleValue in src.m_StyleValues.m_Values)
                {
                    switch (styleValue.id)
                    {
                        case StyleSheets.StylePropertyID.Unknown:
                            break;
                        case StyleSheets.StylePropertyID.MarginLeft:
                            s.marginLeft = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.MarginTop:
                            s.marginTop = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.MarginRight:
                            s.marginRight = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.MarginBottom:
                            s.marginBottom = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.PaddingLeft:
                            s.paddingLeft = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.PaddingTop:
                            s.paddingTop = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.PaddingRight:
                            s.paddingRight = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.PaddingBottom:
                            s.paddingBottom = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.PositionLeft:
                            s.left = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.PositionTop:
                            s.top = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.PositionRight:
                            s.right = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.PositionBottom:
                            s.bottom = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.Width:
                            s.width = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.Height:
                            s.height = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.FlexGrow:
                            s.flexGrow = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.FlexShrink:
                            s.flexShrink = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.BorderLeftWidth:
                            s.borderLeftWidth = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.BorderTopWidth:
                            s.borderTopWidth = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.BorderRightWidth:
                            s.borderRightWidth = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.BorderBottomWidth:
                            s.borderBottomWidth = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.BorderTopLeftRadius:
                            s.borderTopLeftRadius = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.BorderTopRightRadius:
                            s.borderTopRightRadius = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.BorderBottomRightRadius:
                            s.borderBottomRightRadius = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.BorderBottomLeftRadius:
                            s.borderBottomLeftRadius = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.FontSize:
                            s.fontSize = styleValue.number;
                            break;
                        case StyleSheets.StylePropertyID.Color:
                            s.color = styleValue.color;
                            break;
                        case StyleSheets.StylePropertyID.BackgroundColor:
                            s.backgroundColor = styleValue.color;
                            break;
                        case StyleSheets.StylePropertyID.BorderColor:
                            s.borderLeftColor = styleValue.color;
                            s.borderTopColor = styleValue.color;
                            s.borderRightColor = styleValue.color;
                            s.borderBottomColor = styleValue.color;
                            break;
                        case StyleSheets.StylePropertyID.BackgroundImageTintColor:
                            s.unityBackgroundImageTintColor = styleValue.color;
                            break;
                        case StyleSheets.StylePropertyID.Opacity:
                            s.opacity = styleValue.number;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        StyleValues ReadCurrentValues(VisualElement ve, StyleValues targetValuesToRead)
        {
            StyleValues s = new StyleValues();
            var src = ve.resolvedStyle;

            if (targetValuesToRead.m_StyleValues != null)
            {
                foreach (var styleValue in targetValuesToRead.m_StyleValues.m_Values)
                {
                    switch (styleValue.id)
                    {
                        case StyleSheets.StylePropertyID.Unknown:
                            break;
                        case StyleSheets.StylePropertyID.MarginLeft:
                            s.marginLeft = src.marginLeft;
                            break;
                        case StyleSheets.StylePropertyID.MarginTop:
                            s.marginTop = src.marginTop;
                            break;
                        case StyleSheets.StylePropertyID.MarginRight:
                            s.marginRight = src.marginRight;
                            break;
                        case StyleSheets.StylePropertyID.MarginBottom:
                            s.marginBottom = src.marginBottom;
                            break;
                        case StyleSheets.StylePropertyID.PaddingLeft:
                            s.paddingLeft = src.paddingLeft;
                            break;
                        case StyleSheets.StylePropertyID.PaddingTop:
                            s.paddingTop = src.paddingTop;
                            break;
                        case StyleSheets.StylePropertyID.PaddingRight:
                            s.paddingRight = src.paddingRight;
                            break;
                        case StyleSheets.StylePropertyID.PaddingBottom:
                            s.paddingBottom = src.paddingBottom;
                            break;
                        case StyleSheets.StylePropertyID.PositionLeft:
                            s.left = src.left;
                            break;
                        case StyleSheets.StylePropertyID.PositionTop:
                            s.top = src.top;
                            break;
                        case StyleSheets.StylePropertyID.PositionRight:
                            s.right = src.right;
                            break;
                        case StyleSheets.StylePropertyID.PositionBottom:
                            s.bottom = src.bottom;
                            break;
                        case StyleSheets.StylePropertyID.Width:
                            s.width = src.width;
                            break;
                        case StyleSheets.StylePropertyID.Height:
                            s.height = src.height;
                            break;
                        case StyleSheets.StylePropertyID.FlexGrow:
                            s.flexGrow = src.flexGrow;
                            break;
                        case StyleSheets.StylePropertyID.FlexShrink:
                            s.flexShrink = src.flexShrink;
                            break;
                        case StyleSheets.StylePropertyID.BorderLeftWidth:
                            s.borderLeftWidth = src.borderLeftWidth;
                            break;
                        case StyleSheets.StylePropertyID.BorderTopWidth:
                            s.borderTopWidth = src.borderTopWidth;
                            break;
                        case StyleSheets.StylePropertyID.BorderRightWidth:
                            s.borderRightWidth = src.borderRightWidth;
                            break;
                        case StyleSheets.StylePropertyID.BorderBottomWidth:
                            s.borderBottomWidth = src.borderBottomWidth;
                            break;
                        case StyleSheets.StylePropertyID.BorderTopLeftRadius:
                            s.borderTopLeftRadius = src.borderTopLeftRadius;
                            break;
                        case StyleSheets.StylePropertyID.BorderTopRightRadius:
                            s.borderTopRightRadius = src.borderTopRightRadius;
                            break;
                        case StyleSheets.StylePropertyID.BorderBottomRightRadius:
                            s.borderBottomRightRadius = src.borderBottomRightRadius;
                            break;
                        case StyleSheets.StylePropertyID.BorderBottomLeftRadius:
                            s.borderBottomLeftRadius = src.borderBottomLeftRadius;
                            break;
                        case StyleSheets.StylePropertyID.Color:
                            s.color = src.color;
                            break;
                        case StyleSheets.StylePropertyID.BackgroundColor:
                            s.backgroundColor = src.backgroundColor;
                            break;
                        case StyleSheets.StylePropertyID.BorderColor:
                            s.borderColor = src.borderLeftColor;
                            break;
                        case StyleSheets.StylePropertyID.BackgroundImageTintColor:
                            s.unityBackgroundImageTintColor = src.unityBackgroundImageTintColor;
                            break;
                        case StyleSheets.StylePropertyID.Opacity:
                            s.opacity = src.opacity;
                            break;
                        default:
                            break;
                    }
                }
            }

            return s;
        }

        ValueAnimation<StyleValues>  ITransitionAnimations.Start(StyleValues to, int durationMs)
        {
            if (to.m_StyleValues == null)
                to.Values();

            return Start((e) => ReadCurrentValues(e, to), to, durationMs);
        }

        private ValueAnimation<StyleValues> Start(Func<VisualElement, StyleValues> fromValueGetter, StyleValues to, int durationMs)
        {
            return StartAnimation(ValueAnimation<StyleValues>.Create(this, Lerp.Interpolate), fromValueGetter, to, durationMs, AssignStyleValues);
        }

        ValueAnimation<Rect>  ITransitionAnimations.Layout(Rect to, int durationMs)
        {
            return experimental.animation.Start((e) =>
                new Rect(
                    e.resolvedStyle.left,
                    e.resolvedStyle.top,
                    e.resolvedStyle.width,
                    e.resolvedStyle.height
                )
                , to, durationMs,
                (e, c) =>
                {
                    e.style.left = c.x;
                    e.style.top = c.y;
                    e.style.width = c.width;
                    e.style.height = c.height;
                });
        }

        ValueAnimation<Vector2>  ITransitionAnimations.TopLeft(Vector2 to, int durationMs)
        {
            return experimental.animation.Start((e) => new Vector2(e.resolvedStyle.left, e.resolvedStyle.top),
                to, durationMs,
                (e, c) =>
                {
                    e.style.left = c.x;
                    e.style.top = c.y;
                });
        }

        ValueAnimation<Vector2> ITransitionAnimations.Size(Vector2 to, int durationMs)
        {
            return experimental.animation.Start((e) => e.layout.size,
                to, durationMs,
                (e, c) =>
                {
                    e.style.width = c.x;
                    e.style.height = c.y;
                });
        }

        ValueAnimation<float> ITransitionAnimations.Scale(float to, int durationMs)
        {
            return experimental.animation.Start((e) => e.transform.scale.x, to, durationMs, (e, c) => { e.transform.scale = new Vector3(c, c, c); });
        }

        ValueAnimation<Vector3> ITransitionAnimations.Position(Vector3 to, int durationMs)
        {
            return experimental.animation.Start((e) => e.transform.position, to, durationMs, (e, c) => { e.transform.position = c; });
        }

        ValueAnimation<Quaternion> ITransitionAnimations.Rotation(Quaternion to, int durationMs)
        {
            return experimental.animation.Start((e) => e.transform.rotation, to, durationMs, (e, c) => { e.transform.rotation = c; });
        }
    }
}
