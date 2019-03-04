// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal class RenderDocUtil
    {
        static internal bool macEditor => Application.platform == RuntimePlatform.OSXEditor;
        public static readonly string loadRenderDocLabel = macEditor ? "Load Xcode frame debugger" : "Load RenderDoc";
        public static readonly string openInRenderDocLabel = macEditor ? "Capture the current view and open in Xcode frame debugger" : "Capture the current view and open in RenderDoc";
    }
}
