// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    class FallbackGraphElement : GraphElement
    {
        public FallbackGraphElement()
        {
            style.backgroundColor = Color.grey;
            text = "";
        }

        public override void OnDataChanged()
        {
            var elementPresenter = GetPresenter<GraphElementPresenter>();
            text = "Fallback for " + elementPresenter.GetType() + ". No GraphElement registered for this type in this view.";
        }
    }
}
