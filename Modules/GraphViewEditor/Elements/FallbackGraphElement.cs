// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    class FallbackGraphElement : GraphElement
    {
        Label m_Text;

        public FallbackGraphElement()
        {
            style.backgroundColor = Color.grey;
            m_Text = new Label();
            Add(m_Text);
        }

        public override void OnDataChanged()
        {
            var elementPresenter = GetPresenter<GraphElementPresenter>();
            m_Text.text = "Fallback for " + elementPresenter.GetType() + ". No GraphElement registered for this type in this view.";
        }
    }
}
