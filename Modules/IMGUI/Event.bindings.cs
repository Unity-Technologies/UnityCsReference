// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeHeader("Modules/IMGUI/Event.bindings.h"),
     StaticAccessor("GUIEvent", StaticAccessorType.DoubleColon)]
    partial class Event
    {
        [NativeProperty("type", false, TargetType.Field)] public extern EventType rawType { get; }
        [NativeProperty("mousePosition", false, TargetType.Field)] public extern Vector2 mousePosition { get; set; }
        [NativeProperty("delta", false, TargetType.Field)] public extern Vector2 delta { get; set; }
        [NativeProperty("pointerType", false, TargetType.Field)] public extern PointerType pointerType { get; set; }
        [NativeProperty("button", false, TargetType.Field)] public extern int button { get; set; }
        [NativeProperty("modifiers", false, TargetType.Field)] public extern EventModifiers modifiers { get; set; }
        [NativeProperty("pressure", false, TargetType.Field)] public extern float pressure { get; set; }
        [NativeProperty("clickCount", false, TargetType.Field)] public extern int clickCount { get; set; }
        [NativeProperty("character", false, TargetType.Field)] public extern char character { get; set; }
        [NativeProperty("keycode", false, TargetType.Field)] public extern KeyCode keyCode { get; set; }
        [NativeProperty("displayIndex", false, TargetType.Field)] public extern int displayIndex { get; set; }

        public extern EventType type
        {
            [FreeFunction("GUIEvent::GetType", HasExplicitThis = true)] get;
            [FreeFunction("GUIEvent::SetType", HasExplicitThis = true)] set;
        }

        public extern string commandName
        {
            [FreeFunction("GUIEvent::GetCommandName", HasExplicitThis = true)] get;
            [FreeFunction("GUIEvent::SetCommandName", HasExplicitThis = true)] set;
        }

        [NativeMethod("Use")]
        private extern void Internal_Use();

        [FreeFunction("GUIEvent::Internal_Create", IsThreadSafe = true)]
        private static extern IntPtr Internal_Create(int displayIndex);

        [FreeFunction("GUIEvent::Internal_Destroy", IsThreadSafe = true)]
        private static extern void Internal_Destroy(IntPtr ptr);

        [FreeFunction("GUIEvent::Internal_Copy", IsThreadSafe = true)]
        private static extern IntPtr Internal_Copy(IntPtr otherPtr);

        [FreeFunction("GUIEvent::GetTypeForControl", HasExplicitThis = true)]
        public extern EventType GetTypeForControl(int controlID);

        [VisibleToOtherModules("UnityEngine.UIElementsModule"),
         FreeFunction("GUIEvent::CopyFromPtr", IsThreadSafe = true, HasExplicitThis = true)]
        internal extern void CopyFromPtr(IntPtr ptr);

        public static extern bool PopEvent([NotNull] Event outEvent);
        public static extern int GetEventCount();

        private static extern void Internal_SetNativeEvent(IntPtr ptr);

        [RequiredByNativeCode]
        internal static void Internal_MakeMasterEventCurrent(int displayIndex)
        {
            if (s_MasterEvent == null)
                s_MasterEvent = new Event(displayIndex);
            s_MasterEvent.displayIndex = displayIndex;
            s_Current = s_MasterEvent;
            Internal_SetNativeEvent(s_MasterEvent.m_Ptr);
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern int GetDoubleClickTime();
    }
}
