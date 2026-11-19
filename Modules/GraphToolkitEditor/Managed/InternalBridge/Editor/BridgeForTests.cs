// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolsAuthoringFramework.InternalEditorBridge
{
    static class BridgeForTests
    {
        public static void SetDisableInputEvents(this EditorWindow window, bool value)
        {
            window.disableInputEvents = value;
        }

        public static void ClearPersistentViewData(this EditorWindow window)
        {
            window.ClearPersistentViewData();
        }

        public static void DisableViewDataPersistence(this EditorWindow window)
        {
            window.DisableViewDataPersistence();
        }
    }
}
