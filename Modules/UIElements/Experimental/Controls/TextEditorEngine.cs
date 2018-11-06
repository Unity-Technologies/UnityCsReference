// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    internal class TextEditorEngine : TextEditor
    {
        internal delegate void OnDetectFocusChangeFunction();
        internal delegate void OnIndexChangeFunction();

        private OnDetectFocusChangeFunction m_DetectFocusChangeFunction;
        private OnIndexChangeFunction m_IndexChangeFunction;

        public TextEditorEngine(OnDetectFocusChangeFunction detectFocusChange, OnIndexChangeFunction indexChangeFunction)
        {
            m_DetectFocusChangeFunction = detectFocusChange;
            m_IndexChangeFunction = indexChangeFunction;
        }

        internal override Rect localPosition
        {
            get { return new Rect(0, 0, position.width, position.height); }
        }

        internal override void OnDetectFocusChange()
        {
            m_DetectFocusChangeFunction();
        }

        internal override void OnCursorIndexChange()
        {
            m_IndexChangeFunction();
        }

        internal override void OnSelectIndexChange()
        {
            m_IndexChangeFunction();
        }
    }
}
