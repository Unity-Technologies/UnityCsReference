// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Accessibility
{
    [NativeHeader("Modules/Accessibility/Native/AccessibilityNodeDataTests.h")]
    [VisibleToOtherModules("UnityEditor.AccessibilityModule")]
    internal class AccessibilityNodeDataTests
    {
        internal static AccessibilityNodeData nodeDataFromNative;

        [NativeThrows] internal static extern void Test_GetNodeDataToNativeViaBinding(AccessibilityNodeData nodeData);
        [NativeThrows] internal static extern void Test_GetNodeDataToNativeViaProxy();
        internal static extern void Test_GetNodeDataFromNativeViaBinding(ref AccessibilityNodeData nodeData);
        internal static extern void Test_GetNodeDataFromNativeViaProxy();

        [RequiredByNativeCode]
        internal static void Internal_GetNodeDataFromManaged(ref AccessibilityNodeData nodeData)
        {
            nodeData = new AccessibilityNodeData
            {
                childIds = new[] { 1, 2, 3 },
                label = "Label",
                value = "Value",
                hint = "Hint",
                frame = new Rect(10, 20, 100, 200),
                nodeId = 4,
                parentId = 5,
                role = AccessibilityRole.Button,
                state = AccessibilityState.Selected,
                isActive = true,
                allowsDirectInteraction = true,
                implementsInvoked = true,
                implementsScrolled = true,
                implementsDismissed = true,
            };
        }

        [RequiredByNativeCode]
        internal static void Internal_GetNodeDataToManaged(in AccessibilityNodeData nodeData)
        {
            nodeDataFromNative = nodeData;
        }
    }
}

