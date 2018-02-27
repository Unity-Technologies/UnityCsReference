// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;

namespace UnityEditor.UIAutomation
{
    //TODO: Should this be part of the framework?
    class TestEditorWindow : EditorWindow
    {
        public FakeCursor fakeCursor = new FakeCursor();
    }
}
