// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;

namespace UnityEditor.Experimental.UIElements
{
    public class GradientField : VisualElement, INotifyValueChanged<Gradient>
    {
        private bool m_ValueNull;
        Gradient m_Value;
        public Gradient value
        {
            get
            {
                if (m_ValueNull) return null;
                Gradient gradientCopy = new Gradient();
                gradientCopy.colorKeys = m_Value.colorKeys;
                gradientCopy.alphaKeys = m_Value.alphaKeys;
                gradientCopy.mode = m_Value.mode;

                return m_Value;
            }
            set
            {
                if (value != null || !m_ValueNull)  // let's not reinitialize an initialized gradient
                {
                    m_ValueNull = value == null;
                    if (!m_ValueNull)
                    {
                        m_Value.colorKeys = value.colorKeys;
                        m_Value.alphaKeys = value.alphaKeys;
                        m_Value.mode = value.mode;
                    }
                    else // restore the internal gradient to the default state.
                    {
                        m_Value.colorKeys = new GradientColorKey[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) };
                        m_Value.alphaKeys = new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) };
                        m_Value.mode = GradientMode.Blend;
                    }
                }

                UpdateGradientTexture();
            }
        }

        public GradientField()
        {
            VisualElement borderElement = new VisualElement() { name = "border", pickingMode = PickingMode.Ignore };
            Add(borderElement);

            m_Value = new Gradient();
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.GetEventTypeId() == MouseDownEvent.TypeId())
                OnClick();
            else if (evt.GetEventTypeId() == DetachFromPanelEvent.TypeId())
                OnDetach();
            else if (evt.GetEventTypeId() == AttachToPanelEvent.TypeId())
                OnAttach();
        }

        void OnDetach()
        {
            if (style.backgroundImage.value != null)
            {
                Object.DestroyImmediate(style.backgroundImage.value);
                style.backgroundImage = null;
            }
        }

        void OnAttach()
        {
            UpdateGradientTexture();
        }

        void OnClick()
        {
            GradientPicker.Show(m_Value, true, OnGradientChanged);
        }

        public override void OnPersistentDataReady()
        {
            base.OnPersistentDataReady();
            UpdateGradientTexture();
        }

        void UpdateGradientTexture()
        {
            if (m_ValueNull)
            {
                style.backgroundImage = null;
            }
            else
            {
                Texture2D gradientTexture = UnityEditorInternal.GradientPreviewCache.GenerateGradientPreview(value, style.backgroundImage.value);

                style.backgroundImage = gradientTexture;
            }
        }

        void OnGradientChanged(Gradient newValue)
        {
            SetValueAndNotify(newValue);

            GradientPreviewCache.ClearCache(); // needed because GradientEditor itself uses the cache and will no invalidate it on changes.
            Dirty(ChangeType.Repaint);
        }

        public void SetValueAndNotify(Gradient newValue)
        {
            using (ChangeEvent<Gradient> evt = ChangeEvent<Gradient>.GetPooled(value, newValue))
            {
                evt.target = this;
                value = newValue;
                UIElementsUtility.eventDispatcher.DispatchEvent(evt, panel);
            }
        }

        public void OnValueChanged(EventCallback<ChangeEvent<Gradient>> callback)
        {
            RegisterCallback(callback);
        }

        public override void DoRepaint()
        {
            //Start by drawing the checkerboard background for alpha gradients.
            Texture2D backgroundTexture = GradientEditor.GetBackgroundTexture();
            var painter = elementPanel.stylePainter;
            var painterParams = painter.GetDefaultTextureParameters(this);
            painterParams.texture = backgroundTexture;
            painter.DrawTexture(painterParams);

            base.DoRepaint();
        }
    }
}
