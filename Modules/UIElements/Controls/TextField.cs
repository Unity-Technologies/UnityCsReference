// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    public class TextField : TextInputBaseField<string>
    {
        // This property to alleviate the fact we have to cast all the time
        TextInput textInput => (TextInput)textInputBase;

        public new class UxmlFactory : UxmlFactory<TextField, UxmlTraits> {}
        public new class UxmlTraits : BaseFieldTraits<string, UxmlStringAttributeDescription>
        {
            UxmlBoolAttributeDescription m_Multiline = new UxmlBoolAttributeDescription { name = "multiline" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                TextField field = ((TextField)ve);
                field.multiline = m_Multiline.GetValueFromBag(bag, cc);
                base.Init(ve, bag, cc);
            }
        }
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

        public void SelectRange(int rangeCursorIndex, int selectionIndex)
        {
            textInput.SelectRange(rangeCursorIndex, selectionIndex);
        }

        public new static readonly string ussClassName = "unity-text-field";

        public TextField()
            : this(null) {}

        public TextField(int maxLength, bool multiline, bool isPasswordField, char maskChar)
            : this(null, maxLength, multiline, isPasswordField, maskChar) {}

        public TextField(string label)
            : this(label, kMaxLengthNone, false, false, char.MinValue) {}

        public TextField(string label, int maxLength, bool multiline, bool isPasswordField, char maskChar)
            : base(label, maxLength, maskChar, new TextInput() { name = "unity-text-input" })
        {
            AddToClassList(ussClassName);
            pickingMode = PickingMode.Ignore;
            SetValueWithoutNotify("");
            this.multiline = multiline;
            this.isPasswordField = isPasswordField;
        }

        public override string value
        {
            get { return base.value; }
            set
            {
                base.value = value;
                text = rawValue;
            }
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            base.SetValueWithoutNotify(newValue);
            text = rawValue;
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();

            string key = GetFullHierarchicalViewDataKey();

            OverwriteFromViewData(this, key);

            // Here we must make sure the value is restored on screen from the saved value !
            text = rawValue;
        }

        class TextInput : TextInputBase
        {
            TextField parentTextField => (TextField)parentField;

            internal TextInput()
                : base()
            {
            }

            public void SelectRange(int cursorIndex, int selectionIndex)
            {
                if (editorEngine != null)
                {
                    editorEngine.cursorIndex = cursorIndex;
                    editorEngine.selectIndex = selectionIndex;
                }
            }

            internal override void SyncTextEngine()
            {
                if (parentTextField != null)
                {
                    editorEngine.multiline = parentTextField.multiline;
                    editorEngine.isPasswordField = parentTextField.isPasswordField;
                }

                base.SyncTextEngine();
            }

            internal override void DoRepaint(IStylePainter painter)
            {
                var stylePainter = (IStylePainterInternal)painter;
                if (parentTextField.isPasswordField)
                {
                    // if we use system keyboard we will have normal text returned (hiding symbols is done inside os)
                    // so before drawing make sure we hide them ourselves
                    string drawText = "".PadRight(parentTextField.text.Length, parentTextField.maskChar);
                    if (!hasFocus)
                    {
                        // We don't have the focus, don't draw the selection and cursor
                        if (!string.IsNullOrEmpty(drawText) && contentRect.width > 0.0f && contentRect.height > 0.0f)
                        {
                            var textParams = TextStylePainterParameters.GetDefault(this, parentTextField.text);
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

                if (evt == null)
                {
                    return;
                }

                if (evt.eventTypeId == KeyDownEvent.TypeId())
                {
                    KeyDownEvent kde = evt as KeyDownEvent;
                    if (!parentTextField.isDelayed || kde.character == '\n')
                    {
                        parentTextField.value = parentTextField.text;
                    }
                }
                else if (evt.eventTypeId == ExecuteCommandEvent.TypeId())
                {
                    ExecuteCommandEvent commandEvt = evt as ExecuteCommandEvent;
                    string cmdName = commandEvt.commandName;
                    if (!parentTextField.isDelayed && (cmdName == EventCommandNames.Paste || cmdName == EventCommandNames.Cut))
                    {
                        parentTextField.value = parentTextField.text;
                    }
                }
            }

            protected internal override void ExecuteDefaultAction(EventBase evt)
            {
                base.ExecuteDefaultAction(evt);

                if (parentTextField.isDelayed && evt?.eventTypeId == BlurEvent.TypeId())
                {
                    parentTextField.value = parentTextField.text;
                }
            }
        }
    }
}
