// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.UIElements
{
    public class TextField : TextInputFieldBase<string>
    {
        public new class UxmlFactory : UxmlFactory<TextField, UxmlTraits> {}

        public new class UxmlTraits : TextInputFieldBase<string>.UxmlTraits
        {
            UxmlBoolAttributeDescription m_Multiline = new UxmlBoolAttributeDescription { name = "multiline" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                TextField field = ((TextField)ve);
                field.multiline = m_Multiline.GetValueFromBag(bag, cc);
                field.SetValueWithoutNotify(field.text);
            }
        }

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

        public override string value
        {
            get { return base.value; }
            set
            {
                base.value = value;
                text = m_Value;
            }
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            base.SetValueWithoutNotify(newValue);
            text = m_Value;
        }

        public void SelectRange(int cursorIndex, int selectionIndex)
        {
            if (editorEngine != null)
            {
                editorEngine.cursorIndex = cursorIndex;
                editorEngine.selectIndex = selectionIndex;
            }
        }

        public override void OnPersistentDataReady()
        {
            base.OnPersistentDataReady();

            string key = GetFullHierarchicalPersistenceKey();

            OverwriteFromPersistedData(this, key);

            // Here we must make sure the value is restored on screen from the saved value !
            text = m_Value;
        }

        internal override void SyncTextEngine()
        {
            editorEngine.multiline = multiline;
            editorEngine.isPasswordField = isPasswordField;

            base.SyncTextEngine();
        }

        protected override void DoRepaint(IStylePainter painter)
        {
            var stylePainter = (IStylePainterInternal)painter;
            if (isPasswordField)
            {
                // if we use system keyboard we will have normal text returned (hiding symbols is done inside os)
                // so before drawing make sure we hide them ourselves
                string drawText = "".PadRight(text.Length, maskChar);
                if (!hasFocus)
                {
                    // We don't have the focus, don't draw the selection and cursor
                    if (!string.IsNullOrEmpty(drawText) && contentRect.width > 0.0f && contentRect.height > 0.0f)
                    {
                        var textParams = TextStylePainterParameters.GetDefault(this, text);
                        textParams.text = drawText;
                        stylePainter.DrawText(textParams);
                    }
                }
                else
                {
                    DrawWithTextSelectionAndCursor(stylePainter, drawText);
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
                    value = text;
                }
            }
            else if (evt.GetEventTypeId() == ExecuteCommandEvent.TypeId())
            {
                ExecuteCommandEvent commandEvt = evt as ExecuteCommandEvent;
                string cmdName = commandEvt.commandName;
                if (!isDelayed && (cmdName == EventCommandNames.Paste || cmdName == EventCommandNames.Cut))
                {
                    value = text;
                }
            }
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (isDelayed && evt.GetEventTypeId() == BlurEvent.TypeId())
            {
                value = text;
            }
        }
    }
}
