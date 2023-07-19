// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Accessibility;

namespace UnityEditor.Accessibility
{
    internal static class AccessibilityEditor
    {
        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            AccessibilityManager.Internal_Initialize();
        }
    }
}
