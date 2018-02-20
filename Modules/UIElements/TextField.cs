// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public class TextField : TextInputFieldBase<string>
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
        public override bool isPasswordField
        {
            set
            {
                base.isPasswordField = value;
                if (value)
                    multiline = false;
            }
        }

        public TextField() : this(kMaxLengthNone, false, false, char.MinValue)
        {
        }

        public TextField(int maxLength, bool multiline, bool isPasswordField, char maskChar) : base(maxLength, maskChar)
        {
            m_Value = "";
            this.multiline = multiline;
            this.isPasswordField = isPasswordField;
        }

        protected string m_Value;

        public override string value
        {
            get { return m_Value; }
            set
            {
                m_Value = value;
                text = m_Value;
            }
        }

        public void SelectAndReplaceCurrentWord(string newWord)
        {
            editorEngine.SelectCurrentWord();
            editorEngine.ReplaceSelection(newWord);
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
                string drawText = "".PadRight(text.Length, maskChar);
                if (!hasFocus)
                {
                    // We don't have the focus, don't draw the selection and cursor
                    painter.DrawBackground(this);
                    painter.DrawBorder(this);
                    if (!string.IsNullOrEmpty(drawText) && contentRect.width > 0.0f && contentRect.height > 0.0f)
                    {
                        var textParams = painter.GetDefaultTextParameters(this);
                        textParams.text = drawText;
                        painter.DrawText(textParams);
                    }
                }
                else
                {
                    DrawWithTextSelectionAndCursor(painter, drawText);
                }
            }
            else
            {
                base.DoRepaint(painter);
            }
        }

        protected internal override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt.GetEventTypeId() == KeyDownEvent.TypeId())
            {
                KeyDownEvent kde = evt as KeyDownEvent;
                if (!isDelayed || kde.character == '\n')
                {
                    SetValueAndNotify(text);
                }
            }
            else if (evt.GetEventTypeId() == ExecuteCommandEvent.TypeId())
            {
                ExecuteCommandEvent commandEvt = evt as ExecuteCommandEvent;
                string cmdName = commandEvt.commandName;
                if (!isDelayed && (cmdName == EventCommandNames.Paste || cmdName == EventCommandNames.Cut))
                {
                    SetValueAndNotify(text);
                }
            }
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (!isDelayed || evt.GetEventTypeId() == BlurEvent.TypeId())
            {
                SetValueAndNotify(text);
            }
        }
    }
}
