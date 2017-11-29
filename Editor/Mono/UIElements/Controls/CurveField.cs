// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.Experimental.UIElements
{
    public class CurveField : VisualElement, INotifyValueChanged<AnimationCurve>
    {
        private const string k_CurveColorProperty = "curve-color";
        public Rect ranges { get; set; }

        StyleValue<Color> m_CurveColor;
        private Color curveColor
        {
            get
            {
                return m_CurveColor.GetSpecifiedValueOrDefault(Color.green);
            }
        }

        private bool m_ValueNull;
        private AnimationCurve m_Value;
        private bool m_TextureDirty;

        public AnimationCurve value
        {
            get
            {
                if (m_ValueNull) return null;
                AnimationCurve curveCopy = new AnimationCurve();
                curveCopy.keys = m_Value.keys;
                curveCopy.preWrapMode = m_Value.preWrapMode;
                curveCopy.postWrapMode = m_Value.postWrapMode;

                return curveCopy;
            }
            set
            {
                //I need to have total ownership of the curve, I won't be able to know if it is changed outside. so I'm duplicating it.

                if (value != null || !m_ValueNull) // let's not reinitialize an initialized curve
                {
                    m_ValueNull = value == null;
                    if (!m_ValueNull)
                    {
                        m_Value.keys = value.keys;
                        m_Value.preWrapMode = value.preWrapMode;
                        m_Value.postWrapMode = value.postWrapMode;
                    }
                    else
                    {
                        m_Value.keys = new Keyframe[0];
                        m_Value.preWrapMode = WrapMode.Once;
                        m_Value.postWrapMode = WrapMode.Once;
                    }
                }
                m_TextureDirty = true;


                Dirty(ChangeType.Repaint);
            }
        }
        public CurveField()
        {
            ranges = Rect.zero;

            VisualElement borderElement = new VisualElement() { name = "border", pickingMode = PickingMode.Ignore };
            Add(borderElement);

            m_Value = new AnimationCurve(new Keyframe[0]);
        }

        void OnDetach()
        {
            if (style.backgroundImage.value != null)
            {
                Object.DestroyImmediate(style.backgroundImage.value);
                style.backgroundImage = null;
                m_TextureDirty = true;
            }
        }

        public void SetValueAndNotify(AnimationCurve newValue)
        {
            using (ChangeEvent<AnimationCurve> evt = ChangeEvent<AnimationCurve>.GetPooled(value, newValue))
            {
                evt.target = this;
                value = newValue;
                UIElementsUtility.eventDispatcher.DispatchEvent(evt, panel);
            }
        }

        public void OnValueChanged(EventCallback<ChangeEvent<AnimationCurve>> callback)
        {
            RegisterCallback(callback);
        }

        protected override void OnStyleResolved(ICustomStyle style)
        {
            base.OnStyleResolved(style);

            style.ApplyCustomProperty(k_CurveColorProperty, ref m_CurveColor);
        }

        void OnCurveClick()
        {
            if (!enabledInHierarchy)
                return;

            CurveEditorSettings settings = new CurveEditorSettings();
            if (m_Value == null)
                m_Value = new AnimationCurve();
            CurveEditorWindow.curve = m_Value;

            CurveEditorWindow.color = curveColor;
            CurveEditorWindow.instance.Show(OnCurveChanged, settings);
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.GetEventTypeId() == MouseDownEvent.TypeId())
                OnCurveClick();
            else if (evt.GetEventTypeId() == DetachFromPanelEvent.TypeId())
                OnDetach();
        }

        void OnCurveChanged(AnimationCurve curve)
        {
            CurveEditorWindow.curve = m_Value;
            SetValueAndNotify(m_Value);
        }

        private void SendChangeEvent(AnimationCurve newValue)
        {
            using (ChangeEvent<AnimationCurve> evt = ChangeEvent<AnimationCurve>.GetPooled(value, newValue))
            {
                evt.target = this;
                value = newValue;
                UIElementsUtility.eventDispatcher.DispatchEvent(evt, panel);
            }
        }

        public override void OnPersistentDataReady()
        {
            base.OnPersistentDataReady();
            m_TextureDirty = true;
        }

        public override void DoRepaint()
        {
            if (m_TextureDirty)
            {
                m_TextureDirty = false;
                int previewWidth = (int)layout.width;
                int previewHeight = (int)layout.height;

                Rect rangeRect = new Rect(0, 0, 1, 1);

                if (ranges.width > 0 && ranges.height > 0)
                {
                    rangeRect = ranges;
                }
                else if (!m_ValueNull && m_Value.keys.Length > 1)
                {
                    float xMin = Mathf.Infinity;
                    float yMin = Mathf.Infinity;
                    float xMax = -Mathf.Infinity;
                    float yMax = -Mathf.Infinity;

                    for (int i = 0; i < m_Value.keys.Length; ++i)
                    {
                        float y = m_Value.keys[i].value;
                        float x = m_Value.keys[i].time;
                        if (xMin > x)
                        {
                            xMin = x;
                        }
                        if (xMax < x)
                        {
                            xMax = x;
                        }
                        if (yMin > y)
                        {
                            yMin = y;
                        }
                        if (yMax < y)
                        {
                            yMax = y;
                        }
                    }

                    if (yMin == yMax)
                    {
                        yMax = yMin + 1;
                    }
                    if (xMin == xMax)
                    {
                        xMax = xMin + 1;
                    }

                    rangeRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
                }

                if (previewHeight > 0 && previewWidth > 0)
                {
                    if (!m_ValueNull)
                    {
                        style.backgroundImage = AnimationCurvePreviewCache.GenerateCurvePreview(
                                previewWidth,
                                previewHeight,
                                rangeRect,
                                m_Value,
                                curveColor,
                                style.backgroundImage.value);
                    }
                    else
                    {
                        style.backgroundImage = null;
                    }
                }
            }

            base.DoRepaint();
        }
    }
}
