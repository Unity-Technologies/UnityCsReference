// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public class TextField : TextInputFieldBase, INotifyValueChanged<string>
    {
        // TODO: Switch over to default style properties

        // Multiline (lossy behaviour when deactivated)
        bool m_Multiline;
        public bool multiline
        {
            get { return m_Multiline; }
            set
            {
                m_Multiline = value;
                if (!value)
                    text = text.Replace("\n", "");
            }
        }

        // Password field (indirectly lossy behaviour when activated via multiline)
        bool m_IsPasswordField;
        public bool isPasswordField
        {
            get { return m_IsPasswordField; }
            set
            {
                m_IsPasswordField = value;
                if (value)
                    multiline = false;
            }
        }

        public TextField() : this(kMaxLengthNone, false, false, char.MinValue)
        {
        }

        public TextField(int maxLength, bool multiline, bool isPasswordField, char maskChar) : base(maxLength, maskChar)
        {
            this.multiline = multiline;
            this.isPasswordField = isPasswordField;
        }

        protected string m_Value;

        public string value
        {
            get { return m_Value; }
            set
            {
                m_Value = value;
                text = m_Value;
            }
        }

        public void SetValueAndNotify(string newValue)
        {
            if (!EqualityComparer<string>.Default.Equals(value, newValue))
            {
                using (ChangeEvent<string> evt = ChangeEvent<string>.GetPooled(value, newValue))
                {
                    evt.target = this;
                    value = newValue;
                    UIElementsUtility.eventDispatcher.DispatchEvent(evt, panel);
                }
            }
        }

        public void OnValueChanged(EventCallback<ChangeEvent<string>> callback)
        {
            RegisterCallback(callback);
        }

        public override void OnPersistentDataReady()
        {
            base.OnPersistentDataReady();

            string key = GetFullHierarchicalPersistenceKey();

            OverwriteFromPersistedData(this, key);
        }

        internal override void SyncTextEngine()
        {
            editorEngine.multiline = multiline;
            editorEngine.isPasswordField = isPasswordField;

            base.SyncTextEngine();
        }

        internal override void DoRepaint(IStylePainter painter)
        {
            if (isPasswordField)
            {
                // if we use system keyboard we will have normal text returned (hiding symbols is done inside os)
                // so before drawing make sure we hide them ourselves
                string drawText = text;
                text = "".PadRight(text.Length, maskChar);

                if (!hasFocus)
                    base.DoRepaint(painter);
                else
                    DrawWithTextSelectionAndCursor(painter, text);

                text = drawText;
            }
            else
            {
                base.DoRepaint(painter);
            }
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt.GetEventTypeId() == BlurEvent.TypeId())
            {
                SetValueAndNotify(text);
            }
            else if (evt.GetEventTypeId() == KeyDownEvent.TypeId())
            {
                KeyDownEvent kde = evt as KeyDownEvent;
                if (kde.character == '\n')
                {
                    SetValueAndNotify(text);
                }
            }
        }
    }
}
