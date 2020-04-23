// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace - we explicitly want UnityEditor namespace
namespace UnityEditor
{
    // Holds cache/state of things present in clipboard, for a particular clipboard
    // contents value. Once system clipboard changes, a new state is re-created.
    // Doing queries or gets into the state (e.g. query for presence of color in the clipboard)
    // is cached and figured out only once per state lifetime.
    internal class ClipboardState
    {
        internal string m_RawContents;

        internal bool? m_HasGuid;
        internal GUID m_ValueGuid;
        internal void FetchGuid()
        {
            if (!m_HasGuid.HasValue)
                m_HasGuid = ClipboardParser.ParseGuid(m_RawContents, out m_ValueGuid);
        }

        internal bool? m_HasBool;
        internal bool m_ValueBool;
        internal void FetchBool()
        {
            if (!m_HasBool.HasValue)
                m_HasBool = ClipboardParser.ParseBool(m_RawContents, out m_ValueBool);
        }

        internal bool? m_HasLayerMask;
        internal LayerMask m_ValueLayerMask;
        internal void FetchLayerMask()
        {
            if (!m_HasLayerMask.HasValue)
                m_HasLayerMask = ClipboardParser.ParseLayerMask(m_RawContents, out m_ValueLayerMask);
        }

        internal bool? m_HasVector3;
        internal Vector3 m_ValueVector3;
        internal void FetchVector3()
        {
            if (!m_HasVector3.HasValue)
                m_HasVector3 = ClipboardParser.ParseVector3(m_RawContents, out m_ValueVector3);
        }

        internal bool? m_HasVector2;
        internal Vector2 m_ValueVector2;
        internal void FetchVector2()
        {
            if (!m_HasVector2.HasValue)
                m_HasVector2 = ClipboardParser.ParseVector2(m_RawContents, out m_ValueVector2);
        }

        internal bool? m_HasVector4;
        internal Vector4 m_ValueVector4;
        internal void FetchVector4()
        {
            if (!m_HasVector4.HasValue)
                m_HasVector4 = ClipboardParser.ParseVector4(m_RawContents, out m_ValueVector4);
        }

        internal bool? m_HasQuaternion;
        internal Quaternion m_ValueQuaternion;
        internal void FetchQuaternion()
        {
            if (!m_HasQuaternion.HasValue)
                m_HasQuaternion = ClipboardParser.ParseQuaternion(m_RawContents, out m_ValueQuaternion);
        }

        internal bool? m_HasColor;
        internal Color m_ValueColor;
        internal void FetchColor()
        {
            if (!m_HasColor.HasValue)
                m_HasColor = ClipboardParser.ParseColor(m_RawContents, out m_ValueColor);
        }

        internal bool? m_HasRect;
        internal Rect m_ValueRect;
        internal void FetchRect()
        {
            if (!m_HasRect.HasValue)
                m_HasRect = ClipboardParser.ParseRect(m_RawContents, out m_ValueRect);
        }

        internal bool? m_HasBounds;
        internal Bounds m_ValueBounds;
        internal void FetchBounds()
        {
            if (!m_HasBounds.HasValue)
                m_HasBounds = ClipboardParser.ParseBounds(m_RawContents, out m_ValueBounds);
        }

        internal bool? m_HasObject;
        internal Object m_ValueObject;
        internal void FetchObject()
        {
            if (!m_HasObject.HasValue)
            {
                m_HasObject = ClipboardParser.ParseCustom<ObjectWrapper>(m_RawContents, out var wrapper);
                m_ValueObject = wrapper.ToObject();
            }
        }

        internal bool? m_HasGradient;
        internal Gradient m_ValueGradient;
        internal void FetchGradient()
        {
            if (!m_HasGradient.HasValue)
            {
                m_HasGradient = ClipboardParser.ParseCustom<GradientWrapper>(m_RawContents, out var wrapper);
                m_ValueGradient = wrapper.gradient;
            }
        }

        internal bool? m_HasAnimationCurve;
        internal AnimationCurve m_ValueAnimationCurve;
        internal void FetchAnimationCurve()
        {
            if (!m_HasAnimationCurve.HasValue)
            {
                m_HasAnimationCurve = ClipboardParser.ParseCustom<AnimationCurveWrapper>(m_RawContents, out var wrapper);
                m_ValueAnimationCurve = wrapper.curve;
            }
        }

        readonly Dictionary<Type, object> m_ValuesCustom = new Dictionary<Type, object>();

        internal bool FetchCustom<T>(out T res) where T : new()
        {
            var key = typeof(T);
            if (m_ValuesCustom.TryGetValue(key, out var cached))
            {
                if (cached != null)
                {
                    res = (T)cached;
                    return true;
                }
                res = default;
                return false;
            }
            var ok = ClipboardParser.ParseCustom(m_RawContents, out res);
            m_ValuesCustom.Add(key, ok ? res : default);
            return ok;
        }
    }
}
