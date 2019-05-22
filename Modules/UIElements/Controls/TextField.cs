// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    public class TextField : TextInputBaseField<string>
    {
        // This property to alleviate the fact we have to cast all the time
        TextInput textInput => (TextInput)textInputBase;

        // This is to save the value of the tabindex of the visual input to achieve the IMGUI behaviour of tabbing on multiline TextField.
        int m_VisualInputTabIndex;

        public new class UxmlFactory : UxmlFactory<TextField, UxmlTraits> {}
        public new class UxmlTraits : TextInputBaseField<string>.UxmlTraits
        {
            UxmlBoolAttributeDescription m_Multiline = new UxmlBoolAttributeDescription { name = "multiline" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                TextField field = ((TextField)ve);
                field.multiline = m_Multiline.GetValueFromBag(bag, cc);
                base.Init(ve, bag, cc);
            }
        }

        public bool multiline
        {
            get { return textInput.multiline; }
            set { textInput.multiline = value; }
        }


        public void SelectRange(int rangeCursorIndex, int selectionIndex)
        {
            textInput.SelectRange(rangeCursorIndex, selectionIndex);
        }

        public new static readonly string ussClassName = "unity-text-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        public TextField()
            : this(null) {}

        public TextField(int maxLength, bool multiline, bool isPasswordField, char maskChar)
            : this(null, maxLength, multiline, isPasswordField, maskChar) {}

        public TextField(string label)
            : this(label, kMaxLengthNone, false, false, kMaskCharDefault) {}

        public TextField(string label, int maxLength, bool multiline, bool isPasswordField, char maskChar)
            : base(label, maxLength, maskChar, new TextInput())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);

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

        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            // The following code is to help achieve the following behaviour:
            // On IMGUI, a TextArea "in edit mode" is accepting TAB, doing a Shift+Return will get out of the Edit mode
            //     and a TAB will allow the user to get to the next control...
            // To mimic that behaviour in UIE, when in focused-non-edit-mode, we have to make sure the input is not "tabbable".
            //     So, each time, either the main TextField or the Label is receiving the focus, we remove the tabIndex on
            //     the input, and we put it back when the BlurEvent is received.
            if (multiline)
            {
                if ((evt?.eventTypeId == FocusInEvent.TypeId() && evt?.leafTarget == this) ||
                    (evt?.eventTypeId == FocusInEvent.TypeId() && evt?.leafTarget == labelElement))
                {
                    m_VisualInputTabIndex = visualInput.tabIndex;
                    visualInput.tabIndex = -1;
                }
                else if ((evt?.eventTypeId == BlurEvent.TypeId() && evt?.leafTarget == this) ||
                         (evt?.eventTypeId == BlurEvent.TypeId() && evt?.leafTarget == labelElement))
                {
                    visualInput.tabIndex = m_VisualInputTabIndex;
                }
            }
        }

        class TextInput : TextInputBase
        {
            TextField parentTextField => (TextField)parent;

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
            internal TextInput()
            {
                generateVisualContent = null; // Wipe base class's handler
                generateVisualContent += OnOnGenerateVisualContentTextInput;
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
                    editorEngine.multiline = multiline;
                    editorEngine.isPasswordField = isPasswordField;
                }

                base.SyncTextEngine();
            }

            private void OnOnGenerateVisualContentTextInput(MeshGenerationContext mgc)
            {
                if (isPasswordField)
                {
                    // if we use system keyboard we will have normal text returned (hiding symbols is done inside os)
                    // so before drawing make sure we hide them ourselves
                    string drawText = "".PadRight(text.Length, parentTextField.maskChar);
                    if (!hasFocus)
                    {
                        // We don't have the focus, don't draw the selection and cursor
                        if (!string.IsNullOrEmpty(drawText) && contentRect.width > 0.0f && contentRect.height > 0.0f)
                        {
                            var textParams = MeshGenerationContextUtils.TextParams.MakeStyleBased(this, text);
                            textParams.text = drawText;
                            mgc.Text(textParams);
                        }
                    }
                    else
                    {
                        DrawWithTextSelectionAndCursor(mgc, drawText);
                    }
                }
                else
                {
                    base.OnGenerateVisualContent(mgc);
                }
            }

            protected override void ExecuteDefaultActionAtTarget(EventBase evt)
            {
                base.ExecuteDefaultActionAtTarget(evt);
                if (evt == null)
                {
                    return;
                }

                if (evt.eventTypeId == KeyDownEvent.TypeId())
                {
                    KeyDownEvent kde = evt as KeyDownEvent;

                    if (!parentTextField.isDelayed || (!multiline && ((kde?.keyCode == KeyCode.KeypadEnter) || (kde?.keyCode == KeyCode.Return))))
                    {
                        parentTextField.value = text;
                    }

                    if (multiline)
                    {
                        if (kde?.character == '\t' && kde.modifiers == EventModifiers.None)
                        {
                            kde?.StopPropagation();
                            kde?.PreventDefault();
                        }
                        else if (((kde?.character == 3) && (kde?.shiftKey == true)) || // KeyCode.KeypadEnter
                                 ((kde?.character == '\n') && (kde?.shiftKey == true))) // KeyCode.Return
                        {
                            parent.Focus();
                        }
                    }
                    else if ((kde?.character == 3) ||    // KeyCode.KeypadEnter
                             (kde?.character == '\n'))   // KeyCode.Return
                    {
                        parent.Focus();
                    }
                }
                else if (evt.eventTypeId == ExecuteCommandEvent.TypeId())
                {
                    ExecuteCommandEvent commandEvt = evt as ExecuteCommandEvent;
                    string cmdName = commandEvt.commandName;
                    if (!parentTextField.isDelayed && (cmdName == EventCommandNames.Paste || cmdName == EventCommandNames.Cut))
                    {
                        parentTextField.value = text;
                    }
                }
            }

            protected override void ExecuteDefaultAction(EventBase evt)
            {
                base.ExecuteDefaultAction(evt);

                if (parentTextField.isDelayed && evt?.eventTypeId == BlurEvent.TypeId())
                {
                    parentTextField.value = text;
                }
            }
        }
    }
}
