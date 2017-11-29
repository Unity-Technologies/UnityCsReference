// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    public class SimpleElement : GraphElement
    {
        Label m_Text;

        public SimpleElement()
        {
            m_Text = new Label();
            Add(m_Text);
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();
            var elementPresenter = GetPresenter<SimpleElementPresenter>();
            m_Text.text = elementPresenter.title;
        }
    }
}
