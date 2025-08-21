// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEditor;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class EditorApplicationProxy
    {
        internal delegate void HandleUpdateMainWindowTitle(ApplicationTitleDescriptorProxy applicationTitleDescriptor);

        const string EventName = "updateMainWindowTitle";

        internal static void RegisterUpdateMainWindowTitle(HandleUpdateMainWindowTitle function)
        {
            var eventInfo = typeof(EditorApplication).GetEvent(EventName, BindingFlags.NonPublic | BindingFlags.Static);
            var addMethod = eventInfo?.GetAddMethod(true);
            addMethod?.Invoke(null, new object[] { (Action<object>)HandleUpdateMainWindowTitle });
            return;
            // NOTE: happens only once per session (not domain reload)
            void HandleUpdateMainWindowTitle(object o)
            {
                var applicationTitleDescriptorProxy = new ApplicationTitleDescriptorProxy(o);
                function?.Invoke(applicationTitleDescriptorProxy);
            }
        }
    }
}
