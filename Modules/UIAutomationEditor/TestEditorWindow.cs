// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIAutomationEditor not yet converted
using System;
using UnityEditor;

namespace UnityEditor.UIAutomation
{
    //TODO: Should this be part of the framework?
    [UIFramework(UIFrameworkUsage.Excluded)]
    class TestEditorWindow : EditorWindow
    {
        [NonSerialized]
        public FakeCursor fakeCursor = new FakeCursor();
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
