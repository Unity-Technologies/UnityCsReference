// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngineInternal;
using uei = UnityEngine.Internal;

namespace UnityEngine
{
    // MonoBehaviour is the base class every script derives from.
    [RequiredByNativeCode]
    [NativeHeader("Runtime/Mono/MonoBehaviour.h")]
    [NativeHeader("Runtime/Scripting/DelayedCallUtility.h")]
    public class MonoBehaviour : Behaviour
    {
        public MonoBehaviour()
        {
            ConstructorCheck(this);
        }


        // Is any invoke pending on this MonoBehaviour?
        public bool IsInvoking()
        {
            return Internal_IsInvokingAll(this);
        }

        public void CancelInvoke()
        {
            Internal_CancelInvokeAll(this);
        }

        // Invokes the method /methodName/ in time seconds.
        public void Invoke(string methodName, float time)
        {
            InvokeDelayed(this, methodName, time, 0.0f);
        }

        // Invokes the method /methodName/ in /time/ seconds.
        public void InvokeRepeating(string methodName, float time, float repeatRate)
        {
            if (repeatRate <= 0.00001f && repeatRate != 0.0f)
                throw new UnityException("Invoke repeat rate has to be larger than 0.00001F)");

            InvokeDelayed(this, methodName, time, repeatRate);
        }

        // Cancels all Invoke calls with name /methodName/ on this behaviour.
        public void CancelInvoke(string methodName)
        {
            CancelInvoke(this, methodName);
        }

        // Is any invoke on /methodName/ pending?
        public bool IsInvoking(string methodName)
        {
            return IsInvoking(this, methodName);
        }

        [uei.ExcludeFromDocs]
        public Coroutine StartCoroutine(string methodName)
        {
            object value = null;
            return StartCoroutine(methodName, value);
        }

        // Starts a coroutine named /methodName/.
        public Coroutine StartCoroutine(string methodName, [uei.DefaultValue("null")] object value)
        {
            if (string.IsNullOrEmpty(methodName))
                throw new NullReferenceException("methodName is null or empty");

            if (!IsObjectMonoBehaviour(this))
                throw new ArgumentException("Coroutines can only be stopped on a MonoBehaviour");

            return StartCoroutineManaged(methodName, value);
        }

        // Starts a coroutine.
        public Coroutine StartCoroutine(IEnumerator routine)
        {
            if (routine == null)
                throw new NullReferenceException("routine is null");

            if (!IsObjectMonoBehaviour(this))
                throw new ArgumentException("Coroutines can only be stopped on a MonoBehaviour");

            return StartCoroutineManaged2(routine);
        }

        //*undocumented*
        [Obsolete("StartCoroutine_Auto has been deprecated. Use StartCoroutine instead (UnityUpgradable) -> StartCoroutine([mscorlib] System.Collections.IEnumerator)", false)]
        public Coroutine StartCoroutine_Auto(IEnumerator routine)
        {
            return StartCoroutine(routine);
        }

        // Stop a coroutine.
        public void StopCoroutine(IEnumerator routine)
        {
            if (routine == null)
                throw new NullReferenceException("routine is null");

            if (!IsObjectMonoBehaviour(this))
                throw new ArgumentException("Coroutines can only be stopped on a MonoBehaviour");

            StopCoroutineFromEnumeratorManaged(routine);
        }

        // Stop a coroutine.
        public void StopCoroutine(Coroutine routine)
        {
            if (routine == null)
                throw new NullReferenceException("routine is null");

            if (!IsObjectMonoBehaviour(this))
                throw new ArgumentException("Coroutines can only be stopped on a MonoBehaviour");

            StopCoroutineManaged(routine);
        }

        // Stops all coroutines named /methodName/ running on this behaviour.
        public extern void StopCoroutine(string methodName);

        // Stops all coroutines running on this behaviour.
        public extern void StopAllCoroutines();

        public extern bool useGUILayout { get; set; }

        // Allow a specific instance of a MonoBehaviour to run in edit mode (only available in the editor)
        public extern bool runInEditMode { get; set; }

        // Logs message to the Unity Console. This function is identical to [[Debug.Log]].
        public static void print(object message)
        {
            Debug.Log(message);
        }


        [NativeMethod(IsThreadSafe = true)]
        extern static void ConstructorCheck([Writable] Object self);

        [FreeFunction("CancelInvoke")]
        extern static void Internal_CancelInvokeAll(MonoBehaviour self);

        [FreeFunction("IsInvoking")]
        extern static bool Internal_IsInvokingAll(MonoBehaviour self);

        [FreeFunction]
        extern static void InvokeDelayed(MonoBehaviour self, string methodName, float time, float repeatRate);

        [FreeFunction]
        extern static void CancelInvoke(MonoBehaviour self, string methodName);

        [FreeFunction]
        extern static bool IsInvoking(MonoBehaviour self, string methodName);

        [FreeFunction]
        extern static bool IsObjectMonoBehaviour(Object obj);

        extern Coroutine StartCoroutineManaged(string methodName, object value);

        extern Coroutine StartCoroutineManaged2(IEnumerator enumerator);

        extern void StopCoroutineManaged(Coroutine routine);

        extern void StopCoroutineFromEnumeratorManaged(IEnumerator routine);

        extern internal string GetScriptClassName();
    }
}
