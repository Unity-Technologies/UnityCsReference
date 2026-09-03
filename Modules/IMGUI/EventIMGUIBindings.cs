// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine
{

    [NativeHeader("Modules/IMGUI/IMGUIEvent.bindings.h"),
     StaticAccessor("GUIEvent", StaticAccessorType.DoubleColon)]
    internal static class EventIMGUIBindings
    {
        [NoAutoStaticsCleanup] // owns a native event buffer (m_Ptr) registered via Internal_SetNativeEvent; GC-finalizing it while native holds the ptr is a use-after-free; CleanupRoots() handles app-quit
        private static Event s_Current;
        [NoAutoStaticsCleanup] // same ownership as s_Current; Internal_MakeMasterEventCurrent reuses it across reloads; CleanupRoots() handles app-quit
        private static Event s_MasterEvent;

        // GUIState-coupled native shims, kept in the IMGUI module (they resolve against the active GUIState).
        [FreeFunction("GUIEvent::GetType")]
        private static extern EventType ResolveType(IntPtr self);

        [FreeFunction("GUIEvent::GetTypeForControl")]
        private static extern EventType ResolveTypeForControl(IntPtr self, int controlID);

        [FreeFunction("GUIEvent::Internal_SetNativeEvent")]
        private static extern void SetNativeEvent(IntPtr ptr);

        private static Event CurrentGetter()
        {
            // Outside OnGUI (guiDepth == 0) there is no current event. Editor-only for backwards compat.
            return GUIUtility.guiDepth > 0 ? s_Current : null;
        }

        private static void CurrentSetter(Event value)
        {
            s_Current = value ?? s_MasterEvent;
            SetNativeEvent(s_Current.m_Ptr);
        }

        internal static void CleanupRoots()
        {
            // Lets GC collect the root events before Unity managers are destroyed on application quit.
            s_Current = null;
            s_MasterEvent = null;
        }

        [RequiredByNativeCode]
        internal static void Internal_MakeMasterEventCurrent(int displayIndex)
        {
            s_MasterEvent ??= new Event(displayIndex);
            s_MasterEvent.displayIndex = displayIndex;
            s_Current = s_MasterEvent;
            SetNativeEvent(s_MasterEvent.m_Ptr);
        }

        // Registration runs from the static constructor, which the runtime triggers on the first access to
        // this class — in practice the first native Internal_MakeMasterEventCurrent call (see GUIState.cpp),
        // which happens as soon as IMGUI processes an event, in both the editor and players. Before that
        // (or when IMGUI never runs), Event's hooks stay null and Event uses its standalone fallbacks.
        static EventIMGUIBindings() => Register();

        static void Register()
        {
            Event.currentGetter = CurrentGetter;
            Event.currentSetter = CurrentSetter;
            Event.typeResolver = ResolveType;
            Event.typeForControlResolver = ResolveTypeForControl;
        }
    }
}
