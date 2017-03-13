// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


// If you wish to modify this template do so and then regenerate the unity
// events with the command line as shown below from within the directory
// that the template lives in.
//
// perl ../../Tools/Build/GenerateUnityEvents.pl 5 UnityEvent.template .

using System;
using System.Reflection;
using UnityEngineInternal;
using UnityEngine.Scripting;

namespace UnityEngine.Events
{
    public delegate void UnityAction();

    [Serializable]
    public  class UnityEvent : UnityEventBase
    {
        [RequiredByNativeCode]
        public UnityEvent() {}

        public void AddListener(UnityAction call)
        {
            AddCall(GetDelegate(call));
        }

        public void RemoveListener(UnityAction call)
        {
            RemoveListener(call.Target, call.GetMethodInfo());
        }

        protected override MethodInfo FindMethod_Impl(string name, object targetObj)
        {
            return GetValidMethodInfo(targetObj, name, new Type[] {});
        }

        internal override BaseInvokableCall GetDelegate(object target, MethodInfo theFunction)
        {
            return new InvokableCall(target, theFunction);
        }

        private static BaseInvokableCall GetDelegate(UnityAction action)
        {
            return new InvokableCall(action);
        }

        private readonly object[] m_InvokeArray = new object[0];
        public void Invoke()
        {
            Invoke(m_InvokeArray);
        }


        internal void AddPersistentListener(UnityAction call)
        {
            AddPersistentListener(call, UnityEventCallState.RuntimeOnly);
        }

        internal void AddPersistentListener(UnityAction call, UnityEventCallState callState)
        {
            var count = GetPersistentEventCount();
            AddPersistentListener();
            RegisterPersistentListener(count, call);
            SetPersistentListenerState(count, callState);
        }

        internal void RegisterPersistentListener(int index, UnityAction call)
        {
            if (call == null)
            {
                Debug.LogWarning("Registering a Listener requires an action");
                return;
            }

            RegisterPersistentListener(index, call.Target as UnityEngine.Object, call.Method);
        }

    }
}
