// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using System;
using System.Runtime.InteropServices;

namespace UnityEditor
{
    // Render web content in Editor GUI.
    // GUI utility class that wraps a web view for embedding HTML content inside the Unity Editor GUI.
    //
    // This class differs from the stateless GUI widgets in GUI and GUILayout, as it maintains a state inside an object instance.
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(Header = "Editor/Mono/WebView.bindings.h")]
    [MarshalUnityObjectAs(typeof(MonoBehaviour))]
    internal sealed class WebView : ScriptableObject
    {
        #pragma warning disable 649
        [SerializeField] private MonoReloadableIntPtr WebViewWindow;
        #pragma warning restore

        public void OnDestroy()
        {
            DestroyWebView();
        }

        [FreeFunction("WebViewBindings::DestroyWebView", HasExplicitThis = true)]
        extern private void DestroyWebView();

        [FreeFunction("WebViewBindings::InitWebView", HasExplicitThis = true, ThrowsException = true)]
        extern public void InitWebView(GUIView host, int x, int y, int width, int height, bool showResizeHandle);

        [FreeFunction("WebViewBindings::ExecuteJavascript", HasExplicitThis = true)]
        extern public void ExecuteJavascript(string scriptCode);
        [FreeFunction("WebViewBindings::LoadURL", HasExplicitThis = true)]
        extern public void LoadURL(string url);
        [FreeFunction("WebViewBindings::LoadFile", HasExplicitThis = true)]
        extern public void LoadFile(string path);
        [FreeFunction("WebViewBindings::DefineScriptObject", HasExplicitThis = true)]
        extern public bool DefineScriptObject(string path, ScriptableObject obj);
        [FreeFunction("WebViewBindings::SetDelegateObject", HasExplicitThis = true)]
        extern public void SetDelegateObject(ScriptableObject value);
        [FreeFunction("WebViewBindings::SetHostView", HasExplicitThis = true)]
        extern public void SetHostView(GUIView view);
        [FreeFunction("WebViewBindings::SetSizeAndPosition", HasExplicitThis = true)]
        extern public void SetSizeAndPosition(int x, int y, int width, int height);
        [FreeFunction("WebViewBindings::SetFocus", HasExplicitThis = true)]
        extern public void SetFocus(bool value);
        [FreeFunction("WebViewBindings::HasApplicationFocus", HasExplicitThis = true)]
        extern public bool HasApplicationFocus();
        [FreeFunction("WebViewBindings::SetApplicationFocus", HasExplicitThis = true)]
        extern public void SetApplicationFocus(bool applicationFocus);
        [FreeFunction("WebViewBindings::Show", HasExplicitThis = true)]
        extern public void Show();
        [FreeFunction("WebViewBindings::Hide", HasExplicitThis = true)]
        extern public void Hide();
        [FreeFunction("WebViewBindings::Back", HasExplicitThis = true)]
        extern public void Back();
        [FreeFunction("WebViewBindings::Forward", HasExplicitThis = true)]
        extern public void Forward();
        [FreeFunction("WebViewBindings::SendOnEvent", HasExplicitThis = true)]
        extern public void SendOnEvent(string jsonStr);
        [FreeFunction("WebViewBindings::Reload", HasExplicitThis = true)]
        extern public void Reload();
        // Allow Right click menu even in non-developper's build
        [FreeFunction("WebViewBindings::AllowRightClickMenu", HasExplicitThis = true)]
        extern public void AllowRightClickMenu(bool allowRightClickMenu);
        [FreeFunction("WebViewBindings::ShowDevTools", HasExplicitThis = true)]
        extern public void ShowDevTools();
        [FreeFunction("WebViewBindings::ToggleMaximize", HasExplicitThis = true)]
        extern public void ToggleMaximize();

        [FreeFunction("WebViewWindow::OnDomainReload")]
        extern internal static void OnDomainReload();

        public static implicit operator bool(WebView exists)
        {
            return exists != null && !exists.IntPtrIsNull();
        }

        [FreeFunction("WebViewBindings::IntPtrIsNull", HasExplicitThis = true)]
        extern private bool IntPtrIsNull();

        // The following methods are called on the delegate object
        // installed with SetDelegateObject():
        //
        // Called when the web view begins loading a new page and has set up its
        // JavaScript execution environment.  Allows adding custom objects before
        // any JavaScript code actually gets executed.
        // void OnInitScripting();
        //
        // Called when the browser sets the title for the page being loaded.
        // void OnReceiveTitle(string title);
        //
        // Called when the current location in the page has been changed.
        // void OnLocationChanged(string url);
        //
        // Called when a new page is being loaded.
        // void OnBeginLoading(string url);
        //
        // Called when a page has finished loading.
        // void OnFinishLoading(string url);
        //
        // Called when a page has failed loading.System.Serializable
        // void OnLoadError(string url);
    }

    // Add a callback class for OnQuery
    [System.Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class WebViewV8CallbackCSharp
    {
        #pragma warning disable 169
        [SerializeField] IntPtr m_ThisDummy;
        #pragma warning restore

        extern public void Callback(string result);

        public void OnDestroy()
        {
            DestroyCallBack();
        }

        [NativeName("Destroy")] extern private void DestroyCallBack();
    }
}
