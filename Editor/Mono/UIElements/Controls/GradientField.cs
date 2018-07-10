// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.Experimental.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.UIElements
{
    public class GradientField : BaseField<Gradient>
    {
        static readonly GradientColorKey k_WhiteKeyBegin = new GradientColorKey(Color.white, 0);
        static readonly GradientColorKey k_WhiteKeyEnd = new GradientColorKey(Color.white, 1);
        static readonly GradientAlphaKey k_AlphaKeyBegin = new GradientAlphaKey(1, 0);
        static readonly GradientAlphaKey k_AlphaKeyEnd = new GradientAlphaKey(1, 1);
        public new class UxmlFactory : UxmlFactory<GradientField, UxmlTraits> {}

        public new class UxmlTraits : BaseField<Gradient>.UxmlTraits {}

        private bool m_ValueNull;
        public override Gradient value
        {
            get
            {
                if (m_ValueNull) return null;

                return GradientCopy(m_Value);
            }
            set
            {
                if (value != null || !m_ValueNull)  // let's not reinitialize an initialized gradient
                {
                    if (panel != null)
                    {
                        using (ChangeEvent<Gradient> evt = ChangeEvent<Gradient>.GetPooled(m_Value, value))
                        {
                            evt.target = this;
                            SetValueWithoutNotify(value);
                            SendEvent(evt);
                        }
                    }
                    else
                    {
                        SetValueWithoutNotify(value);
                    }
                }
            }
        }

        internal static Gradient GradientCopy(Gradient other)
        {
            Gradient gradientCopy = new Gradient();
            gradientCopy.colorKeys = other.colorKeys;
            gradientCopy.alphaKeys = other.alphaKeys;
            gradientCopy.mode = other.mode;
            return gradientCopy;
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

            if ((evt as MouseDownEvent)?.button == (int)MouseButton.LeftMouse || (evt as KeyDownEvent)?.character == '\n')
                ShowGradientPicker();
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

        void ShowGradientPicker()
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

                IncrementVersion(VersionChangeType.Repaint); // since the Texture2D object can be reused, force dirty because the backgroundImage change will only trigger the Dirty if the Texture2D objects are different.
            }
        }

        void OnGradientChanged(Gradient newValue)
        {
            value = newValue;

            GradientPreviewCache.ClearCache(); // needed because GradientEditor itself uses the cache and will no invalidate it on changes.
            IncrementVersion(VersionChangeType.Repaint);
        }

        public override void SetValueWithoutNotify(Gradient newValue)
        {
            m_ValueNull = newValue == null;
            if (!m_ValueNull)
            {
                m_Value.colorKeys = newValue.colorKeys;
                m_Value.alphaKeys = newValue.alphaKeys;
                m_Value.mode = newValue.mode;
            }
            else // restore the internal gradient to the default state.
            {
                m_Value.colorKeys = new[] { k_WhiteKeyBegin, k_WhiteKeyEnd };
                m_Value.alphaKeys = new[] { k_AlphaKeyBegin, k_AlphaKeyEnd };
                m_Value.mode = GradientMode.Blend;
            }
            UpdateGradientTexture();
        }

        [Obsolete("This method is replaced by simply using this.value. The default behaviour has been changed to notify when changed. If the behaviour is not to be notified, SetValueWithoutNotify() must be used.", false)]
        public override void SetValueAndNotify(Gradient newValue)
        {
            value = newValue;
        }
    }
}
