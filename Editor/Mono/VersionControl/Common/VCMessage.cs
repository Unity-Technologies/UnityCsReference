// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.VersionControl
{
    // Shared message class to give plugin messages consistency
    public partial class Message
    {
        public void Show()
        {
            Message.Info(message);
        }

        private static void Info(string message)
        {
            Debug.Log("Version control:\n" + message);
        }
    }
}
