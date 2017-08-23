// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    [Serializable]
    internal
    class MiniMapPresenter : GraphElementPresenter
    {
        public float maxHeight;
        public float maxWidth;

        [SerializeField]
        private bool m_Anchored;
        public bool anchored
        {
            get { return m_Anchored; }
            set { m_Anchored = value; }
        }

        protected new void OnEnable()
        {
            base.OnEnable();
            capabilities = Capabilities.Floating | Capabilities.Movable;
        }

        protected MiniMapPresenter()
        {
            maxWidth = 200;
            maxHeight = 180;
        }
    }
}
