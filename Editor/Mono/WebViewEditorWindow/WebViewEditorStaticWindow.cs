// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;
using System;
using UnityEditor;
using UnityEditorInternal;

namespace UnityEditor.Web
{
    internal abstract class WebViewEditorStaticWindow : WebViewEditorWindow , IHasCustomMenu
    {
        protected object m_GlobalObject = null;

        // In order to use this class as a parent
        // You must copy paste the section below in the child class.
        //
        //static internal WebView s_WebView;
        //internal override WebView webView
        //{
        //  get {return s_WebView;}
        //  set {s_WebView = value;}
        //}


        // Use EditorWindow.GetWindow<WebViewEditorStaticWindow> to get/create an instance of this class;
        protected WebViewEditorStaticWindow()
        {
            m_GlobalObject = null;
        }

        override public void OnDestroy()
        {
            OnBecameInvisible();
            m_GlobalObject = null;
        }

        override public void OnInitScripting()
        {
            base.SetScriptObject();
        }
    }
}
