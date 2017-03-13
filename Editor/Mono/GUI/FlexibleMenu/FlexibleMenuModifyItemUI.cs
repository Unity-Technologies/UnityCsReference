// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;

namespace UnityEditor
{
    abstract class FlexibleMenuModifyItemUI : PopupWindowContent
    {
        public enum MenuType { Add, Edit };
        protected MenuType m_MenuType;
        public object m_Object;
        protected Action<object> m_AcceptedCallback;
        private bool m_IsInitialized;

        public override void OnClose()
        {
            m_Object = null;
            m_AcceptedCallback = null;
            m_IsInitialized = false;
            EditorApplication.RequestRepaintAllViews(); // When closed ensure FlexibileMenu gets repainted so hover can be removed
        }

        public void Init(MenuType menuType, object obj, Action<object> acceptedCallback)
        {
            m_MenuType = menuType;
            m_Object = obj;
            m_AcceptedCallback = acceptedCallback;
            m_IsInitialized = true;
        }

        public void Accepted()
        {
            if (m_AcceptedCallback != null)
                m_AcceptedCallback(m_Object);
            else
                Debug.LogError("Missing callback. Did you remember to call Init ?");
        }

        public bool IsShowing()
        {
            return m_IsInitialized;
        }
    }
} // namespace
