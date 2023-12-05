// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    public partial class PhysicsDebugWindow : EditorWindow
    {
        private void DrawInternalTab()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(Style.connectSDKVisualDebugger))
                Physics.ConnectPhysicsSDKVisualDebugger();

            if(GUILayout.Button(Style.disconnectSDKVisualDebugger))
                Physics.DisconnectPhysicsSDKVisualDebugger();

            GUILayout.EndHorizontal();
        }
    }
}
