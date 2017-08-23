// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    [Serializable]
    internal
    class SimpleElementPresenter : GraphElementPresenter
    {
        [SerializeField]
        private string m_Title;

        public string title
        {
            get { return m_Title; }
            set { m_Title = value; }
        }

        protected new void OnEnable()
        {
            base.OnEnable();
            title = string.Empty;
        }

        protected SimpleElementPresenter() {}
    }
}
